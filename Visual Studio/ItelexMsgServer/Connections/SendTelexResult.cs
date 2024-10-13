using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Connections
{
	class SendTelexResult
	{
		public enum Results
		{
			Ok,
			Rejected,
			Unknown,
			Error
		}

		public Results Result { get; set; }

		public string RejectReason { get; set; }

		public string Kennung { get; set; }

		public SendTelexResult(Results result, string rejectReason, string kennung = null)
		{
			Result = result;
			RejectReason = rejectReason;
			Kennung = kennung;
		}
	}
}
