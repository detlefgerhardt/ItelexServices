﻿using ItelexRundsender.Languages;
using ItelexCommon;
using System;
using System.Windows.Forms;
using ItelexCommon.Logger;
using System.Linq;

namespace ItelexRundsender
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

			LogManager.Instance.SetLogger(Constants.LOG_PATH, Constants.DEBUG_LOG, Constants.LOG_LEVEL);
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
