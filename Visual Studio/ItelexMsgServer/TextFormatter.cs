using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexMsgServer
{
	class TextFormatter
	{
		// delimiters in RichRextBox
		private readonly List<DelimiterItem> _delimiters = new List<DelimiterItem>()
		{
			new DelimiterItem(" ", true),
			new DelimiterItem("-", true),
			new DelimiterItem("+", true),
			new DelimiterItem("(", true),
			new DelimiterItem(".", false),
			new DelimiterItem(",", false),
			new DelimiterItem(":", false),
			new DelimiterItem("?", false),
			new DelimiterItem(")", false),
			new DelimiterItem("=", false),
			new DelimiterItem("/", false),
			new DelimiterItem(">>", true),
			new DelimiterItem("<<", false),
		};

		public string FormatTelexText(string text, int width)
		{
			text = ReplaceHyphen(text);
			List<string> lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
			lines = FormatTelexLines(lines, width);
			text = string.Join("\r\n", lines);
			text = text.Replace(">>", "''");
			text = text.Replace("<<", "''");
			return text;
		}

		/// <summary>
		/// replaces all opening (odd) " or '' with >> and all closing (even) " or '' with <<
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public string ReplaceHyphen(string text)
		{
			text = text.Replace("''", "\"");
			if (!text.Contains("\"")) return text;

			string[] parts = text.Split('"');
			string newText = "";
			bool first = true;
			for (int i=0; i<parts.Length; i++)
			{
				if (i > 0)
				{
					newText += (first) ? ">>" : "<<";
					first = !first;
				}
				newText += parts[i];
			}
			return newText;
		}

		public List<string> FormatTelexLines(List<string> lines, int width)
		{
			List<string> newLines = new List<string>();
			foreach (string line in lines)
			{
				List<string> wrappedLines = WrapLine(line, width);
				newLines.AddRange(wrappedLines);
			}
			return newLines;
		}

		/// <summary>
		/// Warps one long line to sevaral short lines (<=len), additional spaces are preserved
		/// </summary>
		/// <param name="line"></param>
		/// <param name="len"></param>
		/// <returns></returns>
		private List<string> WrapLine(string line, int len)
		{
			if (string.IsNullOrEmpty(line) || line.Length <= len)
			{
				return new List<string>() { line };
			}

			List<string> newLines = new List<string>();
			while (line.Length >= len)
			{
				int pos = -1;
				for (int i = len - 1; i > 0; i--)
				{
					//Debug.WriteLine(line[i]);
					DelimiterItem delim = _delimiters.Find(d => d.Check(line, i));
					if (delim != null)
					{
						pos = delim.WrapBefore ? i : i + delim.DelimLen;
						//Debug.WriteLine($"delim {line[i]}, pos={pos}");
						break;
					}
				}
				if (pos == -1)
				{
					pos = len - 1;
				}
				//Debug.WriteLine($"newline '{line}'");
				//Debug.WriteLine($"pos={pos}, sub='{line.Substring(0, pos)}'");
				newLines.Add(line.Substring(0, pos));
				line = line.Substring(pos).Trim();
				//Debug.WriteLine($"line='{line}'");
			}
			if (line.Length > 0)
			{
				newLines.Add(line);
			}

			return newLines;
		}
	}

	class DelimiterItem
	{
		public string Delim { get; set; }

		public int DelimLen { get; set; }

		public bool WrapBefore { get; set; }

		public DelimiterItem(string delim, bool wrapBefore)
		{
			Delim = delim;
			DelimLen = delim.Length;
			WrapBefore = wrapBefore;
		}

		public bool Check(string line, int pos)
		{
			if (pos >= line.Length - DelimLen) return false;
			return line.Substring(pos, DelimLen) == Delim;
		}
	}

}
