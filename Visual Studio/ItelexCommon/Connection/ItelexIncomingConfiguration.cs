using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Connection
{
	public enum LoginSeqTypes
	{
		SendTime = 1,
		SendKg = 2,
		SendCustomKg = 3,
		GetKg = 4,
		GetNumber = 5,
		GetNumberAndPin = 6,
		GetShortName = 7,
	}

	public class ItelexIncomingConfiguration
	{
		/// <summary>
		/// Programm version string send as start message behind die program name
		/// </summary>
		public string OurPrgmVersionStr { get; set; }


		/// <summary>
		/// Itelex protocol version string send by Version command (format: ssnnn)
		/// </summary>
		public string OurItelexVersionStr { get; set; }

		public string OverwriteAnswerbackStr { get; set; }

		public ItelexExtensionConfiguration[] ItelexExtensions { get; set; }

		public string LogPath { get; set; }

		public AllLoginSequences LoginSequences { get; set; }

		public ILoginItem LoginItem { get; set; }

		/// <summary>
		/// load account: ILoginItem LoadLoginItem(int itelexNumber)
		/// </summary>
		public Func<int, ILoginItem> LoadLoginItem { get; set; }

		/// <summary>
		/// update account: bool UpdateLoginItem(ILoginItem item)
		/// </summary>
		public Func<ILoginItem, bool> UpdateLoginItem { get; set; }

		/// <summary>
		/// add new account: bool AddAccount(int itelexNumber)
		/// </summary>
		public Func<int, bool> AddAccount { get; set; }

		/// <summary>
		/// Send new pin: bool SendNewPin(int itelexNumber)
		/// </summary>
		public Func<int, bool> SendNewPin { get; set; }

		/// <summary>
		/// Check if the short name is valid: bool CheckShortNameIsValid(string shortName)
		/// </summary>
		public Func<string, bool> CheckShortNameIsValid { get; set; }

		public Dictionary<IncomingTexts, int> LngKeyMapper { get; set; }

		/// <summary>
		/// Get text by LngKey with params: string GetLngText(int key, string[] prms)
		/// </summary>
		public Func<int, string[], string> GetLngText { get; set; }
	}
}
