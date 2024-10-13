using ItelexChatServer.Actions;
using ItelexChatServer.Languages;
using ItelexChatServer.Notification;
using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Threading;

namespace ItelexChatServer
{
	class IncomingChatConnectionManager: IncomingConnectionManagerAbstract
	{
		private const string TAG = nameof(IncomingChatConnectionManager);

		private string _chatEnd;

		private string _messageBuffer;

		private string _chatLine;

		private bool _shutDown;

		private System.Timers.Timer _writeTimer;

		private TickTimer _lastWriteNotification;

		private NotificationConnectionManager _notificationManager => (NotificationConnectionManager)GlobalData.Instance.OutgoingConnectionManager;

		public IncomingChatConnectionManager()
		{
			_messageBuffer = "";
			_lastWriteNotification = new TickTimer();

			_writeTimer = new System.Timers.Timer(ItelexConstants.WRITE_TIMEOUT_SEC * 1000); // 10 seconds
			_writeTimer.Elapsed += WriteTimer_Elapsed;

			//NotificationManager.Instance.LoadNotificationList();

			//DateTime dt = DateTime.Now;
			//SendNotifications($"{dt:HH:mm}this is an test notification");
		}

		protected override ItelexIncoming CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			IncomingChatConnection conn = new IncomingChatConnection(client, connectionId, logPath, logLevel);
			conn.ChatLogin += ChatConn_ChatLogin;
			return conn;
		}

		/// <summary>
		/// Chat needs an own ChatLogin-Event after choosing a nickname
		/// </summary>
		/// <param name="chatConn"></param>
		private void ChatConn_ChatLogin(IncomingChatConnection chatConn)
		{
			IncomingChatConnection activeConn = GetWritingConnection();
			if (activeConn != null)
			{   // send name prompt, if other station is writing
				chatConn.SendUserPrompt(activeConn.ConnectionShortName);
				chatConn.SendAscii("\r\n");
			}

			ActivityHistoryManager.Instance.AddActivity(ActivityHistoryManager.Activities.Login, chatConn.ConnectionShortName);
			InvokeUpdateIncoming();
			string msg = $"new member {chatConn.ConnectionName} [{chatConn.RemoteAnswerback}]";
			MessageDispatcher.Instance.Dispatch(chatConn.ConnectionId, msg);
			SendMessage($"{chatConn.LngText(LngKeys.NewEntrant)} {chatConn.ConnectionShortName}", chatConn);

			DateTime dt = DateTime.Now;
			SendNotifications(chatConn, NotificationTypes.Login);
		}

		protected override void Conn_Received(ItelexConnection conn, string asciiStr)
		{
			IncomingChatConnection chatConn = (IncomingChatConnection)conn;
			try
			{
				switch (chatConn.ChatState)
				{
					case IncomingChatConnection.ChatStates.NotLoggedIn:
						break;
					case IncomingChatConnection.ChatStates.Idle:
						if (_shutDown) return;

						bool sendPrompt = false;
						for (int i = 0; i < asciiStr.Length; i++)
						{
							switch (asciiStr[i])
							{
								case CodeManager.ASC_BEL:
									_logger.Debug(TAG, nameof(Conn_Received), $"[{chatConn.ConnectionName}] Recv BEL");
									if (GetWritingConnection() != null)
									{   // someone is still writing
										_logger.Debug(TAG, nameof(Conn_Received), $"[{chatConn.ConnectionName}] someone is writing");
										break;
									}
									chatConn.ChatState = IncomingChatConnection.ChatStates.Write;
									_chatEnd = "";
									_chatLine = "";
									SendUserPromptToAll(chatConn.ConnectionShortName, null);
									InvokeUpdateIncoming();
									_writeTimer.Start();
									SystemSounds.Beep.Play();

									//if (_lastWriteNotification.IsElapsedMinutes(60))
									{
										_lastWriteNotification.Start();
										DateTime dt = DateTime.Now;
										SendNotifications(chatConn, NotificationTypes.Writing);
									}

									_logger.Debug(TAG, nameof(Conn_Received), $"[{chatConn.ConnectionName}] starts writing");
									return;
								case CodeManager.ASC_LF:
									sendPrompt = true;
									break;
								case '?':
									string helpStr = $"\r\n{chatConn.LngText(LngKeys.Help)}";
									chatConn.SendAscii(helpStr);
									chatConn.SendIdlePrompt();
									chatConn.InputLine = "";
									break;
								case CodeManager.ASC_WRU:
									chatConn.SendAscii($"\r\n{GetSubscribers(chatConn.ConnectionLanguage.Id)}");
									chatConn.SendIdlePrompt();
									chatConn.InputLine = "";
									break;
								case '/':
									ToggleBuRefresh(chatConn);
									break;
								case '=':
									//Message($"{chatConn.ChatNameDebug} send '=' command");
									MessageDispatcher.Instance.Dispatch(chatConn.ConnectionId,
										$"send '=' command");
									chatConn.SendAscii($"\r\n{GetActivityHistory(chatConn.ConnectionLanguage.Id, 10)}");
									chatConn.SendIdlePrompt();
									chatConn.InputLine = "";
									break;
								case ':': // command mode
									//Message($"{chatConn.ChatNameDebug} start command mode");
									MessageDispatcher.Instance.Dispatch(chatConn.ConnectionId,
										$"start command mode");
									chatConn.StartCommandMode();
									break;
								case '+': // nofitier
									//Message($"{chatConn.ChatNameDebug} start notifier configuration");
									MessageDispatcher.Instance.Dispatch(chatConn.ConnectionId,
										$"start notifier configuration");
									chatConn.StartNotifierSetup(ActionBase.ActionCallTypes.Direct);
									//chatConn.SendIdlePrompt();
									chatConn.InputLine = "";
									break;
								/*
							case '.':
								chatConn.StartHamurabi();
								break;
								*/
								default:
									if (asciiStr[i] != CodeManager.ASC_CR)
									{
										chatConn.InputLine += asciiStr[i];
									}
									break;
							}
						}
						if (sendPrompt && !string.IsNullOrEmpty(chatConn.InputLine))
						{
							// send a new prompt if LF was pressed
							//connection.SendAsciiText($"\r\n:");
							//SendIdlePrompt(connection);
							chatConn.SendIdlePrompt();
						}
						break;
					case IncomingChatConnection.ChatStates.Write:
						_writeTimer.Stop(); // retrigger timer
						_writeTimer.Start();
						_chatLine += asciiStr;
						for (int i = 0; i < asciiStr.Length; i++)
						{
							char chr = asciiStr[i];
							switch (chr)
							{
								case '+':
									_chatEnd = "+";
									break;
								case '?':
									if (_chatEnd == "+")
									{
										// "+?" receivend -> release chat
										_chatEnd += "?";
										chatConn.ChatState = IncomingChatConnection.ChatStates.Idle;
										_writeTimer.Stop();
										//Message?.Invoke($"{chatConn.ChatShortName}: {_chatLine}");
										MessageDispatcher.Instance.Dispatch(chatConn.ConnectionId,
											$"{chatConn.ConnectionShortName}: {_chatLine}");
										_logger.Debug(TAG, nameof(Conn_Received), $"chatline={chatConn.ConnectionShortName}: {_chatLine}");
										chatConn.InputLine = "";
										InvokeUpdateIncoming();
									}
									break;
								case CodeManager.ASC_WRU:
									// supress WRU in write mode
									chr = '\x0';
									break;
								default:
									_chatEnd = "";
									break;
							}
							if (chr != '\x0')
							{
								SendAll(chr.ToString(), chatConn);
							}

							// write is finished, send all outstanding message
							if (_chatEnd == "+?")
							{
								ActivityHistoryManager.Instance.AddActivity(ActivityHistoryManager.Activities.Message, chatConn.ConnectionShortName, _chatLine);
								SendAll(_messageBuffer, null);
								_messageBuffer = "";
								//SendIdlePrompt(null);
								SendIdlePromptToAll(null);
							}
						}
						break;
					case IncomingChatConnection.ChatStates.Action:
						break;
				}
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(Conn_Received), "", ex);
			}
		}

		public void ToggleBuRefresh(IncomingChatConnection chatConn, bool loginTime=false)
		{
			if (!chatConn.ToggleBuRefresh())
			{
				return;
			}

			string einausStr = chatConn.BuRefreshActive ? chatConn.LngText(LngKeys.On) : chatConn.LngText(LngKeys.Off);
			if (loginTime)
			{
				chatConn.SendAscii($"\r\n{chatConn.LngText(LngKeys.HoldConnection)} {einausStr}");
			}
			else if (GetWritingConnection() != null)
			{
				chatConn.SendAscii($"\r\n{chatConn.LngText(LngKeys.HoldConnection)} {einausStr})\r\n");
			}
			else
			{
				chatConn.SendAscii($"\r\n{chatConn.LngText(LngKeys.HoldConnection)} {einausStr}");
				chatConn.SendIdlePrompt();
			}
			InvokeUpdateIncoming();

			einausStr = chatConn.BuRefreshActive ? "on" : "off";
			//Message?.Invoke($"{chatConn.ChatNameDebug} hold connection is {einausStr}");
			MessageDispatcher.Instance.Dispatch(chatConn.ConnectionId, $"{chatConn.ConnectionName} hold connection is {einausStr}");
		}

		public string GetActivityHistory(int lngId, int count)
		{
			List<string> list = ActivityHistoryManager.Instance.GetActivityList(count, lngId);
			string str = "";
			for (int i=0; i<list.Count; i++)
			{
				if (i>0)
				{
					str += "\r\n";
				}
				str += list[i];
			}
			return str;
		}

		public string GetSubscribers(int lngId)
		{
			string str = $"{LanguageDefinition.GetText(LngKeys.Subscribers, lngId)}:";
			List<ItelexIncoming> conns = CloneConnections();
			if (conns.Count == 0)
			{
				str += $"\r\n {LanguageDefinition.GetText(LngKeys.None, lngId)}";
			}
			else
			{
				foreach (IncomingChatConnection conn in conns)
				{
					str += $"\r\n {conn.ConnectionShortName} '{conn.RemoteAnswerbackStr}'";
				}
			}
			return str;
		}

		public override void Shutdown()
		{
			_shutDown = true;
			_logger.Debug(TAG, nameof(Shutdown), "wait while anyone writing start");
			while (GetWritingConnection() != null)
			{
				Thread.Sleep(500);
			}
			_logger.Debug(TAG, nameof(Shutdown), "wait while anyone writing end");

			base.Shutdown();
		}

		private void WriteTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			_writeTimer.Stop();
			List<ItelexIncoming> conns = CloneConnections();
			IncomingChatConnection activeConn = GetWritingConnection();
			if (activeConn == null) return;

			ActivityHistoryManager.Instance.AddActivity(ActivityHistoryManager.Activities.Message, activeConn.ConnectionShortName, _messageBuffer);

			// write timeout
			activeConn.ChatState = IncomingChatConnection.ChatStates.Idle;
			SendIdlePromptToAll(null);
		}

		protected override void ConnectionDropped(ItelexIncoming conn)
		{
			try
			{
				IncomingChatConnection chatConn = (IncomingChatConnection)conn;

				//_logger.Info(TAG, nameof(ConnectionDropped), $"[{chatConn.ConnectionName}] connection dropped");
				string msg = $"{chatConn.ConnectionName} has left";
				MessageDispatcher.Instance.Dispatch(chatConn.ConnectionId, msg);
				//LoginLogoff?.Invoke(msg);
				if (chatConn.ChatState == IncomingChatConnection.ChatStates.Idle || chatConn.ChatState == IncomingChatConnection.ChatStates.Write)
				{
					SendMessage($"{chatConn.ConnectionShortName} hat die konferenz verlassen.", chatConn);
				}

				if (chatConn.ConnectionShortName != null)
				{
					ActivityHistoryManager.Instance.AddActivity(ActivityHistoryManager.Activities.Logoff, chatConn.ConnectionShortName);
				}

				if (chatConn.ConnectionShortName != null)
				{
					DateTime dt = DateTime.Now;
					SendNotifications(chatConn, NotificationTypes.Logoff);
				}
				//MonitorManager.Instance.SetInactive();
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(ConnectionDropped), $"error [{conn.ConnectionName}]", ex);
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="notSendChatConn">null: send to all, not null: send to all but notSendConn</param>
		private void SendMessage(string msg, IncomingChatConnection notSendChatConn)
		{
			_logger.Debug(TAG, nameof(SendMessage), $"start msg={msg}");

			if (GetWritingConnection() == null)
			{
				// no writes active
				if (string.IsNullOrEmpty(_messageBuffer))
				{
					SendAll(_messageBuffer, notSendChatConn);
					_messageBuffer = "";
				}
				SendAll("\r\n" + msg, notSendChatConn);
				SendIdlePromptToAll(notSendChatConn);
			}
			else
			{
				// write active
				_messageBuffer += $"\r\n" + $"({DateTime.Now:HH:mm}) " + msg;
			}
			_logger.Debug(TAG, nameof(SendMessage), "end");
		}

		private void SendIdlePromptToAll(IncomingChatConnection notSendChatConn)
		{
			List<ItelexIncoming> conns = CloneConnections();
			foreach (IncomingChatConnection chatConn in conns)
			{
				if ((notSendChatConn == null || chatConn.ConnectionId != notSendChatConn.ConnectionId) &&
					chatConn.ChatState == IncomingChatConnection.ChatStates.Idle)
				{
					chatConn.SendIdlePrompt();
				}
			}
		}

		private void SendUserPromptToAll(string shortName, IncomingChatConnection notSendChatConn)
		{
			List<ItelexIncoming> conns = CloneConnections();
			foreach (IncomingChatConnection chatConn in conns)
			{
				if ((notSendChatConn == null || chatConn.ConnectionId != notSendChatConn.ConnectionId) &&
					chatConn.ChatState != IncomingChatConnection.ChatStates.NotLoggedIn)
				{
					chatConn.SendUserPrompt(shortName);
				}
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="notSendChatConn">null: send to all, not null: send to all but notSendConn</param>
		/// <param name="asciiText"></param>
		public void SendAll(string asciiText, IncomingChatConnection notSendChatConn)
		{
			List<ItelexIncoming> conns = CloneConnections();
			foreach (IncomingChatConnection chatConn in conns)
			{
				if ((notSendChatConn == null || chatConn.ConnectionId != notSendChatConn.ConnectionId) && 
					chatConn.ChatState == IncomingChatConnection.ChatStates.Idle)
				{
					chatConn.SendAscii(asciiText);
				}
			}
		}

		public void SendNotifications(IncomingChatConnection chatConn, NotificationTypes notificationType)
		{
			if (!_shutDown)
			{
				// do not send logoff notification in case of shut down
				_notificationManager.SendNotifications(chatConn, notificationType);
				return;
			}
		}

		public IncomingChatConnection GetConnectionByShortName(string shortName)
		{
			List<IncomingChatConnection> conns = CloneConnections<IncomingChatConnection>();
			return (from c in conns where string.Compare(c.ConnectionShortName, shortName, true) == 0 select c).FirstOrDefault();
		}

		public IncomingChatConnection GetWritingConnection()
		{
			List<IncomingChatConnection> conns = CloneConnections<IncomingChatConnection>();
			return (from c in conns where c.ChatState == IncomingChatConnection.ChatStates.Write select c).FirstOrDefault();
		}
	}
}
