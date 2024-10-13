using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Connection
{
	public class ItelexOutgoingConfiguration
	{
		//public int ConnectionId { get; set; }

		public LoginSeqTypes[] OutgoingType { get; set; }

		public int ItelexNumber { get; set; }

		/// <summary>
		/// Retries starting with 1
		/// </summary>
		public int? RetryCnt { get; set; }

		public bool AsciiMode { get; set; } = false;

		public string OurItelexVersionStr { get; set; }

		public string OurAnswerbackStr { get; set; }

		/// <summary>
		/// zweiter kg, zum beispiel sender-kg beim rundsender
		/// </summary>
		public string CustomAnswerbackStr { get; set; }

		public Func<string, int, int, bool> IsConnectionActive { get; set; }

	}
}
