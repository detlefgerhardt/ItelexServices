using ItelexCommon;
using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Monitor;
using ItelexCommon.Utility;
using ItelexNewsServer.Connections;
using ItelexNewsServer.Data;
using ItelexNewsServer.News;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace ItelexNewsServer
{
	public partial class MainForm : Form
	{
		private static readonly string TAG = nameof(MainForm);

		private readonly Logging _logger;

		private readonly NewsManager _newsManager;

		private readonly MailManager _mailManager;

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

			_newsManager = NewsManager.Instance;
			_mailManager = MailManager.Instance;

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
				MonitorServerType = MonitorServerTypes.ItelexNewsServer,
				MonitorPort = Constants.DEFAULT_MONITOR_PORT,
				ItelexExtensions = new ItelexExtensionConfiguration[]
				{
					new ItelexExtensionConfiguration(11, Constants.DEFAULT_NUMBER_DE, "de", Constants.PROGRAM_SHORTNAME,
						Constants.ANSWERBACK_DE, true),
					new ItelexExtensionConfiguration(12, Constants.DEFAULT_NUMBER_EN, "en", Constants.PROGRAM_SHORTNAME,
						Constants.ANSWERBACK_EN, false)
				}
			};

#if DEBUG
			Test();
#endif

			MainServiceCtrl.StartIncomingConnections<IncomingConnectionManager>(config);
			MainServiceCtrl.StartOutgoingConnections<CallManager>();
		}

#if DEBUG
		private void Test()
		{
			//DateTime dt = DateTime.Now;
			//dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

			//InitChannels initChannels = new InitChannels();
			//initChannels.AddChannels();
			//Tests.InsertNumber();

			//NumberItem userItem = Database.Instance.NumberLoadByTelexNumber(211231);
			//SessionData sessionData = new SessionData(userItem);
			//_sendPinManager.AddPinNumber(userItem);

			//var rssFeed = RssManager.Instance.GetNewMessagesFromFeed("http://hr.de/presse/index.rss", null);
			//Tests.InsertNumber();
			//Tests.HttpResponse();
			//Tests.TestInterpreter();
			//Tests.Format();
			//Tests.Timezones();
			//Tests.TestRss();

			//string fullName = @"d:\daten\itelex\text.exd";
			//string workingDir = Path.GetDirectoryName(fullName);

			//string msg1 = "abc<123>def";
			//string msg2 = CallManager.ConvMsgTextPostillon(msg1);

			//string msg1 = "a&#39;b";
			//string msg2 = CallManager.ReplaceAsciiCode(msg1);

			//int cnt = NewsDatabase.Instance.MsgStatusCleanup(24, 3 * 24);

			//Test2();
			//CheckLevenshtein();

			/*
			DateTime utcNow = DateTime.UtcNow;
			ChannelItem chItem = new ChannelItem()
			{
				Name = "test2",
				LocalId = 1,
				LocalPin = "1234",
				LocalOwner = 211231,
				LocalPublic = false,
				CreateTimeUtc = utcNow,
				LastChangedTimeUtc = utcNow,
				Active = true,
				Type = "lo",
				Category = "lo",
				Language = null,
				Url = null,
			};
			NewsDatabase.Instance.ChannelInsert(chItem);
			*/

			//TimeZone tz = new TimeZone();
			bool isDst = TimeZoneInfo.Local.IsDaylightSavingTime(new DateTime(2023,11,1,0,0,0));

			List<TimeZoneInfo> infos = TimeZoneInfo.GetSystemTimeZones().ToList();

			TimeZone localZone = TimeZone.CurrentTimeZone;
			DaylightTime daylight = localZone.GetDaylightChanges(2023);

		}

		private void Test2()
		{
			int micros = 0;
			int lastMicros = micros;
			for (int i=0; i<300; i++)
			{
				int diff = microsDiff(lastMicros, micros);
				Debug.WriteLine($"{lastMicros} {micros} {diff}");
				if (diff >= 100)
				{
					lastMicros = micros;
				}
				micros++;
			}
		}

		private int microsDiff(int start, int end)
		{
			return end - start;
		}

		private void CheckLevenshtein()
		{
			List<NewsItem> news = NewsDatabase.Instance.NewsLoadAll();
			foreach (NewsItem n in news)
			{
				int dist = Levenshtein.Calculate(n.Title, n.Message);
				//if (n.NewsId == 149917)
				if (dist < 5)
				{
						Debug.WriteLine($"{dist} {n.NewsId}");
				}
			}
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
