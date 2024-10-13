using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer
{
	class SendItem
	{
		public string Uid { get; set; }

		public int TelexNumber { get; set; }

		public SendItem(string line)
		{
			string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Uid = parts[0];
			if (int.TryParse(parts[1], out int number))
			{
				TelexNumber = number;
			}
		}

		public SendItem(string uid, int number)
		{
			Uid = uid;
			TelexNumber = number;
		}

		public bool CheckSend(string uid, int number)
		{
			return uid == Uid && number == TelexNumber;
		}

		public string Line
		{
			get
			{
				return $"{Uid} {TelexNumber}";
			}
		}
	}
}
