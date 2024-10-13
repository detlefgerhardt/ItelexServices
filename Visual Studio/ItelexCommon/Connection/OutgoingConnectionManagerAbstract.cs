using ItelexCommon.Connection;
using ItelexCommon.Logger;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace ItelexCommon
{
	public abstract class OutgoingConnectionManagerAbstract
	{
		private const string TAG = nameof(OutgoingConnectionManagerAbstract);

		protected Logging _logger;

		private readonly object _connectionsLock = new object();
		private List<ItelexOutgoing> _connections;

		public delegate void UpdateEventHandler();
		public event UpdateEventHandler UpdateOutgoing;

		//public delegate void DispatchMessageEventHandler(string message);
		//public event DispatchMessageEventHandler DispatchMessage;

		private MessageDispatcher _messageDispatcher;

		public string OurIpAddressAndPort { get; set; }

		private SubscriberServer _subscriberServer;

		//private ConnectionManagerConfig _config;

		//private bool _shutDown;

		public OutgoingConnectionManagerAbstract()
		{
			_logger = LogManager.Instance.Logger;
			_messageDispatcher = MessageDispatcher.Instance;

			_subscriberServer = new SubscriberServer();
			_connections = new List<ItelexOutgoing>();
		}

		protected virtual ItelexOutgoing CreateConnection(TcpClient client, int connectionId, string logPath)
		{
			return null;
		}

		//private int _currentIdNumber = 0;
		//private object _currentIdNumberLock = new object();

		protected void AddConnection(ItelexOutgoing conn)
		{
			if (conn == null) return;

			_logger.Debug(TAG, nameof(AddConnection), $"{conn.RemoteNumber}");
			lock (_connectionsLock)
			{
				_connections.Add(conn);
			}
			UpdateOutgoing?.Invoke();
		}

		/*
		protected void UpdateOutgoingCall(ItelexOutgoing conn)
		{
			if (conn == null) return;

			/
			ItelexOutgoing outConnExist = _connections.Where(x => x.IsEqual(conn)).FirstOrDefault();
			if (outConnExist != null)
			{
				_connections.Remove(outConnExist);
			}
			/
			//_connections.Add(conn);
			UpdateOutgoing?.Invoke();
		}
		*/

		protected void RemoveConnection(ItelexOutgoing conn)
		{
			if (conn == null) return;

			_logger.Debug(TAG, nameof(RemoveConnection), $"{conn.RemoteNumber}");
			lock (_connectionsLock)
			{
				_connections.Remove(conn);
			}
			UpdateOutgoing?.Invoke();
		}

		// diese methode tut nichts!!!
		protected void RemoveOutgoingCallsByNumber(int calleeNumber)
		{
			lock (_connectionsLock)
			{
				_connections = _connections.Where(c => c.RemoteNumber != calleeNumber).ToList();
			}
		}

		protected ItelexOutgoing GetOutgoingConnectionByNumber(int calleeNumber)
		{
			lock (_connectionsLock)
			{
				return _connections.Where(c => c.RemoteNumber == calleeNumber).FirstOrDefault();
			}
		}

		protected void DispatchUpdateOutgoing()
		{
			UpdateOutgoing?.Invoke();
		}

		protected bool IsOutgoingConnectionActive(int number)
		{
			if (_connections == null) return false;

			lock (_connectionsLock)
			{
				return _connections.Count(c => c.RemoteNumber == number) > 0;
			}
		}

		protected bool IsOutgoingConnectionActive(string host, int port, int ourId)
		{
			if (_connections == null) return false;

			lock (_connectionsLock)
			{
				return _connections.Count(c => c.RemoteHost==host && c.RemotePort==port && c.ConnectionId != ourId) > 0;
			}
		}

		public List<ItelexOutgoing> CloneConnections()
		{
			List<ItelexOutgoing> conns = new List<ItelexOutgoing>();
			lock (_connectionsLock)
			{
				conns.AddRange(_connections);
			}
			return conns;
		}

		protected string LngText(int lngKey, int lngId)
		{
			return LanguageManager.Instance.GetText(lngKey, lngId);
		}

		protected string LngText(int lngKey, int lngId, string[] param)
		{
			return LanguageManager.Instance.GetText(lngKey, lngId, param);
		}


	}

}
