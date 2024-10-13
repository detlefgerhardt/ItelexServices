using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.ChatGpt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ItelexChatGptServer.Languages;

namespace ItelexChatGptServer
{

	class IncomingConnection : ItelexIncoming
	{
		private const int SHORTLINE_MAX = 59;

		private readonly ChatGptAbstract _chatGpt;

		private float _temperature = 0.5f;
		private float _top_p = 0.0f;

		public IncomingConnection()
		{
		}

		public IncomingConnection(TcpClient client, int idNumber, string logPath, LogTypes logLevel) : 
				base(client, idNumber, GetLogger(idNumber, logPath, logLevel))
		{
			TAG = nameof(IncomingConnection);
			_chatGpt = new ChatGptWetstone();
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
						//this.Received -= ChatGptConnection_Received;
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
			Dispose(false);
		}

		public override void Start()
		{
			try
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

				SendAscii($"\r\n{LngText(LngKeys.EnterMultiLine)}\r\n");

				bool end = false;

				while (!end && IsConnected)
				{
					try
					{
						DoInput(ref end);
					}
					catch (Exception ex)
					{
						_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Start), "error", ex);
					};
				}

				Logoff();
			}
			catch (Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Start), $"error", ex);
			}
		}

		private void DoInput(ref bool end)
		{
			SendAscii($"\r\n{LngText(LngKeys.QuestionToChatGpt)}:\r\n");
			InputResult inputResult = InputMultiLine();
			if (inputResult == null) return;
			if (inputResult.Timeout)
			{
				end = true;
				return;
			}
			if (inputResult.ErrorOrTimeoutOrDisconnected) return;

			string inputStr = inputResult.InputString;
			if (string.IsNullOrEmpty(inputStr)) return;

			if (inputStr.Contains("/"))
			{
				int p = inputStr.IndexOf("/");
				inputStr = inputStr.Substring(p + 1, inputStr.Length - p - 1);
			}

			inputStr = inputStr.Replace("\r", " ");
			inputStr = inputStr.Replace("\n", " ");
			inputStr = inputStr.Replace("  ", " ").Trim();

			string msg = $"input: {inputStr}";
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(DoInput), msg);
			DispatchMsg(msg);

			if (string.IsNullOrWhiteSpace(inputStr)) return;

			if (CheckSettings(inputStr)) return;

			Task<string> response = _chatGpt.Request(inputStr, _temperature, _top_p);
			response.Wait();
			string responseStr = _chatGpt.ConvMsgText(response.Result);

			//string responseStr = "response";

			msg = $"response: {responseStr}";
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(DoInput), msg);

			responseStr = _chatGpt.FormatMsg(responseStr);

			DispatchMsg($"Response: {responseStr.Length} chars");
			Log(LogTypes.Debug, nameof(DoInput), $"Respone: {responseStr.Length} chars");
			string[] lines = responseStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach(string line in lines)
			{
				DispatchMsg($"Response: {line}");
				Log(LogTypes.Debug, nameof(DoInput), $"Respone: {line}");
			}

			StartInputGegenschreiben();
			SendAscii($"\r\n{responseStr}\r\n");

			WaitAllSendBuffersEmpty(true, "ex");
			if (_inputGegenschreiben)
			{
				SendAscii($"\r\n{LngText(LngKeys.Aborted)}");
			}

			return;
		}

		private bool CheckSettings(string inputStr)
		{
			string[] parts = inputStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 1) return false;

			if (parts[0].Length < 3) return false;
			if (parts[0] != "settings".Substring(0, parts[0].Length)) return false;

			SendAscii($"\r\nmodel: {_chatGpt.GetModel()}\r\n");

			for (int i = 0; i >= 0; i++)
			{
				if (i > 3) return false;
				float? value = InputFloat("temperature", _temperature);
				if (value == null) continue;
				if (value < 0 || value > 1.4) continue;
				_temperature = value.Value;
				break;
			}

			for (int i = 0; i >= 0; i++)
			{
				if (i > 3) return false;
				float? value = InputFloat("top-p", _top_p);
				if (value == null) continue;
				if (value < 0 || value > 1) continue;
				_top_p = value.Value;
				break;
			}
			//SendAscii("\r\n");
			return true;
		}

		private float? InputFloat(string text, float inpVal)
		{
			InputResult inputResult = InputString($"\r{text}:", ShiftStates.Figs,
					inpVal.ToString("0.0", CultureInfo.InvariantCulture), 5, 1, false, false);
			if (inputResult.ErrorOrTimeoutOrDisconnected || string.IsNullOrEmpty(inputResult.InputString)) return null;
			if (!float.TryParse(inputResult.InputString, NumberStyles.Any, CultureInfo.InvariantCulture, out float val)) return null;
			return val;
		}

		public void Logoff()
		{
			SendAscii("\r\n");
			if (IsConnected) base.Logoff(LngText(LngKeys.Disconnect));
			DispatchMsg("logoff");
			Debug.WriteLine("Logoff logoff");
		}

		public string LngText(LngKeys lngKey)
		{
			return LanguageManager.Instance.GetText((int)lngKey, ConnectionLanguage.Id);
		}

		public string LngText(LngKeys lngKey, params string[] param)
		{
			return LanguageManager.Instance.GetText((int)lngKey, ConnectionLanguage.Id, param);
		}

	}
}
