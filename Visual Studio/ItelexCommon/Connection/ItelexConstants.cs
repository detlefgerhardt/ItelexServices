using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Connection
{
	public static class ItelexConstants
	{
		public const int DEFAULT_IDLE_TIMEOUT_SEC = 0; // no timeout
		public const int WAIT_BEFORE_SEND_MSEC = 50; // 100 ms
		public const int SEND_TIMER_INTERVAL = 40; // 50 ms
		public const int ITELIX_SENDBUFFER_SIZE = 16; // 16 characters
		public const int ITELIX_ACKBUFFER_SIZE = 16; // 16 characters

		public const int BU_REFRESH_SEC = 9 * 60; // 9:00 min
		public const int TLNSERVER_REFRESH_SEC = 10 * 60; // 10 min
		public const int WRITE_TIMEOUT_SEC = 30; // 30 sec
	}
}
