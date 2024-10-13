using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon
{
	public class MessageDispatcher
	{
		public delegate void MessageEventHandler(string message);
		public event MessageEventHandler Message;

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static MessageDispatcher instance;

		public static MessageDispatcher Instance => instance ?? (instance = new MessageDispatcher());

		private MessageDispatcher()
		{
		}

		public void Dispatch(string msg)
		{
			Message?.Invoke(msg);
		}

		public void Dispatch(int? connectionId, string msg)
		{
			string connStr;
			if (connectionId.HasValue)
			{
				connStr = $"{connectionId.Value}";
			}
			else
			{
				connStr = "";
			}
			Message?.Invoke($"[{connStr}] {msg}");
		}

		public void Dispatch(int? connectionId, int? number, string msg)
		{
			string connStr;
			if (number.HasValue)
			{
				connStr = $"{number.Value}";
			}
			else if (connectionId.HasValue)
			{
				connStr = $"{connectionId.Value}";
			}
			else
			{
				connStr = "";
			}
			Message?.Invoke($"[{connStr}] {msg}");
		}

		/*
		public void DispatchName(string connectionName, string msg)
		{
			if (string.IsNullOrEmpty(connectionName))
			{
				Message?.Invoke(msg);
			}
			else
			{
				Message?.Invoke($"[{connectionName}] {msg}");
			}
		}
		*/
	}
}
