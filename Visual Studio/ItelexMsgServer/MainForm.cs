using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Monitor;
using ItelexCommon.Utility;
using ItelexMsgServer.Connections;
using ItelexMsgServer.Data;
using ItelexMsgServer.Fax;
using ItelexMsgServer.Mail;
using ItelexMsgServer.Serial;
using ItelexMsgServer.Telegram;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;

namespace ItelexMsgServer
{
	public partial class MainForm : Form
	{
		private const string TAG = nameof(MainForm);

		private readonly Logging _logger;

		private readonly MailManager _msgManager;

		private readonly MinitelexConnectionManager _minitelexManager;

		private bool _exit = false;

		public MainForm()
		{
			InitializeComponent();

			_logger = LogManager.Instance.Logger;

			ContextMenu trayMenu = new ContextMenu();
			trayMenu.MenuItems.Add("Show", TrayShow);
			trayMenu.MenuItems.Add("Shutdown", TrayShutdown);
			NotifyIcon1.ContextMenu = trayMenu;
			NotifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
			NotifyIcon1.BalloonTipText = $"{Constants.PROGRAM_NAME} {Constants.DEFAULT_NUMBER_DE}";
			NotifyIcon1.Text = $"{Constants.PROGRAM_NAME} {Constants.DEFAULT_NUMBER_DE}";

			this.Text = Helper.GetVersion();

			_msgManager = MailManager.Instance;

			MainServiceCtrl.LoginLogoffEvent += MainServiceCtrl_LoginLogoff;
			MainServiceCtrl.ShutDownEvent += MainServiceCtrl_ShutDown;
			IncomingConnectionManagerConfig config = new IncomingConnectionManagerConfig()
			{
				LogLevel = Constants.LOG_LEVEL,
				PrgmVersionStr = Helper.GetVersionCode(),
				ItelexNumber = Constants.DEFAULT_NUMBER_DE,
				ExePath = Helper.GetExePath(),
				LogPath = Constants.LOG_PATH,
				GetNewSession = GetNewSessionNo,
				FixDns = Constants.FIX_DNS,
				Host = "?",
				IncomingLocalPort = Constants.DEFAULT_LOCAL_PORT,
				IncomingPublicPort = Constants.DEFAULT_PUBLIC_PORT,
				TlnServerServerPin = Constants.DEFAULT_PIN,
				MonitorServerType = MonitorServerTypes.ItelexMsgServer,
				MonitorPort = Constants.DEFAULT_MONITOR_PORT,
				ItelexExtensions = new ItelexExtensionConfiguration[]
				{
					new ItelexExtensionConfiguration(11, Constants.DEFAULT_NUMBER_DE, "de", Constants.PROGRAM_SHORTNAME_REG_DE,
						Constants.ANSWERBACK_DE, true),
					new ItelexExtensionConfiguration(12, Constants.DEFAULT_NUMBER_EN, "en", Constants.PROGRAM_SHORTNAME_REG_EN,
						Constants.ANSWERBACK_EN, false),
				}
			};

			MainServiceCtrl.StartIncomingConnections<IncomingConnectionManager>(config);
			MainServiceCtrl.StartOutgoingConnections<OutgoingManager>();

			// --- Minitelex ---

			IncomingConnectionManagerConfig minitelexConfig = new IncomingConnectionManagerConfig()
			{
				LogLevel = Constants.LOG_LEVEL,
				PrgmVersionStr = Helper.GetVersionCode(),
				//ItelexNumber = Constants.DEFAULT_NUMBER_DE,
				ExePath = Helper.GetExePath(),
				LogPath = Constants.LOG_PATH,
				GetNewSession = GetNewSessionNo,
				//FixDns = Constants.FIX_DNS,
				//Host = "?",
				IncomingLocalPort = Constants.MINITELEX_LOCAL_PORT,
				IncomingPublicPort = Constants.MINITELEX_PUBLIC_PORT,
				//TlnServerServerPin = Constants.DEFAULT_PIN,
				//MonitorServerType = MonitorServerTypes.ItelexMsgServer,
				//MonitorPort = Constants.DEFAULT_MONITOR_PORT,
				ItelexExtensions = new ItelexExtensionConfiguration[]
				{
					new ItelexExtensionConfiguration(null, 0, "de", null, null, true),
				}
			};
			_minitelexManager = MinitelexConnectionManager.Instance;
			_minitelexManager.SetRecvOn2(minitelexConfig);

			// --- Minitelex ---

			// do not start befor OutgoingManager ist set up
			TelegramBot.Instance.Start();

			FaxManager.Instance.Start();

			PruefTexte.Instance.ReadTexte();

#if DEBUG
			//DebugFormManager.Instance.OpenForm();
			//int? count = _database.GetLastHourSendCount();
			//UserItem user = MailGateDatabase.Instance.UserLoadById(5);
			//MsgItem msg = MailGateDatabase.Instance.MsgsLoadById(5);

			//CommonHelper.SendWebMail("webmailtest an 211231", "#211231\r\ntest über webmailer", "mfk@telexgate.de");


			TestFax();
			//TestMail();
			//TestPdf();
			//ScanNum();
			//TestTeleConn();

			//TestTiff();
#endif
		}

#if DEBUG
		private void TestMail()
		{
			//MailManager.Instance.ReceiveAndDistributeMails();
			byte[] data = new byte[] { 0x01, 0x02, 0x03 };

			MailManager.Instance.SendMailSmtp(
					PrivateConstants.DEBUG_EMAIL_ADDRESS, "Test mit Anhang", "Die Nachricht", 211231, "test.bin", data);
		}

		private void TestFax()
		{
			/*
			string faxName = "TestFax.pdf";
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("12345678901234567890123456789012345678901234567890123456789012345678");
			for (int i = 2; i < 100; i++)
			{
				sb.AppendLine($"Zeile {i}");
			}

			PdfCreator2 pdfCreator = new PdfCreator2();
			pdfCreator.Create(faxName, sb.ToString());
			WinFaxManager.Instance.SendFax(211231, "06426921125", null, faxName);
			*/

			//FaxManager.Instance.Test3();

			//SerialPort p = new SerialPort("COM3");
			//p.Open();
			//p.Close();
			//p = new SerialPort("COM3");
			//p.Open();
			//p.Close();

			FaxQueueItem item = new FaxQueueItem()
			{
				Id = 1,
				FaxId = 1,
				UserId = 0,
				FaxFormat = (int)FaxFormat.Endless,
				EdgePrint = true,
				Language = "de",
				IsMiniTelex = false,
				Sender = "7822222",
				Receiver = "06426 242319"
			};
			//              12345678901234567890123456789012345678901234567890123456789012345678
			item.Message = "Das hier ein eine Testnachricht. Die ist achtundsechzig zeichen lang";
			FaxManager.Instance.TransmitFax(item);

			//FaxManager.Instance.SendFax("Das hier ein eine Testnachricht. Die ist achtundsechzig zeichen lang",
			//		FaxFormat.Endless, true, 0, true, "7822222", "06426 242319", "de");
		}

		/*
		private void TestPdf()
		{
			PdfCreator2 pdf = new PdfCreator2();
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("12345678901234567890123456789012345678901234567890123456789012345678");
			for (int i=2; i<=62; i++)
			{
				sb.AppendLine($"zeile {i}");
			}
			pdf.Create("fax_gr1.pdf", sb.ToString());
		}
		*/

		private void ScanNum()
		{
			string line = "#1234,1235 ,1236 , 1237";
			MailManager.ParseNums(ref line);
			line = "#1234,1235,1236,1237A";
			MailManager.ParseNums(ref line);
		}

		private void TestTeleConn()
		{
			TelegramConnection conn = new TelegramConnection(0, null, null);
			conn.AddText("abcdef\r");
			string line = conn.GetLine(false);
			Debug.WriteLine(line);
			Debug.WriteLine(conn.CurrentLine);
		}

		private void TestTiff()
		{
			//FaxConversion.CreateCcittTiff();
			//return;
			//FaxConversion.ConvHexToBin();
			//return;

			//FaxModem mdm = new FaxModem();
			//mdm.SendFax();
			//FaxCompression.Test();
		}
#endif

		private void MainServiceCtrl_LoginLogoff(string msg)
		{
			NotifyIcon1.BalloonTipTitle = Constants.PROGRAM_NAME;
			NotifyIcon1.BalloonTipText = msg;
			NotifyIcon1.ShowBalloonTip(1000);
		}

		private void MainServiceCtrl_ShutDown()
		{
			//_connectionManager.Shutdown();
			_exit = true;
			FormsHelper.ControlInvokeRequired(this, () => Close());
			//Close();
		}

		private int GetNewSessionNo(int oldSessionId)
		{
			return Helper.GetNewSessionNo(oldSessionId);
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!_exit)
			{
				e.Cancel = true;
				Hide();
				//NotifyIcon1.BalloonTipText = $"ITelexChatServer {ConfigManager.Instance.Config.OwnNumber}";
				//NotifyIcon1.Text = $"{Constants.PROGRAM_NAME} {ConfigManager.Instance.Config.OwnNumber}";
				NotifyIcon1.ShowBalloonTip(1000);
			}
			else
			{
				//_monitorManager.ShutDownPrgm();
			}
		}

		private void TrayShutdown(object sender, EventArgs e)
		{
			_exit = true;
			Close();
		}

		private void TrayShow(object sender, EventArgs e)
		{
			Show();
		}
	}
}
