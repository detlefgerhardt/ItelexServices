using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexNewsServer.Data
{
	public enum MsgStatis
	{
		Pending = 0,
		Ok = 1,
		Cleared = 2,
		Outdated = 3,
	}

	[Serializable]
	class MsgStatusItem
	{
		[SqlInt]
		public Int64 NewsId { get; set; }

		[SqlInt]
		public Int64 UserId { get; set; }

		[SqlString(Length = 10)]
		public string LastResult { get; set; }

		[SqlTinyInt]
		public int SendRetries { get; set; }

		[SqlTinyInt]
		public int SendStatus { get; set; }

		public MsgStatis SendStatusEnum
		{
			get
			{
				return (MsgStatis)SendStatus;
			}
			set
			{
				SendStatus = (int)value;
			}
		}

		[SqlDateStr]
		public DateTime DistribTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? SendTimeUtc { get; set; }

		public override string ToString()
		{
			return $"{NewsId} {UserId} {DistribTimeUtc} {SendRetries} {SendStatus} {SendTimeUtc} {LastResult}";
		}
	}


}
