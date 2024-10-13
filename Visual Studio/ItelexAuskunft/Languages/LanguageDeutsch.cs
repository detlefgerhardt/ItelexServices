using ItelexCommon;
using System.Collections.Generic;

namespace ItelexAuskunft.Languages
{
	class LanguageDeutsch
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.de, "de", "Deutsch", true);
			lng.Items = new Dictionary<int, string>
			{
			};
			return lng;
		}
	}
}
