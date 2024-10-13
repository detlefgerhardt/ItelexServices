using ItelexCommon;
using ItelexNewsServer.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ItelexNewsServer.Data
{
	public enum ChannelRights
	{
		WriteAll = 0,
		WriteOwner = 1,
		WriteMods = 2,
		WriteAdmin = 3
	}

	public enum ChannelCategories
	{
		News,
		Science,
		Sport,
		Local,
		Other,
	}

	public enum ChannelLanguages
	{
		De,
		En,
		It,
	}

	public enum ChannelTypes
	{
		Gateway,
		Itelex,
		Rss,
		Local,
		//Twitter
	}

	[Serializable]
	class ChannelItem
	{
		public static readonly List<ChannelCategoryItem> Categories = new List<ChannelCategoryItem>()
		{
			new ChannelCategoryItem(ChannelCategories.News, "news", "ne"),
			new ChannelCategoryItem(ChannelCategories.Science, "science", "sc"),
			new ChannelCategoryItem(ChannelCategories.Sport, "sport", "sp"),
			new ChannelCategoryItem(ChannelCategories.Local, "local", "lo"),
			new ChannelCategoryItem(ChannelCategories.Other, "other", "ot"),
		};

		public static readonly List<ChannelLanguageItem> Languages = new List<ChannelLanguageItem>()
		{
			new ChannelLanguageItem(ChannelLanguages.De, "de"),
			new ChannelLanguageItem(ChannelLanguages.En, "en"),
			new ChannelLanguageItem(ChannelLanguages.It, "it"),
		};

		/*
		public static readonly List<ChannelContentItem> Contents = new List<ChannelContentItem>()
		{
			new ChannelContentItem("t"),
			new ChannelContentItem("d"),
			new ChannelContentItem("td"),
		};
		*/

		public static readonly List<ChannelTypeItem> Types = new List<ChannelTypeItem>()
		{
			new ChannelTypeItem(ChannelTypes.Gateway, "gw"),
			new ChannelTypeItem(ChannelTypes.Itelex, "it"),
			new ChannelTypeItem(ChannelTypes.Rss, "rss"),
			new ChannelTypeItem(ChannelTypes.Local, "lo"),
		};

		[SqlId]
		public Int64 ChannelId { get; set; }

		[SqlString(Length = 40)]
		public string Name { get; set; }

		/// <summary>
		/// Channel type as string for database
		/// </summary>
		[SqlString(Length = 4)]
		public string Type { get; set; }

		/// <summary>
		/// Channel type as enum
		/// </summary>
		public ChannelTypes ChannelType => GetChannelTypeByName(Type).Type;

		/// <summary>
		/// Category (news, science, ...)
		/// </summary>
		[SqlString(Length = 10)]
		public string Category { get; set; }

		/// <summary>
		/// Language (de, en)
		/// </summary>
		[SqlString(Length = 2)]
		public string Language { get; set; }

		[SqlString(Length = 255)]
		public string Url { get; set; }

		/// <summary>
		/// i-Telex number of the the user who owns this private channel (has write access)
		/// </summary>
		[SqlInt]
		public int? LocalOwner { get; set; } = null;

		public string IdAndName => $"{ChannelId} '{Name}'";

		[SqlBool]
		public bool? LocalPublic { get; set; } = null;

		public bool IsPublic => LocalPublic.HasValue && LocalPublic.Value == true;

		/// <summary>
		/// Pin for writing to this private channel
		/// </summary>
		[SqlString(Length = 4)]
		public string LocalPin { get; set; }

		[SqlDateStr]
		public DateTime? CreateTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? LastChangedTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? LastPollTimeUtc { get; set; }

		public DateTime? NextPollTimeUtc { get; set; }

		[SqlSmallInt]
		public int PollIntervallMin { get; set; }

		[SqlDateStr]
		public DateTime? LastMsgTimeUtc { get; set; }

		[SqlInt]
		public int MsgCount { get; set; } = 0;

		[SqlInt]
		public int ErrorCount { get; set; }

		[SqlBool]
		public bool Hidden { get; set; }

		[SqlBool]
		public bool Active { get; set; }

		public bool PollTimeElapsed
		{
			get
			{
				if (!Active || PollIntervallMin == 0) return false;

				DateTime now = DateTime.UtcNow;
				if (NextPollTimeUtc == null)
				{
					NextPollTimeUtc = now.AddMinutes(PollIntervallMin);
					return false;
				}
				if ((DateTime.UtcNow - NextPollTimeUtc.Value).TotalMinutes < 0)
				{
					NextPollTimeUtc = now.AddMinutes(PollIntervallMin);
					return true;
				}
				return false;
			}
		}

		public string GetConvName()
		{
			return ConvChannelName(Name);
		}

		public static string ConvChannelName(string name)
		{
			if (string.IsNullOrEmpty(name)) return "???";
			name = name.ToLower().Replace("_", "-");
			return CodeManager.AsciiStringReplacements(name, CodeSets.ITA2, false, false);
		}

		public static ChannelTypeItem GetChannelType(ChannelTypes type)
		{
			return Types.Where(c => c.Type == type).FirstOrDefault();
		}

		public static ChannelTypeItem GetChannelTypeByName(string typeName)
		{
			return (from t in Types where t.Kennung == typeName select t).FirstOrDefault();
		}

		public static string GetChannelTypeName(ChannelTypes type)
		{
			return (from t in Types where t.Type==type select t.Kennung).FirstOrDefault();
		}

		public static ChannelCategoryItem GetCategory(string catStr)
		{
			if (catStr == null || catStr.Length < 2) return null;

			foreach (ChannelCategoryItem catItem in Categories)
			{
				if (catItem.Name.Length < catStr.Length) continue;
				if (catStr == catItem.Name.Substring(0, catStr.Length)) return catItem;
			}
			return null;
		}

		public static ChannelCategoryItem GetCategory(ChannelCategories cat)
		{
			return Categories.Where(c => c.Category == cat).FirstOrDefault();
		}

		public static string GetCategoryName(ChannelCategories cat)
		{
			return (from c in Categories where c.Category == cat select c.Name).FirstOrDefault();
		}

		public static ChannelLanguageItem GetLanguage(string lngStr)
		{
			if (lngStr == null || lngStr.Length < 2) return null;

			foreach (ChannelLanguageItem catItem in Languages)
			{
				if (catItem.Name.Length < lngStr.Length) continue;
				if (lngStr == catItem.Name.Substring(0, lngStr.Length)) return catItem;
			}
			return null;
		}

		public static ChannelLanguageItem GetLanguage(ChannelLanguages lng)
		{
			return Languages.Where(c => c.Language == lng).FirstOrDefault();
		}

		public override string ToString()
		{
			return $"{ChannelId} {Type} {Name} {Url}";
		}
	}

	class UserChannel : ChannelItem
	{
		public bool Subscribed { get; set; }

		public DateTime? SubscribeTimeUtc { get; set; }

		public int SubscribeCount { get; set; }

		public int NewsCount { get; set; }

		public UserChannel(ChannelItem chItem)
		{
			ChannelId = chItem.ChannelId;
			Name = chItem.Name;
			Type = chItem.Type;
			Category = chItem.Category;
			Language = chItem.Language;
			CreateTimeUtc = chItem.CreateTimeUtc;
			LastChangedTimeUtc = chItem.LastChangedTimeUtc;
			LastPollTimeUtc = chItem.LastPollTimeUtc;
			NextPollTimeUtc = chItem.NextPollTimeUtc;
			MsgCount = chItem.MsgCount;
			ErrorCount = chItem.ErrorCount;
			Active = chItem.Active;
			Hidden = chItem.Hidden;
			LocalOwner = chItem.LocalOwner;
			LocalPublic = chItem.LocalPublic;
		}

		public int CompareTo(UserChannel uc)
		{
			return Name.CompareTo(uc.Name);
		}

		public override string ToString()
		{
			string b = base.ToString();
			return $"{b} {Subscribed} {SubscribeTimeUtc:dd.MM.yy}";
		}
	}

	class UserChannelComparer : Comparer<UserChannel>
	{
		public override int Compare(UserChannel item1, UserChannel item2)
		{
			return item1.CompareTo(item2);
		}
	}

	class ChannelIdComparer: Comparer<UserChannel>
	{
		public override int Compare(UserChannel item1, UserChannel item2)
		{
			return item1.ChannelId.CompareTo(item2.ChannelId);
		}
	}


	class ChannelCategoryItem
	{
		public ChannelCategories Category { get; set; }

		public string Name { get; set; }

		public string ShortName { get; set; }

		public ChannelCategoryItem(ChannelCategories cat, string name, string shortName)
		{
			Category = cat;
			Name = name;
			ShortName = shortName;
		}

		public override string ToString()
		{
			return $"{Category} {Name} {ShortName}";
		}
	}

	class ChannelLanguageItem
	{
		public ChannelLanguages Language { get; set; }

		public string Name { get; set; }

		public ChannelLanguageItem(ChannelLanguages lng, string name)
		{
			Language = lng;
			Name = name;
		}

		public override string ToString()
		{
			return $"{Name}";
		}
	}

	/*
	class ChannelContentItem
	{
		public string Content { get; set; }

		public ChannelContentItem(string content)
		{
			Content = content;
		}

		public override string ToString()
		{
			return $"{Content}";
		}
	}
	*/

	class ChannelTypeItem
	{
		public ChannelTypes Type { get; set; }

		public string Kennung { get; set; }

		public ChannelTypeItem(ChannelTypes type, string kennung)
		{
			Type = type;
			Kennung = kennung;
		}

		public override string ToString()
		{
			return $"{Type} {Kennung}";
		}
	}
}
