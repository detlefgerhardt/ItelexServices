using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexRundsender.Connections
{
	class ParseNumbersResult
	{
		//public static readonly string[] NumEndingString = new string[] { "+++", "+", "e e e", "xxx", "x x x" };

		public static readonly string[] CorrectionString = new string[] { "e e e", "xxx", "x x x" };

		public static readonly string[] NumCmdString = new string[] { "empf" };

		public List<Receiver> Receivers { get; set; }

		public NumberProperty LineEnd { get; set; }

		public bool WithRecvList { get; set; }

		public bool Error { get; set; }

		public ParseNumbersResult(bool error)
		{
			Receivers = null;
			LineEnd = NumberProperty.None;
			WithRecvList = false;
			Error = error;
		}

		//public static NumberProperty CheckLineEnding(string inputStr)
		//{
		//	foreach (string ending in NumEndingString)
		//	{
		//		if (inputStr.EndsWith(ending)) return NumberPropertyToEnum(ending);
		//	}
		//	return NumberProperty.None;
		//}

		public static NumberProperty NumberPropertyToEnum(string endStr)
		{
			switch (endStr)
			{
				//case "+": // end of numbers
				//	return NumberProperty.EndOfNumbers;
				//case "+++": // end of numbers
				//	return NumberProperty.EndOfMessage;
				case "e e e": // skip
				case "xxx": //  skip
				case "xxxxx": // skip
				case "x x x": // skip
					return NumberProperty.NumberCorrection;
				default:
					return NumberProperty.None;
			}
		}

		public override string ToString()
		{
			return $"({Receivers}) {LineEnd}";
		}

	}
}
