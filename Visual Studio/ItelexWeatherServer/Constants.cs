using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexWeatherServer
{
	class Constants
	{
		public const LogTypes LOG_LEVEL = LogTypes.Debug;

		public const string PROGRAM_NAME = "ItelexWeatherServer";
		public const string PROGRAM_SHORTNAME = "wetter-service (dwd)";

		public const string DEBUG_LOG = "ItelexWeatherServer.log";
		public const string DEFAULT_ANSWERBACK = "717171 wetter d";
		public const string APP_CODE = "ws";

		public const string SYSLOG_HOST = "192.168.0.1";
		public const int SYSLOG_PORT = 514;
		public const string SYSLOG_APPNAME = "WeatherService";

#if !DEBUG
		public const string LOG_PATH = @".\logs";
		public const bool FIX_DNS = true;
		public const int DEFAULT_NUMBER = 717171;
		public const int DEFAULT_PUBLIC_PORT = 8143;
		public const int DEFAULT_LOCAL_PORT = 8143;
		public const int DEFAULT_EXTENSION = 11;
		public const int DEFAULT_PIN = 0000;
#else
		public const string LOG_PATH = @".\logs";
		public const bool FIX_DNS = false;
		public const int DEFAULT_NUMBER = PrivateConstants.ITELEX_DEBUG_NUMBER;
		public const int DEFAULT_PUBLIC_PORT = 8135;
		public const int DEFAULT_LOCAL_PORT = 8135;
		public const int DEFAULT_EXTENSION = 12;
		public const int DEFAULT_PIN = PrivateConstants.ITELEX_DEBUG_PIN;
#endif

		public const int DEFAULT_MONITOR_PORT = 9143;
	}
}
