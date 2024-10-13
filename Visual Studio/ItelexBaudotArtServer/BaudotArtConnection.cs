using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using static ItelexCommon.CodeManager;

namespace ItelexBaudotArtServer
{
	class BaudotArtConnection : ItelexIncoming
	{
		private EyeballChar _eyeballChar;

		private bool _inputActive;
		private string _inputLine;
		private string _gegenschreiben;

		private bool _receiveActive;
		private List<byte> _receivedBaudot;
		private string _recveiceFilename;

		public BaudotArtConnection()
		{
		}

		public BaudotArtConnection(TcpClient client, int idNumber, string logPath, LogTypes logLevel) : 
				base(client, idNumber, GetLogger(idNumber, logPath, logLevel))
		{
			TAG = nameof(BaudotArtConnection);

			_eyeballChar = EyeballChar.Instance;

			//IdNumber = idNumber;
			//_allowedExtensionNumbers = ConnectionManager.Instance.GetAllowedExtensionsAsInt();

			_inputActive = false;
			_receiveActive = false;
			_recveiceFilename = null;
			this.ItelexReceived += Connection_ReceivedAscii;
			this.BaudotSendRecv += Connection_ReceivedBaudot;

			InitTimerAndHandler();
		}

		private static ItelexLogger GetLogger(int connectionId, string logPath, LogTypes logLevel)
		{
			return new ItelexLogger(connectionId, ConnectionDirections.In, null, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
		}

		~BaudotArtConnection()
		{
			//LogManager.Instance.Logger.Debug(TAG, "~BaudotArtConnection", "destructor");
			Dispose(false);
		}

		private bool _disposed = false;

		protected override void Dispose(bool disposing)
		{
			//LogManager.Instance.Logger.Debug(TAG, nameof(Dispose), $"_disposed={_disposed} disposing={disposing}");

			if (!this._disposed) return;

			try
			{
				// Explicitly set root references to null to expressly tell the GarbageCollector
				// that the resources have been disposed of and its ok to release the memory 
				// allocated for them.
				if (disposing)
				{
					this.ItelexReceived -= Connection_ReceivedAscii;
					this.BaudotSendRecv -= Connection_ReceivedBaudot;
				}

				// Release all unmanaged resources here
			}
			finally
			{
				this._disposed = true;
				base.Dispose(disposing);
			}
		}

		public override void Start()
		{
			//_inputEndChars = new char[] { CodeManager.ASC_LF };

			BaudotArtFiles.Instance.LoadFileList();

			ItelexIncomingConfiguration config = new ItelexIncomingConfiguration
			{
				//LoginSequences = new LoginTypes[] { LoginTypes.SendTime, LoginTypes.SendKg, LoginTypes.GetKg },
				LoginSequences = new AllLoginSequences(
					new LoginSequenceForExtensions(null, LoginSeqTypes.SendTime, LoginSeqTypes.SendKg, LoginSeqTypes.GetKg)
				),
				OurPrgmVersionStr = Helper.GetVersionCode(),
				OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
				ItelexExtensions = GlobalData.Instance.ItelexValidExtensions,
			};
			if (!StartIncoming(config) || !IsConnected)
			{
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"not connected after StartIncoming");
				Logoff(null);
				return;
			}

			SendAscii($"\r\neingaben mit 'nl' (neue zeile) bestaetigen.\r\n");

			bool showList = false;
			for (int cnt = 0; cnt < 3; cnt++)
			{
				if (!IsConnected) return;

				if (showList)
				{
					SendAscii($"\r\nverfuegbare bilder:");
					SendAscii("\r\nnr name                groesse minuten");
					foreach (BaudotArtItem item in BaudotArtFiles.Instance.BaudotArtList)
					{
						string line = string.Format("\r\n{0:D2} {1,-20} {2,6:D} {3,3:D}", item.Number, item.Name, item.Size, item.DownloadMinutes);
						SendAscii(line);
					}
					SendAscii("\r\nbild zum server senden mit 'send'.\r\n");
					showList = false;
				}

				InputResult result = InputString($"\r\nbildnummer (?=hilfe, m=menue):", ShiftStates.Ltrs, null, 60, 1,
					false, false, true);
				if (result.ErrorOrTimeoutOrDisconnected) continue;

				if (result.IsHelp)
				{
					_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"help");
					DispatchMsg($"help");
					SendHelp();
					continue;
				}

				if (result.InputString == "m")
				{
					showList = true;
					continue;
				}

				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"input {result.InputString}");
				DispatchMsg($"input {result.InputString}");

				if (result.InputString.Length >= 4 && result.InputString.Substring(0, 4) == "send")
				{
					ReceiveBaudotArt();
					continue;
				}

				if (!int.TryParse(result.InputString, out int artNr)) artNr = 0;
				if (artNr == 99) break;

				BaudotArtItem selItem = (from b in BaudotArtFiles.Instance.BaudotArtList where b.Number == artNr select b).FirstOrDefault();
				int waitSec = 8;
				if (selItem != null)
				{
					SendAscii($"\r\ndie ausgabe startet in {waitSec} sekunden.\r\n");
					WaitAllSendBuffersEmpty();
					Thread.Sleep(waitSec * 1000);
					SendBaudotArt(selItem);
				}
				else
				{
					SendAscii("\rungueltiger befehl.\r\n");
				}
			}
			//SendAscii("\r\ndie verbindung wird beendet.\r\n\n\n");
			//Thread.Sleep(4000);
			Logoff("\r\ndie verbindung wird beendet.\r\n\n");
		}

		private void SendHelp()
		{
			//                        12345678901234567890123456789012345678901234567890123456789012345678
			SendAscii("\r\n\nnach auswahl des bildes startet die ausgabe automatisch nach 5");
			SendAscii("\r\nsekunden und kann durch gegenschreiben mit 'x' abgebrochen werden.");
			SendAscii("\r\nbeenden der verbindung mit 99 oder mit st-taste.");
			SendAscii("\r\n\nsenden von bildern:");
			SendAscii("\r\nmit dem befehl 'send' statt einer bildnummer, kann ein bild an den");
			SendAscii("\r\nserver gesendet werden. das gesendete bild wird erst nach pruefung");
			SendAscii("\r\nin die liste aufgenommen.\r\n");
		}

		private void SendBaudotArt(BaudotArtItem item)
		{
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(SendBaudotArt), $"send {item.Name}");
			DispatchMsg($"send {item.Name}");

			byte[] data = File.ReadAllBytes(item.Filename);
			data = CodeManager.MirrorByteArray(data);

			ShiftStates shiftState = ShiftStates.Unknown;
			byte[] buHeader = new byte[] { BAU_LTRS, BAU_LTRS, BAU_LTRS, BAU_LTRS, BAU_LTRS, BAU_LTRS, BAU_LTRS, BAU_LTRS, BAU_LTRS, BAU_LTRS };
			if (true)
			{
				byte[] nameBuffer = _eyeballChar.GetPunchCodesFromAscii(item.Name.ToLower());
				byte[] eyeballBuffer = new byte[0];
				byte[] nullHeader = new byte[] { BAU_NUL, BAU_NUL, BAU_NUL, BAU_NUL, BAU_NUL };
				eyeballBuffer = CommonHelper.AddBytes(eyeballBuffer, buHeader);
				eyeballBuffer = CommonHelper.AddBytes(eyeballBuffer, nullHeader);
				eyeballBuffer = CommonHelper.AddBytes(eyeballBuffer, nameBuffer);
				eyeballBuffer = CommonHelper.AddBytes(eyeballBuffer, nullHeader);
				eyeballBuffer = CommonHelper.AddBytes(eyeballBuffer, buHeader);
				eyeballBuffer = CommonHelper.AddBytes(eyeballBuffer, buHeader);
				SendBaudot(eyeballBuffer);
			}

			shiftState = ShiftStates.Unknown;
			string ascii = BaudotDataToAscii(data, ref shiftState, CodeSets.ITA2, true);

			_inputLine = "";
			_inputActive = true;
			while (IsConnected && ascii.Length > 0)
			{
				int cnt = 20 - _sendBufferCount;
				if (cnt < 0) cnt = 0;
				if (cnt > ascii.Length)
				{
					cnt = ascii.Length;
				}
				string str = ascii.Substring(0, cnt);
				SendAscii(str);
				ascii = ascii.Substring(cnt);
				if (!string.IsNullOrEmpty(_inputLine))
				{
					if (_inputLine.Contains("x"))
					{
						WaitAllSendBuffersEmpty();
						SendAscii("\r\n\ndie ausgabe wurde unterbrochen.\r\n");
						DispatchMsg("output interrupted");
						return;
					}
					_inputLine = "";
				}
				Thread.Sleep(100);
			}
			SendBaudot(buHeader);
			SendBaudot(buHeader);

			_inputActive = false;
			DispatchMsg($"send {item.Name} finished");

			for (int i = 0; i < 5; i++)
			{
				SendAscii(CodeManager.ASC_BEL);
				Thread.Sleep(1000);
			}
		}

		private void ReceiveBaudotArt()
		{
			int recvNr = 1;
			string fileName;
			string fullName;
			while (true)
			{
				fileName = $"recv ({recvNr}).ls";
				fullName = Path.Combine(Helper.GetExePath(), Constants.RECV_PATH, fileName);
				if (!File.Exists(fullName))
				{
					_recveiceFilename = fileName;
					break;
				}
				recvNr++;
			}

			InputResult result = InputString($"name des bildes:", ShiftStates.Ltrs, null, 60, 3);
			if (result.ErrorOrTimeoutOrDisconnected)
			{
				SendAscii("\rabgebrochen\r\n");
				return;
			}
			string name = result.InputString;

			SendAscii("\rbitte jetzt senden. am ende der uebertragung 3 x klingel senden.\r\n\n");

			_receivedBaudot = new List<byte>();
			_receiveActive = true;

			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"recv {fileName}");
			DispatchMsg($"send {fileName}");

			while (IsConnected)
			{
				Thread.Sleep(100);
				if (FindTapeEnd())
				{
					_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"recv terminated");
					DispatchMsg($"recv terminated");
					break;
				}
			}

			SendAscii("\r\n\nuebertragung beendet. daten werden gespeichert.\r\n");

			string str = $"baudot-art upload '{name}' [{RemoteAnswerbackStr}]";
			CommonHelper.SendWebMail(str, str);

			_receiveActive = false;
			byte[] data = CodeManager.MirrorByteArray(_receivedBaudot.ToArray());
			File.WriteAllBytes(fullName, data);
			_receivedBaudot = null;

			string logLine = $"{DateTime.Now:dd.MM.yy HH:mm} {fileName}; \"{name.Trim()}\"; {RemoteAnswerbackStr}; {IpAddress}";
			string logFullName = Path.Combine(Helper.GetExePath(), Constants.RECV_PATH, "recv.log");
			File.AppendAllText(logFullName, logLine + "\r\n");

			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"recv ok");
			DispatchMsg($"recv ok");
		}

		private bool FindTapeEnd()
		{
			if (_receivedBaudot.Count < 3) return false;

			byte bel = CodeManager.BAU_BEL;
			for (int i = _receivedBaudot.Count - 3; i >= 0; i--)
			{
				if (_receivedBaudot[i] == bel && _receivedBaudot[i + 1] == bel && _receivedBaudot[i + 2] == bel)
				{
					return true;
				}
			}
			return false;
		}

		private void Connection_ReceivedAscii(ItelexConnection connection, string asciiText)
		{
			if (_inputActive)
			{
				_inputLine += asciiText;
			}
			else
			{
				_gegenschreiben += asciiText;
			}
		}

		private void Connection_ReceivedBaudot(ItelexConnection connection, byte[] baudot)
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

		public override string ToString()
		{
			return $"{ConnectionId} {IpAddress} {RemoteAnswerbackStr}";
		}
	}
}
