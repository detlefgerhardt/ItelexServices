using ItelexCommon;
using ItelexCommon.Logger;
using ItelexNewsServer.News;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ItelexNewsServer.Data
{
	class NewsDatabase : Database
	{
		private static string TAG = nameof(NewsDatabase);

		private static NewsDatabase instance;

		public static NewsDatabase Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new NewsDatabase();
				}
				return instance;
			}
		}

		private NewsDatabase() : base(Constants.DATABASE_NAME)
		{
			_logger = LogManager.Instance.Logger;
			//CreateDatabase();
			ConnectDatabase();
			//DisconnectDatabase();
		}

		~NewsDatabase()
		{
			Dispose(false);
		}

		public void CreateDatabase()
		{
		}

		private void ChangeDatabase()
		{
		}

		public UserItem UserLoadByTelexNumber(int number)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM user WHERE ItelexNumber={number}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						UserItem userItem = ItemGetQuery<UserItem>(sqlReader);
						return userItem;
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserLoadByTelexNumber), $"{number}", ex);
					return null;
				}
			}
		}

		public UserItem UserLoadById(Int64 userId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM user WHERE UserId={userId}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						UserItem userItem = ItemGetQuery<UserItem>(sqlReader);
						return userItem;
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserLoadById), $"userId={userId}", ex);
					return null;
				}
			}
		}

		public bool UserInsert(UserItem userItem)
		{
			lock (Locker)
			{
				try
				{
					UserItem checkItem = UserLoadByTelexNumber(userItem.ItelexNumber);
					if (checkItem != null)
					{   // telexnumber already exists
						return false;
					}

					string sqlStr = GetItemInsertString(userItem, "user");
					bool ok = DoSqlNoQuery(sqlStr);
					if (!ok)
					{
						return false;
					}

					userItem.UserId = (Int64)DoSqlScalar("SELECT last_insert_rowid() FROM user");


					//sqlStr = "SELECT last_insert_rowid() FROM messages";
					//SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					//userItem.UserId = (Int64)sqlCmd.ExecuteScalar();
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserInsert), $"userItem={userItem}", ex);
					return false;
				}
			}
		}

		public bool UserUpdate(UserItem userItem)
		{
			try
			{
				string updateString = GetItemUpdateString(userItem, "user", $"UserId={userItem.UserId}");
				DoSqlNoQuery(updateString);
				return true;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(UserUpdate), $"userItem={userItem}", ex);
				return false;
			}
		}

		/*
		public bool UserSetPin(int number, string pin)
		{
			lock (_locker)
			{
				try
				{
					int? dateInt = DateTimeToTimestamp(DateTime.UtcNow);
					string updateString = $"UPDATE user SET Pin='{pin}',LastPinChangeTime={dateInt},SendNewPin=0 WHERE ItelexNumber={number}";
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logging.Error(TAG, nameof(UserSetPin), $"number={number}", ex);
					return false;
				}
			}
		}

		public bool UserClearPin(int number)
		{
			lock (_locker)
			{
				try
				{
					int? dateInt = DateTimeToTimestamp(DateTime.UtcNow);
					string updateString = $"UPDATE user SET Pin=null,LastPinChangeTime={dateInt},SendNewPin=1 WHERE ItelexNumber={number}";
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logging.Error(TAG, nameof(UserClearPin), $"number={number}", ex);
					return false;
				}
			}
		}
		*/

		/// <summary>
		/// Update last login data
		/// </summary>
		/// <param name="number">i-telex number</param>
		/// <param name="answerback">null: do not set, "": clear</param>
		/// <returns></returns>
		public bool UserSetLastLogin(int number, string answerback)
		{
			lock (Locker)
			{
				try
				{
					string dateStr = DateTimeStrToTimestamp(DateTime.UtcNow);
					answerback = EscapeSql(answerback);
					string updateString;
					if (answerback != null)
					{
						updateString = $"UPDATE user SET LastLoginTimeUtc='{dateStr}',Kennung='{answerback}' WHERE ItelexNumber={number}";
					}
					else
					{
						updateString = $"UPDATE user SET 'LastLoginTimeUtc={dateStr}' WHERE ItelexNumber={number}";
					}
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserSetLastLogin), $"number={number}", ex);
					return false;
				}
			}
		}

		public bool UserDeleteById(Int64 id)
		{
			lock (Locker)
			{
				try
				{
					string sqlString = $"DELETE FROM user WHERE UserId={id}";
					return DoSqlNoQuery(sqlString);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserDeleteById), $"UserId={id}", ex);
					return false;
				}
			}
		}

		public bool UserDeleteByNumber(int number)
		{
			lock (Locker)
			{
				try
				{
					string sqlString = $"DELETE FROM user WHERE ItelexNumber={number}";
					return DoSqlNoQuery(sqlString);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserDeleteById), $"ItelexNumber={number}", ex);
					return false;
				}
			}
		}

		public List<UserItem> UserLoadAllActive()
		{
			List<UserItem> items = new List<UserItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = "SELECT * FROM user WHERE Active=1 ORDER BY ItelexNumber";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						UserItem item = ItemGetQuery<UserItem>(sqlReader);
						items.Add(item);
						count++;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserLoadAllActive), "error", ex);
					return null;
				}
			}

			_logger.Debug(TAG, nameof(UserLoadAllActive), $"{items.Count} users loaded");
			return items;
		}

		public List<ChannelItem> ChannelLoadAll()
		{
			List<ChannelItem> items = new List<ChannelItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM channels WHERE Active=1";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						ChannelItem item = ItemGetQuery<ChannelItem>(sqlReader);
						items.Add(item);
						count++;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ChannelLoadAll), "error", ex);
					return null;
				}
			}

			_logger.Debug(TAG, nameof(ChannelLoadAll), $"{items.Count} channel items loaded");
			return items;
		}

		public ChannelItem ChannelLoadByName(string name)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM channels WHERE Name='{name}' AND Active=1";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						ChannelItem channelItem = ItemGetQuery<ChannelItem>(sqlReader);
						return channelItem;
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ChannelLoadByName), $"{name}", ex);
					return null;
				}
			}
		}

		public ChannelItem ChannelLoadByLocalId(int localId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM channels WHERE LocalId={localId} AND Active=1";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						ChannelItem channelItem = ItemGetQuery<ChannelItem>(sqlReader);
						return channelItem;
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ChannelLoadByName), $"localId={localId}", ex);
					return null;
				}
			}
		}

		public bool ChannelInsert(ChannelItem channelItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = GetItemInsertString(channelItem, "channels");
					bool ok = DoSqlNoQuery(sqlStr);
					if (!ok)
					{
						return false;
					}

					sqlStr = "SELECT last_insert_rowid() FROM channels";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					channelItem.ChannelId = (Int64)sqlCmd.ExecuteScalar();
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ChannelInsert), $"channelItem={channelItem}", ex);
					return false;
				}
			}
		}

		public bool ChannelUpdate(ChannelItem channelItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = GetItemUpdateString(channelItem, "channels", $"ChannelId={channelItem.ChannelId}");
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					DoSqlNoQuery(sqlStr);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ChannelUpdate), $"userId={channelItem.ChannelId}", ex);
					return false;
				}
			}
		}

		public bool ChannelDeleteById(Int64 channelId)
		{
			lock (Locker)
			{
				try
				{
					string sqlString = $"DELETE FROM channels WHERE ChannelId={channelId}";
					return DoSqlNoQuery(sqlString);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ChannelDeleteById), $"ChannelId={channelId}", ex);
					return false;
				}
			}
		}

		public ChannelItem ChannelLoadById(Int64 channelId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM channels WHERE ChannelId={channelId} AND Active=1";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						ChannelItem channelItem = ItemGetQuery<ChannelItem>(sqlReader);
						return channelItem;
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ChannelLoadById), $"channelId={channelId}", ex);
					return null;
				}
			}
		}

		public int ChannelGetMaxLocalId()
		{
			try
			{
				string sqlStr = $"SELECT MAX(LocalId) FROM channels WHERE Type='{ChannelItem.GetChannelTypeName(ChannelTypes.Local)}' AND Active=1";
				SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
				object result = sqlCmd.ExecuteScalar();
				if (result.GetType() != typeof(DBNull))
				{
					return Convert.ToInt32(result);
				}
				else
				{
					return 0;
				}
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(ChannelGetMaxLocalId), "", ex);
				return -1;
			}
		}

		public List<SubscriptionItem> SubscriptionsLoadAll(long? userId)
		{
			List<SubscriptionItem> items = new List<SubscriptionItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					List<string> whereList = new List<string>();
					if (userId != null)
					{
						whereList.Add($"UserId={userId}");
					}

					string sqlStr = $"SELECT * FROM subscriptions";
					string whereStr = BuildWhereClause(whereList);
					if (!string.IsNullOrEmpty(whereStr))
					{
						sqlStr += $" WHERE {whereStr}";
					}

					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						SubscriptionItem item = ItemGetQuery<SubscriptionItem>(sqlReader);
						items.Add(item);
						count++;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SubscriptionsLoadAll), "error", ex);
					return null;
				}
			}

			_logger.Debug(TAG, nameof(SubscriptionsLoadAll), $"{items.Count} subscription items loaded");
			return items;
		}

		public List<SubscriptionItem> SubscriptionsLoad(Int64? channelId, Int64? userId)
		{
			lock (Locker)
			{
				try
				{
					List<string> whereList = new List<string>();
					if (channelId != null)
					{
						whereList.Add($"ChannelId={channelId}");
					}
					if (userId != null)
					{
						whereList.Add($"UserId={userId}");
					}

					string sqlStr = $"SELECT * FROM subscriptions";
					string whereStr = BuildWhereClause(whereList);
					if (!string.IsNullOrEmpty(whereStr))
					{
						sqlStr += $" WHERE {whereStr}";
					}

					List<SubscriptionItem> items = new List<SubscriptionItem>();

					//string sqlStr = $"SELECT * FROM subscriptions WHERE ChannelId={channelId} AND UserId={userId}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						SubscriptionItem item = ItemGetQuery<SubscriptionItem>(sqlReader);
						items.Add(item);
					}
					return items;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SubscriptionsLoad), $"channelId={channelId} userlId={userId}", ex);
					return null;
				}
			}
		}

		public bool SubscriptionInsert(SubscriptionItem subsItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = GetItemInsertString(subsItem, "subscriptions");
					return DoSqlNoQuery(sqlStr);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SubscriptionInsert), $"subsItem={subsItem}", ex);
					return false;
				}
			}
		}

		public bool SubscriptionDelete(SubscriptionItem subsItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"DELETE FROM subscriptions WHERE ChannelId={subsItem.ChannelId} AND UserId={subsItem.UserId}";
					DoSqlNoQuery(sqlStr);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(SubscriptionDelete), $"subsItem={subsItem}", ex);
					return false;
				}
			}
		}

		public List<NewsItem> NewsLoadAll()
		{
			List<NewsItem> items = new List<NewsItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM news";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						NewsItem item = ItemGetQuery<NewsItem>(sqlReader);
						items.Add(item);
						count++;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(NewsLoadAll), "error", ex);
					return null;
				}
			}

			_logger.Debug(TAG, nameof(NewsLoadAll), $"{items.Count} news items loaded");
			return items;
		}

		public NewsItem NewsLoadById(Int64 newsId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM news WHERE NewsId={newsId}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						NewsItem newsItem = ItemGetQuery<NewsItem>(sqlReader);
						return newsItem;
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(NewsLoadById), $"newsId={newsId}", ex);
					return null;
				}
			}
		}

		public int? NewsGetLastEntryCount(Int64 channelId, int days)
		{
			lock (Locker)
			{
				try
				{
					DateTime dtRange = DateTime.UtcNow.AddDays(-days);
					string dtStr = DateTimeStrToTimestamp(dtRange);
					string sqlStr = $"SELECT COUNT(*) FROM news WHERE ChannelId={channelId} AND NewsTimeUtc>'{dtStr}'";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					return Convert.ToInt32(sqlCmd.ExecuteScalar());
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(NewsGetLastEntryCount), "error", ex);
					return null;
				}
			}
		}

		public NewsItem NewsGetLastEntry(Int64 channelId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT MAX(NewsTimeUtc) FROM news WHERE ChannelId={channelId}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					int newsTime = Convert.ToInt32(sqlCmd.ExecuteScalar());

					sqlStr = $"SELECT * FROM news WHERE NewsTimeUtc={newsTime}";
					sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						return ItemGetQuery<NewsItem>(sqlReader);
					}
					return null;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(NewsGetLastEntry), "error", ex);
					return null;
				}
			}
		}

		public bool NewsEntryExists(string origNewsId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT COUNT(*) FROM news WHERE OriginalNewsId='{EscapeSql(origNewsId)}'";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					int count = Convert.ToInt32(sqlCmd.ExecuteScalar());
					return count != 0;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(NewsEntryExists), $"origNewsId={origNewsId}", ex);
					return false;
				}
			}
		}

		public bool NewsInsert(NewsItem newsItem)
		{
			lock (Locker)
			{
				try
				{
					//newsItem.OriginalNewsId = EscapeSql(newsItem.OriginalNewsId);
					//newsItem.Title = EscapeSql(newsItem.Title);
					//newsItem.Message = EscapeSql(newsItem.Message);

					string sqlStr = GetItemInsertString(newsItem, "news");
					if (!DoSqlNoQuery(sqlStr)) return false;

					sqlStr = "SELECT last_insert_rowid() FROM news";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					newsItem.NewsId = (Int64)sqlCmd.ExecuteScalar();
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(NewsInsert), $"newsItem={newsItem}", ex);
					return false;
				}
			}
		}

		public bool NewsDelete(Int64 newsId)
		{
			lock (Locker)
			{
				try
				{
					string sqlString = $"DELETE FROM news WHERE NewsId={newsId}";
					return DoSqlNoQuery(sqlString);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(NewsDelete), $"newsId={newsId}", ex);
					return false;
				}
			}
		}

		/*
		public List<MsgStatusItem> MsgStatusLoadAllPending(int maxRetries)
		{
			List<MsgStatusItem> items = new List<MsgStatusItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM msgstatus WHERE SendStatus={(int)MsgStatis.Pending} AND SendRetries<{maxRetries}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						MsgStatusItem item = ItemGetQuery<MsgStatusItem>(sqlReader);
						items.Add(item);
						count++;
					}
					_logger.Debug(TAG, nameof(MsgStatusLoadAllPending), $"{items.Count} msgstatus items loaded");
					return items;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgStatusLoadAllPending), "error", ex);
					return null;
				}
			}
		}
		*/

		public List<MsgStatusItem> MsgStatusLoad(Int64? userId, Int64? newsId, bool pending, int? maxRetries)
		{
			lock (Locker)
			{
				try
				{
					List<string> whereList = new List<string>();
					if (newsId != null)
					{
						whereList.Add($"NewsId={newsId}");
					}
					if (userId != null)
					{
						whereList.Add($"UserId={userId}");
					}
					if (pending)
					{
						whereList.Add($"SendStatus={(int)MsgStatis.Pending}");
					}
					if (maxRetries != null)
					{
						whereList.Add($"SendRetries<{maxRetries}");
					}

					string sqlStr = $"SELECT * FROM msgstatus";
					string whereStr = BuildWhereClause(whereList);
					if (!string.IsNullOrEmpty(whereStr))
					{
						sqlStr += $" WHERE {whereStr}";
					}

					List<MsgStatusItem> items = new List<MsgStatusItem>();

					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						MsgStatusItem item = ItemGetQuery<MsgStatusItem>(sqlReader);
						//if (item.NewsId == 217171)
						//{
						//	Debug.Write("");
						//}
						items.Add(item);
					}
					return items;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgStatusLoad), $"userId=={userId} newsId={newsId}", ex);
					return null;
				}
			}
		}

		public int MsgStatusGetPendingCount(Int64 userId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT COUNT(*) FROM msgstatus WHERE UserId={userId} AND SendStatus={(int)MsgStatis.Pending}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					return Convert.ToInt32(sqlCmd.ExecuteScalar());
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgStatusGetPendingCount), $"userId=={userId}", ex);
					return 0;
				}
			}
		}

		public int MsgStatusPendingClearUser(Int64 userId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"UPDATE msgstatus SET SendStatus={(int)MsgStatis.Cleared} WHERE UserId={userId} AND SendStatus={(int)MsgStatis.Pending}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					sqlCmd.ExecuteScalar();
					return GetAffectedRowsCount();
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgStatusPendingClearUser), $"userId={userId}", ex);
					return -1;
				}
			}
		}

		/*
		/// <summary>
		/// Set all msgstatus entries to cleared that are older than n hours
		/// </summary>
		/// <param name="hours"></param>
		/// <returns>number of affected rows</returns>
		public int MsgStatusPendingClearHours(int hours)
		{
			lock (Locker)
			{
				try
				{
					string dateStr = DateTimeStrToTimestamp(DateTime.UtcNow.AddHours(-hours));
					string sqlStr = $"UPDATE msgstatus SET SendStatus={(int)MsgStatis.Outdated} WHERE DistribTimeUtc<'{dateStr}'";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					sqlCmd.ExecuteScalar();
					return GetAffectedRowsCount();
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgStatusPendingClearHours), $"days={hours}", ex);
					return -1;
				}
			}

		}
		*/

		public bool MsgStatusInsert(MsgStatusItem msgStatusItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT COUNT(*) FROM msgstatus WHERE UserId={msgStatusItem.UserId} AND NewsId={msgStatusItem.NewsId}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					if (Convert.ToInt32(sqlCmd.ExecuteScalar()) > 0) return false;

					sqlStr = GetItemInsertString(msgStatusItem, "msgstatus");
					return DoSqlNoQuery(sqlStr);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgStatusInsert), $"msgStatusItem={msgStatusItem}", ex);
					return false;
				}
			}
		}

		public bool MsgStatusUpdate(MsgStatusItem msgStatusItem)
		{
			lock (Locker)
			{
				try
				{
					string updateString = GetItemUpdateString(msgStatusItem, "msgstatus",
						$"NewsId={msgStatusItem.NewsId} AND UserId={msgStatusItem.UserId}");
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgStatusUpdate), $"msgStatusItem={msgStatusItem}", ex);
					return false;
				}
			}
		}

		public int MsgStatusDelete(Int64 userId, Int64 newsId)
		{
			lock (Locker)
			{
				try
				{
					string updateString = $"DELETE FROM msgstatus WHERE UserId={userId} AND NewsId={newsId}";
					DoSqlNoQuery(updateString);
					return GetAffectedRowsCount();
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgStatusDelete), $"userId={userId} newsId={newsId}", ex);
					return -1;
				}
			}
		}

		public List<ConfirmationItem> ConfirmationsLoadAll(bool onlyPending)
		{
			List<ConfirmationItem> items = new List<ConfirmationItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM confirmations WHERE Finished=0";
					if (onlyPending)
					{
						sqlStr += $" AND Sent=0 AND SendRetries<5";
					}
					sqlStr += " ORDER BY ConfId ASC";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						ConfirmationItem item = ItemGetQuery<ConfirmationItem>(sqlReader);
						items.Add(item);
						count++;
					}
					return items;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ConfirmationsLoadAll), "error", ex);
					return null;
				}
			}
		}

		public ConfirmationItem ConfirmationsLoadByType(Int64 userId, ConfirmationTypes type)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM confirmations WHERE Finished=0 AND UserId={userId} AND Type={(int)type}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						ConfirmationItem confItem = ItemGetQuery<ConfirmationItem>(sqlReader);
						return confItem;
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ConfirmationsLoadByType), $"userId={userId} type={type}", ex);
					return null;
				}
			}
		}

		public bool ConfirmationsInsert(ConfirmationItem confirmationItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = GetItemInsertString(confirmationItem, "confirmations");
					return DoSqlNoQuery(sqlStr);
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ConfirmationsInsert), $"confirmationItem={confirmationItem}", ex);
					return false;
				}
			}
		}

		public bool ConfirmationsUpdate(ConfirmationItem confirmationItem)
		{
			lock (Locker)
			{
				try
				{
					string updateString = GetItemUpdateString(confirmationItem, "confirmations",
							$"ConfId={confirmationItem.ConfId}");
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(ConfirmationsUpdate), $"confirmationItem={confirmationItem}", ex);
					return false;
				}
			}
		}

		/// <summary>
		/// Remove subscriptions for user and channels that do no longer exist
		/// </summary>
		public int SubscriptionsCleanup()
		{
			//MessageDispatcher.Instance.Dispatch("SubscriptionsCleanup");
			try
			{
				int count = 0;
				lock (Locker)
				{
					List<UserItem> users = UserLoadAllActive();
					if (users == null) return 0;
					List<ChannelItem> channels = ChannelLoadAll();
					if (channels == null) return 0;
					List<SubscriptionItem> subs = SubscriptionsLoadAll(null);
					if (subs == null) return 0;
					foreach (SubscriptionItem sub in subs)
					{
						UserItem userItem = (from u in users where u.UserId == sub.UserId select u).FirstOrDefault();
						ChannelItem channelItem = (from c in channels where c.ChannelId == sub.ChannelId select c).FirstOrDefault();
						if (userItem != null && channelItem != null) continue;

						// delete subscription
						SubscriptionDelete(sub);
						count++;
						if ((count % 100) == 0)
						{
							MessageDispatcher.Instance.Dispatch($"SubscriptionsCleanup {count} / {subs.Count}");
						}
					}
				}
				_logger.Info(TAG, nameof(SubscriptionsCleanup), $"{count} subscriptions cleared");
				return count;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(SubscriptionsCleanup), $"error", ex);
				return 0;
			}
		}

		/// <summary>
		/// Clear all messages from msgstatus where (user does not exist) or channel is no subscribed 
		/// or msgstatus entry older than 
		/// </summary>
		public int MsgStatusCleanupByDate(int hoursOutdated, int hoursDelete, int hoursOutdatedLocal, int hoursDeleteLocal)
		{
			try
			{
				//MessageDispatcher.Instance.Dispatch("MsgStatusCleanup");
				int delCount = 0;
				int outdatedCount = 0;
				lock (Locker)
				{
					List<UserItem> users = UserLoadAllActive();
					if (users == null) return 0;
					List<NewsItem> news = NewsLoadAll();
					if (news == null) return 0;
					List<ChannelItem> channels = ChannelLoadAll();
					if (channels == null) return 0;
					List<MsgStatusItem> msgStats = MsgStatusLoad(null, null, false, null);
					if (msgStats == null) return 0;

					List<SubscriptionItem> allSubs = SubscriptionsLoadAll(null);

					bool countChanged = false;
					foreach (MsgStatusItem msgStatusItem in msgStats)
					{
						UserItem userItem = (from u in users where u.UserId == msgStatusItem.UserId select u).FirstOrDefault();
						NewsItem newsItem = (from n in news where n.NewsId == msgStatusItem.NewsId select n).FirstOrDefault();
						if (newsItem == null) continue;

						ChannelItem channelItem = null;

						int subsCount = 0;
						if (newsItem != null)
						{
							channelItem = (from c in channels
										   where c.ChannelId == newsItem.ChannelId select c).FirstOrDefault();
							subsCount = (from s in allSubs
										 where s.UserId == userItem.UserId && s.ChannelId == newsItem.ChannelId
										 select s).Count();
						}

						DateTime dtOutdated;
						DateTime dtDelete;
						if (channelItem.ChannelType == ChannelTypes.Local)
						{
							dtOutdated = DateTime.UtcNow.AddHours(-hoursOutdatedLocal);
							dtDelete = DateTime.UtcNow.AddHours(-hoursDeleteLocal);
						}
						else
						{
							dtOutdated = DateTime.UtcNow.AddHours(-hoursOutdated);
							dtDelete = DateTime.UtcNow.AddHours(-hoursDelete);
						}
						// set minutes, seconds = 0
						DateTime utcOutdated = new DateTime(dtOutdated.Year, dtOutdated.Month, dtOutdated.Day, dtOutdated.Hour, 0, 0);
						// set minutes, seconds = 0
						DateTime utcDelete = new DateTime(dtDelete.Year, dtDelete.Month, dtDelete.Day, dtDelete.Hour, 0, 0);

						if (userItem == null || channelItem == null ||
							msgStatusItem.DistribTimeUtc.Date < utcDelete ||
							subsCount == 0)
						{
							// user or channel do not exist
							// or user does not have subscribed this channel
							if (msgStatusItem.DistribTimeUtc.Date < utcDelete)
							{
								// MsgStatus is older than "hoursDelete"
								MsgStatusDelete(msgStatusItem.UserId, msgStatusItem.NewsId);
								delCount++;
								countChanged = true;
							}
							else if (msgStatusItem.SendStatus != (int)MsgStatis.Outdated &&
								msgStatusItem.DistribTimeUtc.Date < utcOutdated)
							{
								// MsgStatus is older than "hoursOutdated"
								msgStatusItem.SendStatus = (int)MsgStatis.Outdated;
								MsgStatusUpdate(msgStatusItem);
								outdatedCount++;
								countChanged = true;
							}
							if (countChanged && (outdatedCount + delCount % 100) == 0)
							{
								countChanged = false;
								MessageDispatcher.Instance.Dispatch(
									null, $"MsgStatusCleanup {outdatedCount}/{delCount} ({msgStats.Count})");
							}
						}
					}
				}
				_logger.Info(TAG, nameof(MsgStatusCleanupByDate), $"{outdatedCount} msgstats outdated, {delCount} msgstats deleted");
				return outdatedCount;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(MsgStatusCleanupByDate), $"error", ex);
				return 0;
			}
		}

		/// <summary>
		/// </summary>
		public int MsgStatusCleanupByMaxPending()
		{
			try
			{
				int surplusCleanedCount = 0;
				lock (Locker)
				{
					List<UserItem> users = UserLoadAllActive();
					if (users == null) return 0;
					List<MsgStatusItem> msgStats = MsgStatusLoad(null, null, true, null);
					if (msgStats == null) return 0;

					foreach (UserItem user in users)
					{
						List<MsgStatusItem> msgStatsUser = (from m in msgStats where m.UserId == user.UserId select m).ToList();
						int surplusCount = msgStatsUser.Count - user.MaxPendingNews;
						if (surplusCount <= 0) continue;

						msgStatsUser.Sort(new MsgsStatusComparer());
						for (int i = 0; i < surplusCount; i++)
						{
							msgStatsUser[i].SendStatusEnum = MsgStatis.Outdated;
							MsgStatusUpdate(msgStatsUser[i]);
							surplusCleanedCount++;
						}
					}
				}
				_logger.Info(TAG, nameof(MsgStatusCleanupByMaxPending), $"{surplusCleanedCount} msgstats max pending");
				return surplusCleanedCount;
			}
			catch (Exception ex)
			{
				_logger.Error(TAG, nameof(MsgStatusCleanupByMaxPending), $"error", ex);
				return 0;
			}
		}


		public int NewsCleanup(int daysOld)
		{
			try
			{
				int delCount = 0;
				lock (Locker)
				{
					DateTime utcLast = DateTime.UtcNow.Date.AddDays(-daysOld);
					List<NewsItem> news = NewsLoadAll();
					if (news == null) return 0;
					List<MsgStatusItem> msgStats = MsgStatusLoad(null, null, true, Constants.MAX_MSG_SEND_RETRIES);
					if (msgStats == null) return 0;

					foreach (NewsItem newsItem in news)
					{
						if (newsItem.NewsTimeUtc.Date > utcLast) continue; // keep

						int pendingCount = (from s in msgStats where s.NewsId == newsItem.NewsId select s).Count();
						if (pendingCount == 0)
						{
							NewsDelete(newsItem.NewsId);
							delCount++;
							if ((delCount % 100) == 0)
							{
								MessageDispatcher.Instance.Dispatch($"News cleanup {delCount} / {news.Count}");
							}
						}
					}
				}
				_logger.Info(TAG, nameof(NewsCleanup), $"{delCount} news deleted");
				return delCount;
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(NewsCleanup), $"error", ex);
				return 0;
			}
		}
	}
}
