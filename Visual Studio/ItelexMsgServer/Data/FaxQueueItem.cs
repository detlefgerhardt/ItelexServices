using ItelexCommon;
using ItelexMsgServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Data
{
	public enum FaxStatis
	{
		Pending = 0,
		Ok = 1,
		Failed = 2,
		Cleared = 3,
		Outdated = 4,
	}

	[Serializable]
	public class FaxQueueItem
	{
		/// <summary>
		/// Table ID
		/// </summary>
		[SqlId]
		public Int64 Id { get; set; }

		/// <summary>
		/// Randon unique fax ID
		/// </summary>
		[SqlInt]
		public int FaxId { get; set; }

		[SqlBool]
		public bool IsMiniTelex { get; set; }

		[SqlInt]
		public Int64 UserId { get; set; }

		[SqlString(Length = 30)]
		public string Sender { get; set; }

		[SqlString(Length = 30)]
		public string Receiver { get; set; }

		[SqlString]
		public string Message { get; set; }

		[SqlTinyInt]
		public int FaxFormat { get; set; }

		[SqlBool]
		public bool EdgePrint { get; set; }

		[SqlString(Length = 2)]
		public string Language { get; set; }

		[SqlTinyInt]
		public int SendRetries { get; set; }

		[SqlTinyInt]
		public int Status { get; set; }

		[SqlTinyInt]
		public int? Response { get; set; }

		[SqlDateStr]
		public DateTime CreateTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? LastRetryTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? SendTimeUtc { get; set; }

		[SqlInt]
		public int LineCount { get; set; }

		public override string ToString()
		{
			return $"{Id} {FaxId} {UserId} {Sender} {Receiver} {SendRetries} {Status} {CreateTimeUtc} {SendTimeUtc} {LineCount}";
		}

	}
}
