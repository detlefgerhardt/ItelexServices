using ItelexCommon;


namespace ItelexNewsServer.Languages
{
	public enum LanguageIds { de=1, en=2, none=0 };

	public enum LngKeys
	{
		Invalid = 0,

		Y,
		N,
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
		InvalidNewPin,
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
		EnterRedirectConfirmPin,
		RedirectActivated,
		RedirectAlreadyActive,
		RedirectNotConfirmed,

		SendRegistrationPinText,
		SendRedirectionPinText,
		SendChangedPinText,
		SendChangeNotificationText,

		CmdPrompt,
		InvalidCommand,
		CommandNotYetSupported,
		SubscribeChannel,
		UnsubscribeChannel,
		ChannelNotFound,
		PendingMsgsCleared,
		SendingTimeFromTo,
		PauseActive,
		PauseInactive,
		InvalidRedirectNumber,
		RedirectInactive,
		SendRedirectConfirmPin,
		InvalidTimezone,
		NoAutomaticDst,
		ActualTimezone,
		SettingNumber,
		SettingPending,
		SettingHours,
		SettingRedirect,
		SettingPaused,
		SettingTimezone,
		SettingMsgFormat,
		SettingMaxPendMsgs,
		ActualMsgFormat,
		MsgFormatStandard,
		MsgFormatShort,
		NoMatchingChannels,

		ConnectionTerminated,
		ShutDown,
		Aborted,
		InternalError,

		LocalChannels,
		NoLocalChannels,
		LocalChannelName,
		LocalChannelNo,
		IsChannelPublic,
		ChannelLanguage,
		InvalidLanguage,
		Pin,
		InvalidChannelPin,
		ChannelDoesNotFound,
		InvalidChannelNo,
		ChannelNameExists,
		ChannelCreatedAndSelected,
		NoChannelSelected,
		ChannelDataChanged,
		ChannelSelected,
		ChannelDeleteNotAllowed,
		DeleteChannelWithSubscriptions,
		ChannelDeleted,
		NoNumbersInChannel,
		ShowNumbersNotAllowed,
		ChannelHeader,
		NotALocalChannel,
		MsgTitle,
		SendToChannel,
		SendACopy,
		NoSubscribersWhenSending,
		InputMessage,
		SendMessage,
		MessageSent,
	}

	class LanguageDefinition
	{
		public const int DEFAULT_LANGUAGE = 1;

		//public static string GetText(LngKeys lngKey, LanguageIds lngId)
		//{
		//	return LanguageManager.Instance.GetText((int)lngKey, lngId);
		//}
		
		//public static string GetText(int lngKey, LanguageIds lngId)
		//{
		//	return LanguageManager.Instance.GetText(lngKey, (int)lngId);
		//}

		//public static string GetText(LngKeys lngKey, int lngId)
		//{
		//	return LanguageManager.Instance.GetText((int)lngKey, lngId);
		//}

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
