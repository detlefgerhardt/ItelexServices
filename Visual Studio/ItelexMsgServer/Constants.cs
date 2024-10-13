using ItelexCommon;

namespace ItelexMsgServer
{
	static class Constants
	{
		public const LogTypes LOG_LEVEL = LogTypes.Debug;

		public const string PROGRAM_NAME = "ItelexMsgServer";
		public const string PROGRAM_SHORTNAME_REG_DE = "message-service";
		public const string PROGRAM_SHORTNAME_REG_EN = "message service";

		public const string APP_CODE = "ms";

		public const string SYSLOG_HOST = "192.168.0.1";
		public const int SYSLOG_PORT = 514;
		public const string SYSLOG_APPNAME = "MsgService";
		public const string DEBUG_EMAIL_ADDRESS = "mail@dgerhardt.de";

#if DEBUG
		public const string LOG_PATH = @".\logs";
		public const string DEBUG_LOG = "ItelexMsgServer.log";
		public const string MAILKIT_LOG = null;
		//public const string MAILKIT_LOG = "MailKit.log";
		public const string DATABASE_NAME = @"d:\daten\Itelex\ItelexMsgServer\ItelexMsgServer.sqlite";
		//public const string EMAIL_ADDRESS = "test@telexgate.de";
		public const string EMAIL_ADDRESS = "telex@telexgate.de";
		public const string EMAIL_PASSWORD = PrivateConstants.MSGSRV_MAILPASSWORD;
		//public const string FAX_PATH = @"d:\daten\Itelex\ItelexMsgServer\";
		public const string PRUEFTEXTE_PATH = @"d:\daten\Itelex\ItelexMsgServer\prueftexte.txt";
#else
		public const string LOG_PATH = @".\logs";
		public const string DEBUG_LOG = "ItelexMsgServer.log";
		public const string MAILKIT_LOG = null;
		public const string DATABASE_NAME = "ItelexMsgServer.sqlite";
		public const string EMAIL_ADDRESS = "telex@telexgate.de";
		public const string EMAIL_PASSWORD = PrivateConstants.TELEX_TELEXGATE_PASSWORD;
		//public const string FAX_PATH = @".\fax\";
		public const string PRUEFTEXTE_PATH = @".\prueftexte.txt";
#endif

		public const string POP3_HOST = "pop.1und1.de";
		public const int POP3_PORT = 995;
		public const string IMAP_HOST = "imap.1und1.de";
		public const int IMAP_PORT = 993;

		public const int ITELEX_SEND_RETRIES = 5;
		public const int ITELEX_SEND_RETRY_DELAY_SEC = 5 * 60; // 5 minutes
		public const int MAX_MAILS_PER_DAY = 10;
		public const int MAX_LINES_PER_DAY = 500;
		public const int MAX_PENDING_MAILS = 10;

		public const string ANSWERBACK_DE = "11170 msg d";
		public const string ANSWERBACK_EN = "11171 msg d";
		//public const string ANSWERBACK_SENDMAIL = "11172 mailsend d";
		//public const string ANSWERBACK_SENDFAX = "11173 faxsend d";

		public const int DEFAULT_MONITOR_PORT = 9142;

#if DEBUG
		public const string TELEGRAM_BOT_TOKEN = PrivateConstants.TELEGRAM_TESTBOT_TOKEN; // itelex_test_bot
#else
		public const string TELEGRAM_BOT_TOKEN = PrivateConstants.TELEGRAM_BOT_TOKEN; // itelex_bot
#endif

#if DEBUG
		public const bool FIX_DNS = false;
		public const int DEFAULT_NUMBER_DE = 905259;
		public const int DEFAULT_NUMBER_EN = 905259;
		//public const int DEFAULT_NUMBER_SENDMAIL = 905259;
		//public const int DEFAULT_NUMBER_SENDFAX = 905259;
		public const int DEFAULT_PUBLIC_PORT = 8135;
		public const int DEFAULT_LOCAL_PORT = 8135;
		public const int DEFAULT_PIN = ITELEX_905259_PIN;
		public const int MINITELEX_LOCAL_PORT = 10000;
		public const int MINITELEX_PUBLIC_PORT = 50000;
		public const string OWN_FAX_NUMBER = PrivateConstants.MSGSRV_FAXNUMBER;
#else
		public const bool FIX_DNS = true;
		public const int DEFAULT_NUMBER_DE = 11170;
		public const int DEFAULT_NUMBER_EN = 11171;
		//public const int DEFAULT_NUMBER_SENDMAIL = 11172;
		//public const int DEFAULT_NUMBER_SENDFAX = 11173;
		public const int DEFAULT_PUBLIC_PORT = 8142;
		public const int DEFAULT_LOCAL_PORT = 8142;
		public const int DEFAULT_PIN = 0000;
		public const int MINITELEX_LOCAL_PORT = 10000;
		public const int MINITELEX_PUBLIC_PORT = 50000;
		public const string OWN_FAX_NUMBER = PrivateConstants.MSGSRV_FAXNUMBER;
#endif
	}
}
