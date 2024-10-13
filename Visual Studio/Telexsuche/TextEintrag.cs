using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telexsuche
{
	public class TextEintrag
	{
		public int PageNumber { get; set; }

		public int Index { get; set; }

		public int ColumnNumber { get; set; }

		public string KennungText { get; set; }

		public string KennungTextWithStar => (KgStar ? "*" : "") + KennungText;

		public bool KgStar { get; set; }

		public string NameText { get; set; }

		public string Town { get; set; }

		public int TownRegionMarkerCount { get; set; }

		public double StartX { get; set; }

		public double StartY { get; set; }

		public bool Changed { get; set; }

		public string Number
		{
			get
			{
				if (string.IsNullOrEmpty(KennungText))
				{
					return null;
				}
				string[] list = KennungText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				return list[0];
			}
		}

		// text column positions (first column = 1)
		private const int COL_KENNUNG = 12;
		private const int COL_TEXT = 37;

		public static TextEintrag ParseLine(string line, out bool parseError, out bool stop)
		{
			// page and index number
			parseError = false;
			stop = false;
			if (line.Length < COL_TEXT - 1 || line[0] == '#')
			{
				// no error
				return null;
			}

			if (!int.TryParse(line.Substring(0, 4), out int pageNr))
			{
				parseError = true;
				stop = true;
				return null;
			}
			if (!int.TryParse(line.Substring(4, 1), out int colNr))
			{
				parseError = true;
				stop = true;
				return null;
			}
			if (!int.TryParse(line.Substring(5, 3), out int index))
			{
				parseError = true;
				stop = true;
				return null;
			}

			bool changed = line[8] == '%';

			// kennung

			int textPre = 3;

			bool kgStar = line.Substring(COL_KENNUNG - 2, 1) == "*";

			string kennung = line.Substring(COL_KENNUNG - 1, COL_TEXT - COL_KENNUNG - textPre).Trim();

			// text

			string text = line.Substring(COL_TEXT - 1 - textPre).Trim();

			int pos = text.IndexOf('[');
			if (pos >= 0)
			{
				text = text.Substring(0, pos).Trim();
			}

			pos = line.IndexOf('[');
			string town = null;
			if (pos != -1)
			{
				town = line.Substring(pos + 1, line.Length - pos - 2);
			}

			// '$' zählen und entfernen
			int townRegionMarker = 0;
			while(true)
			{
				pos = text.LastIndexOf("$");
				if (pos>1 && text[pos-2]==',')
				{
					townRegionMarker++;
					text = text.Substring(0, pos) + text.Substring(pos+1);
				}
				else
				{
					break;
				}

			}

			TextEintrag verzeichnisEintrag = new TextEintrag()
			{
				PageNumber = pageNr,
				Index = index,
				ColumnNumber = colNr,
				KennungText = kennung,
				KgStar = kgStar,
				NameText = text,
				Town = town,
				TownRegionMarkerCount = townRegionMarker,
				Changed = changed,
			};
			return verzeichnisEintrag;
		}

		private string IdentString => $"{PageNumber:D04}{ColumnNumber:D01}{Index:D03}";

		public string GetTextString()
		{
			string starStr = KgStar ? "*" : " ";
			string changed = Changed ? "%" : " ";
			string kgErrorStr = " ";

			// '$' wieder einfügen
			string nameText = NameText;
			int pos = nameText.Length - 1;
			for (int i = 0; i < TownRegionMarkerCount; i++)
			{
				pos = nameText.LastIndexOf(',', pos - 1);
				if (pos==-1)
				{
					break;
				}
				nameText = nameText.Substring(0, pos + 2) + "$" + nameText.Substring(pos + 2);
			}


			return $"{IdentString}{changed}{kgErrorStr}{starStr}{KennungText.PadRight(25)}{nameText} [{Town}]";
		}

	}
}
