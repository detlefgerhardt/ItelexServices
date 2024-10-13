using ItelexCommon;
using ItelexCommon.Utility;
using ItelexNewsServer.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexNewsServer.Data
{
	enum CallTypes { SendPin, SendMsg };

	class CallItem
	{
		//public CallTypes CallType { get; set; }

		public int MaxRetryCount { get; set; }

		//public TickTimer LastRetryTick { get; set; }

		//public bool SendActive { get; set; }

		//public string LastResult { get; set; }

		public UserItem User { get; set; }

		//public string Language { get; set; }

		/// <summary>
		/// is destination a service?
		/// </summary>
		public bool IsServiceNumber { get; set; }

		public List<MessageNewsItem> Messages { get; set; }

		public CallItem(UserItem user)
		{
			User = user;
			Messages = new List<MessageNewsItem>();
		}

		public void AddMsg(MessageNewsItem msgItem)
		{
			Messages.Add(msgItem);
		}

		public override string ToString()
		{
			return $"{User?.ItelexNumber} {Messages?.Count}";
		}
	}
}
