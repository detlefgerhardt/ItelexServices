using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Data
{
	class MailParseData
	{
		public string Pin { get; set; }

		public List<int> TelexNumbers { get; set; }

		public string EventCode { get; set; }

		public string Text { get; set; }

		public int LineCount { get; set; }

		public MailParseData()
		{
			TelexNumbers = new List<int>();
		}

		public void AddNumber(int number)
		{
			if (!TelexNumbers.Contains(number)) TelexNumbers.Add(number);
		}

		public void AddNumbers(List<int> numbers)
		{
			foreach (int num in numbers)
			{
				AddNumber(num);
			}
		}

		public void RemoveDuplicateNumbers()
		{
			List<int> newNumbers = new List<int>();
			foreach(int n in TelexNumbers)
			{
				if (!newNumbers.Contains(n))
				{
					newNumbers.Add(n);
				}
			}
			TelexNumbers = newNumbers;
		}

		public override string ToString()
		{
			string numbers = "";
			foreach (int num in TelexNumbers)
			{
				numbers += $"{num},";
			}
			numbers = numbers.Trim(',');
			return $"{Pin} {numbers} {Text}";
		}
	}
}
