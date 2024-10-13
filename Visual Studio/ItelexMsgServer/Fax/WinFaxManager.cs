using FAXCOMEXLib;
using ItelexCommon;
using ItelexCommon.Logger;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ItelexMsgServer.Fax
{
	class WinFaxManager
	{
		private const string TAG = nameof(WinFaxManager);

		private static FaxServer faxServer;
		private FaxDocument faxDoc;

		private Logging _logger;
		private MessageDispatcher _messageDispatcher;

		private object _faxlocker;

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static WinFaxManager instance;

		public static WinFaxManager Instance => instance ?? (instance = new WinFaxManager());

		private WinFaxManager()
		{
			_logger = LogManager.Instance.Logger;
			_messageDispatcher = MessageDispatcher.Instance;
			_faxlocker = new object();

			try
			{
				faxServer = new FaxServer();
				faxServer.Connect(Environment.MachineName);

				faxServer.OnOutgoingJobAdded += FaxServer_OnOutgoingJobAdded;
				faxServer.OnOutgoingJobChanged += FaxServer_OnOutgoingJobChanged;
				faxServer.OnOutgoingJobRemoved += FaxServer_OnOutgoingJobRemoved;

				var eventsToListen =
						  FAX_SERVER_EVENTS_TYPE_ENUM.fsetFXSSVC_ENDED | FAX_SERVER_EVENTS_TYPE_ENUM.fsetOUT_QUEUE
						| FAX_SERVER_EVENTS_TYPE_ENUM.fsetOUT_ARCHIVE | FAX_SERVER_EVENTS_TYPE_ENUM.fsetQUEUE_STATE
						| FAX_SERVER_EVENTS_TYPE_ENUM.fsetACTIVITY | FAX_SERVER_EVENTS_TYPE_ENUM.fsetDEVICE_STATUS;
				faxServer.ListenToServerEvents(eventsToListen);

				var devices = faxServer.GetDevices();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		public void SendFax(int? fromItelex, string faxNumber, string faxMsg, string debugFile = null)
		{
			Task.Run(() =>
			{
				lock (_faxlocker)
				{
					_messageDispatcher.Dispatch($"send fax to {faxNumber}");
					try
					{
						string fullName;
						if (debugFile == null)
						{
							PdfCreator2 pdf = new PdfCreator2();
							string fileName = $"{faxNumber}_{DateTime.Now:yyMMddHHmmss}.pdf";
							fullName = Path.Combine(Constants.FAX_PATH, fileName);
							pdf.Create(fileName, faxMsg);
						}
						else
						{
							fullName = Path.Combine(Constants.FAX_PATH, debugFile);
						}

						FaxDocumentSetup(fromItelex, faxNumber, fullName);
						object submitReturnValue = faxDoc.Submit(faxServer.ServerName);
						faxDoc = null;
					}
					catch (COMException comException)
					{
						_logger.Error(TAG, nameof(SendFax), "comException", comException);
					}
					catch (Exception ex)
					{
						_logger.Error(TAG, nameof(SendFax), "error", ex);
					}
				}
			});
		}

		private void FaxDocumentSetup(int? fromItelex, string faxNumber, string fullName)
		{
			faxDoc = new FaxDocument();
			faxDoc.Priority = FAX_PRIORITY_TYPE_ENUM.fptHIGH;
			faxDoc.ReceiptType = FAX_RECEIPT_TYPE_ENUM.frtNONE;
			faxDoc.DocumentName = $"i-Telex from {fromItelex} to {faxNumber}";
			faxDoc.AttachFaxToReceipt = true;
			faxDoc.Sender.Name = $"i-Telex {fromItelex}";
			faxDoc.Sender.Company = fromItelex.HasValue ? $"{fromItelex}" : "unknown";
			faxDoc.Body = fullName;
			faxDoc.Subject = $"i-Telex {fromItelex}";
			faxDoc.Recipients.Add(faxNumber, "i-telex");
		}

		private void FaxServer_OnOutgoingJobRemoved(FaxServer pFaxServer, string bstrJobId)
		{
			//throw new NotImplementedException();
		}

		private void FaxServer_OnOutgoingJobChanged(FaxServer pFaxServer, string bstrJobId, FaxJobStatus pJobStatus)
		{
			//throw new NotImplementedException();
		}

		private void FaxServer_OnOutgoingJobAdded(FaxServer pFaxServer, string bstrJobId)
		{
			//throw new NotImplementedException();
		}

		public static string FormatFaxNumber(string number)
		{
			return number;
		}
	}
}
