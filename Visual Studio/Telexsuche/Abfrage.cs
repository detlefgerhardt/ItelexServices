using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telexsuche
{
	public class Abfrage
	{
		private const int MAX_RESULTS = 10;
		
		private List<TextTafelElement> _textTable { get; set; }

		private List<ScatterTafelElement> _scatterTafel { get; set; }

		private Dictionary<string, int> _hashTafel { get; set; }

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static Abfrage instance;

		public static Abfrage Instance => instance ?? (instance = new Abfrage());

		public bool TablesLoaded { get; set; }

		private Abfrage()
		{
			TablesLoaded = false;
		}

		public void LoadTables(string pfad)
		{
			LeseTexttafel(pfad);
			LeseScattertafel(pfad);
			TablesLoaded = true;
		}

		private bool LeseTexttafel(string pfad)
		{
			string fullName = Path.Combine(pfad, Konstanten.TEXTTAFEL_NAME);
			string[] lines = File.ReadAllLines(fullName);

			_textTable = new List<TextTafelElement>();
			foreach (string line in lines)
			{
				string[] fields = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (fields.Length < 3)
				{
					continue;
				}
				TextTafelElement item = new TextTafelElement()
				{
					Nummer = fields[0],
					Kennung = fields[1],
					Zeilen = fields.Skip(2).ToArray()
				};
				_textTable.Add(item);
			}
			return true;
		}

		private void LeseScattertafel(string pfad)
		{
			_scatterTafel = new List<ScatterTafelElement>();
			_hashTafel = new Dictionary<string, int>();

			string dateiname = Path.Combine(pfad, Konstanten.SCATTERTAFEL_NAME);
			string[] zeilen = File.ReadAllLines(dateiname);

			for (int i = 0; i < zeilen.Length; i++)
			{
				string[] fields = zeilen[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				_hashTafel[fields[0]] = i;
				ScatterTafelElement _scatterItem = new ScatterTafelElement()
				{
					TextTafelAdressen = new List<int>()
				};

				//if (fields[0]=="hallo")
				//{
				//	Debug.Write("");
				//}

				for (int a = 2; a < fields.Length; a++)
				{
					if (int.TryParse(fields[a], out int value))
					{
						_scatterItem.TextTafelAdressen.Add(value);
					}
				}
				_scatterTafel.Add(_scatterItem);
			}
		}

		public List<TextTafelElement> Suche(string[] suchWorte, out int count, out bool badEsbToAsb)
		{
			for(int i=0; i < suchWorte.Length; i++)
			{
				suchWorte[i] = suchWorte[i].ToLower();
			}

			int[] minAsb = new int[]
			{
				1, // 1 ESB -> 1..1 ASB
				2, // 2 ESB -> 2..2 ASB
				2, // 3 ESB -> 2..3 ASB
				3, // 4 ...
				4, // 5
				4, // 6
				5, // 7
				5, // 8
				5, // 9
				6, // 10 ESB -> 6..10 ASB
			};

			count = 0;
			badEsbToAsb = false;
			List<TextTafelElement> textItems = new List<TextTafelElement>();

			if (suchWorte.Length == 0)
			{
				return textItems;
			}

			List<string> suchWortListe = suchWorte.ToList();

			// max. 10 search words
			if (suchWortListe.Count > 10)
			{
				suchWortListe = suchWortListe.Take(10).ToList();
			}

			if (suchWortListe[0] == "d" || suchWortListe[0] == "bundesrepublik deutschland" || suchWortListe[0]=="brd" || suchWortListe[0]=="deutschland")
			{
				suchWortListe.RemoveAt(0);
			};
			if (suchWortListe.Count == 0)
			{
				return textItems;
			}

			// pruefen ob erstes Suchwort eine gültige Zahl ist, dann Suche nach Nummer

			if (int.TryParse(suchWortListe[0], out int number))
			{
				TextTafelElement item = (from i in _textTable where i.Nummer == suchWortListe[0] select i).FirstOrDefault();
				if (item != null)
				{
					textItems.Add(item);
				}
				return textItems;
			}

			// todo: weniger relevante Worte in eigener Liste und an Abfrage2 übergeben

			List<int> arbeitsTafel = null;
			int esb = suchWortListe.Count;
			int asb = 0;
			bool show = false;
			int permCnt = 0;
			switch (suchWortListe.Count)
			{
				case 1:
					permCnt = 1;
					break;
				case 2:
					permCnt = 2;
					break;
				default:
					permCnt = 3;
					break;
			}
			for (int i = 0; i < permCnt; i++)
			{
				List<string> suchPerm = Permutation(suchWortListe, i);
				arbeitsTafel = AbfrageProVertauschung(suchPerm, out asb);
				//Debug.WriteLine($"esb={esb} asb={asb}");
				if (asb >= minAsb[esb - 1])
				{
					show = true;
					break;
				}
			}

			if (!show)
			{
				return textItems;
			}

			badEsbToAsb = asb < esb;
			count = arbeitsTafel.Count;

			if (count <= MAX_RESULTS)
			{   // gefundene Eintraege anzeigen, wenn es nicht mehr als 5 sind
				foreach (int index in arbeitsTafel)
				{
					textItems.Add(_textTable[index]);
				}
			}

			return textItems;
		}

		/// <summary>
		/// Die ersten 3 Suchworte entsprechend dem Vertauschungsindex idx vertauschen
		/// Bei 2 Suchworten, werden nur die ersten 2 vertauscht
		/// Bei 1 Suchwort keine Vertauschung
		/// </summary>
		/// <param name="suchWorte"></param>
		/// <param name="idx"></param>
		/// <returns></returns>
		private List<string> Permutation(List<string> suchWorte, int idx)
		{
			int[,] exchange2 = new int[,]
			{
				{ 0,1 },
				{ 1,0 },
			};

			int[,] exchange3 = new int[,]
			{
				{ 0,1,2 },
				{ 2,0,1 },
				{ 1,2,0 },
				{ 1,0,2 },
				{ 0,2,1 },
				{ 2,1,0 },
			};

			if (suchWorte.Count<2)
			{
				return suchWorte;
			}

			string[] suchPerm = new string[suchWorte.Count];

			for (int i = 0; i < suchWorte.Count; i++)
			{
				if (suchWorte.Count <= 2 && i < 2)
				{
					// 2 Suchworte vertauschen
					suchPerm[i] = suchWorte[exchange2[idx, i]];
				}
				else if (i < 3)
				{
					// 3 Suchworte vertauschen
					suchPerm[i] = suchWorte[exchange3[idx, i]];
				}
				else
				{
					// die uebrigen Suchworte unvertauscht anhaengen
					suchPerm[i] = suchWorte[i];
				}
			}
			return suchPerm.ToList();
		}

		private List<int> AbfrageProVertauschung(List<string> suchWorte, out int asb)
		{
			List<int> tempArbeitstafel = new List<int>();
			int ubersprungenenSuchworte = 0;
			foreach (string suchWort in suchWorte)
			{
				if (!_hashTafel.ContainsKey(suchWort))
				{
					ubersprungenenSuchworte++;
					continue;
				}

				int scatterIndex = _hashTafel[suchWort];
				ScatterTafelElement scatterElement = _scatterTafel[scatterIndex];
				List<int> result;
				if (tempArbeitstafel.Count > 0)
				{
					result = Schnittmenge(tempArbeitstafel, scatterElement.TextTafelAdressen);
				}
				else
				{
					result = scatterElement.TextTafelAdressen;
				}
				//Debug.WriteLine($"{suchWort} {result.Count}");
				if (result.Count > 0)
				{
					tempArbeitstafel = result;
				}
				else
				{
					ubersprungenenSuchworte++;
				}
			}

			asb = suchWorte.Count - ubersprungenenSuchworte;
			return tempArbeitstafel;
		}

		private List<int> Schnittmenge(List<int> list1, List<int> list2)
		{
			List<int> result = new List<int>();
			foreach (int i1 in list1)
			{
				if (list2.Contains(i1))
				{
					result.Add(i1);
				}
			}
			return result;
		}

		/*
		public TextTafelElement NummernSuche(int nummer)
		{
			return (from e in _textTable where e.Nummer == nummer.ToString() select e).FirstOrDefault();
		}
		*/
	}
}
