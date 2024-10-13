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
	class GroupItem
	{
		[SqlId]
		public Int64 GroupId { get; set; }

		[SqlString(Length = 20)]
		public string Name { get; set; }

		[SqlString(Length = 4)]
		public string Pin { get; set; }

		[SqlInt]
		public int Owner { get; set; }

		//[SqlInt]
		//public int? CoOwner { get; set; }

		[SqlDateStr]
		public DateTime CreatedTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime LastChangedTimeUtc { get; set; }

		[SqlBool]
		public bool Active { get; set; }

		public GroupItem()
		{
			//CoOwner = null;
		}

		public override string ToString()
		{
			return $"{GroupId} {Name} {Owner}";
		}
	}
}
