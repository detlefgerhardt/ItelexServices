using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon
{
	public class Language
	{
		//public string Key { get; set; }
		public int Id { get; set; }

		public string Version { get; set; }

		public string ShortName { get; set; }

		public bool IsDefault { get; set; }

		public string DisplayName { get; set; }

		public Dictionary<int, string> Items { get; set; }

		public Language()
		{
			Items = new Dictionary<int, string>();
		}

		public Language(int id, string shortName, string displayName, bool isDefault)
		{
			Id = id;
			ShortName = shortName;
			DisplayName = displayName;
			IsDefault = isDefault;
			Items = new Dictionary<int, string>();
		}

		public static int StringToLngKey(string keyStr)
		{
			int lngKey;
			if (Enum.TryParse(keyStr, true, out lngKey))
			{
				return lngKey;
			}
			else
			{
				return 0;
			}
		}

		public override string ToString()
		{
			return $"{Id} {ShortName} {DisplayName} {Items?.Count}";
		}
	}
}
