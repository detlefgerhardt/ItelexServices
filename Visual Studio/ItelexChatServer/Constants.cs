using ItelexCommon;

namespace ItelexChatServer
{
	static class Constants
	{
		public const LogTypes LOG_LEVEL = LogTypes.Debug;

		public const string PROGRAM_NAME = "ItelexChatServer";
		public const string PROGRAM_SHORTNAME_DE = "konferenzdienst";
		public const string PROGRAM_SHORTNAME_EN = "conference service";

		public const string APP_CODE = "cs";

		public const string SYSLOG_HOST = "192.168.0.1";
		public const int SYSLOG_PORT = 514;
		public const string SYSLOG_APPNAME = "ChatService";

		public const string DEBUG_LOG = "itelexchatserver.log";

		public const string ANSWERBACK_DE = "11160 konf d";
		public const string ANSWERBACK_EN = "11161 konf d";

		public const bool PRINT_TIME = true;

#if DEBUG
		public const string LOG_PATH = @".\logs";
		public const bool FIX_DNS = false;
		public const int DEFAULT_NUMBER = 905259;
		public const int DEFAULT_PUBLIC_PORT = 8135;
		public const int DEFAULT_LOCAL_PORT = 8135;
		public const int DEFAULT_PIN = PrivateConstants.ITELEX_905259_PIN;
#else
		public const string LOG_PATH = @".\logs";
		public const bool FIX_DNS = true;
		public const int DEFAULT_NUMBER = 11160;
		public const int DEFAULT_PUBLIC_PORT = 8136;
		public const int DEFAULT_LOCAL_PORT = 8136;
		public const int DEFAULT_PIN = 0000;
#endif

		public const int DEFAULT_MONITOR_PORT = 9136;
	}
}
