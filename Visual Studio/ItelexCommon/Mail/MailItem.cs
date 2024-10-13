using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Mail
{
	public class MailItem
	{
		public string Uid { get; set; }

		public string From { get; set; }

		public string ToName { get; set; }

		public string ToAddress { get; set; }

		public DateTime DateSentUtc { get; set; }

		public string Subject { get; set; }

		//public int[] ItelexNumbers { get; set; }

		public string Body { get; set; }

		public override string ToString()
		{
			return $"{Uid} {From} {ToAddress} {Subject} {DateSentUtc}";
		}
	}
}
