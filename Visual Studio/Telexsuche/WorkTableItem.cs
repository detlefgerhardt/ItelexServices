using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telexsuche
{
	class WorkTableItem
	{
		public int Index { get; set; }

		public int Count { get; set; }

		public WorkTableItem(int idx)
		{
			Index = idx;
			Count = 1;
		}

		public override string ToString()
		{
			return $"{Index} {Count}";
		}
	}
}
