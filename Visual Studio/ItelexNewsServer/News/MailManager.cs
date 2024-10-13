using ItelexCommon.Logger;
using ItelexCommon;
using System;
using System.Threading.Tasks;
using ItelexCommon.Utility;
using ItelexNewsServer.Data;
using ItelexCommon.Mail;

namespace ItelexNewsServer.News
{
	internal class MailManager
	{
		private const string TAG = nameof(MailManager);

		private NewsDatabase _database;
		private NewsManager _newsManager;
		private SubscriberServer _subscribeServer;
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

			_database = NewsDatabase.Instance;
			_newsManager = NewsManager.Instance;
			_subscribeServer = new SubscriberServer();
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
					_mailAgent.ReceiveAndProccessMails("ArchivNews", ProcessMail);
				}
				finally
				{
					_fetchMailsTimerActive = false;
					TaskManager.Instance.RemoveTask(Task.CurrentId);
				}
			});
		}

		private int ProcessMail(MailItem mailItem)
		{
			return DistributeMailToNewsChannel(mailItem);
		}

		private int DistributeMailToNewsChannel(MailItem mailItem)
		{
			_logger.Notice(TAG, nameof(DistributeMailToNewsChannel), $"{mailItem}");

			DateTime utcNow = DateTime.UtcNow;
			MailParseData data = ParseToAndSubjectAndBody(mailItem);
			if (data == null)
			{
				_logger.Notice(TAG, nameof(DistributeMailToNewsChannel), $"invalid subject format {mailItem.Subject}");
				return 0;
			}

			ChannelItem channel = _database.ChannelLoadById(data.LocalChannelNo);
			if (channel == null)
			{
				_logger.Notice(TAG, nameof(DistributeMailToNewsChannel), $"channel {data.LocalChannelNo} does not exist");
				return 0;
			}

			if (channel.ChannelType != ChannelTypes.Local)
			{
				_logger.Notice(TAG, nameof(DistributeMailToNewsChannel), $"channel {data.LocalChannelNo} is not a local channel");
				return 0;
			}

			if (!_subscribeServer.CheckNumberIsValid(data.SenderNumber))
			{
				_logger.Notice(TAG, nameof(DistributeMailToNewsChannel), $"invalid sender number {data.SenderNumber}");
				return 0;
			}

			if (!channel.IsPublic && channel.LocalPin != data.LocalChannelPin)
			{
				_logger.Notice(TAG, nameof(DistributeMailToNewsChannel), $"wrong pin {data.LocalChannelPin} for {data.LocalChannelNo}");
				return 0;
			}

			NewsManager.Instance.SendMessageToChannel(channel, data.SenderNumber.ToString(), data.Text);

			return 1;
		}

		private MailParseData ParseToAndSubjectAndBody(MailItem mailItem)
		{
			MailParseData data = new MailParseData();

			// subject
			string subject = mailItem.Subject.Trim().ToLower();
			if (string.IsNullOrEmpty(subject)) return null;
			if (!subject.StartsWith("#news")) return null;

			string[] parts = subject.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 3 && parts.Length != 4) return null;

			if (!int.TryParse(parts[1], out int channelNo)) return null;
			data.LocalChannelNo = channelNo;

			if (!int.TryParse(parts[2], out int senderNo)) return null;
			data.SenderNumber = senderNo;

			if (parts.Length == 4)
			{
				if (!CommonHelper.IsValidPin(parts[3])) return null;
				data.LocalChannelPin = parts[3];
			}

			// body
			data.Text = mailItem.Body;
			return data;
		}

		private string EmailToSubscriberMail(string email)
		{
			int pos1 = email.IndexOf("<");
			int pos2 = email.IndexOf(">");
			if (pos1 == -1 || pos2 == -1)
				return email.ToLower();

			email = email.Substring(pos1 + 1, pos2 - pos1 - 1);

			return email.ToLower();
		}

		private void DispatchMsg(string msg)
		{
			MessageDispatcher.Instance.Dispatch(msg);
			_logger.Debug(TAG, nameof(DispatchMsg), msg);
		}
	}
}
