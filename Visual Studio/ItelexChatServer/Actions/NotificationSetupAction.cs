using ItelexChatServer.Languages;
using ItelexChatServer.Notification;
using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Actions
{
	class NotificationSetupAction : ActionBase
	{
		private const string TAG = nameof(NotificationSetupAction);

		private readonly NotificationConnectionManager _notificationManager;

		//private const string INPUT_PROMPT = ":";

		private const string NOTIFICATION_NAME = "notifications.xml";

		public NotificationSetupAction(Language language, ActionBase.ActionCallTypes actionCallType, ItelexLogger itelexLogger) : 
				base(Actions.CommandMode, language, actionCallType, itelexLogger)
		{
			_notificationManager = (NotificationConnectionManager)GlobalData.Instance.OutgoingConnectionManager;
		}

		public override void Run(IncomingChatConnection chatConnection, bool debug)
		{
			base.Run(chatConnection, debug);

			_notificationManager.LoadNotificationList();

			NotificationSetup();

			if (_actionCallType == ActionCallTypes.FromCmd)
			{
				// return to command mode
				_chatConnection.StartCommandMode();
			}
		}

		public void NotificationSetup()
		{
			_chatConnection.SendAscii("\r\n");
			InputResult inputResult = _chatConnection.InputSelection(
					LngText(LngKeys.NotificationStartMsg), ShiftStates.Ltrs, null, new string[] { "+", "-", "l" }, 1, 1, false, true);
			if (inputResult.IsHelp)
			{
				_chatConnection.SendAscii(LngText(LngKeys.NotificationHelp));
				_chatConnection.SendAscii("\r\n");
				return;
			}
			switch (inputResult.InputString)
			{
				case "+":
					AddNotification();
					break;
				case "-":
					DeleteNotification();
					break;
				case "l":
					ListNotifications();
					break;
			}
			return;
		}

		private void AddNotification()
		{
			//MessageDispatcher.Instance.Dispatch("notifier add number");

			NotificationSubscriptionItem notification = GetInputsForAdd();
			/*
			for (int i=0; i<3; i++)
			{
				notification = GetInputsForAdd();
				if (notification != null) break;
			}
			*/
			if (notification == null) return;

			MessageDispatcher.Instance.Dispatch(
					_chatConnection.ConnectionId, $"notifier add number {notification.ItelexNumber}");

			_notificationManager.AddNotification(notification);

			string user = _chatConnection.ConnectionShortName + " " + _chatConnection.RemoteAnswerback;

			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(AddNotification), 
					$"Notification now active for {notification.ItelexNumber} {notification.Extension} user={user}");

			_chatConnection.SendAscii(LngText(LngKeys.NotificationNowActive, notification.ItelexNumber.ToString()));
			//_chatConnection.SendAscii($"\r\n" + LngText(LngKeys.NotificationPresentPin, pin.ToString()));

			string msg = 
				LanguageManager.Instance.GetText((int)LngKeys.NotificationAddConfMsg, Language.Id,
					new string[] { notification.ItelexNumber.ToString(), user }) +
				"\r\n" + LanguageManager.Instance.GetText((int)LngKeys.NotificationAbuseMsg, Language.Id,
					new string[] { CommonConstants.ADMIN_ITELEX_MUNBER.ToString() });
			//ConnectionManager.Instance.SendNotification(_chatConnection, notification, msg, NotificationTypes.SetupAdd);
			_notificationManager.AddNotificationToQueue(notification, msg, NotificationTypes.SetupAdd);
		}

		private NotificationSubscriptionItem GetInputsForAdd()
		{
			SubscriberServer subsServer = new SubscriberServer();

			for (int i = 0; i < 3; i++)
			{
				_chatConnection.SendAscii("\r\n");
				InputResult inputResult = _chatConnection.InputNumber(LngText(LngKeys.NotificationEnterNumber), null, 1);
				if (inputResult == null || inputResult.ErrorOrTimeoutOrDisconnected || string.IsNullOrWhiteSpace(inputResult.InputString)) return null;
				if (inputResult.InputNumber == 0 ||
					!subsServer.CheckNumberIsValid(inputResult.InputNumber) || 
					subsServer.CheckNumberIsService(inputResult.InputNumber))
				{
					_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(AddNotification), "invalid number.");
					_chatConnection.SendAscii(LngText(LngKeys.NotificationInvalidNumber));
					continue;
				}
				int number = inputResult.InputNumber;
				/*
				int pin;
				if (number == 211230 || number==905258 || number == 905259)
				{
					pin = 2112;
				}
				else
				{
					pin = CreatePin();
				}
				*/
				/*
				if (!CheckNumber(number))
				{
					_chatConnection.SendAscii(LngText(LngKeys.NotificationUnknownNumber, number.ToString()));
					_itelexLogger.ItelexLog(LogTypes.Info, TAG, nameof(AddNotification), $"Unknown number {number}");
					continue;
				}
				*/

				NotificationSubscriptionItem notification = _notificationManager.FindNotification(number);
				if (notification != null)
				{
					_chatConnection.SendAscii(LngText(LngKeys.NotificationAlreadyActive, number.ToString()));
					_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(AddNotification), $"Number already aktive {number}");
					continue;
				}

				inputResult = _chatConnection.InputNumber(LngText(LngKeys.NotificationEnterExtension), "0", 2);
				if (inputResult == null || inputResult.ErrorOrTimeoutOrDisconnected) return null;
				int extensionNumber = inputResult.InputNumber;
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(AddNotification), $"Extension number {extensionNumber}");

				notification = new NotificationSubscriptionItem()
				{
					ItelexNumber = number,
					Extension = extensionNumber,
					Language = Language.ShortName,
					User = $"{_chatConnection.ConnectionShortName} {_chatConnection.RemoteAnswerback}",
					//Pin = pin
				};

				string[] yesNoStr = LanguageDefinition.GetYesNo(Language.Id);

				inputResult = _chatConnection.InputYesNo(LngText(LngKeys.NotificationOnLogin), null, 2);
				if (inputResult == null || inputResult.ErrorOrTimeoutOrDisconnected) return null;
				notification.NotifyLogin = inputResult.InputBool;

				inputResult = _chatConnection.InputYesNo(LngText(LngKeys.NotificationOnLogoff), null, 2);
				if (inputResult == null || inputResult.ErrorOrTimeoutOrDisconnected) return null;
				notification.NotifyLogoff = inputResult.InputBool;

				inputResult = _chatConnection.InputYesNo(LngText(LngKeys.NotificationOnWriting), null, 2);
				if (inputResult == null || inputResult.ErrorOrTimeoutOrDisconnected) return null;
				notification.NotifyWriting = inputResult.InputBool;

				return notification;
			}
			return null;
		}

		private void DeleteNotification()
		{
			MessageDispatcher.Instance.Dispatch(_chatConnection.ConnectionId, "notify delete number");

			NotificationSubscriptionItem notification = null;
			for (int i = 0; i < 3; i++)
			{
				notification = GetInputsForDelete();
				if (notification != null) break;
			}
			if (notification == null) return;

			MessageDispatcher.Instance.Dispatch(_chatConnection.ConnectionId, $"notify delete number {notification.ItelexNumber}");

			_notificationManager.RemoveNotification(notification);

			string user = _chatConnection.ConnectionShortName + " " + _chatConnection.RemoteAnswerback;

			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(AddNotification), $"Notification deleted for {notification.ItelexNumber} user={user}");
			_chatConnection.SendAscii(LngText(LngKeys.NotificationDeleted, notification.ItelexNumber.ToString()));

			string msg =
				LanguageManager.Instance.GetText((int)LngKeys.NotificationDelConfMsg, Language.Id, new string[] { notification.ItelexNumber.ToString() }) +
				"\r\n" + LanguageManager.Instance.GetText((int)LngKeys.NotificationAbuseMsg, Language.Id);
			//ConnectionManager.Instance.SendNotification(_chatConnection, notification, msg, NotificationTypes.SetupAdd);
			_notificationManager.AddNotificationToQueue(notification, msg, NotificationTypes.SetupDelete);
		}

		private void ListNotifications()
		{
			//_chatConnection.SendAscii("\r\n");
			InputResult inputResult = _chatConnection.InputNumber(LngText(LngKeys.NotificationEnterNumber), null, 1);
			if (inputResult == null || string.IsNullOrWhiteSpace(inputResult.InputString)) return;


			int number = inputResult.InputNumber;
			NotificationSubscriptionItem notification = _notificationManager.FindNotification(number);
			if (notification == null)
			{
				_chatConnection.SendAscii(LngText(LngKeys.NotificationNone));
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ListNotifications), $"List notifications for {inputResult.InputNumber}: none");
			}
			else
			{
				string str = "";
				string[] prms = new string[] { number.ToString() };
				if (notification.NotifyLogin) str += $"{LngText(LngKeys.Notifications, prms)} ";
				if (notification.NotifyLogin) str += $"{LngText(LngKeys.NotificationLogin)} ";
				if (notification.NotifyLogoff) str += $"{LngText(LngKeys.NotificationLogoff)} ";
				if (notification.NotifyWriting) str += $"{LngText(LngKeys.NotificationWriting)}";
				str = str.Trim();
				_chatConnection.SendAscii(str);
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ListNotifications), $"List notifications for {inputResult.InputNumber}: {str}");
			}
		}

		private NotificationSubscriptionItem GetInputsForDelete()
		{
			_chatConnection.SendAscii("\r\n");
			InputResult inputResult = _chatConnection.InputNumber(LngText(LngKeys.NotificationEnterNumber), null, 1);
			if (inputResult == null || string.IsNullOrWhiteSpace(inputResult.InputString))
			{
				return null;
			}
			if (inputResult.InputNumber == 0)
			{
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(DeleteNotification), $"Invalid number");
				_chatConnection.SendAscii(LngText(LngKeys.NotificationInvalidNumber));
				return null;
			}
			int number = inputResult.InputNumber;
			NotificationSubscriptionItem notification = _notificationManager.FindNotification(number);
			if (notification == null)
			{
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(AddNotification), $"Notification not active for {number}");
				_chatConnection.SendAscii(LngText(LngKeys.NotificationNotActive, number.ToString()));
				return null;
			}

			/*
			inputResult = _chatConnection.InputNumber(LngText(LngKeys.NotificationEnterPin), null, 1);
			int pin = inputResult.InputNumber;
			if (pin != notification.Pin)
			{
				_chatConnection.SendAscii(LngText(LngKeys.NotificationInvalidPin, pin.ToString()));
				return;
			}
			*/
			return notification;
		}

		/*
		private int CreatePin()
		{
			Random rnd = new Random();
			int pin = rnd.Next(1234, 9876);
			return pin;
		}
		*/

		/*
		private bool CheckNumber(int number)
		{
			SubscriberServer server = new SubscriberServer();
			server.Connect();
			PeerQueryReply reply = server.SendPeerQuery(number);
			server.Disconnect();
			return reply != null && reply.Data != null;
		}
		*/

		public string LngText(LngKeys lngKey, string parameter)
		{
			return LanguageManager.Instance.GetText((int)lngKey, Language.Id, new string[] { parameter });
		}

		public string LngText(LngKeys lngKey, string[] parameters = null)
		{
			return LanguageManager.Instance.GetText((int)lngKey, Language.Id, parameters);
		}
	}
}
