using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Commands
{
	public class CmdTokenItem
	{
		public TokenTypes TokenType { get; set; }

		//public string Value { get; set; }

		public object Value { get; set; }

		public CmdTokenItem(TokenTypes tokenType)
		{
			TokenType = tokenType;
			Value = null;
		}

		public CmdTokenItem(TokenTypes tokenType, object value)
		{
			TokenType = tokenType;
			Value = value;
		}

		public int? GetNumericValue()
		{
			if (Value.GetType() != typeof(int)) return null;
			return (int)Value;
		}

		public string GetStringValue()
		{
			if (Value.GetType() != typeof(string)) return null;
			return (string)Value;
		}

		public override string ToString()
		{
			return Value != null ? $"[{TokenType} {Value}]" : $"[{TokenType}]";
		}
	}
}
