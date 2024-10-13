using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatGptServer
{
	/// <summary>
	/// Spezieller TextFormatter fuer die Wettermeldungen mit Absatzformatierung
	/// </summary>

	public class TextFormatter
	{
		private const int LINE_LEN = 69;

		public string FormatText(string text, int newLineLen)
		{
			if (string.IsNullOrWhiteSpace(text)) return "";
			text = text.Replace("\r", "");
			string[] lines = text.Split(new char[] { '\n' }, StringSplitOptions.None);
			if (lines.Length == 0) return "";

			// split text in list of lines, line is a list of words
			List<TextLine> textLines = new List<TextLine>();
			foreach (string line in lines)
			{
				if (line == "\u0001" || line == "\u0003") continue;

				List<string> wl = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
				textLines.Add(new TextLine(wl));
			}

			// mark fillable lines
			for (int i = 0; i < textLines.Count - 1; i++)
			{
				//if (textLines[i].Length + textLines[i+1].FirstWordLen > LINE_LEN / 2)
				if (textLines[i].Length > LINE_LEN / 2 && textLines[i + 1].Length > 0)
				{
					textLines[i + 1].Conc = true;
				}
			}

			// concatenate lines
			List<TextLine> concLines = new List<TextLine>();
			TextLine currentLine = new TextLine();
			for (int i = 0; i < textLines.Count; i++)
			{
				if (!textLines[i].Conc)
				{
					concLines.Add(currentLine);
					currentLine = new TextLine();
				}
				currentLine.AddRange(textLines[i]);
			}
			if (currentLine.Length > 0) concLines.Add(currentLine);
			concLines.RemoveAt(0);

			// wrap lines
			List<TextLine> wrappedLines = new List<TextLine>();
			for (int i = 0; i < concLines.Count; i++)
			{
				TextLine line = new TextLine();
				if (concLines[i].Length == 0)
				{
					wrappedLines.Add(line);
					continue;
				}

				foreach (string word in concLines[i])
				{
					if (line.Length + word.Length + 1 > newLineLen)
					{
						wrappedLines.Add(line);
						line = new TextLine();
					}
					line.Add(word);
				}
				if (line.Length > 0) wrappedLines.Add(line);
			}

			string wrappedText = "";
			foreach (TextLine line in wrappedLines)
			{
				wrappedText += line.RawLine + "\r\n";
			}

			return wrappedText;
		}

		/// <summary>
		/// Split line into list of words, preserve blank
		/// </summary>
		/// <param name="line"></param>
		/// <returns>list of words with trailing spaces</returns>
		private List<string> SplitLine(string line)
		{
			List<string> wl = new List<string>();
			while (string.IsNullOrWhiteSpace(line))
			{
				int p1 = -1;
				for (int i = line.Length - 1; i >= 0; i--)
				{
					if (line[i] != ' ')
					{
						p1 = i;
						break;
					}
				}
				if (p1 == -1)
				{
					wl.Add(line);
					break;
				}

				int p2 = -1;
				for (int i = p2 - 1; i >= 0; i--)
				{
					if (line[i] == ' ')
					{
						p2 = i;
						break;
					}
				}
				if (p2 == -1)
				{
					wl.Add(line);
					break;
				}

				string w = line.Substring(p1 + 1);
				line = line.Substring(0, p2);
				wl.Add(w);
			}
			return wl;
		}
	}

	class TextLine : List<string>
	{
		public string RawLine
		{
			get
			{
				return string.Join(" ", this);
			}
		}

		public int Length
		{
			get
			{
				if (Count == 0) return 0;
				return RawLine.Length;
			}
		}

		/// <summary>
		/// Wrap this line to previous line
		/// </summary>
		public bool Conc { get; set; }

		public int FirstWordLen
		{
			get
			{
				return Count > 0 ? this[0].Length : 0;
			}
		}

		public TextLine()
		{
		}

		public TextLine(List<string> words)
		{
			this.AddRange(words);
		}

		public override string ToString()
		{
			return $"{Length} {Conc} {RawLine}";
		}
	}

}
