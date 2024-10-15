using ItelexCommon;

namespace ItelexBaudotArtServer
{
	static class Constants
	{
		public const LogTypes LOG_LEVEL = LogTypes.Debug;

		public const string PROGRAM_NAME = "ItelexBaudotArtServer";
		public const string PROGRAM_SHORTNAME = "detlefs baudotart-service";

		public const string APP_CODE = "ba";

		public const string SYSLOG_HOST = "192.168.0.1";
		public const int SYSLOG_PORT = 514;
		public const string SYSLOG_APPNAME = "BaudotArtService";

		public const string DEBUG_LOG = "itelexbaudotart.log";

		//public const string TABLE_PATH = @"c:\\Itelex\";
		public const string FILE_PATH = @"files\";
		public const string FILES_NAME = "files.xml";
		public const string RECV_PATH = @"recv\";

		public const string DEFAULT_ANSWERBACK = "11166 baudart d";

		//public static readonly string[] DEFAULT_TLN_SERVER_ADDR = new string[] { "sonnibs.no-ip.org", "telexgateway.de" };
		//public const int DEFAULT_TLN_SERVER_PORT = 11811;

#if !DEBUG
		public const string LOG_PATH = @".\logs";
		public const bool FIX_DNS = true;
		public const int DEFAULT_NUMBER = 11166;
		public const int DEFAULT_PUBLIC_PORT = 8139;
		public const int DEFAULT_LOCAL_PORT = 8139;
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

		public const int DEFAULT_MONITOR_PORT = 9139;
	}
}
