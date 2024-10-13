using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Command
{
	class CommandName
	{
		public string Name { get; set; }

		public string ShortName { get; set; }

		public CommandName(string name)
		{
			Name = name.Replace("_", "");

			int idx = name.IndexOf('_');
			if (idx > 0)
			{
				ShortName = name.Substring(0, idx);
			}
			else
			{
				ShortName = name;
			}
		}

		public bool CmdEquals(string cmdStr)
		{
			cmdStr = cmdStr.Trim();

			if (cmdStr.Length < ShortName.Length || cmdStr.Length > Name.Length)
			{
				return false;
			}

			return string.Compare(cmdStr, Name.Substring(0, cmdStr.Length), StringComparison.OrdinalIgnoreCase) == 0;
		}
	}
}
