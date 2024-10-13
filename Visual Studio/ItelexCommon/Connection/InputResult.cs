using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Connection
{
	public class InputResult
	{
		public string InputString { get; set; }

		public int InputNumber { get; set; }

		public bool InputBool { get; set; }

		public bool IsNumber { get; set; }

		public bool IsHelp { get; set; }

		public bool ErrorOrTimeoutOrDisconnected { get; set; }

		public bool Timeout { get; set; }

		public InputResult()
		{
			InputString = "";
		}

		public override string ToString()
		{
			if (ErrorOrTimeoutOrDisconnected)
			{
				return "error";
			}
			if (IsHelp)
			{
				return "help";
			}
			if (IsNumber)
			{
				return $"#{InputNumber}";
			}
			return $"'{InputString}'";
		}
	}
}
