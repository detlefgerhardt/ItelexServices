using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Monitor;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ItelexBaudotArtServer
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0074:Use compound assignment", Justification = "<Pending>")]
	class BaudotArtConnectionManager: IncomingConnectionManagerAbstract
	{
		private const string TAG = nameof(BaudotArtConnectionManager);

		public BaudotArtConnectionManager()
		{
		}

		protected override ItelexIncoming CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			return new BaudotArtConnection(client, connectionId, logPath, logLevel);
		}
	}
}
