using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexNewsServer.Data
{
	class MessageItem
	{
	}

	class MessagePinItem : MessageItem
	{
		public MessagePinItem()
		{
		}
	}

	class MessageNewsItem : MessageItem
	{
		public long NewsId { get; set; }

		public MessageNewsItem(long newsId)
		{
			NewsId = newsId;
		}

		public override string ToString()
		{
			return $"{NewsId}";
		}
	}
}
