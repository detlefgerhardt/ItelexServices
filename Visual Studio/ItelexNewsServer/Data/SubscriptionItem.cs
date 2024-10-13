using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexNewsServer.Data
{
	[Serializable]
	class SubscriptionItem
	{
		[SqlInt]
		public Int64 ChannelId { get; set; }

		[SqlInt]
		public Int64 UserId { get; set; }

		/// <summary>
		/// Selected content (t,d,td)
		/// </summary>
		//[SqlString]
		//public string Content { get; set; }

		[SqlDateStr]
		public DateTime? SubscribeTimeUtc { get; set; }
	}
}
