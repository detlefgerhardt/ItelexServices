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
using ItelexCommon.Logger;

namespace ItelexBildlocher
{
	class BildlocherConnectionManager : IncomingConnectionManagerAbstract
	{
		private const string TAG = nameof(BildlocherConnectionManager);

		public BildlocherConnectionManager()
		{
		}

		protected override ItelexIncoming CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			return new IncomingConnection(client, connectionId, logPath, logLevel);
		}

	}
}
