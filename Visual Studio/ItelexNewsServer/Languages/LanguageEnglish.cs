using ItelexCommon;
using System.Collections.Generic;

namespace ItelexNewsServer.Languages
{
	class LanguageEnglish
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.en, "en", "English", false);
			lng.Items = new Dictionary<int, string>
			{
				{ (int)LngKeys.Y, "y" },
				{ (int)LngKeys.N, "n" },
				{ (int)LngKeys.Yes, "yes" },
				{ (int)LngKeys.No, "no" },
				{ (int)LngKeys.Ok, "ok" },

				{ (int)LngKeys.ServiceName, "i-telex news service (en)" },
				{ (int)LngKeys.ConfirmOwnNumber,
					"confirm own i-telex number with 'nl' (new line) or enter correct\r\nnumber" },
				{ (int)LngKeys.EnterValidNumber, "enter valid i-telex number." },

				{ (int)LngKeys.EnterLoginPin, "enter your pin" },
				{ (int)LngKeys.EnterOldPin, "old pin" },
				{ (int)LngKeys.EnterNewPin, "new pin" },
				{ (int)LngKeys.EnterNewPinAgain, "repeat new pin" },
				{ (int)LngKeys.WrongPin, "wrong pin." },
				{ (int)LngKeys.InvalidNewPin, "invalid new pin." },
				{ (int)LngKeys.PinsNotEqual, "pins not identical." },
				{ (int)LngKeys.PinChanged, "pin changed." },
				{ (int)LngKeys.PinNotChanged, "pin not changed." },
				{ (int)LngKeys.SendNewLoginPin, "send new pin (y/n)" },

				{ (int)LngKeys.NotRegistered, "number @1 is not registered.\r\ncreate new account (y/n)" },
				{ (int)LngKeys.Deactivated, "the account is deactivated. please contact the maintainer of this\r\nservice." },
				{ (int)LngKeys.NewAccountCreated, "a new account was created." },
				{ (int)LngKeys.NewAccountActivated, "your account is now activated." },
				//                                      12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.NewAccountTimezoneInfo, "the timezone is required for the correct display of the message\r\n" +
				//   12345678901234567890123456789012345678901234567890123456789012345678
					"time. no automatic daylight saving time changeover."},
				{ (int)LngKeys.NewAccountEnterTimezone, "your timezone" },

				{ (int)LngKeys.SendNewPinMsg, "a new pin will now be send to the number @1.\r\n" +
					"please call again and login with the new pin." },
				{ (int)LngKeys.EnterRedirectConfirmPin, "confirmation pin for redirection" },
				{ (int)LngKeys.RedirectActivated, "redirection to @1 is now active." },
				{ (int)LngKeys.RedirectAlreadyActive, "redirection to @1 is already active." },

				{ (int)LngKeys.CmdPrompt, "cmd" },
				{ (int)LngKeys.InvalidCommand, "invalid command ('help')." },
				{ (int)LngKeys.CommandNotYetSupported, "command not yet supported." },
				{ (int)LngKeys.SubscribeChannel, "subscribed to channel @1 '@2'." },
				{ (int)LngKeys.UnsubscribeChannel, "unsubscribed @1 '@2'." },
				{ (int)LngKeys.ChannelNotFound, "channel @1 not found." },
				{ (int)LngKeys.PendingMsgsCleared, "@1 pending messages cleared." },
				{ (int)LngKeys.SendingTimeFromTo, "send time from @1 to @2 o'clock." },
				{ (int)LngKeys.PauseActive, "pause active." },
				{ (int)LngKeys.PauseInactive, "pause inactive." },
				{ (int)LngKeys.InvalidRedirectNumber, "invalid i-telex number." },
				{ (int)LngKeys.RedirectInactive, "redirection inactive." },
				{ (int)LngKeys.SendRedirectConfirmPin, "confirmation pin will be send to @1.\r\n" +
				//   12345678901234567890123456789012345678901234567890123456789012345678
					"if you call from this number then terminate the connection now."},
				{ (int)LngKeys.RedirectNotConfirmed, "redirection not confirmed and delete." },
				{ (int)LngKeys.InvalidTimezone, "invalid timezone (-12 bis 14)." },
				{ (int)LngKeys.NoAutomaticDst, "no automatic daylight saving time." },
				{ (int)LngKeys.ActualTimezone, "timezone is now @1." },
				{ (int)LngKeys.ActualMsgFormat, "message format is now @1." },
				{ (int)LngKeys.MsgFormatStandard, "standard" },
				{ (int)LngKeys.MsgFormatShort, "short" },
				{ (int)LngKeys.NoMatchingChannels, "no matching channels." },

				{ (int)LngKeys.SettingNumber, "number: @1" },
				{ (int)LngKeys.SettingPending, "pending msgs: @1" },
				{ (int)LngKeys.SettingHours, "hours: @1 to @2 o'clock" },
				{ (int)LngKeys.SettingRedirect, "redirection: @1" },
				{ (int)LngKeys.SettingPaused, "paused: @1" },
				{ (int)LngKeys.SettingTimezone, "timezone: @1" },
				{ (int)LngKeys.SettingMsgFormat, "message format: @1" },
				{ (int)LngKeys.SettingMaxPendMsgs, "max. pending msgs: @1" },

				{ (int)LngKeys.SendRegistrationPinText, "new news-service pin for number @1 is: @2" },
				//                                      12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.SendRedirectionPinText, "confirmation pin for redirection from @1 to @2\r\nis: @3\r\n" +
				//       12345678901234567890123456789012345678901234567890123456789012345678
						"Enter this pin the next time you call the news-service to enable\r\nredirection."},
				{ (int)LngKeys.SendChangedPinText, "the news-service pin for your number @1 was changed.\r\nnew pin: @2\r\n" },
				{ (int)LngKeys.SendChangeNotificationText, "changes to your news-service subcription" },

				{ (int)LngKeys.ConnectionTerminated, "the connection will be terminated." },
				{ (int)LngKeys.ShutDown, "the service is shutdown for maintenance." },
				{ (int)LngKeys.Aborted, "aborted" },
				{ (int)LngKeys.InternalError, "internal error." },

				{ (int)LngKeys.LocalChannels, "local channels" },
				{ (int)LngKeys.NoLocalChannels, "no local channels found." },
				{ (int)LngKeys.LocalChannelName, "channel name" },
				{ (int)LngKeys.LocalChannelNo, "local channel number" },
				{ (int)LngKeys.IsChannelPublic, "public channel (y/n)" },
				{ (int)LngKeys.ChannelLanguage, "language" },
				{ (int)LngKeys.InvalidLanguage, "invalid language '@1'" },
				{ (int)LngKeys.Pin, "channel pin" },
				{ (int)LngKeys.InvalidChannelPin, "invalid pin '@1'." },
				{ (int)LngKeys.InvalidChannelNo, "invalid channel no '@1'." },
				{ (int)LngKeys.ChannelNameExists, "channel '@1' already exists." },
				{ (int)LngKeys.ChannelCreatedAndSelected, "channel @1 created and selected." },
				{ (int)LngKeys.NoChannelSelected, "no channel selected." },
				{ (int)LngKeys.ChannelDataChanged, "channel data changed." },
				{ (int)LngKeys.ChannelSelected, "channel @1 selected." },
				{ (int)LngKeys.ChannelDeleteNotAllowed, "you are not allowed to delete this channel." },
				{ (int)LngKeys.DeleteChannelWithSubscriptions, "delete channel @1 with @2 subscriptions (y/n)" },
				{ (int)LngKeys.ChannelDeleted, "channel @1 deleted." },
				{ (int)LngKeys.NoNumbersInChannel, "no subscribers for channel @1." },
				{ (int)LngKeys.ShowNumbersNotAllowed, "@1 is not a local public channel." },
				{ (int)LngKeys.ChannelHeader, "channel @1" },
				{ (int)LngKeys.NotALocalChannel, "@1 is not a local channel." },
				{ (int)LngKeys.MsgTitle, "title" },
				{ (int)LngKeys.SendToChannel, "send to channel @1." },
				{ (int)LngKeys.SendACopy, "copy to @1 (y/n)" },
				{ (int)LngKeys.NoSubscribersWhenSending, "this channel has no subscribers. send anyway (y/n)" },
				//                            12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.InputMessage, "send message (end with +++)" },
				{ (int)LngKeys.SendMessage, "send message (y/n)" },
				{ (int)LngKeys.MessageSent, "message will be send." },
			};

			return lng;
		}
	}
}
