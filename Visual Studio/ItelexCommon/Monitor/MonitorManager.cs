using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Windows.Markup;
using ItelexCommon.Utility;

namespace ItelexCommon.Monitor
{
	public enum MonitorServerTypes
	{
		ItelexAuskunft = 1,
		ItelexBaudotArtServer = 2,
		ItelexBildlocher = 3,
		ItelexChatGptServer = 4,
		ItelexChatServer = 5,
		ItelexMsgServer = 6,
		ItelexNewsServer = 7,
		ItelexRundsender = 8,
		ItelexWeatherServer = 9
	}

	public enum MonitorServerStatus
	{
		Idle = 1,
		Active = 2,
		WatchdogTimeout = 10,
		ConnectionTimeout = 11,
	}

	public class MonitorManager
	{
		private const string TAG = nameof(MonitorManager);

		public const int WATCHDOG_TIME = 10; // 10 minutes

#if DEBUG
		private const string SERVERIP = "192.168.0.1";
#else
		private const string SERVERIP = "192.168.0.21";
#endif


		private Logging _logger { get; set; }

		#region singleton pattern

		private static MonitorManager instance;

		public static MonitorManager Instance => instance ?? (instance = new MonitorManager());

		private MonitorManager()
		{
		}

		#endregion singleton pattern

		public const string PRGMS_PATH = @"d:\itelex";

		private readonly List<MonitorServerData> _serverList = new List<MonitorServerData>()
		{
			new MonitorServerData(MonitorServerTypes.ItelexAuskunft, SERVERIP, 9137, 
					@"ItelexAuskunft\ItelexAuskunft.exe"),
			new MonitorServerData(MonitorServerTypes.ItelexBildlocher, SERVERIP, 9144, 
					@"ItelexBildlocher\ItelexBildlocher.exe"),
			new MonitorServerData(MonitorServerTypes.ItelexBaudotArtServer, SERVERIP, 9139, 
					@"ItelexBaudotArt\ItelexBaudotArtServer.exe"),
			new MonitorServerData(MonitorServerTypes.ItelexChatGptServer, SERVERIP, 9145,
					@"ItelexChatGpt\ItelexChatGptServer.exe"),
			new MonitorServerData(MonitorServerTypes.ItelexChatServer, SERVERIP, 9136, 
					@"ItelexChat\ItelexChatServer.exe"),
			new MonitorServerData(MonitorServerTypes.ItelexMsgServer, SERVERIP, 9142,
					@"ItelexMsg\ItelexMsgServer.exe"),
			new MonitorServerData(MonitorServerTypes.ItelexNewsServer, SERVERIP, 9141,
					@"ItelexNews\ItelexNewsServer.exe"),
			new MonitorServerData(MonitorServerTypes.ItelexRundsender, SERVERIP, 9140, 
					@"ItelexRundsender\ItelexRundsender.exe"),
			new MonitorServerData(MonitorServerTypes.ItelexWeatherServer, SERVERIP, 9143, 
					@"ItelexWeather\ItelexWeatherServer.exe"),
			// new MonitorServer(MonitorServerTypes.ItelexBildlocher, "dg1", 9144),
		};

		public delegate void ActionEventHandler(MonitorCmds cmd);
		public event ActionEventHandler Action;

		private TcpListener _tcpListener;

		private bool _shutDown;

		public MonitorServerTypes PrgmType { get; private set; }

		public string PrgmVersion { get; private set; }

		public int ItelexNumber { get; private set; }

		public int ItelexLocalPort { get; private set; }

		public int ItelexPublicPort { get; private set; }

		public DateTime StartupTime { get; private set; }

		private MonitorServerStatus _status;
		public MonitorServerStatus Status
		{
			get
			{
				if (!IsWatchdogOk()) return MonitorServerStatus.WatchdogTimeout;
				return _status;
			}
		}

		private DateTime? _lastLoginTime;
		public DateTime? LastLoginTime
		{
			get
			{
				return _lastLoginTime;
			}
			set
			{
				_lastLoginTime = value;
			}
		}

		private string _lastUser;
		public string LastUser
		{
			get
			{
				return _lastUser;
			}
			set
			{
				_lastUser = value;
			}
		}

		private int _loginUserCount;
		public int LoginUserCount
		{
			get
			{
				return _loginUserCount;
			}
			set
			{
				_loginUserCount = value;
			}
		}

		private int _loginCount;
		public int LoginCount
		{
			get
			{
				return _loginCount;
			}
			set
			{
				_loginCount = value;
			}
		}


		public List<MonitorServerData> GetServerList => _serverList;

		private DateTime _lastWatchdogTrigger;

		private string _exePath;

		public void Start(Logging logger)
		{
			_logger = logger;
		}

		public void Start(Logging logger, int port, MonitorServerTypes prgmType, string prgmVersion, string exePath,
				int itelexNumber, int itelexLocalPort, int itelexPublicPort)
		{
			_logger = logger;
			PrgmType = prgmType;
			PrgmVersion = prgmVersion;
			StartupTime = DateTime.Now;
			LastLoginTime = null;
			LastUser = null;
			LoginCount = 0;
			ItelexNumber = itelexNumber;
			ItelexLocalPort = itelexLocalPort;
			ItelexPublicPort = itelexPublicPort;
			_lastWatchdogTrigger = DateTime.Now;
			_status = MonitorServerStatus.Idle;
			_exePath = exePath;

			LoadCounter();

			_tcpListener = new TcpListener(IPAddress.Any, port);
			_tcpListener.Start();

			_shutDown = false;
			Task _listenerTask = Task.Run(() => Listener());
		}

		public void StartPrgm(MonitorServerTypes type)
		{
			MonitorServerData server = (from s in _serverList where s.PrgmType == type select s).FirstOrDefault();
			if (server == null) return;

			string fullName = Path.Combine(PRGMS_PATH, server.Path);
			string workingDir = Path.GetDirectoryName(fullName);
			_logger.Debug(TAG, nameof(StartPrgm), $"{fullName} {workingDir}");

			try
			{
				Process p = new Process();
				p.StartInfo.FileName = fullName;
				p.StartInfo.WorkingDirectory = workingDir;
				p.Start();
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(StartPrgm), "error", ex);
			}
		}

		public void ShutDownPrgm()
		{
			_shutDown = true;
			_tcpListener?.Stop();
		}

		private void Listener()
		{
			TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(Listener));
			while (!_shutDown)
			{
				try
				{
					//_logger.Info(TAG, nameof(Listener), "Waiting for monitor connection");

					// wait for connection
					TcpClient client = _tcpListener.AcceptTcpClient();
					IPEndPoint endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
					string remoteAddr = $"{endPoint.Address}:{endPoint.Port}";
					//_logger.Info(TAG, nameof(Listener), $"Incoming monitor connection, remote-addr={remoteAddr}");

					Task.Run(() =>
					{
						TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(Listener), "new conn");
						try
						{
							MonitorServerConnection conn = new MonitorServerConnection(client);
							conn.Start();
							client.Close();
							//_logger.Debug(TAG, nameof(Listener), "close monitor connection");
						}
						finally
						{
							TaskManager.Instance.RemoveTask(Task.CurrentId);
						}
					});
				}
				catch (Exception ex)
				{
					if (ex.HResult != -2147467259)
					{
						//_logger.Error(TAG, nameof(Listener), "", ex);
					}
				}
				finally
				{
					TaskManager.Instance.RemoveTask(Task.CurrentId);
				}
			}
		}

		public string ServerStatusToString(MonitorServerStatus status)
		{
			switch(status)
			{
				case MonitorServerStatus.Idle:
					return "Idle";
				case MonitorServerStatus.Active:
					return "Active";
				case MonitorServerStatus.ConnectionTimeout:
					return "No connection";
				case MonitorServerStatus.WatchdogTimeout:
					return "Watchdog error";
				default:
					return "?";
			}
		}

		public void ReceivedAction(MonitorCmds cmd)
		{
			Action?.Invoke(cmd);
		}

		/*
		public void SetActive(string userName)
		{
			_status = MonitorServerStatus.Active;
			LastLoginTime = DateTime.Now;
			LastUser = userName;
		}
		*/

		public void AddLogin(string userName)
		{
			LoginCount++;
			if (!string.IsNullOrEmpty(userName) && userName != "-")
			{
				LoginUserCount++;
				LastUser = userName;
				LastLoginTime = DateTime.Now;
			}
			_status = MonitorServerStatus.Active;
			SaveCounter();
		}

		public void SetUserName(string userName)
		{
			if (!string.IsNullOrEmpty(userName) && userName != "-")
			{
				LastUser = userName;
			}
			_status = MonitorServerStatus.Active;
			SaveCounter();
		}


		public void SetInactive()
		{
			_status = MonitorServerStatus.Idle;
		}

		public void TriggerWatchdog()
		{
			//Debug.WriteLine($"wd {DateTime.Now}");
			_lastWatchdogTrigger = DateTime.Now;
		}

		public bool IsWatchdogOk()
		{
			return (DateTime.Now - _lastWatchdogTrigger).TotalMinutes < WATCHDOG_TIME * 2;
		}

		public void LoadCounter()
		{
			try
			{
				string fullName = Path.Combine(_exePath, GetCounterFilename());
				if (File.Exists(fullName))
				{
					string jsonStr = File.ReadAllText(fullName);
					CounterData data = JsonConvert.DeserializeObject<CounterData>(jsonStr);
					if (data != null)
					{
						_loginCount = data.LoginCount;
						_loginUserCount = data.LoginUserCount;
						_lastLoginTime = data.LastLoginTime;
						_lastUser = data.LastUser;
						return;
					}
				}
				_loginCount = 0;
				_lastLoginTime = null;
				_lastUser = null;
			}
			catch(Exception)
			{
			}
		}

		public void SaveCounter()
		{
			try
			{
				CounterData data = new CounterData()
				{
					LoginCount = _loginCount,
					LoginUserCount = _loginUserCount,
					LastUser = _lastUser,
					LastLoginTime = _lastLoginTime,
				};

				string fullName = Path.Combine(_exePath, GetCounterFilename());
				string jsonStr = JsonConvert.SerializeObject(data);
				File.WriteAllText(fullName, jsonStr);
			}
			catch (Exception)
			{
			}
		}


		private string GetCounterFilename()
		{
			return $"counter_{DateTime.Now:yyMM}.dat";
		}

	}
}
