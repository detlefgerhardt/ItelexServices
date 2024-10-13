using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatGptServer
{
	class Constants
	{
		public const LogTypes LOG_LEVEL = LogTypes.Debug;

		public const string PROGRAM_NAME = "ItelexChatGptServer";
		public const string PROGRAM_SHORTNAME_DE = "chatgpt-service (de)";
		public const string PROGRAM_SHORTNAME_EN = "chatgpt-service (en)";
		public const string DEFAULT_ANSWERBACK_DE = "11168 chatgpt d";
		public const string DEFAULT_ANSWERBACK_EN = "11169 chatgpt d";
		public const string APP_CODE = "cg";

		public const string SYSLOG_HOST = "192.168.0.1";
		public const int SYSLOG_PORT = 514;
		public const string SYSLOG_APPNAME = "ChatGptService";

#if DEBUG
		//public const string LOG_PATH = @".\";
		public const string LOG_PATH = @"d:\Daten\Itelex\ItelexChatGptServer\logs";
		public const string DEBUG_LOG = "itelexchatgtpserver.log";
		//public const string DATABASE_NAME = @"d:\daten\Itelex\ItelexNewsServer\ItelexNewsServer.sqlite";
		//public const string DATABASE_NAME = @"ItelexNewsServer.sqlite";
#else
		public const string LOG_PATH = @".\logs";
		public const string DEBUG_LOG = "itelexchatgtpserver.log";
		//public const string DATABASE_NAME = @"ItelexNewsServer.sqlite";
#endif

#if !DEBUG
		public const bool FIX_DNS = true;
		public const int DEFAULT_NUMBER_DE = 11168;
		public const int DEFAULT_NUMBER_EN = 11169;
		public const int DEFAULT_PUBLIC_PORT = 8145;
		public const int DEFAULT_LOCAL_PORT = 8145;
		public const int DEFAULT_EXTENSION_DE = 11;
		public const int DEFAULT_EXTENSION_EN = 12;
		public const int DEFAULT_PIN = 0000;
#else
		public const bool FIX_DNS = false;
		public const int DEFAULT_NUMBER_DE = 905259;
		public const int DEFAULT_NUMBER_EN = 905259;
		public const int DEFAULT_PUBLIC_PORT = 8135;
		public const int DEFAULT_LOCAL_PORT = 8135;
		public const int DEFAULT_EXTENSION_DE = 11;
		public const int DEFAULT_EXTENSION_EN = 12;
		public const int DEFAULT_PIN = PrivateConstants.ITELEX_905259_PIN;
#endif

		public const int DEFAULT_MONITOR_PORT = 9145;
	}
}
