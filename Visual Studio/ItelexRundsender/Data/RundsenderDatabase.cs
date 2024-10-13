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

namespace ItelexRundsender.Data
{
	class RundsenderDatabase: Database
	{
		private static string TAG = nameof(RundsenderDatabase);

		private static RundsenderDatabase instance;

		private const string TABLE_USER = "user";
		private const string TABLE_CONFIRMATIONS = "confirmations";
		private const string TABLE_GROUPS = "groups";
		private const string TABLE_GROUPMEMBERS = "groupmembers";

		public static RundsenderDatabase Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new RundsenderDatabase();
				}
				return instance;
			}
		}

		private RundsenderDatabase(): base(Constants.DATABASE_NAME)
		{
			_logger = LogManager.Instance.Logger;
			if (ConnectDatabase())
			{
				_logger.Notice(TAG, nameof(RundsenderDatabase), $"connected to database {Constants.DATABASE_NAME}");
			}
			else
			{
				_logger.Error(TAG, nameof(RundsenderDatabase), $"failed to connect to database {Constants.DATABASE_NAME}");
			}
		}

		public void CreateDatabase()
		{
		}

		private void ChangeDatabase()
		{
		}

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

		public List<GroupItem> GroupsLoadAll()
		{
			List<GroupItem> items = new List<GroupItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_GROUPS} WHERE Active=1 ORDER BY Name ASC";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						GroupItem item = ItemGetQuery<GroupItem>(sqlReader);
						items.Add(item);
						count++;
					}
					return items;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(GroupsLoadAll), "error", ex);
					return null;
				}
			}
		}

		public bool GroupInsert(GroupItem groupItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = GetItemInsertString(groupItem, TABLE_GROUPS);
					bool ok = DoSqlNoQuery(sqlStr);
					if (!ok) return false;
					groupItem.GroupId = (Int64)DoSqlScalar($"SELECT last_insert_rowid() FROM {TABLE_GROUPS}");
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(GroupInsert), "error", ex);
					return false;
				}
			}
		}

		public bool GroupUpdate(GroupItem groupItem)
		{
			lock (Locker)
			{
				try
				{
					string updateString = GetItemUpdateString(groupItem, TABLE_GROUPS, $"GroupId={groupItem.GroupId}");
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(GroupUpdate), $"groupItem={groupItem}", ex);
					return false;
				}
			}
		}

		public bool GroupDelete(Int64 groupId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"DELETE FROM {TABLE_GROUPS} WHERE GroupId={groupId}";
					DoSqlNoQuery(sqlStr);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(GroupDelete), $"groupId={groupId}", ex);
					return false;
				}
			}
		}

		public List<GroupMemberItem> GroupMembersLoadByGroup(Int64? groupId = null)
		{
			List<GroupMemberItem> items = new List<GroupMemberItem>();
			int count = 0;

			lock (Locker)
			{
				try
				{
					string sqlStr = $"SELECT * FROM {TABLE_GROUPMEMBERS} WHERE Active=1";
					if (groupId.HasValue)
					{
						sqlStr += $" AND GroupId={groupId}";
					}
					sqlStr += " ORDER BY Number ASC";
					SQLiteCommand sqlCmd = new SQLiteCommand(sqlStr, _sqlConn);
					SQLiteDataReader sqlReader = sqlCmd.ExecuteReader();
					while (sqlReader.Read())
					{
						GroupMemberItem item = ItemGetQuery<GroupMemberItem>(sqlReader);
						items.Add(item);
						count++;
					}
					return items;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(GroupMembersLoadByGroup), "error", ex);
					return null;
				}
			}
		}

		public bool GroupMemberInsert(GroupMemberItem groupMemberItem)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = GetItemInsertString(groupMemberItem, TABLE_GROUPMEMBERS);
					bool ok = DoSqlNoQuery(sqlStr);
					if (!ok) return false;
					groupMemberItem.MemberId = (Int64)DoSqlScalar($"SELECT last_insert_rowid() FROM {TABLE_GROUPMEMBERS}");
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(GroupMemberInsert), "error", ex);
					return false;
				}
			}
		}

		public bool GroupMemberUpdate(GroupMemberItem groupMemberItem)
		{
			lock (Locker)
			{
				try
				{
					string updateString = GetItemUpdateString(groupMemberItem, TABLE_GROUPMEMBERS, $"MemberId={groupMemberItem.MemberId}");
					DoSqlNoQuery(updateString);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(GroupMemberUpdate), $"groupMemberItem={groupMemberItem}", ex);
					return false;
				}
			}
		}


		public bool GroupMemberDeleteByGroupIdAndNumber(Int64 groupId, int number)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"DELETE FROM {TABLE_GROUPMEMBERS} WHERE GroupId={groupId} AND Number={number}";
					DoSqlNoQuery(sqlStr);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(GroupMemberDeleteByGroupIdAndNumber), $"groupId={groupId}, number={number}", ex);
					return false;
				}
			}
		}

		public bool GroupMemberDeleteByGroupId(Int64 groupId)
		{
			lock (Locker)
			{
				try
				{
					string sqlStr = $"DELETE FROM {TABLE_GROUPMEMBERS} WHERE GroupId={groupId}";
					DoSqlNoQuery(sqlStr);
					return true;
				}
				catch (Exception ex)
				{
					_logger.Error(TAG, nameof(GroupMemberDeleteByGroupId), $"groupId={groupId}", ex);
					return false;
				}
			}
		}

	}
}
