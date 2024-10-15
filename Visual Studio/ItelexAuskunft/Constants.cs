using ItelexCommon;

namespace ItelexAuskunft
{
	static class Constants
	{
		public const LogTypes LOG_LEVEL = LogTypes.Debug;

		public const string PROGRAM_NAME = "ItelexAuskunft";
		public const string PROGRAM_SHORTNAME = "hist. auskunft";
		public const string APP_CODE = "as";

		public const string DEBUG_LOG = "itelexauskunft.log";

		//public const string TABLE_PATH = @"c:\\Itelex\";

		public const string DEFAULT_ANSWERBACK = "40140 txinf d";

		//public const string DEFAULT_TLN_SERVER_ADDR1 = "sonnibs.no-ip.org";
		//public const string DEFAULT_TLN_SERVER_ADDR2 = "telexgateway.de";
		//public const int DEFAULT_TLN_SERVER_PORT = 11811;

		public const string SYSLOG_HOST = "192.168.0.1";
		public const int SYSLOG_PORT = 514;
		public const string SYSLOG_APPNAME = "HistAuskunft";

#if !DEBUG
		public const string LOG_PATH = @".\logs";
		public const bool FIX_DNS = true;
		public const int DEFAULT_NUMBER = 40140;
		public const int DEFAULT_PUBLIC_PORT = 8137;
		public const int DEFAULT_LOCAL_PORT = 8137;
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

		public const int DEFAULT_MONITOR_PORT = 9137;
	}
}
