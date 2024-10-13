using ItelexCommon;

namespace ItelexNewsServer
{
	static class Constants
	{
		public const LogTypes LOG_LEVEL = LogTypes.Debug;

		public const string PROGRAM_NAME = "ItelexNewsServer";
		public const string PROGRAM_SHORTNAME = "news-service";

		public const string APP_CODE = "ns";

		public const string SYSLOG_HOST = "192.168.0.1";
		public const int SYSLOG_PORT = 514;
		public const string SYSLOG_APPNAME = "NewsService";

#if DEBUG
		public const string LOG_PATH = @"d:\Daten\Itelex\ItelexNewsServer\logs";
		public const string DEBUG_LOG = "itelexnewsserver.log";
		public const string DATABASE_NAME = @"d:\daten\Itelex\ItelexNewsServer\ItelexNewsServer.sqlite";
		//public const string MAILKIT_LOG = "MailKit.log";
		public const string MAILKIT_LOG = null;
#else
		public const string LOG_PATH = @".\logs";
		public const string DEBUG_LOG = "ItelexNewsServer.log";
		public const string DATABASE_NAME = "ItelexNewsServer.sqlite";
		//public const string MAILKIT_LOG = "MailKit.log";
		public const string MAILKIT_LOG = null;
#endif

		public const string ANSWERBACK_DE = "11180 news d";
		public const string ANSWERBACK_EN = "11181 news d";

		public const int MAX_PENDING_NEWS = 10;
		public const int MAX_MSG_SEND_RETRIES = 5;

		public const int DEFAULT_MONITOR_PORT = 9141;

#if DEBUG
		public const bool FIX_DNS = false;
		public const int DEFAULT_NUMBER_DE = 905259;
		public const int DEFAULT_NUMBER_EN = 905259;
		public const int DEFAULT_PUBLIC_PORT = 8135;
		public const int DEFAULT_LOCAL_PORT = 8135;
		public const int DEFAULT_PIN = PrivateConstants.ITELEX_905259_PIN;
#else
		public const bool FIX_DNS = true;
		public const int DEFAULT_NUMBER_DE = 11180;
		public const int DEFAULT_NUMBER_EN = 11181;
		public const int DEFAULT_NUMBER = 11180;
		public const int DEFAULT_PUBLIC_PORT = 8141;
		public const int DEFAULT_LOCAL_PORT = 8141;
		public const int DEFAULT_PIN = 0000;
#endif
	}
}
