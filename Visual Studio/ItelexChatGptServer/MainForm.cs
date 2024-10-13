using ItelexChatGptServer;
using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Monitor;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItelexChatGptServer
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
			NotifyIcon1.BalloonTipText = $"{Constants.PROGRAM_NAME} {Constants.DEFAULT_NUMBER_DE}";
			NotifyIcon1.Text = $"{Constants.PROGRAM_NAME} {Constants.DEFAULT_NUMBER_DE}";

			this.Text = Helper.GetVersion();

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
				MonitorServerType = MonitorServerTypes.ItelexChatGptServer,
				MonitorPort = Constants.DEFAULT_MONITOR_PORT,
				ItelexExtensions = new ItelexExtensionConfiguration[]
				{
					new ItelexExtensionConfiguration(Constants.DEFAULT_EXTENSION_DE, Constants.DEFAULT_NUMBER_DE, "de",
						Constants.PROGRAM_SHORTNAME_DE, Constants.DEFAULT_ANSWERBACK_DE, true),
					new ItelexExtensionConfiguration(Constants.DEFAULT_EXTENSION_EN, Constants.DEFAULT_NUMBER_EN, "en",
						Constants.PROGRAM_SHORTNAME_EN, Constants.DEFAULT_ANSWERBACK_EN, false)
				}
			};
			MainServiceCtrl.StartIncomingConnections<ChatGptConnectionManager>(config);

			//ChatGpt cg = new ChatGpt();
			//Task task = cg.Request("Wer war Konrad Zuse?");

			//ChatGptWetstone cg2 = new ChatGptWetstone();
			//cg2.Test();
		}

		private void MainServiceCtrl_LoginLogoff(string msg)
		{
			NotifyIcon1.BalloonTipTitle = Constants.PROGRAM_SHORTNAME_DE;
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
