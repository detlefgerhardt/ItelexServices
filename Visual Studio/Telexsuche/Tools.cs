using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telexsuche
{
	public static class Tools
	{
		public static string[] ConcatWords(string[] words)
		{
			List<string> words2 = new List<string>();
			for (int i = 0; i < words.Length; i++)
			{
				if (!words[i].EndsWith("~"))
				{
					words2.Add(words[i]);
				}
				else
				{
					string word1 = words[i].Substring(0, words[i].Length - 1);
					string word2 = words[i + 1];
					// spezielle Trennungsregeln
					if (word1.EndsWith("k") && word2.StartsWith("k"))
					{
						word1 = word1.Substring(0, word1.Length - 1) + "c";
					}
					words2.Add(word1 + word2);
					i++;
				}
			}
			return words2.ToArray();
		}

		public static string GetTafelInfo(string pfad)
		{
			string dateiname = Path.Combine(pfad, Konstanten.TEXTTAFEL_NAME);
			FileInfo fileInfo = new FileInfo(dateiname);
			return $"Tafel-Version {fileInfo.LastWriteTime:dd.MM.yyyy}";
		}
	}
}
