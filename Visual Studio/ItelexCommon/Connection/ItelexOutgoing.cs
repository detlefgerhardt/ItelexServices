using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ItelexCommon.Connection
{
	public class ItelexOutgoing : ItelexConnection
	{
		private const string TAG = nameof(ItelexOutgoing);

		public delegate void GotAnswerbackEventHandler(ItelexOutgoing connection);
		public event GotAnswerbackEventHandler GotAnswerback;

		protected ItelexOutgoingConfiguration _outgoingConfiguration;

		private bool _inputActive;
		private string _inputLine;
		private object _inputLineLock = new object();

		public ItelexOutgoing(int connectionId, int number, string comment, ItelexLogger itelexLogger) : 
			base(ConnectionDirections.Out, null, connectionId, number, comment, itelexLogger)
		{
		}

		public CallResult StartOutgoing(ItelexOutgoingConfiguration config)
		{
			_outgoingConfiguration = config;
			RemoteNumber = config.ItelexNumber;
			ConnectionRetryCnt = config.RetryCnt;

			SubscriberServer server = new SubscriberServer();
			server.Connect(null, null);
			PeerQueryReply reply = server.SendPeerQuery(RemoteNumber.Value);
			server.Disconnect();
			if (reply == null || !reply.Valid)
			{
				_itelexLogger.ItelexLog(LogTypes.Warn, TAG, nameof(StartOutgoing), $"Could not query {RemoteNumber.Value}");
				return new CallResult(RemoteNumber.Value, null, CallStatusEnum.QueryError, null, "", null);
			}

			RemoteHost = reply.Data.Address;
			RemotePort = reply.Data.PortNumber;
			RemoteExtensionNo = reply.Data.ExtensionNumber;

			// check if destination is a service
			bool isDestMinitelex = reply.Data.LongName.ToLower().Contains(":minitelex");
			if (isDestMinitelex)
			{
				MessageDispatcher.Instance.Dispatch($"{RemoteNumber} is Minitelex");
				_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(StartOutgoing), $"{RemoteNumber} is Minitelex");
			}

			if (config.IsConnectionActive != null)
			{
				if (config.IsConnectionActive(RemoteHost, RemotePort, ConnectionId))
				{
					// connection to same host and port is already active
					// funktioniert nicht: race condition
					//return new CallResult(RemoteNumber.Value, null, CallStatusEnum.AlreadyActive, "already active", "", null);
				}
			}

			bool connect = ConnectOut(config, !isDestMinitelex);
			if (!connect || !IsConnected)
			{
				return new CallResult(RemoteNumber.Value, null, CallStatus, RejectReason, RemoteItelexVersionStr, null);
			}

			if (!isDestMinitelex)
			{
				if (_outgoingConfiguration.OutgoingType.Contains(LoginSeqTypes.SendKg) &&
					!string.IsNullOrEmpty(_outgoingConfiguration.OurAnswerbackStr))
				{
					SendAscii($"\r\n{_outgoingConfiguration.OurAnswerbackStr}");
				}

				if (_outgoingConfiguration.OutgoingType.Contains(LoginSeqTypes.SendCustomKg) &&
					!string.IsNullOrEmpty(_outgoingConfiguration.CustomAnswerbackStr))
				{
					SendAscii($"\r\n{_outgoingConfiguration.CustomAnswerbackStr}");
				}

				if (_outgoingConfiguration.OutgoingType.Contains(LoginSeqTypes.GetKg))
				{
					WaitAllSendBuffersEmpty();
					RemoteAnswerback = new Answerback(GetAnswerback());
					GotAnswerback?.Invoke(this);
				}

				SendAscii("\r\n");
			}
			else
			{
				// wait for WRU und send KG
				//MessageDispatcher.Instance.Dispatch($"{RemoteNumber} wait WRU");
				_itelexLogger.ItelexLog(LogTypes.Warn, TAG, nameof(StartOutgoing), $"wait WRU");
				this.ItelexReceived += Outgoing_ItelexReceived;
				_inputLine = "";
				_inputActive = true;

				TickTimer timeout = new TickTimer();
				while(!timeout.IsElapsedMilliseconds(20000))
				{
					if (_inputLine.Contains(CodeManager.ASC_WRU))
					{
						//MessageDispatcher.Instance.Dispatch($"{RemoteNumber} send kg {_outgoingConfiguration.OurAnswerbackStr}");
						_itelexLogger.ItelexLog(LogTypes.Debug, TAG, nameof(StartOutgoing), $"send kg {_outgoingConfiguration.OurAnswerbackStr}");
						SendAscii($"\r\n{_outgoingConfiguration.OurAnswerbackStr}");
						WaitAllSendBuffersEmpty();
						Thread.Sleep(5000);
						break;
					}
					Thread.Sleep(100);
				}
				_inputActive = false;

				// wait until answerback exchange is finished
				//WaitForSilence(5000, 5000, 2000);
				this.ItelexReceived -= Outgoing_ItelexReceived;

			}

			return new CallResult(RemoteNumber.Value, null, CallStatusEnum.Ok, null, RemoteItelexVersionStr,
					RemoteAnswerback);
		}

		private void Outgoing_ItelexReceived(ItelexConnection connection, string asciiText)
		{
			if (_inputActive)
			{
				lock (_inputLineLock)
				{
					_inputLine += asciiText;
				}
			}
		}

		public bool IsEqual(ItelexConnection conn)
		{
			return conn.ConnectionId == ConnectionId;
		}
	}
}
