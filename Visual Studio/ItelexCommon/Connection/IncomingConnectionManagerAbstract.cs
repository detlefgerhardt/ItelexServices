using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Monitor;
using ItelexCommon.Utility;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ItelexCommon
{
	public abstract class IncomingConnectionManagerAbstract
	{
		private const string TAG = nameof(IncomingConnectionManagerAbstract);

		protected Logging _logger;

		private readonly object _connectionsLock = new object();
		private List<ItelexIncoming> _connections;

		public delegate void UpdateEventHandler();
		public event UpdateEventHandler UpdateIncoming;

		public delegate void LoginLogoffEventHandler(string message);
		public event LoginLogoffEventHandler LoginLogoff;

		private MessageDispatcher _messageDispatcher;

		public string OurIpAddressAndPort { get; set; }

		private TcpListener _tcpListener;

		private SubscriberServer _subscriberServer;

		private IncomingConnectionManagerConfig _config;

		private bool _shutDown;

		private System.Timers.Timer _serverUpdateTimer;
		private bool _serverUpdateTimerActive;

		public IncomingConnectionManagerAbstract()
		{
			_logger = LogManager.Instance.Logger;
			_messageDispatcher = MessageDispatcher.Instance;

			_subscriberServer = new SubscriberServer();
			_connections = new List<ItelexIncoming>();

			_serverUpdateTimerActive = false;
			_serverUpdateTimer = new System.Timers.Timer(ItelexConstants.TLNSERVER_REFRESH_SEC * 1000);
			_serverUpdateTimer.Elapsed += ServerUpdateTimer_Elapsed;
		}

		public bool SetRecvOn(IncomingConnectionManagerConfig config)
		{
			_config = config;

			_logger.Debug(TAG, nameof(SetRecvOn), "");
			try
			{
				UpdateIpAddress();

				_tcpListener = new TcpListener(IPAddress.Any, _config.IncomingLocalPort);
				_tcpListener.Start();

				// start listener task for incoming connections
				Task _listenerTask = Task.Run(() => Listener());
				_serverUpdateTimer.Start();
				return true;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(SetRecvOn), "", ex);
				_messageDispatcher.Dispatch(ex.Message);
				return false;
			}
		}

		public bool SetRecvOff()
		{
			_logger.Debug(TAG, nameof(SetRecvOff), "");
			_tcpListener.Stop();
			return true;
		}

		private void ServerUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			UpdateIpAddress();
		}

		public void UpdateIpAddress()
		{
			if (_config.FixDns) return;

			if (_serverUpdateTimerActive) return;
			_serverUpdateTimerActive = true;

			try
			{
				_subscriberServer.Connect();
				SendClientUpdate(_config.ItelexNumber, _config.TlnServerServerPin, _config.IncomingPublicPort);
				_subscriberServer.Disconnect();
				UpdateIncoming?.Invoke();
			}
			finally
			{
				_serverUpdateTimerActive = false;
			}
		}

		private void SendClientUpdate(int number, int pin, int publicPort)
		{
			string oldIpAddr = OurIpAddressAndPort;

			ClientUpdateReply reply = _subscriberServer.SendClientUpdate(number, pin, publicPort);
			string msg;
			if (reply.Success)
			{
				msg = $"update {number} ok / {reply.IpAddress} {publicPort}";
				OurIpAddressAndPort = $"{reply.IpAddress} {publicPort}";
			}
			else
			{
				msg = $"update {number} {reply.Error}";
				OurIpAddressAndPort = reply.Error;
			}

			_logger.Notice(TAG, nameof(SendClientUpdate), msg);
			if (OurIpAddressAndPort == oldIpAddr)
			{
				msg += " (no change)";
			}
			_messageDispatcher.Dispatch(msg);
		}

		private void Listener()
		{
			TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(Listener));
			while (true)
			{
				if (_shutDown)
				{
					SetRecvOff();
					TaskManager.Instance.RemoveTask(Task.CurrentId);
					return;
				}

				try
				{
					_logger.Debug(TAG, nameof(Listener), "Wait for connection");

					// wait for connection
					TickTimer pendingTimer = new TickTimer();

					while (!_tcpListener.Pending())
					{
						Thread.Sleep(50);
						if (pendingTimer.IsElapsedMinutes(MonitorManager.WATCHDOG_TIME))
						{
							MonitorManager.Instance.TriggerWatchdog();
							pendingTimer.Start();
						}
					}
					TcpClient client = _tcpListener.AcceptTcpClient();

					int idNumber = GetNextConnectionId();

					Task.Run(() =>
					{
						try
						{
							TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(Listener), "new conn");
							ItelexIncoming conn = CreateConnection(client, _currentIdNumber, _config.LogPath, _config.LogLevel);

							conn.IncomingLoggedIn += Conn_LoggedIn;
							conn.ItelexUpdate += Conn_Update;
							conn.IncomingGotAnswerback += Conn_GotAnswerback;
							conn.ItelexReceived += Conn_Received;

							AddConnection(conn);

							string msg = $"incoming connection from {conn.RemoteClientAddrStr}";
							_messageDispatcher.Dispatch(conn.ConnectionId, msg);

							conn.Start();
							//UpdateIncoming?.Invoke();

							_logger.Debug(TAG, nameof(Listener), $"session terminated {conn.ConnectionName}");
							_messageDispatcher.Dispatch(conn.ConnectionId, $"session terminated {conn.ConnectionName}");

							ConnectionDropped(conn);

							MonitorManager.Instance.SetInactive(); // TODO: prüfen ob weitere Verbindung vorhanden

							conn.IncomingLoggedIn -= Conn_LoggedIn;
							conn.ItelexUpdate -= Conn_Update;
							conn.IncomingGotAnswerback -= Conn_GotAnswerback;
							conn.ItelexReceived -= Conn_Received;
							RemoveConnection(conn);
							conn.Dispose();
							TaskManager.Instance.RemoveTask(Task.CurrentId);
						}
						finally
						{
							TaskManager.Instance.RemoveTask(Task.CurrentId);
						}
					});
				}
				catch (Exception ex)
				{
					if (ex.HResult == -2147467259)
					{
						_logger.Error(TAG, nameof(Listener), $"Listener ex={ex.Message}");
					}
					else
					{
						_logger.Error(TAG, nameof(Listener), "", ex);
					}
				}
			}
		}

		protected virtual ItelexIncoming CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			return null;
		}

		private int _currentIdNumber = 0;
		private object _currentIdNumberLock = new object();

		public int GetNextConnectionId()
		{
			lock (_currentIdNumberLock)
			{
				_currentIdNumber = _config.GetNewSession(_currentIdNumber);
				return _currentIdNumber;
			}
		}

		private void Conn_GotAnswerback(ItelexIncoming conn)
		{
			MonitorManager.Instance.AddLogin(conn.RemoteAnswerbackStr);
		}

		private void Conn_LoggedIn(ItelexIncoming connection)
		{
			LoggedIn(connection);
		}

		protected virtual void LoggedIn(ItelexIncoming connection)
		{
		}

		protected virtual void Conn_Received(ItelexConnection connection, string asciiText)
		{
		}

		private void Conn_Update(ItelexConnection conn)
		{
			UpdateIncoming?.Invoke();
		}

		/// <summary>
		/// used by chatserver
		/// </summary>
		/// <param name="conn"></param>
		protected virtual void ConnectionDropped(ItelexIncoming conn)
		{
		}

		/// <summary>
		/// used by chatserver
		/// </summary>
		protected void InvokeUpdateIncoming()
		{
			UpdateIncoming?.Invoke();
		}

		public void AddConnection(ItelexIncoming conn)
		{
			//_logger.Debug(TAG, nameof(Listener), $"add connection {conn.ConnectionName}");
			lock (_connectionsLock)
			{
				_connections.Add(conn);
			}
			UpdateIncoming?.Invoke();
		}

		public void RemoveConnection(ItelexIncoming conn)
		{
			//_logger.Debug(TAG, nameof(Listener), $"remove connection {conn.ConnectionName}");
			lock (_connectionsLock)
			{
				_connections.Remove(conn);
			}
			UpdateIncoming?.Invoke();
		}

		public virtual void Shutdown()
		{
			_shutDown = true;

			//Message?.Invoke($"shutdown for maintenance");
			_messageDispatcher.Dispatch("Shutdown for maintenance");
			_logger.Notice(TAG, nameof(Shutdown), $"Shutdown for maintenance, {_connections.Count} active");
			//string timeStr = ItelexConstants.PRINT_TIME ? $"({DateTime.Now:HH:mm})" : "";
			foreach (ItelexIncoming conn in _connections)
			{
				conn.SendAscii($"\r\nshut down for maintenance\r\n\n");
			}
			foreach (ItelexIncoming conn in _connections)
			{
				conn.WaitAllSendBuffersEmpty();
				conn.Disconnect(ItelexConnection.DisconnectReasons.ServiceShutdown);
			}

			TickTimer _timeout = new TickTimer();
			while (_connections.Count > 0)
			{
				if (_timeout.IsElapsedMilliseconds(6000)) break;
				Thread.Sleep(500);
				_logger.Debug(TAG, nameof(Shutdown), $"Waiting for active connections to terminate, {_connections.Count} active");
			}

			//Thread.Sleep(1000);
		}

		public List<ItelexIncoming> CloneConnections()
		{
			List<ItelexIncoming> conns = new List<ItelexIncoming>();
			lock (_connectionsLock)
			{
				conns.AddRange(_connections);
			}
			return conns;
		}

		public List<T> CloneConnections<T>() where T: ItelexIncoming
		{
			List<T> conns = new List<T>();
			lock (_connectionsLock)
			{
				foreach (ItelexIncoming conn in _connections)
				{
					conns.Add((T)conn);
				}
			}
			return conns;
		}
	}
}
