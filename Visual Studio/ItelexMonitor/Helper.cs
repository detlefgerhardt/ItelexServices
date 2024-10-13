using ItelexCommon.Utility;
using System;
using System.IO;
using System.Windows.Forms;

namespace ItelexMonitor
{
	static class Helper
	{
		public static string GetVersion(bool verbose=true)
		{
#if DEBUG
			return CommonHelper.FormatVersionDebug(Constants.PROGRAM_NAME, Application.ProductVersion, Properties.Resources.BuildDate);
			string buildTime;
			if (verbose)
			{
				// show date and time in debug version
				buildTime = Properties.Resources.BuildDate.Trim(new char[] { '\n', '\r' }) + " Debug";
			}
			else
			{
				buildTime = Properties.Resources.BuildDate.Trim(new char[] { '\n', '\r' });
				buildTime = buildTime.Substring(0, 10);
			}
#else
			return CommonHelper.FormatVersionRelease(Constants.PROGRAM_NAME, Application.ProductVersion, Properties.Resources.BuildDate);
			// show only date in release version
			string buildTime = Properties.Resources.BuildDate.Trim(new char[] { '\n', '\r' });
			//buildTime = buildTime.Substring(0, 10);
#endif
			//return $"{Constants.PROGRAM_NAME}  V{Application.ProductVersion}  (Build={buildTime})";
		}

		public static string GetVersionCode()
		{
			return Application.ProductVersion;
			//return $"{version[0]}{version[1]}{version[2]}{version[3]}";
		}

		public static string GetExePath()
		{
			return Application.StartupPath;
		}

		public static DateTime? BuildTime()
		{
			//string dateStr = ConfigurationManager.AppSettings.Get("builddate");
			string dateStr = Properties.Resources.BuildDate.Trim();
			DateTime dt;
			if (!DateTime.TryParse(dateStr, out dt))
			{
				// invalid build time
				return null;
			}
			return dt;
		}

		private static object _sessionLock = new object();

		private const string SESSION_NAME = "session.dat";

		public static int GetNewSessionNo(int lastSessionNo)
		{
			lock (_sessionLock)
			{
				try
				{
					string fullName = Path.Combine(GetExePath(), SESSION_NAME);
					int sessionNo = lastSessionNo;
					if (File.Exists(fullName))
					{
						string[] lines = File.ReadAllLines(fullName);
						if (lines.Length > 0)
						{
							if (int.TryParse(lines[0], out int result))
							{
								sessionNo = result;
							}
						}
					}
					sessionNo++;
					File.WriteAllText(fullName, $"{sessionNo}\r\n");
					return sessionNo;
				}
				catch (Exception)
				{
					return lastSessionNo + 1;
				}
			}
		}

	}
}
