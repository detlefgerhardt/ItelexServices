using ItelexCommon;
using System.Collections.Generic;

namespace ItelexBaudotArtServer.Languages
{
	class LanguageDeutsch
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>")]
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
