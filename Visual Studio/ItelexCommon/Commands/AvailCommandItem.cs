using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Commands
{
	public class AvailCommandItem
	{
		public TokenTypes[] PossibleTokens { get; set; }

		public int Length => PossibleTokens.Length;

		public AvailCommandItem(TokenTypes[] possTokens)
		{
			PossibleTokens = possTokens;
		}

		public override string ToString()
		{
			string str = "";
			foreach (TokenTypes t in PossibleTokens)
			{
				if (str != "") str += " ";
				str += t.ToString();
			}
			return str;
		}
	}
}
