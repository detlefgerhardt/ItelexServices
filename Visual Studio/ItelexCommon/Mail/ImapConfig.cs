using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Mail
{
	public class ImapConfig
	{
		public string ImapHost { get; set; }
		public int ImapPort { get; set; }
		public bool UseSsl { get; set; }
		public string EmailAccount { get; set; }
		public string Password { get; set; }
		public string[] SearchFolders { get; set; }
		public string ArchiveFolder { get; set; }

	}
}
