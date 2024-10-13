using ItelexCommon;
using ItelexCommon.Connection;
using ItelexMsgServer.Data;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using ItelexMsgServer.Languages;
using System.Linq;
using ItelexMsgServer.Fax;
using ItelexCommon.Logger;
using ItelexCommon.Utility;

namespace ItelexMsgServer.Connections
{
	class MinitelexConnection : ItelexIncoming
	{
		private const string INPUT_PROMPT = null;

		private MsgServerDatabase _database;

		private SubscriberServer _subcriberServer;

		private MinitelexConnectionManager _minitelexConnectionManager;

		private MessageDispatcher _messageDispatcher;

		public DateTime SessionStartTime { get; set; }

		private MinitelexUserItem _minitelexUser;


		public MinitelexConnection()
		{
		}

		public MinitelexConnection(TcpClient client, int idNumber, string logPath, LogTypes logLevel) : 
			base(client, idNumber, GetLogger(idNumber, logPath, logLevel))
		{
			TAG = nameof(MinitelexConnection);

			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetLogger), $"start id={ConnectionId} port={LocalPort}");

			_database = MsgServerDatabase.Instance;
			_subcriberServer = new SubscriberServer();
			_minitelexConnectionManager = MinitelexConnectionManager.Instance;
			_messageDispatcher = MessageDispatcher.Instance;

			_inputActive = false;
			this.ItelexReceived += IncomingConnection_Received;
			this.BaudotSendRecv += IncomingConnection_ReceivedBaudot;
			this.ItelexSend += CommingConnection_ItelexSend;

			InitTimerAndHandler();
		}

		#region Dispose

		// flag: has dispose already been called?
		private bool _disposed = false;

		// Protected implementation of Dispose pattern.
		protected override void Dispose(bool disposing)
		{
			//LogManager.Instance.Logger.Debug(TAG, nameof(Dispose), $"disposing={disposing}");

			base.Dispose(disposing);

			if (_disposed) return;

			if (disposing)
			{
				this.ItelexReceived -= IncomingConnection_Received;
				this.BaudotSendRecv -= IncomingConnection_ReceivedBaudot;
				this.ItelexSend -= CommingConnection_ItelexSend;
			}
			_disposed = true;
		}

		#endregion Dispose

		private static ItelexLogger GetLogger(int connectionId, string logPath, LogTypes logLevel)
		{
			ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.In, null, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
			itelexLogger.ItelexLog(LogTypes.Debug, nameof(MinitelexConnection), nameof(GetLogger), $"start id={connectionId}");
			return itelexLogger;
		}

		/// <summary>
		/// init timer and handler, that have to be disposed manually
		/// </summary>
		public override void InitTimerAndHandler()
		{
			base.InitTimerAndHandler();
		}

		public override void Start()
		{
			// check port
			int portIndex = LocalPort - Constants.MINITELEX_LOCAL_PORT;
			_minitelexUser = _database.MinitelexUserByPortIndex(portIndex);
			if (_minitelexUser == null)
			{
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"PortIndex {portIndex} not found");
				_messageDispatcher.Dispatch(ConnectionId, $"Invalid MiniTelex LocalPort {LocalPort}");
				Logoff(null);
				return;
			}

			_messageDispatcher.Dispatch(ConnectionId,
					$"Incoming MiniTelex connection for {_minitelexUser?.ItelexNumber} {_minitelexUser?.Name} " +
					$"{LocalPort}/{portIndex + Constants.MINITELEX_PUBLIC_PORT}");

			ItelexIncomingConfiguration config = new ItelexIncomingConfiguration
			{
				OurPrgmVersionStr = Helper.GetVersionCode(),
				OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
				LoginSequences = new AllLoginSequences(
					new LoginSequenceForExtensions(null, LoginSeqTypes.SendTime, LoginSeqTypes.SendKg, LoginSeqTypes.GetKg)
				),
				ItelexExtensions = _minitelexConnectionManager.Config.ItelexExtensions,
				GetLngText = GetLngText,
				OverwriteAnswerbackStr = $"{_minitelexUser.ItelexNumber} {_minitelexUser.Kennung}",
				LngKeyMapper = new Dictionary<IncomingTexts, int>()
				{
					{ IncomingTexts.ConnectionTerminated, (int)LngKeys.ConnectionTerminated},
					{ IncomingTexts.InternalError, (int)LngKeys.InternalError},
				}
			};

			_inputLine = "";
			_inputActive = true;

			if (!StartIncoming(config) || !IsConnected)
			{
				Logoff(null);
				return;
			}

			_inputActive = false;
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Start), $"id={ConnectionId} Start");

			string preMsg = _inputLine;
			preMsg = preMsg.TrimEnd(new char[] { '\r', '\n' });
			preMsg += "\r\n";
			if (!string.IsNullOrEmpty(RemoteAnswerbackStr))
			{
				preMsg += RemoteAnswerbackStr;
			}

			if (_minitelexUser.Deranged)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				//             1234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\ndieser anschluss ist als gestoert gekennzeichnet.");
				SendAscii("\r\ndie nachricht wird moeglicherweise nicht zugestellt.");
			}

			if (_minitelexUser.Faxnummer.StartsWith("00"))
			{
				SendAscii(CodeManager.ASC_COND_NL);
				//             1234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\nachtung, fuer diese auslandsverbindung fallen beim");
				SendAscii("\r\nbetreiber des dienstes kosten an. bitte nicht exzessive");
				SendAscii("\r\nnutzen.");
			}

			if (!IsConnected)
			{
				Logoff(null);
				return;
			}

			SendAscii(CodeManager.ASC_COND_NL);
			SendAscii("\r\n");
			_inputLine = "";
			_inputActive = true;
			string msg = GetMultiLineMessage("");
			_inputActive = false;
			Logoff(null);
			//_messageDispatcher.Dispatch(ConnectionId, $"{TAG} Logoff");

			// send fax

			msg = ParseMessage(msg);
			if (string.IsNullOrWhiteSpace(msg.Trim(new char[] { '+' })))
			{
				_messageDispatcher.Dispatch(ConnectionId, $"empty message not sent");
				_itelexLogger.ItelexLog(LogTypes.Info, TAG, nameof(Start), $"id={ConnectionId} empty message not sent");
				return;
			}

			preMsg = ParseMessage(preMsg);
			msg = preMsg + "\r\n\n" + msg;

			_itelexLogger.ItelexLog(LogTypes.Info, TAG, nameof(Start),
					$"id={ConnectionId} send msg to {_minitelexUser.ItelexNumber} {_minitelexUser.Faxnummer}");

			string remoteNummer = RemoteNumber.HasValue ? RemoteNumber.Value.ToString() : "unbekannt";

#if DEBUG
			// FaxManager.Instance.SendFax(msg, FaxFormat.Endless, true, 0, true, "7822222", "06426 921125", "de");
			FaxManager.Instance.SendFax(msg, FaxFormat.Endless, true, 0, true, remoteNummer, _minitelexUser.Faxnummer, "de");
#else
			FaxManager.Instance.SendFax(msg, FaxFormat.Endless, true, 0, true, remoteNummer, _minitelexUser.Faxnummer, "de");
#endif
		}

		private string GetLngText(int key, string[] prms = null)
		{
			return LanguageManager.Instance.GetText(key, ConnectionLanguage.Id, prms); 
		}

		private bool _inputActive;
		private string _inputLine;
		private object _inputLineLock = new object();

		/// <summary>
		/// Receive telex from remote. Stay here until remote disconnects.
		/// </summary>
		/// <param name="remainungInput"></param>
		/// <returns></returns>
		private string GetMultiLineMessage(string remainungInput)
		{
			int lastWruCheckPos = 0;
			while (true)
			{
				Thread.Sleep(100);
				lock (_inputLineLock)
				{
					if (!IsConnected)
					{
						string msg = remainungInput + _inputLine;
						_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetMultiLineMessage), "disconnect by remote");
						return msg;
					}

					if (LastSendRecvTime.IsElapsedSeconds(60 * 5))
					{
						string msg = remainungInput + _inputLine;
						_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetMultiLineMessage), "send/recv timeout");
						return msg;
					}

					if (_inputLine == "") continue;

					// check WRU
					if (_inputLine.Substring(lastWruCheckPos).Contains(CodeManager.ASC_WRU))
					{
						lastWruCheckPos = _inputLine.Length;
						SendAscii($"\r\n{_minitelexUser.ItelexNumber} {_minitelexUser.Kennung}");
					}
				}
			}
		}

		private void CommingConnection_ItelexSend(ItelexConnection connection, string asciiText)
		{
			lock (_inputLineLock)
			{
				_inputLine += asciiText;
			}
		}

		private string ParseMessage(string msgStr)
		{
			if (string.IsNullOrEmpty(msgStr)) return "";

			// remove starting "\r\n" and special characters
			//messageStr = messageStr.TrimStart(new char[] { '\r', '\n' });
			//msgStr = msgStr.Replace(CodeManager.ASC_WRU.ToString(), "");
			msgStr = msgStr.Replace(CodeManager.ASC_HEREIS.ToString(), "");
			//msgStr = msgStr.Replace(CodeManager.ASC_BEL.ToString(), "");
			msgStr = msgStr.Replace(CodeManager.ASC_LTRS.ToString(), "");
			msgStr = msgStr.Replace(CodeManager.ASC_FIGS.ToString(), "");
			msgStr = msgStr.Replace(CodeManager.ASC_NUL.ToString(), "");

			// remove after "+++"
			//msgStr = CropPlus(msgStr);

			// remove starting and ending "\r\n"
			msgStr = msgStr.Trim(new char[] { '\r', '\n' });
			return msgStr;
		}

		private string CropPlus(string str)
		{
			int pos = str.IndexOf("+++++");
			if (pos != -1) return str.Substring(0, pos + 5);
			pos = str.IndexOf("++++");
			if (pos != -1) return str.Substring(0, pos + 4);
			pos = str.IndexOf("+++");
			if (pos != -1) return str.Substring(0, pos + 3);
			return str;
		}


		public string LngText(LngKeys lngKey)
		{
			return LanguageManager.Instance.GetText((int)lngKey, ConnectionLanguage.Id);
		}

		public string LngText(LngKeys lngKey, string[] param)
		{
			return LanguageManager.Instance.GetText((int)lngKey, ConnectionLanguage.Id, param);
		}

		private void IncomingConnection_Received(ItelexConnection connection, string asciiText)
		{
			if (_inputActive)
			{
				lock (_inputLineLock)
				{
					_inputLine += asciiText;
				}
			}
		}

		private bool _receiveActive = false;
		private List<byte> _receivedBaudot = null;

		private void IncomingConnection_ReceivedBaudot(ItelexConnection connection, byte[] baudot)
		{
			if (_receiveActive)
			{
				if (_receivedBaudot == null)
				{
					_receivedBaudot = new List<byte>();
				}
				_receivedBaudot.AddRange(baudot);
			}
		}
	}
}
