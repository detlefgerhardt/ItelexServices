using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItelexCommon
{
	public partial class DebugForm : Form
	{
		public enum Modes { Default, Input, Output, Command };

		public DebugForm()
		{
			InitializeComponent();
		}

		public void ClearText()
		{
			FormsHelper.ControlInvokeRequired(TextRtb, () => { TextRtb.Text = ""; });
		}

		public void AddText(string text, Modes mode = Modes.Default)
		{
			Color textColor = GetModeColor(mode);
			AddText(text, textColor);
		}

		public void AddText(string text, Color textColor)
		{
			FormsHelper.ControlInvokeRequired(TextRtb, () =>
			{
				int pos = TextRtb.TextLength;
				TextRtb.AppendText(text);
				TextRtb.Select(pos, text.Length);
				TextRtb.SelectionColor = textColor;
				TextRtb.Select();
			});
		}

		private Color GetModeColor(Modes mode)
		{
			switch (mode)
			{
				case Modes.Output:
				default:
					return Color.Red;
				case Modes.Input:
				case Modes.Default:
					return Color.Black;
				case Modes.Command:
					return Color.Blue;
			}
		}
	}
}
