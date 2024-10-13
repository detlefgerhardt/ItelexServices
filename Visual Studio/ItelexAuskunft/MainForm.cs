using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Utility;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telexsuche;

// Todo:

namespace ItelexAuskunft
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
			NotifyIcon1.BalloonTipText = $"{Constants.PROGRAM_NAME} {Constants.DEFAULT_NUMBER}";
			NotifyIcon1.Text = $"{Constants.PROGRAM_NAME} {Constants.DEFAULT_NUMBER}";

			this.Text = Helper.GetVersion();

			MainServiceCtrl.LoginLogoffEvent += MainServiceCtrl_LoginLogoff;
			MainServiceCtrl.ShutDownEvent += MainServiceCtrl_ShutDown;
			IncomingConnectionManagerConfig config = new IncomingConnectionManagerConfig()
			{
				LogLevel = Constants.LOG_LEVEL,
				PrgmVersionStr = Helper.GetVersionCode(),
				ItelexNumber = Constants.DEFAULT_NUMBER,
				ExePath = Helper.GetExePath(),
				LogPath = Constants.LOG_PATH,
				GetNewSession = GetNewSessionNo,
				FixDns = Constants.FIX_DNS,
				Host = "?",
				IncomingLocalPort = Constants.DEFAULT_LOCAL_PORT,
				IncomingPublicPort = Constants.DEFAULT_PUBLIC_PORT,
				TlnServerServerPin = Constants.DEFAULT_PIN,
				MonitorServerType = ItelexCommon.Monitor.MonitorServerTypes.ItelexAuskunft,
				MonitorPort = Constants.DEFAULT_MONITOR_PORT,
				ItelexExtensions = new ItelexExtensionConfiguration[]
				{
					new ItelexExtensionConfiguration(Constants.DEFAULT_EXTENSION, Constants.DEFAULT_NUMBER, "de",
						Constants.PROGRAM_SHORTNAME, Constants.DEFAULT_ANSWERBACK, true)
				}
			};
			MainServiceCtrl.StartIncomingConnections<AuskunftConnectionManager>(config);

			LoadTables();

			MainServiceCtrl.SetButton1("Create tables", Button1_CreateTables);

			//Tests();
		}

		private void Tests()
		{
			/*
			//string[] searchWords = new string[] { "Paderborn", "Nixdorf" };
			string[] searchWords = new string[] { "Beck", "Wetzlar" };
			List<TextTafelElement> results = Abfrage.Instance.Suche(searchWords, out int count, out bool badEsbToAsb);
			string resultStr = "";
			AuskunftConnection conn = new AuskunftConnection(null, 0, "test", Helper.GetExePath()); ;
			foreach (TextTafelElement result in results)
			{
				resultStr += conn.GetResultStr(result);
			}
			resultStr += "\r\n++";
			*/
		}

		private void MainServiceCtrl_LoginLogoff(string msg)
		{
			NotifyIcon1.BalloonTipTitle = Constants.PROGRAM_SHORTNAME;
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

		private void Button1_CreateTables()
		{
			string path = Helper.GetExePath();
			_logger.Notice(TAG, nameof(Button1_CreateTables), $"srcPath={path}");
			_logger.Notice(TAG, nameof(Button1_CreateTables), $"zielPath={path}");
			ErzeugeSuchtafeln create = new ErzeugeSuchtafeln();
			create.ErzeugeTafeln(path, path);
		}

		private void LoadTables()
		{
			Task task = Task.Run(() =>
			{
				MainServiceCtrl.Button1_Enable(false);
				string path = Helper.GetExePath();
				Abfrage.Instance.LoadTables(path);
				MainServiceCtrl.Button1_Enable(true);
			});
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

		private void MainForm_Shown(object sender, EventArgs e)
		{
			Task.Run(() =>
			{
				Button1_CreateTables();
			});
		}
	}
}