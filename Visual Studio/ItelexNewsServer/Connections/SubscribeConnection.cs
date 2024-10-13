using ItelexCommon;
using ItelexCommon.Commands;
using ItelexCommon.Connection;
using ItelexCommon.Utility;
using ItelexNewsServer.Commands;
using ItelexNewsServer.Data;
using ItelexNewsServer.Languages;
using ItelexNewsServer.News;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using static ItelexNewsServer.Connections.CallNumberResult;
using System.Runtime.Remoting.Channels;
using System.Xml.Linq;
using System.Threading;
using ItelexCommon.Logger;

namespace ItelexNewsServer.Connections
{
	class SubscribeConnection : ItelexIncoming
	{
		private const string INPUT_PROMPT = null;

		private NewsDatabase _database;

		private CallManager _callManager;

		private NewsManager _newsManager;

		private SubscriberServer _subcriberServer;

		private CommandInterpreter _commandInterpreter;

		//private Random _random = new Random();

		private bool _inputActive;
		private string _inputLine;
		private object _inputLineLock = new object();

		public UserItem SessionUser { get; set; }

		public DateTime SessionStartTime { get; set; }

		//public string InputLine { get; set; }

		public SubscribeConnection()
		{
		}

		public SubscribeConnection(TcpClient client, int idNumber, string logPath, LogTypes logLevel) : 
				base(client, idNumber, GetLogger(idNumber, logPath, logLevel))
		{
			TAG = nameof(SubscribeConnection);

			_database = NewsDatabase.Instance;
			_callManager = (CallManager)GlobalData.Instance.OutgoingConnectionManager;
			_newsManager = NewsManager.Instance;
			_subcriberServer = new SubscriberServer();
			_commandInterpreter = new CommandInterpreter();

			_inputActive = false;
			_inputLine = "";
			this.ItelexReceived += SubscribeConnection_Received;

			InitTimerAndHandler();

			SessionStartTime = DateTime.Now;
			BuRefreshActive = true;
		}

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
			this.ItelexReceived += Connection_Received;
			//this.Dropped += Connection_Dropped;
		}

		#region Dispose

		// Flag: Has Dispose already been called?
		private bool _disposed = false;

		// Protected implementation of Dispose pattern.
		protected override void Dispose(bool disposing)
		{
			//LogManager.Instance.Logger.Debug(TAG, nameof(Dispose), $"disposing={disposing}");

			base.Dispose(disposing);

			if (_disposed) return;

			if (disposing)
			{
				this.ItelexReceived -= Connection_Received;
				//this.Dropped -= Connection_Dropped;
			}

			_disposed = true;
		}

		#endregion Dispose

		//private void Connection_Dropped(ItelexConnection connection)
		//{
		//	DroppedEvent?.Invoke(this);
		//}

		private void Connection_Received(ItelexConnection connection, string text)
		{
			//Received?.Invoke(this, text);
		}

		public override void Start()
		{
			//_inputEndChars = new char[] { CodeManager.ASC_LF };

			ItelexIncomingConfiguration config = new ItelexIncomingConfiguration
			{
				OurPrgmVersionStr = Helper.GetVersionCode(),
				OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
				//LoginSequences = new LoginTypes[] { LoginTypes.SendTime, LoginTypes.SendKg, LoginTypes.GetKg, LoginTypes.GetNumberAndPin },
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

			Log(LogTypes.Debug, nameof(Start), "start");

			if (!RemoteNumber.HasValue)
			{
				Logoff(null);
				return;
			}

			SessionUser = _database.UserLoadByTelexNumber(RemoteNumber.Value);

			ConfirmationItem confItem = _database.ConfirmationsLoadByType(SessionUser.UserId, ConfirmationTypes.NewPin);
			if (confItem != null && !confItem.Finished)
			{
				confItem.Finished = true;
				_database.ConfirmationsUpdate(confItem);

				// confirm login
				SendAscii($"\r\n{LngText(LngKeys.NewAccountActivated)}");
				SendAscii($"\r\n{LngText(LngKeys.NewAccountTimezoneInfo)}");
				int? timezone = InputTimezone($"\r\n{LngText(LngKeys.NewAccountEnterTimezone)}", SessionUser.Timezone);
				if (timezone != null)
				{
					SessionUser.Timezone = timezone.Value;
					_database.UserInsert(SessionUser);
				}
			}

			confItem = _database.ConfirmationsLoadByType(SessionUser.UserId, ConfirmationTypes.Redirect);
			if (confItem != null && !confItem.Finished)
			{
				for (int i = 0; i < 3; i++)
				{
					InputResult inputResult = InputPin($"\r\n{LngText(LngKeys.EnterRedirectConfirmPin)}:", 5);
					if (!inputResult.ErrorOrTimeoutOrDisconnected && inputResult.InputString == confItem.Pin)
					{
						// redirection confirmed
						confItem.Finished = true;
						_database.ConfirmationsUpdate(confItem);
						SessionUser.RedirectNumber = confItem.Number;
						_database.UserUpdate(SessionUser);
						SendAscii($"\r\n{LngText(LngKeys.RedirectActivated, SessionUser.RedirectNumber.ToString())}");
						break;
					}
					else
					{
						SendAscii($"\r\n{LngText(LngKeys.WrongPin)}");
					}
				}

				if (!confItem.Finished)
				{
					// no correct pin entered
					SendAscii($"\r\n{LngText(LngKeys.RedirectNotConfirmed)}");
					confItem.Finished = true;
					_database.ConfirmationsUpdate(confItem);
				}
			}

			InterpreterLoop();

			SendAscii("\r\n");
			Logoff($"{LngText(LngKeys.ConnectionTerminated)}\r\n");

			DispatchMsg("Logoff");
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
			DispatchMsg($"Create new account {itelexNumber}");

			// add new number
			DateTime utcNow = DateTime.UtcNow;
			UserItem userItem = new UserItem(itelexNumber)
			{
				RegisterTimeUtc = utcNow,
				LastLoginTimeUtc = utcNow,
				LastPinChangeTimeUtc = utcNow,
				Kennung = RemoteAnswerbackStr,
				Timezone = 2,
				MsgFormat = (int)MsgFormats.Short,
				Active = true,
				MaxPendingNews = Constants.MAX_PENDING_NEWS,
				Pin = CommonHelper.CreatePin(),
			};
			_database.UserInsert(userItem);

			ConfirmationItem confItem = new ConfirmationItem
			{
				UserId = userItem.UserId,
				Number = itelexNumber,
				Pin = userItem.Pin,
				Language = ConnectionLanguage.ShortName,
				CreateTimeUtc = DateTime.UtcNow,
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

			SessionUser.Pin = confItem.Pin;
			_database.UserUpdate(SessionUser);

			SendNewPinMsg(RemoteNumber.Value);
			return true;
		}

		private string GetLngText(int key, string[] prms = null)
		{
			return LanguageManager.Instance.GetText(key, ConnectionLanguage.Id, prms); 
		}

		private void SendNewPinMsg(int number)
		{
			SendAscii($"\r\n{LngText(LngKeys.SendNewPinMsg, number.ToString())}");
			DispatchMsg($"Logoff with send pin to {number}");
		}

		private void InterpreterLoop()
		{
			bool first = true;
			bool empty = false;
			while (IsConnected)
			{
				if (!first)
				{
					SendAscii(CodeManager.ASC_COND_NL);
				}
				first = false;
				if (!empty)
				{
					SendAscii("\r\n");
				}
				empty = false;
				InputResult inputResult = InputString($"{LngText(LngKeys.CmdPrompt)}:", ShiftStates.Ltrs, null, 30, 1);
				if (inputResult.ErrorOrTimeoutOrDisconnected) return;

				Log(LogTypes.Notice, nameof(InterpreterLoop), $"cmd: '{inputResult.InputString}'");
				DispatchMsg($"cmd: {inputResult.InputString}");
				string inputStr = inputResult.InputString.Trim();
				if (string.IsNullOrWhiteSpace(inputStr))
				{
					empty = true;
					continue;
				}

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
						case TokenTypes.End:
							return;
						case TokenTypes.List:
							ok = CmdListChannels(tokens);
							break;
						case TokenTypes.Subscribe:
						case TokenTypes.Unsubscribe:
						case TokenTypes.Preview:
							ok = CmdSubscribe(tokens);
							break;
						case TokenTypes.Clear:
							ok = CmdClearPendingMsgs(tokens);
							break;
						case TokenTypes.Set:
							ok = CmdSettings(tokens);
							break;
						case TokenTypes.Pause:
							ok = CmdSetPause(tokens);
							break;
						case TokenTypes.Redirect:
							ok = CmdSetRedirect(tokens);
							//if (_sendRedirectionPin) return;
							break;
						case TokenTypes.Timezone:
							ok = CmdSetTimezone(tokens);
							break;
						case TokenTypes.Format:
							ok = CmdSetFormat(tokens);
							break;
						case TokenTypes.Pin:
							ok = CmdSetNewPin(tokens);
							break;
						case TokenTypes.Hours:
							ok = CmdSetHours(tokens);
							break;
						case TokenTypes.MaxPendMsgs:
							ok = CmdSetMaxPendingMsgs(tokens);
							break;
						case TokenTypes.Select:
							ok = CmdSelectLocalChannel(tokens);
							break;
						case TokenTypes.New:
							ok = CmdNewLocalChannel(tokens);
							break;
						case TokenTypes.Edit:
							ok = CmdEditSelectedLocalChannel(tokens);
							break;
						case TokenTypes.Delete:
							ok = CmdDeleteSelectedLocalChannel(tokens);
							break;
						case TokenTypes.Numbers:
							ok = CmdListNumbersOfSelectedChannel(tokens);
							break;
						case TokenTypes.Send:
							ok = CmdSendLocalMessage(tokens);
							break;
					}
					if (!ok)
					{
						ShowInvalidCmd();
					}
				}
				catch(Exception ex)
				{
					Log(LogTypes.Error, nameof(InterpreterLoop), "error", ex);
					SendAscii($"\r\n{LngText(LngKeys.InternalError)}");
				}
			}
		}

		private void ShowInvalidCmd()
		{
			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidCommand));
		}

		private void ShowCommandNotYetSupported()
		{
			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.CommandNotYetSupported));
		}

		private bool CmdListChannels(List<CmdTokenItem> tokens)
		{
			List<UserChannel> userChannels = null;

			if (tokens.Count == 1)
			{
				// show all channels
				userChannels = _newsManager.GetUserChannels(SessionUser.UserId, ChannelTypes.Rss, null, null);
				ShowChannels(userChannels, false);
				CmdListLocalChannels(tokens);
				return true;
			}

			if (tokens.Count == 2 && tokens[1].TokenType == TokenTypes.Local)
			{
				return CmdListLocalChannels(tokens);
			}

			// token.Count > 1

			switch (tokens[1].TokenType)
			{
				case TokenTypes.Subscribed:
					userChannels = _newsManager.GetUserChannels(SessionUser.UserId, ChannelTypes.Rss, null, null);
					userChannels = userChannels.Where(c => c.Subscribed).ToList();
					DispatchMsg($"cmd show subscribed channels {userChannels.Count}");
					if (userChannels.Count == 0)
					{
						SendAscii($"\r\n{LngText(LngKeys.NoMatchingChannels)}");
						return true;
					}
					ShowChannels(userChannels, false);
					return true;
				case TokenTypes.Category:
					ChannelCategoryItem catItem = (ChannelCategoryItem)tokens[2].Value;
					// if category is 'local' use channel type 'local'
					ChannelTypes channelType = catItem.Name == ChannelItem.GetCategoryName(ChannelCategories.Local) ?
												ChannelTypes.Local :
												ChannelTypes.Rss;
					userChannels = _newsManager.GetUserChannels(SessionUser.UserId, channelType, catItem.Category, null);
					DispatchMsg($"cmd show channels cat={tokens[2].Value} {userChannels.Count}");
					if (userChannels.Count == 0)
					{
						SendAscii($"\r\n{LngText(LngKeys.NoMatchingChannels)}");
						return true;
					}
					if (channelType == ChannelTypes.Local)
					{
						ShowChannels(userChannels, true);
					}
					else
					{
						ShowChannels(userChannels, false);
					}
					return true;
				case TokenTypes.Language:
					ChannelLanguageItem lngItem = (ChannelLanguageItem)tokens[2].Value;
					userChannels = _newsManager.GetUserChannels(SessionUser.UserId, ChannelTypes.Rss, null, lngItem.Language);
					DispatchMsg($"cmd show channels lng={tokens[2].Value} {userChannels.Count}");
					if (userChannels.Count == 0)
					{
						SendAscii($"\r\n{LngText(LngKeys.NoMatchingChannels)}");
						return true;
					}
					ShowChannels(userChannels, false);
					return true;
			}
			return false;
		}

		private bool CmdSubscribe(List<CmdTokenItem> tokens)
		{
			if (tokens.Count < 2) return false;

			TokenTypes cmdType = tokens[0].TokenType;
			int? channelId = tokens[1].GetNumericValue();
			if (channelId == null) return false;

			ChannelItem channelItem = _database.ChannelLoadById(channelId.Value);
			if (channelItem == null)
			{
				SendAscii($"\r\n{LngText(LngKeys.ChannelNotFound, channelId.Value.ToString(), channelId.ToString())}");
				return true;
			}

			if (cmdType == TokenTypes.Subscribe)
			{
				if (_newsManager.AddSubscription(channelId.Value, SessionUser.UserId, "td"))
				{
					string msg = LngText(LngKeys.SubscribeChannel, channelId.Value.ToString(), channelItem.Name);
					SendAscii($"\r\n{msg}");
					DispatchMsg(msg);
				}
				/*
				// content
				if (tokens.Count == 3)
				{
					if (tokens[2].TokenType != TokenTypes.ArgCont) return false;
					string resultContent = channelItem.GetResultContent(tokens[2].Value);
					if (_newsManager.AddSubscription(channelId.Value, SessionUser.UserId, resultContent))
					{
						SendAscii($"\r\nkanal {channelId} '{channelItem.Name}' cont='{resultContent}' abonniert");
					}
				}
				*/
				return true;
			}
			else if (cmdType == TokenTypes.Unsubscribe)
			{
				if (_newsManager.RemoveSubscription(channelId.Value, SessionUser.UserId))
				{
					string msg = LngText(LngKeys.UnsubscribeChannel, channelId.Value.ToString(), channelItem.Name);
					SendAscii($"\r\n{msg}");
					DispatchMsg(msg);
				}
				return true;
			}
			else if (cmdType == TokenTypes.Preview)
			{
				ShowCommandNotYetSupported();
				return true;
			}
			return false;
		}

		private bool CmdClearPendingMsgs(List<CmdTokenItem> tokens)
		{
			int rows = _database.MsgStatusPendingClearUser(SessionUser.UserId);
			string msg = LngText(LngKeys.PendingMsgsCleared, rows.ToString());
			SendAscii($"\r\n{msg}");
			DispatchMsg(msg);
			return true;
		}

		private bool CmdSetHours(List<CmdTokenItem> tokens)
		{
			int? from = null;
			int? to = null;
			if (tokens.Count == 3)
			{
				// set horus
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
				DispatchMsg("cmd pause activated");
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
				DispatchMsg("cmd pause deactivated");
				return true;
			}
			return false;
		}

		private bool CmdSetRedirect(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;

			if (tokens[1].TokenType == TokenTypes.ArgInt)
			{
				// pause on
				int? number = tokens[1].GetNumericValue();
				if (number == null) return false;

				if (number == SessionUser.RedirectNumber)
				{
					SendAscii($"\r\n{LngText(LngKeys.RedirectAlreadyActive)}");
					return true;
				}

				bool numberValid = _subcriberServer.CheckNumberIsValid(number.Value);
				if (!numberValid)
				{
					SendAscii($"\r\n{LngText(LngKeys.InvalidRedirectNumber)}");
					return true;
				}

				DispatchMsg($"cmd redirect {number}");
				ConfirmationItem confItem = new ConfirmationItem()
				{
					UserId = SessionUser.UserId,
					Type = (int)ConfirmationTypes.Redirect,
					Number = number.Value,
					Pin = CommonHelper.CreatePin(),
					Language = ConnectionLanguage.ShortName,
					Sent = false,
					Finished = false,
					CreateTimeUtc = DateTime.UtcNow,
				};
				_database.ConfirmationsInsert(confItem);

				//_sendRedirectionPin = true;
				SendAscii($"\r\n{LngText(LngKeys.SendRedirectConfirmPin, number.Value.ToString())}");
				return true;
			}
			else if (tokens[1].TokenType == TokenTypes.Off)
			{
				// redirect off
				UserItem userItem = _database.UserLoadById(SessionUser.UserId);
				userItem.RedirectNumber = null;
				_database.UserUpdate(userItem);
				SessionUser = userItem;
				SendAscii($"\r\n{LngText(LngKeys.RedirectInactive)}");
				DispatchMsg($"cmd redirect off");
				return true;
			}
			return false;
		}

		private bool CmdSetTimezone(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;

			if (tokens[1].TokenType == TokenTypes.ArgInt)
			{
				int? timezone = tokens[1].GetNumericValue();
				if (timezone == null) return false;
				if (timezone < -12 || timezone> 14)
				{
					SendAscii($"\r\n{LngText(LngKeys.InvalidTimezone)}");
					return true;
				}

				DispatchMsg($"cmd timezone {timezone}");
				UserItem userItem = _database.UserLoadById(SessionUser.UserId);
				userItem.Timezone = timezone.Value;
				_database.UserUpdate(userItem);
				SessionUser = userItem;

				SendAscii($"\r\n{LngText(LngKeys.ActualTimezone, timezone.Value.ToString())}");
				return true;
			}
			return false;
		}

		private bool CmdSetFormat(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;
			if (tokens[1].TokenType != TokenTypes.ArgMsgFormat) return false;

			MsgFormatItem msgFormItem = (MsgFormatItem)tokens[1].Value;
			MsgFormatItem formatItem = UserItem.GetMsgFormat(msgFormItem.MsgFormat);
			if (formatItem == null) return false;

			DispatchMsg($"cmd format {formatItem.Name}");
			UserItem userItem = _database.UserLoadById(SessionUser.UserId);
			userItem.MsgFormat = (int)formatItem.MsgFormat;
			_database.UserUpdate(userItem);
			SessionUser = userItem;

			string formatStr = formatItem.MsgFormat == MsgFormats.Standard ? LngText(LngKeys.MsgFormatStandard) : LngText(LngKeys.MsgFormatShort);
			string msg = LngText(LngKeys.ActualMsgFormat, formatStr);
			SendAscii($"\r\nmsg");
			return true;
		}

		private bool CmdSetMaxPendingMsgs(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;
			if (tokens[1].TokenType != TokenTypes.ArgInt) return false;

			int msgCnt = tokens[1].GetNumericValue().Value;
			if (msgCnt < 0 || msgCnt > Constants.MAX_PENDING_NEWS) return false;

			DispatchMsg($"cmd max {msgCnt}");
			UserItem userItem = _database.UserLoadById(SessionUser.UserId);
			userItem.MaxPendingNews = msgCnt;
			_database.UserUpdate(userItem);
			SessionUser = userItem;

			SendAscii($"\r\n{LngText(LngKeys.SettingMaxPendMsgs, msgCnt.ToString())}");
			return true;
		}

		private bool CmdSetNewPin(List<CmdTokenItem> tokens)
		{
			UserItem userItem = _database.UserLoadById(SessionUser.UserId);

			// old pin

			SendAscii(CodeManager.ASC_COND_NL);
			InputResult inputResult = InputPin($"{LngText(LngKeys.EnterOldPin)}:", 5);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return true;

			string oldPin = inputResult.InputString;
			if (oldPin != userItem.Pin)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.WrongPin));
				return true;
			}

			// new pin

			SendAscii(CodeManager.ASC_COND_NL);
			inputResult = InputPin($"{LngText(LngKeys.EnterNewPin)}:", 5);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return true;

			string newPin = inputResult.InputString;
			if (newPin == oldPin)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.PinNotChanged));
				return true;
			}
			if (!CommonHelper.IsValidPin(newPin))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidNewPin));
				return true;
			}

			// new pin again

			SendAscii(CodeManager.ASC_COND_NL);
			inputResult = InputPin($"{LngText(LngKeys.EnterNewPinAgain)}:", 5);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return true;

			string newPin2 = inputResult.InputString;
			if (!CommonHelper.IsValidPin(newPin2))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidNewPin));
				return true;
			}
			if (newPin2 != newPin)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.PinsNotEqual));
				return true;
			}

			// change pin

			userItem.Pin = newPin;
			if (!_database.UserUpdate(userItem))
			{
				Log(LogTypes.Error, nameof(CmdSetNewPin), "error changing pin in database.");
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.PinNotChanged));
				return true;
			}
			SessionUser = userItem;

			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.PinChanged));

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
			if (!_database.ConfirmationsInsert(confItem))
			{
				Log(LogTypes.Error, nameof(CmdSetNewPin), "error creating notification.");
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
			}

			return true;
		}

		private bool CmdSettings(List<CmdTokenItem> tokens)
		{
			// number
			SendAscii($"\r\n{LngText(LngKeys.SettingNumber, SessionUser.ItelexNumber.ToString())}");

			// pending msgs
			SendAscii($"\r\n{LngText(LngKeys.SettingPending, _database.MsgStatusGetPendingCount(SessionUser.UserId).ToString())}");

			// max. pending msgs
			SendAscii($"\r\n{LngText(LngKeys.SettingMaxPendMsgs, SessionUser.MaxPendingNews.ToString())}");

			// hours
			int fromHours = 0;
			int toHours = 24;
			if (SessionUser.SendFromHour != null && SessionUser.SendToHour != null)
			{
				fromHours = SessionUser.SendFromHour.Value;
				toHours = SessionUser.SendToHour.Value;
			}
			SendAscii($"\r\n{LngText(LngKeys.SettingHours, fromHours.ToString(), toHours.ToString())}");

			// redirection
			string str = SessionUser.RedirectNumber != null ? SessionUser.RedirectNumber.Value.ToString() : LngText(LngKeys.No);
			SendAscii($"\r\n{LngText(LngKeys.SettingRedirect, str)}");

			// pause
			if (SessionUser.PauseUntilTimeUtc == null)
			{
				str = SessionUser.Paused ? LngText(LngKeys.Yes) : LngText(LngKeys.No);
			}
			else
			{
				str = $"bis {SessionUser.PauseUntilTimeUtc.Value.AddHours(SessionUser.Timezone)}";
			}
			SendAscii($"\r\n{LngText(LngKeys.SettingPaused, str)}");

			// msg format
			str = SessionUser.MsgFormat == 0 ? LngText(LngKeys.MsgFormatStandard) : LngText(LngKeys.MsgFormatShort);
			SendAscii($"\r\n{LngText(LngKeys.SettingMsgFormat, str)}");

			// timezone
			SendAscii($"\r\n{LngText(LngKeys.SettingTimezone, SessionUser.Timezone.ToString())}");

			return true;
		}

		private bool CmdHelp(List<CmdTokenItem> tokens)
		{
			if (tokens.Count == 2 && tokens[1].TokenType == TokenTypes.Local)
			{
				return CmdHelpLocal(tokens);
			}

			if (ConnectionLanguage.ShortName == "de")
			{
				//             12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\nhilfe / erlaubte befehle                            beispiel\n");
				SendAscii("\r\nliste           alle nachrichten-kanaele auflisten   lis");
				SendAscii("\r\nliste abonniert alle abonnierten kanaele auflisten   lis abo");
				SendAscii("\r\n+(kanalnr)      kanal abonnieren                     +21");
				SendAscii("\r\n-(kanalnr)      kanal abbestellen                    -21");
				SendAscii("\r\neinstellungen   eigene einstellungen anzeigen        ein");
				SendAscii("\r\nzeitzone (z)    aktuelle zeitzone einstellen         zei 1");
				SendAscii("\r\npause           nachrichtenversand pausieren         pau");
				SendAscii("\r\npause aus       pausieren aus                        pau aus");
				SendAscii("\r\nstunden (h) (h) zeitraum fuer nachrichtenversand     stu 8 21");
				SendAscii("\r\nstunden aus     zeitraum fuer nachrichtenversand aus stu aus");
				SendAscii("\r\numleitung (nr)  umleitung zu (nr) aktivieren         uml 12345");
				SendAscii("\r\numleitung aus   umleitung aus                        uml aus");
				SendAscii("\r\nformat (f)      nachr.-format, standard/kurz         for kur");
				SendAscii("\r\npin             pin aendern                          pin");
				SendAscii("\r\nende            verbindung beenden (oder st-taste)   end");
				SendAscii("\r\nhilfe lokal     zusaetzlich hilfe zu lokalen kanalen hil lok");
				SendAscii($"\r\n\nbei allen befehlen ist die eingabe der ersten {_commandInterpreter.MinLen} zeichen\r\nausreichend.");
			}
			else
			{
				//             12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\nhelp / allowed commands                             example\n");
				SendAscii("\r\nlist            list all news channels               lis");
				SendAscii("\r\nlist subscribed list all subscribed channels         lis sub");
				SendAscii("\r\n+(channelno)    subscribe channel                    +21");
				SendAscii("\r\n-(channelno)    unsubscribe channel                  -21");
				SendAscii("\r\nsettings        show own settings                    set");
				SendAscii("\r\ntimezone (t)    set current timezone                 tim 1");
				SendAscii("\r\npause           pause news distribution              pau");
				SendAscii("\r\npause off       pause off                            pau off");
				SendAscii("\r\nhours (h) (h)   time interval for distribution       hou 8 21");
				SendAscii("\r\nhours off       time interval for distribution off   hou off");
				SendAscii("\r\nredirect (no)   activate redirection to (no)         red 12345");
				SendAscii("\r\nredirect off    redirection off                      red off");
				SendAscii("\r\nformat (f)      msg format, standard/short           for sho");
				SendAscii("\r\npin             change pin                           pin");
				SendAscii("\r\nend             terminate connection (or st-key)     end");
				SendAscii("\r\nhelp local      additional help on local channels    hel loc");
				SendAscii($"\r\n\nfor all commands, entering the first {_commandInterpreter.MinLen} characters\r\nis sufficient.");
			}
			return true;
		}

		private bool CmdHelpLocal(List<CmdTokenItem> tokens)
		{
			DispatchMsg($"cmd help");
			if (ConnectionLanguage.ShortName == "de")
			{
				//             12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\nhilfe / erlaubte befehle fuer lokale kanaele        beispiel\n");
				SendAscii("\r\nliste lokal     alle lokalen kanaele auflisten       lis lok");
				SendAscii("\r\nneu lokal       neuen lokalen kanal anlegen          neu lok");
				SendAscii("\r\nauswaehl lokal  eigenen lokalen kanal auswaehlen     aus lok");
				SendAscii("\r\nbearbeite lokal eigenen lokalen kanal bearbeiten     bea lok");
				SendAscii("\r\nloeschen lokal  eigenen lokalen kanal loeschen       loe lok");
				SendAscii("\r\nnummern         abonnenten des ausgew. kanal anzeig. num");
				SendAscii("\r\nnummern (nr)    abonnenten eines lok. kanals anzeig. num 75");
				SendAscii("\r\nsende lokal     eine nachricht in einen kanal senden sen lok");
			}
			else
			{
				//             12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\nhelp / allowed commands for local channels          example\n");
				SendAscii("\r\nlist local      list all local channels              lis loc");
				SendAscii("\r\nnew local       create a new local channel           new loc");
				SendAscii("\r\nselect local    select own local channel             sel loc");
				SendAscii("\r\nedit local      edit own local channel               edi loc");
				SendAscii("\r\ndelete local    delete own local channel             del loc");
				SendAscii("\r\nnumbers         show subscribers of select. channel  num");
				SendAscii("\r\nnumbers (no)    show subscribers for a loc. channel  num 75");
				SendAscii("\r\nsend local      send a message into a localchannel   sen loc");
			}
			return true;
		}


		private void ShowChannels(List<UserChannel> userChannels, bool local)
		{
			string msg = "";

			foreach (UserChannel userChannel in userChannels)
			{
				if (local)
				{
					msg += GetLocalChannelLine(userChannel);
				}
				else
				{
					msg += GetChannelLine(userChannel);
				}
			}

			StartInputGegenschreiben();
			SendAscii(msg);

			WaitAllSendBuffersEmpty(true, "ex");
			if (_inputGegenschreiben)
			{
				SendAscii($"\r\n{LngText(LngKeys.Aborted)}");
			}
		}

		private string GetChannelLine(UserChannel userChannel)
		{
			string idStr = string.Format("{0,3:D}", userChannel.ChannelId);
			//string subsStr = string.Format("{0,-2}", userChannel.Subscribed ? userChannel.Content : "");
			string subsStr = string.Format("{0,-1}", userChannel.Subscribed ? "+" : "");
			string catStr = string.Format("{0,-7}", userChannel.Category);
			string lngStr = string.Format("{0,-2}", userChannel.Language);
			//return $"\r\n{idStr} {subsStr} {catStr} {lngStr}  {userChannel.GetConvName()} ({userChannel.SubscribeCount}/{userChannel.NewsCount})";
			return $"\r\n{idStr} {subsStr} {catStr} {lngStr}  {userChannel.GetConvName()} ({userChannel.NewsCount})";
		}

		private string GetLocalChannelLine(UserChannel userChannel)
		{
			string locPub;
			if (userChannel.Hidden)
			{
				locPub = "loc/hid";
			}
			else if (userChannel.IsPublic)
			{
				locPub = "loc/pub";
			}
			else
			{
				locPub = "loc";
			}

			string idStr = string.Format("{0,3:D}", userChannel.ChannelId);
			string subsStr = string.Format("{0,-1}", userChannel.Subscribed ? "+" : "");
			string locPubStr = string.Format("{0,-7}", locPub);
			//string catStr = string.Format("{0,-7}", userChannel.Category);
			string lngStr = string.Format("{0,-2}", userChannel.Language);
			string ownerStr = string.Format("{0,-8:D}", userChannel.LocalOwner);
			//return $"\r\n{idStr} {subsStr} {catStr} {lngStr}  {userChannel.GetConvName()} ({userChannel.SubscribeCount}/{userChannel.NewsCount})";
			return $"\r\n{idStr} {subsStr} {locPubStr} {lngStr} {ownerStr} {userChannel.GetConvName()} ({userChannel.NewsCount})";
		}

		private int? InputTimezone(string text, int? defaultTz)
		{
			for (int i = 0; i < 3; i++)
			{
				string defaultStr = defaultTz != null ? defaultTz.ToString() : "";
				InputResult inputResult = InputString($"{text}:", ShiftStates.Figs, defaultStr, 3, 1);
				if (inputResult.ErrorOrTimeoutOrDisconnected) continue;
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

		private int? InputChannelNumber(int channelCnt)
		{
			InputResult inputResult = InputNumber("kanal-nr: ", null, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return null;
			if (inputResult.InputNumber < 1 || inputResult.InputNumber > channelCnt)
			{
				SendAscii("\r\nungueltige kanalnummer\r\n");
				return null;
			}
			return inputResult.InputNumber - 1;
		}

		private ChannelItem _selectedLocalChannel;

		private bool CmdListLocalChannels(List<CmdTokenItem> tokens)
		{
			List<UserChannel> userChannels = _newsManager.GetUserChannels(SessionUser.UserId, ChannelTypes.Local, null, null);
			userChannels = (from u in userChannels where !u.Hidden || u.LocalOwner == RemoteNumber select u).ToList();
			if (userChannels.Count == 0)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.NoLocalChannels));
				return true;
			}
			userChannels.Sort(new ChannelIdComparer());
			SendAscii($"\r\n{LngText(LngKeys.LocalChannels)}:");
			ShowChannels(userChannels, true);
			return true;
		}

		private bool CmdSelectLocalChannel(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2 && tokens.Count != 3) return false;
			if (tokens[1].TokenType != TokenTypes.Local) return false;

			int channelId;
			if (tokens.Count == 2)
			{
				InputResult result = InputString($"{LngText(LngKeys.LocalChannelNo)}:", ShiftStates.Figs, null, 5, 1);
				if (result.ErrorOrTimeoutOrDisconnected) return false;
				if (!int.TryParse(result.InputString, out int value))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidChannelNo, result.InputString));
					return true;
				}
				channelId = value;
			}
			else
			{
				channelId = tokens[2].GetNumericValue().Value;
			}
			ChannelItem channel = _database.ChannelLoadById(channelId);
			if (channel == null)
			{
				SendAscii(LngText(CodeManager.ASC_COND_NL + LngKeys.ChannelNotFound, channelId.ToString()));
				return true;
			}

			if (channel.ChannelType != ChannelTypes.Local)
			{
				SendAscii(LngText(CodeManager.ASC_COND_NL + LngKeys.NotALocalChannel, channel.IdAndName));
				return true;
			}

			if (channel.LocalOwner != RemoteNumber)
			{
				InputResult result = InputPin($"{LngText(LngKeys.Pin)}:", 5);
				if (result.ErrorOrTimeoutOrDisconnected) return false;
				if (result.InputString.Trim() != channel.LocalPin)
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.WrongPin));
					return true;
				}
			}

			_selectedLocalChannel = channel;
			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.ChannelSelected, _selectedLocalChannel.IdAndName));
			return true;
		}

		private bool CmdNewLocalChannel(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2 && tokens.Count != 3) return false;
			if (tokens[1].TokenType != TokenTypes.Local) return false;

			string name = null;
			if (tokens.Count == 3)
			{
				name = tokens[2].GetStringValue();
			}

			ChannelItem channel = new ChannelItem()
			{
				Name = name,
				LocalOwner = SessionUser.ItelexNumber,
				Type = ChannelItem.GetChannelTypeName(ChannelTypes.Local),
				Category = ChannelItem.GetCategoryName(ChannelCategories.Local),
				Language = null,
				Url = null,
				Active = true,
			};
			if (!EditLocalChannel(channel, false)) return true;

			DateTime utcNow = DateTime.UtcNow;
			channel.CreateTimeUtc = utcNow;
			channel.LastChangedTimeUtc = utcNow;

			if (!_database.ChannelInsert(channel))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
				return true;
			}

			_selectedLocalChannel = channel;
			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.ChannelCreatedAndSelected, channel.IdAndName));
			return true;
		}

		private bool CmdEditSelectedLocalChannel(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;
			if (tokens[1].TokenType != TokenTypes.Local) return false;

			if (_selectedLocalChannel == null)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				SendAscii(LngText(LngKeys.NoChannelSelected));
				return true;
			}

			if (_selectedLocalChannel.LocalOwner != RemoteNumber)
			{
				InputResult result = InputPin($"{LngText(LngKeys.Pin)}:", 5);
				if (result.ErrorOrTimeoutOrDisconnected) return false;
				if (result.InputString.Trim() != _selectedLocalChannel.LocalPin)
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.WrongPin));
					return true;
				}
			}

			if (!EditLocalChannel(_selectedLocalChannel, true)) return true;

			_selectedLocalChannel.LastChangedTimeUtc = DateTime.UtcNow;
			if (!_database.ChannelUpdate(_selectedLocalChannel))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
				return true;
			}
			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.ChannelDataChanged));
			return true;
		}

		private bool CmdDeleteSelectedLocalChannel(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;
			if (tokens[1].TokenType != TokenTypes.Local) return false;

			if (_selectedLocalChannel == null)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.NoChannelSelected));
				return true;
			}

			if (_selectedLocalChannel.LocalOwner != RemoteNumber)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.ChannelDeleteNotAllowed));
				return true;
			}

			int memberCount = _database.SubscriptionsLoad(_selectedLocalChannel.ChannelId, null).Count;
			string msg = LngText(LngKeys.DeleteChannelWithSubscriptions, 
					_selectedLocalChannel.IdAndName, memberCount.ToString());
			InputResult inputResult = InputYesNo($"\r\n{msg} ?", null, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return true;
			if (inputResult.InputBool == true)
			{
				if (!_database.ChannelDeleteById(_selectedLocalChannel.ChannelId))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
					return true;
				}
				SendAscii(CodeManager.ASC_COND_NL);
				SendAscii(LngText(LngKeys.ChannelDeleted, _selectedLocalChannel.IdAndName));
			}
			return true;
		}

		private bool CmdListNumbersOfSelectedChannel(List<CmdTokenItem> tokens)
		{
			long channelId;
			if (tokens.Count == 1)
			{
				if (_selectedLocalChannel == null)
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.NoChannelSelected));
					return true;
				}
				channelId = _selectedLocalChannel.ChannelId;
			}
			else
			{
				channelId = tokens[1].GetNumericValue().Value;
			}

			ChannelItem channel;
			if (_selectedLocalChannel != null && _selectedLocalChannel.ChannelId == channelId)
			{
				channel = _selectedLocalChannel;
			}
			else
			{
				channel = _database.ChannelLoadById(channelId);
				if (channel == null)
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.ChannelNotFound, channelId.ToString()));
					return true;
				}
			}

			if (channel.ChannelType != ChannelTypes.Local || !channel.IsPublic)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.ShowNumbersNotAllowed, channel.IdAndName));
				return true;
			}

			List<int> numbersInChannel = GetNumbersForChannel(channel);
			if (numbersInChannel.Count == 0)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				SendAscii(LngText(LngKeys.NoNumbersInChannel, channel.IdAndName));
				return true;
			}

			SendAscii($"{LngText(LngKeys.ChannelHeader, channel.IdAndName)}:");
			for (int i = 0; i < numbersInChannel.Count; i++)
			{
				SendAscii($"\r\n{i + 1:D2} {numbersInChannel[i]}");
			}
			return true;
		}

		private bool CmdSendLocalMessage(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2 && tokens.Count != 3) return false;
			if (tokens[1].TokenType != TokenTypes.Local) return false;

			InputResult inputResult;
			long channelId;
			if (tokens.Count == 2)
			{
				if (_selectedLocalChannel != null)
				{
					channelId = _selectedLocalChannel.ChannelId;
				}
				else
				{
					inputResult = InputString($"{LngText(LngKeys.LocalChannelNo)}:", ShiftStates.Figs, null, 4, 1);
					if (inputResult.ErrorOrTimeoutOrDisconnected) return false;
					if (!int.TryParse(inputResult.InputString, out int value))
					{
						SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidChannelNo, inputResult.InputString));
						return true;
					}
					channelId = value;
				}
			}
			else
			{
				channelId = tokens[2].GetNumericValue().Value;
			}

			ChannelItem channel;
			if (_selectedLocalChannel != null && _selectedLocalChannel.ChannelId == channelId)
			{
				channel = _selectedLocalChannel;
			}
			else
			{
				channel = _database.ChannelLoadById(channelId);
				if (channel == null)
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.ChannelNotFound, channelId.ToString()));
					return true;
				}
			}

			if (channel.ChannelType != ChannelTypes.Local)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.NotALocalChannel, channel.IdAndName));
				return true;
			}

			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.SendToChannel, channel.IdAndName));

			if (!channel.IsPublic && channel.LocalOwner != RemoteNumber)
			{
				InputResult result = InputPin($"{LngText(LngKeys.Pin)}:", 5);
				if (result.ErrorOrTimeoutOrDisconnected) return false;
				if (result.InputString.Trim() != channel.LocalPin)
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.WrongPin));
					return true;
				}
			}

			SendAscii(CodeManager.ASC_COND_NL);
			inputResult = InputYesNo($"{LngText(LngKeys.SendACopy, RemoteNumber.ToString())}:", null, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return true;
			bool sendACopy = inputResult.InputBool;

			List<int> numbersInChannel = GetNumbersForChannel(channel);
			if (!sendACopy)
			{
				numbersInChannel.Remove(RemoteNumber.Value);
			}

			if (numbersInChannel.Count == 0)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				inputResult = InputYesNo($"{LngText(LngKeys.NoSubscribersWhenSending)}:", null, 1);
				if (inputResult.ErrorOrTimeoutOrDisconnected) return true;
				if (inputResult.InputBool == false) return true;
			}

			SendAscii(CodeManager.ASC_COND_NL + $"{LngText(LngKeys.InputMessage)}:\r\n");

			_inputLine = "";
			_inputActive = true;
			string text = GetMultiLineMessage("");
			_inputActive = false;
			if (!IsConnected) return true;
			if (string.IsNullOrWhiteSpace(text)) return false;

			SendAscii(CodeManager.ASC_COND_NL);
			inputResult = InputYesNo($"\r\n{LngText(LngKeys.SendMessage)} ?", null, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return true;
			if (inputResult.InputBool == false) return true;

			// send message here
			NewsManager.Instance.SendMessageToChannel(channel, SessionUser.ItelexNumber.ToString(), text, 
					sendACopy ? null : RemoteNumber);

			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.MessageSent));
			return true;
		}

		private bool EditLocalChannel(ChannelItem channel, bool edit)
		{
			InputResult inputResult;
			bool valid = false;
			for (int i = 0; i < 3; i++)
			{
				string name = channel.Name;
				SendAscii(CodeManager.ASC_COND_NL);
				inputResult = InputString($"{LngText(LngKeys.LocalChannelName)}:", ShiftStates.Ltrs, name, 20, 1);
				if (inputResult.ErrorOrTimeoutOrDisconnected) return false;
				name = inputResult.InputString;
				ChannelItem ch = _database.ChannelLoadByName(name);
				if (edit && name == channel.Name || ch == null)
				{
					channel.Name = name;
					valid = true;
					break;
				}
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.ChannelNameExists, name));
			}
			if (!valid) return false;

			string isPublic = channel.IsPublic ? LngText(LngKeys.Y) : LngText(LngKeys.N);
			SendAscii(CodeManager.ASC_COND_NL);
			inputResult = InputYesNo($"{LngText(LngKeys.IsChannelPublic)} ?", isPublic, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return false;
			channel.LocalPublic = inputResult.InputBool;

			valid = false;
			for (int i = 0; i < 3; i++)
			{
				string lng = channel.Language;
				string validLngs = GetLanguages();
				SendAscii(CodeManager.ASC_COND_NL);
				inputResult = InputString($"{LngText(LngKeys.ChannelLanguage)} ({validLngs}):", ShiftStates.Ltrs, lng, 3, 1);
				if (inputResult.ErrorOrTimeoutOrDisconnected) return false;
				if (string.IsNullOrEmpty(inputResult.InputString))
				{
					// no language
					valid = true;
					channel.Language = null;
					break;
				}
				ChannelLanguageItem lngItem = ChannelItem.GetLanguage(inputResult.InputString);
				if (lngItem != null)
				{
					valid = true;
					channel.Language = lngItem.Name;
					break;
				}
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidLanguage, inputResult.InputString));
			}
			if (!valid) return false;

			valid = false;
			for (int i = 0; i < 3; i++)
			{
				string pin = channel.LocalPin;
				SendAscii(CodeManager.ASC_COND_NL);
				inputResult = InputPin($"{LngText(LngKeys.Pin)}:", 5, pin);
				if (inputResult.ErrorOrTimeoutOrDisconnected) return false;
				if (CommonHelper.IsValidPin(inputResult.InputString))
				{
					valid = true;
					channel.LocalPin = string.IsNullOrEmpty(inputResult.InputString) ? null : inputResult.InputString;
					break;
				}
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidChannelPin, inputResult.InputString));
			}
			return valid;
		}

		private List<int> GetNumbersForChannel(ChannelItem channel)
		{
			List<SubscriptionItem> subs = _database.SubscriptionsLoad(channel.ChannelId, null);
			List<UserItem> users = _database.UserLoadAllActive();
			List<int> numbers = new List<int>();
			foreach (SubscriptionItem sub in subs)
			{
				UserItem user = (from u in users where u.UserId == sub.UserId select u).FirstOrDefault();
				if (user != null)
				{
					numbers.Add(user.ItelexNumber);
				}
			}
			return SortItelexNumbers(numbers);
		}

		private string GetLanguages()
		{
			string lngs = "";
			foreach(ChannelLanguageItem lng in ChannelItem.Languages)
			{
				if (!string.IsNullOrEmpty(lngs)) lngs += "/";
				lngs += lng.Name;
			}
			return lngs;
		}

		private List<int> SortItelexNumbers(List<int> numbers)
		{
			List<string> strNumbers = (from n in numbers select n.ToString()).ToList();
			strNumbers.Sort();
			return (from s in strNumbers select int.Parse(s)).ToList();
		}

		//private bool _inputActive;
		//private string _inputLine;
		//private object _inputLineLock = new object();

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


		/**
		 * receive characters for message upload
		 */
		private void SubscribeConnection_Received(ItelexConnection connection, string asciiText)
		{
			if (_inputActive)
			{
				lock (_inputLineLock)
				{
					_inputLine += asciiText;
				}
			}
		}

		private string FormatTimezone(int tz)
		{
			if (tz >= 0) return $"utc+{tz}";
			return $"utc{tz}";
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
