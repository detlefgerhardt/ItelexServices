using ItelexCommon;
using ItelexMsgServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Data
{
	[Serializable]
	class UidItem
	{
		[SqlString(Length = 50)]
		public string Uid { get; set; }

		[SqlString(Length = 50)]
		public string Sender { get; set; }

		[SqlDateStr]
		public DateTime CreateTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? MailTimeUtc { get; set; }


		public override string ToString()
		{
			return $"{Uid} {Sender} {CreateTimeUtc} {MailTimeUtc}";
		}

	}
}
