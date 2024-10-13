using ItelexChatServer.Languages;
using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ItelexCommon.Connection.ItelexConnection;

namespace ItelexChatServer.Notification
{
	class NotificationConnectionManager: OutgoingConnectionManagerAbstract
	{
		private const string TAG = nameof(NotificationConnectionManager);

		private const string NOTIFICATION_NAME = "notifications.xml";

		private List<NotificationSubscriptionItem> NotificationList { get; set; }

		public DateTime? LastWriteNotification = null;

		public List<NotificationSendQueueItem> NotificationQueue { get; set; }

		private readonly object _notificationQueueLock = new object();

		/*
		private readonly int[] _forbiddenNumbers =
		{
			11150, // rundsender paul
			11151, // rundsender willi
			11159, // rundsender willi
			11160, // konf. service
			11161, // konf. service
			11162, // rundsender
			11163, // rundsender
			11164, // bildlocher
			11166, // baudot-art
			11168, // chat-gpt
			11170, // mail-gateway
			11180, // news-server
			11181, // news-server
			11301, // telegram service werner
			11302, // telegram service finn
			118811, // auskunft
			121212, // telexgateway
			40140, // historische auskunft
			400365, // telegram service isbrand
			55555, // txp gateway
			881166, // baudot art willi
			881177, // test-server willi
			881188, // klkl server
			881199, // ls-recorder willi
			717171, // weather service detlef
			727272, // weather service willi
			737373, // weather service willi
			747474, // weather service willi
			757575, // weather service willi
			767676, // weather service willi
			787878, // weather service willi
			797979, // weather service willi
			97482, // bildlocher werner
			517682, // bildlocher henning
		
		*/

		/// <summary>
		/// singleton pattern
		/// </summary>
		//private static NotificationManager instance;

		//public static NotificationManager Instance => instance ?? (instance = new NotificationManager());

		private readonly System.Timers.Timer _sendNotificationTimer;
		private bool _sendNotificationTimer_Active;

		public NotificationConnectionManager()
		{
			LoadNotificationList();

			NotificationQueue = new List<NotificationSendQueueItem>();
			_sendNotificationTimer_Active = false;
			_sendNotificationTimer = new System.Timers.Timer(1000); // every second
			_sendNotificationTimer.Elapsed += SendNotificationTimer_Elapsed; ;
			_sendNotificationTimer.Start();
		}


		public bool AddNotification(NotificationSubscriptionItem item)
		{
			NotificationList.Add(item);
			return SaveNotificationList();
		}

		public bool RemoveNotification(NotificationSubscriptionItem item)
		{
			NotificationList.Remove(item);
			return SaveNotificationList();
		}

		public bool LoadNotificationList()
		{
			if (NotificationList != null) return true; // allready loaded

			string fullName = Path.Combine(Helper.GetExePath(), NOTIFICATION_NAME);
			_logger.Notice(TAG, nameof(LoadNotificationList), $"load {fullName}");

			try
			{
				string dataXml = File.ReadAllText(fullName);
				NotificationSaveData data = CommonHelper.DeserializeObject<NotificationSaveData>(dataXml);
				LogNotificationData(data);
				if (data.NotificationList != null)
				{
					NotificationList = data.NotificationList;
				}
				else
				{
					NotificationList = new List<NotificationSubscriptionItem>();
				}
				return true;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(LoadNotificationList), $"Error read notifications file {fullName}", ex);
				NotificationList = new List<NotificationSubscriptionItem>();
				return false;
			}
		}

		public bool SaveNotificationList()
		{
			string fullName = Path.Combine(Helper.GetExePath(), NOTIFICATION_NAME);

			_logger.Notice(TAG, nameof(SaveNotificationList), $"save {fullName}");

			try
			{
				NotificationSaveData data = new NotificationSaveData()
				{
					NotificationList = NotificationList
				};
				LogNotificationData(data);
				string dataXml = CommonHelper.SerializeObject<NotificationSaveData>(data);
				File.WriteAllText(fullName, dataXml);
				return true;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(SaveNotificationList), $"Error writing notifications file {fullName}", ex);
				return false;
			}
		}

		public void LogNotificationData(NotificationSaveData data)
		{
			if (data?.NotificationList == null)
			{
				_logger.Error(TAG, nameof(LogNotificationData), "NotificationData.NotificationList is null");
				return;

			}
			foreach (NotificationSubscriptionItem item in data.NotificationList)
			{
				_logger.Debug(TAG, nameof(LogNotificationData), item.ToString());
			}
		}

		public NotificationSubscriptionItem FindNotification(int number)
		{
			return (from n in NotificationList where n.ItelexNumber == number select n).FirstOrDefault();
		}

		/*
		public int[] GetNotificationNumbers()
		{
			return (from n in NotificationList select n.ItelexNumber).ToArray();
		}
		*/

		/*
		public bool IsForbiddenNumber(int number)
		{
			return (from n in _forbiddenNumbers where number==n select n).Any();
		}
		*/

		public void SendNotifications(IncomingChatConnection chatConn, NotificationTypes notificationType)
		{
			_logger.Notice(TAG, nameof(SendNotifications), $"[{chatConn.ConnectionName}] type={notificationType}");

			//if (_shutDown)
			//{
			//	// do not send logoff notification in case of shut down
			//	return;
			//}

			DateTime dt = DateTime.Now;
			string msg = "";
			switch (notificationType)
			{
				case NotificationTypes.Login:
					msg = $"{dt:HH:mm} login {chatConn.ConnectionShortName} {chatConn.RemoteAnswerbackStr}";
					break;
				case NotificationTypes.Logoff:
					msg = $"{dt:HH:mm} logoff {chatConn.ConnectionShortName} {chatConn.RemoteAnswerbackStr}";
					break;
				case NotificationTypes.Writing:
					msg = $"{dt:HH:mm} writing {chatConn.ConnectionShortName} {chatConn.RemoteAnswerbackStr}";
					break;
			}

			//List<NotificationSubscriptionItem> notificationList = _notificationManager.NotifcationList;

			foreach (NotificationSubscriptionItem item in NotificationList)
			{
				if (item.HasNotification(notificationType) && item.ItelexNumber != chatConn.RemoteNumber)
				{
					AddNotificationToQueue(item, msg, notificationType);
				}
			}
		}


		public void AddNotificationToQueue(NotificationSubscriptionItem item, string msg, NotificationTypes type)
		{
			lock (_notificationQueueLock)
			{
				NotificationSendQueueItem queueItem = (from n in NotificationQueue where n.ItelexNumber == item.ItelexNumber select n).FirstOrDefault();
				if (queueItem == null)
				{
					// add new entry
					queueItem = new NotificationSendQueueItem()
					{
						NotificationNumber = item,
						Type = type,
						Messages = new List<string>() { msg },
						Retries = 0,
						Success = false,
						LastTry = new TickTimer(true)
					};
					NotificationQueue.Add(queueItem);
				}
				else
				{
					// add message to existing entry
					queueItem.Messages.Add(msg);
				}
			}
		}

		private void SendNotificationTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (_sendNotificationTimer_Active || NotificationQueue.Count==0) return;

			_sendNotificationTimer_Active = true;
			List<int> faildNumbers = new List<int>();

			lock (_notificationQueueLock)
			{
				foreach (NotificationSendQueueItem queueItem in NotificationQueue)
				{
					if (queueItem.Success)
					{
						continue;
					}
					if (faildNumbers.Contains(queueItem.ItelexNumber))
					{
						continue;
					}
					// wait 10 seconds for first try, than 120 seconds for next tries
					int elapsedSeconds = queueItem.Retries == 0 ? 20 : 120;
					if (!queueItem.LastTry.IsElapsedSeconds(elapsedSeconds))
					{
						continue;
					}

					bool success = SendNotification(queueItem);
					string successStr = success ? "ok" : "err";
					queueItem.Success = success;
					queueItem.Retries++;
					MessageDispatcher.Instance.Dispatch( 
						$"send {queueItem.Type} notification to {queueItem.ItelexNumber} {successStr} ({queueItem.Retries}. try)");
					queueItem.LastTry.Start(); // restart timer
					if (!success)
					{
						faildNumbers.Add(queueItem.ItelexNumber);
					}
				}

				// delete entries that where successfully send and entries that failed 3 times
				NotificationQueue = (from n in NotificationQueue where !n.Success && n.Retries < 3 select n).ToList();
			}

			_sendNotificationTimer_Active = false;
		}

		private bool SendNotification(NotificationSendQueueItem queueItem)
		{
			_logger.Notice(TAG, nameof(SendNotification), $"{queueItem}");

			Debug.WriteLine(queueItem.ItelexNumber);

			int connectionId = Helper.GetNewSessionNo(1);

			try
			{
				string comment = "";
				switch(queueItem.Type)
				{
					case NotificationTypes.SetupAdd:
						comment = "notify add";
						break;
					case NotificationTypes.SetupDelete:
						comment = "notify del";
						break;
					case NotificationTypes.Login:
						comment = "notify login";
						break;
					case NotificationTypes.Logoff:
						comment = "notify logoff";
						break;
					case NotificationTypes.Writing:
						comment = "notify writing";
						break;
					default:
						comment = "notify";
						break;
				}

				ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.In, null, 
					Constants.LOG_PATH, Constants.LOG_LEVEL, Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
				ItelexOutgoing outgoing = new ItelexOutgoing(connectionId, queueItem.ItelexNumber, comment, itelexLogger);

				ItelexOutgoingConfiguration config = new ItelexOutgoingConfiguration()
				{
					OutgoingType = new LoginSeqTypes[] { LoginSeqTypes.SendKg },
					OurAnswerbackStr = Constants.ANSWERBACK_DE,
					OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
					ItelexNumber = queueItem.ItelexNumber,
					RetryCnt = queueItem.Retries,
				};
				AddConnection(outgoing);
				outgoing.StartOutgoing(config);

				DispatchUpdateOutgoing();

				Thread.Sleep(2000);
				if (!outgoing.IsConnected || outgoing.RejectReason != null)
				{
					_logger.Notice(TAG, nameof(SendNotification), $"Disconnected by remote, reject-reason={outgoing.RejectReason}");
					outgoing.Dispose();
					return false;
				}

				if (string.IsNullOrEmpty(queueItem.NotificationNumber.Language))
				{
					queueItem.NotificationNumber.Language = "de";
				}
				Language lng = LanguageManager.Instance.GetLanguageByShortname(queueItem.NotificationNumber.Language);
				outgoing.SendAscii($"\r\n{LanguageManager.Instance.GetText((int)LngKeys.NotificationText, lng.Id, new string[] { $"{DateTime.Now:dd.MM.yyyy HH:mm}" })}:");
				foreach(string msg in queueItem.Messages)
				{
					outgoing.SendAscii($"\r\n{msg}");
				}
				outgoing.SendAscii($"\r\n\r\n");

				outgoing.WaitAllSendBuffersEmpty();

				outgoing.SendEndCmd();
				Thread.Sleep(2000);
				_logger.Debug(TAG, nameof(SendNotification), $"Disconnect()");
				outgoing.Disconnect(ItelexConnection.DisconnectReasons.Logoff);
				outgoing.Dispose();
				RemoveConnection(outgoing);
				_logger.Notice(TAG, nameof(SendNotification), $"Notification send to {queueItem.ItelexNumber}");
				return true;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(SendNotification), $"Error sending notification to {queueItem.ItelexNumber}", ex);
				return false;
			}
		}

	}
}
