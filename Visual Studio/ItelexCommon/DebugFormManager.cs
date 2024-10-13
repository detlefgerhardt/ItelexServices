using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexCommon
{
	public class DebugFormManager
	{
		private const string TAG = nameof(DebugFormManager);

		private DebugForm _debugForm = null;

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static DebugFormManager instance;

		public static DebugFormManager Instance => instance ?? (instance = new DebugFormManager());

		public void OpenForm()
		{
			if (_debugForm==null)
			{
				_debugForm = new DebugForm();
				_debugForm.Show();
			}
			_debugForm.BringToFront();
		}

		public void CloseForm()
		{
			_debugForm.Close();
			_debugForm = null;
		}

		public void Clear()
		{
			if (_debugForm !=null)
			{
				_debugForm.ClearText();
			}
		}

		public void ShowText(string text)
		{
			if (_debugForm != null)
			{
				DateTime dt = DateTime.Now;
				_debugForm.AddText($"{dt:HH:mm:ss} {text}\r\n");
			}
		}
	}
}
