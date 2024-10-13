using ItelexCommon;
using ItelexCommon.Connection;
using System.Net.Sockets;

namespace ItelexMsgServer.Connections
{
	class IncomingConnectionManager: IncomingConnectionManagerAbstract
	{ 
		private const string TAG = nameof(IncomingConnectionManager);

		public IncomingConnectionManager(): base()
		{
		}

		protected override ItelexIncoming CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			return new IncomingConnection(client, connectionId, logPath, logLevel);
		}
	}
}
