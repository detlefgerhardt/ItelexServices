using ItelexCommon.Utility;
using Newtonsoft.Json;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ItelexCommon.Monitor
{
	internal class MonitorServerConnection
	{
		private const int RECV_BUFFERSIZE = 2048 + 2;

		private TcpClient _client;

		private bool _endSend;

		private bool _connected;

		public MonitorServerConnection(TcpClient client)
		{
			_client = client;
		}

		public void Start()
		{
			if (_client == null) return;

			StartReceive();

			_connected = true;
			while(_connected)
			{
				Thread.Sleep(100);
			}
			_client.Client.Close();
			_client.Close();
			_client.Dispose();
		}

		private void StartReceive()
		{
			byte[] buffer = new byte[RECV_BUFFERSIZE];
			try
			{
				_client.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, DataReceived, buffer);
			}
			catch (Exception)
			{
				_connected = false;
			}
		}

		private void DataReceived(IAsyncResult ar)
		{
			int dataReadCount;

			try
			{
				dataReadCount = _client.Client.EndReceive(ar);
			}
			catch (Exception ex)
			{
				if (ex is SocketException)
				{
				}
				else
				{
				}
				return;
			}

			if (dataReadCount == 0)
			{
				_connected = false;
				return;
			}

			byte[] byteData = ar.AsyncState as byte[];
			//byte[] newData = byteData.Take(dataReadCount).ToArray();
			string jsonString = Encoding.UTF8.GetString(byteData, 0, dataReadCount);

			MonitorRequest request = JsonConvert.DeserializeObject<MonitorRequest>(jsonString);
			if (request != null)
			{
				MonitorResponse response = null;
				MonitorCmds? action = null;

				switch (request.ReqCmd)
				{
					case MonitorCmds.Ping:
						break;
					case MonitorCmds.Info:
						response = new MonitorResponseInfo(request)
						{
							PrgmType = MonitorManager.Instance.PrgmType,
							Version = MonitorManager.Instance.PrgmVersion,
							ItelexNumber = MonitorManager.Instance.ItelexNumber,
							ItelexLocalPort = MonitorManager.Instance.ItelexLocalPort,
							ItelexPublicPort = MonitorManager.Instance.ItelexPublicPort,
							StartupTime = MonitorManager.Instance.StartupTime,
							LoginCount = MonitorManager.Instance.LoginCount,
							LoginUserCount = MonitorManager.Instance.LoginUserCount,
							LastLoginTime = MonitorManager.Instance.LastLoginTime,
							LastUser = MonitorManager.Instance.LastUser,
							Status = MonitorManager.Instance.Status,
						};
						break;
					case MonitorCmds.Shutdown:
						action = MonitorCmds.Shutdown;
						//MonitorManager.Instance.ReceivedAction(MonitorCmds.Shutdown);
						break;
				}

				if (response != null)
				{
					string jsonStr = JsonConvert.SerializeObject(response);
					byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
					_endSend = false;
					_client.Client.BeginSend(jsonBytes, 0, jsonBytes.Length, SocketFlags.None, EndSend, null);
					TickTimer timer = new TickTimer();
					while(!timer.IsElapsedMilliseconds(5000))
					{
						if (_endSend) break;
					}
				}

				if (action != null)
				{
					switch (action)
					{
						case MonitorCmds.Shutdown:
							MonitorManager.Instance.ReceivedAction(MonitorCmds.Shutdown);
							break;
					}
				}
			}
			StartReceive();
		}

		private void EndSend(IAsyncResult ar)
		{
			try
			{
				_client.Client.EndSend(ar);
				_endSend = true;
				if (!_client.Connected)
				{
					_connected = false;
				}
			}
			catch
			{
				_client.Close();
				_connected = false;
			}
		}

	}
}
