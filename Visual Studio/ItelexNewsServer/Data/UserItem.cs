using ItelexCommon;
using ItelexCommon.Connection;
using ItelexNewsServer.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexNewsServer.Data
{
	public enum MsgFormats
	{
		Standard = 0,
		Short = 1
	}

	[Serializable]
	class UserItem: ILoginItem
	{
		public enum SendPinReasons
		{
			None = 0,
			Registration = 1,
			Redirect = 2
		};

		private static readonly MsgFormatItem[] _msgFormatList =
		{
			new MsgFormatItem(MsgFormats.Standard, "standard"),
			new MsgFormatItem(MsgFormats.Short, "short"),
			new MsgFormatItem(MsgFormats.Short, "kurz"),
		};

		[SqlId]
		public Int64 UserId { get; set; }

		[SqlInt]
		public int ItelexNumber { get; set; }

		[SqlString(Length = 4)]
		public string Pin { get; set; }

		[SqlString(Length = 30)]
		public string Kennung { get; set; }

		[SqlSmallInt]
		public int Timezone { get; set; }

		//[SqlString(Length = 2)]
		//public string Language { get; set; }

		[SqlDateStr]
		public DateTime? RegisterTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? LastLoginTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? LastPinChangeTimeUtc { get; set; }

		[SqlBool]
		public bool Active { get; set; }

		[SqlInt]
		public int? RedirectNumber { get; set; }

		//[SqlString(Length = 4)]
		//public string RedirectPin { get; set; }

		[SqlBool]
		public bool Paused { get; set; }

		[SqlDateStr]
		public DateTime? PauseUntilTimeUtc { get; set; }

		[SqlSmallInt]
		public int? SendFromHour { get; set; }

		[SqlSmallInt]
		public int? SendToHour { get; set; }

		[SqlTinyInt]
		public int MsgFormat { get; set; }

		[SqlSmallInt]
		public int MaxPendingNews { get; set; }


		//[SqlInt]
		//public int VerifyCode { get; set; }


		public UserItem() { }

		public UserItem(int number)
		{
			ItelexNumber = number;
			MaxPendingNews = Constants.MAX_PENDING_NEWS;
		}

		public bool IsPaused => Paused || PauseUntilTimeUtc != null && DateTime.UtcNow < PauseUntilTimeUtc.Value;

		public static MsgFormatItem GetMsgFormat(string formatStr)
		{
			if (formatStr == null || formatStr.Length < 2) return null;

			foreach (MsgFormatItem item in _msgFormatList)
			{
				if (item.Name.Length < formatStr.Length) continue;
				if (formatStr == item.Name.Substring(0, formatStr.Length)) return item;
			}
			return null;
		}

		public static MsgFormatItem GetMsgFormat(MsgFormats msgFormat)
		{
			return _msgFormatList.Where(m => m.MsgFormat == msgFormat).FirstOrDefault();
		}

		public bool IsHourActive()
		{
			if (SendFromHour == null || SendToHour == null) return true;

			DateTime now = Helper.ToLocalTime(DateTime.UtcNow, Timezone);
			return now.Hour >= SendFromHour && now.Hour < SendToHour;
		}

		public override string ToString()
		{
			return $"{UserId} {ItelexNumber} {Timezone}";
		}
	}

	/*
	class UserLanguageItem
	{
		public string Name { get; set; }

		public string ShortName { get; set; }

		public string DateTimeFormat { get; set; }

		public UserLanguageItem(string name, string shortName, string dateTimeFormat)
		{
			Name = name;
			ShortName = shortName;
			DateTimeFormat = dateTimeFormat;
		}

		public string FormatDateTime(DateTime dt)
		{
			return dt.ToString(DateTimeFormat);
		}

		public override string ToString()
		{
			return $"{Name} {ShortName} {DateTimeFormat}";
		}
	}
	*/

	class MsgFormatItem
	{
		public MsgFormats MsgFormat { get; set; }

		public string Name { get; set; }

		public MsgFormatItem(MsgFormats msgFormat, string name)
		{
			MsgFormat = msgFormat;
			Name = name;
		}
	}
}
