using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ItelexCommon.CallResult;

namespace ItelexRundsender.Connections
{
	class Receiver
	{
		public int Number { get; set; }

		public OutgoingConnection Connection { get; set; }

		public CallStatusEnum CallStatus { get; set; }

		public string RejectReason { get; set; }

		public string Kennung1 { get; set; }

		public string Kennung2 { get; set; }

		public bool Remove { get; set; }

		public DateTime? ConnectTime { get; set; }

		public string ResultStr1 => CallStatus == CallStatusEnum.Ok || CallStatus == CallStatusEnum.InProgress ? Kennung1 : RejectReason;
		public string ResultStr2 => CallStatus == CallStatusEnum.Ok || CallStatus == CallStatusEnum.InProgress ? Kennung2 : RejectReason;

		public Receiver(int number, bool remove)
		{
			Number = number;
			Remove = remove;
			ConnectTime = null;
			RejectReason = null;
		}

		public bool IsReject(string rejectReason)
		{
			if (CallStatus != CallStatusEnum.Reject) return false;
			return string.Compare(rejectReason, RejectReason, true) == 0;
		}

		public static int ReceiverNumbersIndexOf(List<Receiver> list, int num)
		{
			if (list == null) return -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Number == num) return i;
			}
			return -1;
		}

		public static bool IsCorrection(string str)
		{
			foreach (string ending in ParseNumbersResult.CorrectionString)
			{
				if (str.EndsWith(ending)) return true;
			}
			return false;
		}

		public override string ToString()
		{
			return $"{Number} {CallStatus} {RejectReason} kg1={Kennung1} kg2={Kennung2} remove={Remove}";
		}
	}
}
