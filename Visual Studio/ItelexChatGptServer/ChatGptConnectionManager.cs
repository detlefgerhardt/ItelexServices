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

namespace ItelexChatGptServer
{
	class ChatGptConnectionManager: IncomingConnectionManagerAbstract
	{
		private const string TAG = nameof(ChatGptConnectionManager);

		public ChatGptConnectionManager()
		{
		}

		protected override ItelexIncoming CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			return new IncomingConnection(client, connectionId, logPath, logLevel);
		}

	}
}
