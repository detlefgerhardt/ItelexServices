using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace ItelexWeatherServer
{

	class IncomingConnection : ItelexIncoming
	{
		private const int SHORTLINE_MAX = 59;

		private DwdServer _dwdServer;

		private bool _inputActive;
		private string _inputLine;
		private object _inputLineLock = new object();

		public IncomingConnection()
		{
		}

		public IncomingConnection(TcpClient client, int idNumber, string logPath, LogTypes logLevel) : 
				base(client, idNumber, GetLogger(idNumber, logPath, logLevel))
		{
			TAG = nameof(IncomingConnection);

			_dwdServer = DwdServer.Instance;

			_inputActive = false;
			this.ItelexReceived += WeatherConnection_Received;

			InitTimerAndHandler();
		}

		private static ItelexLogger GetLogger(int connectionId, string logPath, LogTypes logLevel)
		{
			return new ItelexLogger(connectionId, ConnectionDirections.In, null, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
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
						this.ItelexReceived -= WeatherConnection_Received;
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

		~IncomingConnection()
		{
			Debug.WriteLine("~IncomingConnection");
			Dispose(false);
		}

		public override void Start()
		{
			try
			{
				StartEx();
			}
			catch (Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Start), $"error", ex);
			}
		}

		public void StartEx()
		{
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

			bool menuShown = false;
			bool end = false;

			//SendAscii("\r\n\nachtung, geaenderte bedienung. eingaben mit 'neue zeile' (zl)\r\nbestaetigen.\r\n");

			SendAscii("\r\ni = inhalt, x = trennen, ? = hilfe: ");
			DoInput(ref end, ref menuShown);
			if (end || !IsConnected)
			{
				Logoff();
				return;
			}

			menuShown = true;
			if (!menuShown)
			{
				SendMenu();
				menuShown = true;
			}

			while (!end && IsConnected)
			{
				SendAscii("\r\nauswahl (oder i,x,?): ");
				DoInput(ref end, ref menuShown);
			}

			Logoff();
		}

		private void DoInput(ref bool end, ref bool menuShown)
		{
			InputResult result = InputString("", ShiftStates.Ltrs, "", 3, 1);
			if (result.Timeout || !IsConnected)
			{
				end = true;
				return;
			}

			string inputStr = result.InputString;
			if (!string.IsNullOrEmpty(inputStr)) inputStr = inputStr.Trim();
			SendAscii($"{inputStr} gewaehlt.");

			bool shortLines = true;
			if (inputStr.Length >= 2 && inputStr.Substring(0, 1) == "t")
			{
				inputStr = inputStr.Substring(1);
				//shortLines = true;
			}

			DispatchMsg($"auswahl: '{inputStr}'");

			StartInputGegenschreiben();

			switch (inputStr)
			{
				case "i":
					SendMenu();
					menuShown = true;
					break;
				case "x":
					end = true;
					return;
				case "?":
					SendHelpText();
					return;
				case "de":
					break;
				case "wa":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.Deut, shortLines);
					break;
				case "wb":
					SendWeatherText(DwdServer.DwdTypeEnum.WarnWoche, DwdServer.DwdRegionEnum.Deut, shortLines);
					break;
				case "wd":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.SchlesHam, shortLines);
					break;
				case "we":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.NiedersBrem, shortLines);
					break;
				case "wf":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.MecklVorp, shortLines);
					break;
				case "wg":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.NordrWestf, shortLines);
					break;
				case "wh":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.SachsAnh, shortLines);
					break;
				case "wi":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.BrandbBerl, shortLines);
					break;
				case "wj":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.Hess, shortLines);
					break;
				case "wk":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.Thuer, shortLines);
					break;
				case "wl":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.Sachs, shortLines);
					break;
				case "wm":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.RheinlPfSaarl, shortLines);
					break;
				case "wn":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.BadenW, shortLines);
					break;
				case "wo":
					SendWeatherText(DwdServer.DwdTypeEnum.Warnlage, DwdServer.DwdRegionEnum.Bay, shortLines);
					break;
				case "sn":
					SendWeatherText(DwdServer.DwdTypeEnum.SeewarnNord_DE, DwdServer.DwdRegionEnum.Marit, shortLines);
					break;
				case "sm":
					SendWeatherText(DwdServer.DwdTypeEnum.SeewarnSued_DE, DwdServer.DwdRegionEnum.Marit, shortLines);
					break;
				case "sa":
					SendWeatherText(DwdServer.DwdTypeEnum.Alpen, DwdServer.DwdRegionEnum.Alpen, shortLines);
					break;
				default:
					SendAscii("\r\nungueltige auswahl.");
					break;
			}

			WaitAllSendBuffersEmpty(true, "ex");
			if (_inputGegenschreiben)
			{
				SendAscii("\r\nabbruch");
			}

			return;
		}

		private void SendMenu()
		{
			StartInputGegenschreiben();

			SendAscii("\r\n-- menu");
			//SendAscii("\r\ni = inhaltsverzeichnis    x = verbindung trennen");
			SendAscii("\r\nwarnmeldungen:");
			SendAscii("\r\nwa = deutschl.  wb = deutschl. woche");
			SendAscii("\r\nwd = sh/hh      we = nds/bre    wf = mvp     wg = nrw");
			SendAscii("\r\nwh = sachsen-a  wi = bb/berlin  wj = hessen  wk = thuer");
			SendAscii("\r\nwl = sachsen    wm = rp/saar    wn = bw      wo = bayern");
			SendAscii("\r\n");
			SendAscii("\r\nwettermeldungen:");
			SendAscii("\r\nsn = seewetter kueste (nord/ost)  sm = seewetter mittelmeer");
			SendAscii("\r\nsa = alpen/lawinen");
			//SendAscii("\r\n");
			//SendAscii("\r\nsonstiges:");
			//SendAscii("\r\np  = wetterprosa     f = 10-tage-prognose");
			//SendAscii("\r\n--");
			//SendAscii($"\r\n{SHORTLINE_MAX} zeichen breiter text durch voranstellen buchstabe 't':");
			//SendAscii("\r\n(beispiele: tws = bayern,  tsn = seewetter)");
			SendAscii("\r\n--");

			WaitAllSendBuffersEmpty(true, "ex");
			if (_inputGegenschreiben)
			{
				SendAscii("\r\nabbruch");
			}
		}

		private void SendHelpText()
		{
			SendAscii("\r\n");
			string fullName = null;
			string filename = $"weatherhilfe.txt";
			try
			{
				fullName = Path.Combine(FormsHelper.GetExePath(), filename);
				string hilfeText = File.ReadAllText(fullName);
				SendAscii("\r\n");
				SendAscii(hilfeText);
			}
			catch (Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(SendHelpText), $"help file {fullName} is missing", ex);
				SendAscii($"\r\nkeine hilfe vorhanden.");
			}
			WaitAllSendBuffersEmpty();
		}

		private void SendWeatherText(DwdServer.DwdTypeEnum type, DwdServer.DwdRegionEnum region, bool shortLines)
		{
			string text = _dwdServer.GetWeather(type, region);
			if (string.IsNullOrWhiteSpace(text))
			{
				SendAscii("\r\nmeldung nicht gefunden.\r\n");
				DispatchMsg("meldung not found");
				return;
			}

			text = CodeManager.CodePage437ToPlainAscii(text.ToLower());
			TextFormatter tf = new TextFormatter();
			text = tf.FormatText(text, shortLines ? 59 : 68);

			SendAscii("\r\n\n");
			SendAscii(text);
			SendAscii("\r\n");

			DispatchMsg($"{text.Length} chars sent");

			/*
			text = text.Replace("\r", "");
			string[] lines = text.Split(new string[] { "\n" }, StringSplitOptions.None);
			if (lines.Length == 0)
			{
				SendAscii("\r\nmeldung nicht gefunden.");
				return;
			}

			int maxLen = shortLines ? SHORTLINE_MAX : 68;
			bool empty = false;
			SendAscii("\r\n");
			foreach (string line in lines)
			{
				string l = line.TrimEnd();

				// supress special lines
				if (l == "\u0001" || l == "\u0003") continue;

				// suppress multiple empty lines
				if (string.IsNullOrWhiteSpace(l))
				{
					if (empty) continue;
					empty = true;
				}
				else
				{
					empty = false;
				}

				l = CodeManager.AsciiStringReplacments(l, CodeSets.ITA2, false);
				l = SplitLine(l, maxLen);
				SendAscii("\r\n" + l);
			}
			SendAscii("\r\n\r\n");
			*/

		}

		public static string SplitLine(string line, int maxLen)
		{
			/*
			string newLine = "";
			while (true)
			{
				if (line.TrimEnd().Length <= maxLen) break;

				int p = line.LastIndexOf(' ');
				if (p == -1) p = maxLen;
				newLine = line.Substring(p) + newLine;
				line = line.Substring(0, p);
			}
			if (!string.IsNullOrWhiteSpace(newLine)) line += "\r\n";
			return line + newLine.Trim();
			*/

			if (line.TrimEnd().Length <= maxLen) return line.TrimEnd();
			List<string> lines = WrapLine(line, maxLen);
			if (lines.Count > 1) lines[0] += "\r\n";
			return lines[0] + lines[1];
		}

		/// <summary>
		/// Warps one long line to sevaral short lines (<=len), additional spaces are preserved
		/// </summary>
		/// <param name="line"></param>
		/// <param name="len"></param>
		/// <returns></returns>
		private static List<string> WrapLine(string line, int len)
		{
			if (string.IsNullOrEmpty(line) || line.Length <= len)
			{
				return new List<string>() { line };
			}

			List<string> newLines = new List<string>();
			// char lastChar = '\0';
			while (line.Length >= len)
			{
				int pos = -1;
				for (int i = len - 1; i > 0; i--)
				{
					DelimiterItem delim = DelimiterItem.Delimiters.Find(d => d.Char == line[i]);
					if (delim != null)
					{
						DelimiterItem.WrapModeEnum wrapMode = delim.WrapMode;
						if (wrapMode == DelimiterItem.WrapModeEnum.Both)
						{
							char charBefore = line[i - 1];
							wrapMode = charBefore == ' ' ? 
								DelimiterItem.WrapModeEnum.Before : DelimiterItem.WrapModeEnum.After;
						}
						pos = wrapMode == DelimiterItem.WrapModeEnum.Before ? i : i + 1;
						break;
					}
				}
				if (pos == -1) pos = len - 1;

				if (newLines.Count == 0)
				{   // keep the indentation in the first line
					line = line.TrimEnd();
				}
				else
				{
					// no indentation in wrapped lines
					line = line.Trim();
				}
				newLines.Add(line.Substring(0, pos));
				line = line.Substring(pos).Trim();
			}
			if (line.Length > 0) newLines.Add(line);

			return newLines;
		}

		private void Error()
		{
			Disconnect(DisconnectReasons.Error);
			DispatchMsg("disconnected");
			return;
		}

		public void Logoff()
		{
			if (IsConnected)
			{
				SendAscii("\r\n");
				base.Logoff("die verbindung wird getrennt.");
			}
			DispatchMsg("logoff");
		}

		private void WeatherConnection_Received(ItelexConnection connection, string asciiText)
		{
			if (_inputActive)
			{
				lock (_inputLineLock)
				{
					_inputLine += asciiText;
				}
			}
		}
	}

	class DelimiterItem
	{
		public const string DELIMITER = " -?()";
		public static readonly List<DelimiterItem> Delimiters = new List<DelimiterItem>()
		{
			new DelimiterItem(' ', WrapModeEnum.Before),
			new DelimiterItem('-', WrapModeEnum.Both), // WarpMode depends on space before (space before: before, no space before: after)
			new DelimiterItem('+', WrapModeEnum.Before),
			new DelimiterItem('(', WrapModeEnum.Before),
			new DelimiterItem('.', WrapModeEnum.After),
			new DelimiterItem(',', WrapModeEnum.After),
			new DelimiterItem(':', WrapModeEnum.After),
			new DelimiterItem('?', WrapModeEnum.After),
			new DelimiterItem(')', WrapModeEnum.After),
			new DelimiterItem('=', WrapModeEnum.After),
		};

		public enum WrapModeEnum { Before, After, Both };

		public char Char { get; set; }

		public WrapModeEnum WrapMode { get; set; }
		// true = before

		public DelimiterItem(char chr, WrapModeEnum wrapMode)
		{
			Char = chr;
			WrapMode = wrapMode;
		}
	}

}
