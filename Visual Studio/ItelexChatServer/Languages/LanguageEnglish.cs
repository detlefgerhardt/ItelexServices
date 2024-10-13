using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Languages
{
	class LanguageEnglish
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.en, "en", "English", false);
			lng.Items = new Dictionary<int, string>
			{
				{ (int)LngKeys.ServiceName, "i-telex conference service" },
				//{ (int)LngKeys.InputAnswerback, "confirm your answerback with 'nl' (new line)" },
				//{ (int)LngKeys.NoAnswerbackReceived, "no answerback received." },
				{ (int)LngKeys.EnterShortName, "short name (? for help):" },
				{ (int)LngKeys.InputHelp, "please use a readable short name" },
				{ (int)LngKeys.ShortNameInUse, "this short name is currently in use." },
				//{ (int)LngKeys.LoginTerminated, "login aborted." },
				{ (int)LngKeys.HelpHint, "? for help" },
				{ (int)LngKeys.NewEntrant, "new entrant" },
				{ (int)LngKeys.HasLeft, "has left the conference." },
				{ (int)LngKeys.HoldConnection, "hold connection:" },
				{ (int)LngKeys.On, "on" },
				{ (int)LngKeys.Off, "off" },
				{ (int)LngKeys.Subscribers, "subscribers" },
				{ (int)LngKeys.None, "none" },
				{ (int)LngKeys.HistRestart, "restart" },
				{ (int)LngKeys.HistLogin, "login" },
				{ (int)LngKeys.HistLogoff, "logoff" },
				{ (int)LngKeys.Help,
						"? = help" +
						"\r\nBELL = write request (+? for end)" +
						"\r\nWRU = conference list" +
						"\r\n'=' = history" +
						"\r\n'/' = hold connection on/off" +
						"\r\n'+' = setup notifications" +
						"\r\nleave with st key" },
				{ (int)LngKeys.ShutDown, "the conference service was shut down for\r\nmaintenance." },
				{ (int)LngKeys.CmdHelp, 
						"commands in command mode:" +
						"\r\nhelp" +
						"\r\nend = exit command mode" +
						"\r\nhold on/off = hold connection" +
						"\r\nlist members" +
						"\r\nrun help" +
						"\r\nlogoff" },
				{ (int)LngKeys.CmdRunHelp,
						"\r\nrun hilfe" +
						"\r\nrun hamurabi" +
						"\r\nrun biorhyhtmus" },
				{ (int)LngKeys.CmdError, "cmd error" },

				{ (int)LngKeys.NotificationStartMsg, "change notifications (+,-,l,?) ?" },
				//{ (int)LngKeys.NotificationAddDelShow, "(+)new number, (-)delete number ?" },
				{ (int)LngKeys.NotificationEnterNumber, "i-telex number:" },
				{ (int)LngKeys.NotificationEnterExtension, "optional extension number:" },
				//{ (int)LngKeys.NotificationEnterPin, "pin number:" },
				{ (int)LngKeys.NotificationOnLogin, "when login in (y,n) ?" },
				{ (int)LngKeys.NotificationOnLogoff, "when loging off (y,n) ?" },
				{ (int)LngKeys.NotificationOnWriting, "when writing (y,n) ?" },
				{ (int)LngKeys.NotificationInvalidNumber, "invalid number." },
				{ (int)LngKeys.NotificationUnknownNumber, "number not found on subscription server." },
				{ (int)LngKeys.NotificationNone, "no notifications." },
				//{ (int)LngKeys.NotificationInvalidPin, "invalid pin-number @1." },
				{ (int)LngKeys.NotificationAlreadyActive, "notification already active for @1." },
				{ (int)LngKeys.NotificationNotActive, "notification not active for @1." },
				{ (int)LngKeys.NotificationNowActive, "notification is now active for @1." },
				//{ (int)LngKeys.NotificationPresentPin, "pin-nummer to delete the notification is: @1." },
				{ (int)LngKeys.NotificationDeleted, "notification for @1 deleted." },
				{ (int)LngKeys.NotificationText, "notification from conference service @1" },
				{ (int)LngKeys.NotificationAddConfMsg,
						"your i-telex number @1 was just added to the\r\n" +
						"conference service notifications.\r\n" +
						"user: @2" },
				{ (int)LngKeys.NotificationDelConfMsg,
						"your i-telex number @1 was just deleted from the\r\n" +
						"conference service notifications." },
				{ (int)LngKeys.NotificationAbuseMsg,
						"in case of abuse please send a note to @1." },
				{ (int)LngKeys.NotificationHelp,
					//  1234567890123456789012345678901234567890123456789012345678
						"here you can configure an i-telex number to which auto-\r\n" +
						"matically a notification will be sent when a user logs on\r\n" +
						"or off or writes a message. so you don't have to be perma-\r\n" +
						"nently online to know when something is happening.\r\n" +
						" + add notification\r\n" +
						" - remove notification\r\n" +
						" l list notifications\r\n" +
						" ? this help\r\n" +
						"to change an existing notification, delete it and create\r\n" +
						"a new one."},
				{ (int)LngKeys.Notifications, "notifications for @1:" },
				{ (int)LngKeys.NotificationLogin, "login" },
				{ (int)LngKeys.NotificationLogoff, "logoff" },
				{ (int)LngKeys.NotificationWriting, "writing" },

				{ (int)LngKeys.ConnectionTerminated, "the connection will be terminated." },
				{ (int)LngKeys.InternalError, "internal error." },
			};

			return lng;
		}
	}
}
