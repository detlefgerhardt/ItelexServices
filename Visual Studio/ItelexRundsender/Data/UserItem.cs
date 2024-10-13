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

		[SqlDateStr]
		public DateTime RegisterTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime LastLoginTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? LastPinChangeTimeUtc { get; set; }

		[SqlBool]
		public bool Active { get; set; }

		[SqlBool]
		public bool Activated { get; set; }

		[SqlString(Length = 2)]
		public string Language { get; set; }

		public override string ToString()
		{
			return $"{UserId} {ItelexNumber}"; ;
		}
	}
}
