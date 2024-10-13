using ItelexCommon;
using System;
using System.Collections.Generic;

namespace ItelexChatGptServer.Languages
{
	class LanguageDeutsch
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.de, "de", "Deutsch", true);
			lng.Items = new Dictionary<int, string>
			{
				{ (int)LngKeys.Yes, "ja" },
				{ (int)LngKeys.No, "nein" },
				{ (int)LngKeys.Ok, "ok" },

				{ (int)LngKeys.ServiceName, "chatgpt-service (de)" },
				{ (int)LngKeys.EnterMultiLine, "mehrzeilige eingabe moeglich. abschluss mit +?" },
				{ (int)LngKeys.QuestionToChatGpt, "frage an chat gpt" },
				{ (int)LngKeys.Aborted, "abbruch" },
				{ (int)LngKeys.Disconnect, "die verbindung wird getrennt." },
			};
			return lng;
		}
	}
}
