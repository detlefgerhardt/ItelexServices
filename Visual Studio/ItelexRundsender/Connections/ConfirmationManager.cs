using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Utility;
using ItelexRundsender.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItelexRundsender.Languages;

namespace ItelexRundsender.Connections
{
	class ConfirmationManager: OutgoingConnectionManagerAbstract
	{
		private const string TAG = nameof(ConfirmationManager);

		private const bool FORCE = true;

		private const int MAX_PIN_SEND_RETRIES = 5;

#if DEBUG
		private const int TLN_MSG_INTERVAL_MIN = 1;
#else
		private const int TLN_MSG_INTERVAL_MIN = 5;
#endif

		private RundsenderDatabase _database;

		private System.Timers.Timer _sendTimer;
		private bool _sendPinActive = false;

		//private TickTimer _sendMsgTimer = new TickTimer();

		private int[] SendRetryIntervals = new int[Constants.MAIL_SEND_RETRIES] { 0, 1, 2, 5, 30 };

		public ConfirmationManager()
		{
			_database = RundsenderDatabase.Instance;

			_sendTimer = new System.Timers.Timer(1000);
			_sendTimer.Elapsed += SendTimer_Elapsed;
			_sendTimer.Start();
		}

		private void SendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			SendAllPins();
		}

		private void SendAllPins()
		{
			Task.Run(() =>
			{
				if (_sendPinActive) return;
				_sendPinActive = true;
				try
				{
					List<ConfirmationItem> confItems = _database.ConfirmationsLoadAll(false, Constants.MAIL_SEND_RETRIES);
					if (confItems == null || confItems.Count == 0) return;

					// set older active confirmations to finished
					CleanUpConfirmations(confItems);

					foreach (ConfirmationItem confItem in confItems)
					{
						if (!confItem.Finished && !confItem.Sent)
						{
							SendPinToTln(confItem);
						}
					}
				}
				finally
				{
					_sendPinActive = false;
				}
			});
		}

		private void CleanUpConfirmations(List<ConfirmationItem> confItems)
		{
			for (int i = confItems.Count - 1; i > 0; i--)
			{
				long userId = confItems[i].UserId;
				for (int j = i - 1; j >= 0; j--)
				{
					if (confItems[j].UserId == userId && !confItems[j].Finished)
					{
						// set old confirmation entry to finished
						confItems[j].Finished = true;
						_database.ConfirmationsUpdate(confItems[j]);
					}
				}
			}
		}

		private void SendPinToTln(ConfirmationItem confItem)
		{
#if DEBUG
			const int FIRST_SEC = 30;
#else
			const int FIRST_SEC = 120;
#endif

			DateTime utcNow = DateTime.UtcNow;
			if (utcNow < confItem.CreateTimeUtc.Value.AddSeconds(FIRST_SEC)) return;

			if (confItem.SentTimeUtc != null && utcNow < confItem.SentTimeUtc.Value.AddMinutes(SendRetryIntervals[confItem.SendRetries])) return;

			if (IsOutgoingConnectionActive(confItem.Number))
			{
				DispatchMsg(null, $"Connection to {confItem.Number} already active. Skip.");
				return;
			}

			//UserItem userItem = (from u in _users where u.UserId == confItem.UserId select u).FirstOrDefault();
			UserItem userItem = _database.UserLoadById(confItem.UserId);
			if (userItem == null) return;

#if DEBUG
			if (userItem.ItelexNumber != 211231) return;
#endif

			DispatchMsg(null, $"Send pin for {userItem.ItelexNumber} to {confItem.Number} ({confItem.SendRetries + 1}. retry)");

			Language lng = LanguageManager.Instance.GetLanguageOrDefaultByShortname(confItem.Language);
			string message = null;
			switch((ConfirmationTypes)confItem.Type)
			{
				case ConfirmationTypes.NewPin:
					message = LngText((int)LngKeys.SendRegistrationPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
					break;
				//case ConfirmationTypes.Redirect:
				//	message = LngText((int)LngKeys.SendRedirectionPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
				//	break;
				//case ConfirmationTypes.Changed:
				//	message = LngText((int)LngKeys.SendChangedPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
				//	break;
				default:
					return;
			}

			CallResult result = SendPinToTln2(confItem, userItem, message);
			confItem.SendRetries++;
			if (result.CallStatus == CallStatusEnum.Ok)
			{   // ok
				confItem.Sent = true;
				confItem.AnswerBack = result.Kennung.Name;
				DispatchMsg(null, $"Sent pin for {userItem.ItelexNumber} to {confItem.Number}: ok");
			}
			else
			{
				string msg = "";
				if (confItem.SendRetries >= MAX_PIN_SEND_RETRIES)
				{
					confItem.Finished = true; // too many retries
					msg = "too many retries";
				}
				else
				{
					msg = $"next retry in {SendRetryIntervals[confItem.SendRetries]} min.";
				}
				DispatchMsg(null, $"Send pin for {userItem.ItelexNumber} to {confItem.Number}: {result.RejectReason}, {msg}");
			}
			confItem.SentTimeUtc = DateTime.UtcNow; // last send retry
			_database.ConfirmationsUpdate(confItem);
		}

		private CallResult SendPinToTln2(ConfirmationItem confItem, UserItem userItem, string message)
		{
			_logger.Notice(TAG, nameof(SendPinToTln2), $"{confItem}");

			ItelexOutgoing outgoing = null;
			try
			{
				int connectionId = GetNextConnectionId();
				ItelexLogger itelexLogger = new ItelexLogger(connectionId, ItelexConnection.ConnectionDirections.Out,
						confItem.Number, Constants.LOG_PATH, Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT,
						Constants.SYSLOG_APPNAME);
				outgoing = new ItelexOutgoing(connectionId, confItem.Number, "send pin", itelexLogger);
				AddConnection(outgoing);
				ItelexOutgoingConfiguration config = new ItelexOutgoingConfiguration()
				{
					OutgoingType = new LoginSeqTypes[] { LoginSeqTypes.SendKg, LoginSeqTypes.GetKg },
					ItelexNumber = confItem.Number,
					OurItelexVersionStr = Helper.GetVersionCode(),
					OurAnswerbackStr = Constants.ANSWERBACK_RUND_DE,
					RetryCnt = confItem.SendRetries, // starting from 1
				};
				CallResult result = outgoing.StartOutgoing(config);

				if (!outgoing.IsConnected || outgoing.RejectReason != null)
				{
					_logger.Notice(TAG, nameof(SendPinToTln2), $"Disconnected by remote, reject-reason={outgoing.RejectReason}");
					outgoing.Dispose();
					return new CallResult(confItem.Number, null, CallStatusEnum.Reject, outgoing.RejectReason, "", null);
				}

				DispatchUpdateOutgoing();

				outgoing.SendAscii($"\r\n{message}\r\n\n");
				outgoing.Logoff(null);

				_logger.Debug(TAG, nameof(SendPinToTln2), $"Disconnect()");
				outgoing.Dispose();
				_logger.Notice(TAG, nameof(SendPinToTln2), $"Msgs send to {confItem.Number}");
				return new CallResult(confItem.Number, null, CallStatusEnum.Ok, "", outgoing.RemoteItelexVersionStr, outgoing.RemoteAnswerback);
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(SendPinToTln2), $"Error sending msgs to {confItem.Number}", ex);
				return new CallResult(confItem.Number, null, CallStatusEnum.Error, "", "", null);
			}
			finally
			{
				if (outgoing != null)
				{
					RemoveConnection(outgoing);
					DispatchUpdateOutgoing();
				}
			}
		}

		private void DispatchMsg(int? connectionId, string msg)
		{
			MessageDispatcher.Instance.Dispatch(connectionId, msg);
			_logger.Debug(TAG, nameof(DispatchMsg), $"{connectionId} {msg}");
		}

		private int _currentIdNumber = 0;
		private object _currentIdNumberLock = new object();

		public int GetNextConnectionId()
		{
			lock (_currentIdNumberLock)
			{
				_currentIdNumber = Helper.GetNewSessionNo(_currentIdNumber);
				return _currentIdNumber;
			}
		}
	}

	class CallNumberResult
	{
		public enum Results { TlnServerError, ConnectError, Success, Other }

		public Results Result { get; set; }

		public string LastResult { get; set; }

		public CallNumberResult(Results result, string lastResult = "")
		{
			Result = result;
			LastResult = lastResult;
		}
	}
}
