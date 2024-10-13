using ItelexRundsender.Languages;
using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using ItelexCommon.Connection;
using ItelexCommon.Utility;
using static ItelexCommon.Connection.ItelexConnection;
using ItelexRundsender.Data;

namespace ItelexRundsender.Connections
{
	public enum ConnectionStatusTypes { None, Incoming, Outgoing, Report }

	public enum ReportTypes { Intermediate, Final }

	class OutgoingConnectionManager : OutgoingConnectionManagerAbstract
	{
		private const string TAG = nameof(OutgoingConnectionManager);

		private const int MAX_PIN_SEND_RETRIES = 5;

		private RundsenderDatabase _database;

		private System.Timers.Timer _sendTimer;
		private bool _sendPinActive = false;

		public delegate void LoginLogoffEventHandler(string message);
		public event LoginLogoffEventHandler LoginLogoff;

		private int[] SendRetryIntervals = new int[Constants.MAIL_SEND_RETRIES] { 0, 1, 2, 5, 30 };

#if !DEBUG
		private static readonly int[] RETRY_DELAYS1 = new int[] { 0, 2, 7, 17 }; // 0, 2, 5, 10 minutes
		private static readonly int[] RETRY_DELAYS2 = new int[] { 20, 50, 110 }; // 20, 30, 60 minutes
#else
		private static readonly int[] RETRY_DELAYS1 = new int[] { 0, 2 };
		private static readonly int[] RETRY_DELAYS2 = new int[] { 2, 3 };
#endif
		private static readonly int[] RETRY_DELAYS_REPORT = new int[] { 0, 2, 12, 42, 102 }; // 0, 2, 10, 30, 60 minutes

		//public delegate void UpdateEventHandler();
		//public event UpdateEventHandler Update;

		//private bool _shutDown;

		public OutgoingConnectionManager()
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
			if (userItem.ItelexNumber != 211231 && userItem.ItelexNumber != 211230) return;
#endif

			DispatchMsg(null, $"Send pin for {userItem.ItelexNumber} to {confItem.Number} ({confItem.SendRetries + 1}. retry)");

			Language lng = LanguageManager.Instance.GetLanguageOrDefaultByShortname(confItem.Language);
			string message = null;
			switch ((ConfirmationTypes)confItem.Type)
			{
				case ConfirmationTypes.NewPin:
					message = LngText((int)LngKeys.SendRegistrationPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
					break;
				//case ConfirmationTypes.Redirect:
				//	message = LngText((int)LngKeys.SendRedirectionPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
				//	break;
				case ConfirmationTypes.Changed:
					message = LngText((int)LngKeys.SendChangedPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
					break;
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
				ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.Out, confItem.Number, 
						Constants.LOG_PATH, Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT,
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

				outgoing.SendAscii($"\r\n{message}\r\n++++\r\n\n");
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

		public List<CallResult> CallReceiversDirect(SendProperties sendProps)
		{
			var tasks = new List<Task<CallResult>>();
			foreach (Receiver receiver in sendProps.GetReceivers(false))
			{
				Task<CallResult> task = Task.Run(() =>
				{
					DispatchMsg("", $"send direct {receiver.Number}");
					LoginLogoff?.Invoke($"send direct {receiver.Number}");

					int connectionId = GetNextConnectionId();
					ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.Out, receiver.Number, 
							Constants.LOG_PATH, Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT,
							Constants.SYSLOG_APPNAME);
					OutgoingConnection outConn = new OutgoingConnection(connectionId, receiver.Number, "send direct",
							itelexLogger);
					AddConnection(outConn);

					while (outConn.ConnectionState == ConnectionStates.Connected)
					{
						Thread.Sleep(500);
					}

					CallResult result = outConn.ConnectAndReadAnswerback(receiver.Number, sendProps.CallerAnswerbackStr, true);
					if (result.CallStatus != CallStatusEnum.Ok)
					{	// TODO testen
						//RemoveConnection(outConn);
					}

					DispatchUpdateOutgoing();

					string msg;
					if (result.CallStatus == CallStatusEnum.InProgress)
					{
						msg = $"{receiver.Number} kg is {result.Kennung.Name}";
					}
					else
					{
						msg = $"{receiver.Number} {result.CallStatusAsString(sendProps.CallerLanguageStr)}";
					}
					DispatchMsg(outConn.ConnectionName, msg);
					//MessageDispatcher.Instance.Dispatch(msg);
					LoginLogoff?.Invoke(msg);
					return result;
				});
				tasks.Add(task);
			}
			Task.WaitAll(tasks.ToArray());
			List<CallResult> results = tasks.Select(r => r.Result).ToList();
			return results;
		}

		public void SendMessagesDirect(SendProperties sendProps, string message)
		{
			if (string.IsNullOrEmpty(message)) return;

			var tasks = new List<Task>();

			List<OutgoingConnection> connections = sendProps.GetConnections();
			foreach (OutgoingConnection outConn in connections)
			{
				if (outConn == null || !outConn.IsConnected) continue;

				Task task = Task.Run(() =>
				{
					outConn.SendAscii(message);
					/*
					string msg = $"send message to {outConn.Number}";
					MessageDispatcher.Instance.Dispatch(msg);
					LoginLogoff?.Invoke(msg);
					outConn.SendMessage(message, null);
					CallResult result = outConn.ReadAnswerbackAndDisconnect();
					if (result.CallStatus == CallResult.CallStatusEnum.Ok)
					{
						msg = $"kg of {outConn.Number} is {result.Kennung}";
					}
					else
					{
						msg = $"error = {result.RejectReason}";
					}
					MessageDispatcher.Instance.Dispatch(msg);
					LoginLogoff?.Invoke(msg);

					lock (_callsLock)
					{
						_outgoingCalls.Remove(outConn);
					}
					Update?.Invoke();
					*/

					return;
				});
				tasks.Add(task);
			}
			Task.WaitAll(tasks.ToArray());
			//List<CallResult> results = tasks.Select(r => r.Result).ToList();
			//return results;
		}

		public List<CallResult> ReadAnswerbackAndDisconnectDirect(SendProperties sendProps)
		{
			try
			{
				var tasks = new List<Task<CallResult>>();
				List<OutgoingConnection> connections = sendProps.GetConnections();
				foreach (OutgoingConnection outConn in connections)
				{
					if (outConn == null || !outConn.IsConnected) continue;

					Task<CallResult> task = Task.Run(() =>
					{
						string msg;
						CallResult result = outConn.ReadAnswerbackAndDisconnect();
						if (result.CallStatus == CallStatusEnum.Ok)
						{
							msg = $"kg of {outConn.RemoteNumber} is {result.Kennung}";
						}
						else
						{
							msg = $"error = {result.CallStatusAsString(sendProps.CallerLanguageStr)}";
						}
						DispatchMsg(outConn.ConnectionName, msg);

						RemoveConnection(outConn);
						return result;
					});
					tasks.Add(task);
				}
				Task.WaitAll(tasks.ToArray());
				List<CallResult> results = tasks.Select(r => r.Result).ToList();
				return results;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(ReadAnswerbackAndDisconnectDirect), "", ex);
				return null;
			}
		}

		public void TestDelayedAll()
		{
			Task.Run(() =>
			{
				SendProperties sendProps = new SendProperties()
				{
					SendMode = RundsendeModus.Deferred,
					CallerAnswerbackStr = "211231 dege d",
					CallerLanguageId = LanguageIds.de,
					CallerNumber = 211231,
					IncludeReceiverList = false,
					MessageText = "nur ein kurzer test",
					NumberCheck = false,
				};

				List<Receiver> receivers = new List<Receiver>()
				{
					new Receiver(211231, false),
					new Receiver(8227481, false),
					new Receiver(218308, false),
					new Receiver(380172, false)
				};

				sendProps.Receivers = receivers;

				SendDelayedAll(sendProps, false);
			});
		}

		public void SendDelayedAll(SendProperties sendProps, bool directDelayed)
		{
			SendDelayedIntermediateOrFinal(sendProps, directDelayed, RETRY_DELAYS1);

			List<Receiver> occList = (from x in sendProps.Receivers
									  where x.IsReject("occ")
									  select x).ToList();
			if (occList.Count > 0)
			{
				Thread.Sleep(10000);
				SendReport(sendProps, ReportTypes.Intermediate);
				SendDelayedIntermediateOrFinal(sendProps, true, RETRY_DELAYS2);
			}

			Thread.Sleep(10000);

			SendReport(sendProps, ReportTypes.Final);
		}

		public void SendDelayedIntermediateOrFinal(SendProperties sendProps, bool occOnly, int[] retryDelays)
		{
			List<Task> tasks = new List<Task>();
			foreach (Receiver receiver in sendProps.GetReceivers(occOnly))
			{
				Task task = SendDelayed(sendProps, receiver, retryDelays);
				tasks.Add(task);
			}
			Task.WaitAll(tasks.ToArray());
			RemoveOutgoingCallsByNumber(sendProps.CallerNumber); // <-- diese methode tut nichts!!!
		}

		public Task SendDelayed(SendProperties sendProps, Receiver receiver, int[] retryDelays)
		{
			return Task.Run(() =>
			{
				OutgoingConnection outConn = null;
				TickTimer timer = new TickTimer();
				int retries = 0;
				while (true)
				{
					try
					{
						while (!timer.IsElapsedMinutes(retryDelays[retries]))
						{
							Thread.Sleep(1000);
						}
						if (retries >= retryDelays.Length - 1) break;
						retries++;

						_logger.Notice(TAG, nameof(SendDelayed),
							$"{sendProps.CallerNumber}->{receiver.Number} retry={retries} {timer.ElapsedSeconds}s");

						//DispatchMsg("", $"send to {receiver.Number}");
						//LoginLogoff?.Invoke($"send to {receiver.Number}");

						int connectionId = GetNextConnectionId();
						ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.Out, receiver.Number, 
							Constants.LOG_PATH, Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT,
							Constants.SYSLOG_APPNAME);
						outConn = new OutgoingConnection(connectionId, receiver.Number, "send delayed", itelexLogger);
						AddConnection(outConn);
						//outConn.Retries = retries;
						//UpdateOutgoingCall(outConn);
						//DispatchUpdateOutgoing();

						CallResult result = outConn.ConnectAndReadAnswerback(
								receiver.Number, sendProps.CallerAnswerbackStr, false);
						receiver.Kennung1 = result.Kennung?.Name;
						receiver.CallStatus = result.CallStatus;
						receiver.RejectReason = result.RejectReason;

						// testweise immer eine ConnectTime setzen
						receiver.ConnectTime = DateTime.Now;

						//outConn.CallStatus = result.CallStatus;
						//outConn.RejectReason = result.RejectReason;
						//outConn.Retries = retries;
						//UpdateOutgoingCall(outConn);
						DispatchUpdateOutgoing();

						string msg = $"status={result.CallStatusAsString(sendProps.CallerLanguageStr)} ({retries})";
						DispatchMsg(outConn.ConnectionName, msg);

						if (result.CallStatus == CallStatusEnum.InProgress)
						{
							string recvList = sendProps.IncludeReceiverList ? sendProps.GetReceiverLines(68, "empf: ") : "";
							outConn.SendMessage(sendProps.MessageText, recvList);
							result = outConn.ReadAnswerbackAndDisconnect();
							receiver.Kennung2 = result.Kennung?.Name;
							receiver.CallStatus = result.CallStatus;
							receiver.RejectReason = null;
							receiver.ConnectTime = DateTime.Now;
							RemoveConnection(outConn);

							// stop logging
							//ItelexLogger.Instance.End(outConn.ConnectionId);
						}
						else if (result.IsReject("occ"))
						{
							RemoveConnection(outConn);
							continue;
						}

						RemoveConnection(outConn);

						// finished
						break;
					}
					catch (Exception ex)
					{
						_logger.Error(TAG, nameof(SendDelayed), "", ex);
					}
				}
			});
		}

		private void SendReport(SendProperties sendProps, ReportTypes reportType)
		{
			_logger.Notice(TAG, nameof(SendReport), $"CallerNumber = {sendProps.CallerNumber}");

			Task.Run(() =>
			{
				string filename = null;
				string reportText = null;
				try
				{
					reportText = sendProps.GetReportText(reportType);
					filename = $"{DateTime.Now:yyMMdd.HHmm}_{sendProps.CallerNumber}_{sendProps.SendMode}_{reportType}.txt";
					File.WriteAllText(@"msgs\" + filename, reportText);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SendReport), $"Error writing {filename}", ex);
				}

				TickTimer timer = new TickTimer();
				int retries = 0;
				try
				{
					while (true)
					{
						while (!timer.IsElapsedMinutes(RETRY_DELAYS_REPORT[retries]))
						{
							Thread.Sleep(1000);
						}
						if (retries >= RETRY_DELAYS_REPORT.Length - 1) break;
						retries++;

						_logger.Notice(TAG, nameof(SendReport),
								$"rundsend->{sendProps.CallerNumber} retry={retries} {timer.ElapsedSeconds}s");
						Receiver receiver = new Receiver(sendProps.CallerNumber, false);

						ReportConnection reportConn = new ReportConnection(GetNextConnectionId(), sendProps.CallerNumber,
								Constants.LOG_PATH, Constants.LOG_LEVEL);
						reportConn.GotAnswerback += ReportConn_GotAnswerback;
						AddConnection(reportConn);

						while (reportConn.ConnectionState == ItelexConnection.ConnectionStates.Connected)
						{
							Thread.Sleep(100);
						}

						//reportConn.Retries = retries;

						string msg = $"send report to {sendProps.CallerNumber}";
						DispatchMsg(reportConn.ConnectionName, msg);
						LoginLogoff?.Invoke(msg);

						CallResult result = reportConn.Start(sendProps.CallerNumber, reportText);
						receiver.CallStatus = result.CallStatus;
						receiver.RejectReason = result.RejectReason;
						receiver.Kennung1 = result.Kennung?.Name;

						reportConn.GotAnswerback -= ReportConn_GotAnswerback;
						RemoveConnection(reportConn);

						msg = $"status={result.CallStatusAsString(sendProps.CallerLanguageStr)} ({retries})";
						DispatchMsg(reportConn.ConnectionName, msg);
						LoginLogoff?.Invoke(msg);

						if (result.IsReject("occ")) continue;
						break;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SendReport), "", ex);
				}
			});
		}

		private void ReportConn_GotAnswerback(ItelexOutgoing connection)
		{
			DispatchUpdateOutgoing();
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

		private void DispatchMsg(string connectionName, string msg)
		{
			MessageDispatcher.Instance.Dispatch($"{connectionName}: {msg}");
		}
	}
}
