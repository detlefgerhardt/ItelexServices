using ItelexCommon;
using ItelexRundsender.Languages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexRundsender.Connections
{
	class SendProperties
	{
		private const string TAG = nameof(SendProperties);

		public string CallerAnswerbackStr { get; set; }

		public int CallerNumber { get; set; }

		public LanguageIds CallerLanguageId { get; set; }

		public string CallerLanguageStr => LanguageDefinition.GetLanguageById(CallerLanguageId).ShortName;

		public List<Receiver> Receivers { get; set; }

		public string MessageText { get; set; }

		public bool NumberCheck { get; set; }

		public bool IncludeReceiverList { get; set; }

		public RundsendeModus SendMode { get; set; }

		public DateTime? SendTime { get; set; }

		public List<Receiver> SuccessReivers =>
			Receivers == null ? null : Receivers.Where(n => n.CallStatus == CallStatusEnum.Ok || n.CallStatus == CallStatusEnum.InProgress).ToList();

		public SendProperties()
		{
			Receivers = new List<Receiver>();
			MessageText = "";
			NumberCheck = true;
			IncludeReceiverList = false;
			SendMode = RundsendeModus.None;
			SendTime = null;
			CallerLanguageId = LanguageIds.de;
		}

		public string GetReceiverLines(int lineLength, string prefix)
		{
			//if (!IncludeReceiverList) return "";
			return GetReceiverLines(Receivers, lineLength, prefix);
		}

		public static string GetReceiverLines(List<Receiver> receivers, int lineLength, string prefix)
		{
			if (receivers == null || receivers.Count == 0) return "";

			string lines = "";
			string line = prefix != null ? prefix : "";
			foreach(Receiver recv in receivers)
			{
				string numStr = recv.Number.ToString();
				if (line.Length + numStr.Length + 1 >= lineLength)
				{
					lines += "\r\n" + line;
					line = "";
				}
				line += numStr + ",";
			}
			if (!string.IsNullOrEmpty(line)) lines += "\r\n" + line;
			return lines.Trim(new char[] { '\r', '\n', ',' });
		}

		public List<int> GetNumbers(bool occOnly)
		{
			return (from r in Receivers
					where !occOnly || r.IsReject("occ")
					select r.Number).ToList();
		}

		public List<Receiver> GetReceivers(bool occOnly)
		{
			return (from r in Receivers
					where !occOnly || r.IsReject("occ")
					select r).ToList();
		}

		public List<OutgoingConnection> GetConnections()
		{
			return Receivers.Select(n => n.Connection).ToList();
		}

		public string GetReportText(ReportTypes reportType)
		{
			StringBuilder sb = new StringBuilder();

			switch (reportType)
			{
				case ReportTypes.Intermediate:
				default:
					sb.Append($"\r\n{LngText(LngKeys.TransmissionReportIntermediate, CallerLanguageId)}");
					break;
				case ReportTypes.Final:
					sb.Append($"\r\n{LngText(LngKeys.TransmissionReportFinal, CallerLanguageId)}");
					break;
			}

			foreach (Receiver recv in Receivers)
			{
				string timeStr = recv.ConnectTime != null ? recv.ConnectTime.Value.ToString("HH:mm") : "-";
				string okStr = recv.CallStatus == CallStatusEnum.Ok ? "(ok)" : "";
				//string resultStr = recv.ResultStr2 == CallResult.CR_QUERYERROR ? "fehler" : recv.ResultStr2;
				string resultStr =
						recv.CallStatus == CallStatusEnum.Ok ?
						recv.ResultStr2 :
						CallResult.CallStatusToString(recv.CallStatus, recv.RejectReason, CallerLanguageStr);
				sb.Append(string.Format("\r\n{0,-9} {1,-5} {2} {3}", recv.Number.ToString() + ":", timeStr, resultStr, okStr));
			}

			if (reportType == ReportTypes.Intermediate)
			{
				sb.Append($"\r\n\r\n{LngText(LngKeys.TransmissionReportIntermediateHint, CallerLanguageId)}");
			}
			return sb.ToString();
		}

		public string LngText(LngKeys lngKey, LanguageIds lngId)
		{
			return LanguageManager.Instance.GetText((int)lngKey, (int)lngId);
		}

		public string GetMessageTextAscii()
		{
			if (string.IsNullOrEmpty(MessageText)) return "";

			string text = MessageText;
			while (text.Contains("\r\r"))
			{
				text = text.Replace("\r\r", "\r");
			}
			return text;
		}
	}
}
