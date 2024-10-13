using ItelexRundsender.Languages;
using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using ItelexCommon.Monitor;
using ItelexCommon.Connection;
using static ItelexCommon.Connection.ItelexConnection;

namespace ItelexRundsender.Connections
{
	class RundsenderConnectionManager: IncomingConnectionManagerAbstract
	{
		private const string TAG = nameof(RundsenderConnectionManager);

		public RundsenderConnectionManager()
		{
		}

		protected override ItelexIncoming CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			ItelexLogger itelexLogger = new ItelexLogger(connectionId, ConnectionDirections.In, null, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
			return new IncomingConnection(client, connectionId, itelexLogger);
		}

		private static ItelexLogger GetLogger(int connectionId, string logPath, LogTypes logLevel)
		{
			return new ItelexLogger(connectionId, ConnectionDirections.In, null, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
		}

	}
}
