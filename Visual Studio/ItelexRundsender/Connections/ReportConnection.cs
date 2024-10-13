using ItelexCommon;
using ItelexCommon.Connection;
using ItelexRundsender.Languages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ItelexRundsender.Connections
{
	class ReportConnection : ItelexOutgoing
	{
		private const string TAG = nameof(OutgoingConnection);

		//private bool _inputActive;
		//private string _inputLine;

		public ReportConnection(int idNumber, int number, string logPath, LogTypes logLevel) : 
				base(idNumber, number, "send report", GetLogger(idNumber, number, logPath, logLevel))
		{
			//_inputActive = false;
		}

		private static ItelexLogger GetLogger(int connectionId, int? number, string logPath, LogTypes logLevel)
		{
			return new ItelexLogger(connectionId, ConnectionDirections.Out, number, logPath, logLevel,
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
		}

		public CallResult Start(int calleeNumber, string reportText)
		{
			ItelexOutgoingConfiguration config = new ItelexOutgoingConfiguration()
			{
				OutgoingType = new LoginSeqTypes[] { LoginSeqTypes.SendKg, LoginSeqTypes.GetKg },
				OurItelexVersionStr = Constants.APP_CODE + Helper.GetVersionCode(),
				ItelexNumber = calleeNumber,
				OurAnswerbackStr = Constants.ANSWERBACK_RUND_DE,
			};
			CallResult result = StartOutgoing(config);
			if (!IsConnected || result.CallStatus != CallStatusEnum.Ok)
			{
				return result;
			}

			SendAscii(reportText);
			SendAscii("\r\n++++\r\n\n");
			Logoff(null);

			return new CallResult(RemoteNumber.Value, this, CallStatusEnum.Ok, null, RemoteItelexVersionStr, RemoteAnswerback);
		}

		/*
		private void ItelexConnection_Received(ItelexConnection connection, string asciiText)
		{
			if (_inputActive)
			{
				_inputLine += asciiText;
			}

			string dump = asciiText;
			dump = dump.Replace("\r", "");
			dump = dump.Replace("\n", "");
			if (!string.IsNullOrWhiteSpace(dump))
			{
				string activeStr = _inputActive ? "1" : "0";
				//Debug.WriteLine($"{activeStr}: {dump}");
			}
		}
		*/

		public override string ToString()
		{
			return $"{RemoteNumber}";
		}
	}
}
