using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Utility;
using System;
using System.IO;
using System.Text;
using static ItelexCommon.Connection.ItelexConnection;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ItelexCommon
{
	public class ItelexLogger: IDisposable
	{
		private const string TAG = nameof(ItelexLogger);

		private LogTypes _logLevel;

		private string _logPath;

		private string _fullName;

		private Logging _logger;

		private SysLog _sysLog;

		private int _connectionId { get; set; }

		private bool _open { get; set; }

		private StreamWriter _streamWriter;

		private ConnectionDirections _dir;

		private int? _itelexNumber;

		private Acknowledge _ack;

		private ShiftStates _shiftState;

		private object _lockRename = new object();

		public ItelexLogger(string logPath)
		{
			_logPath = logPath;
			_logger = LogManager.Instance.Logger;
		}

		public ItelexLogger(string logPath, int connectionId, ConnectionDirections dir, int? number, LogTypes logLevel)
		{
			_logger = LogManager.Instance.Logger;
			_logPath = logPath;
			_logLevel = logLevel;
			_connectionId = connectionId;
			_dir = dir;
			_itelexNumber = number;
			ItelexLog(LogTypes.Info, TAG, nameof(ItelexLogger), "--- start of logfile ---");
			Init();
		}

		public ItelexLogger(int connectionId, ConnectionDirections dir, int? number, string logPath, LogTypes logLevel,
				string sysLogHost, int sysLogPort, string appName)
		{
			_logger = LogManager.Instance.Logger;
			_logPath = logPath;
			_logLevel = logLevel;
			_connectionId = connectionId;
			_dir = dir;
			_itelexNumber = number;
			_sysLog = new SysLog(sysLogHost, sysLogPort, appName, 0);
			ItelexLog(LogTypes.Debug, TAG, nameof(ItelexLogger), "--- start of logfile ---");
			Init();
		}

		~ItelexLogger()
		{
			//LogManager.Instance.Logger.Debug(TAG, "~ItelexLogger", "destructor");
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

			if (disposing)
			{
				// Free any other managed objects here.
				if (_streamWriter != null) Close();
			}
			_disposed = true;
		}

		#endregion Dispose

		private void Init()
		{
			try
			{
				//string numStr = _itelexNumber != null ? $"_{_itelexNumber.Value}" : "";
				//string fileName = $"connection_{_connectionId}_{_dir}{numStr}.log";
				_fullName = GetName();
				OpenStream(false);
				_open = true;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(Init), "error opening logfile stream {_fullName}", ex);
				_sysLog.Log(LogTypes.Error, $"error opening logfile stream {_fullName}");
				_open = false;
			}

		}

		private string GetName()
		{
			string numStr = _itelexNumber != null ? $"_{_itelexNumber.Value}" : "";
			string fileName = $"connection_{_connectionId}_{_dir}{numStr}.log";
			return Path.Combine(_logPath, fileName);
		}

		private void OpenStream(bool append)
		{
			//_logStream = File.OpenWrite(_fullName);
			_streamWriter = new StreamWriter(_fullName, append, Encoding.ASCII);
		}

		public void SetAck(Acknowledge ack)
		{
			_ack = ack;
		}

		public void End()
		{
			ItelexLog(LogTypes.Debug, TAG, nameof(End), "--- end of logfile ---");
			Close();
			//_logItems.Remove(logItem);
		}

		private void Close()
		{
			if (_streamWriter != null && _streamWriter.BaseStream != null)
			{
				try
				{
					if (_streamWriter != null)
					{
						_streamWriter.Close();
						_streamWriter.Dispose();
						_streamWriter = null;
					}
				}
				catch (Exception)
				{
				}
			}
		}

		/// <summary>
		/// set itelex-number an rename log-file
		/// </summary>
		public void SetNumber(int id, int itelexNumber)
		{
			if (_itelexNumber.HasValue || !_open) return;

			_itelexNumber = itelexNumber;
			lock (_lockRename)
			{
				try
				{
					Close();
					string oldName = _fullName;
					_fullName = GetName();
					File.Move(oldName, _fullName);
					OpenStream(true);
					return;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SetNumber), $"error", ex);
					_open = false;
				}
			}
		}

		/*
		public void ItelexLog2(LogTypes logType, string tag, string method, ItelexPacket packet, CodeManager.SendRecv sendRecv)
		{
			if (IsActiveLevel(logType))
			{
				if (_logItem == null) return;
				ItelexLog2(logType, tag, method, packet, sendRecv);
			}
		}

		public void ItelexLog2(LogTypes logType, string tag, string method, string msg)
		{
			if (IsActiveLevel(logType))
			{
				ItelexLog2(logType, tag, method, msg);
			}
		}

		public void ItelexLog2(LogTypes logType, string tag, string method, string msg, Exception ex)
		{
			if (IsActiveLevel(logType))
			{
				ItelexLog2(logType, tag, method, msg, ex);
			}
		}
		*/

		public void ItelexLog(LogTypes logType, string tag, string method, ItelexPacket packet, CodeManager.SendRecv sendRecv)
		{
			string msg = GetExtDebugData(packet, ref _shiftState, sendRecv);
			string sendRecvStr = sendRecv == CodeManager.SendRecv.Recv ? "recv" : "send";
			Log($"[{logType.ToString().PadRight(5)}] [{tag}] [{method}] {sendRecvStr} {msg}");
			SysLog(logType, tag, method, msg);
		}

		public void ItelexLog(LogTypes logType, string tag, string method, string msg)
		{
			Log($"[{logType.ToString().PadRight(5)}] [{tag}] [{method}] {msg}");
			SysLog(logType, tag, method, msg);
		}

		public void ItelexLog(LogTypes logType, string tag, string method, string msg, Exception ex)
		{
			Log($"[{logType.ToString().PadRight(5)}] [{tag}] [{method}] {msg} {ex}");
			SysLog(logType, tag, method, msg, ex);
		}

		private void Log(string text)
		{
			string logStr = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss.fff} {text}";
			try
			{
				if (_streamWriter != null)
				{
					_streamWriter.WriteLine(logStr);
					_streamWriter.Flush();
				}
			}
			catch
			{
				SysLog(LogTypes.Error, nameof(ItelexLogger), nameof(Log), $"error writing logfile {GetName()}");
			}
		}

		private void SysLog(LogTypes logType, string tag, string method, string msg)
		{
			int maxLen = 49; // max. length for Visual SysLog Tag field
			string vsTag = $"[{tag}][{method}]";
			if (vsTag.Length > maxLen) vsTag = vsTag.Substring(0, maxLen);

			_sysLog.Log(logType, $"{vsTag}: {msg}");
		}

		private void SysLog(LogTypes logType, string tag, string method, string msg, Exception ex)
		{
			int maxLen = 49; // max. length for Visual SysLog Tag field
			string vsTag = $"[{tag}][{method}]";
			if (vsTag.Length > maxLen) vsTag = vsTag.Substring(0, maxLen);

			_sysLog.Log(logType, $"{vsTag}: {msg} {ex?.Message}");
		}

		private string GetExtDebugData(ItelexPacket packet, ref ShiftStates shiftState, CodeManager.SendRecv sendRecv)
		{
			if (packet == null)
			{
				return "packet==null";
			}

			string hexData = $"[{packet.GetDebugPacket()}]";
			switch (packet.CommandType)
			{
				case ItelexConnection.ItelexCommands.Heartbeat:
					return $"{packet.CommandType} {hexData}";
				case ItelexConnection.ItelexCommands.DirectDial:
					if (packet.Data != null)
					{
						return $"{packet.CommandType} {packet.Data[0]} {hexData}";
					}
					else
					{
						return $"{packet.CommandType} null {hexData}";
					}
				case ItelexConnection.ItelexCommands.BaudotData:
					string ascii = CodeManager.BaudotDataToAsciiDebug(packet.Data, ref shiftState, CodeSets.ITA2);
					//return $"{packet.CommandType} \"{ascii}\" {hexData}";
					return $"{packet.CommandType} \"{ascii}\"";
				case ItelexConnection.ItelexCommands.End:
					return $"{packet.CommandType} {hexData}";
				case ItelexConnection.ItelexCommands.Reject:
					string reason = Encoding.ASCII.GetString(packet.Data, 0, packet.Data.Length);
					return $"{packet.CommandType} {reason} {hexData}";
				case ItelexConnection.ItelexCommands.Ack:
					if (_ack == null)
					{
						return $"_ack==null";
					}
					string ackStr;
					if (sendRecv == CodeManager.SendRecv.Recv)
					{
						ackStr = $"[{_ack.TransCnt} {_ack.RemoteBufferCount}]";
					}
					else
					{
						ackStr = $"[{_ack.SendAckCnt}]";
					}
					return $"{packet.CommandType} {packet.Data[0]} {ackStr} {hexData}";
				case ItelexConnection.ItelexCommands.ProtocolVersion:
					string versionStr = "";
					if (packet.Data.Length > 1)
					{
						// get version string
						versionStr = Encoding.ASCII.GetString(packet.Data, 1, packet.Data.Length - 1);
						versionStr = versionStr.TrimEnd('\x00'); // remove 00-byte suffix
					}
					return $"{packet.CommandType} {packet.Data[0]} {versionStr} {hexData}";
				case ItelexConnection.ItelexCommands.SelfTest:
					return $"{packet.CommandType} {hexData}";
				case ItelexConnection.ItelexCommands.RemoteConfig:
					return $"{packet.CommandType} {hexData}";
				case ItelexConnection.ItelexCommands.ConnectRemote:
					int remoteNumber = (int)BitConverter.ToUInt32(packet.Data, 0);
					int remotePin = BitConverter.ToUInt16(packet.Data, 4);
					return $"{packet.CommandType} {remoteNumber} {remotePin} {hexData}";
				case ItelexConnection.ItelexCommands.RemoteConfirm:
					return $"{packet.CommandType} {hexData}";
				case ItelexConnection.ItelexCommands.RemoteCall:
					return $"{packet.CommandType} {hexData}";
				case ItelexConnection.ItelexCommands.AcceptCallRemote:
					return $"{packet.CommandType} {hexData}";
				default:
					return $"invalid Packet {packet.Command} {hexData}";
			}
		}

		private bool IsActiveLevel(LogTypes current)
		{
			return (int)current <= (int)_logLevel;
		}
	}
}
