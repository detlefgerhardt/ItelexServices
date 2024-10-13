using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ItelexBildlocher
{
	class IncomingConnection : ItelexIncoming, IDisposable
	{
		//public delegate void ConnUpdateEventHandler(IncomingConnection connection);
		//public event ConnUpdateEventHandler ConnUpdate;
		//public delegate void GotAnswerbackEventHandler(IncomingConnection connection);
		//public event GotAnswerbackEventHandler GotAnswerback;

		private EyeballChar _eyeballChar;

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

			_eyeballChar = new EyeballChar();

			_inputActive = false;
			this.ItelexReceived += IncommingConnection_Received;

			InitTimerAndHandler();
		}

		private static ItelexLogger GetLogger(int connectionId, string logPath, LogTypes logLevel)
		{
			return new ItelexLogger(connectionId, ConnectionDirections.In, null, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
		}

		~IncomingConnection()
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
						this.ItelexReceived -= IncommingConnection_Received;
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

			//SendAscii("\r\n");

			//_inputEndChars = new char[] { CodeManager.ASC_LF };

			while (true)
			{
				SendAscii("\r\n\nbitte text eingeben und mit 'zl' (neue zeile) abschliessen.");
				InputResult result = InputString("\r\n", ShiftStates.Ltrs, "", 68, 1);
				SendAscii("\r\n");
				if (result.Timeout || !IsConnected)
				{
					Logoff(null);
					return;
				}

				string text = result.InputString;
				if (string.IsNullOrWhiteSpace(text)) continue;

				while (true)
				{
					result = InputSelection("\r\nok (j/n/pruefen) ?", ShiftStates.Ltrs, "", new string[] { "j", "n", "p", "c" }, 1, 3);

					// pruefen or check
					if (result.InputString == "p" || result.InputString == "c")
					{
						SendAscii($"\r\n\n{text}\r\n");
						continue;
					}
					break;
				}
				if (result.InputString != "j") continue;

				SendAscii("\r\n\n'zl' (neue zeile) druecken - senden beginnt in 5 sekunden.");
				result = InputString("", ShiftStates.Ltrs, "", 1, 1);
				SendAscii("\r\n");
				SendAscii(CodeManager.ASC_LTRS);
				Thread.Sleep(5000);

				string header = new string(CodeManager.ASC_NUL, 15);
				text =  $"{header}{text}{header}";
				byte[] eyeballData = _eyeballChar.GetPunchCodesFromAscii(text);
				SendBaudot(eyeballData);

				WaitAllSendBuffersEmpty(true, null, null);
				Thread.Sleep(5000);
			}
		}

		private void SendHelpText()
		{
			SendAscii("\r\n");
			string fullName = null;
			string filename = $"bildlocherhilfe.txt";
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

		private void Error()
		{
			Disconnect(DisconnectReasons.Error);
			DispatchMsg("disconnected");
			return;
		}

		public void Logoff()
		{
			if (IsConnected) base.Logoff("die verbindung wird getrennt.");
			DispatchMsg("logoff");
			//Debug.WriteLine("Logoff logoff");
		}

		private void IncommingConnection_Received(ItelexConnection connection, string asciiText)
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
