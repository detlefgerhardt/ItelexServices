using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telexsuche
{
	public class TextTafelElement
	{
		public string Kennung { get; set; }

		public string Nummer { get; set; }

		public string[] Zeilen { get; set;}

		public int Anzahl { get; set; }

		public override string ToString()
		{
			return $"[{Kennung}] {string.Join(",", Zeilen)} {Anzahl}";
		}
	}
}
