using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Commands
{
	public class AvailTokenItem
	{

		public TokenTypes TokenType { get; set; }

		public string[] Names { get; set; }

		public AvailTokenItem(TokenTypes tokenType, string[] names)
		{
			TokenType = tokenType;
			Names = names;
		}

		public bool IsCommand(string cmdStr, int minLen)
		{
			foreach (string cmd in Names)
			{
				int min = Math.Min(minLen, cmd.Length);
				if (cmd == cmdStr) return true;
				if (cmd.Length < cmdStr.Length) continue;
				if (cmdStr == cmd.Substring(0, cmdStr.Length)) return true;
			}
			return false;
		}

		public override string ToString()
		{
			return $"{TokenType} {Names[0]}";
		}
	}
}
