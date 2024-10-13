using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexNewsServer.Data
{
	[Serializable]
	class NewsItem
	{
		[SqlId]
		public Int64 NewsId { get; set; }

		[SqlInt]
		public Int64 ChannelId { get; set; }

		[SqlString(Length = 20)]
		public string Author { get; set; }

		[SqlString]
		public string OriginalNewsId { get; set; }

		[SqlDateStr]
		public DateTime NewsTimeUtc
		{
			get
			{
				return _newsTimeUtc;
			}
			set
			{
				// store as utc kind
				_newsTimeUtc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
			}
		}
		private DateTime _newsTimeUtc;

		[SqlString]
		public string Title { get; set; }

		[SqlString]
		public string Message { get; set; }

		[SqlBool]
		public bool AllSend { get; set; }

		public bool Error { get; set; }

		public bool IsInvalid()
		{
			return string.IsNullOrEmpty(OriginalNewsId) || (string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Message));
		}

		public bool Contains(string str)
		{
			if (str == null) return false;
			str = str.ToLower();
			return Title != null && Title.ToLower().Contains(str) || Message != null && Message.ToLower().Contains(str);
		}

		public override string ToString()
		{
			return $"{NewsId} {ChannelId} {NewsTimeUtc} {Title} {Message}";
		}
	}
}
