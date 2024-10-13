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
using Telexsuche;

namespace ItelexAuskunft
{
	class AuskunftConnectionManager: IncomingConnectionManagerAbstract
	{
		private const string TAG = nameof(AuskunftConnectionManager);

		public AuskunftConnectionManager() : base()
		{
			try
			{
				Abfrage.Instance.LoadTables(Helper.GetExePath());
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(AuskunftConnectionManager), "Error in LoadTables", ex);
			}
		}

		protected override ItelexIncoming CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			return new AuskunftConnection(client, connectionId, logPath, logLevel);
		}

	}
}
