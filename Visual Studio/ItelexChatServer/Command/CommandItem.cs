using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Command
{
	class CommandItem
	{
		public Commands Cmd { get; private set; }

		public CommandName[] CmdNames { get; private set; }

		public int ParamCnt { get; private set; }
		public int FreeParamCnt { get; private set; }

		public CommandItem(Commands cmd, string[] cmdStr, int paramCnt=0, int freeParamCnt=0)
		{
			Cmd = cmd;
			ParamCnt = paramCnt;
			FreeParamCnt = freeParamCnt;
			CmdNames = new CommandName[cmdStr.Length];
			for (int i=0; i<cmdStr.Length; i++)
			{
				CmdNames[i] = new CommandName(cmdStr[i]);
			}
		}

		public bool CmdEquals(string cmdStr)
		{
			if (string.IsNullOrWhiteSpace(cmdStr))
			{
				return false;
			}

			foreach (CommandName cmdName in CmdNames)
			{
				if (cmdName.CmdEquals(cmdStr))
				{
					return true;
				}
			}

			return false;
		}
	}
}
