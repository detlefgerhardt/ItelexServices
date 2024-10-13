using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Utility;
using ItelexMsgServer.Data;
using ItelexMsgServer.Languages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ItelexCommon.Connection.ItelexConnection;

namespace ItelexMsgServer.Connections
{
	class OutgoingManager: OutgoingConnectionManagerAbstract
	{
		private const string TAG = nameof(OutgoingManager);

		private const bool FORCE = true;

		private const int MAX_PIN_SEND_RETRIES = 5;

#if DEBUG
		private const int TLN_MSG_INTERVAL_MIN = 1;
#else
		private const int TLN_MSG_INTERVAL_MIN = 1;
#endif

		private MsgServerDatabase _database;

		private System.Timers.Timer _sendTimer;
		private bool _sendPinActive = false;
		private bool _sendMsgActive = false;

		private int[] SendRetryIntervals = new int[Constants.ITELEX_SEND_RETRIES] { 0, 2, 5, 10, 30 };

		public OutgoingManager()
		{
			_database = MsgServerDatabase.Instance;

			_sendTimer = new System.Timers.Timer(1000 * 60);
			_sendTimer.Elapsed += SendTimer_Elapsed;
			_sendTimer.Start();
		}

		private void SendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			SendAllPins();

			//Debug.WriteLine(_sendMsgTimer.ElapsedMilliseconds);
			//if (_sendMsgTimer.IsElapsedMinutes(TLN_MSG_INTERVAL_MIN))
			{
				//NewMessage = false;
				//_sendMsgTimer.Start();
				SendAllMessages();
			}
		}

		private void SendAllPins()
		{
			if (_sendPinActive) return;
			_sendPinActive = true;
			try
			{
				List<ConfirmationItem> confItems = _database.ConfirmationsLoadAll(false, Constants.ITELEX_SEND_RETRIES);
				if (confItems == null || confItems.Count == 0) return;

				// set older active confirmations to finished
				CleanUpConfirmations(confItems);

				foreach (ConfirmationItem confItem in confItems)
				{
					if (!confItem.Finished && !confItem.Sent)
					{
						Task.Run(() =>
						{
							TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(SendAllPins));
							try
							{
								SendPinToTln(confItem);
							}
							finally
							{
								TaskManager.Instance.RemoveTask(Task.CurrentId);
							}
						});
					}
				}
			}
			finally
			{
				_sendPinActive = false;
			}
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
			switch ((ConfirmationTypes)confItem.Type)
			{
				case ConfirmationTypes.NewPin:
					message = LngText((int)LngKeys.SendRegistrationPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
					break;
				case ConfirmationTypes.Redirect:
					message = LngText((int)LngKeys.SendRedirectionPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
					break;
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
				ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.Out,
						confItem.Number, Constants.LOG_PATH, Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT,
						Constants.SYSLOG_APPNAME);
				outgoing = new ItelexOutgoing(connectionId, confItem.Number, "send pin", itelexLogger);
				AddConnection(outgoing);
				ItelexOutgoingConfiguration config = new ItelexOutgoingConfiguration()
				{
					OutgoingType = new LoginSeqTypes[] { LoginSeqTypes.SendKg, LoginSeqTypes.GetKg },
					ItelexNumber = confItem.Number,
					OurItelexVersionStr = Helper.GetVersionCode(),
					OurAnswerbackStr = Constants.ANSWERBACK_DE,
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

		public void SendAllMessages()
		{
			Task.Run(() =>
			{
				TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(SendAllMessages));
				try
				{
					if (_sendMsgActive) return;
					_sendMsgActive = true;
					try
					{
						//lock (MailManager.Instance.GlobalMessageLock)
						{
							List<MsgItem> msgs = _database.MsgsLoadAllPending();
							if (msgs == null || msgs.Count == 0) return;

							//CleanUpMsgStats();

							List<CallGroup> callGroups = GetCallGroups(msgs);

							List<Task> tasks = new List<Task>();
							foreach (CallGroup callGroup in callGroups)
							{
								tasks.Add(Task.Run(() =>
								{
									TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(SendAllMessages), callGroup.UserId.ToString());
									try
									{
										SendMessagesToTln(callGroup);
									}
									catch (Exception ex)
									{
										_logger.Error(TAG, nameof(SendAllMessages), $"Error callGroup={callGroup}", ex);
									}
									finally
									{
										TaskManager.Instance.RemoveTask(Task.CurrentId);
									}
								}));
							}
							Task.WaitAll(tasks.ToArray());
						}
					}
					finally
					{
						_sendMsgActive = false;
					}
				}
				finally
				{
					TaskManager.Instance.RemoveTask(Task.CurrentId);
				}
			});
		}

		private List<CallGroup> GetCallGroups(List<MsgItem> msgs)
		{
			List<CallGroup> callGroups = new List<CallGroup>();
			foreach(MsgItem msgItem in msgs)
			{
				CallGroup group = callGroups.Where(g => g.UserId == msgItem.UserId).FirstOrDefault();
				if (group == null)
				{
					group = new CallGroup(msgItem);
					callGroups.Add(group);
				}
				else
				{
					group.Add(msgItem);
				}
			}
			return callGroups;
		}

		private void SendMessagesToTln(CallGroup callGroup)
		{
			UserItem userItem = _database.UserLoadById(callGroup.UserId);
			if (userItem == null) return;
			if (!userItem.IsHourActive()) return;

			// check LastTryTimeUtc of callGroup
			DateTime utcNow = DateTime.UtcNow;
			//_logger.Debug(TAG, nameof(SendMessagesToTln), $"check LastTryTimeUtc");
			foreach (MsgItem msgItem in callGroup.Msgs)
			{
				/*
				DateTime? dt = msgItem.LastTryTimeUtc.HasValue ?
						msgItem.LastTryTimeUtc.Value.AddMinutes(SendRetryIntervals[msgItem.SendRetries]) :
						(DateTime?)null;
				_logger.Debug(TAG, nameof(SendMessagesToTln),
					$"{msgItem.UserId} [{utcNow}] [{msgItem.LastTryTimeUtc}] [{dt}] {msgItem.SendRetries}");
				*/
				if (msgItem.LastTryTimeUtc != null)
				{
					DateTime nextSend = msgItem.LastTryTimeUtc.Value.AddMinutes(SendRetryIntervals[msgItem.SendRetries]);
					//_logger.Debug(TAG, nameof(SendMessagesToTln), $"{msgItem.UserId} nextSend=[{nextSend}]");
					if (utcNow < nextSend)
					{
						// at least for one msg the retry delay is not elapsed
						_logger.Debug(TAG, nameof(SendMessagesToTln),
							$"do not send msg UserId={msgItem.UserId} {utcNow} " +
							$"retries/delay={msgItem.SendRetries}/{SendRetryIntervals[msgItem.SendRetries]}");
						return;
					}
				}
			}

#if DEBUG
			//if (userItem.ItelexNumber != 211231) return;
#endif

			DispatchMsg(null, $"Send {callGroup.Msgs.Count} msgs to {userItem.ItelexNumber} ({callGroup.MaxSendRetries})");

			CallResult result = CallTln(userItem, callGroup);

			if (result.CallStatus == CallStatusEnum.Ok)
			{
				DispatchMsg(null, $"Sent {callGroup.Msgs.Count} msgs to {userItem.ItelexNumber}: ok");
			}
			else if (result.CallStatus == CallStatusEnum.AlreadyActive)
			{
				// skip
				DispatchMsg(null, $"call to {userItem.ItelexNumber} already active");
			}
			else
			{
				DispatchMsg(null,
					$"Send {callGroup.Msgs.Count} msgs to {userItem.ItelexNumber}: {result.RejectReason}");
			}
		}

		private CallResult CallTln(UserItem userItem, CallGroup callGroup)
		{
			int number = userItem.ItelexNumber;
			_logger.Info(TAG, nameof(CallTln), $"{number} {callGroup}");

			ItelexOutgoing outgoing = null;
			try
			{
				int connectionId = GetNextConnectionId();
				ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.Out, number,
						Constants.LOG_PATH, Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT,
						Constants.SYSLOG_APPNAME);
				outgoing = new ItelexOutgoing(connectionId, number, "send msgs", itelexLogger);
				ItelexOutgoingConfiguration config = new ItelexOutgoingConfiguration()
				{
					ItelexNumber = number,
					OutgoingType = new LoginSeqTypes[] { LoginSeqTypes.SendKg, LoginSeqTypes.GetKg },
					OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
					OurAnswerbackStr = Constants.ANSWERBACK_DE,
					RetryCnt = null,
					IsConnectionActive = IsOutgoingConnectionActive,
				};

				AddConnection(outgoing);
				CallResult result = outgoing.StartOutgoing(config);

				DateTime utcNow = DateTime.UtcNow;

				if (result.CallStatus != CallStatusEnum.Ok || !outgoing.IsConnected)
				{
					_logger.Notice(TAG, nameof(CallTln), $"Disconnected by remote, reject-reason={outgoing.RejectReason}");

					lock (_database.MailGateLocker)
					{
						foreach (MsgItem msgItem in callGroup.Msgs)
						{
							msgItem.SendRetries++;
							msgItem.LastTryTimeUtc = utcNow;
							if (msgItem.SendRetries >= Constants.ITELEX_SEND_RETRIES)
							{
								msgItem.SendStatus = (int)MsgStatis.NotSent;
							}
							_database.MsgsUpdate(msgItem);
						}
					}
					return result;
				}

				DispatchUpdateOutgoing();

				Language lng = LanguageManager.Instance.GetLanguageOrDefaultByShortname(userItem.Language);
				int lngId = lng.Id;

				int lineCnt = 0;
				for (int i = 0; i < callGroup.Msgs.Count; i++)
				{
					MsgItem msgItem = callGroup.Msgs[i];
					string msgTime;
					if (msgItem.MailTimeUtc.HasValue)
					{
						msgTime = msgItem.MailTimeUtc.Value.AddHours(userItem.Timezone).ToString("yyyy-MM-dd HH:mm");
					}
					else
					{
						msgTime = "-";
					}

					outgoing.SendAscii($"\r\n{LngText((int)LngKeys.MailTime, lng.Id)}: {msgTime}  {i + 1}/{callGroup.Msgs.Count}");

					if (userItem.ShowSender)
					{
						string from = ConvSender($"{LngText((int)LngKeys.MailFrom, lng.Id)}: {msgItem.Sender}");
						from = FormatMsg(from);
						outgoing.SendAscii($"\r\n{from}");
					}

					string to = ConvSender($"{LngText((int)LngKeys.MailTo, lng.Id)}: i-telex {userItem.ItelexNumber}");
					to = FormatMsg(to);
					outgoing.SendAscii($"\r\n{to}");

					if (!string.IsNullOrWhiteSpace(msgItem.Subject))
					{
						string subj = ConvMsgText($"{LngText((int)LngKeys.MailSubject, lng.Id)}: {msgItem.Subject}");
						subj = FormatMsg(subj);
						outgoing.SendAscii($"\r\n\n{subj}");
					}

					if (!string.IsNullOrEmpty(msgItem.Message))
					{
						string msg = ConvMsgText(msgItem.Message);
						msg = FormatMsg(msg);
						outgoing.SendAscii($"\r\n\n{msg}");
					}
					if (i < callGroup.Msgs.Count - 1)
					{
						outgoing.SendAscii("\r\n===\r\n");
					}
					else
					{
						outgoing.SendAscii("\r\n+++\r\n");
					}

					outgoing.WaitAllSendBuffersEmpty();
					lock (_database.MailGateLocker)
					{
						msgItem.SendStatus = (int)MsgStatis.Ok;
						msgItem.SendRetries++;
						msgItem.SendTimeUtc = utcNow;
						msgItem.LastTryTimeUtc = utcNow;
						_database.MsgsUpdate(msgItem);
					}
					lineCnt += msgItem.LineCount;
				}

				outgoing.SendAscii("\r\n");
				outgoing.Logoff(null);

				RemoveConnection(outgoing);

				lock (_database.MailGateLocker)
				{
					userItem = _database.UserLoadById(userItem.UserId);
					userItem.TotalMailsReceived++;
					userItem.MailsPerDay += callGroup.Msgs.Count;
					userItem.LinesPerDay += lineCnt;
					userItem.LastReceiveTimeUtc = DateTime.UtcNow;
					_database.UserUpdate(userItem);
				}

				_logger.Debug(TAG, nameof(CallTln), $"Disconnect()");
				_logger.Notice(TAG, nameof(CallTln), $"Msgs send to {number}");
				return new CallResult(number, null, CallStatusEnum.Ok, "", outgoing.RemoteItelexVersionStr, null /*new Answerback(kennung)*/);
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(CallTln), $"Error sending msgs to {number}", ex);
				return new CallResult(number, null, CallStatusEnum.Error, "", "", null);
			}
			finally
			{
				if (outgoing != null)
				{
					outgoing.Dispose();
					RemoveConnection(outgoing);
				}
			}
		}

		public ItelexOutgoing OpenTelegramConnection(int itelexNumber, string ourAnswerback, out CallResult callResult)
		{
			int number = itelexNumber;
			_logger.Notice(TAG, nameof(OpenTelegramConnection), $"{number}");

			ItelexOutgoing outgoing = null;
			try
			{
				int connectionId = GetNextConnectionId();
				ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.Out, number, Constants.LOG_PATH,
						Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
				outgoing = new ItelexOutgoing(connectionId, number, "telegram conn", itelexLogger);
				ItelexOutgoingConfiguration config = new ItelexOutgoingConfiguration()
				{
					ItelexNumber = number,
					OutgoingType = new LoginSeqTypes[] { LoginSeqTypes.SendKg, LoginSeqTypes.GetKg },
					OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
					OurAnswerbackStr = ourAnswerback,
					RetryCnt = null,
					IsConnectionActive = IsOutgoingConnectionActive,
				};
				AddConnection(outgoing);
				callResult = outgoing.StartOutgoing(config);
				if (callResult.CallStatus != CallStatusEnum.Ok)
				{
					RemoveConnection(outgoing);
				}
				return outgoing;
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(OpenTelegramConnection), "error", ex);
				callResult = null;
				return null;
			}
		}

		public void CloseTelegramConnection(int itelexNumber)
		{
			ItelexOutgoing outConn = GetOutgoingConnectionByNumber(itelexNumber);
			if (outConn == null) return;

			outConn.SendAscii("\r\n\n");
			outConn.Logoff(null);

			RemoveConnection(outConn);
		}

		private string FormatMsg(string msg)
		{
			if (msg == null) return "";

			msg = msg.Replace("\r", "");
			StringBuilder sb = new StringBuilder();
			string[] lines = msg.Split(new char[] { '\n' }, StringSplitOptions.None);
			foreach (string line in lines)
			{
				string[] words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				string newLine = "";
				foreach (string word in words)
				{
					if ((newLine + " " + word).Length >= 68)
					{
						sb.Append(newLine.Trim() + "\r\n");
						newLine = "";
					}
					newLine += word + " ";
				}
				if (!string.IsNullOrWhiteSpace(newLine))
				{
					sb.Append(newLine.Trim());
				}
				sb.Append("\r\n");
			}
			string newMsg = sb.ToString();
			if (newMsg.EndsWith("\r\n")) newMsg = newMsg.Substring(0, newMsg.Length - 2);
			return newMsg;
		}

		public static string ConvSender(string msg)
		{
			if (msg == null) return null;
			msg = msg.Replace("@", "(at)");
			msg = msg.Replace("<", "(");
			msg = msg.Replace(">", ")");
			return msg;
		}

		private string ConvMsgText(string msg)
		{
			if (string.IsNullOrWhiteSpace(msg)) return "";
			msg = msg.ToLower();

			//msg = msg.Replace("\r", " ");
			//msg = msg.Replace("\n", " ");
			msg = msg.Replace("@", "(at)");
			msg = msg.Replace("#", "+");
			msg = msg.Replace("&", "+");
			msg = msg.Replace("%", "o/o");
			msg = msg.Replace("\"", "'");
			msg = msg.Replace("''", "'");
			msg = msg.Replace("„", "'");
			msg = msg.Replace("“", "'");
			msg = msg.Replace("`", "'");
			msg = msg.Replace("´", "'");
			msg = msg.Replace("\u2013", "-");
			msg = msg.Replace("~", "-");
			msg = msg.Replace("!", ".");
			msg = msg.Replace("*", "x");
			msg = msg.Replace("•", "-");

			msg = msg.Replace("€", "euro");
			msg = msg.Replace("  ", " ");
			msg = msg.Trim(new char[] { ' ','\r', '\n', '+' });
			msg = msg.TrimEnd(new char[] { ' ', '\r', '\n', '+' });

			return CodeManager.AsciiStringReplacements(msg, CodeSets.ITA2, false, false);

		}

		/*
		private List<UserItem> _users = null;

		private void LazyLoadUsers(bool force = false)
		{
			if (force || _users == null)
			{
				_users = _database.UserLoadAll();
			}
		}

		private List<MsgItem> _msgs = null;

		private void LazyLoadMsgs(bool force = false)
		{
			if (force || _msgs == null)
			{
				_msgs = _database.MsgsLoadAllPending(Constants.MAIL_SEND_RETRIES);
			}
		}
		*/

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

		/*
		public string LngText(LngKeys lngKey, int lngId)
		{
			return LanguageManager.Instance.GetText((int)lngKey, lngId);
		}

		public string LngText(LngKeys lngKey, int lngId, string[] param)
		{
			return LanguageManager.Instance.GetText((int)lngKey, lngId, param);
		}
		*/
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

	class CallGroup
	{
		public Int64 UserId
		{
			get
			{
				if (Msgs.Count == 0) return 0;
				return Msgs[0].UserId;
			}
		}

		public int MaxSendRetries
		{
			get
			{
				if (Msgs == null || Msgs.Count == 0) return 0;
				return Msgs.Max(x => x.SendRetries) + 1;
			}
		}

		public List<MsgItem> Msgs { get; set; }

		public CallGroup(MsgItem msgItem)
		{
			Msgs = new List<MsgItem>();
			Msgs.Add(msgItem);
		}

		public void Add(MsgItem msgItem)
		{
			Msgs.Add(msgItem);
		}

		public override string ToString()
		{
			return $"{UserId} {Msgs.Count} msgs";
		}
	}
}
