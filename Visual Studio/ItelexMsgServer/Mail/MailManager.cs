using ItelexCommon.Logger;
using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ItelexMsgServer.Data;
using System.Diagnostics;
using ItelexCommon.Utility;
using ItelexCommon.Mail;
using System.Windows.Forms;
using ItelexCommon.ChatGpt;

namespace ItelexMsgServer.Mail
{
	public class MailManager
	{
		private const string TAG = nameof(MailManager);

		private MsgServerDatabase _database;
		private MailAgent _mailAgent;

		private Logging _logger;

		private bool _fetchMailsTimerActive;
		private System.Timers.Timer _fetchMailsTimer;

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static MailManager instance;

		public static MailManager Instance => instance ?? (instance = new MailManager());

		public object GlobalMessageLock = new object();

		public MailManager()
		{
			_logger = LogManager.Instance.Logger;
			_database = MsgServerDatabase.Instance;
			_mailAgent = new MailAgent(_logger, Constants.MAILKIT_LOG);

#if !DEBUG
			_fetchMailsTimer = new System.Timers.Timer(1000 * 120);
#else
			_fetchMailsTimer = new System.Timers.Timer(1000 * 30);
#endif
			_fetchMailsTimerActive = false;
			_fetchMailsTimer.Elapsed += FetchMailsTimer_Elapsed;
			_fetchMailsTimer.Start();
		}

		private void FetchMailsTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			ReceiveAndDistributeMails();
			//ReceiveAndDistributeUserMails();
		}

		public void ReceiveAndDistributeMails()
		{
			Task.Run(() =>
			{
				TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(ReceiveAndDistributeMails));
				try
				{
					if (_fetchMailsTimerActive) return;
					_fetchMailsTimerActive = true;
					//DispatchMsg("check for incoming mails");
					_mailAgent.ReceiveAndProccessMails("ArchivMails", ProcessMail);
				}
				finally
				{
					_fetchMailsTimerActive = false;
					TaskManager.Instance.RemoveTask(Task.CurrentId);
				}
				return;
			});
		}

		private int ProcessMail(MailItem mailItem)
		{
			if (_database.UidExists(mailItem.Uid)) return 0; // mail already processed

			int cnt = DistributeMailToItelexNumbers(mailItem);

			if (cnt > 0)
			{
				// save Uid = mail processed
				UidItem uidItem = new UidItem()
				{
					Uid = mailItem.Uid,
					Sender = mailItem.From,
					CreateTimeUtc = DateTime.UtcNow,
					MailTimeUtc = mailItem.DateSentUtc,
				};
				if (!_database.UidInsert(uidItem))
				{
					DispatchMsg($"error inserting uid {mailItem.Uid} from {mailItem.From}");
				}
			}
			return cnt;
		}

#if false
		public void ReceiveAndDistributeUserMails()
		{
			Task.Run(() =>
			{
				if (_fetchMailsTimerActive) return;
				_fetchMailsTimerActive = true;
				try
				{
					List<EmailAccountItem> accountList = _database.EmailAccountsLoadAllActive();

					_logger.Debug(TAG, nameof(ReceiveAndDistributeUserMails), $"accountList.Count={accountList.Count}");

					if (accountList == null || accountList.Count == 0) return;

					List<UserItem> users = _database.UserLoadAll();
					if (accountList == null || accountList.Count == 0) return;

					//const string hostname = "pop.1und1.de";
					//const int port = 995;
					const bool useSsl = true;

					foreach (EmailAccountItem accountItem in accountList)
					{
						try
						{
							//UserItem user = _database.UserLoadById(accountItem.UserId);
							//if (user == null) continue;

							// the client disconnects from the server when being disposed
							using (Pop3Client client = new Pop3Client())
							{
								client.Connect(accountItem.Server, accountItem.Port, useSsl);
								client.Authenticate(accountItem.Username, accountItem.Password);
								int messageCount = client.GetMessageCount();
								List<MailItem> allMessages = new List<MailItem>();
								for (int msgNo = 0; msgNo < messageCount; msgNo++)
								{
									MimeMessage message = client.GetMessage(msgNo);
									MailItem mailItem = ParseMessage(message);
									mailItem.Uid = message.MessageId;
									if (_database.UidExists(mailItem.Uid)) continue; // mail already processed

									int cnt = DistributeMailToItelexNumbers(mailItem, accountItem.ItelexNumber);
									if (cnt > 0 && accountItem.DeleteAfterRead)
									{
										// no error and at least one distribution
										_logger.Debug(TAG, nameof(ReceiveAndDistributeUserMails), $"delete msgno {msgNo}");
										client.DeleteMessage(msgNo);
									}

									// save Uid = mail processed
									UidItem uidItem = new UidItem()
									{
										Uid = mailItem.Uid,
										Sender = mailItem.From,
										CreateTimeUtc = DateTime.UtcNow,
										MailTimeUtc = mailItem.DateSentUtc,
									};
									if (!_database.UidInsert(uidItem))
									{
										DispatchMsg($"error inserting uid {mailItem.Uid} from {mailItem.From}");
									}
								}
							}
						}
						catch (Exception ex)
						{
							_logger.Error(TAG, nameof(ReceiveAndDistributeUserMails), $"error {accountItem.UserId} {accountItem.ItelexNumber}", ex);
						}
					}
				}
				finally
				{
					_fetchMailsTimerActive = false;
				}
			});
		}
#endif

		public int DistributeMailToItelexNumbers(MailItem mailItem, int? number = null)
		{
			_logger.Debug(TAG, nameof(DistributeMailToItelexNumbers), $"[{mailItem}] {number}");

			string senderMailAddr = EmailToSubscriberMail(mailItem.From);

			DateTime utcNow = DateTime.UtcNow;
			MailParseData parseData = ParseToAndSubjectAndBody(mailItem);

			// explicit receiver (special e-mail-address for special i-telex-numbers)
			//if (mailItem.ToAddress != Constants.EMAIL_ADDRESS)
			//{
			//	UserItem ui = _database.UserLoadByReceiver(mailItem.ToAddress);
			//	if (ui != null)
			//	{
			//		data.TelexNumbers.Add(ui.ItelexNumber);
			//	}
			//}

			parseData.RemoveDuplicateNumbers();

			List<int> numbers = number.HasValue ? new List<int>() { number.Value } : parseData.TelexNumbers;
			_logger.Debug(TAG, nameof(DistributeMailToItelexNumbers), $"TelexNumbers.Count={parseData.TelexNumbers.Count}");

			int distribCnt = 0;

			string origMessage = parseData.Text;
			string censoredMessage = null;

			if (numbers.Count > 0)
			{
				foreach (int num in numbers)
				{
					UserItem userItem = _database.UserLoadByTelexNumber(num);
					if (userItem == null || !userItem.Activated || !userItem.AllowRecvMails.Value)
					{
						// not registered for mail
						string msg = $"{num} not registered for mail or invalid";
						DispatchMsg(msg);
						continue;
					}
					distribCnt++;

					if (!string.IsNullOrEmpty(userItem.EventPin) && parseData.EventCode != userItem.EventPin)
					{
						DispatchMsg($"invalid event code {parseData.EventCode}");
						continue;
					}

					if (!string.IsNullOrEmpty(userItem.AllowedSender))
					{
						string allowedSender = MailAgent.ConvertMailAddrFromBaudotCode(userItem.AllowedSender);
						if (senderMailAddr.CompareTo(allowedSender) != 0)
						{
							DispatchMsg($"{senderMailAddr} is not an allowed sender for {userItem.ItelexNumber}");
							continue;
						}
					}

					string pauseStr = userItem.Paused ? " (paused)" : "";
					DispatchMsg($"Mail from {mailItem.From} to {userItem.ItelexNumber}{pauseStr}");
					if (userItem.Paused) continue; // paused

					//int hour = utcNow.AddHours(userItem.Timezone).Hour;
					//if (userItem.SendFromHour != null && userItem.SendToHour != null &&
					//	hour < userItem.SendFromHour && hour >= userItem.SendToHour) continue;

					string message = parseData.Text;
					string subject = mailItem.Subject;
					if (userItem.Public)
					{
						subject = "";
						if (censoredMessage == null)
						{
							censoredMessage = GetCensoredMessage(message);
						}
						message = censoredMessage;
					}

					lock (_database.MailGateLocker)
					{
						// save Uid = mail processed
						UidItem uidItem = new UidItem()
						{
							Uid = mailItem.Uid,
							Sender = mailItem.From,
							CreateTimeUtc = DateTime.UtcNow,
							MailTimeUtc = mailItem.DateSentUtc,
						};
						if (!_database.UidInsert(uidItem))
						{
							DispatchMsg($"error inserting uid {mailItem.Uid} from {mailItem.From}");
							return 0; // error
						}

						MsgItem msgItem = new MsgItem()
						{
							UserId = userItem.UserId,
							MsgType = (int)MsgTypes.Email,
							CreateTimeUtc = utcNow,
							Sender = mailItem.From,
							Subject = subject,
							Uid = mailItem.Uid,
							Message = message,
							LineCount = parseData.LineCount,
							MailTimeUtc = mailItem.DateSentUtc,
							SendRetries = 0,
						};

						if (!_database.MsgsInsert(msgItem))
						{
							DispatchMsg($"error inserting mail from {mailItem.From} to {userItem.ItelexNumber}");
							return 0; // error
						}
					}
				}
			}

			_logger.Debug(TAG, nameof(DistributeMailToItelexNumbers), $"distribCnt = {distribCnt}");

			return distribCnt;
		}

		public string GetCensoredMessage(string message)
		{
			message = message.Trim();
			if (string.IsNullOrWhiteSpace(message)) return message;

			float temperature = 0.5f;
			float top_p = 0.0f;

			ChatGptAbstract chatGpt = new ChatGptWetstone();

			string intro = "Hallo ChatGPT, kannst du bitte in dem folgenden Text alle anstößigen Worte durch 'zensiert' ersetzen?\r\n\r\n";
			string marker = "das ist ein text.";
			message = message.Replace("\r", " ");
			message = message.Replace("\n", " ");
			message = message.Replace("  ", " ").Trim();

			Task<string> response = chatGpt.Request(intro + marker + message, temperature, top_p);
			response.Wait();
			string responseStr = chatGpt.ConvMsgText(response.Result);
			int p = responseStr.IndexOf(marker);
			if (p != -1)
			{
				responseStr = responseStr.Substring(p + marker.Length + 1);
			}
			if (responseStr.Contains("zensiert"))
			{
				string zensurHinweis = "(einige von chat-gpt als anstoessig erkannte ausdruecke wurden zensiert)";
				responseStr = responseStr + "\r\n" + zensurHinweis; 
			}

			return responseStr;
		}

		private MailParseData ParseToAndSubjectAndBody(MailItem mailItem)
		{
			MailParseData data = new MailParseData();

			// search for number in toName
			string toName = mailItem.ToName.ToLower();
			if (toName.Contains("itelex") || toName.Contains("#"))
			{
				// format "itelex 12345" or "itelex #12345 or "#12345"
				toName = toName.Trim(new char[] { '\"', '\'' });
				string[] parts = toName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				string numStr;
				if (parts.Length > 0 && parts[0].StartsWith("#"))
				{
					numStr = parts[0].Substring(1).Trim();
					if (int.TryParse(numStr, out int num))
					{
						data.AddNumber(num);
						_logger.Debug(TAG, nameof(ParseToAndSubjectAndBody), $"found itelex number in toName {mailItem.ToName}");
					}
				}

				if (parts.Length == 2 && parts[0] == "itelex")
				{
					numStr = parts[1];
					if (numStr.Length >= 4)
					{
						numStr = numStr.Trim();
						if (numStr.StartsWith("#")) numStr = numStr.Substring(1).Trim();
						if (int.TryParse(numStr, out int num))
						{
							data.AddNumber(num);
							_logger.Debug(TAG, nameof(ParseToAndSubjectAndBody), $"found itelex number in toName {mailItem.ToName}");
						}
					}
				}
			}

			// search for numbers in subject
			if (mailItem.Subject.Contains("#"))
			{
				string subj = mailItem.Subject;
				string orgSubj = subj;
				int pos = subj.IndexOf('#');
				subj = subj.Substring(pos);
				List<int> nums = ParseNums(ref subj);
				if (nums.Count > 0)
				{
					data.AddNumbers(nums);
					_logger.Debug(TAG, nameof(ParseToAndSubjectAndBody), $"found itelex number(s) in subject {mailItem.Subject}");
				}
				//mailItem.Subject = subj;
			}

			// search for event code in subject
			if (mailItem.Subject.Contains("*"))
			{
				string subj = mailItem.Subject;
				string orgSubj = subj;
				int pos = subj.IndexOf('*');
				string eventCode = subj.Substring(pos + 1, 4);
				if (CommonHelper.IsValidPin(eventCode))
				{
					data.EventCode = eventCode;
					mailItem.Subject = subj.Substring(0, pos - 1) + subj.Substring(pos + 1 + 4);
					_logger.Debug(TAG, nameof(ParseToAndSubjectAndBody), $"found eventCode {eventCode} in subject");
				}
			}

			// search for numbers in body
			List<string> lines = mailItem.Body.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
			for (int i = 0; i < lines.Count; i++)
			{
				string line = lines[i];
				if (string.IsNullOrEmpty(line)) break;

				if (line.StartsWith("#"))
				{
					string orgLine = line;
					data.AddNumbers(ParseNums(ref line));
					_logger.Debug(TAG, nameof(ParseToAndSubjectAndBody), $"found itelex number(s) in body {orgLine}");
					lines[i] = line;
					continue;
				}

				//string newline = CodeManager.AsciiStringReplacements(line, CodeSets.ITA2, false, true);
				//newLines.Add(line);
			}

			//newLines = tf.FormatTelexLines(newLines, width);
			//string text = string.Join("\r\n", newLines).ToLower();
			//text = text.Replace(">>", "''");
			//text = text.Replace("<<", "''");
			//data.Text = text;
			data.Text = string.Join("\r\n", lines);
			data.LineCount = lines.Count;
			return data;
		}

		/// <summary>
		/// Parse i-Telex numbers in subject and message body.
		/// Detects:
		/// #12345
		/// #12345,123456
		/// #12345,#123456
		/// #12345 Any text (numbers separated by blank)
		/// </summary>
		/// <param name="lineToParse"></param>
		/// <returns></returns>
		public static List<int> ParseNums(ref string lineToParse)
		{
			List<int> numbers = new List<int>();

			if (lineToParse.Length <= 2) return numbers;

			// '#12345' -> '12345'
			//lineToParse = lineToParse.Substring(1);
			lineToParse = lineToParse.Trim();

			string line = lineToParse;
			string numStr = "";
			while (line.Length > 0)
			{
				char nextChar = line[0];
				if (char.IsWhiteSpace(nextChar))
				{
					line = line.Substring(1);
					continue;
				}

				if (char.IsDigit(nextChar))
				{   // digit
					numStr += nextChar;
					line = line.Substring(1);
					continue;
				}
				else
				{   // no digit
					if (numStr.Length > 0)
					{
						if (int.TryParse(numStr, out int num))
						{
							numbers.Add(num);
						}
						numStr = "";
					}
					if (nextChar != ',' && nextChar != '#') break;
					line = line.Substring(1);
				}
			}

			if (numStr.Length > 0)
			{
				if (int.TryParse(numStr, out int num))
				{
					numbers.Add(num);
				}
			}

			lineToParse = lineToParse.Replace('#', '+');

			return numbers;
		}

		private string EmailToSubscriberMail(string email)
		{
			int pos1 = email.IndexOf("<");
			int pos2 = email.IndexOf(">");
			if (pos1 == -1 || pos2 == -1) return email.ToLower();

			email = email.Substring(pos1 + 1, pos2 - pos1 - 1);

			return email.ToLower();
		}

		public bool SendMailSmtp(string receiver, string subject, string msg, int? itelexNumber)
		{
			return SendMailSmtp(receiver, subject, msg, itelexNumber, null, null);
		}

		public bool SendMailSmtp(string receiver, string subject, string msg, int? itelexNumber, string attachmentName, byte[] attachmentData)
		{
			string from = "telex@telexgate.de";
			if (itelexNumber.HasValue)
			{
				from = $"itelex {itelexNumber} <{from}>";
			}
			return _mailAgent.SendMailSmtp(from, receiver, subject, msg, attachmentName, attachmentData);
		}

		private void DispatchMsg(string msg)
		{
			MessageDispatcher.Instance.Dispatch(msg);
			_logger.Debug(TAG, nameof(DispatchMsg), msg);
		}
	}
}