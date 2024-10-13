using ItelexCommon.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Connection
{
	public class IncomingConnectionManagerConfig
	{
		public string PrgmVersionStr { get; set; }

		public int ItelexNumber { get; set; }

		public bool FixDns { get; set; }

		public string Host { get; set; }

		public int IncomingPublicPort { get; set; }

		public int IncomingLocalPort { get; set; }

		public int TlnServerServerPin { get; set; }

		public ItelexExtensionConfiguration[] ItelexExtensions { get; set; }

		public MonitorServerTypes MonitorServerType { get; set; }

		public int MonitorPort { get; set; }

		public string ExePath { get; set; }

		public string LogPath { get; set; }

		public Func<int, int> GetNewSession { get; set; }

		public LogTypes LogLevel { get; set; }
	}
}
