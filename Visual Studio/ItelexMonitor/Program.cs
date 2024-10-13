﻿using ItelexCommon;
using ItelexCommon.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItelexMonitor
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

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
