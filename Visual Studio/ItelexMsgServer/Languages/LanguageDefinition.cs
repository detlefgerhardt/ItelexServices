using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Languages
{
	public enum LanguageIds { de=1, en=2, none=0 };

	public enum LngKeys
	{
		Invalid = 0,

		Yes,
		No,
		Ok,

		ServiceName,
		ConfirmOwnNumber,
		EnterValidNumber,

		EnterLoginPin,
		EnterOldPin,
		EnterNewPin,
		EnterNewPinAgain,
		WrongPin,
		InvalidPin,
		PinsNotEqual,
		PinChanged,
		PinNotChanged,

		SendNewLoginPin,
		NotRegistered,
		Deactivated,
		NewAccountCreated,
		NewAccountActivated,
		NewAccountTimezoneInfo,
		NewAccountEnterTimezone,
		SendNewPinMsg,

		SendRegistrationPinText,
		SendRedirectionPinText,
		SendChangedPinText,
		//SendChangeNotificationText,

		CmdPrompt,
		InvalidCommand,
		CommandNotYetSupported,
		PendingMailsCleared,
		SendingTimeFromTo,
		PauseActive,
		PauseInactive,
		ShowSenderActive,
		ShowSenderInactive,
		AllowedSender,
		AllowedSenderOff,
		EventCode,
		EventCodeOff,
		InvalidTimezone,
		NoAutomaticDst,
		ActualTimezone,
		MailOrFaxReceiver,
		MailSubject,
		MailTime,
		MailFrom,
		MailTo,
		MailHeader,
		InputMessage,
		MailSendSuccessfully,
		MailSendError,
		InvalidMailAdress,
		InvalidEventCode,
		InvalidFaxNumber,
		FaxWillBeSend,
		ForbiddenFaxNumber,

		PunchTapeFilename,
		PunchTapeSend,
		PunchTapeMailHeader,
		Filename,
		InvalidFilename,
		InvalidData,

		SettingNumber,
		SettingPending,
		SettingHours,
		SettingPaused,
		SettingTimezone,
		SettingAllowedSender,
		SettingAllowRecvMails,
		SettingAllowRecvTelegram,
		SettingMaxMailsPerDay,
		SettingMaxLinesPerDay,
		SettingMaxPendMails,
		SettingTelegramChatId,
		SettingTelegramChatIdLinked,
		TelegramChatIdInvalid,
		TelegramChatIdCanNotBeLinked,

		PruefTextNotFound,

		SettingAssociatedMailAddr,
		SettingShowSenderAddr,
		SettingEventPin,

		ConnectionTerminated,
		ShutDown,
		Aborted,
		InternalError
	}

	class LanguageDefinition
	{
		public const int DEFAULT_LANGUAGE = 1;

		public static Language GetLanguageById(LanguageIds lngId)
		{
			return LanguageManager.Instance.GetLanguageById((int)lngId);
		}

		public static string GetLanguageNameById(LanguageIds lngId)
		{
			return GetLanguageById(lngId).ShortName;
		}
	}
}
