using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon
{
	public class GlobalData
	{
		public IncomingConnectionManagerAbstract IncomingConnectionManager { get; set; }

		public OutgoingConnectionManagerAbstract OutgoingConnectionManager { get; set; }

		public ItelexExtensionConfiguration[] ItelexValidExtensions { get; set; }

		public Logging Logger { get; set; }

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static GlobalData instance;

		public static GlobalData Instance => instance ?? (instance = new GlobalData());

		public string GetValidExtensionsStr()
		{
			if (ItelexValidExtensions == null || ItelexValidExtensions.Length == 0) return "?";
			int?[] extNums = (from e in ItelexValidExtensions select e.ExtensionNumber).ToArray();
			string joinStr = "";
			foreach(int? extNum in extNums)
			{
				if (joinStr != "") joinStr += ",";
				joinStr += extNum.HasValue ? extNum.Value.ToString() : "";
			}
			return joinStr;
		}
	}
}
