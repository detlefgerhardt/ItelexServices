using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItelexCommon.ChatGpt
{
	/// <summary>
	/// Netizine OpenAI Library
	/// https://github.com/Netizine/OpenAI
	/// </summary>
	public abstract class ChatGptAbstract
	{
		private const string TAG = nameof(ChatGptAbstract);

		protected const string KEY = PrivateConstants.CHATGPT_KEY;
		protected const string ORG = PrivateConstants.CHATGPT_ORG;

		public ChatGptAbstract()
		{
		}

		public virtual void Test()
		{
		}

		public abstract string GetModel();

		public virtual async Task<string> Request(string reqStr, float? temperature = null, float? top_p = null)
		{
			await Task.Delay(0); // suppress not async warning
			return null;
		}

		public string FormatMsg(string msg)
		{
			string[] words = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			StringBuilder sb = new StringBuilder();
			string line = "";
			foreach (string word in words)
			{
				if ((line + " " + word).Length >= 68)
				{
					sb.Append(line.Trim() + "\r\n");
					line = "";
				}
				line += word + " ";
			}
			if (!string.IsNullOrWhiteSpace(line))
			{
				sb.Append(line.Trim());
			}
			string newMsg = sb.ToString();
			if (newMsg.EndsWith("\r\n")) newMsg = newMsg.Substring(0, newMsg.Length - 2);
			return newMsg;
		}

		public string ConvMsgText(string msg)
		{
			msg = msg.ToLower();

			msg = msg.Replace("\r", " ");
			msg = msg.Replace("\n", " ");
			msg = msg.Replace("#", "//");
			msg = msg.Replace("&", "+");
			msg = msg.Replace("%", "o/o");
			msg = msg.Replace("\"", "'");
			msg = msg.Replace("''", "'");
			msg = msg.Replace("„", "'");
			msg = msg.Replace("“", "'");
			msg = msg.Replace("`", "'");
			msg = msg.Replace("´", "'");
			msg = msg.Replace("\u2013", "-");
			msg = msg.Replace("~", "-");
			msg = msg.Replace("!", ".");
			//msg = msg.Replace("?", ".");
			msg = msg.Replace("*", "x");
			msg = msg.Replace("•", " - ");

			msg = msg.Replace("€", " euro ");
			msg = msg.Replace("$", " dollar ");
			msg = msg.Replace("£", " gbp ");

			msg = msg.Replace("  ", " ");
			msg = msg.Trim();
			return CodeManager.AsciiStringReplacements(msg, CodeSets.ITA2, false, false);
		}
	}
}
