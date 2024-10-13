using ItelexCommon;
using ItelexMsgServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Data
{
	public enum MsgTypes
	{
		Email = 1,
		Fax = 2,
		Telegram = 3,
	}

	public enum MsgStatis
	{
		Pending = 0,
		Ok = 1,
		Cleared = 2,
		Outdated = 3,
		NotSent = 4,
	}

	[Serializable]
	class MsgItem
	{
		[SqlId]
		public Int64 MsgId { get; set; }

		[SqlTinyInt]
		public int MsgType { get; set; }

		[SqlString]
		public string Uid { get; set; }

		[SqlInt]
		public Int64 UserId { get; set; }

		[SqlString(Length = 30)]
		public string LastResult { get; set; }

		[SqlTinyInt]
		public int SendRetries { get; set; }

		[SqlTinyInt]
		public int SendStatus { get; set; }

		[SqlDateStr]
		public DateTime CreateTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? LastTryTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? SendTimeUtc { get; set; }

		[SqlString(Length = 100)]
		public string Sender { get; set; }

		[SqlString(Length = 255)]
		public string Subject { get; set; }

		[SqlString]
		public string Message { get; set; }

		[SqlInt]
		public int LineCount { get; set; }

		[SqlDateStr]
		public DateTime? MailTimeUtc { get; set; }


		public override string ToString()
		{
			return $"{MsgId} {MsgType} {UserId} {SendStatus} {Sender} {CreateTimeUtc}";
		}

	}
}
