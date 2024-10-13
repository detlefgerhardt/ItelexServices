using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Mail
{
	public class SmtpConfig
	{
		public string SmtpHost { get; set; }
		public int SmtpPort { get; set; }
		public bool UseSsl { get; set; }
		public string EmailAccount { get; set; }
		public string Password { get; set; }
		public string From { get; set; }
		public SecureSocketOptions SecureSocketOptions { get; set; }
	}
}
