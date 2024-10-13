using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ItelexRundsender.Connections
{
	class OutgoingConnection : ItelexOutgoing
	{
		private const string TAG = nameof(OutgoingConnection);

		private bool _inputActive;
		private string _inputLine;

		//private string _logPath;

		//public int CalleeNumber { get; }

		//public SendProperties SendProps { get;  }

		//public int Retries { get; set; }

		//public string CallerAnswerback { get; }

		//public Answerback Answerback { get; }

		public OutgoingConnection(int connectionId, int number, string comment, ItelexLogger itelexLogger) : 
				base(connectionId, number, comment, itelexLogger)
		{
			_inputActive = false;
			this.ItelexReceived += ItelexConnection_Received;
		}

		public CallResult ConnectAndReadAnswerback(int remoteNumber, string callerAnswerbackStr, bool direct)
		{
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ConnectAndReadAnswerback), "start");

			try
			{
				ItelexOutgoingConfiguration config = new ItelexOutgoingConfiguration()
				{
					OutgoingType = new LoginSeqTypes[] { LoginSeqTypes.SendKg, LoginSeqTypes.SendCustomKg, LoginSeqTypes.GetKg },
					OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
					ItelexNumber = remoteNumber,
					CustomAnswerbackStr = !string.IsNullOrEmpty(callerAnswerbackStr) ? callerAnswerbackStr : remoteNumber.ToString(),
					OurAnswerbackStr = Constants.ANSWERBACK_RUND_DE,
				};
				CallResult result = StartOutgoing(config);
				if (result.CallStatus != CallStatusEnum.Ok) return result;

				Thread.Sleep(500);

				if (!IsConnected)
				{
					_itelexLogger.ItelexLog(LogTypes.Notice, TAG,nameof(ConnectAndReadAnswerback), $"Scan call disconnected by remote, reject-reason={RejectReason}");
					return new CallResult(RemoteNumber.Value, null, CallStatus, RejectReason, GetVersion(), null);
				}

				if (direct)
				{
					SendAscii("mom...\r\n\n");
				}

				if (CallStatus == CallStatusEnum.Ok) CallStatus = CallStatusEnum.InProgress;
				if (IsConnected)
				{
					return new CallResult(RemoteNumber.Value, this, CallStatusEnum.InProgress, null, GetVersion(), RemoteAnswerback);
				}
				else
				{
					return new CallResult(RemoteNumber.Value, this, CallStatusEnum.Aborted, null, GetVersion(), null);
				}
			}
			catch (Exception ex)
			{
				_itelexLogger.ItelexLog(LogTypes.Error, TAG, nameof(ConnectAndReadAnswerback), "", ex);
				return new CallResult(RemoteNumber.Value, this, CallStatusEnum.Error, null, GetVersion(), null);
			}
		}

		public void SendMessage(string text, string recvList)
		{
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(SendMessage), "send message");

			if (!string.IsNullOrEmpty(recvList)) SendAscii($"\r\n{recvList}\r\n");
			SendAscii($"\r\n{text}\r\n+++\r\n");
		}

		public CallResult ReadAnswerbackAndDisconnect()
		{
			_itelexLogger.ItelexLog(LogTypes.Notice, TAG, nameof(ReadAnswerbackAndDisconnect), "start");

			string kennung = GetAnswerback();
			kennung = CleanAnswerback(kennung);
			SendAscii(CodeManager.ASC_FIGS);
			SendAscii($"\r\n{CodeManager.ASC_FIGS}{Constants.ANSWERBACK_RUND_DE}");
			SendAscii($"\r\n\n\n");

			CallResult result;
			if (IsConnected)
			{
				result = new CallResult(RemoteNumber.Value, this, CallStatusEnum.Ok, null, GetVersion(), new Answerback(kennung));
			}
			else
			{
				result = new CallResult(RemoteNumber.Value, null, CallStatusEnum.Error, null, GetVersion(), new Answerback(kennung));
			}
			Logoff(null);

			return result;
		}

		private string CleanAnswerback(string ab)
		{
			ab = ab.Replace('\r', '?');
			ab = ab.Replace('\n', '?');
			return ab;
		}

		private string GetVersion()
		{
			if (RemoteItelexVersion != null)
			{
				return $"{RemoteItelexVersion} {RemoteItelexVersionStr}";
			}
			else
			{
				return null;
			}
		}

		private void ItelexConnection_Received(ItelexConnection connection, string asciiText)
		{
			if (_inputActive)
			{
				_inputLine += asciiText;
			}

			//string dump = asciiText;
			//dump = dump.Replace("\r", "");
			//dump = dump.Replace("\n", "");
			//if (!string.IsNullOrWhiteSpace(dump))
			//{
			//	string activeStr = _inputActive ? "1" : "0";
			//	Debug.WriteLine($"{activeStr}: {dump}");
			//}
		}

		//private void DispatchMsg(string msg)
		//{
		//	MessageDispatcher.Instance.Dispatch($"{ConnectionId}: {msg}");
		//}

		public override string ToString()
		{
			return $"{RemoteNumber}";
		}
	}
}
