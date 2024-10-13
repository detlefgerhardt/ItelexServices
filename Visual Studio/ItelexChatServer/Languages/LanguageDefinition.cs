using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Languages
{
	public enum LanguageIds { de=1, en=2, none=0 };

	public enum LngKeys
	{
		Invalid = 0,

		ShortYes = 1,
		ShortNo = 2,

		ServiceName = 10,
		//InputAnswerback = 11,
		//NoAnswerbackReceived = 12,
		//InputHint,
		//InputNumber,
		//NumberInUse,
		EnterShortName = 13,
		InputHelp = 14,
		ShortNameInUse = 15,
		HelpHint = 17,
		HelpText = 18,
		NewEntrant = 19,
		HasLeft = 20,
		HoldConnection = 21,
		On = 22,
		Off = 23,
		Subscribers = 24,
		None = 25,
		Help = 26,
		ShutDown = 27,
		HistRestart = 28,
		HistLogin = 29,
		HistLogoff = 30,
		CmdHelp = 31,
		CmdRunHelp = 32,
		CmdError = 33,

		NotificationStartMsg = 40,
		//NotificationAddDelShow = 41,
		NotificationEnterNumber = 42,
		NotificationEnterExtension = 43,
		//NotificationEnterPin = 44,
		NotificationOnLogin = 45,
		NotificationOnLogoff = 46,
		NotificationOnWriting = 47,
		NotificationInvalidNumber = 48,
		NotificationUnknownNumber = 49,
		NotificationNone = 50,
		//NotificationInvalidPin = 50,
		NotificationAlreadyActive = 51,
		NotificationNotActive = 52,
		NotificationNowActive = 53,
		//NotificationPresentPin = 54,
		NotificationDeleted = 55,
		NotificationText = 56,
		NotificationAddConfMsg = 57,
		NotificationDelConfMsg = 58,
		NotificationAbuseMsg = 59,
		NotificationHelp = 60,
		Notifications = 61,
		NotificationLogin = 62,
		NotificationLogoff = 63,
		NotificationWriting = 64,

		ConnectionTerminated = 65,
		InternalError = 66,
	}

	class LanguageDefinition
	{
		public const int DEFAULT_LANGUAGE = 1;

		public static string GetText(LngKeys lngKey, LanguageIds lngId)
		{
			return LanguageManager.Instance.GetText((int)lngKey, (int)lngId);
		}

		public static string GetText(int lngKey, LanguageIds lngId)
		{
			return LanguageManager.Instance.GetText(lngKey, (int)lngId);
		}

		public static string GetText(LngKeys lngKey, int lngId)
		{
			return LanguageManager.Instance.GetText((int)lngKey, lngId);
		}

		public static Language GetLanguageById(LanguageIds lngId)
		{
			return LanguageManager.Instance.GetLanguageById((int)lngId);
		}

		public static string[] GetYesNo(int lngId)
		{
			return new string[] { GetText(LngKeys.ShortYes, lngId), GetText(LngKeys.ShortNo, lngId) };
		}
	}
}
