using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexBaudotArtServer
{
	class MessageDispatcher
	{
		public delegate void MessageEventHandler(string message);
		public event MessageEventHandler Message;

		/// <summary>
		/// singleton MessageDespatcher
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
	}
}
