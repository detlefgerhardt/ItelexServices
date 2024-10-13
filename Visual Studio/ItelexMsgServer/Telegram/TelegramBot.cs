using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Update = Telegram.Bot.Types.Update;
using Message = Telegram.Bot.Types.Message;
using ItelexMsgServer.Data;
using ItelexCommon;
using ItelexMsgServer.Connections;
using ItelexCommon.Logger;
using ItelexCommon.Connection;
using ItelexMsgServer.Mail;

namespace ItelexMsgServer.Telegram
{
	class TelegramBot
	{
		/// <summary>
		/// singleton pattern
		/// </summary>
		private static TelegramBot instance;

		public static TelegramBot Instance => instance ?? (instance = new TelegramBot());

		private const string TAG = nameof(TelegramBot);

		private CancellationTokenSource cts;
		private TelegramBotClient _bot;
		//private User me;

		protected Logging _logger;
		private MsgServerDatabase _database;
		private MailManager _mailManager;
		private OutgoingManager _outgoingManager;

		private List<TelegramConnection> _telegramConnections;
		private System.Timers.Timer _timeoutTimer;
		private bool _timeoutTimerActive;
		private DateTime _init;

		private TelegramBot()
		{
			_logger = LogManager.Instance.Logger;
			_database = MsgServerDatabase.Instance;
			_mailManager = MailManager.Instance;
			_outgoingManager = (OutgoingManager)GlobalData.Instance.OutgoingConnectionManager;
			_telegramConnections = new List<TelegramConnection>();
			_init = DateTime.UtcNow;

			_timeoutTimerActive = false;
			_timeoutTimer = new System.Timers.Timer(1000);
			_timeoutTimer.Elapsed += _TimeoutTimer_Elapsed; ;
			_timeoutTimer.Start();
		}

		private async void _TimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
#if DEBUG
			const int TIMEOUT = 120;
#else
			const int TIMEOUT = 300;
#endif

			if (_timeoutTimerActive) return;
			try
			{
				_timeoutTimerActive = true;

				DateTime utcNow = DateTime.UtcNow;
				for (int i = _telegramConnections.Count - 1; i >= 0; i--)
				{
					TelegramConnection teleConn = _telegramConnections[i];
					if (utcNow.Subtract(teleConn.LastSendTimeUtc).TotalSeconds > TIMEOUT)
					{
						// timeout
						await CloseConnection(teleConn.Chat, teleConn);
					}
				}
			}
			finally
			{
				_timeoutTimerActive = false;
			}
		}

		public async void Start()
		{
			cts = new CancellationTokenSource();
			_bot = new TelegramBotClient(Constants.TELEGRAM_BOT_TOKEN);
			_bot.StartReceiving(OnUpdate, OnError);
			User me = await _bot.GetMeAsync();
			Debug.WriteLine($"bot name={me.Username}  bot id={me.Id}");

#if RELEASE
			try
			{
				long chatId = 7223032866;
				await _bot.SendTextMessageAsync(chatId, $"i-telex bot is up again.");
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(CmdSend), "error", ex);
			}
#endif
		}

		// CancellationToken not working
		public void Stop()
		{
			cts.Cancel(); // stop the bot
		}

		// CancellationToken not working
		private async Task OnError(ITelegramBotClient client, Exception exception, CancellationToken ct)
		{
			Console.WriteLine(exception);
			await Task.Delay(2000, ct);
		}

		// method that handle updates coming for the bot:
		// CancellationToken not working
		private async Task OnUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
		{
			// igonre all msgs for 10 seconds to cleanup chat at start up
			if (DateTime.UtcNow.Subtract(_init).TotalSeconds < 10) return;

			if (update.Message is null) return; // we want only updates about new Message
			if (update.Message.Text is null) return; // we want only updates about new Text Message

			Message msg = update.Message;
			Chat chat = msg.Chat;

			_logger.Info(TAG, nameof(OnUpdate), $"Received message '{msg.Text}' {msg.Date} in @{chat.Username}");

			DateTime now = DateTime.UtcNow;
			if (msg.Date.AddSeconds(5 * 60) < now)
			{
				_logger.Notice(TAG, nameof(OnUpdate), $"msg is outdated (5 mins)");
				return; // older than 5 minutes
			}
			if (string.IsNullOrEmpty(msg.Text))
			{
				_logger.Info(TAG, nameof(OnUpdate), $"msg is empty {now} {msg.Date}");
				return; // empty
			}

			string text = msg.Text.ToLower();
			if (!text.StartsWith("/"))
			{
				TelegramConnection teleConn = (from c in _telegramConnections
											   where chat.Id == c.Chat.Id
											   select c).FirstOrDefault();
				if (teleConn != null)
				{
					// existing connection: send text directly to telex
					_logger.Info(TAG, nameof(CmdSend), $"send '{text}' to user '{msg.Chat.Username}'");
					string convText = ConvTelegramMsg(msg.Text);
					teleConn.OutgoingConn.SendAscii(convText + "\r\n");
					teleConn.UpdateLastSendTime();
					return;
				}

				await _bot.SendTextMessageAsync(chat, $"no command (use /help");
				return;
			}

			string[] parts = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0) return;
			string cmd = parts[0];
			_logger.Debug(TAG, nameof(OnUpdate), $"cmd='{cmd}'");

			switch (cmd)
			{
				case "/help":
					await CmdHelp(msg, text);
					break;
				case "/send":
					await CmdSend(msg, text);
					break;
				case "/at":
					await CmdAt(msg.Chat, text);
					break;
				case "/st":
					await CmdSt(msg.Chat);
					break;
				case "/link":
					await CmdLink(msg.Chat, text);
					break;
				default:
					await _bot.SendTextMessageAsync(chat, $"invalid command '{text}'");
					break;
			}

			// let's echo back received text in the chat
			//await bot.SendTextMessageAsync(msg.Chat, $"{msg.From} said: {msg.Text}");
		}

		private async Task CmdHelp(Message msg, string text)
		{
			await _bot.SendTextMessageAsync(msg.Chat,
					"Commands:\r\n" +
					"/send number message - Send a message to an i-Telex number.\r\n" +
					"/at number - Open a connection to an i-Telex number.\r\n" +
					"/st number - Close a connection.\r\n" +
					"/link number - Link this chat to an i-Telex account.\r\n" +
					"If a connection is opend all text is send over this connection.\r\n");
		}

		private async Task CmdSend(Message msg, string text)
		{
			_logger.Info(TAG, nameof(CmdSend), $"user={msg.Chat}, text={text}");
			DispatchMsg(null, $"@{msg.Chat.Username}: {text}");

			Chat chat = msg.Chat;
			string[] parts = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 3)
			{
				await _bot.SendTextMessageAsync(msg.Chat, $"incomplete command '{text}'");
				DispatchMsg(null, $"incomplete command");
				return;
			}
			string numStr = parts[1];
			if (!int.TryParse(numStr, out int num))
			{
				await _bot.SendTextMessageAsync(chat, $"invalid number '{num}'");
				DispatchMsg(null, $"invalid number '{num}'");
				return;
			}

			UserItem userItem = _database.UserLoadByTelexNumber(num);
			if (userItem == null || !userItem.Activated || !userItem.AllowRecvTelegram.Value)
			{
				// not registered
				await _bot.SendTextMessageAsync(chat, $"{num} not registered for telegram messages");
				DispatchMsg(null, $"{num} not registered for telegram messages");
				return;
			}

			parts = parts.Skip(2).ToArray();
			text = string.Join(" ", parts);

			//if (userItem.Public)
			//{
			//	text = _mailManager.GetCensoredMessage(text);
			//}

			string uid = $"telegram {msg.From} {Guid.NewGuid()}";
			uid = uid.Replace(" ", "_");

			MsgItem msgItem = new MsgItem()
			{
				UserId = userItem.UserId,
				MsgType = (int)MsgTypes.Telegram,
				CreateTimeUtc = DateTime.UtcNow,
				Sender = $"telegram (at){msg.From.Username}",
				Subject = null,
				Uid = uid,
				Message = text,
				LineCount = 0,
				MailTimeUtc = DateTime.UtcNow,
				SendRetries = 0,
				SendStatus = 0,
			};

			if (!_database.MsgsInsert(msgItem))
			{
				await _bot.SendTextMessageAsync(chat, $"error inserting message to {num}");
				DispatchMsg(null, $"error inserting message to {num}");
				return;
			}

			await _bot.SendTextMessageAsync(chat, $"sending telex to {num}");
			_logger.Info(TAG, nameof(CmdSend), $"sending telex to {num}");
		}

		private async Task CmdAt(Chat chat, string text)
		{
			_logger.Info(TAG, nameof(CmdAt), $"user=@{chat.Username}, text={text}");
			DispatchMsg(null, $"@{chat.Username}: {text}");

			string[] parts = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2)
			{
				await _bot.SendTextMessageAsync(chat, $"incomplete command");
				return;
			}
			string numStr = parts[1];
			if (!int.TryParse(numStr, out int num))
			{
				await _bot.SendTextMessageAsync(chat, $"invalid number '{num}'");
				return;
			}

			UserItem userItem = _database.UserLoadByTelexNumber(num);
			if (userItem == null || !userItem.Activated || !userItem.AllowRecvTelegram.Value)
			{
				// not registered
				await _bot.SendTextMessageAsync(chat, $"{num} not registered for telegram messages");
				//DispatchMsg(msg);
				return;
			}

			string ourAnswerback = "telegram bot";
			ItelexOutgoing outgoing = _outgoingManager.OpenTelegramConnection(userItem.ItelexNumber, ourAnswerback, out CallResult callResult);
			if (callResult.CallStatus == CallStatusEnum.Ok)
			{
				await _bot.SendTextMessageAsync(chat, outgoing.RemoteAnswerbackStr + "\r\n");
				await _bot.SendTextMessageAsync(chat, ourAnswerback + "\r\n");

				TelegramConnection conn = new TelegramConnection(userItem.ItelexNumber, outgoing, chat);
				outgoing.ItelexReceived += Outgoing_Received;				
				_telegramConnections.Add(conn);
			}
			else
			{
				await _bot.SendTextMessageAsync(chat, callResult.RejectReason + "\r\n");
			}
		}

		private async Task CmdSt(Chat chat)
		{
			_logger.Info(TAG, nameof(CmdSt), $"user=@{chat.Username}");
			DispatchMsg(null, $"@{chat.Username} /st");
			TelegramConnection teleConn = (from c in _telegramConnections
										   where chat.Id == c.Chat.Id
										   select c).FirstOrDefault();
			if (teleConn == null) return;
			await CloseConnection(chat, teleConn);
		}

		private async Task CmdLink(Chat chat, string text)
		{
			_logger.Info(TAG, nameof(CmdAt), $"user=@{chat.Username}, text={text}");
			DispatchMsg(null, $"@{chat.Username}: {text}");

			string[] parts = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2)
			{
				await _bot.SendTextMessageAsync(chat, $"incomplete command");
				return;
			}
			string numStr = parts[1];
			if (!int.TryParse(numStr, out int num))
			{
				await _bot.SendTextMessageAsync(chat, $"invalid number '{num}'");
				return;
			}

			UserItem userItem = _database.UserLoadByTelexNumber(num);
			if (userItem == null || !userItem.Activated || !userItem.AllowRecvTelegram.Value)
			{
				// not registered
				await _bot.SendTextMessageAsync(chat, $"{num} not registered for telegram messages");
				//DispatchMsg(msg);
				return;
			}

			userItem.TelegramChatId = chat.Id.ToString();
			_database.UserUpdate(userItem);

			await _bot.SendTextMessageAsync(chat, $"Chat-Id {chat.Id} linked to i-telex account {num}.");
		}

		private async Task CloseConnection(Chat chat, TelegramConnection teleConn)
		{
			_logger.Info(TAG, nameof(CloseConnection), $"user=@{chat.Username} itelex={teleConn.ItelexNumber}");
			while (true)
			{
				string line = teleConn.GetLine(false);
				if (line.Length == 0) break;
				await _bot.SendTextMessageAsync(chat, line);
			}
			_outgoingManager.CloseTelegramConnection(teleConn.ItelexNumber);
			_telegramConnections.Remove(teleConn);

			await _bot.SendTextMessageAsync(chat, $"connection to {teleConn.ItelexNumber} closed.");
			DispatchMsg(null, $"{teleConn.ItelexNumber}: connection closed");
		}

		private async void Outgoing_Received(ItelexConnection connection, string asciiText)
		{
			TelegramConnection teleConn = (from c in _telegramConnections
										   where c.OutgoingConn.ConnectionId == connection.ConnectionId
										   select c).FirstOrDefault();
			if (teleConn == null || asciiText == "") return;

			teleConn.AddText(asciiText);
			string line = teleConn.GetLine(false);
			if (line.Length > 0)
			{
				await _bot.SendTextMessageAsync(teleConn.Chat, line);
			}
		}

		public string ConvTelegramMsg(string msg)
		{
			if (string.IsNullOrWhiteSpace(msg)) return "";
			msg = msg.ToLower();

			msg = msg.Replace("@", "(at)");
			msg = msg.Replace("#", "+");
			msg = msg.Replace("&", "+");
			msg = msg.Replace("%", "o/o");
			msg = msg.Replace("\"", "'");
			msg = msg.Replace("''", "'");
			msg = msg.Replace("„", "'");
			msg = msg.Replace("“", "'");
			msg = msg.Replace("`", "'");
			msg = msg.Replace("´", "'");
			msg = msg.Replace("\u2013", "-");
			msg = msg.Replace("~", "-");
			msg = msg.Replace("!", ".");
			msg = msg.Replace("*", "x");
			msg = msg.Replace("•", "-");

			msg = msg.Replace("€", "euro");
			msg = msg.Replace("  ", " ");

			return CodeManager.AsciiStringReplacements(msg, CodeSets.ITA2, false, false);
		}

		private void DispatchMsg(int? connectionId, string msg)
		{
			if (connectionId.HasValue)
			{
				MessageDispatcher.Instance.Dispatch(connectionId, msg);
				_logger.Debug(TAG, nameof(DispatchMsg), $"{connectionId} {msg}");
			}
			else
			{
				MessageDispatcher.Instance.Dispatch(msg);
				_logger.Debug(TAG, nameof(DispatchMsg), $"{msg}");
			}
		}
	}

	public class TelegramConnection
	{
		public int ItelexNumber { get; set; }

		public ItelexOutgoing OutgoingConn { get; set; }

		public Chat Chat { get; set; }

		//public Message Msg { get; set; }

		public DateTime LastSendTimeUtc { get; set; }

		public string CurrentLine { get; set; }

		public TelegramConnection(int itelexNumber, ItelexOutgoing conn, Chat chat)
		{
			ItelexNumber = itelexNumber;
			OutgoingConn = conn;
			Chat = chat;
			CurrentLine = "";
			UpdateLastSendTime();
		}

		public void UpdateLastSendTime()
		{
			LastSendTimeUtc = DateTime.UtcNow;
		}

		public void AddText(string text)
		{
			CurrentLine += text;
			UpdateLastSendTime();
		}

		public string GetLine(bool force)
		{
			if (CurrentLine.Length == 0) return "";
			string line = "";
			if (force)
			{
				line = CurrentLine;
				CurrentLine = "";
			}
			else
			{
				int crPos = CurrentLine.IndexOf('\r');
				if (crPos == -1) return "";
				line = CurrentLine.Substring(0, crPos);
				CurrentLine = CurrentLine.Substring(crPos + 1, CurrentLine.Length - crPos - 1);
			}
			line = line.Replace("\n", "");
			return line;
		}
	}
}
