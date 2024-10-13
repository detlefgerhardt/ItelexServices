using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatGptServer.Languages
{
	public enum LanguageIds { de=1, en=2, none=0 };

	public enum LngKeys
	{
		Invalid = 0,

		Yes,
		No,
		Ok,

		ServiceName,
		EnterMultiLine,
		QuestionToChatGpt,
		Aborted,
		Disconnect
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
