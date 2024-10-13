using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ItelexNewsServer.News
{
	class WebClientEx : WebClient
	{
		public int Timeout { get; set; } = 10000; // 10 sec

		//private Uri _responseUri;

		//public Uri ResponseUri => _responseUri;

		protected override WebRequest GetWebRequest(Uri uri)
		{
			WebRequest lWebRequest = base.GetWebRequest(uri);
			lWebRequest.Timeout = Timeout;
			((HttpWebRequest)lWebRequest).ReadWriteTimeout = Timeout;
			return lWebRequest;
		}

		/*
		protected override WebResponse GetWebResponse(WebRequest request)
		{
			WebResponse response = base.GetWebResponse(request);
			_responseUri = response.ResponseUri;
			return response;
		}
		*/

		public static WebClientEx GetWebClient()
		{
			WebClientEx wClient = new WebClientEx();
			wClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:53.0) Gecko/20100101 Firefox/53.0");
			//wClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)");
			wClient.Headers.Add("Content-Type", "application / zip, application / octet - stream");
			//wClient.Headers.Add("Accept-Encoding", "gzip,deflate");
			wClient.Headers.Add("Accept-Language", "de,en-US;q=0.7,en;q=0.3");
			//wClient.Headers.Add("Referer", "http://Something");
			wClient.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
			//wClient.Headers.Add("Cookie", "__cfduid=df06a6600ec3932876e4a4f0177900a591490861379; _ga=GA1.2.1651624166.1490861383; _ga=GA1.3.1651624166.1490861383; _gid=GA1.2.1301826189.1494695262; _gid=GA1.3.1215533166.1494695494");
			return wClient;
		}
	}
}
