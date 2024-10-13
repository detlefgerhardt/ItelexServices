using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon
{
	public enum CallStatusEnum
	{
		None = 0,
		Ok = 1,
		QueryError = 2,
		NoConn = 3,
		Aborted = 4,
		Error = 5,
		Reject = 6,
		InProgress = 7,
		AlreadyActive = 8,
		//RejectNc = 6,
		//RejectNa = 7,
		//RejectOcc = 8,
		//RejectOther = 9,
	}

	public class CallResult
	{
		public const string CR_DE_OK = "ok";
		public const string CR_DE_QUERYERROR = "adressfehler";
		//public const string CR_QUERYERROR = "keine verb.";
		public const string CR_DE_NOCONN = "keine ip-verb.";
		public const string CR_DE_ABORTED = "abbruch";
		public const string CR_DE_ERROR = "fehler";
		public const string CR_DE_NC = "nc";
		public const string CR_DE_NA = "na";
		public const string CR_DE_OCC = "occ";
		public const string CR_DE_INPROGRESS = "aktiv";

		public const string CR_EN_OK = "ok";
		public const string CR_EN_QUERYERROR = "query error";
		//public const string CR_QUERYERROR = "keine verb.";
		public const string CR_EN_NOCONN = "no ip conn.";
		public const string CR_EN_ABORTED = "aborted";
		public const string CR_EN_ERROR = "error";
		public const string CR_EN_NC = "nc";
		public const string CR_EN_NA = "na";
		public const string CR_EN_OCC = "occ";
		public const string CR_EN_INPROGRESS = "active";

		public int Number { get; }

		public CallStatusEnum CallStatus { get; }

		public Answerback Kennung { get; }

		public string FirmwareVersion { get; }

		public string RejectReason { get; }

		public bool Connected => Connection != null ? Connection.IsConnected : false;

		public ItelexConnection Connection { get; set; }

		//public CallResult(int number, ItelexConnection connection, CallStatusEnum callStatus, string rejectReason, string firmwareVersion, Answerback kennung)
		//{
		//	Connection = connection;
		//	Number = number;
		//	CallStatus = callStatus;
		//	Kennung = kennung;
		//	FirmwareVersion = firmwareVersion;
		//	CallStatus = callStatus;
		//	RejectReason = rejectReason;
		//}

		public CallResult(int number, ItelexConnection connection, CallStatusEnum callStatus, string rejectReason,
			string firmwareVersion, Answerback kennung)
		{
			Connection = connection;
			Number = number;
			CallStatus = callStatus;
			RejectReason = rejectReason == null ? "" : rejectReason;
			FirmwareVersion = firmwareVersion;
			Kennung = kennung;
		}

		public bool IsReject(string rejectReason)
		{
			if (CallStatus != CallStatusEnum.Reject) return false;
			return string.Compare(rejectReason, RejectReason, true) == 0;
		}

		/*
		public CallResult(int number, CallStatusEnum callStatus, string rejectReason, string firmwareVersion)
		{
			Connection = null;
			Number = number;
			CallStatus = callStatus;
			Kennung = null;
			FirmwareVersion = firmwareVersion;
			RejectReason = rejectReason;
		}
		*/

		public string CallStatusAsString(string lngStr)
		{
			return CallStatusToString(CallStatus, RejectReason, lngStr);
		}

		public static string CallStatusToString(CallStatusEnum callStatus, string rejectReason, string lngStr)
		{
			lngStr = lngStr.ToLower();

			switch (lngStr)
			{
				case "de":
					switch (callStatus)
					{
						case CallStatusEnum.None:
							return "";
						case CallStatusEnum.InProgress:
							return CR_DE_INPROGRESS;
						case CallStatusEnum.Ok:
							return CR_DE_OK;
						case CallStatusEnum.QueryError:
							return CR_DE_QUERYERROR;
						case CallStatusEnum.NoConn:
							return CR_DE_NOCONN;
						case CallStatusEnum.Aborted:
							return CR_DE_ABORTED;
						case CallStatusEnum.Reject:
							return $"{rejectReason?.ToLower()}";
						case CallStatusEnum.Error:
							return CR_EN_ERROR;
						default:
							return callStatus.ToString();
					}
				case "en":
					switch (callStatus)
					{
						case CallStatusEnum.None:
							return "";
						case CallStatusEnum.InProgress:
							return CR_EN_INPROGRESS;
						case CallStatusEnum.Ok:
							return CR_EN_OK;
						case CallStatusEnum.QueryError:
							return CR_EN_QUERYERROR;
						case CallStatusEnum.NoConn:
							return CR_EN_NOCONN;
						case CallStatusEnum.Aborted:
							return CR_EN_ABORTED;
						case CallStatusEnum.Reject:
							return $"{rejectReason?.ToLower()}";
						case CallStatusEnum.Error:
							return CR_EN_ERROR;
						default:
							return callStatus.ToString(); 
					}
				default:
					return "";
			}
		}

		public override string ToString()
		{
			return $"{CallStatus} {RejectReason} {FirmwareVersion} '{Kennung}'";
		}
	}
}
