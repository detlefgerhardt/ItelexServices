using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Data
{
	public enum EmailTypes { Pop3 = 1, Imap = 2};

	[Serializable]
	class EmailAccountItem
	{
		[SqlId]
		public Int64 UserId { get; set; }

		[SqlInt]
		public int ItelexNumber { get; set; }

		[SqlString]
		public string Server { get; set; }

		[SqlTinyInt]
		public int AccountType { get; set; }

		[SqlSmallInt]
		public int Port { get; set; }

		[SqlString]
		public string MailAddress { get; set; }

		[SqlString]
		public string Username { get; set; }

		[SqlString]
		public string Password { get; set; }

		[SqlBool]
		public bool DeleteAfterRead	 { get; set; }

		[SqlBool]
		public bool Active { get; set; }

		public override string ToString()
		{
			return $"{UserId} {ItelexNumber} {AccountType} {MailAddress}"; ;
		}
	}
}
