using ItelexCommon;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Notification
{
	/// <summary>
	/// Notification queue item for one or more messages
	/// </summary>
	[DataContract(Namespace = "")]
	public class NotificationSendQueueItem
	{
		[DataMember]
		public NotificationSubscriptionItem NotificationNumber { get; set; }

		[DataMember]
		public NotificationTypes Type { get; set; }

		[DataMember]
		public List<string> Messages { get; set; }

		[DataMember]
		public int Retries { get; set; } = 0;

		[DataMember]
		public bool Success { get; set; } = false;

		[DataMember]
		public TickTimer LastTry { get; set; }

		public int ItelexNumber => NotificationNumber == null ? 0 : NotificationNumber.ItelexNumber;

		public override string ToString()
		{
			return $"{NotificationNumber?.ItelexNumber} {NotificationNumber?.Extension} {Retries} {Type}";
		}
	}
}
