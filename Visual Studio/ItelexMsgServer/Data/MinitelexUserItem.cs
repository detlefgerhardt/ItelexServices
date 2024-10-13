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
	public class MinitelexUserItem
	{
		[SqlId]
		public Int64 Id { get; set; }

		[SqlString(Length = 50)]
		public string Name { get; set; }

		[SqlInt]
		public int ItelexNumber { get; set; }

		[SqlString(Length = 20)]
		public string Faxnummer { get; set; }

		[SqlInt]
		public int PortIndex { get; set; }

		[SqlString(Length = 20)]
		public string Kennung { get; set; }

		[SqlBool]
		public bool EdgePrint { get; set; }

		[SqlBool]
		public bool Endless { get; set; }

		[SqlBool]
		public bool Deranged { get; set; }

		[SqlBool]
		public bool Active { get; set; }



		public override string ToString()
		{
			return $"{Id} {PortIndex} {Name} {ItelexNumber} {Faxnummer} {Kennung} {Active}";
		}

	}
}
