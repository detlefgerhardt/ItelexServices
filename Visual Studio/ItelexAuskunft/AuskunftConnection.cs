using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using Telexsuche;

namespace ItelexAuskunft
{
	class AuskunftConnection : ItelexIncoming, IDisposable
	{
		private bool _inputActive;
		private string _inputLine;

		/// <summary>
		/// Contructor for generics
		/// </summary>
		public AuskunftConnection()
		{
		}

		public AuskunftConnection(TcpClient client, int idNumber, string logPath, LogTypes logLevel) : 
			base(client, idNumber, GetLogger(idNumber, logPath, logLevel))
		{
			TAG = nameof(AuskunftConnection);

			_inputActive = false;
			this.ItelexReceived += AuskunftConnection_Received;

			InitTimerAndHandler();
		}

		private static ItelexLogger GetLogger(int connectionId, string logPath, LogTypes logLevel)
		{
			return new ItelexLogger(connectionId, ConnectionDirections.In, null, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
		}

		~AuskunftConnection()
		{
			Dispose(false);
		}

		private bool _disposed = false;

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (!this._disposed)
				{
					// Explicitly set root references to null to expressly tell the GarbageCollector
					// that the resources have been disposed of and its ok to release the memory 
					// allocated for them.
					if (disposing)
					{
						this.ItelexReceived -= AuskunftConnection_Received;
					}

					// Release all unmanaged resources here
				}
			}
			finally
			{
				this._disposed = true;
				// explicitly call the base class Dispose implementation
				base.Dispose(disposing);
			}
		}

		public override void Start()
		{
			Thread.Sleep(500);

			while (!Abfrage.Instance.TablesLoaded)
			{
				Thread.Sleep(100);
			}

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
				Logoff(null);
				return;
			}

			//_inputEndChars = new char[] { CodeManager.ASC_CR, CodeManager.ASC_LF };

			for(int i=0; i<3; i++)
			{
				InputResult auskunftJaNein = InputString($"\r\nmoechten sie auskunft ueber einen telexteilnehmer? ja/nein/hilfe\r\n",
					ShiftStates.Ltrs, null, 60, 1);
				Thread.Sleep(500);
				if (auskunftJaNein.ErrorOrTimeoutOrDisconnected || auskunftJaNein.InputString == "nein")
				{
					Logoff();
					return;
				}

				if (auskunftJaNein.InputString == "ja") break;

				if (auskunftJaNein.InputString == "hilfe")
				{
					SendHilfe();
					continue;
				}
			}

			SendAscii("\r\n");
			//SendAscii("\r\nhilfe mit '+hilfe+?'\r\n");

			int cnt = 0;
			while (cnt < 10)
			{
				cnt++;

				_inputLine = "";
				_inputActive = true;
				SendAscii("bitte land und suchbegriffe:\r\n");
				while (true)
				{
					Thread.Sleep(500);
					if (!IsConnected)
					{
						//Disconnect(DisconnectReasons.NotConnected);
						return;
					}
					if (_inputLine.EndsWith("+?"))
					{
						_inputLine = _inputLine.Substring(0, _inputLine.Length - 2);
						break;
					}

					if (LastSendRecvTime.ElapsedSeconds > 120)
					{
						// 120 s timeout
						if (!string.IsNullOrWhiteSpace(_inputLine))
						{
							break;
						}
						else
						{
							// empty input
							Logoff();
							return;
						}
					}
				}

				_inputLine = _inputLine.Trim();
				_inputLine = _inputLine.Replace("\r", " ");
				_inputLine = _inputLine.Replace("\n", " ");
				_inputLine = _inputLine.Replace(",", " ");
				_inputLine = _inputLine.Replace(".", " ");
				_inputLine = _inputLine.Trim();
				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Start), $"_inputLine = '{_inputLine}'");

				string resultStr;
				if (_inputLine == "+info")
				{
					resultStr = InfoText();
				}
				//if (_inputLine == "+hilfe")
				//{
				//	resultStr = SendHilfe();
				//}
				/*
				else if (int.TryParse(_inputLine, out int number))
				{
					resultStr = NummernSuche(number);
				}
				*/
				else
				{
					resultStr = Suche(_inputLine);
				}

				SendAscii(resultStr + "\r\n");
				WaitAllSendBuffersEmpty();
			}

			Logoff();
		}

		private void Logoff()
		{
			Logoff("\r\ndie verbindung wird beendet.\r\n\n");
		}

		private string Suche(string eingabe)
		{
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Suche), $"eingabe='{eingabe}'");

			string[] searchWords = _inputLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"search = {string.Join(",", searchWords)}");
			DispatchMsg($"search: {string.Join(",", searchWords)}");

			if (searchWords.Length == 0)
			{
				return null;
			}

			List<TextTafelElement> results = Abfrage.Instance.Suche(searchWords, out int count, out bool badEsbToAsb);

			string resultStr = "\r\n";
			string resultLog = "";
			if (count > 0 && results.Count == 0)
			{
				resultStr = "\r\nihre angaben reichen nicht aus. bitte eingabe regeln beachten++\r\n";
				resultLog = "angaben reichen nicht aus";
			}
			else if (count == 0 && results.Count == 0)
			{
				resultStr = "\r\nunter ihren angaben kein teilnehmer verzeichnet++\r\n";
				resultLog = "nichts gefunden";
			}
			else if (badEsbToAsb && results.Count == 1)
			{
				resultStr = "\r\ndie meisten suchbegriffe kommen in folgendem eintrag vor:\r\n";
				resultLog = "meisten suchbegriffe";
			}
			else if (badEsbToAsb && results.Count > 1)
			{
				resultStr = "\r\ndie meisten suchbegriffe kommen in folgenden eintraegen vor:\r\n";
				resultLog = "meisten suchbegriffe";
			}

			if (string.IsNullOrEmpty(resultLog))
			{
				DispatchMsg($"result: {resultLog}");
			}

			if (results.Count > 0)
			{
				foreach (TextTafelElement result in results)
				{
					resultStr += GetResultStr(result);
				}
				resultStr += "\r\n++\r\n";
			}

			return resultStr;
		}

		/*
		private string NummernSuche(int nummer)
		{
			Logging.Instance.Debug(TAG, nameof(NummernSuche), $"nummer = '{nummer}'");
			TextTafelElement result = Abfrage.Instance.NummernSuche(nummer);
			return GetResultStr(result);
		}
		*/

		public string GetResultStr(TextTafelElement result)
		{
			if (result == null) return "";

			string resultStr = "\r\n---\r\n";

			string resultLog = "";
			for (int i = 0; i < result.Zeilen.Length; i++)
			{
				string line = result.Zeilen[i];
				resultStr += line + "\r\n";
				if (!string.IsNullOrEmpty(resultLog))
				{
					resultLog += ",";
				}
				resultLog += line;
			}
			resultStr += result.Kennung;
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"result: {resultLog}");
			DispatchMsg($"result: {resultLog}");

			return resultStr;
		}

		private string InfoText()
		{
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(InfoText), $"send info");
			return $"\r\n\n{Helper.GetVersion(false)}\r\n{Tools.GetTafelInfo(Helper.GetExePath().ToLower())}\r\n";
		}

		private void SendHilfe()
		{
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(SendHilfe), $"send hilfe");
			//       12345678901234567890123456789012345678901234567890123456789012345678
			string hilfeText = 
				"\r\ndiese auskunft ist der bedienung der historischen auskunft von 1973" +
				"\r\nnachempfunden und liefert die ergebnisse des telexverzeichnisses von" +
				"\r\n1987. die orginal-auskunft hatte keine hilfefunktion. das merkblatt" +
				"\r\nzur bedienung ist im i-telex-wiki unter dienste / historische" +
				"\r\nauskunft zu finden.";
			SendAscii(hilfeText);
		}

		private void AuskunftConnection_Received(ItelexConnection connection, string asciiText)
		{
			if (_inputActive)
			{
				_inputLine += asciiText;
			}
		}
	}
}
