using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Monitor;
using ItelexCommon.Utility;
using ItelexRundsender.Connections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ItelexCommon.CallResult;

// Todo:
// x Meldung an alle bei neuer verbindung
// x Meldungsfenster zum Ende scrollen
// x Leerzeilen optimieren, kein neue Zeile mit ':' wenn Eingabe leer
// x WRU beim Schreiben unterdrucken
// x KG-Abfrage beim Anmelden und Anzeige in der Teilnehmerliste
// ? try/catches wegen Programmabsturz
// ? Connections durch lock schützen
// - Kurzname zusammen mit KG speichern (sqlite)
// - Beim Login abruf des KG des Dienstes abwarte (Testen mit Tastaturwahl)
// - Vor Input warten, bis alle Zeichen gesendet wurden (alle Puffer leer)

namespace ItelexRundsender
{
	public partial class MainForm : Form
	{
		private static readonly string TAG = nameof(MainForm);

		private readonly Logging _logger;

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
			NotifyIcon1.BalloonTipText = $"{Constants.PROGRAM_NAME} {Constants.ITELEX_NUMBER_RUND_DE}";
			NotifyIcon1.Text = $"{Constants.PROGRAM_NAME} {Constants.ITELEX_NUMBER_RUND_DE}";

			this.Text = Helper.GetVersion();

			MainServiceCtrl.LoginLogoffEvent += MainServiceCtrl_LoginLogoff;
			MainServiceCtrl.ShutDownEvent += MainServiceCtrl_ShutDown;
			IncomingConnectionManagerConfig config = new IncomingConnectionManagerConfig()
			{
				LogLevel = Constants.LOG_LEVEL,
				PrgmVersionStr = Helper.GetVersionCode(),
				ItelexNumber = Constants.ITELEX_NUMBER_RUND_DE,
				ExePath = Helper.GetExePath(),
				LogPath = Constants.LOG_PATH,
				GetNewSession = GetNewSessionNo,
				FixDns = Constants.FIX_DNS,
				Host = "?",
				IncomingLocalPort = Constants.LOCAL_PORT,
				IncomingPublicPort = Constants.PUBLIC_PORT,
				TlnServerServerPin = Constants.ITELEX_PIN,
				MonitorServerType = MonitorServerTypes.ItelexRundsender,
				MonitorPort = Constants.MONITOR_PORT,
				ItelexExtensions = new ItelexExtensionConfiguration[]
				{
					new ItelexExtensionConfiguration(Constants.EXTNUM_RUND_DE, Constants.ITELEX_NUMBER_RUND_DE, "de",
							Constants.PRGMNAME_RUND_DE, Constants.ANSWERBACK_RUND_DE, true),
					new ItelexExtensionConfiguration(Constants.EXTNUM_RUND_EN, Constants.ITELEX_NUMBER_RUND_EN, "en",
							Constants.PRGMNAME_RUND_EN, Constants.ANSWERBACK_RUND_EN, false),
					new ItelexExtensionConfiguration(Constants.EXTNUM_ADMIN_DE, Constants.ITELEX_NUMBER_ADMIN_DE, "de",
							Constants.PRGMNAME_ADMIN_DE, Constants.ANSWERBACK_ADMIN_DE, false),
					new ItelexExtensionConfiguration(Constants.EXTNUM_ADMIN_EN, Constants.ITELEX_NUMBER_ADMIN_EN, "en",
							Constants.PRGMNAME_ADMIN_EN, Constants.ANSWERBACK_ADMIN_EN, false)
				}
			};

			MainServiceCtrl.StartIncomingConnections<RundsenderConnectionManager>(config);
			MainServiceCtrl.StartOutgoingConnections<OutgoingConnectionManager>();

			//Test1();
			//Test2();
		}

		private void Test1()
		{
			/*
			int sendCnt = 10;
			int recvAck = 0;

			for (int i=0; i<30; i++)
			{
				sendCnt += 10;
				if (sendCnt > 255) sendCnt -= 256;
				recvAck += 10;
				if (recvAck > 255) recvAck -= 256;
				int remoteBuf = sendCnt - recvAck;
				if (remoteBuf > 255)
				{
					remoteBuf -= 256;
				}
				if (remoteBuf < 0) remoteBuf = 256 + remoteBuf;
				Debug.WriteLine($"{sendCnt} {recvAck} {remoteBuf}");
			}
			*/

			/*
			SendProperties props = new SendProperties();
			props.MessageText = "test\r\rtest\r\n";
			Debug.WriteLine(props.GetMessageTextAscii());
			*/

			/*
			string s = "test";
			for(int i=-1; i<=5; i++)
			{
				for (int j=-1; j<=5; j++)
				{
					Debug.WriteLine($"{i} {j} {s.ExSubstring(i)}");
				}
			}
			*/

			HashSet<int> hashSet = new HashSet<int>();
			hashSet.Add(1);
			hashSet.Add(1);
			hashSet.Add(2);
		}

		private void Test2()
		{
			/*
			string str = "01234+456789";
			string str1 = str.ExRightString("+");
			string str2 = str.ExLeftString("+");

			string str3 = str.ExCropLeft("123");
			string str4 = str.ExCropRight("678");

			//ParseNumbersResult result = RundsenderConnection.ParseNumbers("212131+");
			ParseNumbersResult result1 = RundsenderConnection.ParseNumbers("empf,7822222\r\n211231\n-7822222\r211230 e e e");

			string numStr = "";
			for (int i = 10000; i < 10050; i++)
			{
				numStr += i.ToString() + "\r\n";
			}
			ParseNumbersResult result2 = RundsenderConnection.ParseNumbers(numStr);
			string str5 = SendProperties.GetReceiverLines(result2.Receivers, 68, "empf: ");
			*/
		}

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

		private void MainForm_Load(object sender, EventArgs e)
		{
#if DEBUG
			//((OutgoingConnectionManager)GlobalData.Instance.OutgoingConnectionManager).TestDelayedAll();
#endif
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

	/*
	class CountItem
	{
		public int Wert { get; set; }

		public CountItem(int wert)
		{
			Wert = wert;
		}
	}
	*/
}