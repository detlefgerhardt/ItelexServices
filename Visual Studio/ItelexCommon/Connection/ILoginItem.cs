using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Connection
{
	public interface ILoginItem
	{
		Int64 UserId { get; set; }

		int ItelexNumber { get; set; }

		string Pin { get; set; }

		bool Active { get; set; }
	}
}
