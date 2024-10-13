using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexBildlocher
{
	class Constants
	{
		public const LogTypes LOG_LEVEL = LogTypes.Debug;

		public const string PROGRAM_NAME = "ItelexBildlocher";
		public const string PROGRAM_SHORTNAME = "detlefs bildlocher";
		public const string DEBUG_LOG = "ItelexBildlocher.log";
		public const string DEFAULT_ANSWERBACK = "11174 bild d";
		public const string APP_CODE = "ws";

		public const string SYSLOG_HOST = "192.168.0.1";
		public const int SYSLOG_PORT = 514;
		public const string SYSLOG_APPNAME = "BildLocher";

#if !DEBUG
		public const string LOG_PATH = @".\logs";
		public const int DEFAULT_NUMBER = 11174;
		public const int DEFAULT_PUBLIC_PORT = 8144;
		public const int DEFAULT_LOCAL_PORT = 8144;
		public const int DEFAULT_EXTENSION = 11;
		public const int DEFAULT_PIN = 0000;
		public const bool FIX_DNS = true;
#else
		public const string LOG_PATH = @".\logs";
		public const int DEFAULT_NUMBER = 905259;
		public const int DEFAULT_PUBLIC_PORT = 8135;
		public const int DEFAULT_LOCAL_PORT = 8135;
		public const int DEFAULT_EXTENSION = 12;
		public const int DEFAULT_PIN = PrivateConstants.ITELEX_905259_PIN;
		public const bool FIX_DNS = false;
#endif

		public const int DEFAULT_MONITOR_PORT = 9144;
	}
}
