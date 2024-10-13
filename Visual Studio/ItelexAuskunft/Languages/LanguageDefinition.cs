using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexAuskunft.Languages
{
	public enum LanguageIds { de=1, en=2, none=0 };

	public enum LngKeys
	{
		Invalid = 0,
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

		//public static string[] GetYesNo(int lngId)
		//{
		//	return new string[] { GetText(LngKeys.ShortYes, lngId), GetText(LngKeys.ShortNo, lngId) };
		//}
	}
}
