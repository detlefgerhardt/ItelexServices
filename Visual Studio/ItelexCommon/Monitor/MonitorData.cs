using System;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ItelexCommon.Monitor
{
	public enum MonitorCmds { Ping = 1, Info = 2, Start = 3, Shutdown = 4, Error = 5 }

	[Serializable]
	public class MonitorRequest
	{
		public int ReqId { get; set; }

		public MonitorCmds ReqCmd { get; set; }

		public override string ToString()
		{
			return $"{ReqId} {ReqCmd}";
		}
	}

	[Serializable]
	public class MonitorResponse
	{
		public int RespId { get; set; }

		public MonitorCmds RespCmd { get; set; }

		public override string ToString()
		{
			return $"{RespId} {RespCmd}";
		}
	}

	[Serializable]
	public class MonitorResponseError : MonitorResponse
	{
		public int Error { get; set; }

		public MonitorResponseError(MonitorRequest req)
		{
			RespId = req.ReqId;
			RespCmd = req.ReqCmd;
		}

		public override string ToString()
		{
			return $"{RespId} {RespCmd} {Error}";
		}

	}

	[Serializable]
	public class MonitorResponseOk : MonitorResponse
	{
		public bool Ok { get; set; }

		public override string ToString()
		{
			return $"{base.ToString()} {Ok}";
		}

	}


	[Serializable]
	public class MonitorResponseInfo: MonitorResponse
	{
		public MonitorServerTypes PrgmType { get; set; }

		public string Version { get; set; }

		public int ItelexNumber { get; set; }

		public int ItelexLocalPort { get; set; }
		
		public int ItelexPublicPort { get; set; }

		public DateTime StartupTime { get; set; }

		public DateTime? LastLoginTime { get; set; }

		public string LastUser { get; set; }

		public int LoginCount { get; set; }

		public int LoginUserCount { get; set; }

		public MonitorServerStatus Status { get; set; }

		public MonitorResponseInfo()
		{
		}

		public MonitorResponseInfo(MonitorRequest req)
		{
			RespId = req.ReqId;
			RespCmd = req.ReqCmd;
		}

		public override string ToString()
		{
			return $"{base.ToString()} {PrgmType} {Status}";
		}

	}

	[Serializable]
	class CounterData
	{
		public int LoginCount { get; set; }

		public int LoginUserCount { get; set; }

		public DateTime? LastLoginTime { get; set; }

		public string LastUser { get; set; }

		public override string ToString()
		{
			return $"{LoginCount}/{LoginUserCount} {LastLoginTime} {LastUser}";
		}

	}
}
