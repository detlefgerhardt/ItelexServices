using System;
using System.IO;
using System.Threading.Tasks;

namespace ItelexCommon
{
	enum LoggerTypes { None = 0, Fatal = 1, Error = 2, Warn = 3, Info = 4, Debug = 5, Itelex = 100 };

	//[Serializable]
	class Logger: ILogger
	{
		public delegate void RecvLogEvent(object sender, LogArgs e);
		public event RecvLogEvent RecvLog;

		public LogTypes LogLevel { get; set; }

		public string LogfilePath { get; set; }

		private object _lock = new object();

		private string _logPath;

		private string _logName;

		public Logger()
		{
			LogLevel = LogTypes.Debug;
		}

		public void Init(string logPath, string logName)
		{
			_logPath = logPath;
			_logName = logName;
		}

		public void Debug(string section, string method, string text)
		{
			Log(LogTypes.Debug, section, method, text);
		}

		public void Info(string section, string method, string text)
		{
			Log(LogTypes.Info, section, method, text);
		}

		public void Warn(string section, string method, string text)
		{
			Log(LogTypes.Warn, section, method, text);
		}

		public void Error(string section, string method, string text)
		{
			Log(LogTypes.Error, section, method, text);
		}

		public void Error(string section, string method, string text, Exception ex = null)
		{
			if (ex != null)
			{
				text = $"{text} result={ex.HResult} {ex.Message}";
			}
			Log(LogTypes.Error, section, method, text);
		}

		public void Fatal(string section, string method, string text)
		{
			Log(LogTypes.Fatal, section, method, text);
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
				string fullName = "";
				int? id = Task.CurrentId;
				if (!id.HasValue)
				{
					System.Diagnostics.Debug.Write("");
				}
				string prefix = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} {logType.ToString().PadRight(5)} [{Task.CurrentId}] [{section}]";
				string logStr = $"{prefix} [{method}] {text}\r\n";
				try
				{
					string path = string.IsNullOrWhiteSpace(LogfilePath) ? _logPath : LogfilePath;
					fullName = Path.Combine(path, _logName);
					File.AppendAllText(fullName, logStr);
				}
				catch
				{
					// try to log in program directory
					string newName = Path.Combine(_logPath, _logName);
					File.AppendAllText(newName, $"{prefix} [AppendLog] Error writing logfile to {fullName}\r\n");
					File.AppendAllText(newName, logStr);
				}
			}
		}

		private bool IsActiveLevel(LogTypes current)
		{
			return (int)current <= (int)LogLevel;
		}
	}

	public class LoggerArgs : EventArgs
	{
		public LogTypes LogType { get; set; }

		public string Section { get; set; }

		public string Method { get; set; }

		public string Message { get; set; }

		public LoggerArgs(LogTypes logType, string section, string method, string msg)
		{
			LogType = logType;
			Section = section;
			Method = method;
			Message = msg;
		}
	}

}
