using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using ItelexCommon.Monitor;
using ItelexCommon.Connection;

namespace ItelexWeatherServer
{
	class WeatherConnectionManager: IncomingConnectionManagerAbstract
	{
		private const string TAG = nameof(WeatherConnectionManager);

		public WeatherConnectionManager(): base()
		{
		}

		protected override ItelexIncoming CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			return new IncomingConnection(client, connectionId, logPath, logLevel);
		}
	}
}
