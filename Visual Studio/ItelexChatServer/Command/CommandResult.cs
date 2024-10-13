using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Command
{
	class CommandResult
	{
		public Commands Cmd { get; set; }

		public Commands? Param { get; set; }

		public string FreeParam { get; set; }
	}
}
