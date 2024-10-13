using ItelexChatServer;
using ItelexChatServer.Languages;
using ItelexCommon;
using ItelexCommon.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer
{
	class ActivityHistoryManager
	{
		private const string TAG = nameof(ActivityHistoryManager);

		private Logging _logger;

		public const int SIZE = 100;

		public enum Activities { Login, Logoff, Message, Restart }

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static ActivityHistoryManager instance;

		public static ActivityHistoryManager Instance => instance ?? (instance = new ActivityHistoryManager());

		public List<ActivityHistoryItem> ActivityHistory { get; set; }

		private ActivityHistoryManager()
		{
			_logger = LogManager.Instance.Logger;

			ActivityHistory = new List<ActivityHistoryItem>();
			AddActivity(Activities.Restart, null);
		}

		public void AddActivity(Activities activity, string shortName, string message = null)
		{
			_logger.Debug(TAG, nameof(AddActivity), "start");

			ActivityHistoryItem item = new ActivityHistoryItem()
			{
				Timestamp = DateTime.Now,
				Activity = activity,
				Shortname = shortName,
				Message = ShortenMessage(message)
			};
			ActivityHistory.Add(item);
			if (ActivityHistory.Count > SIZE)
			{
				ActivityHistory.RemoveAt(0);
			}

			_logger.Debug(TAG, nameof(AddActivity), "end");
		}

		private string ShortenMessage(string msg)
		{
			if (string.IsNullOrWhiteSpace(msg))
			{
				return msg;
			}

			int idx = msg.IndexOf(CodeManager.ASC_CR);
			if (idx >= 0)
			{
				msg = msg.Substring(0, idx);
			}
			idx = msg.IndexOf(CodeManager.ASC_LF);
			if (idx >= 0)
			{
				msg = msg.Substring(0, idx);
			}
			return msg.Trim();
		}

		public List<string> GetActivityList(int count, int lngId)
		{
			int start = ActivityHistory.Count - count;
			if (start < 0)
			{
				start = 0;
			}
			List<string> list = new List<string>();
			for (int i = start; i < ActivityHistory.Count; i++)
			{
				list.Add(ActivityHistory[i].GetString(true, lngId));
			}
			return list;
		}
	}
}
