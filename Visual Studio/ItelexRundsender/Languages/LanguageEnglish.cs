using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexRundsender.Languages
{
	class LanguageEnglish
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.en, "en", "English", false);
			lng.Items = new Dictionary<int, string>
			{
				{ (int)LngKeys.DateTimeFormat, "yy-MM-dd  HH:mm" },

				{ (int)LngKeys.ServiceName, "detlef's broadcast message service" },
				{ (int)LngKeys.NoValidAnswerback, "no answerback with valid number found." },
				{ (int)LngKeys.EnterValidNumber, "enter valid number:" },
				{ (int)LngKeys.ConfirmOwnNumber,
					"confirm own number with 'nl' (new line) or enter correct\r\nnumber" },
				{ (int)LngKeys.ChooseSendMode, "direct or time-delayed (d/t/g=group info/?=help) ?" },
				{ (int)LngKeys.GroupInfo, "group info" },
				{ (int)LngKeys.GroupName, "group name" },
				{ (int)LngKeys.EnterDestNumbers, "destination numbers (finish with +)" },
				{ (int)LngKeys.NoDestNumbers, "no destination numbers." },
				{ (int)LngKeys.EstablishConnections, "establishing connections, mom..." },
				{ (int)LngKeys.DestNumbers, "destination numbers:" },
				{ (int)LngKeys.IncludeReceiverList, "include receiver-list" },
				{ (int)LngKeys.IsOk, "ok (y/n) ?" },
				{ (int)LngKeys.CreateTransmissionReport, "creating transmission report, mom..." },
				{ (int)LngKeys.ReportOk, "ok" },
				{ (int)LngKeys.ReportError, "error" },
				{ (int)LngKeys.NoHelpAvailable, "no help available." },
				{ (int)LngKeys.SomeDestNumbersNotAvailable, "some destination numbers not available." },
				{ (int)LngKeys.SendThemDeferred, "send them time-delayed (y/n) ?" },
				{ (int)LngKeys.ReceiverPrefix, "recv:" },
				{ (int)LngKeys.PleaseSendMessage, "please send message (finish with +++):" },
				{ (int)LngKeys.TerminateConnectionAndTransmit, "terminating connection and transmitting message." },
				{ (int)LngKeys.PleaseSendNumbersAndMessage, "destination numbers (finish with +) and message (finish with +++):" },
				{ (int)LngKeys.WaitMoment, "mom..." },
				{ (int)LngKeys.TransmissionReportIntermediate, "intermediate transmission report:" },
				{ (int)LngKeys.TransmissionReportIntermediateHint,
					"busy lines will continue to be called.\r\nfinal transmission report in max. 60 min." },
				{ (int)LngKeys.TransmissionReportFinal, "final transmission report:" },

				{ (int)LngKeys.MsgTooLong, "--- message too long ---" },

				// group administration

				{ (int)LngKeys.EnterLoginPin, "enter your pin" },
				{ (int)LngKeys.WrongPin, "wrong pin." },
				{ (int)LngKeys.EnterOldPin, "old pin" },
				{ (int)LngKeys.EnterNewPin, "new pin" },
				{ (int)LngKeys.EnterNewPinAgain, "repeat new pin" },
				{ (int)LngKeys.InvalidPin, "invalid new pin." },
				{ (int)LngKeys.PinsNotEqual, "pins not identical." },
				{ (int)LngKeys.PinChanged, "pin changed." },
				{ (int)LngKeys.PinNotChanged, "pin not changed." },
				//                                  12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.SendChangedPinText, "your pin for the broadcast service administration for your number\r\n@1 was changed. new pin: @2\r\n" },

				{ (int)LngKeys.NotRegistered, "number @1 is not registered.\r\ncreate new account (y/n)" },
				//                           12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.Deactivated, "the account is deactivated. please contact the maintainer of this\r\nservice." },
				{ (int)LngKeys.SendNewLoginPin, "send new pin (y/n)" },
				{ (int)LngKeys.NewAccountCreated, "a new account was created." },
				{ (int)LngKeys.SendNewPinMsg, "a new pin will now be send to the number @1.\r\n" +
					"please call again and login with the new pin to activate the\r\n" +
					"account." },
				//                                       12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.SendRegistrationPinText, "new pin for broadcast service admininstration for number\r\n@1 is: @2" },
				{ (int)LngKeys.ConnectionTerminated, "the connection will be terminated." },
				{ (int)LngKeys.InternalError, "internal error." },
				{ (int)LngKeys.NewAccountActivated, "your account is now activated." },
				{ (int)LngKeys.CmdPrompt, "cmd" },
				{ (int)LngKeys.InvalidCommand, "invalid command ('help')." },

				{ (int)LngKeys.NoGroupsFound, "no groups found." },
				{ (int)LngKeys.GroupNotFound, "group '@1' not found." },
				//                                       12345678901234567890123456789012345678901234567890123456789012345678
				{ (int)LngKeys.InvalidCharsInGroupName, "The group name can only contain letters, numbers and '-'." },
				{ (int)LngKeys.GroupSelected, "group '@1' selected." },
				{ (int)LngKeys.GroupExists, "group '@1' already exists." },
				{ (int)LngKeys.GroupCreatedAndSelected, "group '@1' created and selected." },
				{ (int)LngKeys.NoGroupSelected, "no group selected." },
				{ (int)LngKeys.GroupDataChanged, "data changed." },
				{ (int)LngKeys.GroupDeleteNotAllowed, "you are not allowed to delete this group." },
				{ (int)LngKeys.DeleteGroupWithMembers, "delete group '@1' with @2 numbers" },
				{ (int)LngKeys.GroupDeleted, "group '@1' deleted." },
				{ (int)LngKeys.NoNumbersInGroup, "no numbers in group '@1'." },
				{ (int)LngKeys.GroupHeader, "group '@1'" },
				{ (int)LngKeys.InvalidNumber, "number @1 is invalid." },
				{ (int)LngKeys.NumberAdded, "number @1 added." },
				{ (int)LngKeys.DeleteNumberFromGroup, "delete number @1 from group '@2'" },
				{ (int)LngKeys.NumberNotInGroup, "number @1 not in selected group." },
				{ (int)LngKeys.NumberDeleted, "number @1 deleted." },
				//{ (int)LngKeys.GroupName, "name" },
				{ (int)LngKeys.Pin, "pin" },
			};

			return lng;
		}
	}
}
