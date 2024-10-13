using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Utility;
using ItelexNewsServer.Data;
using ItelexNewsServer.Languages;
using ItelexNewsServer.News;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static ItelexCommon.Connection.ItelexConnection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace ItelexNewsServer.Connections
{
	class CallManager: OutgoingConnectionManagerAbstract
	{
		private const string TAG = nameof(CallManager);

		private const bool FORCE = true;

		private const int MAX_TLN_SEND_RETRIES = 5;

#if DEBUG
		private const int TLN_SEND_INTERVAL_MIN = 1;
#else
		private const int TLN_SEND_INTERVAL_MIN = 10;
#endif

		private NewsDatabase _database;

		private object _sendMsgLock = new object();
		private System.Timers.Timer _sendMessageTimer;
		private bool _sendPinActive = false;
		private bool _sendMsgActive = false;

		private TickTimer _sendPinTimer = new TickTimer();
		private TickTimer _sendMsgTimer = new TickTimer();

		public CallManager()
		{
			//_logger = LogManager.Instance.Logger;
			_database = NewsDatabase.Instance;

			_sendMessageTimer = new System.Timers.Timer(1000);
			_sendMessageTimer.Elapsed += SendMsgTimer_Elapsed;
			_sendMessageTimer.Start();

			SendAllMessages();
		}

		private void SendMsgTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (_sendPinTimer.IsElapsedSeconds(60))
			{
				_sendPinTimer.Start();
				SendAllPins();
			}

#if !DEBUG
			if (_sendMsgTimer.IsElapsedSeconds(60 * 7))
#else
			if (_sendMsgTimer.IsElapsedSeconds(60))
#endif
			{
				_sendMsgTimer.Start();
				SendAllMessages();
			}
		}

		private void SendAllPins()
		{
			Task.Run(() =>
			{
				TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(SendAllPins));

				if (_sendPinActive) return;
				_sendPinActive = true;

				lock (NewsManager.Instance.GlobalMessageLock)
				{
					try
					{
						List<ConfirmationItem> confItemsList = _database.ConfirmationsLoadAll(false);
						if (confItemsList == null || confItemsList.Count == 0) return;

						// set older active confirmations to finished
						CleanUpConfirmations(confItemsList);

						LazyLoadUsers(FORCE);

						foreach (ConfirmationItem confItem in confItemsList)
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
						TaskManager.Instance.RemoveTask(Task.CurrentId);
					}
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

#if DEBUG
			if (confItem.Number != 211231) return;
#endif


			DateTime utcNow = DateTime.UtcNow;
			if (utcNow < confItem.CreateTimeUtc.Value.AddSeconds(FIRST_SEC))
			{
				return;
			}

			if (confItem.SentTimeUtc != null && utcNow < confItem.SentTimeUtc.Value.AddMinutes(TLN_SEND_INTERVAL_MIN))
			{
				return;
			}

			if (IsOutgoingConnectionActive(confItem.Number))
			{
				DispatchMsg($"Connection to {confItem.Number} already active. Skip.");
				return;
			}

			UserItem userItem = (from u in _userItems where u.UserId == confItem.UserId select u).FirstOrDefault();
			DispatchMsg($"Send pin for {userItem.ItelexNumber} to {confItem.Number}");

#if DEBUG
			if (userItem.ItelexNumber != 7822222) return;
#endif

			Language lng = LanguageManager.Instance.GetLanguageByShortname(confItem.Language);

			string message = null;
			switch ((ConfirmationTypes)confItem.Type)
			{
				case ConfirmationTypes.NewPin:
					message = LngText((int)LngKeys.SendRegistrationPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
					break;
				case ConfirmationTypes.Redirect:
					message = LngText((int)LngKeys.SendRedirectionPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), confItem.Number.ToString(), confItem.Pin });
					break;
				case ConfirmationTypes.Changed:
					message = LngText((int)LngKeys.SendChangedPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
					break;
				default:
					return;
			}

			/*
			string message = null;
			if (confItem.Type == (int)ConfirmationTypes.NewPin)
			{
				message = LanguageManager.Instance.GetText((int)LngKeys.SendRegistrationPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), userItem.Pin });
			}
			else if (confItem.Type == (int)ConfirmationTypes.Redirect)
			{
				message = LanguageManager.Instance.GetText((int)LngKeys.SendRedirectionPinText, lng.Id, new string[] { userItem.ItelexNumber.ToString(), confItem.Number.ToString(), confItem.Pin });
			}
			else
			{
				return;
			}
			*/

			confItem.SendRetries++;

			CallResult result = SendPinToTln2(confItem, userItem, message);
			if (result.CallStatus == CallStatusEnum.Ok)
			{   // ok
				confItem.Sent = true;
				if (confItem.Type == (int)ConfirmationTypes.NewPin)
				{
					confItem.Finished = true;
				}
				confItem.AnswerBack = result.Kennung.Name;
				DispatchMsg($"Sent pin for {userItem.ItelexNumber} to {confItem.Number}: ok");
			}
			else
			{
				if (confItem.SendRetries >= MAX_TLN_SEND_RETRIES)
				{
					confItem.Finished = true; // too many retries
				}
				DispatchMsg($"Sent pin for {userItem.ItelexNumber} to {confItem.Number}: error");
			}
			confItem.SentTimeUtc = DateTime.UtcNow; // last send retry
			_database.ConfirmationsUpdate(confItem);
			_logger.Notice(TAG, nameof(SendPinToTln), $"update confirmations confItem={confItem}");
		}

		private CallResult SendPinToTln2(ConfirmationItem confItem, UserItem userItem, string message)
		{
			_logger.Notice(TAG, nameof(SendPinToTln2), $"{confItem}");

			ItelexOutgoing outgoing = null;
			try
			{
				int connectionId = GetNextConnectionId();
				ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.Out, confItem.Number, 
					Constants.LOG_PATH, Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
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
					_logger.Notice(TAG, nameof(SendPinToTln2), 
						$"Disconnected by remote, reject-reason={outgoing.RejectReason}");
					return new CallResult(confItem.Number, null, CallStatusEnum.Reject, outgoing.RejectReason, "", null);
				}

				DispatchUpdateOutgoing();

				outgoing.SendAscii($"\r\n{message}\r\n++++\r\n\n");
				outgoing.Logoff(null);

				_logger.Debug(TAG, nameof(SendPinToTln2), $"Disconnect()");
				_logger.Notice(TAG, nameof(SendPinToTln2), $"Sent pin to {confItem.Number}");
				return new CallResult(confItem.Number, null, CallStatusEnum.Ok, "", 
						outgoing.RemoteItelexVersionStr, outgoing.RemoteAnswerback);
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
					outgoing.Dispose();
					RemoveConnection(outgoing);
					DispatchUpdateOutgoing();
				}
			}
		}

		public void SendAllMessages()
		{
			if (_sendMsgActive) return;
			_sendMsgActive = true;

			Task.Run(() =>
			{
				TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(SendAllMessages));
				//lock (NewsManager.Instance.GlobalMessageLock)
				{
					try
					{
						NewsManager.Instance.MessageCleanUp();

						LazyLoadUsers(FORCE);
						if (_userItems == null || _userItems.Count == 0) return;

						LazyLoadChannels(FORCE);
						if (_channels == null || _channels.Count == 0) return;

						LazyLoadSubscriptions(FORCE);
						if (_subscriptions == null || _subscriptions.Count == 0) return;

						LazyLoadNews(FORCE);
						if (_news == null || _news.Count == 0) return;

						List <MsgStatusItem> msgStatusList = _database.MsgStatusLoad(null, null, false, null);
						msgStatusList = (from m in msgStatusList
										 where m.SendStatus == (int)MsgStatis.Pending && m.SendRetries < Constants.MAX_MSG_SEND_RETRIES
										select m).ToList();
						//var x = (from m in msgStatusList where m.NewsId == 217171 select m).FirstOrDefault();

						List<CallItem> sendList = GroupMsgsByUser(msgStatusList);
						if (sendList == null || sendList.Count == 0) return;

						DispatchMsg($"Pending msgs for {sendList.Count} user(s)");
						//List<Task> tasks = new List<Task>();
						foreach (CallItem callItem in sendList)
						{
							//tasks.Add(Task.Run(() =>
							Task.Run(() =>
							{
								TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(SendAllMessages), callItem.User.UserId.ToString());
								try
								{
									SendMessagesToTln(callItem);
								}
								catch (Exception ex)
								{
									_logger.Error(TAG, nameof(SendAllMessages), $"Error callItem={callItem}", ex);
								}
								finally
								{
									TaskManager.Instance.RemoveTask(Task.CurrentId);
								}
							});
						}
					}
					finally
					{
						_sendMsgActive = false;
						TaskManager.Instance.RemoveTask(Task.CurrentId);
					}
				}
			});
		}

		private List<CallItem> GroupMsgsByUser(List<MsgStatusItem> msgStatusList)
		{
			try
			{
				List<CallItem> sendList = new List<CallItem>();

				// select users that are not paused
				List<UserItem> users = _userItems.Where(u => !u.IsPaused).ToList();

				if (msgStatusList.Count > 0)
				{
					IEnumerable<IGrouping<long, MsgStatusItem>> newsGroups = msgStatusList.GroupBy(s => s.UserId);
					foreach (IGrouping<long, MsgStatusItem> newsGroup in newsGroups)
					{
						UserItem userItem = (from u2 in users where u2.UserId == newsGroup.Key select u2).FirstOrDefault();
						if (userItem == null) continue; // user does not exist or is inactivated
						//if (userItem.ItelexNumber == 211231)
						//{
						//	Debug.Write("");
						//}
						CallItem callItem = new CallItem(userItem);
						foreach (MsgStatusItem newsStatus in newsGroup)
						{
							callItem.AddMsg(new MessageNewsItem(newsStatus.NewsId));
							callItem.MaxRetryCount = Math.Max(callItem.MaxRetryCount, newsStatus.SendRetries);
						}
						if (callItem.Messages.Count > 0)
						{
							sendList.Add(callItem);
						}
					}
				}

				_logger.Debug(TAG, nameof(GroupMsgsByUser), $"{sendList.Count} calls found");

				return sendList;
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(GroupMsgsByUser), $"Error", ex);
				return null;
			}
		}

		private void SendMessagesToTln(CallItem callItem)
		{
			UserItem userItem = callItem.User;

			if (!userItem.IsHourActive()) return;

			_logger.Notice(TAG, nameof(SendMessagesToTln),
				$"send {callItem.Messages.Count} msgs to {userItem.ItelexNumber} retries={callItem.MaxRetryCount}");
			if (userItem.RedirectNumber != null)
			{
				_logger.Notice(TAG, nameof(SendMessagesToTln), $"redirected to {userItem.RedirectNumber}");
			}

			if (IsOutgoingConnectionActive(userItem.ItelexNumber))
			{
				_logger.Notice(TAG, nameof(SendMessagesToTln),
					$"connection to {userItem.ItelexNumber} already active. skip.");
				DispatchMsg($"Connection to {userItem.ItelexNumber} already active. Skip.");
				return;
			}

			int msgCnt = callItem.Messages.Count;
			if (msgCnt == 0) return;

			string numStr = userItem.RedirectNumber == null ?
					$"{userItem.ItelexNumber}" :
					$"{userItem.ItelexNumber} -> {userItem.RedirectNumber}";
			DispatchMsg($"Send {msgCnt} msgs to {numStr}");

#if DEBUG
			if (numStr != "211231") return;
#endif

			CallResult result = CallTln(userItem, callItem);

			if (result.CallStatus == CallStatusEnum.Ok)
			{
				DispatchMsg($"Sent {msgCnt} msgs to {numStr}: ok");
				_logger.Notice(TAG, nameof(SendMessagesToTln),
					$"sent {msgCnt} msgs to {userItem.ItelexNumber} ok");
			}
			else
			{
				DispatchMsg($"Sent {msgCnt} msgs to {numStr}: {result.RejectReason}");
				_logger.Notice(TAG, nameof(SendMessagesToTln),
					$"sent {msgCnt} msgs to {userItem.ItelexNumber} {result.RejectReason}");
			}
		}

		private CallResult CallTln(UserItem userItem, CallItem callItem)
		{
			int number = userItem.RedirectNumber == null ? userItem.ItelexNumber : userItem.RedirectNumber.Value;
			//_logger.Notice(TAG, nameof(CallTln), $"{number} {callItem}");

			ItelexOutgoing outgoing = null;
			try
			{
				IncrementMsgStatusRetries(userItem.UserId);

				int connectionId = GetNextConnectionId();
				ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.Out, number, 
					Constants.LOG_PATH, Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
				outgoing = new ItelexOutgoing(connectionId, number, "send msgs", itelexLogger);
				ItelexOutgoingConfiguration config = new ItelexOutgoingConfiguration()
				{
					ItelexNumber = number,
					OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
					OurAnswerbackStr = Constants.ANSWERBACK_DE,
					RetryCnt = null,
				};

				if (userItem.MsgFormat == (int)MsgFormats.Standard)
				{
					config.OutgoingType = new LoginSeqTypes[] { LoginSeqTypes.SendKg };
				}
				else
				{
					config.OutgoingType = new LoginSeqTypes[] { };
				}
				AddConnection(outgoing);
				CallResult result = outgoing.StartOutgoing(config);

				if (!outgoing.IsConnected || outgoing.RejectReason != null)
				{
					_logger.Notice(TAG, nameof(CallTln), $"Disconnected by remote, reject-reason={outgoing.RejectReason}");
					return new CallResult(number, null, CallStatusEnum.Reject, outgoing.RejectReason, "", null);
				}

				_logger.Debug(TAG, nameof(CallTln), $"StartOutgoing ok");
				//MessageDispatcher.Instance.Dispatch($"{config.ItelexNumber} StartOutgoing ok");

				DispatchUpdateOutgoing();

				outgoing.SendAscii($"\r\n");

				int msgCnt = 0;
				foreach (MessageNewsItem msgItem in callItem.Messages)
				{
					msgCnt++;

					string message = GetMsgText(userItem, msgItem, msgCnt, callItem.Messages.Count);
					if (string.IsNullOrEmpty(message))
					{
						_logger.Warn(TAG, nameof(CallTln), $"message is empty, newsId = {msgItem.NewsId}");
						continue;
					}

					if (msgCnt > 1)
					{
						// message delimiter
						if (userItem.MsgFormat == (int)MsgFormats.Standard)
						{
							outgoing.SendAscii("===\r\n\n");
						}
						else
						{
							outgoing.SendAscii("===\r\n");
						}
					}
					outgoing.SendAscii(message);
					outgoing.WaitAllSendBuffersEmpty();

					// set msgstatus to send
					MsgStatusItem msgStatus =
						_database.MsgStatusLoad(callItem.User.UserId, msgItem.NewsId, false, null).FirstOrDefault();
					if (msgStatus == null) continue;
					//msgStatus.SendRetries++;
					msgStatus.SendTimeUtc = DateTime.UtcNow;
					msgStatus.SendStatusEnum = MsgStatis.Ok;
					_database.MsgStatusUpdate(msgStatus);
				}
				// end of msgs
				outgoing.SendAscii($"+++\r\n");
				if (userItem.MsgFormat == (int)MsgFormats.Standard)
				{
					outgoing.SendAscii("\r\n");
				}

				outgoing.Logoff(null);

				_logger.Debug(TAG, nameof(CallTln), $"Disconnect()");
				//_logger.Notice(TAG, nameof(CallTln), $"Msgs send to {number}");
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

		private void IncrementMsgStatusRetries(long userId)
		{
			List<MsgStatusItem> msgStatusList =
					_database.MsgStatusLoad(userId, null, true, Constants.MAX_MSG_SEND_RETRIES);
			//_logger.Notice(TAG, nameof(SendMessagesToTln), $"send to {userId} incrCnt={msgStatusList.Count}");
			foreach (MsgStatusItem item in msgStatusList)
			{
				item.SendRetries++;
				_database.MsgStatusUpdate(item);
			}
		}

		private string GetMsgText(UserItem userItem, MessageNewsItem msgItem, int msgNum, int msgCnt)
		{
			StringBuilder sb = new StringBuilder();
			NewsItem newsItem = (from n in _news where n.NewsId == msgItem.NewsId select n).FirstOrDefault();
			if (newsItem == null)
			{
				_logger.Error(TAG, nameof(GetMsgText), $"newsItem is null, newsId={msgItem.NewsId}");
				return null;
			}
			ChannelItem channelItem = (from c in _channels where c.ChannelId == newsItem.ChannelId select c).FirstOrDefault();
			if (channelItem == null)
			{
				_logger.Error(TAG, nameof(GetMsgText), $"channelItem is null, channelId={newsItem.ChannelId}");
				return null;
			}
			bool isLocalChannel = channelItem.ChannelType == ChannelTypes.Local;

			// detect equal title and message -> clear message
			int levDist = 0;
			try
			{
				levDist = Levenshtein.Calculate(newsItem.Title, newsItem.Message);
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(GetMsgText), "Levenshtein error", ex);
				levDist = -1;
			}
			_logger.Debug(TAG, nameof(GetMsgText), $"newsId={newsItem.NewsId} levDist={levDist}");
			_logger.Debug(TAG, nameof(GetMsgText), $"title=\"{newsItem.Title}\"");
			_logger.Debug(TAG, nameof(GetMsgText), $"msg  =\"{newsItem.Message}\"");
			if (levDist != -1 && levDist < 5)
			{
				newsItem.Message = "";
			}

			sb.Append(CodeManager.ASC_FIGS);
			DateTime dt = Helper.ToLocalTime(newsItem.NewsTimeUtc, userItem.Timezone);
			//string timeStr = userItem.Language == "de" ? $"{dt:dd.MM.yyyy HH:mm}" : $"{dt:yyyy-MM-dd HH:mm}";
			string timeStr = $"{dt:yyyy-MM-dd HH:mm}";
			string msgCntStr = $"{msgNum}/{msgCnt}";
			string chIdStr = $"={channelItem.ChannelId}/{newsItem.NewsId}";
			sb.Append(($"+{timeStr}  {msgCntStr}  {chIdStr}  {channelItem.GetConvName()}\r\n"));

			string message = newsItem.Message;

			if (userItem.MsgFormat == (int)MsgFormats.Standard)
			{
				// long format
				if (isLocalChannel && !string.IsNullOrEmpty(newsItem.Author))
				{
					sb.Append($"von/from: {newsItem.Author}\r\n");
				}
				if (!string.IsNullOrWhiteSpace(newsItem.Title))
				{
					string title = ConvMsgText(newsItem.Title, isLocalChannel);
					title = title.TrimEnd(new char[] { '.', '-' });
					title = ReformatMsg(title);
					sb.Append(title);
					sb.Append("\r\n---\r\n");
				}

				if (!string.IsNullOrWhiteSpace(message))
				{
					string msg = ConvMsgText(message, isLocalChannel);
					if (!isLocalChannel) msg = ReformatMsg(msg);
					sb.Append(msg);
					sb.Append("\r\n");
				}
			}
			else
			{
				// short format
				string msg = "";
				if (isLocalChannel && !string.IsNullOrEmpty(newsItem.Author))
				{
					msg += $"von/from {newsItem.Author}:\r\n";
				}
				if (!string.IsNullOrWhiteSpace(newsItem.Title))
				{
					string title = newsItem.Title;
					title = title.Replace(":", " - ");
					msg += ConvMsgText(title, isLocalChannel) + ": ";
				}
				if (!string.IsNullOrWhiteSpace(message))
				{
					msg += ConvMsgText(message, isLocalChannel);
				}
				if (!isLocalChannel) msg = ReformatMsg(msg);
				sb.Append(msg);
				sb.Append("\r\n");
			}

			return sb.ToString();
		}

		private string ReformatMsg(string msg)
		{
			msg = msg.Replace("\r", " ");
			msg = msg.Replace("\n", " ");
			msg = msg.Replace("  ", " ");

			string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			StringBuilder sb = new StringBuilder();
			string line = "";
			foreach (string word in words)
			{
				if ((line + " " + word).Length >= 68)
				{
					sb.Append(line.Trim() + "\r\n");
					line = "";
				}
				line += word + " ";
			}
			if (!string.IsNullOrWhiteSpace(line))
			{
				sb.Append(line.Trim());
			}
			string newMsg = sb.ToString();
			if (newMsg.EndsWith("\r\n")) newMsg = newMsg.Substring(0, newMsg.Length - 2);
			return newMsg;
		}

		private string ConvMsgText(string msg, bool localChannel)
		{
			msg = msg.ToLower();

			int idx = msg.IndexOf("https");
			if (idx != -1)
			{
				msg = msg.Substring(0, idx).Trim();
			}
			
			if (!localChannel) msg = ReplaceAsciiCode(msg);
			if (!localChannel) msg = msg.Replace("\r", " ");
			if (!localChannel) msg = msg.Replace("\n", " ");
			msg = msg.Replace("#", "//");
			msg = msg.Replace("&", "+");
			msg = msg.Replace("%", "o/o");
			msg = msg.Replace("\"", "'");
			if (!localChannel) msg = msg.Replace("''", "'");
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
			if (!localChannel) msg = msg.Replace("  ", " ");
			msg = msg.Trim(new char[] { ' ', '\r', '\n' });
			return CodeManager.AsciiStringReplacements(msg, CodeSets.ITA2, false, false);
		}

		public static string ReplaceAsciiCode(string msg)
		{
			msg = msg.Replace("&quot;", "'");
			msg = msg.Replace("&amp;", "+");
			msg = msg.Replace("&lt;", "");
			msg = msg.Replace("&gt;", "");
			msg = msg.Replace("&quot;", "'");
			msg = msg.Replace("&tilde;", "'");
			msg = msg.Replace("&euro;", "euro");
			msg = msg.Replace("&raquo;", "'");
			msg = msg.Replace("&laquo;", "'");

			while (true)
			{
				int p1 = msg.IndexOf("&#");
				if (p1 == -1) break;
				int p2 = msg.IndexOf(";", p1);
				if (p2 == -1 || p2 - p1 > 4) break;
				string valStr = msg.Substring(p1 + 2, p2 - p1 - 2);
				if (int.TryParse(valStr, out int val))
				{
					if (val >= 0 && val <= 255)
					{
						string code = ((char)val).ToString();
						msg = msg.Substring(0, p1) + code + msg.Substring(p2 + 1, msg.Length - p2 - 1);
					}
				}
			}
			return msg;
		}

		List<ChannelItem> _channels = null;

		private void LazyLoadChannels(bool force = false)
		{
			if (force || _channels == null)
			{
				_channels = _database.ChannelLoadAll();
			}
		}

		List<NewsItem> _news = null;

		private void LazyLoadNews(bool force = false)
		{
			if (force || _news == null)
			{
				_news = _database.NewsLoadAll();
			}
		}

		private List<SubscriptionItem> _subscriptions = null;

		private void LazyLoadSubscriptions(bool force = false)
		{
			if (force || _subscriptions == null)
			{
				_subscriptions = _database.SubscriptionsLoad(null, null);
			}
		}

		private List<UserItem> _userItems = null;

		private void LazyLoadUsers(bool force = false)
		{
			if (force || _userItems == null)
			{
				_userItems = _database.UserLoadAllActive();
			}
		}

		/*
		private List<MsgStatusItem> _msgStatusList = null;
		private void LazyLoadMsgStatusList(bool force = false)
		{
			if (force || _msgStatusList == null)
			{
				_msgStatusList = _database.MsgStatusLoad(null, null);
			}
		}
		*/

		private void DispatchMsg(string msg)
		{
			MessageDispatcher.Instance.Dispatch(msg);
			_logger.Debug(TAG, nameof(DispatchMsg), msg);
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
}
