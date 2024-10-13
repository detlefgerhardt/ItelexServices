using System;

namespace ItelexCommon
{
	public interface ILogger
	{
		void Init(string logPath, string logName);

		void Debug(string section, string method, string text);

		void Info(string section, string method, string text);

		void Warn(string section, string method, string text);

		void Error(string section, string method, string text);

		void Error(string section, string method, string text, Exception ex = null);

		void Fatal(string section, string method, string text);

		void Log(LogTypes logType, string section, string method, string text, bool show = true);

		void OnLog(LogArgs e);
	}
}
