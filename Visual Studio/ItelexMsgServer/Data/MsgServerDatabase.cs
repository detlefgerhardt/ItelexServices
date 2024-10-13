using ItelexCommon;
using ItelexCommon.Logger;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer.Data
{
	class MsgServerDatabase: Database
	{
		private static string TAG = nameof(MsgServerDatabase);

		private const int MAX_ITELEX_RETRIES = 5;

		private const string TABLE_USER = "user";
		private const string TABLE_UIDS = "uids";
		private const string TABLE_MSGS = "msgs";
		private const string TABLE_EMAILACCOUNTS = "emailaccounts";
		private const string TABLE_CONFIRMATIONS = "confirmations";
		private const string TABLE_MINITELEXUSER = "minitelexuser";
		private const string TABLE_FAXQUEUE = "faxqueue";

		public object MailGateLocker { get; set; } = new object();

		private static MsgServerDatabase instance;

		public static MsgServerDatabase Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new MsgServerDatabase();
				}
				return instance;
			}
		}

		private MsgServerDatabase(): base(Constants.DATABASE_NAME)
		{
			_logger = LogManager.Instance.Logger;
			if (ConnectDatabase())
			{
				_logger.Notice(TAG, nameof(MsgServerDatabase), $"connected to database {Constants.DATABASE_NAME}");
			}
			else
			{
				_logger.Error(TAG, nameof(MsgServerDatabase), $"failed to connect to database {Constants.DATABASE_NAME}");
			}
		}

		public void CreateDatabase()
		{
		}

		private void ChangeDatabase()
		{
		}


		/*
		public bool UserCheck(string mailAddr, string pin)
		{
			lock (_locker)
			{
				try
				{
					string sqlStr = $"SELECT COUNT(*) FROM user WHERE MailAddr LIKE '{mailAddr}' AND Pin='{pin}'";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					return Convert.ToInt32(sqlCmd.ExecuteScalar()) > 0;
				}
				catch (Exception ex)
				{
					_logging.Error(TAG, nameof(UserCheck), "error", ex);
					return false;
				}
			}
		}
		*/

		public List<UserItem> UserLoadAll()
		{
			List<UserItem> items = new List<UserItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_USER} WHERE Active=1 ORDER BY ItelexNumber";
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
					_logger.Error(TAG, nameof(UserLoadAll), "error", ex);
					return null;
				}
			}

			//_logger.Info(TAG, nameof(UserLoadAll), $"{items.Count} users loaded");
			return items;
		}

		public UserItem UserLoadById(Int64 userId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_USER} WHERE UserId={userId} AND Active=1";
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


		public UserItem UserLoadByMailAddr(string mailAddr)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_USER} WHERE MailAddr LIKE '{mailAddr}' AND Active=1";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						return ItemGetQuery<UserItem>(sqlReader);
					}
					return null;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserLoadByMailAddr), "error", ex);
					return null;
				}
			}
		}

		public UserItem UserLoadByTelexNumber(int number)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_USER} WHERE ItelexNumber={number} AND Active=1";
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
					_logger.Error(TAG, nameof(UserLoadByTelexNumber), $"number={number}", ex);
					return null;
				}
			}
		}

		public UserItem UserLoadByReceiver(string receiver)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_USER} WHERE Receiver='{EscapeSql(receiver)}' AND Active=1";
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
					_logger.Error(TAG, nameof(UserLoadByReceiver), $"receiver={receiver}", ex);
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
					string sqlStr = GetItemInsertString(userItem, TABLE_USER);
					bool ok = DoSqlNoQuery(sqlStr);
					if (!ok) return false;
					userItem.UserId = (Int64)DoSqlScalar($"SELECT last_insert_rowid() FROM {TABLE_USER}");
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserInsert), "error", ex);
					return false;
				}
			}
		}

		public bool UserUpdate(UserItem userItem)
		{
			lock (Locker)
			{
				try
				{
					string updateString = GetItemUpdateString(userItem, TABLE_USER, $"UserId={userItem.UserId}");
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UserUpdate), $"userItem={userItem}", ex);
					return false;
				}
			}
		}

		public List<EmailAccountItem> EmailAccountsLoadAllActive()
		{
			List<EmailAccountItem> items = new List<EmailAccountItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_EMAILACCOUNTS} WHERE Active=1 ORDER BY UserId";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						EmailAccountItem item = ItemGetQuery<EmailAccountItem>(sqlReader);
						items.Add(item);
						count++;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(EmailAccountsLoadAllActive), "error", ex);
					return null;
				}
			}

			//_logger.Info(TAG, nameof(EmailAccountsLoadAllActive), $"{items.Count} email entries loaded");
			return items;
		}

		public List<MsgItem> MsgsLoadAllPending()
		{
			List<MsgItem> items = new List<MsgItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_MSGS} WHERE SendStatus={(int)MsgStatis.Pending}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						MsgItem item = ItemGetQuery<MsgItem>(sqlReader);
						items.Add(item);
						count++;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgsLoadAllPending), "error", ex);
					return null;
				}
			}

			//_logger.Info(TAG, nameof(MsgsLoadAllPending), $"{items.Count} users loaded");
			return items;
		}

		public int MsgsAllPendingCount(Int64 userId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT COUNT(*) FROM {TABLE_MSGS} WHERE UserId={userId} AND SendStatus={(int)MsgStatis.Pending}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					return Convert.ToInt32(sqlCmd.ExecuteScalar());
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgsAllPendingCount), $"userId=={userId}", ex);
					return 0;
				}
			}
		}

		public MsgItem MsgsLoadById(Int64 msgId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_MSGS} WHERE MsgId={msgId}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						MsgItem msgItem = ItemGetQuery<MsgItem>(sqlReader);
						return msgItem;
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgsLoadById), $"msgId={msgId}", ex);
					return null;
				}
			}
		}

		public bool MsgsInsert(MsgItem msgItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = GetItemInsertString(msgItem, TABLE_MSGS);
					bool ok = DoSqlNoQuery(sqlStr);
					if (!ok) return false;
					msgItem.MsgId = (Int64)DoSqlScalar($"SELECT last_insert_rowid() FROM {TABLE_MSGS}");
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgsInsert), $"msgItem={msgItem}", ex);
					return false;
				}
			}
		}

		public bool MsgsUpdate(MsgItem msgItem)
		{
			lock (Locker)
			{
				try
				{
					string updateString = GetItemUpdateString(msgItem, TABLE_MSGS, $"MsgId={msgItem.MsgId}");
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MsgsUpdate), $"msgItem={msgItem}", ex);
					return false;
				}
			}
		}

		public List<FaxQueueItem> FaxQueueLoadAllPending(int maxRetries)
		{
			List<FaxQueueItem> items = new List<FaxQueueItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_FAXQUEUE} WHERE Status={(int)FaxStatis.Pending} AND SendRetries<{maxRetries}";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						FaxQueueItem item = ItemGetQuery<FaxQueueItem>(sqlReader);
						items.Add(item);
						count++;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(FaxQueueLoadAllPending), "error", ex);
					return null;
				}
			}
			return items;
		}

		public int FaxQueueGetFaxId()
		{
			lock (Locker)
			{
				try
				{
					Random rnd = new Random();

					while (true)
					{
						int faxId = rnd.Next(1000, 99999);
						string sqlStr = $"SELECT COUNT(*) FROM {TABLE_FAXQUEUE} WHERE FaxId={faxId}";
						SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
						int cnt = Convert.ToInt32(sqlCmd.ExecuteScalar());
						if (cnt == 0) return faxId;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(FaxQueueInsert), "", ex);
					return 0;
				}
			}
		}

		public bool FaxQueueInsert(FaxQueueItem faxQueueItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = GetItemInsertString(faxQueueItem, TABLE_FAXQUEUE);
					bool ok = DoSqlNoQuery(sqlStr);
					if (!ok) return false;
					faxQueueItem.Id = (Int64)DoSqlScalar($"SELECT last_insert_rowid() FROM {TABLE_FAXQUEUE}");
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(FaxQueueInsert), $"faxQueueItem={faxQueueItem}", ex);
					return false;
				}
			}
		}

		public bool FaxQueueUpdate(FaxQueueItem faxQueueItem)
		{
			lock (Locker)
			{
				try
				{
					string updateString = GetItemUpdateString(faxQueueItem, TABLE_FAXQUEUE, $"FaxId={faxQueueItem.FaxId}");
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(FaxQueueUpdate), $"msgItem={faxQueueItem}", ex);
					return false;
				}
			}
		}

		public bool UidInsert(UidItem uidItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = GetItemInsertString(uidItem, TABLE_UIDS);
					if (!DoSqlNoQuery(sqlStr)) return false;
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UidInsert), $"uidItem={uidItem}", ex);
					return false;
				}
			}
		}

		public UidItem UidLoadById(string uid)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_UIDS} WHERE Uid='{EscapeSql(uid)}'";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						return ItemGetQuery<UidItem>(sqlReader);
					}
					return null;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UidLoadById), "error", ex);
					return null;
				}
			}
		}

		public bool UidExists(string uid)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT COUNT(*) FROM {TABLE_UIDS} WHERE Uid='{EscapeSql(uid)}'";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					return Convert.ToInt32(sqlCmd.ExecuteScalar()) > 0;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(UidExists), "error", ex);
					return false;
				}
			}
		}

		public List<ConfirmationItem> ConfirmationsLoadAll(bool onlyPending, int maxRetries)
		{
			List<ConfirmationItem> items = new List<ConfirmationItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_CONFIRMATIONS} WHERE Finished=0";
					if (onlyPending)
					{
						sqlStr += $" AND Sent=0 AND SendRetries<{maxRetries}";
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
					string sqlStr = $"SELECT * FROM {TABLE_CONFIRMATIONS} WHERE Finished=0 AND UserId={userId} AND Type={(int)type}";
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
					string sqlStr = GetItemInsertString(confirmationItem, TABLE_CONFIRMATIONS);
					bool ok = DoSqlNoQuery(sqlStr);
					if (!ok) return false;
					confirmationItem.ConfId = (Int64)DoSqlScalar($"SELECT last_insert_rowid() FROM {TABLE_CONFIRMATIONS}");
					return true;
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
					string updateString = GetItemUpdateString(confirmationItem, TABLE_CONFIRMATIONS,
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

		public List<MinitelexUserItem> MinitelexUserLoadAll()
		{
			List<MinitelexUserItem> items = new List<MinitelexUserItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_MINITELEXUSER} WHERE Active=1";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						MinitelexUserItem item = ItemGetQuery<MinitelexUserItem>(sqlReader);
						items.Add(item);
						count++;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MinitelexUserLoadAll), "error", ex);
					return null;
				}
			}
			return items;
		}

		public MinitelexUserItem MinitelexUserByPortIndex(int portIndex)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_MINITELEXUSER} WHERE PortIndex={portIndex} AND Active=1";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					if (sqlReader.Read())
					{
						MinitelexUserItem item = ItemGetQuery<MinitelexUserItem>(sqlReader);
						return item;
					}
					else
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MinitelexUserByPortIndex), $"port={portIndex}", ex);
					return null;
				}
			}
		}


		public bool MinitelexUserUpdate(MinitelexUserItem minitelexUserItem)
		{
			lock (Locker)
			{
				try
				{
					string updateString = GetItemUpdateString(minitelexUserItem, TABLE_MINITELEXUSER, $"Id={minitelexUserItem.Id}");
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(MinitelexUserUpdate), $"minitelexUserItem={minitelexUserItem}", ex);
					return false;
				}
			}
		}

	}
}
