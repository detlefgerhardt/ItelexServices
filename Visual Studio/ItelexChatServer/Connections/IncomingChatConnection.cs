using ItelexChatServer.Command;
using ItelexChatServer.Languages;
using ItelexChatServer.Actions;
using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;
using ItelexCommon.Connection;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;

namespace ItelexChatServer
{
	class IncomingChatConnection : ItelexIncoming
	{
		private const string INPUT_PROMPT = ": ";

		public enum ChatStates
		{
			NotLoggedIn,
			Idle,
			Write,
			Action
		}

		//public enum Actions { None, CommandMode, NotifySetup, Hamurabi };

		public delegate void ChatLoginEventHandler(IncomingChatConnection chatConnection);
		public event ChatLoginEventHandler ChatLogin;

		//public delegate void ChatDroppedEventHandler(IncomingChatConnection connection);
		//public event ChatDroppedEventHandler ChatDropped;

		public delegate void ChatReceivedEventHandler(IncomingChatConnection connection, string asciiText);
		public event ChatReceivedEventHandler ChatReceived;

		//public delegate void ChatUpdateEventHandler(IncomingChatConnection connection);
		//public event ChatUpdateEventHandler ChatUpdate;

		//public delegate void GotAnswerbackEventHandler(ChatConnection connection);
		//public event GotAnswerbackEventHandler GotAnswerback;

		public ChatStates ChatState { get; set; }

		public string ChatAction
		{
			get
			{
				if (_activeAction!=null)
				{
					return _activeAction.ChatAction;
				}
				else
				{
					return null;
				}
			}
		}

		public string InputLine { get; set; }

		private ActionBase _deferredAction;
		private ActionBase _activeAction;

		//private System.Timers.Timer _deferredTimer;
		//private bool _deferredTimerActive;
		//private object _deferredTimerLock = new object();

		public IncomingChatConnection()
		{
		}

		public IncomingChatConnection(TcpClient client, int idNumber, string logPath, LogTypes logLevel) : 
				base(client, idNumber, GetLogger(idNumber, logPath, logLevel))
		{
			TAG = nameof(IncomingChatConnection);

			ChatState = ChatStates.NotLoggedIn;
			BuRefreshActive = true;
			//UseBuTimer = true;
			_deferredAction = null;
			_activeAction = null;

			this.ItelexReceived += Connection_Received;
		}

		private static ItelexLogger GetLogger(int connectionId, string logPath, LogTypes logLevel)
		{
			return new ItelexLogger(connectionId, ConnectionDirections.In, null, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
		}

		//~ChatConnection()
		//{
		//	Dispose(false);
		//}


		#region Dispose

		// Flag: Has Dispose already been called?
		private bool _disposed = false;

		// Protected implementation of Dispose pattern.
		protected override void Dispose(bool disposing)
		{
			if (_disposed) return;

			//_itelexLogger?.ItelexLog(LogTypes.Debug, TAG, nameof(Dispose), $"ChatConnection {ConnectionName} disposing={disposing}");

			if (disposing)
			{
				this.ItelexReceived -= Connection_Received;
				//this.Dropped -= Connection_Dropped;
				//base.Dispose();
				//if (_deferredTimer != null)
				//{
				//	_deferredTimer.Stop();
				//	_deferredTimer.Elapsed -= DeferredTimer_Elapsed;
				//}
			}

			_disposed = true;
			base.Dispose(disposing);
		}

		#endregion Dispose

		private void Connection_Received(ItelexConnection connection, string text)
		{
			ChatReceived?.Invoke(this, text);
		}

		public override void Start()
		{
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Start), $"id={ConnectionId} Start");

			try
			{
				ItelexIncomingConfiguration config = new ItelexIncomingConfiguration
				{
					//LoginSequences = new LoginTypes[] { LoginTypes.GetKg, LoginTypes.GetShortName },
					LoginSequences = new AllLoginSequences(
						new LoginSequenceForExtensions(null, LoginSeqTypes.SendTime, LoginSeqTypes.SendKg, LoginSeqTypes.GetShortName)
					),
					OurPrgmVersionStr = Helper.GetVersionCode(),
					OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
					ItelexExtensions = GlobalData.Instance.ItelexValidExtensions,
					CheckShortNameIsValid = CheckShortnameIsValid,
					GetLngText = ConfigGetLngText,
					LngKeyMapper = new Dictionary<IncomingTexts, int>()
					{
						{ IncomingTexts.EnterShortName, (int)LngKeys.EnterShortName},
						{ IncomingTexts.InvalidShortName, (int)LngKeys.ShortNameInUse},
						{ IncomingTexts.ConnectionTerminated, (int)LngKeys.ConnectionTerminated},
						{ IncomingTexts.InternalError, (int)LngKeys.InternalError},
					}
				};

				if (!StartIncoming(config) || !IsConnected)
				{
					Logoff(null);
					return;
				}

				/*
				bool ok = false;
				int cnt = 0;
				while (cnt < 4)
				{
					InputResult shortNameResult = InputString($"{LngText(LngKeys.EnterShortName)}", "", ShiftStates.Ltrs, null, 10, 3);
					_itelexLogger.ItelexLog(LogTypes.Info, TAG, nameof(Start), $"id={ConnectionId} i={cnt}, shortNameInput={shortNameResult}");
					Thread.Sleep(500);
					if (shortNameResult.Error)
					{
						// error
						return;
					}
					if (shortNameResult.IsHelp)
					{
						// help
						SendAscii($"{LngText(LngKeys.InputHelp)}\r\n");
						continue;
					}
					string shortName = shortNameResult.InputString.Trim();
					if (string.IsNullOrEmpty(shortName))
					{
						// input is empty
						cnt++;
						continue;
					}
					if (((IncomingChatConnectionManager)GlobalData.Instance.IncomingConnectionManager).GetConnectionByShortName(shortName) != null)
					{
						// already in use
						SendAscii($"{LngText(LngKeys.ShortNameInUse)}\r\n");
						cnt++;
						continue;
					}

					ConnectionShortName = shortName;

					// input ok
					ok = true;
					break;
				}
				if (!ok)
				{
					return;
				}
				*/

				Thread.Sleep(500);

				ChatLogin?.Invoke(this);
				InvokeItelexUpdate();

				ChatMainLoop();
				return;
			}
			catch (Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Start), "error", ex);
				return;
			}
		}

		public void ChatMainLoop()
		{
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ChatMainLoop), "start");
			SendIdlePrompt();
			ChatState = ChatStates.Idle;

			InputLine = "";
			while (true)
			{
				CheckAndRunAction();

				try
				{
					if (!IsConnected) return;
				}
				catch
				{
					break;
				}
				Thread.Sleep(500);
			}

			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ChatMainLoop), $"Disconnect()");
			Logoff(null);
		}

		public void SendIdlePrompt()
		{
			string timeStr = Constants.PRINT_TIME ? $"({DateTime.Now:HH:mm})" : "";
			SendAscii($"\r\n{timeStr}:");
		}

		public void SendCommandPrompt()
		{
			string timeStr = Constants.PRINT_TIME ? $"({DateTime.Now:HH:mm})" : "";
			SendAscii($"\r\n{timeStr} cmd");
		}

		public void SendUserPrompt(string shortName)
		{
			string timeStr = Constants.PRINT_TIME ? $"({DateTime.Now:HH:mm}) " : "";
			SendAscii($"\r\n{timeStr}{shortName}:{CodeManager.ASC_LTRS}");
		}

		public bool ToggleBuRefresh()
		{
			if (ConnectionState != ConnectionStates.ItelexTexting)
			{
				return false;
			}

			BuRefreshActive = !BuRefreshActive;
			return true;
		}

		public string LngText(LngKeys lngKey)
		{
			return LanguageManager.Instance.GetText((int)lngKey, ConnectionLanguage.Id);
		}

		private string ConfigGetLngText(int key, string[] prms = null)
		{
			return LanguageManager.Instance.GetText(key, ConnectionLanguage.Id, prms);
		}

		public bool CheckShortnameIsValid(string shortName)
		{
			return ((IncomingChatConnectionManager)GlobalData.Instance.IncomingConnectionManager).GetConnectionByShortName(shortName) == null;
		}

		#region deferred actions

#if false
		private void DeferredTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			lock (_deferredTimerLock)
			{
				if (_deferredTimerActive || _activeAction != null || _deferredAction == null)
				{
					return;
				}

				try
				{
					_deferredTimerActive = true;
					_activeAction = _deferredAction;
					_deferredAction = null;
					_activeAction.Start(this, true);
					_activeAction = null;
					if (_deferredAction == null)
					{   // no new action -> idle
						ChatState = ChatStates.Idle;
						SendIdlePrompt();
					}
				}
				finally
				{
					_deferredTimerActive = false;
				}
			}
		}
#endif

		private void CheckAndRunAction()
		{
			if (_activeAction != null || _deferredAction == null)
			{
				// action active or no action to run
				return;
			}

			_activeAction = _deferredAction;
			_deferredAction = null;

			_activeAction.Run(this, true);

			_activeAction = null;
			if (_deferredAction == null)
			{   // no new action -> idle
				ChatState = ChatStates.Idle;
				SendIdlePrompt();
			}
		}

		private bool StartAction(ActionBase actionClass)
		{
			_deferredAction = actionClass;
			//ChatState = ChatStates.Action;
			//ChatUpdate?.Invoke(this);
			return true;
		}

		public bool StartCommandMode()
		{
			return StartAction(new CommandModeAction(ConnectionLanguage, _itelexLogger));
		}

		public bool StartNotifierSetup(ActionBase.ActionCallTypes actionCallType)
		{
			return StartAction(new NotificationSetupAction(ConnectionLanguage, actionCallType, _itelexLogger));
		}

		public bool StartHamurabi(ActionBase.ActionCallTypes actionCallType)
		{
			return StartAction(new HamurabiAction(actionCallType, _itelexLogger));
		}

		public bool StartBiorhythmus(ActionBase.ActionCallTypes actionCallType)
		{
			return StartAction(new BiorhythmusAction(actionCallType, _itelexLogger));
		}

		#endregion deferred actions

		public override string ToString()
		{
			return $"{ConnectionName} {ConnectionShortName} ";
		}
	}
}
