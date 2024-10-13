using ItelexCommon;
using ItelexNewsServer.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Xml;
using System.Windows.Forms;
using ItelexNewsServer.Commands;
using ItelexCommon.Commands;
using ItelexNewsServer.News;

namespace ItelexNewsServer
{
#if DEBUG
	static class Tests
	{
		public static void TestTimestamp()
		{
			//string timeStr = NewsDatabase.DateTimeStrToTimestamp(DateTime.UtcNow);
			//DateTime dt = NewsDatabase.TimestampToDateTime(timestamp.Value);
			//Debug.WriteLine(dt);
		}

		public static void InsertNumber()
		{
			const int num = 123454;
			DateTime now = DateTime.UtcNow;
			UserItem newNumber = new UserItem(num)
			{
				Kennung = $"{num} abcd d",
				Pin = "1234",
				Timezone = 1,
				RegisterTimeUtc = now,
				LastLoginTimeUtc = now,
				LastPinChangeTimeUtc = now,
				Active = true,
				SendFromHour = 7,
				SendToHour = 22,
				MsgFormat = (int)MsgFormats.Short,
				PauseUntilTimeUtc = now.AddMonths(3),
				RedirectNumber = 54321,
			};
			bool ok = NewsDatabase.Instance.UserInsert(newNumber);

			UserItem readNumber = NewsDatabase.Instance.UserLoadByTelexNumber(num);

			ok = NewsDatabase.Instance.UserDeleteByNumber(readNumber.ItelexNumber);
		}

		public static void TestInterpreter()
		{
			CommandInterpreter ci = new CommandInterpreter();

			string cmdLine;
			List<CmdTokenItem> tokens;

			cmdLine = "list cat sc";
			tokens = ci.Parse(cmdLine);
			ListTokens(tokens);

			cmdLine = "list cat sp";
			tokens = ci.Parse(cmdLine);
			ListTokens(tokens);

			cmdLine = "list la de";
			tokens = ci.Parse(cmdLine);
			ListTokens(tokens);

			cmdLine = "list su";
			tokens = ci.Parse(cmdLine);
			ListTokens(tokens);

			cmdLine = "list ab";
			tokens = ci.Parse(cmdLine);
			ListTokens(tokens);

			cmdLine = "-10";
			tokens = ci.Parse(cmdLine);
			ListTokens(tokens);

			// int rows = NewsDatabase.Instance.MsgStatusPendingClearUser(17);
			//int rows = NewsDatabase.Instance.MsgStatusPendingClearHours(1);
		}

		public static void Timezones()
		{
			DateTimeOffset thisTime = DateTimeOffset.Now;
			TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
			bool isDaylight = tzi.IsDaylightSavingTime(thisTime);
			DateTimeOffset timeInUtcTimeZone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
			DateTimeOffset timeInPstTimeZone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
		}

		private static void ListTokens(List<CmdTokenItem> tokens)
		{
			string str = "";
			foreach(CmdTokenItem t in tokens)
			{
				if (!string.IsNullOrEmpty(str)) str += " ";
				str += t.ToString();
			}
			Debug.WriteLine(str);
		}

		public static void Format()
		{
			Debug.WriteLine(string.Format("'{0,3:D}'", 22));
			Debug.WriteLine(string.Format("'{0,3:D}'", 1222));
			Debug.WriteLine(string.Format("'{0,-5}'", "th"));
			Debug.WriteLine(string.Format("'{0,-5}'", "t"));
		}

		public static void TestRss()
		{
			string msg = "<p>Die Deutschen trinken immer weniger Bier. Das ist einerseits gut und gesund. Aber am Wochenende ist zwischen Linstow und Malchow etwas geschehen, das den Blick auf die Abstinenz verändern könnte.</p>";
			//string msg = "<img src=\"https://www.sueddeutsche.de/2023/08/13/97a61889-371e-4a46-b3aa-9c4f859e1eb0.jpeg?rect=101%2C0%2C1200%2C900&width=208&fm=jpg&q=60\" data-portal-copyright=\"Fabian Hammerl\" /><p>Ein Streifzug durchs Internationale Sommerfestival in Hamburg, unter anderem mit einem Tanzabend ohne Worte und einer exzessiven Turnstunde.</p>";
			string msg2 = RssManager.SzCleanMessage(msg);
		}

		public static void HttpResponse()
		{
			string feedUri = "https://www.tagesschau.de/xml/rss2";
			HttpWebResponse response = null;
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(feedUri);
				httpWebRequest.MaximumAutomaticRedirections = 10;
				httpWebRequest.AllowAutoRedirect = true;
				//myHttpWebRequest.Timeout = 2000;
				response = (HttpWebResponse)httpWebRequest.GetResponse();
				if (response.StatusCode == HttpStatusCode.Redirect)
				{
					Debug.WriteLine("redirected to:" + response.GetResponseHeader("Location"));
				}
				Debug.WriteLine("redirected to:" + response.GetResponseHeader("Location"));


				Stream stream = response.GetResponseStream();
				Debug.WriteLine(response.Headers["Location"]);
				StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("utf-8"));
				// convert the buffer into string and store in content
				StringBuilder sb = new StringBuilder();
				while (reader.Peek() >= 0)
				{
					sb.Append(reader.ReadToEnd());
				}
				string xml = sb.ToString();
				response.Close();
			}
			catch (WebException ex)
			{
				var resp = ex.Response.Headers["Location"];
				Debug.WriteLine(resp);
				Debug.WriteLine(ex.GetType());
				if (response != null)
				{
					Debug.WriteLine(response.Headers["Location"]);
				}
			}

		}
	}
#endif
}
