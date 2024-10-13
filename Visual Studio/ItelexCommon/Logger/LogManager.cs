using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Logger
{
	public class LogManager
	{
		/// <summary>
		/// singleton pattern
		/// </summary>
		private static LogManager instance;
		
		public static LogManager Instance => instance ?? (instance = new LogManager());

		private LogManager()
		{
		}

		public Logging Logger { get; private set; }

		//public ItelexLogger ItelexLogger { get; private set; }

		public void SetLogger(string logPath, string logName, LogTypes logLevel)
		{
			Logger = new Logging(logPath, logName, logLevel);
		}

		public void SetLogger(string logPath, string logName, LogTypes logLevel, string sysLogHost, int sysLogPort, string appName)
		{
			Logger = new Logging(logPath, logName, logLevel, sysLogHost, sysLogPort, appName);
		}

		//public ItelexLogger SetItelexLogger(string logPath, string logName, int connectionId, ItelexConnection.ConnectionDirections dir, int? number, Acknowledge ack)
		//{
		//	Logger = new ItelexLogger(logPath, logName, connectionId, dir, number, ack);
		//}

		public void InitAutoCleanup(LoggerCleanupItem[] cleanupItems)
		{
			Logger.InitAutoCleanup(cleanupItems);
		}
	}
}
