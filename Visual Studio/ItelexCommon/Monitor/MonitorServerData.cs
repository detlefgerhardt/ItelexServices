using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Monitor
{
	public class MonitorServerData
	{
		public MonitorServerTypes PrgmType { get; set; }

		public string Address { get; set; }

		public int Port { get; set; }

		public string Path { get; set; }

		public string Name => PrgmType.ToString();

		public MonitorServerData(MonitorServerTypes type, string address, int port, string path)
		{
			PrgmType = type;
			Address = address;
			Port = port;
			Path = path;
		}
	}
}
