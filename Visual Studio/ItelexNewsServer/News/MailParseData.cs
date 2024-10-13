using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexNewsServer.News
{
	class MailParseData
	{
		public int LocalChannelNo { get; set; }

		public string LocalChannelPin { get; set; }

		public int SenderNumber { get; set; }

		public string Text { get; set; }

		//public int LineCount { get; set; }

		public MailParseData()
		{
		}

		public override string ToString()
		{
			return $"{LocalChannelNo} {LocalChannelPin} {SenderNumber} {Text}";
		}
	}
}
