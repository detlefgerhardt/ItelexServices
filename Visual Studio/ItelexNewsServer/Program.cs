using ItelexCommon;
using ItelexCommon.Logger;
using ItelexNewsServer.Languages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItelexNewsServer
{
	static class Program
	{
		private const string TAG = nameof(Program);

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(
				System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
			{
				// other instance already running
				return;
			}

			LogManager.Instance.SetLogger(Constants.LOG_PATH, Constants.DEBUG_LOG, Constants.LOG_LEVEL, 
					Constants.SYSLOG_HOST, Constants.SYSLOG_PORT, Constants.SYSLOG_APPNAME);
			LogManager.Instance.Logger.Info(TAG, nameof(Main), $"----- Start {Helper.GetVersion()} -----");
			LogManager.Instance.InitAutoCleanup(new LoggerCleanupItem[] {
				new LoggerCleanupItem("connection_?????_In.log"),
				new LoggerCleanupItem("connection_?????_Out*.log", null)
			});


			LanguageManager.Instance.Init(LanguageDefinition.DEFAULT_LANGUAGE);
			LanguageManager.Instance.AddLanguages(LanguageDeutsch.GetLng());
			LanguageManager.Instance.AddLanguages(LanguageEnglish.GetLng());

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
