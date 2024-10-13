using ItelexCommon;
using ItelexCommon.Commands;
using ItelexCommon.Connection;
using ItelexCommon.Utility;
using ItelexRundsender.Commands;
using ItelexRundsender.Data;
using ItelexRundsender.Languages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using static ItelexRundsender.Connections.CallNumberResult;
using static System.Windows.Forms.LinkLabel;

namespace ItelexRundsender.Connections
{
	enum NumberProperty
	{
		None,
		Comma,
		EndOfNumbers,
		EndOfNumbersWithText,
		EndOfNumbersNoCheck,
		EndOfNumbersDelayed,
		EndOfMessage,
		CmdWithReceiverList,
		CmdDelayed,
		CmdHelp,
		NumberCorrection,
		NumberRemove,
	}

	enum RundsendeModus
	{
		None,
		Direct,
		Deferred
	}

	class IncomingConnection : ItelexIncoming, IDisposable
	{
		private OutgoingConnectionManager _outgoingConnectionManager;

		private bool _inputActive;
		private string _inputLine;
		private object _inputLineLock = new object();

		public IncomingConnection()
		{
		}

		public IncomingConnection(TcpClient client, int idNumber, ItelexLogger itelexLogger) : 
			base(client, idNumber, itelexLogger)
		{
			TAG = nameof(IncomingConnection);
	
			_database = RundsenderDatabase.Instance;
			_subcriberServer = new SubscriberServer();
			_commandInterpreter = new CommandInterpreter();
			_outgoingConnectionManager = (OutgoingConnectionManager)GlobalData.Instance.OutgoingConnectionManager;

			_inputActive = false;
			this.ItelexReceived += RundsenderConnection_Received;
			InitTimerAndHandler();
		}

		~IncomingConnection()
		{
			Dispose(false);
		}

		private enum InputMode { Numbers, EndOfNumbers, Message };

		public override void Start()
		{
			try
			{
				StartEx();
			}
			catch(Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Start), $"error", ex);
				Logoff(null);
				return;
			}
		}

		public void StartEx()
		{
			ItelexIncomingConfiguration config = new ItelexIncomingConfiguration
			{
				OurPrgmVersionStr = Helper.GetVersionCode(),
				OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
				GetLngText = GetLngText,
				LoginSequences = new AllLoginSequences(
					new LoginSequenceForExtensions(new int[] { 11, 12 }, LoginSeqTypes.SendTime, LoginSeqTypes.SendKg, LoginSeqTypes.GetKg,
						LoginSeqTypes.GetNumber), // standard login
					new LoginSequenceForExtensions(new int[] { 13, 14 }, LoginSeqTypes.SendTime, LoginSeqTypes.SendKg, LoginSeqTypes.GetKg,
						LoginSeqTypes.GetNumberAndPin) // admin login with pin
				),
				ItelexExtensions = GlobalData.Instance.ItelexValidExtensions,
				LoadLoginItem = LoginItemLoad,
				UpdateLoginItem = LoginItemUpdate,
				AddAccount = LoginItemAddAccount,
				SendNewPin = LoginItemSendNewPin,

				LngKeyMapper = new Dictionary<IncomingTexts, int>()
				{
					{ IncomingTexts.ConfirmOwnNumber, (int)LngKeys.ConfirmOwnNumber},
					//{ IncomingTexts.InternalError, (int)LngKeys.InternalError},
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

			if (!RemoteNumber.HasValue)
			{
				Logoff(null);
				return;
			}

			ConnectionLanguage = LanguageManager.Instance.GetLanguageByShortname(_extension.Language);

			if (ExtensionNumber.Value == Constants.EXTNUM_ADMIN_DE || ExtensionNumber.Value == Constants.EXTNUM_ADMIN_EN)
			{
				GroupAdministration();
				Logoff(null);
				return;
			}

			SendProperties sendProps = new SendProperties();
			sendProps.CallerLanguageId = (LanguageIds)ConnectionLanguage.Id;
			sendProps.CallerNumber = RemoteNumber.Value;
			sendProps.CallerAnswerbackStr = RemoteAnswerback != null ? RemoteAnswerback.Name : null;

			//_inputEndChars = new char[] { CodeManager.ASC_CR, CodeManager.ASC_LF };

			RundsendeModus? rundsendMode = GetRundsendeModus();
			if (rundsendMode == null)
			{
				Logoff(null);
				return;
			}

			sendProps.SendMode = rundsendMode.Value;
			bool delayed = false;

			if (sendProps.SendMode == RundsendeModus.Direct)
			{
				_inputLine = "";
				_inputActive = true;

				// TODO: erneute Abfrage, wenn keine Receiver
				ParseNumbersResult numberResult = GetReceiverNumbers($"{LngText(LngKeys.EnterDestNumbers)}",
						out string remainingInput, sendProps.SendMode);
				if (numberResult == null || numberResult.Error)
				{
					Error();
					return;
				}

				sendProps.Receivers = numberResult.Receivers;
				sendProps.IncludeReceiverList = numberResult.WithRecvList;

				_inputActive = false;
				if (!IsConnected) return;

				CallReceiversDirect(sendProps);
				/*
				if (sendProps.SuccessReivers.Count == 0)
				{
					DispatchMsg("no dest number");
					SendAscii("\r\nkeine zielnummern.");
					Logoff(true);
					return;
				}
				*/

				if (sendProps.SuccessReivers.Count < sendProps.Receivers.Count)
				{
					SendAscii($"\r\n{LngText(LngKeys.SomeDestNumbersNotAvailable)}");
					InputResult inputResult = InputYesNo($"\r\n{LngText(LngKeys.SendThemDeferred)}", null, 3);
					if (inputResult.ErrorOrTimeoutOrDisconnected)
					{
						Error();
						return;
					}
					delayed = inputResult.InputBool;
					if (sendProps.SuccessReivers.Count == 0 && !delayed)
					{
						DispatchMsg("no dest number");
						SendAscii($"\r\n{LngText(LngKeys.NoDestNumbers)}\r\n");
						Logoff(null);
						return;
					}
				}

				if (sendProps.IncludeReceiverList)
				{
					string recvLines = sendProps.GetReceiverLines(68, $"{LngText(LngKeys.ReceiverPrefix)} ");
					_outgoingConnectionManager.SendMessagesDirect(sendProps, recvLines);
				}

				SendAscii($"\r\n{LngText(LngKeys.PleaseSendMessage)}\r\n");
				sendProps.MessageText = GetAndSendMessageDirect(sendProps);

				if (!IsConnected)
				{
					Error();
					return;
				}

				ReadAnswerbackAndDisconnectDirect(sendProps);

				//WaitAllSendBuffersEmpty();
				//SendMessageToReceiversDirect(sendProps);
				WaitAllSendBuffersEmpty();

				DebugSaveMsg(sendProps);

				if (delayed)
				{
					WaitAllSendBuffersEmpty();
					SendAscii($"\r\n\n{LngText(LngKeys.TerminateConnectionAndTransmit)}\r\n");
					SendAscii("+++\r\n\n\n");
					Logoff(null);
				}
				else
				{
					Logoff(null);
				}
			}
			else
			{
				// zeitversetzt

				_inputLine = "";
				_inputActive = true;

				ParseNumbersResult numberResult = GetReceiverNumbers(
						$"{LngText(LngKeys.PleaseSendNumbersAndMessage)}",
						out string remainingInput, sendProps.SendMode);
				if (numberResult == null || numberResult.Error)
				{
					Error();
					return;
				}

				sendProps.Receivers = numberResult.Receivers;
				sendProps.IncludeReceiverList = numberResult.WithRecvList;

				string msg = GetMessageDelayed(remainingInput);
				if (string.IsNullOrWhiteSpace(msg))
				{
					SendAscii($"\r\n\n{LngText(LngKeys.ConnectionTerminated)}\r\n\n");
					Logoff(null);
					return;
				}

				msg = IncludeReceiver(msg, sendProps.GetReceiverLines(68, ""));
				sendProps.MessageText = TruncateMessage(msg, Constants.MAX_LINES);
				Thread.Sleep(2000);
				_inputActive = false;

				if (!IsConnected)
				{
					Error();
					return;
				}

				DebugSaveMsg(sendProps);

				SendAscii($"\r\n\n{LngText(LngKeys.TerminateConnectionAndTransmit)}\r\n\n");

				Logoff(null);
			}

			if (sendProps != null && sendProps.SendMode == RundsendeModus.Deferred)
			{
				_outgoingConnectionManager.SendDelayedAll(sendProps, false);
			}
			else if (delayed)
			{
				//sendProps.Receivers = new HashSet<Receiver>(sendProps.Receivers.Where(x => x.IsReject("occ")));
				_outgoingConnectionManager.SendDelayedAll(sendProps, true);
			}
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
			UserItem userItem = new UserItem()
			{
				ItelexNumber = itelexNumber,
				RegisterTimeUtc = utcNow,
				LastLoginTimeUtc = utcNow,
				LastPinChangeTimeUtc = utcNow,
				Kennung = RemoteAnswerbackStr,
				Active = true,
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
			string msg = LngText(LngKeys.SendNewPinMsg, number.ToString());
			SendAscii($"\r\n{msg}");
			DispatchMsg($"Logoff with send pin to {number}");
		}

		private void Error()
		{
			DispatchMsg("Disconnected");
			Disconnect(DisconnectReasons.Error);
			return;
		}

		public override void Logoff(string msg)
		{
			DispatchMsg("logoff");
			base.Logoff(msg);
		}

		private RundsendeModus? GetRundsendeModus()
		{
			//SendAscii("\r\n");
			while (true)
			{
				InputResult inputResult = InputSelection($"\r\n{LngText(LngKeys.ChooseSendMode)}", ShiftStates.Ltrs, "",
						new string[] { "d", "z", "t", "g", "?" }, 1, 3);
				if (inputResult.ErrorOrTimeoutOrDisconnected) return null;
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(GetRundsendeModus), $"mode = '{inputResult}'");

				switch (inputResult.InputString)
				{
					case "d":
						return RundsendeModus.Direct;
					case "z": // zeitversetzt
					case "t": // time-delayed
						return RundsendeModus.Deferred;
					case "g": // group info
						GroupInformation();
						break;
					case "?":
						SendHelpText();
						break;
					default:
						return null;
				}
			}
		}

		private void GroupInformation()
		{
			SendAscii(CodeManager.ASC_COND_NL + $"{LngText(LngKeys.GroupInfo)}:");
			while (true)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				InputResult inputResult = InputString($"\r\n{LngText(LngKeys.GroupName)}:", ShiftStates.Ltrs, null, 20, 1);
				if (inputResult.ErrorOrTimeoutOrDisconnected) return;
				string name = inputResult.InputString.Trim();
				if (string.IsNullOrEmpty(name)) return;

				if (!IsValidGroupAndIsMember(name))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.GroupNotFound, name));
					continue;
				}

				GroupItem group = GetGroupByName(name);
				List<GroupMemberItem> members = GetSortedNumbersForGroup(group);
				SendAscii(CodeManager.ASC_COND_NL);
				foreach (GroupMemberItem member in members)
				{
					SendAscii($"\r\n{member.Number}");
					if (!string.IsNullOrEmpty(member.Name))
					{
						SendAscii($" {member.Name}");
					}
				}
			}
		}

		private ParseNumbersResult GetReceiverNumbers(string prompt, out string remainingInput, RundsendeModus modus)
		{
			ParseNumbersResult result;
			remainingInput = "";

			for (int i = 0; i < 3; i++)
			{
				if (!IsConnected) return null;

				string lastInputLine = _inputLine;
				bool showNumberPrompt = true;
				result = null;

				while (result == null)
				{
					Thread.Sleep(100);

					if (LastSendRecvTime.ElapsedSeconds > 120)
					{
						// 120 s timeout
						Logoff(null);
						DispatchMsg("timeout");
						return null;
					}

					if (!IsConnected) return null;

					if (showNumberPrompt)
					{
						SendAscii($"\r\n{prompt}\r\n");
						showNumberPrompt = false;
					}

					lock (_inputLineLock)
					{
						if (lastInputLine == _inputLine) continue; // no changes
						lastInputLine = _inputLine;
						if (!_inputLine.Contains("+")) continue; // no '+' received: go on

						remainingInput = _inputLine.ExtRightString("+");
						string numberStr = _inputLine.ExtLeftString("+");
						_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetReceiverNumbers), $"remainingInput = {remainingInput}");
						_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetReceiverNumbers), $"numberStr = {numberStr}");
						_inputLine = "";
						if (string.IsNullOrWhiteSpace(numberStr)) continue;

						result = ParseNumbers(numberStr);
						_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetReceiverNumbers), "recv numend (+)");

						//if (result == null) return new ParseNumbersResult(true);
					}
				}

				if (result.Receivers.Count == 0)
				{
					DispatchMsg("no dest number");
					if (modus == RundsendeModus.Direct)
					{
						SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.NoDestNumbers));
					}
					Logoff(null);
					return new ParseNumbersResult(true);
				}

				if (modus == RundsendeModus.Direct)
				{
					if (CheckNumbers(result)) return result;
				}
				else
				{
					// zeitversetzt
					return result;
				}
			}
			return null;
		}

		private ParseNumbersResult ParseNumbers(string inputStr)
		{
			ParseNumbersResult result = new ParseNumbersResult(false);

			inputStr = inputStr.Trim(' ');
			inputStr = inputStr.Replace("\r", ",");
			inputStr = inputStr.Replace("\n", ",");
			inputStr = inputStr.Trim();
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Start), $"inputStr = '{inputStr}'");

			List<Receiver> recvAdd = new List<Receiver>();
			List<Receiver> recvRemoves = new List<Receiver>();

			string[] parts = inputStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			// expand groups
			List<string> numbers = new List<string>();
			foreach(string part in parts)
			{
				char[] trimChr = new char[] { ' ', ',', '\r', '\n' };
				string numStr = part.Trim(trimChr);
				if (IsValidGroupAndIsMember(numStr))
				{	// group found, add numbers
					GroupItem group = GetGroupByName(numStr);
					List<GroupMemberItem> members = GetSortedNumbersForGroup(group);
					foreach(GroupMemberItem member in members)
					{
						if (member.Number != RemoteNumber)
						{
							numbers.Add(member.Number.ToString());
						}
					}
				}
				else
				{
					numbers.Add(numStr);
				}
			}

			foreach (string part in numbers)
			{
				string numStr = part;
				switch (numStr)
				{
					case "empf":
					case "recv":
						result.WithRecvList = true;
						continue;
				}

				if (Receiver.IsCorrection(numStr)) continue; // skip correction

				numStr = numStr.Replace(" ", "");
				bool remove = false;
				if (numStr.StartsWith("-"))
				{
					numStr = numStr.Substring(1);
					remove = true;
				}
				if (numStr.StartsWith("+"))
				{
					numStr = numStr.Substring(1);
				}

				Receiver recv;
				if (int.TryParse(numStr, out int num))
				{
					if (num > 0)
					{
						recv = new Receiver(num, remove);
						if (!remove)
						{
							if (!recvAdd.Where(i => i.Number == recv.Number).Any())
							{
								recvAdd.Add(recv);
							}
						}
						else
						{
							if (!recvRemoves.Where(i => i.Number == recv.Number).Any())
							{
								recvRemoves.Add(recv);
							}
						}
					}
				}
				else
				{
					//recv = new Receiver(0, remove);
					//if (!remove ) recvAdd.Add(recv);
				}
			}

			foreach (Receiver recvRem in recvRemoves)
			{
				Receiver rem = recvAdd.Where(i => i.Number == recvRem.Number).FirstOrDefault();
				if (rem != null)
				{
					recvAdd.Remove(rem);
				}
			}

			result.Receivers = new List<Receiver>();
			//int cnt = 0;
			foreach (Receiver num in recvAdd)
			{
				result.Receivers.Add(num);
				//if (++cnt >= 30) break;
			}

			return result;
		}

		private bool CheckNumbers(ParseNumbersResult parseResult)
		{
			List<int> num = parseResult.Receivers.Select(n => n.Number).ToList();
			string lines = SendProperties.GetReceiverLines(parseResult.Receivers, 68, null);
			SendAscii($"\r\n{LngText(LngKeys.DestNumbers)}\r\n{lines}");
			if (parseResult.WithRecvList) SendAscii($"\r\n{LngText(LngKeys.IncludeReceiverList)}");

			InputResult inputResult = InputYesNo($"\r\n{LngText(LngKeys.IsOk)}", null, 1);
			Thread.Sleep(500);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return false;
			return inputResult.InputBool;
		}

		private void CallReceiversDirect(SendProperties sendProps)
		{
			SendAscii($"\r\n{LngText(LngKeys.EstablishConnections)}");
			List<CallResult> callResults = _outgoingConnectionManager.CallReceiversDirect(sendProps);

			if (sendProps.SendMode == RundsendeModus.Deferred) return;

			foreach (CallResult result in callResults)
			{
				Receiver receiver = sendProps.Receivers.Where(n => n.Number == result.Number).FirstOrDefault();
				receiver.CallStatus = result.CallStatus;
				receiver.RejectReason = result.RejectReason;
				receiver.Connection = (OutgoingConnection)result.Connection;
				receiver.Kennung1 = result.Kennung?.Name;
				SendAscii($"\r\n{result.Number}: {receiver.ResultStr1}");
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(CallReceiversDirect), $"receiver status: {result.Number}: {receiver.ResultStr1}");
			}
			SendAscii($"\r\n");
		}

		private string GetAndSendMessageDirect(SendProperties sendProps)
		{
			string msg = "";
			string inputStr;
			int lineCnt = 0;

			_inputLine = "";
			_inputActive = true;

			try
			{

				while (true)
				{
					Thread.Sleep(100);
					if (!IsConnected) return "";

					lock (_inputLineLock)
					{
						inputStr = _inputLine;
						_inputLine = "";
						if (inputStr == "") continue;
						int p = inputStr.IndexOf("+++");
						if (p >= 0)
						{
							inputStr = _inputLine.ExtSubstring(0, p);
							_outgoingConnectionManager.SendMessagesDirect(sendProps, inputStr);
							_outgoingConnectionManager.SendMessagesDirect(sendProps, "\r\n+++\r\n\n");
							msg += inputStr;
							_inputActive = false;
							return msg;
						}
						int plusCnt = EndsWithPlusCnt(inputStr);
						if (plusCnt > 0)
						{
							inputStr = inputStr.ExtSubstring(0, inputStr.Length - plusCnt);
							_inputLine = new string('+', plusCnt);
						}
					}
					_outgoingConnectionManager.SendMessagesDirect(sendProps, inputStr);
					msg += inputStr;
					lineCnt += MessageCountLines(inputStr);
					if (lineCnt > Constants.MAX_LINES)
					{
						string truncMsg = $"\r\n{LngText(LngKeys.MsgTooLong)}\r\n+++\r\n\n\n";
						_outgoingConnectionManager.SendMessagesDirect(sendProps, truncMsg);
						_inputActive = false;
						return msg += truncMsg;
					}
				}
			}
			catch(Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(GetAndSendMessageDirect), "error", ex);
				return "";
			}
		}

		private int EndsWithPlusCnt(string msg)
		{
			if (string.IsNullOrEmpty(msg)) return 0;

			int cnt = 0;
			for (int i=msg.Length-1; i >=0; i--)
			{
				if (msg[i] != '+') return cnt;
				cnt++;
			}
			return cnt;
		}

		private void ReadAnswerbackAndDisconnectDirect(SendProperties sendProps)
		{
			SendAscii($"\r\n\n{LngText(LngKeys.CreateTransmissionReport)}");
			List<OutgoingConnection> connections = sendProps.GetConnections();
			List<CallResult> sendResults = _outgoingConnectionManager.ReadAnswerbackAndDisconnectDirect(sendProps);
			if (sendResults == null) return; // error
			foreach (CallResult result in sendResults)
			{
				Receiver recv = sendProps.Receivers.Where(n => n.Number == result.Number).FirstOrDefault();
				recv.CallStatus = result.CallStatus;
				recv.Kennung2 = result.Kennung?.Name;
				string okStr = result.CallStatus == CallStatusEnum.Ok ? $"({LngText(LngKeys.ReportOk)})" : "";
				//string resultStr = receiver.ResultStr2 == CallResult.CR_QUERYERROR ? LngText(LngKeys.ReportError) : receiver.ResultStr2;
				string resultStr = recv.CallStatus == CallStatusEnum.Ok ?
					recv.ResultStr2 :
					CallResult.CallStatusToString(recv.CallStatus, recv.RejectReason, sendProps.CallerLanguageStr);
				SendAscii(string.Format("\r\n{0,-9} {1} {2}", recv.Number.ToString() + ":", resultStr, okStr));
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ReadAnswerbackAndDisconnectDirect), $"kg status: {result.Number}: {recv.ResultStr2}");
			}
			SendAscii($"\r\n");
		}

		private string GetMessageDelayed(string remainungInput)
		{
			while (true)
			{
				Thread.Sleep(100);
				if (!IsConnected) return ""; // disconnected
				if (LastSendRecvTime.IsElapsedSeconds(60 * 5))
				{
					// timeout
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetMessageDelayed), "recv timeout");
					return "";
				}

				lock (_inputLineLock)
				{
					if (_inputLine == "") continue;
					int p = _inputLine.IndexOf("+++");
					if (p >= 0)
					{
						string msg = remainungInput + _inputLine.Substring(0, p);
						msg = ParseMessage(msg);
						_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetMessageDelayed), "recv textend (+++)");
						return msg;
					}
				}
			}
		}

		private string IncludeReceiver(string msg, string recvLines)
		{
			return msg.Replace(":empfaenger:", recvLines);
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

		private string TruncateMessage(string text, int linesMax)
		{
			if (string.IsNullOrEmpty(text)) return "";

			StringBuilder sb = new StringBuilder();
			int lineCnt = 0;
			foreach (char chr in text)
			{
				if (chr == '\n')
				{
					lineCnt++;
					if (lineCnt > linesMax) break;
				}
				sb.Append(chr);
			}
			if (lineCnt > linesMax)
			{
				sb.Append($"\r\n{LngText(LngKeys.MsgTooLong)}\r\n");
			}
			return sb.ToString();
		}

		private int MessageCountLines(string text)
		{
			int lineCnt = 0;
			foreach (char chr in text)
			{
				if (chr == '\n') lineCnt++;
			}
			return lineCnt;
		}

		private void SendHelpText()
		{
			SendAscii("\r\n");
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(SendHelpText), $"send help file");
			string fullName = null;
			string filename = $"rundsendehilfe_{ConnectionLanguage.ShortName}.txt";
			try
			{
				fullName = Path.Combine(FormsHelper.GetExePath(), filename);
				string hilfeText = File.ReadAllText(fullName);
				SendAscii("\r\n");
				SendAscii(hilfeText);
			}
			catch(Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(SendHelpText), $"help file {fullName} is missing", ex);
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.NoHelpAvailable));
			}
			WaitAllSendBuffersEmpty();
		}


		//private Random _random = new Random();
		private RundsenderDatabase _database;
		private SubscriberServer _subcriberServer;
		private CommandInterpreter _commandInterpreter;
		private UserItem SessionUser;
		private GroupItem _selectedGroup = null;

		private void GroupAdministration()
		{
			//ConnectionLanguage = LanguageManager.Instance.GetLanguageByShortname(_extension.Language);
			SessionUser = _database.UserLoadByTelexNumber(RemoteNumber.Value);

			ConfirmationItem confItem = _database.ConfirmationsLoadByType(SessionUser.UserId, ConfirmationTypes.NewPin);
			if (confItem != null && (!confItem.Finished || !SessionUser.Activated))
			{
				// confirm login
				SendAscii($"\r\n{LngText(LngKeys.NewAccountActivated)}");

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

			//MessageDispatcher.Instance.Dispatch(ConnectionId, "Logoff");
			//_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(GroupAdministration), "Logoff");
		}

		private void InterpreterLoop()
		{
			//bool first = true;
			while (IsConnected)
			{
				//if (!first)
				//{
				//	SendAscii("\n");
				//}
				//first = false;
				SendAscii(CodeManager.ASC_COND_NL);
				InputResult inputResult = InputString($"\r\n{LngText(LngKeys.CmdPrompt)}:", ShiftStates.Ltrs, null, 30, 1);
				if (!IsConnected || inputResult.Timeout) return;

				DispatchMsgAndLog($"Input: {inputResult.InputString}");
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
						case TokenTypes.List:
							ok = CmdListGroups(tokens);
							break;
						case TokenTypes.Select:
							ok = CmdSelectGroup(tokens);
							break;
						case TokenTypes.New:
							ok = CmdNewGroup(tokens);
							break;
						case TokenTypes.Edit:
							ok = CmdEditSelectedGroup(tokens);
							break;
						case TokenTypes.Delete:
							ok = CmdDeleteSelectedGroup(tokens);
							break;
						case TokenTypes.Numbers:
							ok = CmdListNumbersOfSelectedGroup(tokens);
							break;
						case TokenTypes.Add:
							ok = CmdAddNumberToSelectedGroup(tokens);
							break;
						case TokenTypes.Remove:
							ok = CmdRemoveNumberFromSelectedGroup(tokens);
							break;
						case TokenTypes.Pin:
							ok = CmdSetNewPin(tokens);
							break;
						case TokenTypes.End:
							return;
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
			SendAscii(CodeManager.ASC_COND_NL);
			SendAscii($"{LngText(LngKeys.InvalidCommand)}");
		}

		private bool CmdHelp(List<CmdTokenItem> tokens)
		{
			DispatchMsgAndLog($"cmd help");
			if (ConnectionLanguage.ShortName == "de")
			{
				//             12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\nhilfe / erlaubte befehle                         beispiel\n");
				SendAscii("\r\nliste gruppen     (eigene) gruppen auflisten      lis gru");
				SendAscii("\r\nwaehle gruppe (g) gruppe zum bearbeiten auswaehl. wae gru name");
				SendAscii("\r\nneue gruppe (g)   neue gruppe anlegen             neu gru name");
				SendAscii("\r\nbearbeite gruppe  ausgewaehlte gruppe bearbeiten  bea gru");
				SendAscii("\r\nnummern           nummern der akt. grup. auflist. num");
				SendAscii("\r\nzufueg (nr) (na)  nummer zur ausg. gruppe zufueg. zuf 12345 hans");
				SendAscii("\r\nentferne (nr)     nummer von ausg. gruppe entf.   ent 12345");
				SendAscii("\r\npin               persoenliche pin aendern        pin");
				SendAscii("\r\nende              verb. beenden (oder st-taste)   end");
				//               12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\n\nes werden nur die gruppen angezeigt, in denen man mitglied ist.");
				SendAscii("\r\neine gruppe kann nur ausgewaelt werden, wenn man der besitzer ist\r\n" +
							  "oder wenn man den gruppen-pin kennt.");
				SendAscii($"\r\nbei allen befehlen ist die eingabe der ersten {_commandInterpreter.MinLen} zeichen\r\nausreichend.");
			}
			else
			{
				//             12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\nhelp / allowed commands                         example\n");
				SendAscii("\r\nlist groups     list own groups                  lis gro");
				SendAscii("\r\nselect (g)      select group for editing         sel gro name");
				SendAscii("\r\nnew group (g)   create new group                 new gro name");
				SendAscii("\r\nedit group      edit selected group              edi gro");
				SendAscii("\r\ndelete group    delete selected group            del gro");
				SendAscii("\r\nnumbers         list numbers of selected group   num");
				SendAscii("\r\nadd (no) (na)   add number to selected group     add 12345 joe");
				SendAscii("\r\nremove (no)     remove number from select. group rem 12345");
				SendAscii("\r\npin             change personal pin              pin");
				SendAscii("\r\nend             terminate conn. (or st-key)      end");
				//               12345678901234567890123456789012345678901234567890123456789012345678
				SendAscii("\r\n\nonly the groups in which you are a member are displayed.");
				SendAscii("\r\nes werden nur die gruppen angezeigt, in denen man mitglied ist.");
				SendAscii("\r\na group can only be selected if you are the owner or if you know\r\n" +
								"the group pin.");
				SendAscii($"\r\n\nfor all commands, entering the first {_commandInterpreter.MinLen}\r\ncharacters is sufficient.");
			}
			return true;
		}

		private bool CmdListGroups(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;
			if (tokens[1].TokenType != TokenTypes.Groups) return false;

			List<GroupItem> groups = GetGroups();

			// all groups where i'm member or owner
			List<GroupItem> myGroups = new List<GroupItem>();
			foreach (GroupItem group in groups)
			{
				if (IsMember(group) || group.Owner == RemoteNumber.Value)
				{
					myGroups.Add(group);
				}
			}

			if (myGroups.Count == 0)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				SendAscii($"{LngText(LngKeys.NoGroupsFound)}");
				return true;
			}

			for (int i = 0; i < myGroups.Count; i++)
			{
				GroupItem grp = myGroups[i];
				int memberCount = GetMemberCount(grp);
				string mark = _selectedGroup != null && string.Compare(grp.Name, _selectedGroup.Name, true) == 0 ? "+" : " ";
				SendAscii($"\r\n{mark}{myGroups[i].Name} {memberCount}");
			}
			return true;
		}

		private bool CmdSelectGroup(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2 && tokens.Count != 3) return false;
			if (tokens[1].TokenType != TokenTypes.Group) return false;

			string msg;
			string groupName;
			if (tokens.Count == 2)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				InputResult result = InputString($"{LngText(LngKeys.GroupName)}:", ShiftStates.Figs, null, 20, 1);
				if (result.ErrorOrTimeoutOrDisconnected) return false;
				// check groupname
				groupName = result.InputString;
			}
			else
			{
				groupName = tokens[2].GetStringValue().Trim();
			}

			if (!IsGroupNameValid(groupName))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidCharsInGroupName));
				return true;
			}
			GroupItem groupItem = GetGroupByName(groupName);
			if (groupItem == null)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				msg = LngText(LngKeys.GroupNotFound, groupName);
				SendAscii(msg);
				return true;
			}

			if (groupItem.Owner != RemoteNumber)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				InputResult result = InputPin($"{LngText(LngKeys.Pin)}:", 5);
				if (result.ErrorOrTimeoutOrDisconnected) return false;
				if (result.InputString.Trim() != groupItem.Pin)
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.WrongPin));
					return true;
				}
			}

			_selectedGroup = groupItem;
			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.GroupSelected, _selectedGroup.Name));
			return true;
		}

		private bool CmdNewGroup(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2 && tokens.Count != 3) return false;
			if (tokens[1].TokenType != TokenTypes.Group) return false;

			string msg;
			string groupName;
			if (tokens.Count == 2)
			{
				InputResult result = InputString($"{LngText(LngKeys.GroupName)}:", ShiftStates.Figs, null, 20, 1);
				if (result.ErrorOrTimeoutOrDisconnected) return false;
				// check groupname
				groupName = result.InputString;
			}
			else
			{
				groupName = tokens[2].GetStringValue().Trim();
			}
			if (!IsGroupNameValid(groupName))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidCharsInGroupName));
				return true;
			}
			GroupItem groupItem = GetGroupByName(groupName);
			if (groupItem != null)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				msg = LngText(LngKeys.GroupExists, groupName);
				SendAscii(msg);
				return true;
			}

			groupItem = new GroupItem();
			groupItem.Name = groupName;
			groupItem.Owner = RemoteNumber.Value;
			groupItem.Active = true;

			EditGroup(groupItem, false);
			DateTime utcNow = DateTime.UtcNow;
			groupItem.CreatedTimeUtc = utcNow;
			groupItem.LastChangedTimeUtc = utcNow;
			if (!_database.GroupInsert(groupItem))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
				return true;
			}
			_selectedGroup = groupItem;
			SendAscii(CodeManager.ASC_COND_NL);
			msg = LngText(LngKeys.GroupCreatedAndSelected, groupItem.Name);
			SendAscii(msg);
			GetGroups(true); // reload groups
			return true;
		}

		private bool CmdEditSelectedGroup(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;
			if (tokens[1].TokenType != TokenTypes.Group) return false;

			if (_selectedGroup == null)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				SendAscii(LngText(LngKeys.NoGroupSelected));
				return true;
			}

			EditGroup(_selectedGroup, true);
			_selectedGroup.LastChangedTimeUtc = DateTime.UtcNow;
			if (!_database.GroupUpdate(_selectedGroup))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
				return true;
			}
			SendAscii(CodeManager.ASC_COND_NL);
			SendAscii(LngText(LngKeys.GroupDataChanged));

			GetGroups(true); // reload groups
			return true;
		}

		private bool CmdDeleteSelectedGroup(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;
			if (tokens[1].TokenType != TokenTypes.Group) return false;

			if (_selectedGroup == null)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				SendAscii(LngText(LngKeys.NoGroupSelected));
				return true;
			}

			if (_selectedGroup.Owner != RemoteNumber)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				SendAscii(LngText(LngKeys.GroupDeleteNotAllowed));
				return true;
			}

			int memberCount = GetMemberCount(_selectedGroup);
			string msg = LngText(LngKeys.DeleteGroupWithMembers, _selectedGroup.Name, memberCount.ToString());
			InputResult inputResult = InputYesNo($"\r\n{msg} ?", null, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return true;
			if (inputResult.InputBool == true)
			{
				if (!_database.GroupDelete(_selectedGroup.GroupId))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
					return true;
				}
				if (!_database.GroupMemberDeleteByGroupId(_selectedGroup.GroupId))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
					return true;
				}
				SendAscii(CodeManager.ASC_COND_NL);
				msg = LngText(LngKeys.GroupDeleted, _selectedGroup.Name);
				SendAscii(msg);
			}
			return true;
		}

		private bool CmdListNumbersOfSelectedGroup(List<CmdTokenItem> tokens)
		{
			if (_selectedGroup == null)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				SendAscii(LngText(LngKeys.NoGroupSelected));
				return true;
			}

			string msg;
			List<GroupMemberItem> membersInGroup = GetSortedNumbersForGroup(_selectedGroup);
			if (membersInGroup.Count == 0)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				msg = LngText(LngKeys.NoNumbersInGroup, _selectedGroup.Name);
				SendAscii(msg);
				return true;
			}

			msg = LngText(LngKeys.GroupHeader, _selectedGroup.Name);
			SendAscii($"{msg}:");
			for (int i=0; i<membersInGroup.Count; i++)
			{
				SendAscii($"\r\n{i + 1:D2} {membersInGroup[i].Number}");
				if (!string.IsNullOrEmpty(membersInGroup[i].Name))
				{
					SendAscii($" {membersInGroup[i].Name}");
				}
			}
			return true;
		}

		private bool CmdAddNumberToSelectedGroup(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2 && tokens.Count != 3) return false;

			if (_selectedGroup == null)
			{
				SendAscii(CodeManager.ASC_COND_NL);
				SendAscii(LngText(LngKeys.NoGroupSelected));
				return true;
			}

			int? number = tokens[1].GetNumericValue();
			string name = (tokens.Count == 3) ? tokens[2].GetStringValue() : null;
			if (!_subcriberServer.CheckNumberIsValid(number.Value))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidNumber, number.ToString()));
				return true;
			}

			GroupMemberItem member = GetGroupMemberByNumber(_selectedGroup, number.Value);
			if (member == null)
			{	// new member
				member = new GroupMemberItem()
				{
					GroupId = _selectedGroup.GroupId,
					Number = number.Value,
					Name = name,
					AddedTimeUtc = DateTime.UtcNow,
					Active = true
				};
				if (!_database.GroupMemberInsert(member))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
					return true;
				}
			}
			else
			{	// update member
				member.Name = name;
				if (!_database.GroupMemberUpdate(member))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
					return true;
				}
			}
			GetGroupMembers(true);

			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.NumberAdded, number.ToString()));
			return true;
		}

		private bool CmdRemoveNumberFromSelectedGroup(List<CmdTokenItem> tokens)
		{
			if (tokens.Count != 2) return false;

			if (_selectedGroup == null)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.NoGroupSelected));
				return true;
			}

			string msg;
			int? number = tokens[1].GetNumericValue();
			List<GroupMemberItem> members = GetGroupMembers();
			GroupMemberItem member = (from m in members
									  where m.GroupId == _selectedGroup.GroupId && m.Number == number.Value
									  select m).FirstOrDefault();
			if (member == null)
			{
				SendAscii(LngText(CodeManager.ASC_COND_NL + LngKeys.NumberNotInGroup, number.ToString()));
				return true;
			}

			msg = CodeManager.ASC_COND_NL + LngText(LngKeys.DeleteNumberFromGroup, number.ToString(), _selectedGroup.Name);
			InputResult inputResult = InputYesNo($"{msg} ?", null, 1);
			if (inputResult.ErrorOrTimeoutOrDisconnected) return true;
			if (inputResult.InputBool == false) return true;

			if (!_database.GroupMemberDeleteByGroupIdAndNumber(_selectedGroup.GroupId, number.Value))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
				return true;
			}

			GetGroupMembers(true);
			SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.NumberDeleted, number.ToString()));
			return true;
		}

		private bool CmdSetNewPin(List<CmdTokenItem> tokens)
		{
			UserItem userItem = _database.UserLoadById(SessionUser.UserId);

			// old pin

			InputResult inputResult = InputPin($"\r\n{LngText(LngKeys.EnterOldPin)}:", 5);
			if (!IsConnected || inputResult.ErrorOrTimeoutOrDisconnected) return true;

			string oldPin = inputResult.InputString;
			if (oldPin != userItem.Pin)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.WrongPin));
				return true;
			}

			// new pin

			inputResult = InputPin($"{LngText(LngKeys.EnterNewPin)}:", 5);
			if (!IsConnected || inputResult.ErrorOrTimeoutOrDisconnected) return true;

			string newPin = inputResult.InputString;
			if (newPin == oldPin)
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.PinNotChanged));
				return true;
			}
			if (!CommonHelper.IsValidPin(newPin))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidPin));
				return true;
			}

			// new pin again

			SendAscii(CodeManager.ASC_COND_NL);
			inputResult = InputPin($"{LngText(LngKeys.EnterNewPinAgain)}:", 5);
			if (!IsConnected || inputResult.ErrorOrTimeoutOrDisconnected) return true;

			string newPin2 = inputResult.InputString;
			if (!CommonHelper.IsValidPin(newPin2))
			{
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidPin));
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
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(CmdSetNewPin), "error changing pin in database.");
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InternalError));
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.PinNotChanged));
				return true;
			}
			SessionUser = userItem;

			SendAscii(CodeManager.ASC_COND_NL);
			SendAscii(LngText(LngKeys.PinChanged));

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


		private bool EditGroup(GroupItem group, bool edit)
		{
			InputResult result;
			bool valid = false;
			for (int i = 0; i < 3; i++)
			{
				string groupName = group.Name;
				SendAscii(CodeManager.ASC_COND_NL);
				result = InputString($"{LngText(LngKeys.GroupName)}:", ShiftStates.Ltrs, groupName, 20, 1);
				if (result.ErrorOrTimeoutOrDisconnected) return false;
				groupName = result.InputString;
				if (!IsGroupNameValid(groupName))
				{
					SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.InvalidCharsInGroupName));
					continue;
				}

				GroupItem g = GetGroupByName(groupName);
				if (edit && groupName == g.Name ||  g == null)
				{
					group.Name = groupName;
					valid = true;
					break;
				}
				SendAscii(CodeManager.ASC_COND_NL + LngText(LngKeys.GroupExists, groupName));
			}
			if (!valid) return false;

			string pin = group.Pin;
			SendAscii(CodeManager.ASC_COND_NL);
			result = InputPin($"{LngText(LngKeys.Pin)}:", 5);
			if (result.ErrorOrTimeoutOrDisconnected) return false;
			group.Pin = string.IsNullOrEmpty(result.InputString) ? null : result.InputString;

			/*
			valid = false;
			for (int i = 0; i < 3; i++)
			{
				string coOwner = group.CoOwner.HasValue ? group.CoOwner.ToString() : null;
				result = InputString("co-owner:", ShiftStates.Figs, coOwner, 10, 1);
				if (result.ErrorOrTimeoutOrDisconnected) return false;
				if (!int.TryParse(result.InputString, out int num)) continue;
				if (string.IsNullOrEmpty(result.InputString))
				{
					group.CoOwner = null;
					valid = true;
					break;
				}
				if (_subcriberServer.CheckNumberIsValid(num))
				{
					group.CoOwner = num;
					valid = true;
					break;
				}
				SendAscii($"\r\n{num} ist ungueltig.");
			}
			if (!valid) return false;
			*/

			return true;
		}

		private GroupItem GetGroupByName(string name)
		{
			List<GroupItem> groups = GetGroups();
			return (from g in groups
					where string.Compare(g.Name, name, true) == 0
					select g).FirstOrDefault();
		}

		private const string _validGroupChars = "abcdefghijklmnopqrstuvwxyz0123456789_";

		private bool IsGroupNameValid(string groupName)
		{
			if (string.IsNullOrEmpty(groupName)) return false;
			foreach(char c in groupName)
			{
				if (!_validGroupChars.Contains(c)) return false;
			}
			return true;
		}

		private List<GroupMemberItem> GetSortedNumbersForGroup(GroupItem grp)
		{
			List<GroupMemberItem> members = GetGroupMembers();
			List<GroupMemberItem> membersInGroup = (from m in members
					where m.GroupId == grp.GroupId
					select m).ToList();
			membersInGroup.Sort(new GroupMemberComparer());
			return membersInGroup;
		}

		private int GetMemberCount(GroupItem grp)
		{
			List<GroupMemberItem> members = GetGroupMembers();
			return (from m in members
					where m.GroupId == grp.GroupId
					select m).Count();
		}

		private GroupMemberItem GetGroupMemberByNumber(GroupItem grp, int number)
		{
			List<GroupMemberItem> members = GetGroupMembers();
			return (from m in members
					where m.GroupId == grp.GroupId && m.Number == number
					select m).FirstOrDefault();
		}

		private bool IsMember(GroupItem grp)
		{
			List<GroupMemberItem> members = GetGroupMembers();
			return (from m in members 
					where m.GroupId == grp.GroupId && m.Number == RemoteNumber.Value
					select m).Any();
		}

		private bool IsValidGroupAndIsMember(string name)
		{
			GroupItem grp = GetGroupByName(name);
			if (grp == null) return false;

			List<GroupMemberItem> members = GetGroupMembers();
			return (from m in members
					where m.GroupId == grp.GroupId && m.Number == RemoteNumber.Value
					select m).Any();
		}


		private List<GroupItem> _groups = null;

		private List<GroupItem> GetGroups(bool force = false)
		{
			if (force || _groups == null)
			{
				_groups = _database.GroupsLoadAll();
			}
			return _groups;
		}

		private List<GroupMemberItem> _groupMembers = null;

		private List<GroupMemberItem> GetGroupMembers(bool force = false)
		{
			if (force || _groupMembers == null)
			{
				_groupMembers = _database.GroupMembersLoadByGroup();
			}
			return _groupMembers;
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

		public void DebugSaveMsg(SendProperties sendProps)
		{
			string numStr = sendProps.CallerNumber.ToString();
			string filename = $"{DateTime.Now:yyMMdd.HHmm}_{numStr}_{sendProps.SendMode}.txt";

			string recvLines = SendProperties.GetReceiverLines(sendProps.Receivers, 79, "empf:");
			string textLines = sendProps.GetMessageTextAscii();
			File.WriteAllText(@"msgs\" + filename, recvLines + "\r\n" + textLines);
		}

		private void RundsenderConnection_Received(ItelexConnection connection, string asciiText)
		{
			if (_inputActive)
			{
				lock (_inputLineLock)
				{
					_inputLine += asciiText;
				}
			}
		}

		/*
		private string CreatePin()
		{
			while (true)
			{
				string pinStr = _random.Next(1001, 9988).ToString();
				int[] digitCnt = new int[10];
				bool max2 = true;
				for (int i = 0; i < 4; i++)
				{
					int digit = ((int)pinStr[i]) - 48;
					digitCnt[digit]++;
					if (digitCnt[digit] > 2) max2 = false;
				}
				if (max2) return pinStr;
			}
		}
		*/
	}

	class GroupMemberComparer: IComparer<GroupMemberItem>
	{
		/// <summary>
		/// Sort by number ascending (sort as string)
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public int Compare(GroupMemberItem x, GroupMemberItem y)
		{
			string xn = x.Number.ToString();
			string yn = y.Number.ToString();
			return string.Compare(xn, yn);
		}
	}

}
