using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexRundsender.Data
{
	[Serializable]
	class GroupMemberItem
	{
		[SqlId]
		public Int64 MemberId { get; set; }

		[SqlInt]
		public Int64 GroupId { get; set; }

		[SqlInt]
		public int Number { get; set; }

		[SqlString(Length = 20)]
		public string Name { get; set; }

		[SqlDateStr]
		public DateTime AddedTimeUtc { get; set; }

		[SqlBool]
		public bool Active { get; set; }

		public override string ToString()
		{
			return $"{MemberId} {GroupId} {Number}";
		}
	}
}
