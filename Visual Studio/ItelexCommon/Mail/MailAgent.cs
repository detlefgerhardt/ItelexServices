using ItelexCommon.Logger;
using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit;
using MimeKit;
using MailKit.Net.Pop3;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit.Text;
using MailKit.Net.Imap;
using System.Diagnostics;
using MailKit.Search;
using ItelexCommon.Utility;
using System.Runtime.InteropServices;

namespace ItelexCommon.Mail
{
	public class MailAgent
	{
		private const string TAG = nameof(MailAgent);

		private const string MAIL_ACCOUNT = "telex@telexgate.de";
		private const string MAIL_PASSWORD = PrivateConstants.TELEX_TELEXGATE_PASSWORD;

		private Logging _logger;
		private ProtocolLogger _mailKitLogger = null;

		public MailAgent(Logging logger, string mailkitLog)
		{
			_logger = logger;
			if (mailkitLog != null)
			{
#pragma warning disable CS0162 // Unreachable code detected
				_mailKitLogger = new ProtocolLogger(mailkitLog, true);
#pragma warning restore CS0162 // Unreachable code detected
			}
		}

		public void ReceiveAndProccessMails(string archiveFolder, Func<MailItem, int> processMail)
		{
			ImapConfig imapConfig = new ImapConfig()
			{
				ImapHost = "imap.1und1.de",
				ImapPort = 993,
				UseSsl = true,
				EmailAccount = MAIL_ACCOUNT,
				Password = MAIL_PASSWORD,
				SearchFolders = new string[] { "INBOX", "Spam" },
				ArchiveFolder = archiveFolder,
			};
			ReceiveAndProccessMails(imapConfig, processMail);
		}

		public int ReceiveAndProccessMails(ImapConfig imapConfig, Func<MailItem, int> processMail)
		{
			ImapClient client = null;
			try
			{
				// the client disconnects from the server when being disposed
				if (_mailKitLogger != null)
				{
					client = new ImapClient(_mailKitLogger);
				}
				else
				{
					client = new ImapClient();
				}
				try
				{
					client.Connect(imapConfig.ImapHost, imapConfig.ImapPort, imapConfig.UseSsl);
					client.Authenticate(imapConfig.EmailAccount, imapConfig.Password);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ReceiveAndProccessMails), $"error connecting to mail account {imapConfig.EmailAccount}", ex);
					return 0;
				}

				IMailFolder archivFolder = client.GetFolder(imapConfig.ArchiveFolder);

				int totalCnt = 0;
				foreach (string folderName in imapConfig.SearchFolders)
				{
					var folder = client.GetFolder(folderName);
					folder.Open(FolderAccess.ReadWrite);
					List<UniqueId> uids = folder.Search(SearchQuery.All).ToList();

					foreach (UniqueId uid in uids)
					{
						try
						{
							var message = folder.GetMessage(uid);
							MailItem mailItem = ParseMessage(message);
							mailItem.Uid = message.MessageId;
							//if (_database.UidExists(mailItem.Uid)) continue; // mail already processed
							int cnt = processMail(mailItem);
							totalCnt += cnt;
							//bool success = DistributeMailToNewsChannel(mailItem);
							if (cnt > 0)
							{
								_logger.Debug(TAG, nameof(ReceiveAndProccessMails), $"delete mail {uid}");
								//folder.AddFlags(uid, MessageFlags.Deleted, true);
								folder.AddFlags(uid, MessageFlags.Seen, true);
								var uidMap = folder.MoveTo(uid, archivFolder);
							}
						}
						catch (Exception ex)
						{
							_logger.Error(TAG, nameof(ReceiveAndProccessMails), $"error parsing and distributing mail {uid}", ex);
							continue;
						}
					}
					folder.Expunge();
					folder.Close();
				}
				return totalCnt;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(ReceiveAndProccessMails), "error", ex);
				return 0;
			}
			finally
			{
				if (client != null)
				{
					client.Disconnect(true);
					client.Dispose();
				}
			}
		}

		private MailItem ParseMessage(MimeMessage message)
		{
			MailItem mailItem = new MailItem();
			mailItem.Subject = message.Subject;
			mailItem.From = EmailToNameAndAddress(message.From);
			mailItem.ToAddress = EmailToAddress(message.To);
			mailItem.ToName = EmailToName(message.To);
			mailItem.DateSentUtc = message.Date.UtcDateTime;
			mailItem.Uid = message.MessageId;

			string body = "";
			if (!string.IsNullOrEmpty(message.TextBody))
			{
				body = message.TextBody;
			}
			else if (!string.IsNullOrEmpty(message.HtmlBody))
			{
				try
				{
					body = HtmlUtilities.ConvertToPlainText(message.HtmlBody);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ParseMessage), "error", ex);
					body = "";
				}
			}
			mailItem.Body = body;

			return mailItem;
		}

		private string EmailToName(InternetAddressList addresses)
		{
			MailboxAddress address = (MailboxAddress)addresses[0];
			if (!string.IsNullOrEmpty(address.Name) && address.Name != address.Address)
			{
				return address.Name;
			}
			else
			{
				return "";
			}
		}

		private string EmailToAddress(InternetAddressList addresses)
		{
			MailboxAddress address = (MailboxAddress)addresses[0];
			return address.Address;
		}

		private string EmailToNameAndAddress(InternetAddressList addresses)
		{
			MailboxAddress address = (MailboxAddress)addresses[0];
			if (!string.IsNullOrEmpty(address.Name) && address.Name != address.Address)
			{
				return $"{address.Name} <{address.Address}>";
			}
			else
			{
				return $"{address.Address}";
			}
		}

		/*
		private void GetMailsPop3()
		{
			const string POP3_SERVER = "pop.1und1.de";
			const int POP3_PORT = 995;
			const bool USE_SSL = true;
			const string POP3_USERNAME = "telex@telexgate.de";
			const string POP3_PASSWORD = PrivateConstants.MSGSRV_MAILPASSWORD;

			List<MimeMessage> messages = new List<MimeMessage>();
			using (var pop3Client = new Pop3Client())
			{
				pop3Client.Connect(POP3_SERVER, POP3_PORT, USE_SSL);
				pop3Client.Authenticate(POP3_USERNAME, POP3_PASSWORD);
				for (int i = 0; i < pop3Client.Count; i++)
				{
					var mimeMessage = pop3Client.GetMessage(i);
					messages.Add(mimeMessage);
					//pop3Client.DeleteMessage(i);
				}
				pop3Client.Disconnect(true);
			}
		}
		*/

		/*
		private void GetMailsImap()
		{
			const string IMAP_SERVER = "imap.1und1.de";
			const int IMAP_PORT = 993;
			const bool USE_SSL = true;
			const string IMAP_USERNAME = "telex@telexgate.de";
			const string IMAP_PASSWORD = PrivateConstants.MSGSRV_MAILPASSWORD;


			List<MimeMessage> messages = new List<MimeMessage>();
			using (var imapClient = new ImapClient())
			{
				imapClient.Connect(IMAP_SERVER, IMAP_PORT, USE_SSL);
				imapClient.Authenticate(IMAP_USERNAME, IMAP_PASSWORD);
				imapClient.Inbox.Open(FolderAccess.ReadOnly);
				var uids = imapClient.Inbox.Search(SearchQuery.All);
				foreach (var uid in uids)
				{
					var mimeMessage = imapClient.Inbox.GetMessage(uid);
					// mimeMessage.WriteTo($"{uid}.eml"); // for testing
					messages.Add(mimeMessage);

					string subj = mimeMessage.Subject;
					if (subj != "T100 HTML und Reintext") continue;
					DateTime dt = mimeMessage.Date.UtcDateTime;
					string from = EmailToString(mimeMessage.From);
					string to = EmailToString(mimeMessage.To);
					string body = "";
					if (!string.IsNullOrEmpty(mimeMessage.TextBody))
					{
						body = mimeMessage.TextBody;
					}
					else if (!string.IsNullOrEmpty(mimeMessage.HtmlBody))
					{
						body = ParseHtmlBody(mimeMessage.HtmlBody);

					}


					//mailItem.DateSent = mimeMessage.Headers.DateSent;
					//message.
					//MessagePart plainText = message.GetTextBody();
					//if (plainText != null)
					//{
					//	byte[] buffer = plainText.Body;
					//	string textUtf8 = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
					//	string textAscii = System.Text.Encoding.ASCII.GetString(buffer, 0, buffer.Length);
					//	//mailItem.Body = plainText.GetBodyAsText();
					//	mailItem.Body = textUtf8;
					//}
					
				}
				imapClient.Disconnect(true);
			}
		}
		*/

		public bool SendMailSmtp(string from, string receiver, string subject, string msg,
				string attachmentName, byte[] attachmentData)
		{
			SmtpConfig smtpConfig = new SmtpConfig()
			{
				SmtpHost = "smtp.1und1.de",
				SmtpPort = 587,
				EmailAccount = MAIL_ACCOUNT,
				Password = MAIL_PASSWORD,
				From = from,
				SecureSocketOptions = SecureSocketOptions.StartTls
			};
			return SendMailSmtp(smtpConfig, receiver, subject, msg, attachmentName, attachmentData);
		}

		public bool SendMailSmtp(SmtpConfig smtpConfig, string receiver, string subject, string msg, /*int? itelexNumber,*/
				string attachmentName, byte[] attachmentData)
		{
			//const string SMTP_SERVER = smtpConfig.SmtpHost;
			//const int SMTP_PORT = smtpConfig.SmtpPort;
#if DEBUG
			//const string SMTP_USERNAME = "telex@telexgate.de";
			//const string SMTP_PASSWORD = PrivateConstants.MSGSRV_MAILPASSWORD;
			//const string FROM = "telex@telexgate.de";
#else
			//const string SMTP_USERNAME = "telex@telexgate.de";
			//const string SMTP_PASSWORD = PrivateConstants.MSGSRV_MAILPASSWORD;
			//const string FROM = "telex@telexgate.de";
#endif
			//const string TO = PrivateConstants.DEBUG_EMAIL_ADDRESS;
			//const string SUBJ = "MailKit-Test 1";
			//const string BODY = "Hallo äöüÄÖÜß";

			//var body = new MimeMessage();
			//body.Body = new TextPart("plain") { Text = "Hallo äöüÄÖÜß" };

			//string from = FROM;
			//if (itelexNumber.HasValue)
			//{
			//	from = $"itelex {itelexNumber} <{from}>";
			//}

			try
			{
				receiver = ConvertMailAddrFromBaudotCode(receiver);
				subject = ConvertMailAddrFromBaudotCode(subject);
				msg = ConvertMailAddrFromBaudotCode(msg);

				MimeMessage message = new MimeMessage();
				message.From.Add(MailboxAddress.Parse(smtpConfig.From));
				message.To.Add(MailboxAddress.Parse(receiver));
				message.Subject = subject;

				if (string.IsNullOrEmpty(attachmentName) || attachmentData == null)
				{
					message.Body = new TextPart(TextFormat.Plain) { Text = msg };
				}
				else
				{
					BodyBuilder bb = new BodyBuilder();
					bb.TextBody = msg;
					bb.Attachments.Add(attachmentName, attachmentData);
					message.Body = bb.ToMessageBody();
				}

				SmtpClient client = null;
				try
				{
					if (_mailKitLogger != null)
					{
						client = new SmtpClient(_mailKitLogger);
					}
					else
					{
						client = new SmtpClient();
					}
					client.Connect(smtpConfig.SmtpHost, smtpConfig.SmtpPort, smtpConfig.SecureSocketOptions);
					client.Authenticate(smtpConfig.EmailAccount, smtpConfig.Password);
					client.Send(message);
					client.Disconnect(true);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SendMailSmtp), "error connecting to SMTP server", ex);
					if (client != null) client.Disconnect(false);
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(SendMailSmtp), "error sending SMTP message", ex);
				return false;
			}
		}

		/*
		 * Hex-Tab
		 * ! +21
		 * # +23
		 * $ +24
		 * % +25
		 * & +26
		 * ' +27
		 * * +2A
		 * + +2B
		 * ^ +5E
		 * _ +5F
		 * ` +60
		 * { +7B
		 * | +7C
		 * } +7D
		 * ~ +7E
		 * 
		 * ä +E4
		 * 
		 */
		public static string ConvertMailAddrFromBaudotCode(string str)
		{
			str = str.Replace("(at)", "@");

			// convert hex-numbers (+hh) to characters, only code 0x20 to 0x7E allowed
			while (true)
			{
				int p = str.IndexOf("+");
				if (p == -1 || p > str.Length - 3) break;
				string hexCode = str.Substring(p + 1, 2);
				int asciiVal = Convert.ToInt32(hexCode, 16);
				string asciiStr = asciiVal >= 0x20 && asciiVal <= 0x7E ? ((char)asciiVal).ToString() : "";
				str = str.Substring(0, p) + asciiStr + str.Substring(p + 3, str.Length - p - 3);
			}
			return str;
		}
	}
}
