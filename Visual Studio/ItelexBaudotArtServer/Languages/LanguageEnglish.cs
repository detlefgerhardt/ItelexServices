using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexBaudotArtServer.Languages
{
	class LanguageEnglish
	{
		public static Language GetLng()
		{
			Language lng = new Language((int)LanguageIds.en, "en", "English", false);
			lng.Items = new Dictionary<int, string>
			{
			};
			return lng;
		}
	}
}
