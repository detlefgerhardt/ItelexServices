using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Languages
{
	class LanguageEnglish
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.en, "en", "English", false);
			lng.Items = new Dictionary<int, string>
			{
				{ (int)LngKeys.Yes, "yes" },
				{ (int)LngKeys.No, "no" },
				{ (int)LngKeys.Ok, "ok" },

				{ (int)LngKeys.ServiceName, "i-telex mail/fax service (en)" },
				{ (int)LngKeys.ConfirmOwnNumber, 
				//   12345678901234567890123456789012345678901234567890123456789012345678
					"confirm own i-telex number with 'nl' (new line) or enter correct\r\nnumber" },
				{ (int)LngKeys.EnterValidNumber, "enter valid i-telex number." },

				{ (int)LngKeys.EnterLoginPin, "enter your pin" },
				{ (int)LngKeys.EnterOldPin, "old pin" },
				{ (int)LngKeys.EnterNewPin, "new pin" },
				{ (int)LngKeys.EnterNewPinAgain, "repeat new pin" },
				{ (int)LngKeys.WrongPin, "wrong pin." },
				{ (int)LngKeys.InvalidPin, "invalid new pin." },
				{ (int)LngKeys.PinsNotEqual, "pins not identical." },
				{ (int)LngKeys.PinChanged, "pin changed." },
				{ (int)LngKeys.PinNotChanged, "pin not changed." },
				{ (int)LngKeys.SendNewLoginPin, "send new pin (y/n)" },

				{ (int)LngKeys.NotRegistered, "number @1 is not registered.\r\ncreate new account (y/n)" },
				{ (int)LngKeys.Deactivated, "the account is deactivated. please contact the maintainer of this\r\nservice." },
				{ (int)LngKeys.NewAccountCreated, "a new account was created." },
				//                                      12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.NewAccountActivated, "your account is now activated." },
				{ (int)LngKeys.NewAccountTimezoneInfo, "the timezone is required for the correct display of the message\r\n" +
				//   12345678901234567890123456789012345678901234567890123456789012345678
					"time. no automatic daylight saving time changeover."},
				{ (int)LngKeys.NewAccountEnterTimezone, "your timezone" },

				{ (int)LngKeys.SendNewPinMsg, "a new pin will now be send to the number @1.\r\n" +
					"please call again and login with the new pin to activate the\r\n" +
					"account." },

				{ (int)LngKeys.CmdPrompt, "cmd" },
				{ (int)LngKeys.InvalidCommand, "invalid command ('help')." },
				{ (int)LngKeys.CommandNotYetSupported, "command not yet supported." },
				{ (int)LngKeys.PendingMailsCleared, "@1 pending messages cleared." },
				{ (int)LngKeys.SendingTimeFromTo, "send time from @1 to @2 o'clock." },
				{ (int)LngKeys.PauseActive, "pause active." },
				{ (int)LngKeys.PauseInactive, "pause inactive." },
				{ (int)LngKeys.ShowSenderActive, "show sender active." },
				{ (int)LngKeys.ShowSenderInactive, "show sender inactive." },
				{ (int)LngKeys.AllowedSender, "allowed sender is '@1'." },
				{ (int)LngKeys.AllowedSenderOff, "allowed sender is off." },
				//   12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.InvalidTimezone, "invalid timezone (-12 to 14)." },
				{ (int)LngKeys.NoAutomaticDst, "no automatic daylight saving time." },
				{ (int)LngKeys.ActualTimezone, "timezone is now @1." },
				{ (int)LngKeys.MailOrFaxReceiver, "receiver" },
				{ (int)LngKeys.MailSubject, "subject" },
				{ (int)LngKeys.MailTime, "time" },
				{ (int)LngKeys.MailFrom, "from" },
				{ (int)LngKeys.MailTo, "to" },
				{ (int)LngKeys.MailHeader, "This is a message from i-telex number @1." },

				{ (int)LngKeys.InputMessage, "send message (end with +++)" },
				{ (int)LngKeys.MailSendSuccessfully, "mail sent." },
				{ (int)LngKeys.MailSendError, "error sending the message." },
				{ (int)LngKeys.InvalidMailAdress, "invalid mail address." },
				{ (int)LngKeys.InvalidEventCode, "invalid event code (1000-9999)." },
				{ (int)LngKeys.FaxWillBeSend, "the fax will be send to @1." },
				{ (int)LngKeys.InvalidFaxNumber, "invalid fax number." },
				{ (int)LngKeys.ForbiddenFaxNumber, "foreign or service numbers not allowed." },

				{ (int)LngKeys.PunchTapeSend, "send punch tape now (end with 3 x bell)" },
				{ (int)LngKeys.PunchTapeFilename, "name for the punch tape file" },
				{ (int)LngKeys.Filename, "filename" },
				{ (int)LngKeys.PunchTapeMailHeader, "As attachment the punch tape file @1 from i-telex-number @2." },
				{ (int)LngKeys.InvalidFilename, "invalid filename." },
				{ (int)LngKeys.InvalidData, "received invalid or incomplete data." },

				{ (int)LngKeys.SettingAllowedSender, "allowed sender: @1" },
				{ (int)LngKeys.SettingAllowRecvMails, "allow receiving emails: @1" },
				{ (int)LngKeys.SettingAllowRecvTelegram, "allow receiving telegram: @1" },
				{ (int)LngKeys.SettingMaxMailsPerDay, "max. mails per day: @1 (@2)" },
				{ (int)LngKeys.SettingMaxLinesPerDay, "max. lines per day: @1 (@2)" },
				{ (int)LngKeys.SettingMaxPendMails, "max. pending mails: @1 (@2)" },

				{ (int)LngKeys.SettingTelegramChatId, "telegram chatid: @1" },
				{ (int)LngKeys.SettingTelegramChatIdLinked, "telegram chatid linked." },
				{ (int)LngKeys.TelegramChatIdInvalid, "telegram chatid invalid." },
				{ (int)LngKeys.TelegramChatIdCanNotBeLinked, "telegram chatid could not be linked." },

				{ (int)LngKeys.SettingAssociatedMailAddr, "associated mail address: @1" },
				{ (int)LngKeys.SettingShowSenderAddr, "show sender address: @1" },
				{ (int)LngKeys.SettingEventPin, "event pin: @1" },

				{ (int)LngKeys.SettingNumber, "number: @1" },
				{ (int)LngKeys.SettingPending, "pending msgs: @1" },
				{ (int)LngKeys.SettingHours, "hours: @1 to @2 o'clock" },
				{ (int)LngKeys.SettingPaused, "paused: @1" },
				{ (int)LngKeys.SettingTimezone, "timezone: @1" },

				{ (int)LngKeys.SendRegistrationPinText, "new mail/fax-service pin for number @1 is: @2" },
				{ (int)LngKeys.SendRedirectionPinText, "confirmation pin for redirection from @1 to @2 is: @3\r\n" +
						"Enter this pin the next time you call the mail/fax-service to\r\nenable redirection."},
				//                                  12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.SendChangedPinText, "the mail/fax-service pin for your number @1 was changed.\r\nnew pin: @2\r\n" },

				{ (int)LngKeys.PruefTextNotFound, "test text '@1' not found." },

				{ (int)LngKeys.ConnectionTerminated, "the connection will be terminated." },
				{ (int)LngKeys.ShutDown, "the service is shutdown for maintenance." },
				{ (int)LngKeys.Aborted, "aborted" },
				{ (int)LngKeys.InternalError, "internal error." },
			};

			return lng;
		}
	}
}
