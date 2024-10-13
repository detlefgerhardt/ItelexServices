using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telexsuche
{
	public class TextVerzeichnis
	{
		private const string TAG = nameof(TextVerzeichnis);

		public static List<TextEintrag> LeseTelexverzeichnis(string verzeichnisName, out int errLine)
		{
			List<TextEintrag> verzeichnisListe = new List<TextEintrag>();
			errLine = 0;

			string[] verzeichnisZeilen = File.ReadAllLines(verzeichnisName);
			bool error = false;
			for (int i = 0; i < verzeichnisZeilen.Length; i++)
			{
				TextEintrag item = TextEintrag.ParseLine(verzeichnisZeilen[i], out bool err, out bool stop);
				if (err)
				{
					errLine = i + 1;
					error = true;
				}
				if (stop)
				{
					break;
				}

				if (item != null)
				{
					verzeichnisListe.Add(item);
				}
			}

			if (!error)
			{
				errLine = 0;
			}
			else
			{
				throw new Exception($"parse error in korr line {errLine}");
			}

			return verzeichnisListe;
		}

		public static void SchreibeTelexverzeichnis(string fullName, List<TextEintrag> eintraege)
		{
			StringBuilder sb = new StringBuilder();
			int lastPage = 0;
			foreach (TextEintrag eintrag in eintraege)
			{
				if (eintrag.PageNumber != lastPage)
				{
					sb.AppendLine($"###### page {eintrag.PageNumber} ######");
					lastPage = eintrag.PageNumber;
				}
				sb.AppendLine(eintrag.GetTextString());
			}
			File.WriteAllText(fullName, sb.ToString());
			return;
		}

		public static void ReadTextData(string fileName, List<TextEintrag> textEintraege)
		{
			string[] lines = File.ReadAllLines(fileName);

			Dictionary<int, TextEintrag> dict = new Dictionary<int, TextEintrag>();
			foreach (TextEintrag item in textEintraege)
			{
				dict[item.PageNumber * 200 + item.Index] = item;
			}

			for (int i = 0; i < lines.Length; i++)
			{
				if (i % 1000 == 0)
				{
					Debug.WriteLine(i);
				}
				DataEintrag data = ParseDataLine(lines[i]);
				if (data==null)
				{
					throw new Exception($"parse error in data line {i + 1}");
				}
				int idx = data.PageNumber * 200 + data.Index;
				if (dict.ContainsKey(idx))
				{
					TextEintrag mainItem = dict[idx];
					mainItem.StartX = data.StartX;
					mainItem.StartY = data.StartY;
				}

			}
		}

		private static DataEintrag ParseDataLine(string line)
		{
			if (string.IsNullOrEmpty(line))
			{
				return null;
			}
			string[] list = line.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (list.Length != 6)
			{
				return null;
			}

			//return $"{PageNumber};{Index};{ColumnNumber};{StartX:F3};{StartX:F3};{GetItemErrorStr()}";

			if (!int.TryParse(list[0], out int pageNr))
			{
				return null;
			}
			if (!int.TryParse(list[1], out int index))
			{
				return null;
			}
			if (!int.TryParse(list[2], out int colNr))
			{
				return null;
			}
			if (!double.TryParse(list[3], out double startX))
			{
				return null;
			}
			if (!double.TryParse(list[4], out double startY))
			{
				return null;
			}

			DataEintrag data = new DataEintrag()
			{
				PageNumber = pageNr,
				Index = index,
				ColumnNumber = colNr,
				StartX = startX,
				StartY = startY
			};
			return data;
		}


	}
}
