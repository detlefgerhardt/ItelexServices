using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telexsuche
{
	class ScatterTafelElement
	{
		public List<int> TextTafelAdressen { get; set;}

		public override string ToString()
		{
			return $"{string.Join(",", TextTafelAdressen)}";
		}
	}
}
