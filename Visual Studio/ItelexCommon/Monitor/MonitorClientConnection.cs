using ItelexCommon.Utility;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ItelexCommon.Monitor
{
	public class MonitorClientConnection
	{
		private const string TAG = nameof(MonitorClientConnection);

		private const int RECV_BUFFERSIZE = 2048 + 2;

		private const int TCP_TIMEOUT_MS = 5000;
		private const int RESPONSE_TIMEOUT_MS = 10000;

		private TcpClientWithTimeout _clientWithTimeout;
		private TcpClient _client;

		private int _requestId;

		private int _port;

		private bool _connected;

		private Logging _logger;

		private MonitorResponse _response;

		public MonitorClientConnection(Logging logger)
		{
			_logger = logger;
		}

		public bool Connect(string host, int port)
		{
			if (host == null) host = "127.0.0.1";

			try
			{
				_clientWithTimeout = new TcpClientWithTimeout(host, port, TCP_TIMEOUT_MS);
				_client = _clientWithTimeout.Connect();
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(Connect), "", ex);
				return false;
			}

			//_client = new TcpClient(host, port);
			if (_client == null || !_client.Connected) return false;

			//if (port == 9141)
			//{
			//	Debug.Write("");
			//}

			IPEndPoint endPoint = (IPEndPoint)_client.Client.RemoteEndPoint;
			string remoteClientAddr = $"{endPoint.Address}:{endPoint.Port}";
			_port = port;

			StartReceive();

			return true;
		}

		public void Disconnect()
		{
			try
			{
				_client?.Client?.Close();
				_client?.Close();
				_client.Dispose();
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(Disconnect), "", ex);
			}
		}

		public MonitorResponseInfo RequestInfo()
		{
			MonitorRequest request = new MonitorRequest()
			{
				ReqId = ++_requestId,
				ReqCmd = MonitorCmds.Info,
			
			};
			return SendRequest<MonitorResponseInfo>(request);
		}

		public MonitorResponseOk RequestShutdown()
		{
			MonitorRequest request = new MonitorRequest()
			{
				ReqId = ++_requestId,
				ReqCmd = MonitorCmds.Shutdown
			};

			return SendRequest<MonitorResponseOk>(request);
		}

		private T SendRequest<T>(MonitorRequest request)
		{
			_logger.Debug(TAG, nameof(SendRequest), $"{request}");

			try
			{
				string jsonStr = JsonConvert.SerializeObject(request);
				byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
				_client.Client.BeginSend(jsonBytes, 0, jsonBytes.Length, SocketFlags.None, EndSend, null);

				_response = null;

				if (request.ReqCmd == MonitorCmds.Shutdown) return default(T);

				TickTimer timer = new TickTimer();
				while (_response == null)
				{
					Thread.Sleep(100);
					if (timer.IsElapsedMilliseconds(RESPONSE_TIMEOUT_MS))
					{
						_logger.Debug(TAG, nameof(SendRequest), $"response timeout request={request} {timer.ElapsedMilliseconds}");
						return default(T);
					}
				}

				_logger.Debug(TAG, nameof(SendRequest), $"response {_response} {timer.ElapsedMilliseconds}");
				if (_response.GetType() != typeof(T) || _response.RespId != request.ReqId)
				{
					Debug.WriteLine($"wrong type {request.GetType()} or reqId {request.ReqId}");
					return default(T);
				}

				_logger.Debug(TAG, nameof(SendRequest), $"{_response}");
				return (T)(object)_response;
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(SendRequest), $"error sending {request}", ex);
				return default(T);
			}
		}

		private void StartReceive()
		{
			byte[] buffer = new byte[RECV_BUFFERSIZE];
			try
			{
				_client.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, DataReceived, buffer);
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(StartReceive), "", ex);
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
				_logger.Error(TAG, nameof(StartReceive), "", ex);
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
				_client.Close();
				_connected = false;
				return;
			}

			byte[] byteData = ar.AsyncState as byte[];
			//byte[] newData = byteData.Take(dataReadCount).ToArray();
			string jsonString = Encoding.UTF8.GetString(byteData, 0, dataReadCount);

			MonitorResponse response = JsonConvert.DeserializeObject<MonitorResponse>(jsonString);
			if (response != null)
			{
				switch (response.RespCmd)
				{
					case MonitorCmds.Ping:
						break;
					case MonitorCmds.Info:
						_response = JsonConvert.DeserializeObject<MonitorResponseInfo>(jsonString);
						return;
				}
			}
			StartReceive();
		}

		private void EndSend(IAsyncResult ar)
		{
			try
			{
				_client.Client.EndSend(ar);
				if (!_client.Connected)
				{
					_connected = false;
				}
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(StartReceive), "", ex);
				_client.Close();
				_connected = false;
			}
		}

	}
}
