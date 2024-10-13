using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ItelexChatServer.Notification
{
	[DataContract(Namespace = "")]
	public class NotificationSaveData
	{
		[DataMember]
		public List<NotificationSubscriptionItem> NotificationList { get; set; }
		//public List<NotificationQueueItem> NotificationList { get; set; }
	}
}
