using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Data
{
	public enum ConfirmationTypes { NewPin = 1, Redirect = 2, Changed = 3 }

	[Serializable]
	class ConfirmationItem
	{
		[SqlId]
		public Int64 ConfId { get; set; }

		[SqlInt]
		public Int64 UserId { get; set; }

		[SqlTinyInt]
		public int Type { get; set; }

		[SqlInt]
		public int Number { get; set; }

		[SqlString(Length = 4)]
		public string Pin { get; set; }

		[SqlString(Length = 2)]
		public string Language { get; set; }

		[SqlTinyInt]
		public int SendRetries { get; set; }

		[SqlBool]
		public bool Sent { get; set; }

		[SqlBool]
		public bool Finished { get; set; }

		[SqlDateStr]
		public DateTime? CreateTimeUtc { get; set; }

		[SqlDateStr]
		public DateTime? SentTimeUtc { get; set; }

		[SqlString(Length = 30)]
		public string AnswerBack { get; set; }

		public ConfirmationItem()
		{
			Sent = false;
			Finished = false;
		}

		public override string ToString()
		{
			return $"{ConfId} {UserId} {Type} {Number} {Pin} {Sent} {CreateTimeUtc} {SentTimeUtc}";
		}
	}
}
