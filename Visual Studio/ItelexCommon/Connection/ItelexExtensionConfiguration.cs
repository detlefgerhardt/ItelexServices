using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Connection
{
	public class ItelexExtensionConfiguration
	{
		public int? ExtensionNumber { get; set; }

		public int ItelexNumber { get; set; }

		public string Language { get; set; }

		public string ServiceName { get; set; }

		public string ServiceAnswerback { get; set; }

		public bool Default { get; set; }

		public ItelexExtensionConfiguration(int? extNum, int itelexNum, string lng, string srvName, string srvAnswerback, bool defExt)
		{
			ExtensionNumber = extNum;
			ItelexNumber = itelexNum;
			Language = lng;
			ServiceName = srvName;
			ServiceAnswerback = srvAnswerback;
			Default = defExt;
		}

		public override string ToString()
		{
			return $"{ExtensionNumber} {ItelexNumber} {Language}";
		}
	}
}
