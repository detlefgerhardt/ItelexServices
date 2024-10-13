using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ItelexCommon.Utility
{
	public class FormsHelper
	{
		//public static string GetVersionCode()
		//{
		//	return Application.ProductVersion;
		//	//return $"{version[0]}{version[1]}{version[2]}{version[3]}";
		//}

		public static string GetVersionString()
		{
			return Application.ProductVersion;
		}

		public static string GetItelexVersion(string appStr)
		{
			string versionCode = GetVersionString();
			string verStr = "";
			foreach (char ch in versionCode)
			{
				if (char.IsDigit(ch)) verStr += ch;
			}
			return appStr + verStr;
		}

		public static string GetExePath()
		{
			return Application.StartupPath;
		}

		/// <summary>
		/// Helper method to determin if invoke required, if so will rerun method on correct thread.
		/// if not do nothing.
		/// </summary>
		/// <param name="c">Control that might require invoking</param>
		/// <param name="a">action to preform on control thread if so.</param>
		/// <returns>true if invoke required</returns>
		public static void ControlInvokeRequired(Control c, Action a)
		{
			if (c.InvokeRequired)
			{
				c.Invoke(new MethodInvoker(delegate { a(); }));
			}
			else
			{
				a();
			}
		}

		public static Point CenterForm(Form form, Rectangle parentPos)
		{
			int screenNr = GetScreenNr(parentPos);
			Rectangle sc = Screen.AllScreens[screenNr].WorkingArea;

			int x = sc.Left + (sc.Width - form.Width) / 2;
			int y = sc.Top + (sc.Height - form.Height) / 2;

			return new Point(x, y);
		}

		// Error if parentPos = Fullscreen
		public static int GetScreenNr(Rectangle parentPos)
		{
			int mx = parentPos.Left + (parentPos.Right - parentPos.Left) / 2;

			Screen[] screens = Screen.AllScreens;
			int screenNr = 1;
			for (int i = 0; i < screens.Length; i++)
			{
				Rectangle scrnBounds = screens[i].WorkingArea;
				if (mx >= scrnBounds.Left && mx <= scrnBounds.Left + scrnBounds.Width)
					screenNr = i;
			}
			return screenNr;
		}

		public static Font ButtonFont(bool bold)
		{
			FontStyle fontStyle = bold ? FontStyle.Bold : FontStyle.Regular;
			Font font = new Font(new FontFamily("Arial"), 8.25F, fontStyle);
			return font;
		}

		public static void PaintRuler(Graphics g, int screenWidth, float scale)
		{
			for (int i = 0; i < screenWidth + 3; i++)
			{
				float x = (float)(2 + i * scale);
				Pen pen = (i == screenWidth) ? new Pen(Color.Red, 2) : new Pen(Color.Black, 1);
				pen.StartCap = LineCap.Square;
				pen.EndCap = LineCap.Square;
				if (i % 10 == 0)
				{
					g.DrawLine(pen, x, 0, x, 10);
				}
				else
				{
					g.DrawLine(pen, x, 5, x, 10);
				}
			}
		}
	}
}
