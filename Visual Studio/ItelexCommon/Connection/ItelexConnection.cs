using ItelexCommon.Logger;
using ItelexCommon.Utility;
using MimeKit.Cryptography;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Timers;

namespace ItelexCommon.Connection
{
	/// <summary>
	/// Implements the i-telex and ascii protocol
	/// </summary>
	public class ItelexConnection : IDisposable
	{
		private const string TAG = nameof(ItelexConnection);

		private const int VERSION_NUMBER = 1;

		private const int INPUT_TIMEOUT = 120; // min

		public ItelexLogger _itelexLogger;

		public enum ItelexCommands
		{
			Heartbeat = 0x00,
			DirectDial = 0x01,
			BaudotData = 0x02,
			End = 0x03,
			Reject = 0x04,
			Ack = 0x06,
			ProtocolVersion = 0x07,
			SelfTest = 0x08,
			RemoteConfig = 0x09,
			ConnectRemote = 0x81,
			RemoteConfirm = 0x82,
			RemoteCall = 0x83,
			AcceptCallRemote = 0x84,
		}

		public enum ConnectionStates
		{
			Disconnected,
			TcpConnected, // after TCP connect and before first data (texting mode unknown)
			Connected, // after direct dial cmd was received or direct dial = 0
			AsciiTexting, // ascii texting
			ItelexTexting, // baudot texting
			ItelexDisconnected // waiting for reconnect
		}

		public enum ConnectionDirections
		{
			In,
			Out,
			Remote, // remote server
		}

		public enum DisconnectReasons
		{
			Logoff,
			EndCmdReceived,
			Reject,
			LoginError,
			ServiceShutdown,
			AckTimeout,
			SendReceiveTimeout,
			SendCmdError,
			NotConnected,
			Error,
			TcpDisconnectByRemote,
			TcpStartReceiveError,
			TcpDataReceivedError,
			Dispose,
			MultipleLogin,
			SendPin,
			None
		}

		protected string[] okResultStrings = new string[] { "j", "ja", "y", "yes", "ok", "." };

		public delegate void ItelexConnectEventHandler(ItelexConnection connection);
		public event ItelexConnectEventHandler ItelexConnected;

		public delegate void ItelexDroppedEventHandler(ItelexConnection connection);
		public event ItelexDroppedEventHandler ItelexDropped;

		public delegate void ItelexReceivedEventHandler(ItelexConnection connection, string asciiText);
		public event ItelexReceivedEventHandler ItelexReceived;

		public delegate void ItelexUpdateEventHandler(ItelexConnection connection);
		public event ItelexUpdateEventHandler ItelexUpdate;

		public delegate void ItelexSendEventHandler(ItelexConnection connection, string asciiText);
		public event ItelexSendEventHandler ItelexSend;

		public delegate void BaudotSendRecvEventHandler(ItelexConnection connection, byte[] code);
		public event BaudotSendRecvEventHandler BaudotSendRecv;

		private const int RECV_BUFFERSIZE = 2048 + 2;

		private bool _disconnectActive = false;

		private readonly object _sendCmdLock = new object();

		protected TcpClient _client;

		private object _clientReceiveBufferLock = new object();
		private Queue<byte> _clientReceiveBuffer;

		public int ConnectionId { get; private set; }

		public TickTimer _connectionTimer;

		public string Comment { get; private set; }

		public string ConnectionNameWithTime
		{
			get
			{
				return $"[{ConnectinStartTime:dd.MM. HH:mm}] {ConnectionName}";
			}
		}

		public string ConnectionName
		{
			get
			{
				string stateStr;
				switch (ConnectionState)
				{
					case ConnectionStates.Connected:
						stateStr = "C";
						break;
					case ConnectionStates.Disconnected:
					case ConnectionStates.ItelexDisconnected:
						stateStr = "D";
						break;
					case ConnectionStates.TcpConnected:
						stateStr = "T";
						break;
					case ConnectionStates.ItelexTexting:
						stateStr = "I";
						break;
					case ConnectionStates.AsciiTexting:
						stateStr = "A";
						break;
					default:
						stateStr = "?";
						break;
				}

				string name = $"{ConnectionId} {LocalPort} ";

				if (ConnectionRetryCnt.HasValue)
				{
					name += $"(#{ConnectionRetryCnt}) ";
				}

				name += $"{stateStr}:";

				if (RemoteNumber.HasValue)
				{
					name += " " + RemoteNumber.ToString();
				}
				else
				{
					name += " " + IpAddress;
				}

				if (!string.IsNullOrWhiteSpace(RemoteAnswerbackStr))
				{
					name += $" '{RemoteAnswerbackStr}'";
				}

				if (!string.IsNullOrWhiteSpace(ConnectionShortName))
				{
					name += $" ({ConnectionShortName})";
				}

				if (!string.IsNullOrEmpty(Comment)) name += " - " + Comment;

				if (CallStatus == CallStatusEnum.NoConn)
				{
					name += " (noconn)";
				}
				else if (CallStatus == CallStatusEnum.Reject)
				{
					name += $" ({RejectReason})";
				}

				return name;
			}
		}

		public string ConnectionShortName { get; set; }

		/// <summary>
		/// Retries staring from 1, null = no retryno available
		/// </summary>
		public int? ConnectionRetryCnt { get; set; }

		public DateTime ConnectinStartTime { get; private set; }

		//protected Language SessionLanguage { get; set; }

		/// <summary>
		/// Itelex number of the calling or called station
		/// </summary>
		public int? RemoteNumber { get; protected set; }

		public string RemoteHost { get; protected set; }

		public int RemotePort { get; protected set; }

		public int RemoteExtensionNo { get; protected set; }

		public int? RemoteItelexVersion { get; protected set; }

		public string RemoteItelexVersionStr { get; protected set; }

		public string OurItelexVersionStr { get; private protected set; }

		public Language ConnectionLanguage { get; protected set; }

		public Answerback RemoteAnswerback { get; protected set; }

		public string RemoteAnswerbackStr
		{
			get
			{
				if (RemoteAnswerback == null) return "";
				return RemoteAnswerback.ToString();
			}
		}

		public string RemoteClientAddrStr { get; set; }

		private List<string> _dataLog;
		private ShiftStates _dataLogShiftState;

		public bool _clientReceiveTimerActive { get; set; }
		private System.Timers.Timer _clientReceiveTimer;

		public bool BuRefreshActive { get; set; }
		private System.Timers.Timer _buTimer;
		private bool _buTimerActive;

		//private System.Timers.Timer _reconnectTimeoutTimer;

		private System.Timers.Timer _sendTimer;
		private bool _sendTimerActive;

		private System.Timers.Timer _ackTimer;
		private bool _ackTimerActive;

		//private bool _ackRecvFlag;
		private TickTimer _lastAckReceived;
		private readonly object _sendLock = new object();

		private TickTimer _lastSentMs;

		// conditional new line
		private bool _condLastCr = false;
		private bool _condLastLf = false;

		public TickTimer LastSendRecvTime { get; private set; }

		public bool DataReceivedFlag { get; set; }

		private int _inputMaxLength;
		protected string _inputBuffer;
		private bool _inputActive;
		private bool _inputDefaultActive;
		private bool _inputAllowCorr;
		private bool _inputAllowSecure;
		private bool _inputAllowHelp;
		private bool _inputMultiline;
		private string _inputEndChar2;
		private bool _inputKgActive;
		protected bool _inputGegenschreiben;
		private TickTimer _inputTimer = new TickTimer(false);

		private DateTime? _connStartTime = null;
		public int ConnTimeMin
		{
			get
			{
				if (_connStartTime == null || !IsConnected)
				{
					return 0;
				}
				else
				{
					return (int)(DateTime.Now.Subtract(_connStartTime.Value).Ticks / (10000000 * 60));
				}
			}
		}

		protected ShiftStates _shiftState;

		protected Acknowledge _ack;

		private ConcurrentQueue<char> _sendBuffer;
		protected int _sendBufferCount => _sendBuffer.Count;

		/// <summary>
		/// Ack timeout in seconds
		/// </summary>
		public int AckTimeout
		{
			get
			{
				int timeout = _lastAckReceived.ElapsedSeconds;
				return timeout > 60 ? timeout : 0;
			}
		}

		public int? ExtensionNumber { get; set; }

		public string IpAddress
		{
			get
			{
				if (_client?.Client == null) return"";

				try
				{
					IPEndPoint endPoint = (IPEndPoint)_client.Client.RemoteEndPoint;
					return endPoint.Address.ToString();
				}
				catch (Exception)
				{
					return "0.0.0.0";
				}
			}
		}

		public int LocalPort
		{
			get
			{
				if (_client?.Client == null) return 0;

				try
				{
					IPEndPoint endPoint = (IPEndPoint)_client.Client.LocalEndPoint;
					return endPoint.Port;
				}
				catch
				{
					return 0;
				}
			}
		}


		public bool Local { get; set; } = true;

		public bool IsConnected => ConnectionState != ConnectionStates.Disconnected && ConnectionState != ConnectionStates.ItelexDisconnected;

		protected readonly ConnectionDirections _connectionDirection;

		public bool ConnectionStarted { get; set; }

		public CallStatusEnum CallStatus { get; set; }
		public string RejectReason { get; set; }

		public string TextingState
		{
			get
			{
				if (ConnectionState == ConnectionStates.ItelexTexting)
				{
					return "Itelex";
				}
				else if (ConnectionState == ConnectionStates.AsciiTexting)
				{
					return "Ascii";
				}
				else
				{
					return "???";
				}
			}
		}

		public bool RecvOn { get; set; }

		private ConnectionStates _connectionState;
		public ConnectionStates ConnectionState
		{
			get { return _connectionState; }
			set { _connectionState = value; }
		}

		public DisconnectReasons DisconnectReason { get; set; }

		public string ConnectionStateString
		{
			get
			{
				switch (ConnectionState)
				{
					case ConnectionStates.Disconnected:
						return "Disconnected";
					case ConnectionStates.TcpConnected:
						return "TCP connection";
					case ConnectionStates.Connected:
						return $"Connected";
					case ConnectionStates.AsciiTexting:
						return "Ascii";
					case ConnectionStates.ItelexTexting:
						return "i-Telex";
					case ConnectionStates.ItelexDisconnected:
						return "Pause";
					default:
						return "???";
				}
			}
		}

		public ItelexConnection()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public ItelexConnection(ConnectionDirections direction, TcpClient client, int connectionId, int? number, string comment,
			ItelexLogger itelexLogger)
		{
			_connectionDirection = direction;

			Comment = comment;

			// set number very early for debugging and ConnectionName
			RemoteNumber = number;

			//_logger = LogManager.Instance.Logger;
			ConnectionId = connectionId;

			_itelexLogger = itelexLogger;
			//_itelexLogger = new ItelexLogger(logPath, ConnectionId, direction, number, logLevel);
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ItelexConnection), $"id={ConnectionId} number={number}");

			_clientReceiveBuffer = new Queue<byte>();
			_clientReceiveTimerActive = false;
			_clientReceiveTimer = new System.Timers.Timer(50);
			_clientReceiveTimer.Elapsed += ClientReceiveTimer_Elapsed;
			_clientReceiveTimer.Start();

			_connectionTimer = new TickTimer();

			bool startPhase = direction == ConnectionDirections.Out;
			_ack = new Acknowledge(connectionId, startPhase, _itelexLogger);
			_itelexLogger.SetAck(_ack);
			_ackTimerActive = true; // do not send ack

			RemoteAnswerback = null;

			ConnectinStartTime = DateTime.Now;

			try
			{
				_client = client;

				if (client != null)
				{
					IPEndPoint endPoint = (IPEndPoint)_client.Client.RemoteEndPoint;
					RemoteClientAddrStr = $"{endPoint.Address}:{endPoint.Port}";
					RemotePort = endPoint.Port;
				}

				_dataLog = new List<string>();
				_dataLogShiftState = ShiftStates.Unknown;

				InitTimerAndHandler();
				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ItelexConnection), $"id={ConnectionId} end");
			}
			catch (Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(ItelexConnection), $"error", ex);
			}
		}

		~ItelexConnection()
		{
			//LogManager.Instance.Logger.Debug(TAG, "~ItelexConnection", "destructor");
			Dispose(false);
		}

		#region Dispose

		// Flag: Has Dispose already been called?
		private bool _disposed = false;

		// Public implementation of Dispose pattern callable by consumers.
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Protected implementation of Dispose pattern.
		protected virtual void Dispose(bool disposing)
		{
			//LogManager.Instance.Logger.Debug(TAG, nameof(Dispose), $"_disposed={_disposed} disposing={disposing}");

			if (_disposed) return;

			//_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Dispose), $"disposing={disposing} start");
			if (disposing)
			{
				// Free any other managed objects here.
				//LogManager.Instance.Logger.Debug(TAG, nameof(Dispose), $"disposed timers");

				if (_clientReceiveTimer != null)
				{
					_clientReceiveTimer.Stop();
					_clientReceiveTimer.Elapsed -= ClientReceiveTimer_Elapsed;
				}

				if (_sendTimer != null)
				{
					_sendTimer.Stop();
					_sendTimer.Elapsed -= SendTimer_Elapsed;
				}

				if (_ackTimer != null)
				{
					_ackTimer.Stop();
					_ackTimer.Elapsed -= AckTimer_Elapsed;
				}

				if (_buTimer != null)
				{
					_buTimer.Stop();
					_buTimer.Elapsed -= BuTimer_Elapsed;
				}

				//if (_reconnectTimeoutTimer != null)
				//{
				//	_reconnectTimeoutTimer.Stop();
				//	_reconnectTimeoutTimer.Elapsed -= ReconnectTimeoutTimer_Elapsed;
				//}

				//_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Dispose), $"disposing={disposing} ok");
			}
			_itelexLogger?.End();

			_disposed = true;
		}

		#endregion Dispose

		/// <summary>
		/// Init timer and handler that have to be disposed manually
		/// </summary>
		public virtual void InitTimerAndHandler()
		{
			_sendTimer = new System.Timers.Timer(ItelexConstants.SEND_TIMER_INTERVAL);
			_sendTimer.Elapsed += SendTimer_Elapsed;

			_ackTimer = new System.Timers.Timer(2000);
			_ackTimer.Elapsed += AckTimer_Elapsed;

			_buTimerActive = false;
			_buTimer = new System.Timers.Timer(ItelexConstants.BU_REFRESH_SEC * 1000);
			_buTimer.Elapsed += BuTimer_Elapsed;

			//_reconnectTimeoutTimer = new System.Timers.Timer(30 * 1000);
			//_reconnectTimeoutTimer.Elapsed += ReconnectTimeoutTimer_Elapsed;
		}

		protected void InvokeItelexUpdate()
		{
			ItelexUpdate?.Invoke(this);
		}

		public void ConnectIn(ItelexIncomingConfiguration config)
		{
			try
			{
				OurItelexVersionStr = config.OurItelexVersionStr;

				IPAddress remoteAddr = ((IPEndPoint)_client.Client.RemoteEndPoint).Address;
				//_logger.Info(TAG, nameof(ConnectIn), $"incoming connection from {remoteAddr}");
				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ConnectIn), $"id={ConnectionId} from {remoteAddr}");

				ConnectionState = ConnectionStates.TcpConnected;
				ConnectInit();

				//if (ConnectionState == ConnectionStates.Disconnected) return;

				// wait for version and extension
				TickTimer wait = new TickTimer(true);
				while (!wait.IsElapsedMilliseconds(5000))
				{
					Thread.Sleep(100);
					if (!IsConnected) return;
					if (RemoteItelexVersion.HasValue && ExtensionNumber.HasValue) break;
				}
				_ackTimerActive = false;
				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ConnectIn),
						$"RemoteItelexVersion={RemoteItelexVersion} ExtensionNumber={ExtensionNumber}");
			}
			catch(Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(ConnectIn), $"error", ex);
			}
		}

		public bool ConnectOut(ItelexOutgoingConfiguration config, bool waitForSilence = true)
		{
			OurItelexVersionStr = config.OurItelexVersionStr;

			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ConnectOut),
					$"start outgoing connection to number={RemoteNumber} {RemoteHost}:{RemotePort} ext={RemoteExtensionNo} ascii={config.AsciiMode}");

			try
			{
				_client = new TcpClient(RemoteHost, RemotePort);
				if (_client == null || !_client.Connected)
				{
					//Log(LogTypes.Info, nameof(ConnectOut), $"outgoing connection {host}:{port} failed");
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ConnectOut),
							$"outgoing connection {RemoteHost}:{RemotePort} failed");
					CallStatus = CallStatusEnum.NoConn;
					return false;
				}

				IPEndPoint endPoint = (IPEndPoint)_client.Client.RemoteEndPoint;
				RemoteClientAddrStr = $"{endPoint.Address}:{endPoint.Port}";
			}
			catch (Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ConnectOut), "error", ex);
				CallStatus = CallStatusEnum.NoConn;
				return false;
			}

			ConnectionState = ConnectionStates.TcpConnected;

			try
			{
				//StartReceive();
				ConnectInit();
			}
			catch(Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ConnectOut), "error in ConnectInit()", ex);
			}

			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ConnectOut), $"outgoing connection extablished");

			// wait 2 seconds for ascii code (ascii texting)

			TickTimer timer = new TickTimer();
			while (timer.ElapsedMilliseconds < 2000)
			{
				if (ConnectionState != ConnectionStates.TcpConnected) break;
				Thread.Sleep(100);
			}

			if (ConnectionState == ConnectionStates.Disconnected) return false;
			if (ConnectionState == ConnectionStates.AsciiTexting) return true;

			//SendHeartbeatCmd();
			SendVersionCodeCmd(null, OurItelexVersionStr);

			// wait 5 seconds for version cmd from remote
			timer.Start();
			while (!timer.IsElapsedMilliseconds(5000))
			{
				if (RemoteItelexVersion != null)
				{
					break;
				}
				if (ConnectionState == ConnectionStates.Disconnected) return false;
				Thread.Sleep(100);
			}
			//_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ConnectOut),
			//	$"after version wait ConnectionState={ConnectionState} RemoteVersion={RemoteItelexVersion} ElapsedMilliseconds={timer.ElapsedMilliseconds}ms");

			if (ConnectionState == ConnectionStates.TcpConnected)
			{
				ConnectionState = ConnectionStates.AsciiTexting;
				return true;
			}
			ConnectionState = ConnectionStates.ItelexTexting;

			// notwendig für piTelex
			Thread.Sleep(500);

			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ConnectOut), $"SendDirectDialCmd {RemoteExtensionNo}");
			SendDirectDialCmd(RemoteExtensionNo);

			// Start sending ack, after sending the SendDirectDialCmd!
			_ackTimerActive = false;

			//_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ConnectOut), "WaitForSilence after SendDirectDialCmd");
			//WaitForSilence(4000, 3000, 10000);
			if (waitForSilence)
			{
				WaitForSilence(2000, 2000, 10000);
			}
			WaitAllSendBuffersEmpty(true);

			//Thread.Sleep(2000);

			if (!IsConnected) return (false);

			_ack.ResyncSend();
			_ack.ClearStartPhase();
			//_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ConnectOut), "force ack resync");
			//_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ConnectOut), "return true");
			return true;
		}

		private void ConnectInit()
		{
			_ack.Reset();
			_sendBuffer = new ConcurrentQueue<char>();

			_ackTimer.Stop();
			_ackTimer.Start();

			_sendTimerActive = false;
			_sendTimer.Stop();
			_sendTimer.Start();

			_shiftState = ShiftStates.Unknown;

			Local = false;
			CallStatus = CallStatusEnum.None;

			_connStartTime = DateTime.Now;

			_itelexSendCount = 0;
			_lastSentMs = new TickTimer();
			_lastAckReceived = new TickTimer(false);
			LastSendRecvTime = new TickTimer(false);

			ConnectionState = ConnectionStates.TcpConnected;
			ConnectionStarted = false;
			ExtensionNumber = null;

			RemoteItelexVersion = null;
			RemoteItelexVersionStr = null;

			BuRefreshActive = false;

			_inputActive = false;
			_inputKgActive = false;
			_inputTimer.Stop();

			ItelexUpdate?.Invoke(this);
			ItelexConnected?.Invoke(this);
		}

		public void AckReset()
		{
			_ack.Reset();
		}

		public void Disconnect(DisconnectReasons reason, string reasonStr = "")
		{
			if (reasonStr != "") reasonStr = " " + reasonStr;
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Disconnect), $"Disconnect reason={reason}{reasonStr}");

			if (_disconnectActive)
			{
				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Disconnect), $"Disconnect already active");
				return;
			}
			_disconnectActive = true;

			try
			{
				if (ConnectionState == ConnectionStates.Disconnected)
				{
					_itelexLogger.ItelexLog(LogTypes.Warn, TAG, nameof(Disconnect), $"connection already disconnected");
					return;
				}

				try
				{
					_clientReceiveTimer?.Stop();
					_ackTimer?.Stop();
					_sendTimer?.Stop();
					_buTimer?.Stop();
					//_reconnectTimeoutTimer?.Stop();
				}
				catch (Exception ex)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Disconnect), $"stop timers", ex);
				}

				ConnectionState = ConnectionStates.Disconnected;
				Local = false;
				ClearSendBuffer();

				if (_client.Client != null)
				{
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Disconnect), "close TcpClient");
					//try
					//{
					//	_client.Client.Shutdown(SocketShutdown.Both);
					//}
					//catch (Exception ex)
					//{
					//	_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Disconnect), $"_client.Client.Shutdown", ex);
					//}
					//try
					//{
					//	_client.Client.Close();
					//}
					//catch (Exception ex)
					//{
					//	_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Disconnect), $"_client.Client.Close", ex);
					//}
					try
					{
						NetworkStream stream = _client.GetStream();
						stream.Close();
					}
					catch (Exception ex)
					{
						_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Disconnect), $"stream.Close", ex);
					}
					try
					{
						_client.Close();
					}
					catch(Exception ex)
					{
						_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(Disconnect), $"_client.Close", ex);
					}
				}

				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(Disconnect), $"connection dropped");

				Thread.Sleep(1000);

				ItelexDropped?.Invoke(this);
				ItelexUpdate?.Invoke(this);
			}
			finally
			{
				_disconnectActive = false;
			}
		}

		private void ClearSendBuffer()
		{
			_itelexSendCount = 0;
			while (_sendBuffer.TryDequeue(out _))
			{
			}
		}

		//private void ReconnectTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
		//{
		//}

		private void SendTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (!IsConnected || _ackTimerActive || _sendTimerActive) return;
			_sendTimerActive = true;

			try
			{
				try
				{
					if (_client == null || !_client.Connected)
					{
						_sendTimerActive = false;
						return;
					}
				}
				catch (Exception ex)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(SendTimer_Elapsed), "_client invalid", ex);
					return;
				}


				if (_lastAckReceived.IsElapsedSeconds(60))
				{
					// receive ack timeout
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(SendTimer_Elapsed), $"recv-ack timeout state={ConnectionState} (60s)");
					Disconnect(DisconnectReasons.AckTimeout);
					return;
				}

				try
				{
					SendTimer();
				}
				catch (Exception ex)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(SendTimer_Elapsed), "", ex);
				}
			}
			finally
			{
				_sendTimerActive = false;
			}
		}

		private readonly byte[] _itelexSendBuffer = new byte[ItelexConstants.ITELIX_SENDBUFFER_SIZE];
		private int _itelexSendCount;

		private void SendTimer()
		{
			lock (_sendLock)
			{
				//Debug.WriteLine($"_sendBuffer.Count={_sendBuffer.Count} itelexSendCount={_itelexSendCount} " +
				//	$"_ackCount={_ack.RemoteBufferCount} {_ack.RemoteBufferCount < ItelexConstants.ITELIX_ACKBUFFER_SIZE - 1}");
				if (_sendBuffer.Count > 0)
				{
					int ackCount = _ack.RemoteBufferCount;
					for (int i = 0; !_sendBuffer.IsEmpty && _itelexSendCount < ItelexConstants.ITELIX_SENDBUFFER_SIZE - 1
						&& ackCount < ItelexConstants.ITELIX_ACKBUFFER_SIZE - 1; i++)
					{
						if (!_sendBuffer.TryDequeue(out char asciiChr))
						{
							break;
						}

						byte[] data;
						if (asciiChr < 128)
						{
							data = CodeManager.AsciiCharToBaudotWithShift(asciiChr, ref _shiftState, CodeSets.ITA2);
						}
						else
						{
							data = new byte[] { (byte)(asciiChr & 127) };
						}

						for (int c = 0; c < data.Length; c++)
						{
							_itelexSendBuffer[_itelexSendCount++] = data[c];
							ackCount++;
						}
					}
				}
			}

			// if character count in itelex send buffer is < ITELIX_SENDBUFFER_SIZE wait for
			// WAIT_BEFORE_SEND_MSEC before sending them.

			if (_itelexSendCount == 0) return;

			if (_itelexSendCount < ItelexConstants.ITELIX_SENDBUFFER_SIZE &&
					!_lastSentMs.IsElapsedMilliseconds(ItelexConstants.WAIT_BEFORE_SEND_MSEC))
			{
				return;
			}

			byte[] baudotData = new byte[_itelexSendCount];
			Buffer.BlockCopy(_itelexSendBuffer, 0, baudotData, 0, _itelexSendCount);
			SendCmd(ItelexCommands.BaudotData, baudotData);
			//Debug.WriteLine($"SendCmd {_itelexSendCount}");
			LastSendRecvTime.Start();
			_itelexSendCount = 0;
			_lastSentMs.Start();
		}

		private void BuTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (!IsConnected || _buTimerActive) return;

			_buTimerActive = true;

			try
			{
				BuTimerReset();
				if (ConnectionState == ConnectionStates.ItelexTexting && BuRefreshActive)
				{
					_shiftState = ShiftStates.Ltrs;
					byte[] data = new byte[] { CodeManager.BAU_LTRS };
					SendCmd(ItelexCommands.BaudotData, data);
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(BuTimer_Elapsed), $"send BuRefresh");
				}
			}
			finally
			{
				_buTimerActive = false;
			}
		}

		private void BuTimerReset()
		{
			_buTimer.Stop();
			_buTimer.Start();
		}

		private void AckTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (ConnectionState != ConnectionStates.ItelexTexting || _ackTimerActive) return;

			_ackTimerActive = true;

			try
			{
				// send ack
				SendAckCmd(_ack.RecvAckCnt);
			}
			catch (Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(AckTimer_Elapsed), "", ex);
			}
			finally
			{
				_ackTimerActive = false;
			}
		}

		/*
		private void StartReceive(bool first)
		{
			if (_client?.Client == null || (!first && ConnectionState == ConnectionStates.Disconnected)) return;
			//if (_client?.Client == null) return;

			byte[] buffer = new byte[RECV_BUFFERSIZE];
			try
			{
				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(StartReceive), $"start receive, avail= {_client.Available}");
				byte[] preBuffer = new byte[0];
				if (_client.Available > 0)
				{
					preBuffer = new byte[_client.Available];
					_client.Client.Receive(preBuffer, SocketFlags.None);
					List<string> dump = CommonHelper.DumpByteArrayStr(preBuffer, 0);
					foreach(string line in dump)
					{
						_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(StartReceive), line);
					}
				}
				//TcpClientState state = new TcpClientState(RECV_BUFFERSIZE, preBuffer);
				AddReceiveBuffer(preBuffer);

				_client.Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, DataReceived, buffer);
			}
			catch (Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(StartReceive), "error", ex);
				Disconnect(DisconnectReasons.TcpStartReceiveError);
			}
		}
		*/

		/// <summary>
		/// wait for silence
		/// </summary>
		/// <param name="minWaitMs">minimum wait time (ms)</param>
		/// <param name="waitSilenceMs">periode of silence to wait for (ms)</param>
		/// <param name="timeoutMs">timeout while waiting for silence (ms)</param>
		/// <returns></returns>
		public bool WaitForSilence(int minWaitMs, int waitSilenceMs, int timeoutMs, [CallerMemberName] string caller = null)
		{
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(WaitForSilence),
				$"start minWait={minWaitMs}ms waitSilence={waitSilenceMs}ms timeout={timeoutMs}ms (caller={caller})");

			if (!IsConnected) return false;

			Thread.Sleep(minWaitMs + waitSilenceMs);

			TickTimer waitTime = new TickTimer();
			while (true)
			{
				if (!IsConnected)
				{
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(WaitForSilence), $"no connected");
					return false;
				}

				if (waitTime.IsElapsedMilliseconds(timeoutMs))
				{
					// timeout
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(WaitForSilence), $"timeout {waitTime.ElapsedMilliseconds}ms");
					return false;
				}
				if (LastSendRecvTime.IsElapsedMilliseconds(waitSilenceMs))
				{
					// silence
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(WaitForSilence),
							$"silence detected after {waitTime.ElapsedMilliseconds}ms, LastSendRecvTime={LastSendRecvTime.ElapsedMilliseconds}ms");
					return true;
				}
				Thread.Sleep(100);
			}
		}

		public void WaitAllSendBuffersEmpty(bool checkRemoteBufferCount = true, string gegenschreiben = null,
				[CallerMemberName] string caller = null)
		{
			if (!IsConnected || _sendBuffer == null) return;

			//Thread.Sleep(500);
			Thread.Sleep(100); // neu

			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(WaitAllSendBuffersEmpty), 
				$"waitEmpty start RemoteBufferCount={_ack.RemoteBufferCount}, checkRemoteBufferCount={checkRemoteBufferCount} (caller={caller})");

			int lastSendCnt = _sendBuffer.Count;
			TickTimer lastSendTimer = new TickTimer();
			while (!_sendBuffer.IsEmpty || _itelexSendCount > 0 || checkRemoteBufferCount && _ack.RemoteBufferCount > 0)
			{
				if (!IsConnected) return;

				if (_sendBuffer.Count < lastSendCnt)
				{
					lastSendTimer.Start();
					lastSendCnt = _sendBufferCount;
					continue;
				}

				if (gegenschreiben != null && !_inputGegenschreiben && _inputActive &&  _inputBuffer != null
						&& _inputBuffer.Length > 0)
				{
					foreach(char c in gegenschreiben)
					{
						if (_inputBuffer.Contains(c))
						{
							while (!_sendBuffer.IsEmpty) { _sendBuffer.TryDequeue(out _ ); }
							_itelexSendCount = 0;
							_inputGegenschreiben = true;
							return;
						}
					}
				}

				Thread.Sleep(100);
			}
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(WaitAllSendBuffersEmpty),
					$"waitEmpty end {_sendBufferCount} {_itelexSendCount} {_ack.RemoteBufferCount}");
		}

		/// <summary>
		/// send character [asciiChr] [cnt] times
		/// </summary>
		/// <param name="asciiChr"></param>
		/// <param name="cnt"></param>
		public void SendAscii(char asciiChr, int cnt)
		{
			if (!IsConnected) return;

			string asciiStr = CodeManager.ChrToStr(asciiChr, cnt);
			SendAscii(asciiStr);
		}

		/// <summary>
		/// send character [asciiChr]
		/// </summary>
		/// <param name="asciiChr"></param>
		public void SendAscii(char asciiChr)
		{
			if (!IsConnected) return;

			string asciiStr = asciiChr.ToString();
			SendAscii(asciiStr);
		}

		/*
		public async Task SendBaudotAsync(byte[] baudotData)
		{
			string baudotStr = "";
			foreach (byte b in baudotData)
			{
				baudotStr += (char)(b + 128);
			}
			await SendAsciiAsync(baudotStr);
		}
		*/

		public void SendBaudot(byte[] baudotData)
		{
			string baudotStr = "";
			foreach(byte b in baudotData)
			{
				baudotStr += (char)(b + 128);
			}
			SendAscii(baudotStr);
		}

		public void SendAscii(string asciiStr)
		{
			if (!IsConnected) return;
			if (string.IsNullOrEmpty(asciiStr)) return;

			try
			{
				if (ConnectionState == ConnectionStates.AsciiTexting)
				{
					string replStr = CodeManager.AsciiStringReplacements(asciiStr, CodeSets.ITA2, true, true);
					replStr = CodeManager.CleanExAscii(replStr);
					byte[] data = Encoding.ASCII.GetBytes(replStr);
					_client.Client.BeginSend(data, 0, data.Length, SocketFlags.None, EndSend, null);
				}
				else
				{
					if (LastSendRecvTime.IsElapsedSeconds(25))
					{
						// send 4 LTRS as delay, in case the remote machine fell asleep
						for (int i = 0; i < 4; i++)
						{
							EnqueueAsciiCode(CodeManager.ASC_LTRS);
						}
					}

					string replStr = CodeManager.AsciiStringReplacements(asciiStr, CodeSets.ITA2, false, true);
					ItelexSend?.Invoke(this, replStr);

					replStr = ProcessCondNewLine(replStr);
					for (int c = 0; c < replStr.Length; c++)
					{
						EnqueueAsciiCode(replStr[c]);
					}
					//ItelexUpdate?.Invoke(this);
					//LastSendRecvTime.Start();
				}
			}
			catch(Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(SendAscii), "error", ex);
			}
		}

		public void SendAckCmd(int ackVal)
		{
			byte[] data = new byte[] { (byte)ackVal };
			SendCmd(ItelexCommands.Ack, data);
		}

		public void SendHeartbeatCmd()
		{
			SendCmd(ItelexCommands.Heartbeat, new byte[0]);
		}

		public void SendRejectCmd(string reason)
		{
			//Log(LogTypes.Info, nameof(SendRejectCmd), $"reason={reason}");
			byte[] data = Encoding.ASCII.GetBytes(reason);
			SendCmd(ItelexCommands.Reject, data);
		}

		public void SendDirectDialCmd(int extension)
		{
			if (ConnectionState != ConnectionStates.Disconnected)
			{
				//Log(LogTypes.Info, nameof(SendDirectDialCmd), $"extension={extension}");
				byte[] data = new byte[] { (byte)extension };
				SendCmd(ItelexCommands.DirectDial, data);
			}
		}

		public void SendVersionCodeCmd(int? remoteVersion, string versionStr)
		{
			int version = VERSION_NUMBER;
			if (remoteVersion != null)
			{
				version = Math.Min(version, remoteVersion.Value);
			}

			if (ConnectionState != ConnectionStates.Disconnected)
			{
				versionStr = versionStr.Replace(".", "");
				List<byte> data = new List<byte>();
				data.Add((byte)version);
				int len = versionStr.Length <= 5 ? versionStr.Length : 5;
				for (int i = 0; i < len; i++)
				{
					//if (char.IsDigit(version[i])) data.Add((byte)version[i]);
					data.Add((byte)versionStr[i]);
				}
				if (data.Count < 6) data.Add(0x00);
				SendCmd(ItelexCommands.ProtocolVersion, data.ToArray());
			}
		}

		public void SendEndCmd()
		{
			if (ConnectionState != ConnectionStates.Disconnected)
			{
				SendCmd(ItelexCommands.End);
			}
		}

		public void SendRemoteConfirmCmd()
		{
			if (ConnectionState != ConnectionStates.Disconnected)
			{
				SendCmd(ItelexCommands.RemoteConfirm);
			}
		}

		public void SendRemoteCallCmd()
		{
			if (ConnectionState != ConnectionStates.Disconnected)
			{
				SendCmd(ItelexCommands.RemoteCall);
			}
		}

		public void SendAcceptCallRemoteCmd()
		{
			if (ConnectionState != ConnectionStates.Disconnected)
			{
				SendCmd(ItelexCommands.AcceptCallRemote);
			}
		}

		private void EnqueueAsciiCode(char asciiChr)
		{
			if (Local)
			{
				//ItelexUpdate?.Invoke(this);
				return;
			}

			if (!IsConnected) return;

			lock (_sendLock)
			{
				_sendBuffer.Enqueue(asciiChr);
			}
			//ItelexUpdate?.Invoke(this);
		}

		protected void SendCmd(ItelexCommands cmd, byte[] data = null)
		{
			if (!IsConnected || _client?.Client == null) return;

			lock (_sendCmdLock)
			{
				int cmdCode = (int)cmd;

				if (ConnectionState == ConnectionStates.Disconnected) return;

				byte[] sendData;
				if (data != null)
				{
					sendData = new byte[data.Length + 2];
					sendData[0] = (byte)cmdCode;
					sendData[1] = (byte)data.Length;
					Buffer.BlockCopy(data, 0, sendData, 2, data.Length);
				}
				else
				{
					sendData = new byte[2];
					sendData[0] = (byte)cmdCode;
					sendData[1] = 0;
				}

				ItelexPacket packet = new ItelexPacket(sendData);

				if (packet.CommandType != ItelexCommands.Ack)
				{
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(SendCmd), packet, CodeManager.SendRecv.Send);
				}

				switch ((ItelexCommands)packet.Command)
				{
					case ItelexCommands.BaudotData:
						_ack.AddTransCharCount(data.Length);
						BuTimerReset();

						if (data.Length == 0)
						{
							_itelexLogger.ItelexLog(LogTypes.Warn, TAG, nameof(SendCmd), "data.Length==0");
						}
						else
						{
							for (int i = 0; i < data.Length; i++)
							{
								if (data[i] == 0)
								{
									_itelexLogger.ItelexLog(LogTypes.Warn, TAG, nameof(SendCmd), "data[]==0 found");
								}
							}
						}
						break;
					case ItelexCommands.Heartbeat:
					case ItelexCommands.Ack:
					case ItelexCommands.DirectDial:
					case ItelexCommands.End:
					case ItelexCommands.Reject:
					case ItelexCommands.ProtocolVersion:
					case ItelexCommands.SelfTest:
					case ItelexCommands.RemoteConfig:
					case ItelexCommands.ConnectRemote:
					case ItelexCommands.RemoteConfirm:
					case ItelexCommands.RemoteCall:
					case ItelexCommands.AcceptCallRemote:
						break;
				}

				try
				{
					_client.Client.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, EndSend, null);
				}
				catch (SocketException sockEx)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(SendCmd), cmd.ToString(), sockEx);
					Disconnect(DisconnectReasons.NotConnected);
				}
				catch(Exception ex)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(SendCmd), cmd.ToString(), ex);
					Disconnect(DisconnectReasons.SendCmdError);
				}
			}
		}

		private void EndSend(IAsyncResult ar)
		{
			if (ConnectionState == ConnectionStates.Disconnected) return;

			try
			{
				_client.Client.EndSend(ar);
				if (!_client.Connected)
				{
					Disconnect(DisconnectReasons.TcpDisconnectByRemote);
				}
			}
			catch
			{
			}
		}

		/*
		private void DataReceived(IAsyncResult ar)
		{
			//if (ConnectionState == ConnectionStates.Disconnected) return;

			int dataReadCount;
			try
			{
				dataReadCount = _client.Client.EndReceive(ar);
			}
			catch (Exception ex)
			{
				if (ex is SocketException)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(DataReceived), "socket closed by remote", ex);
					Disconnect(DisconnectReasons.TcpDisconnectByRemote);
				}
				else
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(DataReceived), "error1", ex);
					Disconnect(DisconnectReasons.TcpDataReceivedError);
				}
				return;
			}

			if (dataReadCount == 0)
			{
				_itelexLogger.ItelexLog(LogTypes.Info, TAG, nameof(DataReceived), "received empty data");
				Disconnect(DisconnectReasons.TcpDisconnectByRemote);
				return;
			}

			byte[] buffer = ar.AsyncState as byte[];
			Array.Resize(ref buffer, dataReadCount);
			AddReceiveBuffer(buffer);
			StartReceive(false);
		}
		*/

		private void ClientReceiveTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (_clientReceiveTimerActive) return;
			_clientReceiveTimerActive = true;
			try
			{
				ProcessReceivedData();
			}
			finally
			{
				_clientReceiveTimerActive = false;
			}
		}

		private void ProcessReceivedData()
		{
			if (ConnectionState == ConnectionStates.Disconnected) return;
			if (!_client.Connected || !_client.Client.Connected)
			{
				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ProcessReceivedData),
						$"_client.Connected={_client.Connected} _client.Client.Connected={_client.Client.Connected}");
				Disconnect(DisconnectReasons.NotConnected);
				return;
			}

			// check for 10 minutes timeout
			if (LastSendRecvTime.IsElapsedSeconds(10 * 60))
			{
				Logoff("\r\ntimeout\r\n");
				return;
			}

			// early check to save computing time
			if (_client.Available == 0 && _clientReceiveBuffer.Count == 0) return;

			byte[] preBuffer;
			int avail = _client.Available;
			if (avail > 0)
			{
				preBuffer = new byte[avail];
				_client.Client.Receive(preBuffer, avail, SocketFlags.None);
				//List<string> dump = CommonHelper.DumpByteArrayStr(preBuffer, 0);
				//foreach (string line in dump)
				//{
				//	_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ProcessReceivedData), line);
				//}
			}
			else
			{
				preBuffer = new byte[0];
			}

			lock (_clientReceiveBufferLock)
			{
				foreach(byte b in preBuffer)
				{
					_clientReceiveBuffer.Enqueue(b);
				}

				if (_clientReceiveBuffer.Count == 0) return;

				if (ConnectionState == ConnectionStates.TcpConnected || ConnectionState == ConnectionStates.Connected)
				{
					byte data0 = _clientReceiveBuffer.Peek();
					if (data0 <= 0x09 || data0 >= 0x10 && data0 < 0x1F || data0 >= 0x81 && data0 <= 0x84)
					{
						ConnectionState = ConnectionStates.ItelexTexting;
					}
					else
					{
						ConnectionState = ConnectionStates.AsciiTexting;
						_lastAckReceived.Stop();
					}
					//ItelexUpdate?.Invoke(this);
					//_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ProcessReceivedData),
					//	$"recv data={data0} set ConnectionState to {ConnectionState}");
				}

				try
				{
					if (ConnectionState == ConnectionStates.AsciiTexting)
					{   // ascii
						byte[] newData = _clientReceiveBuffer.ToArray();
						_clientReceiveBuffer.Clear();

						string asciiStr = Encoding.ASCII.GetString(newData, 0, newData.Length);
						asciiStr = asciiStr.Replace('%', CodeManager.ASC_BEL);
						asciiStr = asciiStr.Replace('@', CodeManager.ASC_WRU);

						if (_inputActive || _inputKgActive)
						{
							AddInput(asciiStr);
							LastSendRecvTime.Start();
						}
						else
						{
							//ItelexReceived?.Invoke(this, asciiStr);
						}
						ItelexReceived?.Invoke(this, asciiStr);
					}
					else
					{   // i-telex
						while (true)
						{
							if (_clientReceiveBuffer.Count < 2) return;
							byte cmdType = _clientReceiveBuffer.ElementAt(0); // cmd
							if (cmdType > 0x09 && cmdType < 0x81 || cmdType > 0x84)
							{
								// remove invalid cmdType
								_clientReceiveBuffer.Dequeue();
								continue;
							}

							byte cmdLen = _clientReceiveBuffer.ElementAt(1); // len
							if (_clientReceiveBuffer.Count < cmdLen + 2) return;

							byte[] packetData = new byte[cmdLen + 2];
							for (int i = 0; i < cmdLen + 2; i++)
							{
								packetData[i] = _clientReceiveBuffer.Dequeue();
							}
							ItelexPacket packet = new ItelexPacket(packetData);
							DecodePacket(packet);
						}
					}
				}
				catch (Exception ex)
				{
					_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(ProcessReceivedData), "error", ex);
				}
			}
		}

		private void AddReceiveBuffer(byte[] data)
		{
			lock (_clientReceiveBufferLock)
			{
				foreach (byte b in data)
				{
					_clientReceiveBuffer.Enqueue(b);
				}
			}
		}

		private void DecodePacket(ItelexPacket packet)
		{
			if (packet.CommandType != ItelexCommands.Ack)
			{
				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(DecodePacket), packet, CodeManager.SendRecv.Recv);
			}

			// check for empty packet
			switch(packet.CommandType)
			{
				case ItelexCommands.DirectDial:
				case ItelexCommands.BaudotData:
				case ItelexCommands.Reject:
				case ItelexCommands.Ack:
				case ItelexCommands.ProtocolVersion:
					if (packet.Len == 0)
					{
						_itelexLogger.ItelexLog(LogTypes.Warn, TAG, nameof(DecodePacket), $"empty {packet.CommandType} received");
						return;
					}
					break;
			}

			switch (packet.CommandType)
			{
				case ItelexCommands.DirectDial:
					ExtensionNumber = packet.Data[0];
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(DecodePacket), $"direct dial cmd received {ExtensionNumber}");
					break;
				case ItelexCommands.BaudotData:
					BuTimerReset();
					string asciiStr = CodeManager.BaudotDataToAscii(packet.Data, ref _shiftState, CodeSets.ITA2);
					SetCondNewLine(asciiStr);
					_ack.AddReceivedCharCount(packet.Data.Length);
					_ack.AddPrintedCharCount(packet.Data.Length);
					//Debug.WriteLine($"{asciiStr} {_ack.TransCnt}");
					if (_inputActive || _inputKgActive)
					{
						AddInput(asciiStr);
					}
					else
					{
						ItelexReceived?.Invoke(this, asciiStr);
						BaudotSendRecv?.Invoke(this, packet.Data);
						//ItelexUpdate?.Invoke(this);
					}
					LastSendRecvTime.Start();
					DataReceivedFlag = true;
					ConnectionStarted = true;
					break;
				case ItelexCommands.End:
					if (_connectionDirection != ConnectionDirections.Remote)
					{
						Disconnect(DisconnectReasons.EndCmdReceived);
					}
					else
					{
						RemoteEndReceived();
					}
					break;
				case ItelexCommands.Reject:
					string reason = Encoding.ASCII.GetString(packet.Data, 0, packet.Data.Length);
					CallStatus = CallStatusEnum.Reject;
					RejectReason = reason.TrimEnd('\x00');
					Disconnect(DisconnectReasons.Reject);
					break;
				case ItelexCommands.Heartbeat:
					_lastAckReceived.Start();
					break;
				case ItelexCommands.Ack:
					lock (_sendLock)
					{
						_ack.SendAckCnt = packet.Data[0];
						//_ackRecvFlag = true;
						_lastAckReceived.Start();
					}
					//_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(DecodePacket),
					//	$"recv ack {_ack.SendAckCnt} RemoteBuffer={_ack.RemoteBufferCount}");
					ConnectionStarted = true;
					break;
				case ItelexCommands.ProtocolVersion:
					RemoteItelexVersion = packet.Data[0];
					if (packet.Data.Length > 1)
					{
						// get version string
						string versionStr = Encoding.ASCII.GetString(packet.Data, 1, packet.Data.Length - 1);
						versionStr = versionStr.TrimEnd('\x00'); // remove 00-byte suffix
						RemoteItelexVersionStr = versionStr;
					}
					if (_connectionDirection == ConnectionDirections.In)
					{
						// answer with own version
						SendVersionCodeCmd(RemoteItelexVersion, OurItelexVersionStr);
					}
					break;
				case ItelexCommands.SelfTest:
					//Log(LogTypes.Debug, nameof(DecodePacket), "self test command");
					break;
				case ItelexCommands.RemoteConfig:
					//Log(LogTypes.Debug, nameof(DecodePacket), "remote config command");
					break;
				case ItelexCommands.ConnectRemote:
					if (packet.Len != 6) return;
					// 81 06 4D 97 53 00 21 B4
					int remoteNumber = (int)BitConverter.ToUInt32(packet.Data, 0);
					int remotePin = BitConverter.ToUInt16(packet.Data, 4);
					if (remoteNumber > 0 && remotePin > 0)
					{
						_remoteNumber = remoteNumber;
						_remotePin = remotePin;
					}
					//Log(LogTypes.Debug, nameof(DecodePacket), $"connect remote {_remoteNumber}");
					break;
				case ItelexCommands.AcceptCallRemote:
					if (packet.Len != 0) return;
					AcceptCallRemoteReceived();
					break;
			}
		}

		private void SetCondNewLine(string asciiText)
		{
			foreach (char c in asciiText)
			{
				SetCondNewLine(c);
			}
		}

		private void SetCondNewLine(char asciiChr)
		{
			switch (asciiChr)
			{
				case CodeManager.ASC_LTRS:
				case CodeManager.ASC_FIGS:
					break;
				case '\r':
					_condLastCr = true;
					break;
				case '\n':
					_condLastLf = true;
					break;
				default:
					_condLastCr = false;
					_condLastLf = false;
					break;
			}
		}

		/// <summary>
		/// conditional new line makes shure that we are on a new line without sending an additional new line
		/// </summary>
		/// <param name="asciiText"></param>
		/// <returns></returns>
		private string ProcessCondNewLine(string asciiText)
		{
			string newStr = "";
			foreach(char c in asciiText)
			{
				if (c == CodeManager.ASC_COND_NL)
				{
					newStr += CodeManager.ASC_CR;
					SetCondNewLine(CodeManager.ASC_CR);
					if (!_condLastLf)
					{
						newStr += CodeManager.ASC_LF;
						SetCondNewLine(CodeManager.ASC_LF);
					}
					continue;
				}
				SetCondNewLine(c);
				newStr += c;
			}
			return newStr;
		}

		public InputResult InputNumber(string text, string defaultValue, int repeat, 
			bool allowCorr = true, bool allowSecure = true, bool allowHelp = false)
		{
			if (defaultValue != null) defaultValue = defaultValue.Replace(" ", "");
			return Input(text, ShiftStates.Figs, defaultValue, true, null, 10, repeat, allowCorr, allowSecure, allowHelp, false);
		}

		public InputResult InputYesNo(string text, string defaultValue, int repeat)
		{
			InputResult result = InputSelection(text, ShiftStates.Ltrs, defaultValue, new string[] { "y", "j", "n" }, 1,
					repeat, false, false);

			switch(result.InputString.ToLower())
			{
				case "j":
				case "y":
					result.InputBool = true;
					break;
				case "n":
					result.InputBool = false;
					break;
				default:
					result.ErrorOrTimeoutOrDisconnected = true;
					result.InputBool = false;
					break;
			}
			return result;
		}

		public InputResult InputPin(string text, int length, string defaultValue = null)
		{
			return Input(text, ShiftStates.Figs, defaultValue, false, null, length, 1, false, true, false, false);
		}

		public InputResult InputString(string text, ShiftStates shiftState, string defaultValue, int length,
			int repeat, bool allowCorr = true, bool allowSecure = false, bool allowHelp = false)
		{
			return Input(text, shiftState, defaultValue, false, null, length, repeat, allowCorr, allowSecure, allowHelp, false);
		}

		public InputResult InputSelection(string text, ShiftStates shiftState, string defaultValue, string[] selector,
				int? length, int repeat, bool allowCorr = false, bool allowHelp = false)
		{
			int maxLen;
			if (!length.HasValue)
			{
				maxLen = 0;
				foreach (string sel in selector)
				{
					if (sel.Length > maxLen)
					{
						maxLen = sel.Length;
					}
				}
				length = maxLen + 1;
			}
			return Input(text, shiftState, defaultValue, false, selector, length.Value, repeat, allowCorr, false, allowHelp, false);
		}

		public InputResult InputMultiLine()
		{
			char[] inputEndCharsTemp = _inputEndChars;
			_inputEndChars = new char[0];
			InputResult result = Input("", ShiftStates.Both, "", false, null, 0, 1, false, false, false, true);
			_inputEndChars = inputEndCharsTemp;
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="shiftState"></param>
		/// <param name="defaultValue"></param>
		/// <param name="isNumber"></param>
		/// <param name="selector"></param>
		/// <param name="length"></param>
		/// <param name="repeat"></param>
		/// <param name="allowCorr"></param>
		/// <param name="allowSecure"></param>
		/// <param name="allowHelp"></param>
		/// <param name="multiLine"></param>
		/// <returns>InputResult - not null</returns>
		private InputResult Input(string text, ShiftStates shiftState, string defaultValue, bool isNumber,
				string[] selector, int length, int repeat, bool allowCorr = true, bool allowSecure = false,
				bool allowHelp = false, bool multiLine = false)
		{
			InputResult result = new InputResult();

			for (int i = 0; i < repeat; i++)
			{
				if (!IsConnected)
				{
					result.ErrorOrTimeoutOrDisconnected = true;
					return result;
				}

				SendAscii($"{text}");
				if (!text.EndsWith("\n") && !string.IsNullOrEmpty(text))
				{
					SendAscii(" ");
				}
				//if (!string.IsNullOrWhiteSpace(prompt))
				//{
				//	SendAscii($"{prompt} ");
				//}
				if (!string.IsNullOrWhiteSpace(defaultValue))
				{
					SendAscii($"{defaultValue} ");
				}

				WaitAllSendBuffersEmpty(false);

				switch (shiftState)
				{
					case ShiftStates.Ltrs:
						SendAscii(CodeManager.ASC_LTRS);
						break;
					case ShiftStates.Figs:
						SendAscii(CodeManager.ASC_FIGS);
						break;
				}

				StartInput(length, !string.IsNullOrWhiteSpace(defaultValue), allowCorr, allowSecure, allowHelp, multiLine);
				int reminder = 30;
				while (_inputActive && IsConnected)
				{
					if (_inputTimer.IsElapsedSeconds(INPUT_TIMEOUT))
					{   // input timeout
						result.ErrorOrTimeoutOrDisconnected = true;
						result.Timeout = true;
						return result;
					}

					if (_inputTimer.IsElapsedSeconds(reminder))
					{
						if (_shiftState == ShiftStates.Ltrs)
						{
							SendAscii(CodeManager.ASC_FIGS);
							SendAscii(CodeManager.ASC_LTRS);
						}
						else
						{
							SendAscii(CodeManager.ASC_LTRS);
							SendAscii(CodeManager.ASC_FIGS);
						}
						reminder += 15;
					}
					Thread.Sleep(100);
				}

				if (!IsConnected)
				{
					result.ErrorOrTimeoutOrDisconnected = true;
					return result;
				}

				string endStr = "";
				foreach (char c in _inputEndChar2)
				{
					if (c == CodeManager.ASC_CR || c == CodeManager.ASC_LF) endStr += c;
				}
				if (!endStr.Contains(CodeManager.ASC_CR))
				{
					SendAscii(CodeManager.ASC_CR);
				}
				if (!endStr.Contains(CodeManager.ASC_LF))
				{
					SendAscii(CodeManager.ASC_LF);
				}

				//_inputBuffer = _inputBuffer.TrimEnd(new char[] { '\r', '\n' });
				if (!_inputMultiline)
				{
					_inputBuffer = _inputBuffer.Replace("\r", "");
					_inputBuffer = _inputBuffer.Replace("\n", "");
				}

				_inputBuffer = _inputBuffer.Trim();

				if (!string.IsNullOrWhiteSpace(defaultValue) &&
					(_inputBuffer.Length==0 || _inputBuffer.Length == 1 && _inputBuffer[0] == '.'))
				{
					_inputBuffer = defaultValue;
				}
				if (_inputAllowHelp && _inputEndChar2 == "?")
				{
					result.InputString = "?";
					result.IsHelp = true;
					return result;
				}

				if (_inputBuffer == "") continue;

				//if (_inputBuffer.Contains("xxx")) continue;

				if (selector != null)
				{
					bool selOk = false;
					foreach (string sel in selector)
					{
						if (_inputBuffer == sel)
						{
							selOk = true;
							break;
						}
					}
					if (!selOk) continue;
				}

				if (isNumber)
				{
					_inputBuffer = _inputBuffer.Replace(" ", "");
					if (!int.TryParse(_inputBuffer, out int number)) continue;
					result.InputNumber = number;
					result.IsNumber = true;
					result.InputString = _inputBuffer;
					return result;
				}

				result.InputString = _inputBuffer;
				return result;
			}

			result.ErrorOrTimeoutOrDisconnected = false;
			return result;
		}

		protected string GetAnswerback()
		{
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetAnswerback), "GetAnswerback start");

			WaitAllSendBuffersEmpty();
			WaitForSilence(0, 1000, 5000);
			StartInputKg(0, false);
			SendAscii(CodeManager.ASC_FIGS);
			SendAscii(CodeManager.ASC_WRU);

			// wait for first character from KG

			TickTimer timeout = new TickTimer();
			DataReceivedFlag = false;
			while (true)
			{
				if (timeout.IsElapsedMilliseconds(5000))
				{
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetAnswerback),
							$"timeout waiting for first kg char ({timeout.ElapsedMilliseconds}ms)");
					return ""; // no kg received
				}
				if (DataReceivedFlag)
				{
					// character received
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetAnswerback),
							$"first kg char received ({timeout.ElapsedMilliseconds}ms)");
					break;
				}
				Thread.Sleep(100);
			}

			// wait for end of kg

			//WaitForSilence(5000, 2000, 10000);
			WaitForSilence(0, 3000, 5000); // neu

			_inputKgActive = false;
			string kennung = CodeManager.CleanAscii(_inputBuffer).Trim();

			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(GetAnswerback), $"GetAnswerback end [{kennung}]");

			return kennung;
		}

		protected void StartInput(int length, bool defaultActive, bool allowCorr, bool allowSecure, bool allowHelp, bool multiline)
		{
			_inputMaxLength = length;
			_inputBuffer = "";
			_inputEndChar2 = "";
			_inputTimer.Start();
			_inputDefaultActive = defaultActive;
			_inputAllowCorr = allowCorr;
			_inputAllowSecure = allowSecure;
			_inputAllowHelp = allowHelp;
			_inputMultiline = multiline;
			_inputActive = true;
		}

		private void StartInputKg(int length, bool defaultActive)
		{
			_inputMaxLength = length;
			_inputBuffer = "";
			_inputTimer.Start();
			_inputKgActive = true;
			_inputDefaultActive = defaultActive;
			_inputAllowCorr = false;
			_inputAllowHelp = false;
		}

		protected void StartInputGegenschreiben()
		{
			_inputBuffer = "";
			_inputGegenschreiben = false;
			_inputActive = true;
		}

		public virtual void Logoff(string msg)
		{
			if (IsConnected)
			{
				if (!string.IsNullOrEmpty(msg)) SendAscii(msg);
				WaitAllSendBuffersEmpty();
				Thread.Sleep(3000);
				SendEndCmd();
				Thread.Sleep(1000);
				Disconnect(DisconnectReasons.Logoff);
			}
			Log(LogTypes.Debug, nameof(Logoff), $"logoff, connectionId = {ConnectionId}");
			Log(LogTypes.Notice, nameof(Logoff), $"--- connection end ({_connectionTimer.ElapsedSeconds}) sec. ---");
			
			_itelexLogger.End();
		}

		// default
		protected char[] _inputEndChars = new char[] { CodeManager.ASC_CR, CodeManager.ASC_LF };

		private void AddInput(string asciiStr)
		{
			if (!_inputActive && !_inputKgActive) return;
			//if (!_inputActive) return;

			for (int i = 0; i < asciiStr.Length; i++)
			{
				char chr = asciiStr[i];
				string chrStr = null;
				if (!_inputKgActive)
				{
					if (_inputEndChars.Contains(chr))
					{
						//_inputBuffer = CodeManager.CleanAscii(_inputBuffer);
						_inputTimer.Stop();
						AddInputEndChar2(asciiStr);
						//_inputEndChar2 = char.ToString(chr);
						_inputActive = false;
						return;
					}
					else if (_inputAllowCorr && (_inputBuffer + chr).EndsWith("xxx"))
					{
						// correction
						_inputBuffer = "";
						chr = (char)0x00;
						SendAscii("\r\n");
					}
					else if (_inputAllowSecure && chr == 's')
					{
						// secure
						char[] pattern = new char[] { '0', '1', '2', '4', '5', '6', '7', '8', '9' };
						SendAscii("\r\n");
						foreach (char p in pattern)
						{
							SendAscii(new string(p, _inputMaxLength - 1));
							SendAscii("\r");
						}
						_inputBuffer = "";
						chr = (char)0x00;
					}
					else if (_inputDefaultActive && (_inputBuffer + chr) == ".")
					{
						// default '.'
						_inputBuffer += chr;
						_inputTimer.Stop();
						_inputEndChar2 = ".";
						_inputActive = false;
						return;
					}
					else if (_inputAllowHelp && chr == '?')
					{   // help
						_inputBuffer += chr;
						_inputEndChar2 = "?";
						_inputTimer.Start();
						_inputActive = false;
						return;
					}

					if (_inputMultiline)
					{
						if (chr == CodeManager.ASC_CR || chr == CodeManager.ASC_LF) chr = ' ';
					}
					if (chr != 0x00)
					{
						chrStr = CodeManager.CleanAsciiKeepCrLf(chr.ToString());
					}
				}
				else
				{
					chrStr = CodeManager.CleanAscii(chr.ToString());
				}
				_inputBuffer += chrStr;
				_inputTimer.Start();
				int lengthWithoutCrLf = CodeManager.CleanAscii(_inputBuffer).Length;
				if (_inputMaxLength > 0 && lengthWithoutCrLf >= _inputMaxLength)
				{   // input length reached
					_inputKgActive = false;
					AddInputEndChar2(chr.ToString());
					_inputTimer.Stop();
					_inputActive = false;
					return;
				}

				if (_inputMultiline)
				{
					int p = _inputBuffer.IndexOf("+?");
					if (p > 0)
					{
						_inputBuffer = _inputBuffer.Substring(0, p);
						_inputEndChar2 = "+?";
						_inputTimer.Stop();
						_inputActive = false;
						return;
					}
				}

			}
		}

		private void AddInputEndChar2(string str)
		{
			foreach(char chr in str)
			{
				if (_inputEndChars.Contains(chr))
				{
					_inputEndChar2 += chr;
					if (_inputEndChar2.Length>2)
					{
						_inputEndChar2 = _inputEndChar2.ExtRightString(2);
					}
				}
			}
		}

		public static int? GetNumberFromAnswerback(string answerback)
		{
			if (string.IsNullOrWhiteSpace(answerback)) return null;

			string[] parts = answerback.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0) return null;

			if (int.TryParse(parts[0], out int number))
			{
				return number;
			}
			return null;
		}

		private string ReasonToString(string reason)
		{
			switch(reason.ToLower())
			{
				case "occ":
					return "occupied";
				case "abs":
					return "temporarily disabled";
				case "na":
					return "not allowed";
				case "nc":
					return "not connected";
				case "der":
					return "derailed";
				default:
					return reason;
			}
		}

		public void AddDataLog(CodeManager.SendRecv sendRecv, byte code)
		{
			if (code == CodeManager.BAU_LTRS)
			{
				_dataLogShiftState = ShiftStates.Ltrs;
			}
			else if (code == CodeManager.BAU_FIGS)
			{
				_dataLogShiftState = ShiftStates.Figs;
			}

			string asciiStr = CodeManager.BaudotCodeToPuncherText(code, _dataLogShiftState, CodeSets.ITA2);

			string sendRecvStr = sendRecv == CodeManager.SendRecv.Send ? "S" : "R";
			string line = $"{DateTime.Now:dd.MM.yy HH:mm:ss} {sendRecvStr} {code:0X02} {asciiStr}";
			_dataLog.Add(line);
		}

		protected void Log(LogTypes logTypes, string method, string msg, Exception ex = null)
		{
			string connStr = RemoteNumber.HasValue ? $"{RemoteNumber} {_connectionDirection} " : "";
			if (ex == null)
			{
				_itelexLogger.ItelexLog(logTypes, TAG, method, $"{ConnectionId} {connStr}{msg}");
			}
			else
			{
				_itelexLogger.ItelexLog(logTypes, TAG, method, $"{connStr}{msg} {ex}");
			}
		}

		public override string ToString()
		{
			return $"{RemoteClientAddrStr} {ConnectionState}";
		}

		#region limited clients

		protected int? _remoteNumber;

		protected int _remotePin;

		public virtual void AcceptCallRemoteReceived()
		{
		}

		public virtual void RemoteEndReceived()
		{
		}

		#endregion
	}

	public class Acknowledge
	{
		private const string TAG = nameof(Acknowledge);

		private int _connectionId;

		/// <summary>
		/// characters received from remote
		/// </summary>
		public int RecvCnt { get; set; }

		/// <summary>
		/// n_recv, characters received/printed locally
		/// </summary>
		public int RecvAckCnt { get; set; }

		/// <summary>
		/// n_trans, characters send to remote
		/// </summary>
		public int TransCnt { get; set; }

		/// <summary>
		/// n_ack, characters printed by remote
		/// </summary>
		private int _sendAckCnt = 0;
		public int SendAckCnt
		{
			get
			{
				return _sendAckCnt;
			}
			set
			{
				_sendAckCnt = value;
				if (_lastSendAckCnt != null && RemoteBufferCount > 0 && _sendAckCnt == _lastSendAckCnt &&
					_sendAckCntChangedTimeout.IsElapsedMilliseconds(8000))
				{
					// remote buffer is not empty and did not change for the last 8 seconds: suppose that remote buffer is empty
					// and set SendCnt = SendAckCnt
					TransCnt = _sendAckCnt;
					//Debug.WriteLine($"reset SendCnt to {_sendAckCnt}");
					//LogManager.Instance.Logger.Debug(nameof(Acknowledge), nameof(SendAckCnt), "ack resync");
					_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(SendAckCnt),
							$"resync ack {TransCnt} dt={_sendAckCntChangedTimeout.ElapsedMilliseconds}");
				}
				if (_sendAckCnt != _lastSendAckCnt) _sendAckCntChangedTimeout.Start();
				_lastSendAckCnt = _sendAckCnt;
			}
		}

		private int? _lastSendAckCnt;

		private TickTimer _sendAckCntChangedTimeout = new TickTimer(false);

		private bool _isStartPhase;

		protected ItelexLogger _itelexLogger;

		public Acknowledge(int connectiondId, bool startPhase, ItelexLogger itelexLogger)
		{
			_connectionId = connectiondId;
			_isStartPhase = startPhase;
			_itelexLogger = itelexLogger;
		}

		public void Reset()
		{
			RecvCnt = 0;
			RecvAckCnt = 0;
			TransCnt = _sendAckCnt;
			_lastSendAckCnt = null;
		}

		public void ResyncSend()
		{
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ResyncSend), $"_sendAckCnt={_sendAckCnt}");
			TransCnt = _sendAckCnt;
		}

		public void ClearStartPhase()
		{
			_isStartPhase = false;
			_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(ClearStartPhase), "");
		}

		public void AddReceivedCharCount(int n)
		{
			RecvCnt = (RecvCnt + n) % 256;
		}

		public void AddPrintedCharCount(int n)
		{
			RecvAckCnt = (RecvAckCnt + n) % 256;
		}

		public void AddTransCharCount(int n)
		{
			TransCnt = (TransCnt + n) % 256;
			//_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(AddTransCharCount), $"TransCnt={TransCnt} +{n}");
		}

		/// <summary>
		/// Character left to print in remote buffer
		/// </summary>
		public int RemoteBufferCount
		{
			get
			{
				if (_isStartPhase)
				{
					return (256 - SendAckCnt) % 256;
				}
				else
				{
					int remBuf = TransCnt - SendAckCnt; // send to remote minus printed by remote
					if (remBuf > 255) remBuf -= 256;
					//ItelexLogger.ItelexLog($"remBuf={remBuf}");
					if (remBuf < 0) remBuf = 256 + remBuf;
					//ItelexLogger.ItelexLog($"remBuf={remBuf}");
					return remBuf;
				}
			}
		}

		public int LocalBufferCount
		{
			get
			{
				int locBuf = RecvCnt - RecvAckCnt;
				if (locBuf > 255) locBuf -= 256;
				if (locBuf < 0) locBuf = 256 + locBuf;
				return locBuf;
			}
		}

		public string RecvToString()
		{
			return $"RecvCnt={RecvCnt} RecvAckCnt={RecvAckCnt} LocalBufferCount={LocalBufferCount}";
		}

		public string SendToString()
		{
			return $"SendCnt={TransCnt} SendAckCnt={SendAckCnt} RemoteBufferCount={RemoteBufferCount}";
		}
	}
}
