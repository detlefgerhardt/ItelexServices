using ItelexCommon;
using ItelexCommon.Connection;
using System.Collections.Generic;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net;
using ItelexCommon.Logger;
using ItelexCommon.Monitor;
using ItelexCommon.Utility;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using ItelexMsgServer.Data;

namespace ItelexMsgServer.Connections
{
	class MinitelexConnectionManager
	{ 
		private const string TAG = nameof(MinitelexConnectionManager);

		protected Logging _logger;

		private MessageDispatcher _messageDispatcher;

		private List<TcpListenerItem> _tcpListeners = new List<TcpListenerItem>();
		private List<TcpListenerItem2> _tcpListeners2 = new List<TcpListenerItem2>();
		private object _tcpListeners2Lock = new object();

		public IncomingConnectionManagerConfig Config { get; private set; }

		private MsgServerDatabase _database;

		private bool _shutDown;

		private static MinitelexConnectionManager _instance;
		public static MinitelexConnectionManager Instance => _instance ?? (_instance = new MinitelexConnectionManager());

		private MinitelexConnectionManager()
		{
			_logger = LogManager.Instance.Logger;
			_database = MsgServerDatabase.Instance;
			_messageDispatcher = MessageDispatcher.Instance;
		}

		public void SetRecvOn(IncomingConnectionManagerConfig config)
		{
			Config = config;

			List<MinitelexUserItem> minitelexUsers = _database.MinitelexUserLoadAll();
			if (minitelexUsers == null || minitelexUsers.Count == 0)
			{
				_messageDispatcher.Dispatch("No Minitelex tasks started.");
				_logger.Info(TAG, nameof(SetRecvOn), "No Minitelex tasks started");
				return;
			}

			Task.Run(() =>
			{
				TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(SetRecvOn));
				_logger.Debug(TAG, nameof(SetRecvOn), "");
				try
				{
					_tcpListeners = new List<TcpListenerItem>();
					List<Task> tasks = new List<Task>();
					foreach(MinitelexUserItem minitelexUser in minitelexUsers)
					{
						int localPort = config.IncomingLocalPort + minitelexUser.PortIndex;

						TcpListenerItem item = new TcpListenerItem()
						{
							Listener = new TcpListener(IPAddress.Any, localPort),
							Port = localPort
						};

						item.ListenerTask = Listener(item.Listener, localPort, Ready);
						item.Listener.Start();
						_tcpListeners.Add(item);
						tasks.Add(item.ListenerTask);
					}

					TickTimer timeout = new TickTimer();
					while (true)
					{
						int readyCnt = _tcpListeners.Count(t => t.Ready == true);
						if (readyCnt == _tcpListeners.Count) break;
						if (timeout.IsElapsedMilliseconds(10000))
						{
							_logger.Error(TAG, nameof(SetRecvOn), "timeout starting Minitelex tasks");
							break;
						}

						Task.Delay(100);
					};
					_messageDispatcher.Dispatch($"{_tcpListeners.Count} Minitelex tasks started.");
					_logger.Info(TAG, nameof(SetRecvOn), $"{_tcpListeners.Count} Minitelex tasks started");
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SetRecvOn), "", ex);
					_messageDispatcher.Dispatch(ex.Message);
					return false;
				}
				finally
				{
					TaskManager.Instance.RemoveTask(Task.CurrentId);
				}
			});
		}

		public void SetRecvOn2(IncomingConnectionManagerConfig config)
		{
			Config = config;

			List<MinitelexUserItem> minitelexUsers = _database.MinitelexUserLoadAll();
			if (minitelexUsers == null || minitelexUsers.Count == 0)
			{
				_messageDispatcher.Dispatch("No Minitelex tasks started.");
				_logger.Info(TAG, nameof(SetRecvOn), "No Minitelex tasks started");
				return;
			}

			_shutDown = false;
			//Task.Run(() =>
			{
				_logger.Debug(TAG, nameof(SetRecvOn), "");
				try
				{
					_tcpListeners2 = new List<TcpListenerItem2>();
					//List<Task> tasks = new List<Task>();
					foreach (MinitelexUserItem minitelexUser in minitelexUsers)
					{
						int localPort = config.IncomingLocalPort + minitelexUser.PortIndex;
						TcpListenerItem2 item = new TcpListenerItem2()
						{
							Port = localPort,
							User = minitelexUser,
							Stopped = false
						};

						ThreadPool.QueueUserWorkItem(Listener2, item);

						lock (_tcpListeners2Lock)
						{
							_tcpListeners2.Add(item);
						}
					}

					/*
					TickTimer timeout = new TickTimer();
					while (true)
					{
						int readyCnt = _tcpListeners.Count(t => t.Ready == true);
						if (readyCnt == _tcpListeners.Count) break;
						if (timeout.IsElapsedMilliseconds(10000))
						{
							_logger.Error(TAG, nameof(SetRecvOn), "timeout starting Minitelex tasks");
							break;
						}

						Task.Delay(100);
					};
					*/
					_messageDispatcher.Dispatch($"{_tcpListeners2.Count} Minitelex tasks started.");
					_logger.Info(TAG, nameof(SetRecvOn), $"{_tcpListeners2.Count} Minitelex tasks started");
					return;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SetRecvOn), "", ex);
					_messageDispatcher.Dispatch(ex.Message);
					return;
				}
			}
			//);
		}

		private void Ready(int port)
		{
			TcpListenerItem item = (from t in _tcpListeners where t.Port == port select t).FirstOrDefault();
			if (item == null) return;
			item.Ready = true;
			Debug.WriteLine(item.Port);
		}

		public bool SetRecvOff()
		{
			_logger.Debug(TAG, nameof(SetRecvOff), "");
			foreach (TcpListenerItem item in _tcpListeners)
			{
				item.Listener.Stop();
			}
			return true;
		}

		private Task Listener(TcpListener tcpListener, int port, Action<int> ready)
		{
			return Task.Run(() =>
			{
				TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(Listener));
				try
				{
					while (true)
					{
						if (_shutDown)
						{
							SetRecvOff();
							return;
						}

						int idNumber = 0;
						try
						{
							_logger.Debug(TAG, nameof(Listener), $"Wait for connection port={port}");

							ready(port);
							TcpClient client = tcpListener.AcceptTcpClient();

							idNumber = GetNextConnectionId();

							Task.Run(() =>
							{
								TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(Listener), $"new conn at port={port}");
								try
								{
									MinitelexConnection conn = CreateConnection(client, idNumber, Config.LogPath, Config.LogLevel);
									_logger.Debug(TAG, nameof(Listener), $"connection created id={idNumber}");

									//conn.IncomingLoggedIn += Conn_LoggedIn;
									//conn.ItelexUpdate += Conn_Update;
									//conn.IncomingGotAnswerback += Conn_GotAnswerback;
									//conn.ItelexReceived += Conn_Received;

									GlobalData.Instance.IncomingConnectionManager.AddConnection(conn);

									string msg = $"incoming connection from {conn.RemoteClientAddrStr}";
									_messageDispatcher.Dispatch(conn.ConnectionId, msg);

									conn.Start();
									//UpdateIncoming?.Invoke();

									_messageDispatcher.Dispatch(conn.ConnectionId, $"session terminated {conn.ConnectionName}");
									_logger.Debug(TAG, nameof(Listener), $"session terminated {conn.ConnectionName}");

									//ConnectionDropped(conn);
									MonitorManager.Instance.SetInactive(); // TODO: prüfen ob weitere Verbindung vorhanden

									GlobalData.Instance.IncomingConnectionManager.RemoveConnection(conn);
									_logger.Debug(TAG, nameof(Listener), $"connection removed {conn.ConnectionName}");
									conn.Dispose();
								}
								finally
								{
									TaskManager.Instance.RemoveTask(Task.CurrentId);
								}
							});
						}
						catch (Exception ex)
						{
							_messageDispatcher.Dispatch(idNumber, $"Minitelex Listener port={port} ex={ex.Message}");
							if (ex.HResult == -2147467259)
							{
								_logger.Error(TAG, nameof(Listener), $"ex={ex.Message}");
							}
							else
							{
								_logger.Error(TAG, nameof(Listener), "", ex);
							}
						}
					}
				}
				finally
				{
					TaskManager.Instance.RemoveTask(Task.CurrentId);
				}
			});
		}

		private void Listener2(object state)
		{
			TcpListenerItem2 listenerItem = (TcpListenerItem2)state;
			TcpListener tcpListener = new TcpListener(IPAddress.Any, listenerItem.Port);
			//TcpListener tcpListener = listenerItem.Listener;
			tcpListener.Start();

			//return Task.Run(() =>
			{
				while (true)
				{
					if (_shutDown)
					{
						lock (_tcpListeners2)
						{
							listenerItem.Stopped = true;
							return;
						}
					}

					int idNumber = 0;
					try
					{
						_logger.Debug(TAG, nameof(Listener), $"Wait for connection port={listenerItem.Port}");

						//ready(port);
						Debug.WriteLine($"wait for {listenerItem.Port}");
						TcpClient client = tcpListener.AcceptTcpClient();

						idNumber = GetNextConnectionId();

						Task.Run(() =>
						{
							TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(Listener2), "new conn");
							try
							{
								MinitelexConnection conn = CreateConnection(client, idNumber, Config.LogPath, Config.LogLevel);
								_logger.Info(TAG, nameof(Listener), $"connection created id={idNumber}");

								//conn.IncomingLoggedIn += Conn_LoggedIn;
								//conn.ItelexUpdate += Conn_Update;
								//conn.IncomingGotAnswerback += Conn_GotAnswerback;
								//conn.ItelexReceived += Conn_Received;

								GlobalData.Instance.IncomingConnectionManager.AddConnection(conn);

								string msg = $"incoming connection from {conn.RemoteClientAddrStr}";
								_messageDispatcher.Dispatch(conn.ConnectionId, msg);

								conn.Start();
								//UpdateIncoming?.Invoke();

								_messageDispatcher.Dispatch(conn.ConnectionId, $"session terminated {conn.ConnectionName}");
								_logger.Debug(TAG, nameof(Listener), $"session terminated {conn.ConnectionName}");

								//ConnectionDropped(conn);
								MonitorManager.Instance.SetInactive(); // TODO: prüfen ob weitere Verbindung vorhanden

								GlobalData.Instance.IncomingConnectionManager.RemoveConnection(conn);
								//_logger.Debug(TAG, nameof(Listener), $"connection removed {conn.ConnectionName}");
								conn.Dispose();
							}
							finally
							{
								TaskManager.Instance.RemoveTask(Task.CurrentId);
							}
						});
					}
					catch (Exception ex)
					{
						_messageDispatcher.Dispatch(idNumber, $"Minitelex Listener port={listenerItem.Port} ex={ex.Message}");
						if (ex.HResult == -2147467259)
						{
							_logger.Error(TAG, nameof(Listener), $"ex={ex.Message}");
						}
						else
						{
							_logger.Error(TAG, nameof(Listener), "", ex);
						}
					}
				}
			}
			//);
		}


		public MinitelexConnection CreateConnection(TcpClient client, int connectionId, string logPath, LogTypes logLevel)
		{
			return new MinitelexConnection(client, connectionId, logPath, logLevel);
		}

		public int GetNextConnectionId()
		{
			return GlobalData.Instance.IncomingConnectionManager.GetNextConnectionId();
		}

		/*
		public virtual void Shutdown()
		{
			_shutDown = true;

			//Message?.Invoke($"shutdown for maintenance");
			_messageDispatcher.Dispatch("Shutdown for maintenance");
			_logger.Notice(TAG, nameof(Shutdown), $"Shutdown for maintenance, {_connections.Count} active");
			//string timeStr = ItelexConstants.PRINT_TIME ? $"({DateTime.Now:HH:mm})" : "";
			foreach (MinitelexConnection conn in _connections)
			{
				conn.SendAscii($"\r\nshut down for maintenance\r\n\n");
			}
			foreach (MinitelexConnection conn in _connections)
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
		*/
	}

	public class TcpListenerItem
	{
		public TcpListener Listener { get; set; }

		public int Port { get; set; }

		public Task ListenerTask { get; set; }

		public bool Ready { get; set; }
	}

	public class TcpListenerItem2
	{
		//public TcpListener Listener { get; set; }

		public int Port { get; set; }

		//public Task ListenerTask { get; set; }

		public MinitelexUserItem User { get; set; }

		public bool Stopped { get; set; }

		//public bool Ready { get; set; }
	}
}
