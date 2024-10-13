using ItelexChatServer.Languages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer
{
	class ActivityHistoryItem
	{
		public ActivityHistoryManager.Activities Activity { get; set; }

		public DateTime Timestamp { get; set; }

		public string Shortname { get; set; }

		public string Message { get; set; }

		public string GetString(bool withMessage, int lngId)
		{
			string timeStr = $"{Timestamp:HH:mm}";
			string result = "";
			switch (Activity)
			{
				case ActivityHistoryManager.Activities.Restart:
					result = $"{timeStr} {LanguageDefinition.GetText(LngKeys.HistRestart, lngId)}";
					break;
				case ActivityHistoryManager.Activities.Login:
					result = $"{timeStr} {LanguageDefinition.GetText(LngKeys.HistLogin, lngId)} {Shortname}";
					break;
				case ActivityHistoryManager.Activities.Logoff:
					result = $"{timeStr} {LanguageDefinition.GetText(LngKeys.HistLogoff, lngId)} {Shortname}";
					break;
				case ActivityHistoryManager.Activities.Message:
					if (withMessage)
					{
						result = $"{timeStr} {Shortname}:{Message}";
					}
					else
					{
						result = $"{timeStr} {Shortname}:";
					}
					break;
				default:
					result = "";
					break;
			}
			if (result.Length>68)
			{
				result = result.Substring(0, 68);
			}
			return result;
		}
	}
}
