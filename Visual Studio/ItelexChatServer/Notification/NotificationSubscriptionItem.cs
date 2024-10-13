using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ItelexChatServer.Notification
{
	public enum NotificationTypes { SetupAdd, SetupDelete, Login, Logoff, Writing };

	[DataContract(Namespace = "")]
	public class NotificationSubscriptionItem
	{
		[DataMember]
		public int ItelexNumber { get; set; }

		[DataMember]
		public int Pin { get; set; }

		[DataMember]
		public int Extension { get; set; }

		[DataMember]
		public bool NotifyLogin { get; set; }

		[DataMember]
		public bool NotifyLogoff { get; set; }

		[DataMember]
		public bool NotifyWriting { get; set; }

		[DataMember]
		public string Language { get; set; }

		[DataMember]
		public string User { get; set; }

		public bool HasNotification(NotificationTypes type)
		{
			switch (type)
			{
				case NotificationTypes.SetupAdd:
				case NotificationTypes.SetupDelete:
					return true;
				case NotificationTypes.Login:
					return NotifyLogin;
				case NotificationTypes.Logoff:
					return NotifyLogoff;
				case NotificationTypes.Writing:
					return NotifyWriting;
			}
			return false;
		}

		public override string ToString()
		{
			return $"{ItelexNumber}({Extension}) {NotifyLogin} {NotifyLogoff} {NotifyWriting} lng={Language} user='{User}'";
		}
	}
}
