using ItelexCommon;
using ItelexCommon.Connection;
using ItelexMsgServer.Data;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using ItelexMsgServer.Languages;
using ItelexMsgServer.Commands;
using ItelexCommon.Commands;
using ItelexCommon.Utility;
using System.Linq;
using System.IO;
using ItelexMsgServer.Fax;
using ItelexMsgServer.Mail;
using ItelexCommon.Logger;

namespace ItelexMsgServer.Connections
{
	class IncomingConnection : ItelexIncoming
	{
		private const string INPUT_PROMPT = null;

		private MsgServerDatabase _database;

		private SubscriberServer _subcriberServer;

		private CommandInterpreter _commandInterpreter;

		//private Random _random = new Random();

		public UserItem SessionUser { get; set; }

		public DateTime SessionStartTime { get; set; }

		public IncomingConnection()
		{
		}

		public IncomingConnection(TcpClient client, int idNumber, string logPath, LogTypes logLevel) : 
			base(client, idNumber, GetLogger(idNumber, logPath, logLevel))
		{
			TAG = nameof(IncomingConnection);

			_database = MsgServerDatabase.Instance;
			_subcriberServer = new SubscriberServer();
			_commandInterpreter = new CommandInterpreter();

			_inputActive = false;
			this.ItelexReceived += IncomingConnection_Received;
			this.BaudotSendRecv += IncomingConnection_ReceivedBaudot;

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
			}

			_disposed = true;
		}

		#endregion Dispose

		private static ItelexLogger GetLogger(int connectionId, string logPath, LogTypes logLevel)
		{
			return new ItelexLogger(connectionId, ConnectionDirections.In, null, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
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
			//_inputEndChars = new char[] { CodeManager.ASC_LF };

			//if (RemoteExtensionNo == 13)
			//{
			//	IncomingSendMail();
			//	return;
			//}
			//else if (RemoteExtensionNo == 14)
			//{
			//	IncomingSendFax();
			//	return;
			//}

			ItelexIncomingConfiguration config = new ItelexIncomingConfiguration
			{
				OurPrgmVersionStr = Helper.GetVersionCode(),
				OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
				//LoginType = new LoginTypes[] { LoginTypes.SendTime, LoginTypes.SendKg, LoginTypes.GetKg, LoginTypes.GetNumberAndPin },
				LoginSequences = new AllLoginSequences(
					new LoginSequenceForExtensions(null, LoginSeqTypes.SendTime, LoginSeqTypes.SendKg, LoginSeqTypes.GetKg, LoginSeqTypes.GetNumberAndPin)
				),
				LoadLoginItem = LoginItemLoad,
				UpdateLoginItem = LoginItemUpdate,
				AddAccount = LoginItemAddAccount,
				SendNewPin = LoginItemSendNewPin,
				ItelexExtensions = GlobalData.Instance.ItelexValidExtensions,
				GetLngText = GetLngText,
				LngKeyMapper = new Dictionary<IncomingTexts, int>()
				{
					{ IncomingTexts.ConfirmOwnNumber, (int)LngKeys.ConfirmOwnNumber},
					{ IncomingTexts.EnterValidNumber, (int)LngKeys.EnterValidNumber},
					{ IncomingTexts.NotRegistered, (int)LngKeys.NotRegistered},
					{ IncomingTexts.Deactivated, (int)LngKeys.Deactivated},
					{ IncomingTexts.EnterLoginPin, (int)LngKeys.EnterLoginPin},
					{ IncomingTexts.WrongPin, (int)LngKeys.WrongPin},
					{ IncomingTexts.SendNewLoginPin, (int)LngKeys.SendNewLoginPin},
					{ IncomingTexts.ConnectionTerminated, (int)LngKeys.ConnectionTerminated},
					{ IncomingTexts.InternalError, (int)LngKeys.InternalError},
				}
			};
			if (!StartIncoming(config) || !IsConnected)
			{
				Logoff(null);
				return;
			}

			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Start), $"id={ConnectionId} Start");

			if (!RemoteNumber.HasValue)
			{
				Logoff(null);
				return;
			}

			//ConnectionLanguage = LanguageManager.Instance.GetLanguageByShortname(_extension.Language);
			SessionUser = _database.UserLoadByTelexNumber(RemoteNumber.Value);

			ConfirmationItem confItem = _database.ConfirmationsLoadByType(SessionUser.UserId, ConfirmationTypes.NewPin);
			if (confItem != null && (!confItem.Finished || !SessionUser.Activated))
			{
				// confirm login
				SendAscii($"\r\n{LngText(LngKeys.NewAccountActivated)}");
				SendAscii($"\r\n{LngText(LngKeys.NewAccountTimezoneInfo)}");
				int? timezone = InputTimezone($"\r\n{LngText(LngKeys.NewAccountEnterTimezone)}", SessionUser.Timezone);
				if (!IsConnected)
				{
					Logoff(null);
					return;
				}
				if (timezone != null)
				{
					SessionUser.Timezone = timezone.Value;
				}

				confItem.Finished = true;
				bool success = _database.ConfirmationsUpdate(confItem);
				if (!success)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Start), $"error confirming account {SessionUser.ItelexNumber}");
					Logoff(LngText(LngKeys.InternalError));
				}

				SessionUser.Activated = true;
				SessionUser.LastLoginTimeUtc = DateTime.UtcNow;
				if (!string.IsNullOrEmpty(confItem.AnswerBack))
				{
					SessionUser.Kennung = confItem.AnswerBack;
				}
				success = _database.UserUpdate(SessionUser);
				if (!success)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Start), $"error activating account {SessionUser.ItelexNumber}");
					Logoff(LngText(LngKeys.InternalError));
				}
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(Start), $"account {SessionUser.ItelexNumber} confirmed and activated");
			}
			else
			{
				SessionUser.LastLoginTimeUtc = DateTime.UtcNow;
				_database.UserUpdate(SessionUser);
			}

			InterpreterLoop();

			SendAscii("\r\n");
			Logoff($"{LngText(LngKeys.ConnectionTerminated)}\r\n");

			DispatchMsgAndLog("Logoff");
		}

		private UserItem LoginItemLoad(int itelexNumber)
		{
			return _database.UserLoadByTelexNumber(itelexNumber);
		}

		private bool LoginItemUpdate(ILoginItem userItem)
		{
			return _database.UserUpdate((UserItem)userItem);
		}

		private bool LoginItemAddAccount(int itelexNumber)
		{
			DispatchMsgAndLog($"Create new account {itelexNumber}");

			// add new number
			DateTime utcNow = DateTime.UtcNow;
			UserItem userItem = new UserItem()
			{
				ItelexNumber = itelexNumber,
				Pin = CommonHelper.CreatePin(),
				RegisterTimeUtc = utcNow,
				LastLoginTimeUtc = utcNow,
				LastPinChangeTimeUtc = utcNow,
				Kennung = RemoteAnswerbackStr,
				Timezone = 2,
				Paused = false,
				MaxPendMails = Constants.MAX_PENDING_MAILS,
				MaxMailsPerDay = Constants.MAX_MAILS_PER_DAY,
				MaxLinesPerDay = Constants.MAX_LINES_PER_DAY,
				ShowSender = true,
				Active = true,
				AllowRecvMails = true,
				AllowRecvTelegram = true,
				Activated = false,
			};
			_database.UserInsert(userItem);

			ConfirmationItem confItem = new ConfirmationItem
			{
				UserId = userItem.UserId,
				Number = itelexNumber,
				Pin = userItem.Pin,
				SendRetries = 0,
				Language = ConnectionLanguage.ShortName,
				CreateTimeUtc = utcNow,
				Type = (int)ConfirmationTypes.NewPin
			};
			_database.ConfirmationsInsert(confItem);

			SendAscii($"\r\n{LngText(LngKeys.NewAccountCreated)}");
			SendNewPinMsg(itelexNumber);
			return true;
		}

		private bool LoginItemSendNewPin(int iTelexNumber)
		{
			UserItem userItem = _database.UserLoadByTelexNumber(iTelexNumber);

			ConfirmationItem confItem = new ConfirmationItem
			{
				UserId = userItem.UserId,
				Type = (int)ConfirmationTypes.NewPin,
				Number = RemoteNumber.Value,
				Pin = CommonHelper.CreatePin(),
				Language = ConnectionLanguage.ShortName,
				CreateTimeUtc = DateTime.UtcNow,
			};
			_database.ConfirmationsInsert(confItem);

			userItem.Pin = confItem.Pin;
			if (RemoteAnswerback != null)
			{
				userItem.Kennung = RemoteAnswerback.Name;
			}
			_database.UserUpdate(userItem);

			SendNewPinMsg(RemoteNumber.Value);
			return true;
		}

		private void InterpreterLoop()
		{
			bool first = true;
			while (IsConnected)
			{
				if (!first)
				{
					SendAscii("\n");
				}
				first = false;
				InputResult inputResult = InputString($"\r\n{LngText(LngKeys.CmdPrompt)}:", ShiftStates.Ltrs, null, 30, 1);
				if (!IsConnected || inputResult.Timeout) return;

				DispatchMsgAndLog($"Cmd: {inputResult.InputString}");
				List<CmdTokenItem> tokens = _commandInterpreter.Parse(inputResult.InputString);
				if (tokens == null)
				{
					ShowInvalidCmd();
					continue;
				}

				try
				{
					bool ok = false;
					switch (tokens[0].TokenType)
					{
						case TokenTypes.Help:
							ok = CmdHelp(tokens);
							break;
						case TokenTypes.Set:
							ok = CmdSettings(tokens);
							break;
						case TokenTypes.Send:
							ok = CmdSend(tokens);
							break;
						case TokenTypes.List:
							ok = CmdListPruefTexts(tokens);
							break;
						case TokenTypes.Pause:
							ok = CmdSetPause(tokens);
							break;
						case TokenTypes.Hours:
							ok = CmdSetHours(tokens);
							break;
						case TokenTypes.Allowed:
							ok = CmdSetAllowed(tokens);
							break;
						case TokenTypes.EventCode:
							ok = CmdSetEventCode(tokens);
							break;
						case TokenTypes.Show:
							ok = CmdSetShowSender(tokens);
							break;
						case TokenTypes.End:
							return;
						case TokenTypes.Timezone:
							ok = CmdSetTimezone(tokens);
							break;
						case TokenTypes.Pin:
							ok = CmdSetNewPin(tokens);
							break;
					}
					if (!ok)
					{
						ShowInvalidCmd();
					}
				}
				catch (Exception ex)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(InterpreterLoop), "error", ex);
					SendAscii($"\r\n{LngText(LngKeys.InternalError)}");
				}
			}
		}

		private void ShowInvalidCmd()
		{
			SendAscii($"\r\n{LngText(LngKeys.InvalidCommand)}");
		}

		private void ShowCommandNotYetSupported()
		{
			SendAscii($"\r\n{LngText(LngKeys.CommandNotYetSupported)}");
		}

		private bool CmdSetTimezone(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;

			if (tokens[1].TokenType == TokenTypes.ArgInt)
			{
				int? timezone = tokens[1].GetNumericValue();
				if (timezone == null) return false;
				if (timezone < -12 || timezone > 14)
				{
					SendAscii($"\r\n{LngText(LngKeys.InvalidTimezone)}");
					return true;
				}

				DispatchMsgAndLog($"cmd timezone {timezone}");
				UserItem userItem = _database.UserLoadById(SessionUser.UserId);
				userItem.Timezone = timezone.Value;
				_database.UserUpdate(userItem);
				SessionUser = userItem;

				string msg = LngText(LngKeys.ActualTimezone, timezone.Value.ToString());
				SendAscii($"\r\n{msg}");
				return true;
			}
			return false;
		}

		private bool CmdSetPause(List<CmdTokenItem> tokens)
		{
			if (tokens.Count == 1)
			{
				// pause on
				UserItem userItem = _database.UserLoadById(SessionUser.UserId);
				userItem.Paused = true;
				_database.UserUpdate(userItem);
				SessionUser = userItem;
				SendAscii($"\r\n{LngText(LngKeys.PauseActive)}");
				DispatchMsgAndLog("cmd pause activated");
				return true;
			}
			else if (tokens[1].TokenType == TokenTypes.Off)
			{
				// pause off
				UserItem userItem = _database.UserLoadById(SessionUser.UserId);
				userItem.Paused = false;
				_database.UserUpdate(userItem);
				SessionUser = userItem;
				SendAscii($"\r\n{LngText(LngKeys.PauseInactive)}");
				DispatchMsgAndLog("cmd pause deactivated");
				return true;
			}
			return false;
		}

		private bool CmdSetHours(List<CmdTokenItem> tokens)
		{
			int? from = null;
			int? to = null;
			if (tokens.Count == 3)
			{
				// set hours
				from = tokens[1].GetNumericValue();
				to = tokens[2].GetNumericValue();
				if (from == null || to == null) return false;
				UserItem userItem = _database.UserLoadById(SessionUser.UserId);
				userItem.SendFromHour = from;
				userItem.SendToHour = to;
				_database.UserUpdate(userItem);
				SessionUser = userItem;
			}
			else if (tokens.Count == 2 && tokens[1].TokenType == TokenTypes.Off)
			{
				from = 0;
				to = 24;
				UserItem userItem = _database.UserLoadById(SessionUser.UserId);
				userItem.SendFromHour = null;
				userItem.SendToHour = null;
				_database.UserUpdate(userItem);
				SessionUser = userItem;
			}

			string msg = LngText(LngKeys.SendingTimeFromTo, from.Value.ToString(), to.Value.ToString());
			SendAscii($"\r\n{msg}");
			DispatchMsg(msg);

			return true;
		}

		private bool CmdSetAllowed(List<CmdTokenItem> tokens)
		{
			if (tokens.Count < 2) return false;
			if (tokens[1].TokenType == TokenTypes.Send) return CmdSetAllowedSender(tokens);
			if (tokens[1].TokenType == TokenTypes.Mails) return CmdSetAllowMailOrTelegram(tokens);
			if (tokens[1].TokenType == TokenTypes.Telegram) return CmdSetAllowMailOrTelegram(tokens);
			return false;
		}

		private bool CmdSetAllowedSender(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 3) return false;
			if (tokens[1].TokenType != TokenTypes.Send) return false;

			string sender = tokens[2].GetStringValue().ToLower();
			if (sender == "off" || sender == "aus")
			{
				sender = null;
			}
			else
			{
				if (!sender.Contains("(at)"))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidMailAdress));
					return true;
				}
			}

			UserItem userItem = _database.UserLoadById(SessionUser.UserId);
			userItem.AllowedSender = sender;
			_database.UserUpdate(userItem);
			SessionUser = userItem;
			if (sender != null)
			{
				DispatchMsgAndLog($"set allowed sender to {sender}");
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.AllowedSender, sender));
			}
			else
			{
				DispatchMsgAndLog("set allowed sender off");
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.AllowedSenderOff));
			}
			return true;
		}

		private bool CmdSetAllowMailOrTelegram(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 3) return false;

			TokenTypes? mailTelegramToken = tokens[1].TokenType;
			if (mailTelegramToken != TokenTypes.Mails && mailTelegramToken != TokenTypes.Telegram)
			{
				return false;
			}

			bool on;
			string onOffStr = tokens[2].GetStringValue().ToLower();
			if (CommandInterpreter.IsOnValue(onOffStr))
			{
				on = true;
			}
			else if (CommandInterpreter.IsOffValue(onOffStr))
			{
				on = false;
			}
			else
			{
				return false;
			}

			UserItem userItem = _database.UserLoadById(SessionUser.UserId);
			if (mailTelegramToken == TokenTypes.Mails)
			{
				userItem.AllowRecvMails = on;
			}
			if (mailTelegramToken == TokenTypes.Telegram)
			{
				userItem.AllowRecvTelegram = on;
			}
			_database.UserUpdate(userItem);
			SessionUser = userItem;
			return true;
		}

		private bool CmdSetEventCode(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;

			string eventPin = tokens[1].GetStringValue().ToLower();
			if (eventPin == "off" || eventPin == "aus")
			{
				eventPin = null;
			}
			else
			{
				if (!CommonHelper.IsValidPin(eventPin))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidEventCode));
					return true;
				}
			}

			UserItem userItem = _database.UserLoadById(SessionUser.UserId);
			userItem.EventPin = eventPin;
			_database.UserUpdate(userItem);
			SessionUser = userItem;
			if (eventPin != null)
			{
				DispatchMsgAndLog($"set event code to {eventPin}");
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.EventCode, eventPin));
			}
			else
			{
				DispatchMsgAndLog("set event code off");
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.EventCodeOff));
			}
			return true;
		}

		private bool CmdSetShowSender(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2 && tokens.Count != 3) return true;
			if (tokens[1].TokenType != TokenTypes.Send) return false;

			if (tokens.Count == 2)
			{
				// show sender
				UserItem userItem = _database.UserLoadById(SessionUser.UserId);
				userItem.ShowSender = true;
				_database.UserUpdate(userItem);
				SessionUser = userItem;
				SendAscii($"\r\n{LngText(LngKeys.ShowSenderActive)}");
				DispatchMsgAndLog("cmd show sender activated");
				return true;
			}
			else if (tokens.Count == 3 && tokens[2].TokenType == TokenTypes.Off)
			{
				// show sender off
				UserItem userItem = _database.UserLoadById(SessionUser.UserId);
				userItem.ShowSender = false;
				_database.UserUpdate(userItem);
				SessionUser = userItem;
				SendAscii($"\r\n{LngText(LngKeys.ShowSenderInactive)}");
				DispatchMsgAndLog("cmd show sender deactivated");
				return true;
			}
			return false;
		}

		private bool CmdSetNewPin(List<CmdTokenItem> tokens)
		{
			UserItem userItem = _database.UserLoadById(SessionUser.UserId);

			// old pin

			InputResult inputResult = InputString($"\r\n{LngText(LngKeys.EnterOldPin)}:", ShiftStates.Figs, null, 5, 1);
			if (!IsConnected || inputResult.ErrorOrTimeoutOrDisconnected) return true;

			string oldPin = inputResult.InputString;
			if (oldPin != userItem.Pin)
			{
				SendAscii($"\r\n{LngText(LngKeys.WrongPin)}");
				return true;
			}

			// new pin

			inputResult = InputString($"{LngText(LngKeys.EnterNewPin)}:", ShiftStates.Figs, null, 5, 1);
			if (!IsConnected || inputResult.ErrorOrTimeoutOrDisconnected) return true;

			string newPin = inputResult.InputString;
			if (newPin == oldPin)
			{
				SendAscii($"\r\n{LngText(LngKeys.PinNotChanged)}");
				return true;
			}
			if (!CommonHelper.IsValidPin(newPin))
			{
				SendAscii($"\r\n{LngText(LngKeys.InvalidPin)}");
				return true;
			}

			// new pin again

			inputResult = InputString($"{LngText(LngKeys.EnterNewPinAgain)}:", ShiftStates.Figs, null, 5, 1);
			if (!IsConnected || inputResult.ErrorOrTimeoutOrDisconnected) return true;

			string newPin2 = inputResult.InputString;
			if (!CommonHelper.IsValidPin(newPin2))
			{
				SendAscii($"\r\n{LngText(LngKeys.InvalidPin)}");
				return true;
			}
			if (newPin2 != newPin)
			{
				SendAscii($"\r\n{LngText(LngKeys.PinsNotEqual)}");
				return true;
			}

			// change pin

			userItem.Pin = newPin;
			if (!_database.UserUpdate(userItem))
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(CmdSetNewPin), "error changing pin in database.");
				SendAscii($"\r\n{LngText(LngKeys.InternalError)}");
				SendAscii($"\r\n{LngText(LngKeys.PinNotChanged)}");
				return true;
			}
			SessionUser = userItem;

			SendAscii($"\r\n{LngText(LngKeys.PinChanged)}");

			// send notification

			ConfirmationItem confItem = new ConfirmationItem
			{
				UserId = userItem.UserId,
				Type = (int)ConfirmationTypes.Changed,
				Number = RemoteNumber.Value,
				Pin = newPin,
				Language = ConnectionLanguage.ShortName,
				CreateTimeUtc = DateTime.UtcNow,
			};
			_database.ConfirmationsInsert(confItem);

			return true;
		}

		private bool CmdSettings(List<CmdTokenItem> tokens)
		{
			string msg;

			// number
			msg = LngText(LngKeys.SettingNumber, SessionUser.ItelexNumber.ToString());
			SendAscii($"\r\n{msg}");

			// pending msgs
			//msg = LngText(LngKeys.SettingPending, _database.MsgsAllPendingCount(SessionUser.UserId).ToString());
			//SendAscii($"\r\n{msg}");

			// hours
			int fromHours = 0;
			int toHours = 24;
			if (SessionUser.SendFromHour != null && SessionUser.SendToHour != null)
			{
				fromHours = SessionUser.SendFromHour.Value;
				toHours = SessionUser.SendToHour.Value;
			}
			msg = LngText(LngKeys.SettingHours, fromHours.ToString(), toHours.ToString());
			SendAscii($"\r\n{msg}");

			// pause
			string str = SessionUser.Paused ? LngText(LngKeys.Yes) : LngText(LngKeys.No);
			msg = LngText(LngKeys.SettingPaused, str);
			SendAscii($"\r\n{msg}");

			// timezone
			msg = LngText(LngKeys.SettingTimezone, SessionUser.Timezone.ToString());
			SendAscii($"\r\n{msg}");

			// allow receiving mail 
			str = SessionUser.AllowRecvMails.Value ? LngText(LngKeys.Yes) : LngText(LngKeys.No);
			msg = LngText(LngKeys.SettingAllowRecvMails, str);
			SendAscii($"\r\n{msg}");

			// allow receiving telegram
			str = SessionUser.AllowRecvTelegram.Value ? LngText(LngKeys.Yes) : LngText(LngKeys.No);
			msg = LngText(LngKeys.SettingAllowRecvTelegram, str);
			SendAscii($"\r\n{msg}");

			// max mails per day
			msg = LngText(LngKeys.SettingMaxMailsPerDay, SessionUser.MaxMailsPerDay.ToString(), SessionUser.MailsPerDay.ToString());
			SendAscii($"\r\n{msg}");

			// max lines per day
			msg = LngText(LngKeys.SettingMaxLinesPerDay, SessionUser.MaxLinesPerDay.ToString(), SessionUser.LinesPerDay.ToString());
			SendAscii($"\r\n{msg}");

			// max pending mails
			int pendMails = _database.MsgsAllPendingCount(SessionUser.UserId);
			msg = LngText(LngKeys.SettingMaxPendMails, SessionUser.MaxPendMails.ToString(), pendMails.ToString());
			SendAscii($"\r\n{msg}");

			// allowed sender
			str = OutgoingManager.ConvSender(SessionUser.AllowedSender) != null ? SessionUser.AllowedSender : "-";
			msg = LngText(LngKeys.SettingAllowedSender, str);
			SendAscii($"\r\n{msg}");

			// associated fix mail-address
			/*
			str = SessionUser.Receiver != null ? SessionUser.Receiver : "-";
			str = str.Replace("@", "(at)");
			msg = LngText(LngKeys.SettingAssociatedMailAddr, str);
			SendAscii($"\r\n{msg}");
			*/

			// show sender address
			str = SessionUser.ShowSender ? LngText(LngKeys.Yes) : LngText(LngKeys.No);
			msg = LngText(LngKeys.SettingShowSenderAddr, str);
			SendAscii($"\r\n{msg}");

			// event pin
			if (SessionUser.EventPin != null)
			{
				//str = SessionUser.EventPin != null ? SessionUser.EventPin : "-";
				msg = LngText(LngKeys.SettingEventPin, SessionUser.EventPin);
				SendAscii($"\r\n{msg}");
			}

			return true;
		}

		private bool CmdSend(List<CmdTokenItem> tokens)
		{
			switch(tokens[1].TokenType)
			{
				case TokenTypes.Mails:
					if (tokens.Count != 2) return false;
					return SendMail();
				case TokenTypes.Fax:
					if (tokens.Count != 2) return false;
					return SendFax();
				case TokenTypes.PunchTape:
					if (tokens.Count != 2) return false;
					return SendPunchTape();
				case TokenTypes.Pruef:
					return SendPruefTexts(tokens);
				default:
					return false;
			}
		}

		private bool SendMail()
		{
			InputResult inputResult = InputString($"\r\n{LngText(LngKeys.MailOrFaxReceiver)}:", ShiftStates.Ltrs, null, 50, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected || string.IsNullOrWhiteSpace(inputResult.InputString)) return true;
			string receiver = inputResult.InputString.Trim();
			if (!receiver.Contains("(at)"))
			{
				SendAscii($"\r\n{LngText(LngKeys.InvalidMailAdress)}");
				return true;
			}

			inputResult = InputString($"\r\n{LngText(LngKeys.MailSubject)}:", ShiftStates.Ltrs, null, 50, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected || string.IsNullOrWhiteSpace(inputResult.InputString)) return true;
			string subject = inputResult.InputString.Trim();

			SendAscii($"\r\n{LngText(LngKeys.MailOrFaxReceiver)}: {receiver}");
			SendAscii($"\r\n{LngText(LngKeys.MailSubject)}: {subject}");
			inputResult = InputYesNo($"\r\n{LngText(LngKeys.Ok)} ?", null, 3);
			if (inputResult.ErrorOrTimeoutOrDisconnected || inputResult.InputBool == false) return true;

			SendAscii($"\r\n{LngText(LngKeys.InputMessage)}:\r\n");
			_inputLine = "";
			_inputActive = true;
			string msg = GetMultiLineMessage("");
			_inputActive = false;
			if (!IsConnected) return true;

			if (string.IsNullOrWhiteSpace(msg)) return false;
								
			string header = LngText(LngKeys.MailHeader, SessionUser.ItelexNumber.ToString());
			string msgWithHeader = $"{header}\r\n\r\n{msg}";

			subject = $"#{SessionUser.ItelexNumber} {subject}";
			if (MailManager.Instance.SendMailSmtp(receiver, subject, msgWithHeader, SessionUser.ItelexNumber))
			{
				SendAscii($"\r\n\n{LngText(LngKeys.MailSendSuccessfully)}");
				DispatchMsgAndLog($"send mail to {receiver}: ok");
				SessionUser.TotalMailsSent++;
				_database.UserUpdate(SessionUser);
			}
			else
			{
				SendAscii($"\r\n\n{LngText(LngKeys.MailSendError)}");
				DispatchMsgAndLog($"send mail to {receiver}: error");
			}

			return true;
		}

		private bool SendFax()
		{
			string[] allowedPrefixes = new string[]
			{
				"001", // USA 1,9 Cent/Min.
				"0031", // Niederlande 9 Cent/Min.
				"0033", // Frankreich 9 Cent/Min.
				"0034", // Spanien 9 Cent/Min.
				"0039", // Italien 9 Cent/Min.
				"0041", // Schweiz 9 Cent/Min.
				"0043", // Österreich 9 Cent/Min.
				"0044", // Großbritannien 9 Cent/Min.
				"0049", // Deutschland
			};

			string[] forbiddenPrefixes = new string[]
			{
				"00",
				"01",
				"032",
				"0700",
				"0800",
				"0900",
				"118"
			};

			InputResult inputResult = InputString($"\r\n{LngText(LngKeys.MailOrFaxReceiver)}:", ShiftStates.Ltrs, null, 50, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected || string.IsNullOrWhiteSpace(inputResult.InputString)) return true;
			string receiver = inputResult.InputString.Trim();
			receiver = receiver.Replace(" ", "");
			receiver = receiver.Replace("-", "");
			receiver = receiver.Replace("(", "");
			receiver = receiver.Replace(")", "");
			receiver = receiver.Replace("/", "");

			if (receiver.Length < 5)
			{
				SendAscii($"\r\n{LngText(LngKeys.InvalidFaxNumber)}");
				return true;
			}

			if (receiver.StartsWith("+"))
			{
				receiver = "00" + receiver.Substring(1);
			}

			// must not contain other characters than digits
			foreach (char c in receiver)
			{
				if (!char.IsDigit(c))
				{
					SendAscii($"\r\n{LngText(LngKeys.InvalidFaxNumber)}");
					return true;
				}
			}

			// must start with "0"
			if (!receiver.StartsWith("0"))
			{
				SendAscii($"\r\n{LngText(LngKeys.ForbiddenFaxNumber)}");
				return true;
			}

			// check allowed prefixes
			bool allowed = false;
			foreach (string p in allowedPrefixes)
			{
				if (receiver.StartsWith(p))
				{
					allowed = true;
					break;
				}
			}

			if (!allowed)
			{
				// no forbidden prefixes
				foreach (string p in forbiddenPrefixes)
				{
					if (receiver.StartsWith(p))
					{
						SendAscii($"\r\n{LngText(LngKeys.ForbiddenFaxNumber)}");
						return true;
					}
				}
			}

			SendAscii($"\r\n{LngText(LngKeys.MailOrFaxReceiver)} {receiver}");
			inputResult = InputYesNo($"\r\n{LngText(LngKeys.Ok)} ?", null, 3);
			if (inputResult.ErrorOrTimeoutOrDisconnected || inputResult.InputBool == false) return true;

			SendAscii($"\r\n{LngText(LngKeys.InputMessage)}:\r\n");
			_inputLine = "";
			_inputActive = true;
			string msg = GetMultiLineMessage("");
			_inputActive = false;

			msg = msg.Trim(new char[] { '\r', '\n', '+', ' ' });
			if (msg.Length == 0) return true;

			//WinFaxManager.Instance.SendFax(SessionUser.ItelexNumber, receiver, msg);
			FaxManager.Instance.SendFax(msg, FaxFormat.Endless, true, SessionUser.UserId, false,
					SessionUser.ItelexNumber.ToString(), receiver, "de");

			string text = LngText(LngKeys.FaxWillBeSend, receiver);
			SendAscii(CodeManager.ASC_COND_NL + $"\r\n{text}");
			return true;
		}

		private bool SendPunchTape()
		{
			InputResult inputResult = InputString($"\r\n{LngText(LngKeys.MailOrFaxReceiver)}:", ShiftStates.Ltrs, null, 50, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected || string.IsNullOrWhiteSpace(inputResult.InputString)) return true;
			string receiver = inputResult.InputString.Trim();
			if (!receiver.Contains("(at)"))
			{
				SendAscii($"\r\n{LngText(LngKeys.InvalidMailAdress)}");
				return true;
			}

			string filename = null;
			for (int i=0; i<3; i++)
			{
				inputResult = InputString($"\r\n{LngText(LngKeys.PunchTapeFilename)}:", ShiftStates.Ltrs, null, 40, 1);
				if (inputResult.ErrorOrTimeoutOrDisconnected || string.IsNullOrWhiteSpace(inputResult.InputString)) return true;
				string fn = CommonHelper.CleanupFilename(inputResult.InputString);
				if (fn.Length >= 1)
				{
					filename = fn;
					break;
				}
				Log(LogTypes.Notice, nameof(SendPunchTape), $"invalid ls-filename {inputResult.InputString}");
			}
			if (filename == null)
			{
				SendAscii($"\r\n{LngText(LngKeys.InvalidFilename)}");
				return true;
			}
			if (!Path.HasExtension(filename)) filename += ".ls";

			SendAscii($"\r\n{LngText(LngKeys.MailOrFaxReceiver)}: {receiver}");
			SendAscii($"\r\n{LngText(LngKeys.Filename)}: {filename}");
			inputResult = InputYesNo($"\r\n{LngText(LngKeys.Ok)} ?", null, 3);
			if (inputResult.ErrorOrTimeoutOrDisconnected || inputResult.InputBool == false) return true;

			SendAscii($"\r\n{LngText(LngKeys.PunchTapeSend)}:\r\n");

			// start receiving
			_receivedBaudot = new List<byte>();
			_receiveActive = true;

			while (true)
			{
				Thread.Sleep(100);
				if (!IsConnected) return true;
				if (FindPunchTapeEnd())
				{
					Log(LogTypes.Notice, nameof(SendPunchTape), $"recv terminated {ConnectionName}");
					DispatchMsg($"recv terminated {ConnectionName}");
					break;
				}
			}

			//SendAscii("\r\n\nuebertragung beendet. daten werden per email gesendet.\r\n");

			// end receiving
			_receiveActive = false;
			byte[] data = CheckAndStrip3Bell(_receivedBaudot.ToArray());
			if (data == null)
			{
				SendAscii($"\r\n\n{LngText(LngKeys.InvalidData)}");
				return true;
			}

			data = CodeManager.MirrorByteArray(data);
			_receivedBaudot = null;

			string subject = LngText(LngKeys.PunchTapeMailHeader, filename, SessionUser.ItelexNumber.ToString());
			string msg = subject;
			if (MailManager.Instance.SendMailSmtp(receiver, subject, msg, SessionUser.ItelexNumber, filename, data))
			{
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(SendPunchTape), $"mail sent successfully.");
				SendAscii($"\r\n\n{LngText(LngKeys.MailSendSuccessfully)}");
				DispatchMsgAndLog($"sent mail to {receiver}: ok");
				SessionUser.TotalMailsSent++;
				_database.UserUpdate(SessionUser);
			}
			else
			{
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(SendPunchTape), $"error sending mail.");
				SendAscii($"\r\n\n{LngText(LngKeys.MailSendError)}");
				DispatchMsgAndLog($"sent mail to {receiver}: error");
			}

			// for debugging
			MailManager.Instance.SendMailSmtp(Constants.DEBUG_EMAIL_ADDRESS, subject, msg, SessionUser.ItelexNumber, filename, data);
			return true;
		}

		private bool FindPunchTapeEnd()
		{
			if (_receivedBaudot.Count < 3) return false;

			const byte BEL = CodeManager.BAU_BEL;
			for (int i = _receivedBaudot.Count - 3; i >= 0; i--)
			{
				if (_receivedBaudot[i] == BEL && _receivedBaudot[i + 1] == BEL && _receivedBaudot[i + 2] == BEL)
				{
					return true;
				}
			}
			return false;
		}

		private byte[] CheckAndStrip3Bell(byte[] data)
		{
			if (data == null || data.Length < 4) return null;

			int len = data.Length;
			const byte BEL = CodeManager.BAU_BEL;
			if (_receivedBaudot[len - 3] != BEL || _receivedBaudot[len - 2] != BEL || _receivedBaudot[len - 1] != BEL) return null;
			return data.Take(len - 3).ToArray();
		}

		private bool CmdListPruefTexts(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2 || tokens[1].TokenType != TokenTypes.Pruef) return false;
			List<PruefTextItem> items = PruefTexte.Instance.GetPruefTextItems();
			foreach(PruefTextItem item in items)
			{
				SendAscii($"\r\n{item.Name}");
			}
			//SendAscii(CodeManager.ASC_COND_NL + "\r\n");
			return true;
		}

		private bool SendPruefTexts(List<CmdTokenItem> tokens)
		{
			if (tokens[1].TokenType != TokenTypes.Pruef || tokens.Count < 3 || tokens.Count > 4) return false;
			string name = tokens[2].GetStringValue();
			int repeat = 1;
			if (tokens.Count == 4)
			{
				int? value = tokens[3].GetNumericValue();
				if (!value.HasValue) return false;
				repeat = value.Value;
			}
			if (repeat == 0) return true;

			PruefTextItem pt = PruefTexte.Instance.GetPruefTextItem(name);
			if (pt == null)
			{
				SendAscii(CodeManager.ASC_COND_NL + $"\r\n{LngText(LngKeys.PruefTextNotFound, name)}");
				//SendAscii($"\r\nprueftext '{name}' nicht vorhanden.");
				return true;
			}

			SendAscii(CodeManager.ASC_COND_NL + "\r\n");
			for (int i=0; i<repeat; i++)
			{
				if (i != 0) SendAscii("\r\n");
				SendAscii(pt.Text);
			}
			//SendAscii(CodeManager.ASC_COND_NL + "\r\n");

			return true;
		}


		private bool CmdHelp(List<CmdTokenItem> tokens)
		{
			DispatchMsgAndLog($"cmd help");
			if (ConnectionLanguage.ShortName == "de")
			{
				//              12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\nhilfe / erlaubte befehle                        beispiel\n");
				SendAscii("\r\nsende mail      sende eine mail                  sen mai");
				SendAscii("\r\nsende fax       sende ein fax                    sen fax");
				SendAscii("\r\nsende ls        sende einen lochstreifen         sen ls");
				SendAscii("\r\neinstellungen   zeigt alle einstellungen         ein");
				SendAscii("\r\nzeitzone (z)    setze zeitzone                   zei 2");
				SendAscii("\r\npause           telex-versand pausieren          pau");
				SendAscii("\r\npause aus       pausieren aus                    pau aus");
				SendAscii("\r\nstunden (h) (h) zeitraum fuer nachrichtenversand stu 8 21");
				SendAscii("\r\nstunden aus     zeitraum fuer nachrichtenv. aus  stu aus");
				SendAscii("\r\nzeige absender  e-mail-absender zeigen           zei abs");
				SendAscii("\r\nzeige abs aus   e-mail-absender nicht zeigen     zei abs aus");
				SendAscii("\r\npin             pin aendern                      pin");
				SendAscii("\r\nerlaub mail (m) erlaube mail-empfang ein/aus     erl mai ein");
				SendAscii("\r\nerlaub tele (m) erlaube telegr-empfang ein/aus   erl tel ein");
				//SendAscii("\r\nloesche         loesche wartende mails           loe");
				//SendAscii("\r\nmax mails (n)   setze max. mails pro tag         max mai 10");
				//SendAscii("\r\nmax zeilen (n)  setze max. zeilen pro tag        max zei 200");
				//SendAscii("\r\nmax wartend (n) setze max. wartende mails        max war 5");
				//SendAscii("\r\nabsender (s)    setze erlaubten absender         abs mail(at)test.de");
				//SendAscii("\r\nabsender aus    erlaubten absender loeschen      abs aus");
				SendAscii("\r\nlist pruef      liste der prueftexte ausgeben    lis pru");
				SendAscii("\r\nsend pruef (p) (n) prueftext p n-mal ausgeben    sen pru ry 5");
				SendAscii("\r\nende            verb. beenden (oder st-taste)    end");
				SendAscii($"\r\n\nbei allen befehlen ist die eingabe der ersten {_commandInterpreter.MinLen} zeichen\r\nausreichend.");
			}
			else
			{
				//              12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\nhelp / allowed commands                         example\n");
				SendAscii("\r\nsend mail       send mail                        sen mai");
				SendAscii("\r\nsend fax        send fax                         sen fax");
				SendAscii("\r\nsend punchtape  send a punch tape                sen pun");
				SendAscii("\r\nsettings        shows all settings               set");
				SendAscii("\r\ntimezone (t)    set timezone                     tim 2");
				SendAscii("\r\npause           pause telex distribution         pau");
				SendAscii("\r\npause off       pause off                        pau off");
				SendAscii("\r\nhours (h) (h)   hours for distribution           hou 8 21");
				SendAscii("\r\nhours off       hours for distrib. off           hou off");
				SendAscii("\r\nshow sender     show email sender address        sho sen");
				SendAscii("\r\nshow sender off show email sender address off    sho sen off");
				SendAscii("\r\npin             change pin                       pin");
				SendAscii("\r\nallow mail (m)  allow recv. mail on/off          all mai on");
				SendAscii("\r\nallow tele (m)  allow recv. telegramm on/off     all tel on");
				//SendAscii("\r\nclear           clear pending mails              cle");
				//SendAscii("\r\nmax mails (n)   set max. mails per day           max mai 10");
				//SendAscii("\r\nmax lines (n)   set max. lines per day           max lin 200");
				//SendAscii("\r\nmax pending (n) set max. pending mails           max pen 5");
				//SendAscii("\r\noriginator (s)  set allowed originator           ori mail(at)test.de");
				//SendAscii("\r\noriginator off  clear allowed sender             ori off");
				SendAscii("\r\nlist test       list all test texts              lis tes");
				SendAscii("\r\nsend test (t) (n) print test text x times        sen tes ry 5");
				SendAscii("\r\nend             term. connection (or st-key)     end");
				SendAscii($"\r\n\nfor all commands, entering the first {_commandInterpreter.MinLen}\r\ncharacters is sufficient.");
			}
			return true;
		}

		private void EmailHelp()
		{
			if (ConnectionLanguage.ShortName == "de")
			{
				//             12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\n(at) fuer at-Zeichen verwenden.");
				SendAscii("\r\n%xx fuer asciizeichen in hexadezimaldarstellen (%20-%7e).");
				SendAscii("\r\nz.b. '_' = %7e.");
			}
		}

		private string GetLngText(int key, params string[] param)
		{
			return LanguageManager.Instance.GetText(key, ConnectionLanguage.Id, param); 
		}

		private void SendNewPinMsg(int number)
		{
			string msg = LngText(LngKeys.SendNewPinMsg, number.ToString());
			SendAscii($"\r\n{msg}");
			DispatchMsgAndLog($"Logoff with send pin to {number}");
		}

		private int? InputTimezone(string text, int? defaultTz)
		{
			for (int i = 0; i < 3; i++)
			{
				string defaultStr = defaultTz != null ? defaultTz.ToString() : "";
				InputResult inputResult = InputString($"{text}:", ShiftStates.Figs, defaultStr, 3, 1);
				if (inputResult == null || inputResult.ErrorOrTimeoutOrDisconnected) continue;
				if (!int.TryParse(inputResult.InputString, out int timezone)) continue;
				if (timezone < -12 || timezone > 14)
				{
					SendAscii($"\r\n{LngText(LngKeys.InvalidTimezone)}");
					continue;
				}
				return timezone;
			}
			return null;
		}

		private bool _inputActive;
		private string _inputLine;
		private object _inputLineLock = new object();

		private string GetMultiLineMessage(string remainungInput)
		{
			while (true)
			{
				Thread.Sleep(100);
				if (!IsConnected) return null;

				lock (_inputLineLock)
				{
					if (_inputLine == "") continue;
					int p = _inputLine.IndexOf("+++");
					if (p >= 0)
					{
						string msg = remainungInput + _inputLine.Substring(0, p);
						msg = ParseMessage(msg);
						_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetMultiLineMessage), "recv textend (+++)");
						return msg;
					}
				}
			}
		}

		private string ParseMessage(string messageStr)
		{
			// remove starting "\r\n"
			messageStr = messageStr.TrimStart(new char[] { '\r', '\n' });

			// remove "+++"
			int pos = messageStr.IndexOf("+++");
			if (pos > 0)
			{
				messageStr = messageStr.Substring(0, pos);
			}

			// remove ending "\r\n"
			messageStr = messageStr.TrimEnd(new char[] { '\r', '\n' });
			return messageStr;
		}

		private void DispatchMsgAndLog(string msg)
		{
			MessageDispatcher.Instance.Dispatch(ConnectionId, msg);
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(DispatchMsgAndLog), msg);
		}

		public string LngText(LngKeys lngKey)
		{
			return LanguageManager.Instance.GetText((int)lngKey, ConnectionLanguage.Id);
		}

		public string LngText(LngKeys lngKey, params string[] param)
		{
			return LanguageManager.Instance.GetText((int)lngKey, ConnectionLanguage.Id, param);
		}

		/*
		private string CreatePin()
		{
			while(true)
			{
				string pinStr = _random.Next(1001, 9988).ToString();
				int[] digitCnt = new int[10];
				bool max2 = true;
				for (int i=0; i<4; i++)
				{
					int digit = ((int)pinStr[i]) - 48;
					digitCnt[digit]++;
					if (digitCnt[digit] > 2) max2 = false;
				}
				if (max2) return pinStr;
			}
		}
		*/

		/*
		private void IncomingSendMail()
		{
			ItelexIncomingConfiguration config = new ItelexIncomingConfiguration
			{
				OurPrgmVersionStr = Helper.GetVersionCode(),
				OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
				//LoginType = new LoginTypes[] { LoginTypes.SendTime, LoginTypes.SendKg, LoginTypes.GetKg },
				LoginType = new LoginTypesWithExtension(
					new LoginTypeWithExtension(null, LoginTypes.SendTime, LoginTypes.SendKg, LoginTypes.GetKg )
				),
				ItelexExtensions = GlobalData.Instance.ItelexValidExtensions,
				GetLngText = GetLngText,
				LngKeyMapper = new Dictionary<IncomingTexts, int>() {}
			};
			if (!StartIncoming(config) || !IsConnected)
			{
				Logoff(null);
				return;
			}

			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Start), $"id={ConnectionId} Start IncomingSendMail");

			Logoff(null);
			// send a message
		}

		private void IncomingSendFax()
		{
			ItelexIncomingConfiguration config = new ItelexIncomingConfiguration
			{
				OurPrgmVersionStr = Helper.GetVersionCode(),
				OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
				LoginType = new LoginTypes[] { LoginTypes.SendTime, LoginTypes.SendKg, LoginTypes.GetKg },
				ItelexExtensions = GlobalData.Instance.ItelexValidExtensions,
				GetLngText = GetLngText,
				LngKeyMapper = new Dictionary<IncomingTexts, int>() { }
			};
			if (!StartIncoming(config) || !IsConnected)
			{
				Logoff(null);
				return;
			}

			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Start), $"id={ConnectionId} Start IncomingSendFax");

			Logoff(null);
			// send a message

		}
		*/

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
