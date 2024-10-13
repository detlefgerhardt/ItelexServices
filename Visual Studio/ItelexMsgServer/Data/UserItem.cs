using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Data
{
	[Serializable]
	class UserItem: ILoginItem
	{
		[SqlId]
		public Int64 UserId { get; set; }

		[SqlInt]
		public int ItelexNumber { get; set; }

		[SqlString(Length = 30)]
		public string Kennung { get; set; }

		[SqlString(Length = 4)]
		public string Pin { get; set; }

		[SqlSmallInt]
		public int Timezone { get; set; }

		[SqlDateStr]
		public DateTime RegisterTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime LastLoginTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? LastPinChangeTimeUtc { get; set; }

		[SqlBool]
		public bool? AllowRecvMails { get; set; }

		[SqlBool]
		public bool? AllowRecvTelegram { get; set; }

		[SqlBool]
		public bool Active { get; set; }

		[SqlBool]
		public bool Activated { get; set; }

		[SqlString(Length = 20)]
		public string TelegramChatId { get; set; }

		[SqlString(Length = 50)]
		public string MailAddr { get; set; }

		[SqlString(Length = 50)]
		public string AllowedSender { get; set; }

		[SqlInt]
		public int MaxMailsPerDay { get; set; }

		[SqlInt]
		public int MaxLinesPerDay { get; set; }

		[SqlInt]
		public int MaxPendMails { get; set; }

		[SqlInt]
		public int MailsPerDay { get; set; }

		[SqlInt]
		public int LinesPerDay { get; set; }

		[SqlDateStr]
		public DateTime? LastReceiveTimeUtc { get; set; }

		[SqlBool]
		public bool Paused { get; set; }

		[SqlTinyInt]
		public int? SendFromHour { get; set; }

		[SqlTinyInt]
		public int? SendToHour { get; set; }

		[SqlString(Length = 2)]
		public string Language { get; set; }

		/// <summary>
		/// Explicit mail adress to autmatically identify mails to specific to this account (for museums and presentations)
		/// </summary>
		[SqlString(Length = 50)]
		public string Receiver { get; set; }

		/// <summary>
		/// Set to false for museums or public presentations
		/// </summary>
		[SqlBool]
		public bool ShowSender { get; set; }

		/// <summary>
		/// Set to true if send per email is public
		/// </summary>
		[SqlBool]
		public bool Public { get; set; }

		/// <summary>
		/// This is a pin-number for explicit mail addresses that is given in the subject of the incoming mails and must match the pin-number in the user settings.
		/// The ideal is to change the pin-number from day to day, so that only local visitors can use the mail address.
		/// </summary>
		[SqlString(Length = 10)]
		public string EventPin { get; set; }

		public bool IsHourActive()
		{
			if (SendFromHour == null || SendToHour == null) return true;

			DateTime now =  DateTime.UtcNow.AddHours(Timezone);
			return now.Hour >= SendFromHour && now.Hour < SendToHour;
		}

		[SqlInt]
		public int TotalMailsReceived { get; set; }

		[SqlInt]
		public int TotalMailsSent { get; set; }


		public override string ToString()
		{
			return $"{UserId} {ItelexNumber}"; ;
		}
	}
}
