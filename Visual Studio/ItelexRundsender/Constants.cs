using ItelexCommon;

namespace ItelexRundsender
{
	static class Constants
	{
		public const LogTypes LOG_LEVEL = LogTypes.Debug;

		public const string PROGRAM_NAME = "ItelexRundsender";
		public const string PRGMNAME_RUND_DE = "detlefs rundsender";
		public const string PRGMNAME_RUND_EN = "detlef's broadcast";
		public const string PRGMNAME_ADMIN_DE = "detlefs rundsender gruppenverw.";
		public const string PRGMNAME_ADMIN_EN = "detlef's broadcast group admin.";

		public const string APP_CODE = "rs";

		public const string DEFAULT_LNGSTR = "en";

		public const string SYSLOG_HOST = "192.168.0.1";
		public const int SYSLOG_PORT = 514;
		public const string SYSLOG_APPNAME = "Rundsender";

		public const string DEBUG_LOG = "itelexrundsender.log";

		public const int MAIL_SEND_RETRIES = 5;

#if DEBUG
		//public const string DEBUG_PATH = @".\";
		public const string LOG_PATH = @".\logs";
#else
		//public const string DEBUG_PATH = @"d:\daten\ItelexRundsender\logs";
		public const string LOG_PATH = @".\logs";
#endif

		public const string ANSWERBACK_RUND_DE = "11162 rs d";
		public const string ANSWERBACK_RUND_EN = "11163 rs d";
		public const string ANSWERBACK_ADMIN_DE = "11164 rsgv d";
		public const string ANSWERBACK_ADMIN_EN = "11165 rsgv d";

		public const int EXTNUM_RUND_DE = 11;
		public const int EXTNUM_RUND_EN = 12;
		public const int EXTNUM_ADMIN_DE = 13;
		public const int EXTNUM_ADMIN_EN = 14;

		public const int MAX_LINES = 200;

#if !DEBUG
		public const string DATABASE_NAME = @".\ItelexRundsender.sqlite";
		public const int ITELEX_NUMBER_RUND_DE = 11162;
		public const int ITELEX_NUMBER_RUND_EN = 11163;
		public const int ITELEX_NUMBER_ADMIN_DE = 11164;
		public const int ITELEX_NUMBER_ADMIN_EN = 11164;
		public const int PUBLIC_PORT = 8140;
		public const int LOCAL_PORT = 8140;
		//public const int EXTENSION = 11;
		public const int ITELEX_PIN = 0000;
		public const bool FIX_DNS = true;
#else
		public const string DATABASE_NAME = @"d:\daten\Itelex\ItelexRundsender\ItelexRundsender.sqlite";
		public const int ITELEX_NUMBER_RUND_DE = PrivateConstants.ITELEX_DEBUG_NUMBER;
		public const int ITELEX_NUMBER_RUND_EN = PrivateConstants.ITELEX_DEBUG_NUMBER;
		public const int ITELEX_NUMBER_ADMIN_DE = PrivateConstants.ITELEX_DEBUG_NUMBER;
		public const int ITELEX_NUMBER_ADMIN_EN = PrivateConstants.ITELEX_DEBUG_NUMBER;
		public const int PUBLIC_PORT = 8135;
		public const int LOCAL_PORT = 8135;
		//public const int EXTENSION = 12;
		public const int ITELEX_PIN = PrivateConstants.ITELEX_DEBUG_PIN;
		public const bool FIX_DNS = false;
#endif

		public const int MONITOR_PORT = 9140;
	}
}
