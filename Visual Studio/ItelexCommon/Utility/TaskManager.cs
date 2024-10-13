using ItelexCommon.Logger;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon.Utility
{
	public class TaskManager
	{
		private const string TAG = nameof(TaskManager);

		private object _taskItemsLock = new object();
		private List<TaskItem> _taskItems;

		private static TaskManager instance;

		public static TaskManager Instance => instance ?? (instance = new TaskManager());

		private System.Timers.Timer _logTimer;

		private TaskManager()
		{
			lock (_taskItemsLock)
			{
				_taskItems = new List<TaskItem>();
			}
			_logTimer = new System.Timers.Timer(60 * 1 * 1000);
			//_logTimer.Elapsed += LogTimer_Elapsed;
			//_logTimer.Start();
		}

		private void LogTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			LogTaskItems();
		}

		public void AddTask(int? taskId, string className, string procName, string comment = null)
		{
			/*
			if (!taskId.HasValue) return;

			if (!string.IsNullOrEmpty(comment))
			{
				procName += " " + comment;
			}

			TaskItem taskItem = new TaskItem(taskId.Value, className, procName);
			lock(_taskItemsLock)
			{
				_taskItems.Add(taskItem);
			}
			*/
		}

		public void RemoveTask(int? taskId)
		{
			/*
			if (!taskId.HasValue) return;

			lock (_taskItemsLock)
			{
				TaskItem taskItem = (from t in _taskItems where taskId==t.TaskId select t).FirstOrDefault();
				if (taskItem != null)
				{
					_taskItems.Remove(taskItem);
				}
			}
			*/
		}

		private void LogTaskItems()
		{
			lock (_taskItemsLock)
			{
				LogManager.Instance.Logger.Debug(TAG, nameof(LogTaskItems), $"{_taskItems.Count} tasks active");
				for (int t = 0; t < _taskItems.Count; t++)
				{
					LogManager.Instance.Logger.Debug(TAG, nameof(LogTaskItems), $"#{t} {_taskItems[t].ToString()}");
				}
			}
		}
	}

	public class TaskItem
	{
		public int TaskId { get; set; }

		public string ClassName { get; set; }

		public string ProcName { get; set; }

		public DateTime TimeStamp { get; set; }

		public TaskItem(int taskId, string className, string procName)
		{
			TaskId = taskId;
			ClassName = className;
			ProcName = procName;
			TimeStamp = DateTime.Now;
		}

		public override string ToString()
		{
			return $"{TaskId} {ClassName} {ProcName} {TimeStamp:dd.MM. HH:mm:ss}";
		}
	}
}
