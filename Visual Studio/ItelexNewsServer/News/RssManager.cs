using ItelexCommon;
using ItelexCommon.Logger;
using ItelexCommon.Utility;
using ItelexNewsServer.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ItelexNewsServer.News
{
	class RssManager
	{
        private const string TAG = nameof(RssManager);

		private static RssManager instance;

		public static RssManager Instance => instance ?? (instance = new RssManager());

        private Logging _logger;

        private RssManager()
		{
            _logger = LogManager.Instance.Logger;
		}

        public List<FetchNewsFromFeedResult> FetchNewsFromAllFeeds(List<ChannelItem> channels)
        {
            object feedLock = new object();

            List<Task> tasks = new List<Task>();
            List<FetchNewsFromFeedResult> newsItems = new List<FetchNewsFromFeedResult>(); 
            foreach (ChannelItem ch in channels)
            {
                tasks.Add(Task.Run(() =>
                {
					TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(FetchNewsFromAllFeeds));
					FetchNewsFromFeedResult result = FetchNewsFromFeed(ch.Url, ch.ChannelId, ch.LastMsgTimeUtc);
                    result.Channel = ch;
                    //if (items != null)
                    {
                        lock (feedLock)
                        {
                              newsItems.Add(result);
                        }
                    }
					TaskManager.Instance.RemoveTask(Task.CurrentId);
				}));
            }
            Task.WaitAll(tasks.ToArray());
            return newsItems;
        }

        public FetchNewsFromFeedResult FetchNewsFromFeed(string feedUri, long channelId, DateTime? lastMsgTime)
        {
            FetchNewsFromFeedResult result = new FetchNewsFromFeedResult();

			WebClientEx wClient = WebClientEx.GetWebClient();
            wClient.Timeout = 10000;
            wClient.Encoding = Encoding.UTF8;
            wClient.UseDefaultCredentials = true;

            List<NewsItem> newsItems = new List<NewsItem>();
            XmlDocument xml = new XmlDocument();
            XmlNodeList entries;

            try
            {
                string xmlSource = wClient.DownloadString(feedUri);
                xml.Load(new StringReader(xmlSource));
                entries = xml.SelectNodes("/rss/channel/item");
            }
            catch (Exception ex)
            {
				wClient.Dispose();
				_logger.Error(TAG, nameof(FetchNewsFromFeed), $"Error fetching RSS-Feed {feedUri}", ex);
				result.Error = true;
                return result;
            }
            wClient.Dispose();

            foreach (XmlNode node in entries)
            {
                try
                {
                    string pubDateStr = node["pubDate"]?.InnerText;
                    if (!DateTime.TryParse(pubDateStr, out DateTime pubDate)) continue; // invalid msg date
                    pubDate = pubDate.ToUniversalTime();
                    if (lastMsgTime != null && pubDate <= lastMsgTime) continue; // skip old msgs

                    NewsItem newsItem = new NewsItem();
                    newsItem.NewsTimeUtc = pubDate;
                    bool isPermaLink = node.Attributes?["guid"]?.Value == "true";
                    newsItem.ChannelId = channelId;
                    newsItem.OriginalNewsId = node["guid"]?.InnerText;
                    newsItem.Title = node["title"]?.InnerText;
					string desc = node["description"]?.InnerText;

					if (feedUri.Contains("faz.net"))
                    {
                        desc = FazCleanMessage(desc);
                    }
                    else if (feedUri.Contains("sueddeutsche.de"))
                    {
                        desc = SzCleanMessage(desc);
                    }
					else if (feedUri.Contains("postillon"))
					{
						desc = PostillonCleanMessage(desc);
					}
					else
					{
                        desc = CleanMessage(desc);
                    }
                    newsItem.Message = desc;

					if (!newsItem.IsInvalid() && !SkipNewsByContent(newsItem))
                    {
						_logger.Debug(TAG, nameof(FetchNewsFromFeed), $"add msg {newsItem.NewsId} {newsItem.NewsTimeUtc} {newsItem.Title}");
						newsItems.Add(newsItem);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.HResult == -2146233079)
                    {
                        _logger.Error(TAG, nameof(FetchNewsFromFeed), "Connection error reading RSS-Item");
                    }
                    else
                    {
                        _logger.Error(TAG, nameof(FetchNewsFromFeed), "Error reading RSS-Item", ex);
                    }
					result.Error = true;
					return result;
                }
            }
			result.Error = false;
            result.NewsItems = newsItems;
            return result;
        }

		public string FazCleanMessage(string desc)
        {
            if (desc == null || string.IsNullOrWhiteSpace(desc)) return "";

            string[] parts = desc.Split(new string[] { "</p><p>" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return CleanMessage(parts[0]);
	
			return CleanMessage(parts[1]);
    	}

		public static string SzCleanMessage(string desc)
		{
			if (desc == null || string.IsNullOrWhiteSpace(desc)) return "";

			string[] parts = desc.Split(new string[] { "<p>" }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 1) return CleanMessage(parts[0]);

			return CleanMessage(parts[1]);
		}

		public static string PostillonCleanMessage(string desc)
		{
			if (string.IsNullOrWhiteSpace(desc)) return "";

			LogManager.Instance.Logger.Debug(TAG, nameof(PostillonCleanMessage), "start");

			while (true)
			{
				int p1 = desc.IndexOf("<");
				if (p1 == -1) break;
				int p2 = desc.IndexOf(">", p1);
				if (p2 == -1) break;
				desc = desc.Substring(0, p1) + desc.Substring(p2 + 1, desc.Length - p2 - 1);
			}
			desc = desc.Replace("mehr...", "");
			desc = desc.Replace("+++\n+++", "+++");
			LogManager.Instance.Logger.Debug(TAG, nameof(PostillonCleanMessage), $"replace +++ +++\r\n{desc}\r\n");
			desc = desc.Trim(new char[] { ' ', '\r', '\n' });
			return CleanMessage(desc);
		}

		private static string CleanMessage(string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return "";

			string newStr = str.Replace("<p>", "");
			newStr = newStr.Replace("</p>", "");
			newStr = newStr.Replace("&amp;", "+");
			newStr = newStr.Replace("&lt;", "");
			newStr = newStr.Replace("&gt;", "");
			newStr = newStr.Replace("&quot;", "'");
			newStr = newStr.Replace("&tilde;", "'");
			newStr = newStr.Replace("&euro;", "Euro");
			newStr = newStr.Replace("&raquo;", "'");
			newStr = newStr.Replace("&laquo;", "'");
			newStr = newStr.Replace("&#034;", "'");
			newStr = newStr.Replace("$", "s");
			newStr = newStr.Replace("%", "o/o");
			return newStr.Trim();
		}

        private bool SkipNewsByContent(NewsItem newsItem)
        {
            string[] skipTexts = new string[]
            {
				"tagesschau in 100 Sekunden",
				"Bilder des Tages",
				"Fotografie:",
                "Bilder:",
				"Bilderrätsel:",
			};

            foreach (string s in skipTexts)
            {
                if (newsItem.Contains(s))
                {
					_logger.Debug(TAG, nameof(SkipNewsByContent), $"skip '{newsItem.Title}'");
					return true;
                }
            }
			return false;
        }

		public DateTime? GetDateTime(string dateStr, string format)
        {
            if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                return dt;
            else
                return null;
        }
    }

	class FetchNewsFromFeedResult
    {
        public ChannelItem Channel { get; set; }

		public List<NewsItem> NewsItems { get; set; }

		public bool Error { get; set; }

	}
}
