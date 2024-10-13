using ItelexCommon;
using ItelexCommon.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer
{
	public class PruefTexte
	{
		private const string TAG = nameof(PruefTexte);

		private List<PruefTextItem> _pruefTexte;

		private Logging _logger;

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static PruefTexte instance;

		public static PruefTexte Instance => instance ?? (instance = new PruefTexte());

		private PruefTexte()
		{
			_logger = LogManager.Instance.Logger;
		}

		public void ReadTexte()
		{
			string[] lines = null;
			try
			{
				lines = File.ReadAllLines(Constants.PRUEFTEXTE_PATH);
			}
			catch(Exception ex)
			{
				_logger.Error(TAG, nameof(ReadTexte), "", ex);
				_pruefTexte = null;
				return;
			}

			_pruefTexte = new List<PruefTextItem>();
			PruefTextItem pt = null;
			foreach (string line in lines)
			{
				string key = line.Substring(0, 5).ToLower();
				string prm = line.Substring(5);
				switch(key)
				{
					case "name:":
						if (pt !=null && pt.Text.Length > 0)
						{
							_pruefTexte.Add(pt);
						}
						pt = new PruefTextItem(prm.Trim());
						break;
					case "text:":
						prm = prm.Replace("\\r", "\r");
						prm = prm.Replace("\\n", "\n");
						pt.Text += prm.TrimEnd(new char[] { ' ' });
						break;
				}
			}

			if (pt != null && pt.Text.Length > 0)
			{
				_pruefTexte.Add(pt);
			}
		}

		public List<PruefTextItem> GetPruefTextItems()
		{
			return _pruefTexte;
		}

		public PruefTextItem GetPruefTextItem(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return null;
			name = name.ToLower();
			return (from p in _pruefTexte where p.Name == name select p).FirstOrDefault();
		}
	}

	public class PruefTextItem
	{
		public string Name { get; set; }

		public string Text { get; set; }

		public PruefTextItem(string name)
		{
			Name = name;
			Text = "";
		}

		public override string ToString()
		{
			return $"{Name} {Text}";
		}
	}
}
