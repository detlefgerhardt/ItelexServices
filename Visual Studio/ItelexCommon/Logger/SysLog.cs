using SyslogNet.Client.Serialization;
using SyslogNet.Client;
using SyslogNet.Client.Transport;
using System;

namespace ItelexCommon.Logger
{
	public class SysLog
	{
		ISyslogMessageSerializer _serializer;
		ISyslogMessageSender _sender;
		private string _appName;
		private Facility _facility;

		public SysLog(string host, int port, string appName, int facility)
		{
			_appName = appName;
			_serializer = new SyslogRfc3164MessageSerializer();
			_sender = new SyslogUdpSender(host, port);

			Facility[] facilities = new Facility[]
			{
				Facility.LocalUse0,
				Facility.LocalUse1,
				Facility.LocalUse2,
				Facility.LocalUse3,
				Facility.LocalUse4,
				Facility.LocalUse5,
				Facility.LocalUse6,
				Facility.LocalUse7,
			};
			if (facility < 0 || facility > 7) facility = 0;
			_facility = facilities[facility];
		}

		public void Log(LogTypes logType, string msg)
		{
			Severity severity;
			switch (logType)
			{
				case LogTypes.None:
					return;
				case LogTypes.Fatal:
					severity = Severity.Emergency;
					break;
				case LogTypes.Error:
					severity = Severity.Error;
					break;
				case LogTypes.Warn:
					severity = Severity.Warning;
					break;
				case LogTypes.Notice:
					severity = Severity.Notice;
					break;
				case LogTypes.Info:
					severity = Severity.Informational;
					break;
				case LogTypes.Debug:
					severity = Severity.Debug;
					break;
				default:
					severity = Severity.Informational;
					break;
			}

			SyslogMessage sysLogMessage = new SyslogMessage(
							DateTimeOffset.Now,
							_facility,
							severity,
							_appName,
							"", // AppName
							null, // ProcId
							"MessageType", // Message type name ???
							msg); // message to be sent

			//SyslogMessage sysLogMessage2 = new SyslogMessage(
			//				DateTimeOffset.Now,
			//				Facility.LocalUse0, // Facility - LogAlert
			//				Severity.Debug, // Severity
			//				"MailGate", // Host
			//				"", // AppName
			//				null, // ProcId
			//				"MessageType", // Message type name
			//				"message to be sent"); // message to be sent


			try
			{
				_sender.Send(sysLogMessage, _serializer);
				//_sender.Send(sysLogMessage2, _serializer);
			}
			catch (Exception)
			{
			}
		}
	}
}
