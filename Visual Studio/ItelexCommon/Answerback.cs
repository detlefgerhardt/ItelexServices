using System;
using System.Collections.Generic;
using System.Linq;

namespace ItelexCommon
{
	public class Answerback
	{
		public string Name { get; }

		public string NumberStr { get; }

		public int? NumberInt { get; }

		public Answerback(string answerback)
		{
			if (string.IsNullOrEmpty(answerback))
			{
				Name = "";
				NumberStr = "";
				NumberInt = null;
				return;
			}

			Name = answerback.Trim(new char[] { ' ', CodeManager.ASC_CR, CodeManager.ASC_LF });

			if (string.IsNullOrWhiteSpace(Name))
			{
				Name = answerback;
				NumberStr = null;
				NumberInt = null;
				return;
			}

			NumberInt = ParseNumber(Name);
			NumberStr = NumberInt.HasValue ? NumberInt.ToString() : null;
		}

		private int? ParseNumber(string kg)
		{
			string numStr = "";
			for (int i = 0; i < kg.Length; i++)
			{
				if (!char.IsDigit(kg[i])) break;
				numStr += kg[i];
			}

			if (int.TryParse(numStr, out int num)) return num;
			return null;
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(Name))
			{
				return "";
			}
			return Name;
		}
	}
}
