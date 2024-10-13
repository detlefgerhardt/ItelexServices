using ItelexCommon.Logger;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItelexCommon
{
	public enum LogTypes { None = 0, Fatal = 1, Error = 2, Warn = 3, Notice = 4, Info = 5, Debug = 6 };

	[Serializable]
	public class Logging
	{
		private const string TAG = nameof(Logging);

		public delegate void RecvLogEvent(object sender, LogArgs e);
		public event RecvLogEvent RecvLog;

		private SysLog _sysLog;

		private bool _cleanupTimerActive;
		private System.Timers.Timer _cleanupTimer = null;
		private DateTime _lastAutoCleanup = DateTime.Now.AddDays(-2);
		//private string[] _cleanupPattern = null;
		//private int _cleanupKeyDayCount = 0;
		//private int? _cleanupMinSize = 0;

		private LoggerCleanupItem[] _autoCleanupItems;

		public LogTypes LogLevel { get; set; }

		public string LogfileFullname
		{
			get
			{
				if (string.IsNullOrEmpty(_logPath) || string.IsNullOrEmpty(_logName)) return "";
				return Path.Combine(_logPath, _logName);
			}
		}

		private object _lock = new object();

		private string _logPath;

		private string _logName;

		public string LogfilePath
		{
			get { return _logPath; }
			set { _logPath = value; }
		}

		//public static Logging Instance
		//{
		//	get
		//	{
		//		if (instance == null)
		//		{
		//			instance = new Logging();
		//		}
		//		return instance;
		//	}
		//}

		public Logging(string logPath, string logName, LogTypes logLevel)
		{
			_logPath = logPath;
			_logName = logName;
			LogLevel = logLevel;
			Init();
		}

		public Logging(string logPath, string logName, LogTypes logLevel, string sysLogHost, int sysLogPort, string appName)
		{
			_logPath = logPath;
			_logName = logName;
			LogLevel = logLevel;
			_sysLog = new SysLog(sysLogHost, sysLogPort, appName, 0);
			Init();
		}

		private void Init()
		{
			try
			{
				// create directoy if it does not exist
				Directory.CreateDirectory(_logPath);
			}
			catch (Exception ex)
			{
				_sysLog.Log(LogTypes.Error, $"error creating logpath {_logPath} {ex.Message}");
			}
		}

		public void InitAutoCleanup(LoggerCleanupItem[] cleanupItems)
		{
			_autoCleanupItems = cleanupItems;
			foreach (LoggerCleanupItem item in cleanupItems)
			{
				Info(TAG, nameof(InitAutoCleanup), item.ToString());
			}
			//_cleanupPattern = pattern;
			//_cleanupKeyDayCount = keepDayCount;
			//_cleanupMinSize = minSize;
			_cleanupTimer = new System.Timers.Timer(1000 * 60);
			_cleanupTimer.Elapsed += AutoCleanupTimer_Elapsed;
			_cleanupTimer.Start();
		}

		private void AutoCleanupTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (_cleanupTimerActive) return;

			try
			{
				_cleanupTimerActive = true;

				DateTime now = DateTime.Now;
				if (now.Day == _lastAutoCleanup.Day) return;

				_lastAutoCleanup = now;
				DirectoryInfo di = new DirectoryInfo(_logPath);
				foreach (LoggerCleanupItem item in _autoCleanupItems)
				{
					FileInfo[] files = di.GetFiles(item.Pattern);
					int cntDel = 0;
					foreach (FileInfo fi in files)
					{
						//System.Diagnostics.Debug.WriteLine($"{fi.Name} {fi.LastWriteTime:dd.MM.yy}");
						if (fi.LastWriteTime.AddDays(item.MaxDays) >= now) continue;
						if (item.MinSize.HasValue && fi.Length >= item.MinSize) continue;

						try
						{
							Debug(TAG, nameof(AutoCleanupTimer_Elapsed), $"Cleanup {fi.Name} {fi.Length} {fi.CreationTime:hh.MM.yyyy}");
							File.Delete(fi.FullName);
							cntDel++;
						}
						catch (Exception ex)
						{
							Error(TAG, nameof(AutoCleanupTimer_Elapsed), "", ex);
						}
					}
					Debug(TAG, nameof(AutoCleanupTimer_Elapsed), $"pattern='{item.Pattern}' files found={files.Length}, delete={cntDel}");
				}
			}
			finally
			{
				_cleanupTimerActive = false;
			}
		}

		public void Debug(string section, string method, string text)
		{
			Log(LogTypes.Debug, section, method, text);
			//_sysLog.Log(LogTypes.Debug, text);
		}

		public void Info(string section, string method, string text)
		{
			Log(LogTypes.Info, section, method, text);
			//_sysLog.Log(LogTypes.Info, text);
		}

		public void Notice(string section, string method, string text)
		{
			Log(LogTypes.Notice, section, method, text);
			//_sysLog.Log(LogTypes.Notice, text);
		}

		public void Warn(string section, string method, string text)
		{
			Log(LogTypes.Warn, section, method, text);
			//_sysLog.Log(LogTypes.Warn, text);
		}

		public void Error(string section, string method, string text)
		{
			Log(LogTypes.Error, section, method, text);
			//_sysLog.Log(LogTypes.Error, text);
		}

		public void Error(string section, string method, string text, Exception ex = null)
		{
			if (ex != null)
			{
				text = $"{text} result={ex.HResult} {ex.Message}\r\n{ex}";
			}
			Log(LogTypes.Error, section, method, text);
			//_sysLog.Log(LogTypes.Error, text);
		}

		public void Fatal(string section, string method, string text)
		{
			Log(LogTypes.Fatal, section, method, text);
			//_sysLog.Log(LogTypes.Fatal, text);
		}

		public void Log(LogTypes logType, string section, string method, string text, bool show = true)
		{
			if (IsActiveLevel(logType))
			{
				AppendLog(logType, section, method, text);
				OnLog(new LogArgs(logType, section, method, text));
			}
		}

		public void OnLog(LogArgs e)
		{
			RecvLog?.Invoke(this, e);
		}

		private void AppendLog(LogTypes logType, string section, string method, string text)
		{
			lock(_lock)
			{
				int? id = Task.CurrentId;
				if (!id.HasValue)
				{
					System.Diagnostics.Debug.Write("");
				}
				string prefix = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss.ff} {logType.ToString().PadRight(5)} [{Task.CurrentId}] [{section}]";
				string logStr = $"{prefix} [{method}] {text}\r\n";
				try
				{
					File.AppendAllText(LogfileFullname, logStr);
				}
				catch
				{
					_sysLog?.Log(LogTypes.Error, $"[{nameof(Logging)}][{nameof(AppendLog)}]: error writing logfile {LogfileFullname}");

					// try to log in program directory
					try
					{
						string newName = Path.Combine(Application.StartupPath, _logName);
						File.AppendAllText(newName, $"{prefix} [AppendLog] Error writing logfile to {LogfileFullname}\r\n");
						File.AppendAllText(newName, logStr);
					}
					catch{ }
				}
			}

			int maxLen = 49; // max. length for Visual SysLog Tag field
			string tag = $"[{section}][{method}]";
			if (tag.Length > maxLen) tag = tag.Substring(0, maxLen);
			_sysLog?.Log(logType, $"{tag}: {text}");
		}

		private bool IsActiveLevel(LogTypes current)
		{
			return (int)current <= (int)LogLevel;
		}
	}

	public class LogArgs : EventArgs
	{
		public LogTypes LogType { get; set; }

		public string Section { get; set; }

		public string Method { get; set; }

		public string Message { get; set; }

		public LogArgs(LogTypes logType, string section, string method, string msg)
		{
			LogType = logType;
			Section = section;
			Method = method;
			Message = msg;
		}
	}

	public class LoggerCleanupItem
	{
		public string Pattern { get; set; }

		public int? MinSize { get; set; }

		public int MaxDays { get; set; }

		public LoggerCleanupItem(string pattern, int? maxSize = 5000, int maxDays = 5)
		{
			Pattern = pattern;
			MinSize = maxSize;
			MaxDays = maxDays;
		}

		public override string ToString()
		{
			return $"{Pattern} {MinSize} {MaxDays}";
		}
	}
}
