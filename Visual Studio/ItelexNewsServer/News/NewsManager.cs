using ItelexCommon;
using ItelexCommon.Logger;
using ItelexCommon.Utility;
using ItelexNewsServer.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItelexNewsServer.News
{
	class NewsManager
	{
		private const string TAG = nameof(NewsManager);

		private Logging _logger;

		public const int LAST_NEWS_DAYS = 7;
		public const int LAST_MSG_STATUS_HOURS = 12;

		public const int RSS_INTERVALL = 60 * 1000 * 5;
		public const int USERCHANNELS_INTERVALL = 60 * 1000 * 5;

		private NewsDatabase _database;

		private RssManager _rss;

		//private TwitterManager _twitter;
		//private bool _twitterTimerActive;
		//private System.Timers.Timer _twitterTimer;

		private bool _fetchNewsTimerActive;
		private System.Timers.Timer _fetchNewsTimer;

		private TickTimer _rssNewsTimer = new TickTimer();
		//private bool _rssNewsTimerFirst = true;
		//private TickTimer _itelexNewsTimer = new TickTimer();

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static NewsManager instance;

		public static NewsManager Instance => instance ?? (instance = new NewsManager());

		public object GlobalMessageLock = new object();

		private NewsManager()
		{
			_logger = LogManager.Instance.Logger;

			_database = NewsDatabase.Instance;

			_rss = RssManager.Instance;

			//_twitter = TwitterManager.Instance;
			//_twitterTimerActive = false;
			//_twitterTimer = new System.Timers.Timer(60 * 1000);
			//_twitterTimer.Elapsed += TwitterTimer_Elapsed;
			//_twitterTimer.Start();

			_fetchNewsTimerActive = false;
#if !DEBUG
			_fetchNewsTimer = new System.Timers.Timer(1000 * 60 * 5);
#else
			_fetchNewsTimer = new System.Timers.Timer(1000 * 60);
#endif
			_fetchNewsTimer.Elapsed += FetchNewsTimer_Elapsed;
			_fetchNewsTimer.Start();

			// prevent: "The request was aborted: Could not create SSL/TLS secure channel".
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			//Task task = FetchRssNewsAsync();
			//FetchNewsTimer_Elapsed(null, null);
		}

		public List<UserChannel> GetUserChannels(long userId, ChannelTypes? type, ChannelCategories? category,
			ChannelLanguages? language)
		{
			List<ChannelItem> channels = _database.ChannelLoadAll();
			List<SubscriptionItem> subscriptions = _database.SubscriptionsLoad(null, userId);
			List<UserChannel> userChannels = new List<UserChannel>();
			ChannelCategoryItem catItem = category != null ? ChannelItem.GetCategory(category.Value) : null;
			ChannelLanguageItem lngItem = language != null ? ChannelItem.GetLanguage(language.Value) : null;
			ChannelTypeItem typeItem = type != null ? ChannelItem.GetChannelType(type.Value) : null;

			foreach (ChannelItem chItem in channels)
			{
				if (typeItem != null && chItem.Type != typeItem.Kennung) continue;
				if (catItem != null && chItem.Category != catItem.Name) continue;
				if (lngItem != null && chItem.Language != lngItem.Name) continue;

				UserChannel userChannel = new UserChannel(chItem);
				userChannel.SubscribeCount = (from s in subscriptions where s.ChannelId == chItem.ChannelId select s).Count();
				userChannel.NewsCount = _database.NewsGetLastEntryCount(chItem.ChannelId, LAST_NEWS_DAYS).GetValueOrDefault();
				userChannel.Name = CodeManager.CleanAscii(userChannel.Name.ToLower()).Trim();

				SubscriptionItem subsItem = (from s in subscriptions
											 where s.ChannelId == chItem.ChannelId && s.UserId == userId
											 select s).FirstOrDefault();
				if (subsItem != null)
				{
					userChannel.Subscribed = true;
					userChannel.SubscribeTimeUtc = subsItem.SubscribeTimeUtc;
					//userChannel.SubcribedContent = subsItem.Content;
				}

				userChannels.Add(userChannel);
			}

			userChannels.Sort(new UserChannelComparer());

			return userChannels;
		}

		public bool AddSubscription(long channelId, long userId, string content)
		{
			SubscriptionItem subsItem = _database.SubscriptionsLoad(channelId, userId).FirstOrDefault();
			if (subsItem != null) return true; // already subscribed

			//ChannelItem channenItem = _database.ChannelLoadById(channelId);
			//string resultCont = channenItem.GetResultContent(content);

			subsItem = new SubscriptionItem()
			{
				ChannelId = channelId,
				UserId = userId,
				//Content = resultCont,
				SubscribeTimeUtc = DateTime.UtcNow
			};
			return _database.SubscriptionInsert(subsItem);
		}

		public bool RemoveSubscription(long channelId, long userId)
		{
			List<SubscriptionItem> subscriptions = _database.SubscriptionsLoad(null, userId);
			SubscriptionItem subsItem = (from s in subscriptions where s.ChannelId == channelId && s.UserId == userId select s).FirstOrDefault();
			if (subsItem == null) return false; // no subscription found
			return _database.SubscriptionDelete(subsItem);
		}

		private void FetchNewsTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Task.Run(() =>
			{
				TaskManager.Instance.AddTask(Task.CurrentId, TAG, nameof(FetchNewsTimer_Elapsed));
				if (_fetchNewsTimerActive) return;
				_fetchNewsTimerActive = true;
				FetchRssNews();
				_fetchNewsTimerActive = false;
				TaskManager.Instance.RemoveTask(Task.CurrentId);
			});
		}

		private int FetchRssNews()
		{
#if DEBUG
			return 0;
#endif

			int count = 0;
			Debug.WriteLine("FetchRssNewsAsync start");
			lock (GlobalMessageLock)
			{
				_subscriptions = null;
				_userItems = null;
				Dictionary<long, int> distribCount = new Dictionary<long, int>();

				List<ChannelItem> channels = _database.ChannelLoadAll();
				List<ChannelItem> rssChannels = (from c in channels where c.Type == "rss" select c).ToList();
				//DateTime startDate = DateTime.UtcNow.AddDays(-1);

				// fetch all channels async simultaneously
				List<FetchNewsFromFeedResult> channelResults = _rss.FetchNewsFromAllFeeds(rssChannels);

				LazyLoadSubscriptions();
				LazyLoadUsers();
				LazyLoadPendingMsgStatusItems();

				//foreach (ChannelItem chItem in channels)
				foreach (FetchNewsFromFeedResult result in channelResults)
				{
					ChannelItem chItem = result.Channel;
					List<NewsItem> newsItems = result.NewsItems;

					if (result.Error)
					{
						result.Channel.ErrorCount++;
						_database.ChannelUpdate(result.Channel);
						continue;
					}

					//List<NewsItem> channelNews = (from n in result.NewsItems where n.ChannelId == chItem.ChannelId select n).ToList();
					_logger.Debug(TAG, nameof(FetchRssNews), 
							$"fetched {newsItems.Count} news for channeldId={chItem.ChannelId}");

					if (newsItems.Count == 0) continue;

					DateTime? lastMsgTime = newsItems.Max(n => n.NewsTimeUtc);
					if (lastMsgTime != null)
					{
						chItem.LastMsgTimeUtc = lastMsgTime;
						_database.ChannelUpdate(chItem);
					}

					if (newsItems.Where(n => n.Error).Any())
					{
						DispatchMsg($"error getting {chItem.Name}");
						continue;
					}
					if (newsItems.Count > 0)
					{
						AddNewsForChannel(chItem, newsItems, distribCount);
						count += newsItems.Count;
					}
				}

				foreach (KeyValuePair<long, int> entry in distribCount)
				{
					int number = (from u in _userItems where u.UserId == entry.Key select u.ItelexNumber).FirstOrDefault();
					DispatchMsg($"{entry.Value} new message(s) for {number}");
				};
			}
			return count;
		}

		public void SendMessageToChannel(ChannelItem chItem, string author, string message, int? skipThisReveiver = null)
		{
			// extract title and message
			//text = text.Replace("\r", "");
			//List<string> lines = text.Split('\n').ToList();
			//string title = lines[0];
			//lines.RemoveAt(0);
			//string message = string.Join("\r\n", lines.ToArray());
			SendMessageToChannel(chItem, author, null, message, skipThisReveiver);
		}

		public void SendMessageToChannel(ChannelItem chItem, string author, string title, string message, int? skipThisReveiver = null)
		{
			LazyLoadSubscriptions();
			LazyLoadUsers();
			LazyLoadPendingMsgStatusItems();

			NewsItem newsItem = new NewsItem()
			{
				OriginalNewsId = chItem.ChannelId + "@" + Guid.NewGuid().ToString(),
				ChannelId = chItem.ChannelId,
				Author = author,
				Title = title,
				Message = message,
				NewsTimeUtc = DateTime.UtcNow
			};

			Dictionary<long, int> distribCount = new Dictionary<long, int>();
			AddNewsForChannel(chItem, new List<NewsItem>() { newsItem }, distribCount, skipThisReveiver);
		}

		private void AddNewsForChannel(ChannelItem chItem, List<NewsItem> news, Dictionary<long, int> distribCount,
				int? skipThisReveiver = null)
		{
			if (news == null) return;

			int count = 0;
			foreach (NewsItem newsItem in news)
			{
				//if (newsItem.NewTimeUtc <= newsItem.NewTimeUtc) continue; // old msg
				if (_database.NewsEntryExists(newsItem.OriginalNewsId)) continue; // msg exists

				newsItem.ChannelId = chItem.ChannelId;
				newsItem.AllSend = false;
				if (_database.NewsInsert(newsItem))
				{
					_logger.Debug(TAG, nameof(AddNewsForChannel), 
							$"add news id {newsItem.NewsId} {newsItem.NewsTimeUtc} '{newsItem.Title}'");
					DistributeNewsEntry(chItem, newsItem, distribCount, skipThisReveiver);
					count++;
				}
			}

			if (count > 0)
			{
				DispatchMsg($"{count} new message(s) from '{chItem.Name}'");
			}
		}

		private void DistributeNewsEntry(ChannelItem chItem, NewsItem newsItem, Dictionary<long, int> distribCount,
				int? skipThisReveiver = null)
		{
			List<MsgStatusItem> subsStatus = (from s in _subscriptions
											  where s.ChannelId == chItem.ChannelId
											  select new MsgStatusItem()
											  {
												  NewsId = newsItem.NewsId,
												  UserId = s.UserId,
												  SendStatus = (int)MsgStatis.Pending,
												  DistribTimeUtc = DateTime.UtcNow,
											  }).ToList();

			foreach (MsgStatusItem statusItem in subsStatus)
			{
				UserItem userItem = (from u in _userItems where u.UserId == statusItem.UserId select u).FirstOrDefault();
				if (userItem == null) continue; // user does not exist or is inactivated
				if (userItem.Paused) continue;
				if (chItem.ChannelType != ChannelTypes.Local &&  !userItem.IsHourActive()) continue;
				if (skipThisReveiver.HasValue && userItem.ItelexNumber == skipThisReveiver) continue;

				List<MsgStatusItem> msgStatusListForUser = (from m in _pendingMsgStatusItems
															where m.UserId == userItem.UserId
															select m).ToList();
				msgStatusListForUser.Sort(new MsgsStatusComparer());

				// set old msgStatusItems to "outdated"
				while (PendingMsgCnt(userItem.UserId) >= userItem.MaxPendingNews)
				{
					// set oldest msgstatus to "outdated"
					MsgStatusItem oldest = msgStatusListForUser.First();
					oldest.SendStatus = (int)MsgStatis.Outdated;
					_database.MsgStatusUpdate(oldest);
					_pendingMsgStatusItems.Remove(oldest); // no longer pending
					msgStatusListForUser.Remove(oldest); // no longer pending
				}

				_logger.Debug(TAG, nameof(DistributeNewsEntry),
						$"add news id {statusItem.NewsId} for user {statusItem.UserId} '{newsItem.Title}'");

				_database.MsgStatusInsert(statusItem);
				_pendingMsgStatusItems.Add(statusItem);

				if (!distribCount.ContainsKey(userItem.UserId)) distribCount[userItem.UserId] = 0;
				distribCount[userItem.UserId]++;
			}
		}

		private int PendingMsgCnt(long userId)
		{
			int count = (from m in _pendingMsgStatusItems where m.UserId == userId select m).Count();
			//Debug.WriteLine(count);
			return count;
		}

		/// <summary>
		/// Give a list of all users with active subscriptions that are currently not paused
		/// </summary>
		/// <returns></returns>
		public List<UserItem> GetAllActiveUsers()
		{
			List<UserItem> activeUsers = new List<UserItem>();
			List<SubscriptionItem> subs = new List<SubscriptionItem>();
			foreach(SubscriptionItem s in _subscriptions)
			{
				UserItem user = (from u in _userItems where u.UserId == s.UserId select u).FirstOrDefault();
				if (user == null) continue;
				if (user.IsPaused || !user.IsHourActive()) continue;
				activeUsers.Add(user);
			}
			return activeUsers;
		}

		List<ChannelItem> _channels = null;

		private void LazyLoadChannels(bool force = false)
		{
			if (force || _channels == null)
			{
				_channels = _database.ChannelLoadAll();
			}
		}

		private List<SubscriptionItem> _subscriptions = null;

		private void LazyLoadSubscriptions(bool force = false)
		{
			if (force || _subscriptions == null)
			{
				_subscriptions = _database.SubscriptionsLoad(null, null);
			}
		}

		private List<UserItem> _userItems = null;

		private void LazyLoadUsers(bool force = false)
		{
			if (force || _userItems == null)
			{
				_userItems = _database.UserLoadAllActive();
			}
		}

		private List<MsgStatusItem> _pendingMsgStatusItems = null;

		private void LazyLoadPendingMsgStatusItems(bool force = false)
		{
			if (force || _pendingMsgStatusItems == null)
			{
				_pendingMsgStatusItems = _database.MsgStatusLoad(null, null, true, Constants.MAX_MSG_SEND_RETRIES);
			}
		}

		public void MessageCleanUp()
		{
			DispatchMsg("Cleanup");
			lock (GlobalMessageLock)
			{
				int subsCount = _database.SubscriptionsCleanup();
				// news channel: outdated after 1 day, delete after 3 days
				// local channel: outdated after 3 days, delete after 7 days
				int msgStatCount = _database.MsgStatusCleanupByDate(24, 3 * 24, 24 * 3, 24 * 7);
				//int msgStat2Count = _database.MsgStatusCleanupByMaxPending();
				int newsCount = _database.NewsCleanup(LAST_NEWS_DAYS); // older than LAST_NEWS_DAYS days
				DispatchMsg($"cleaned {subsCount} subscriptions");
				DispatchMsg($"cleaned {msgStatCount} msgstatus date");
				//DispatchMsg($"cleaned {msgStat2Count} msgstatus max pending");
				DispatchMsg($"cleaned {newsCount} news");
			}
		}

		private void DispatchMsg(string msg)
		{
			MessageDispatcher.Instance.Dispatch(msg);
			_logger.Debug(TAG, nameof(DispatchMsg), msg);
		}
	}

	/// <summary>
	/// Compare by DistribTimeUtc ascending
	/// </summary>
	class MsgsStatusComparer : Comparer<MsgStatusItem>
	{
		public override int Compare(MsgStatusItem item1, MsgStatusItem item2)
		{
			return DateTime.Compare(item1.DistribTimeUtc, item2.DistribTimeUtc);
		}

	}
}
