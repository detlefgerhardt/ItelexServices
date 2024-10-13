using ItelexCommon.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace ItelexMonitor
{
	internal class ServiceItem
	{
		public MonitorServerTypes PrgmType { get; set; }

		public string Name { get; set; }

		public string PrgmVersion { get; set; }

		public string Address { get; set; }

		public int ItelexNumber { get; set; }

		public int ItelexLocalPort { get; set; }

		public int ItelexPublicPort { get; set; }

		public DateTime? StartupTime { get; set; }

		public DateTime? LastLoginTime { get; set; }

		public string LastUser { get; set; }

		public int LoginCount { get; set; }

		public int LoginUserCount { get; set; }

		public MonitorServerStatus Status { get; set; }

		public string StatusString => MonitorManager.Instance.ServerStatusToString(Status);

		public string LastLoginTimeString
		{
			get
			{
				if (LastLoginTime == null) return "";
				return $"{LastLoginTime:dd.MM. HH:mm}";
			}
		}

		public string UptimeString
		{
			get
			{
				if (StartupTime == null) return "";
				int totalMinutes = (int)(DateTime.Now - StartupTime.Value).TotalMinutes;
				if (totalMinutes < 60) return $"{totalMinutes} min";
				return $"{totalMinutes / 60} h";
			}
		}

		public string LastUserString => LastUser != null ? LastUser : "";

		public bool Equals(ServiceItem item)
		{
			return PrgmType == item.PrgmType &&
				PrgmVersion == item.PrgmVersion &&
				Address == item.Address &&
				ItelexNumber == ItelexNumber &&
				ItelexLocalPort == item.ItelexLocalPort &&
				ItelexPublicPort == item.ItelexPublicPort &&
				StartupTime == item.StartupTime &&
				LastLoginTime == item.LastLoginTime &&
				LastUser == item.LastUser &&
				LoginCount == item.LoginCount &&
				LoginUserCount == item.LoginUserCount;
		}

	}
}
