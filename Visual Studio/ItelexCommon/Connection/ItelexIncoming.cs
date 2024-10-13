using ItelexCommon.Logger;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ItelexCommon.Connection
{
	public enum IncomingTexts
	{
		ConfirmOwnNumber,
		EnterValidNumber,
		NotRegistered,
		Deactivated,
		EnterLoginPin,
		WrongPin,
		SendNewLoginPin,
		EnterShortName,
		InvalidShortName,
		ShortnameHelp,
		ConnectionTerminated,
		InternalError,
	}

	public enum IncomingResultTypes
	{
		/// <summary>
		/// result is ok, continue
		/// </summary>
		Ok,
		/// <summary>
		/// result is not ok, terminate connection
		/// </summary>
		NotOk,
		/// <summary>
		/// terminate connection because of sending pin or other action
		/// </summary>
		End,
		/// <summary>
		/// internal error
		/// </summary>
		Error,
	}

	public class ItelexIncoming: ItelexConnection
	{
		protected string TAG = nameof(ItelexIncoming);

		public delegate void IncomingLoggedInEventHandler(ItelexIncoming connection);
		public event IncomingLoggedInEventHandler IncomingLoggedIn;

		public delegate void GotAnswerbackEventHandler(ItelexIncoming connection);
		public event GotAnswerbackEventHandler IncomingGotAnswerback;

		private ItelexIncomingConfiguration _incomingConfiguration;

		public string CallerStringForMailNotification
		{
			get
			{
				if (RemoteAnswerback != null && RemoteNumber.HasValue && RemoteAnswerback.NumberInt != RemoteNumber.Value)
				{
					return $"[{RemoteNumber}] [{RemoteAnswerback}]";
				}
				else if (RemoteAnswerback != null)
				{
					return $"[{RemoteAnswerback}]";
				}
				else if (RemoteNumber.HasValue)
				{
					return $"[{RemoteNumber}]";
				}
				return "";
			}
		}



		/// <summary>
		/// Active extension configuration, depending on incoming extension number
		/// </summary>
		protected ItelexExtensionConfiguration _extension;

		public ItelexIncoming()
		{
		}

		public ItelexIncoming(TcpClient client, int connectionId, ItelexLogger itelexLogger): 
			base(ConnectionDirections.In, client, connectionId, null, null, itelexLogger)
		{
		}

		public virtual void Start()
		{
		}

		public bool StartIncoming(ItelexIncomingConfiguration config)
		{
			Log(LogTypes.Notice, nameof(StartIncoming), $"Incoming connection from {RemoteClientAddrStr}");

			try
			{
				_incomingConfiguration = config;

				_inputEndChars = new char[] { CodeManager.ASC_LF };

				ConnectIn(config);
				if (!IsConnected || ConnectionState == ConnectionStates.TcpConnected)
				{
					//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: !IsConnected");
					Log(LogTypes.Notice, nameof(StartIncoming), $"not connected after ConnectIn");
					return false;
				}

				Log(LogTypes.Debug, nameof(StartIncoming), $"ConnectIn successful Extension={ExtensionNumber}");
				//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: ExtensionNumber={ExtensionNumber}");

				if (ExtensionNumber.HasValue && ExtensionNumber != 0)
				{
					_extension = _incomingConfiguration.ItelexExtensions
						.Where(e => e.ExtensionNumber == ExtensionNumber)
						.FirstOrDefault();
				}
				else
				{
					_extension = _incomingConfiguration.ItelexExtensions
						.Where(e => e.ExtensionNumber == null)
						.FirstOrDefault();
					//_extension.ServiceName = "Minitelex";
					ExtensionNumber = LocalPort;
				}
				Log(LogTypes.Debug, nameof(StartIncoming), $"_extension=({_extension})");
				//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: _extension={_extension}");

				if (_extension == null)
				{
					// no valid extension number
					//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: Reject NA");
					RejectReason = "NA";
					CallStatus = CallStatusEnum.Reject;
					Log(LogTypes.Notice, nameof(StartIncoming), $"invalid extension ({ExtensionNumber}) -> reject na");
					SendRejectCmd(RejectReason);
					InvokeItelexUpdate();
					return false;
				}
				ConnectionLanguage = LanguageManager.Instance.GetLanguageByShortname(_extension.Language);

				//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: ConnectionLanguage={ConnectionLanguage}");
				if (!IsConnected)
				{
					//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: logoff");
					Logoff(null);
					return false;
				}

				Log(LogTypes.Debug, nameof(StartIncoming), "Send 7xBU");
				//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: SendAscii(7xBU)");
				SendAscii(CodeManager.ASC_LTRS, 7);

				if (_incomingConfiguration.LoginSequences.Contains(ExtensionNumber ?? ExtensionNumber.Value, LoginSeqTypes.SendTime))
				{
					//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: SendAscii(Date/Time)");
					Log(LogTypes.Debug, nameof(StartIncoming), "Send date+time");
					SendAscii($"\r\n{DateTime.Now:dd.MM.yyyy HH:mm}");
				}

				if (!string.IsNullOrEmpty(_extension.ServiceName))
				{
					Log(LogTypes.Debug, nameof(StartIncoming), "Send servicename+version");
					SendAscii($"\r\n\n{_extension.ServiceName} V{_incomingConfiguration.OurPrgmVersionStr}");
				}

				IncomingResultTypes resultType = IncomingResultTypes.Ok;
				string answerback = string.IsNullOrEmpty(config.OverwriteAnswerbackStr) ?
														_extension.ServiceAnswerback :
														config.OverwriteAnswerbackStr;
				if (_incomingConfiguration.LoginSequences.Contains(ExtensionNumber.Value, LoginSeqTypes.SendKg) &&
					answerback != null)
				{
					//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: SendAscii(KG)");
					Log(LogTypes.Debug, nameof(StartIncoming), $"Send answerback {answerback}");
					SendAscii($"\r\n\n{answerback}");
				}

				bool setNumber = false;
				if (_incomingConfiguration.LoginSequences.Contains(ExtensionNumber.Value, LoginSeqTypes.GetKg))
				{
					//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: GetKG");
					resultType = GetKg();
					Log(LogTypes.Debug, nameof(StartIncoming), $"GetKg result={resultType} kg={RemoteAnswerback} number={RemoteNumber}");
					setNumber = true;
					InvokeItelexUpdate();
				}
				if (_incomingConfiguration.LoginSequences.Contains(ExtensionNumber.Value, LoginSeqTypes.GetNumber))
				{
					resultType = GetNumber();
					Log(LogTypes.Debug, nameof(StartIncoming), $"GetNumber result={resultType} number={RemoteNumber}");
					if (resultType != IncomingResultTypes.Ok)
					{
						SendAscii("\r\n");
						Logoff($"{GetLngText(IncomingTexts.ConnectionTerminated)}");
						InvokeItelexUpdate();
						return false;
					}
					setNumber = true;
					InvokeItelexUpdate();
				}
				else if (_incomingConfiguration.LoginSequences.Contains(ExtensionNumber.Value, LoginSeqTypes.GetNumberAndPin))
				{
					resultType = GetNumberAndPin();
					Log(LogTypes.Debug, nameof(StartIncoming), $"GetNumberAndPin result={resultType} number={RemoteNumber}");
					if (resultType != IncomingResultTypes.Ok)
					{
						SendAscii("\r\n");
						Logoff($"{GetLngText(IncomingTexts.ConnectionTerminated)}");
						InvokeItelexUpdate();
						return false;
					}
					setNumber = true;
					InvokeItelexUpdate();
				}

				if (_incomingConfiguration.LoginSequences.Contains(ExtensionNumber.Value, LoginSeqTypes.GetShortName))
				{
					resultType = GetShortName();
					Log(LogTypes.Debug, nameof(StartIncoming), $"GetShortName result={resultType} shortname={ConnectionShortName}");
					if (resultType != IncomingResultTypes.Ok)
					{
						SendAscii("\r\n");
						Logoff($"{GetLngText(IncomingTexts.ConnectionTerminated)}");
						InvokeItelexUpdate();
						return false;
					}
					//RemoteNumber = RemoteAnswerback.NumberInt;
					setNumber = true;
					InvokeItelexUpdate();
				}

				if (setNumber)
				{
					SetNumber();
				}

				Log(LogTypes.Notice, nameof(StartIncoming), $"login Answerback='{RemoteAnswerbackStr}' Number={RemoteNumber}");
				//string caller = "";
				//if (RemoteAnswerback != null && RemoteNumber.HasValue && RemoteAnswerback.NumberInt != RemoteNumber.Value)
				//{
				//	caller = $"[{RemoteNumber}] [{RemoteAnswerback}]";
				//}
				//else if (RemoteAnswerback != null)
				//{
				//	caller = $"[{RemoteAnswerback}]";
				//}
				//else if (RemoteNumber.HasValue)
				//{
				//	caller = $"[{RemoteNumber}]";
				//}

				if (!IsConnected)
				{
					Log(LogTypes.Notice, nameof(StartIncoming), $"not connected before SendMail");
					return false;
				}

				CommonHelper.SendMail(
					$"login {_extension.ServiceName} {_extension.ItelexNumber} {ExtensionNumber} {CallerStringForMailNotification}",
					null);

				IncomingLoggedIn?.Invoke(this);
				InvokeItelexUpdate();

				return true;
			}
			catch (Exception ex)
			{
				//MessageDispatcher.Instance.Dispatch(ConnectionId, $"{TAG} StartIncoming: login error");
				Log(LogTypes.Error, nameof(StartIncoming), "login error", ex);
				return false;
			}
		}

		private void SetNumber()
		{
			if (RemoteNumber.HasValue)
			{
				_itelexLogger.SetNumber(ConnectionId, RemoteNumber.Value);
				Log(LogTypes.Debug, nameof(SetNumber), $"set itelex-number to {RemoteNumber}");
			}
		}

		/// <summary>
		/// get answerback. No verification, answerback can be empty
		/// </summary>
		/// <returns></returns>
		private IncomingResultTypes GetKg()
		{
			WaitAllSendBuffersEmpty(); // neu
			for (int cnt = 0; cnt < 2; cnt++)
			{
				Log(LogTypes.Debug, nameof(GetKg), $"start {cnt}");

				if (!IsConnected) return IncomingResultTypes.NotOk;

				string kgStr = GetAnswerback();
				if (kgStr.Length > 4)
				{
					RemoteAnswerback = new Answerback(kgStr);
					RemoteNumber = RemoteAnswerback.NumberInt;
					IncomingGotAnswerback?.Invoke(this);
					SendAscii("\r\n");
					Log(LogTypes.Debug, nameof(GetKg), $"answerback={RemoteAnswerback}");
					return IncomingResultTypes.Ok;
				}
			}
			SendAscii("\r\n");
			return IncomingResultTypes.NotOk;
		}

		/// <summary>
		/// Get answerback and itelexnumber. With verification of itelexnumber
		/// </summary>
		/// <returns></returns>
		private IncomingResultTypes GetNumber()
		{
			SubscriberServer subscribeServer = new SubscriberServer();

			// --- enter number (3 retries) ---

			for (int cnt = 0; cnt < 3; cnt++)
			{
				if (!IsConnected) return IncomingResultTypes.NotOk;

				string numStr = RemoteAnswerback != null ? RemoteAnswerback.NumberStr : "";
				InputResult inputResult =
						InputString($"\r\n{GetLngText(IncomingTexts.ConfirmOwnNumber)}:", ShiftStates.Figs,
						numStr, 30, 1);
				bool invalidNumber = false;
				int number = 0;
				if (inputResult == null || inputResult.ErrorOrTimeoutOrDisconnected)
				{
					invalidNumber = true;
				}
				if (!invalidNumber && !int.TryParse(inputResult.InputString, out number))
				{
					invalidNumber = true;
				}
				if (!invalidNumber && !subscribeServer.CheckNumberIsValid(number))
				{
					invalidNumber = true;
				}
				if (invalidNumber)
				{
					Log(LogTypes.Notice, nameof(GetNumber), $"invalid number {inputResult.InputString}");
					SendAscii($"\r\n{GetLngText(IncomingTexts.EnterValidNumber)}");
					continue;
				}

				/// number is valid
				RemoteNumber = number;
				Log(LogTypes.Notice, nameof(GetNumber), $"number={RemoteNumber}");
				return IncomingResultTypes.Ok;
			}
			return IncomingResultTypes.NotOk;
		}

		/// <summary>
		/// get answerback, itelexnumber and pin. With verification of itelexnumber and pin
		/// </summary>
		/// <returns></returns>
		private IncomingResultTypes GetNumberAndPin()
		{
			SubscriberServer subscribeServer = new SubscriberServer();

			// --- enter number and pin (3 retries) ---

			for (int cnt = 0; cnt < 3; cnt++)
			{
				if (!IsConnected) return IncomingResultTypes.NotOk;

				// --- enter number ---

				string numStr = RemoteAnswerback != null ? RemoteAnswerback.NumberStr : "";
				InputResult inputResult =
						InputString($"\r\n{GetLngText(IncomingTexts.ConfirmOwnNumber)}:", ShiftStates.Figs,
						numStr, 30, 1);
				bool invalidNumber = false;
				int number = 0;
				if (inputResult == null || inputResult.ErrorOrTimeoutOrDisconnected)
				{
					invalidNumber = true;
				}
				if (!invalidNumber && !int.TryParse(inputResult.InputString, out number))
				{
					invalidNumber = true;
				}
				if (!invalidNumber && !subscribeServer.CheckNumberIsValid(number))
				{
					invalidNumber = true;
				}
				if (invalidNumber)
				{
					Log(LogTypes.Notice, nameof(GetNumberAndPin), $"invalid number {inputResult.InputString}");
					SendAscii(CodeManager.ASC_COND_NL);
					SendAscii($"{GetLngText(IncomingTexts.EnterValidNumber)}");
					continue;
				}

				// number valid

				RemoteNumber = number;
				Log(LogTypes.Debug, nameof(GetNumberAndPin), $"number={RemoteNumber}");

				ILoginItem loginItem = _incomingConfiguration.LoadLoginItem(RemoteNumber.Value);
				if (loginItem == null)
				{
					// --- new number ---

					string msg = GetLngText(IncomingTexts.NotRegistered, new string[] { RemoteNumber.ToString() });
					InputResult isNewInput = InputYesNo($"\r\n{msg} ?", null, 3);
					if (isNewInput.ErrorOrTimeoutOrDisconnected || isNewInput.InputBool == false) continue;

					// --- subscribe new number ---

					//RemoteNumber = number;
					_incomingConfiguration.AddAccount(RemoteNumber.Value);

					return IncomingResultTypes.End;
				}

				// --- number exists, enter pin (3 retries)---
				if (!loginItem.Active)
				{
					SendAscii($"\r\n{GetLngText(IncomingTexts.Deactivated)}");
					return IncomingResultTypes.End;
				}

				RemoteNumber = number;

				for (int i = 0; i < 3; i++)
				{
					if (!IsConnected) return IncomingResultTypes.NotOk;

					SendAscii("\r\n");
					InputResult passwordInput = InputPin($"{GetLngText(IncomingTexts.EnterLoginPin)}:", 5);
					if (passwordInput.ErrorOrTimeoutOrDisconnected || string.IsNullOrWhiteSpace(passwordInput.InputString)) continue;

					if (passwordInput.InputString == loginItem.Pin)
					{
						// --- login successfull ---
						Log(LogTypes.Debug, nameof(GetNumberAndPin), "pin ok");
						return IncomingResultTypes.Ok;
					}
					Log(LogTypes.Notice, nameof(GetNumberAndPin), $"invalid pin {inputResult.InputString}");
					SendAscii($"{GetLngText(IncomingTexts.WrongPin)}");

					inputResult = InputYesNo($"\r\n{GetLngText(IncomingTexts.SendNewLoginPin)} ?", null, 3);
					if (inputResult.ErrorOrTimeoutOrDisconnected || inputResult.InputBool == false) continue;

					_incomingConfiguration.SendNewPin(RemoteNumber.Value);
					return IncomingResultTypes.End;
				}

				// --- failed to enter correct pin, retry number ---
			}

			// --- failed to enter valid number and pin ---

			return IncomingResultTypes.NotOk;
		}

		private IncomingResultTypes GetShortName()
		{
			int cnt = 0;
			while (cnt < 4)
			{
				InputResult shortNameResult = InputString($"\r\n{GetLngText(IncomingTexts.EnterShortName)}", ShiftStates.Ltrs, null, 10, 3);
				Log(LogTypes.Debug, nameof(Start), $"id={ConnectionId} i={cnt}, shortNameInput={shortNameResult}");
				Thread.Sleep(500);
				if (shortNameResult == null || shortNameResult.ErrorOrTimeoutOrDisconnected)
				{
					// error
					return IncomingResultTypes.NotOk;
				}
				if (shortNameResult.IsHelp)
				{
					// help
					SendAscii($"{GetLngText(IncomingTexts.ShortnameHelp)}\r\n");
					continue;
				}
				string shortName = shortNameResult.InputString.Trim();
				if (string.IsNullOrEmpty(shortName))
				{
					// input is empty
					cnt++;
					continue;
				}
				if (!_incomingConfiguration.CheckShortNameIsValid(shortName))
				{
					// invalid shortname
					Log(LogTypes.Notice, nameof(GetNumber), $"invalid shortname {shortNameResult.InputString}");
					SendAscii(CodeManager.ASC_COND_NL);
					SendAscii($"{GetLngText(IncomingTexts.InvalidShortName)}\r\n");
					cnt++;
					continue;
				}

				ConnectionShortName = shortName;

				// input ok
				return IncomingResultTypes.Ok;
			}

			return IncomingResultTypes.NotOk;
		}

		//protected static bool IsValidPin(string pin)
		//{
		//	if (pin == null || pin.Length != 4) return false;
		//	return pin.ExtContainsOnlyDigits();
		//}

		private string GetLngText(IncomingTexts incomingText, string[] prms = null)
		{
			if (!_incomingConfiguration.LngKeyMapper.ContainsKey(incomingText)) return "";
			int lngKey = _incomingConfiguration.LngKeyMapper[incomingText];
			return _incomingConfiguration.GetLngText(lngKey, prms);
		}

		protected void DispatchMsg(string msg)
		{
			MessageDispatcher.Instance.Dispatch(ConnectionId, RemoteNumber, msg);
			//Log(LogTypes.Notice, nameof(DispatchMsg), msg);
		}

	}
}
