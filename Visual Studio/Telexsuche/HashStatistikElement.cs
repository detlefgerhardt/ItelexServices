using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telexsuche
{
	class HashStatistikElement
	{
		public string Wort { get; set; }

		public int Anzahl { get; set; }

		public override string ToString()
		{
			return $"{Wort} {Anzahl}";
		}
	}


	class HashStatEintragComparer : IComparer<HashStatistikElement>
	{
		public int Compare(HashStatistikElement x, HashStatistikElement y)
		{
			return y.Anzahl.CompareTo(x.Anzahl);
		}
	}


}
