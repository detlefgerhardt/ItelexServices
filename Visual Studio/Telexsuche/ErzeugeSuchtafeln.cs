using ItelexCommon;
using ItelexCommon.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telexsuche
{
	public class ErzeugeSuchtafeln
	{
		private const string TAG = nameof(ErzeugeSuchtafeln);

		private const string FILENAME = "ATxT87_990.txt";

		private List<TextTafelElement> _textTafel { get; set; }

		/*
		 * Die Trennung in Scatter- und Hashtafel macht in C# eigentlich keinen Sinn, dass man die Einträge
		 * der Scattertafel auf direkt in die Hashtafel eintragen könnte.
		 * Die Trennung erfolgt nur als Anlehung an die historische Datenstruktur.
		 */

		private List<ScatterTafelElement> _scatterTafel { get; set; }

		private Dictionary<string, int> _hashTafel { get; set; }

		public ErzeugeSuchtafeln()
		{
		}

		public void ErzeugeTafeln(string srcPfad, string zielPfad)
		{
			Log(LogTypes.Notice, nameof(ErzeugeTafeln), $"srcPfad={srcPfad}, zielPfad={zielPfad}");

			_textTafel = new List<TextTafelElement>();
			_scatterTafel = new List<ScatterTafelElement>();
			_hashTafel = new Dictionary<string, int>();

			string srcName = Path.Combine(srcPfad, FILENAME);
			Log(LogTypes.Debug, nameof(ErzeugeTafeln), $"srcName={srcName}");
			List<TextEintrag> lineMainItems = TextVerzeichnis.LeseTelexverzeichnis(srcName, out int errLine);
			int cnt = 0;
			foreach (TextEintrag eintrag in lineMainItems)
			{

				VerzeichnisEintragHinzufuegen(eintrag);
				cnt++;
				if (cnt % 1000 == 0)
				{
					Debug.WriteLine($"{cnt} {_scatterTafel.Count}");
				}
			}

			Log(LogTypes.Notice, nameof(ErzeugeTafeln), $"Texttafel-Einträge: {_textTafel.Count}");
			Log(LogTypes.Notice, nameof(ErzeugeTafeln), $"Scattertafel-Einträge: {_scatterTafel.Count}");
			Log(LogTypes.Notice, nameof(ErzeugeTafeln), $"Hashtafel-Einträge: {_hashTafel.Count}");

			SchreibeTextTafel(zielPfad);
			SchreibeScatterUndHashTafel(zielPfad);
		}

		private void VerzeichnisEintragHinzufuegen(TextEintrag item)
		{
			if (string.IsNullOrEmpty(item.KennungText) || string.IsNullOrEmpty(item.KennungText) ||
				item.Number == null)
			{
				// invalid item
				return;
			}

			// Eintrag fuer Texttafen aufbereiten, Suchfeldern ermitteln

			string name = item.NameText.Replace("\\-", "-");
			name = name.Replace("\\", " ");
			List<string> suchFelder = name.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			if (!string.IsNullOrEmpty(item.KennungText))
			{
				// Kennungsgeber zu den Suchworten hinzufuegen
				string[] kgList = item.KennungText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i=0; i<kgList.Length; i++)
				{
					if (kgList[i] != "d") suchFelder.Add(kgList[i]);
				}
			}

			for (int i = 0; i < suchFelder.Count; i++)
			{
				suchFelder[i] = TextfeldKonvertieren(suchFelder[i]);
			}

			// Textfelder ohne KG-Felder
			int count = string.IsNullOrEmpty(item.KennungText) ? suchFelder.Count : suchFelder.Count - 2;
			string[] textFelder = new string[count];
			for (int i = 0; i < count; i++)
			{
				textFelder[i] = suchFelder[i].Replace("&", "und");
			}

			// Eintrag in die Texttafel aufnehmen, Adresse merken

			_textTafel.Add(new TextTafelElement()
			{
				Kennung = item.KennungText,
				Nummer = item.Number,
				Zeilen = textFelder
			});
			int textAdresse = _textTafel.Count - 1;

			// Liste aller Suchworte dieses Verzeichniseintrags erstellen

			List<string> suchwortListe = new List<string>();

			foreach (string suchFeld in suchFelder)
			{
				string feld = SuchfeldFiltern(suchFeld);
				List<string> feldWorte = feld.Split(new char[] { ' ' },
						StringSplitOptions.RemoveEmptyEntries).ToList();
				foreach (string feldWort in feldWorte)
				{
					string wort = feldWort.Trim();
					suchwortListe.Add(wort);
					if (wort.Contains('-') || !wort.Contains('/'))
					{
						// bei Worten, die '-' oder '/' enthalten, die einzelnen Teilworte zusätzlich als
						// Suchworte aufnehmen
						wort = wort.Replace('/', '-');
						string[] zusaetzlicheWorte = wort.Split(new char[] { '-' },
								StringSplitOptions.RemoveEmptyEntries);
						suchwortListe.AddRange(zusaetzlicheWorte);
					}
				}
			}

			// Suchworte zur Scatter und Hash-Tafel hinzufuegen

			suchwortListe = RemoveDuplicateWords(suchwortListe);

			foreach (string word in suchwortListe)
			{
				ScatterTafelElement scatterTableItem;
				if (!_hashTafel.ContainsKey(word))
				{
					// new word
					scatterTableItem = new ScatterTafelElement();
					scatterTableItem.TextTafelAdressen = new List<int> { textAdresse };
					_scatterTafel.Add(scatterTableItem);
					_hashTafel[word] = _scatterTafel.Count - 1;
				}
				else
				{
					// add TextTafeladresse
					scatterTableItem = ErmittleScattertafelEintrag(word);
					scatterTableItem.TextTafelAdressen.Add(textAdresse);
					//if (textAdresse == 51690)
					//{
					//	Debug.Write("");
					//}
				}
			}
		}

		private void SchreibeTextTafel(string destPath)
		{
			Log(LogTypes.Notice, nameof(SchreibeTextTafel), $"count={_textTafel.Count}, destPath={destPath}");

			StringBuilder sb = new StringBuilder();
			foreach (TextTafelElement item in _textTafel)
			{
				sb.AppendLine($"{item.Nummer},{item.Kennung},{string.Join(",", item.Zeilen)}");
			}
			string fullName = Path.Combine(destPath, Konstanten.TEXTTAFEL_NAME);
			Log(LogTypes.Notice, nameof(SchreibeTextTafel), $"fullName={fullName}");
			try
			{
				File.WriteAllText(fullName, sb.ToString());
			}
			catch (Exception ex)
			{
				Log(LogTypes.Error, nameof(SchreibeTextTafel), "", ex);
			}
		}

		/// <summary>
		/// Iterate through _hashTable, find korresponding item from _scatterTable and write them together to "scattertable.txt"
		/// Write a wordlist with count to "hashlist.txt"
		/// </summary>
		private void SchreibeScatterUndHashTafel(string zielPfad)
		{
			Log(LogTypes.Notice, nameof(SchreibeTextTafel), $"_hashTafel count={_hashTafel.Count}");
			StringBuilder sb = new StringBuilder();

			List<HashStatistikElement> hashStatistik = new List<HashStatistikElement>();

			foreach (KeyValuePair<string, int> entry in _hashTafel)
			{
				ScatterTafelElement scatterEintrag = _scatterTafel[entry.Value];
				string adressenString = string.Join(",", scatterEintrag.TextTafelAdressen);
				string line = $"{entry.Key},{entry.Value},{adressenString}";
				sb.AppendLine(line);

				HashStatistikElement hashStatEintrag = new HashStatistikElement()
				{
					Wort = entry.Key,
					Anzahl = scatterEintrag.TextTafelAdressen.Count
				};
				hashStatistik.Add(hashStatEintrag);
			}

			string dateiname = Path.Combine(zielPfad, Konstanten.SCATTERTAFEL_NAME);
			try
			{
				File.WriteAllText(dateiname, sb.ToString());
			}
			catch (Exception ex)
			{
				Log(LogTypes.Error, nameof(SchreibeScatterUndHashTafel), $"write {dateiname}", ex);
			}

			hashStatistik.Sort(new HashStatEintragComparer());
			sb = new StringBuilder();
			foreach (HashStatistikElement hashStatEintrag in hashStatistik)
			{
				string line = $"{hashStatEintrag.Wort}, {hashStatEintrag.Anzahl}";
				sb.AppendLine(line);
			}

			dateiname = Path.Combine(zielPfad, Konstanten.HASHSTATISTIK_NAME);
			try
			{
				File.WriteAllText(dateiname, sb.ToString());
			}
			catch (Exception ex)
			{
				Log(LogTypes.Error, nameof(SchreibeScatterUndHashTafel), $"write {dateiname}", ex);
			}
		}

		private ScatterTafelElement ErmittleScattertafelEintrag(string wort)
		{
			int index = _hashTafel[wort];
			return _scatterTafel[index];
		}

		/// <summary>
		/// erlaube Zeichen in der Text-Tafel
		/// </summary>
		private string _erlaubteTextZeichen = "abcdefghijklmnopqrstuvwxyz0123456789.-~'/() ";

		private List<string> RemoveDuplicateWords(List<string> words)
		{
			List<string> newWords = new List<string>();
			foreach (string word in words)
			{
				if (!newWords.Contains(word))
				{
					newWords.Add(word);
				}
			}
			return newWords;
		}

		private string TextfeldKonvertieren(string part)
		{
			string newPart = "";
			part = part.ToLower();
			foreach (char chr in part)
			{
				if (_erlaubteTextZeichen.Contains(chr))
				{
					newPart += chr;
					continue;
				}

				string replStr = null;
				switch (chr)
				{
					case 'ä':
						replStr = "ae";
						break;
					case 'á':
						replStr = "a";
						break;
					case 'é':
						replStr = "e";
						break;
					case 'ö':
						replStr = "oe";
						break;
					case 'ó':
						replStr = "o";
						break;
					case 'ü':
						replStr = "ue";
						break;
					case 'ß':
						replStr = "ss";
						break;
					case '"':
						replStr = "'";
						break;
				}
				if (!string.IsNullOrEmpty(replStr))
				{
					newPart += replStr;
				}
			}

			// mehrfach leerzeichen entfernen
			if (newPart.Contains("  "))
			{
				//Debug.Write("");
			}

			string[] list = newPart.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			list = Tools.ConcatWords(list);
			return string.Join(" ", list);
		}

		/// <summary>
		/// Erlaubte Zeichen in der Hash-Tafel
		/// </summary>
		private string _erlaubteSuchwortZeichen = "abcdefghijklmnopqrstuvwxyz0123456789-/ ";

		private string SuchfeldFiltern(string suchFeld)
		{
			string ergebnis = "";
			foreach (char chr in suchFeld)
			{
				if (_erlaubteSuchwortZeichen.Contains(chr))
				{
					ergebnis += chr;
					continue;
				}

				/*
				string replStr = null;
				switch (chr)
				{
				}
				if (!string.IsNullOrEmpty(replStr))
				{
					ergebnis += replStr;
				}
				*/
			}
			return ergebnis;
		}

		private void Log(LogTypes logType, string method, string msg, Exception ex = null)
		{
			if (ex == null)
			{
				LogManager.Instance.Logger.Log(logType, TAG, method, msg);
			}
			else
			{
				LogManager.Instance.Logger.Error(TAG, method, msg, ex);
			}
		}


	}
}
