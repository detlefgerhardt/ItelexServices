using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Connection
{
	/// <summary>
	/// Holds all login sequences
	/// </summary>
	public class AllLoginSequences
	{
		public LoginSequenceForExtensions[] LoginSequences { get; set; }

		public AllLoginSequences(params LoginSequenceForExtensions[] loginSequences)
		{
			LoginSequences = loginSequences;
		}

		public bool Contains(int extension, LoginSeqTypes loginType)
		{
			LoginSequenceForExtensions lt = (from l in LoginSequences
											 where l.Extensions == null || l.Extensions.Contains(extension)
											 select l).FirstOrDefault();
			if (lt == null) return false; // invalid extension number
			return lt.LoginSequence.Contains(loginType);
		}
	}
}
