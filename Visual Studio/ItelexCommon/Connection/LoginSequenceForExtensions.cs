using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Connection
{
	/// <summary>
	/// Holds the login sequence for a list of extension number
	/// </summary>
	public class LoginSequenceForExtensions
	{
		/// <summary>
		/// Holds all extension numbers with this login squence
		/// If null than all extension numbers are valid
		/// </summary>
		public int[] Extensions { get; set; }

		public LoginSeqTypes[] LoginSequence { get; set; }

		public LoginSequenceForExtensions(int[] extensions, params LoginSeqTypes[] loginSeq)
		{
			Extensions = extensions;
			LoginSequence = loginSeq;
		}
	}
}
