using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatGptServer.Languages
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

				{ (int)LngKeys.ServiceName, "chatgpt-service (en)" },
				{ (int)LngKeys.EnterMultiLine, "multi-line input possible. end input with '+?'" },
				{ (int)LngKeys.QuestionToChatGpt, "question for chat-gpt" },
				{ (int)LngKeys.Aborted, "aborted" },
				{ (int)LngKeys.Disconnect, "the connection will be terminated." },
			};

			return lng;
		}
	}
}
