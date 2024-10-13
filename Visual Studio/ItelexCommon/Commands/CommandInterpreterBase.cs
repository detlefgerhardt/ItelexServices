using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ItelexCommon.Commands
{
	internal enum TokenCats
	{
		Command,
		Argument
	}

	public enum TokenTypes
	{
		Help,
		List, // channel list
		Category, // channel category
		Subscribed, // channels
		Language, // channel language
		Subscribe, // subscribe channel
		Unsubscribe, // unsubscribe channel
		Preview, // preview channel
		Set, // user settings
		Hours, // receive hours, from/to
		Number, // change main number
		Redirect, // redirect number
		Timezone, // set timezone
		Format, // set msg format
		MaxPendMsgs, // max. pending messages
		Pin, // set pin
		Pause, // pause all news
		Clear, // clear all pending news
		Local, // private/user channel
		Create, // create new user channel
		Delete, // delete user channel / delete user channel owner
		Send, // send to user channel / send mail or fax
		Owner, // user channel owner
		Add, // add user channel owner
		Off, // off
		End, // add user channel owner
		Show, // show sender mail-address in received telex
		Groups, // show groups
		Group, // add/delete group
		New, // new group
		Select, // select a group
		Edit, // edit group
		Numbers, // list numbers
		Remove, // remove number

		ArgCat, // category
		ArgLng, // de/en
		//ArgCont, // td
		ArgMsgFormat, // msg format
		ArgInt,
		ArgStr, // string argument

		// ItelexMailGate
		Max, // max. pending messages
		Pending, // max. pending messages
		Mails, // max mails per day or send mail, allow mail
		Fax, // send fax
		EventCode, // allowed sender
		Allowed, // allowed sender, allow mail/telegram
		Sender, // sender / absender
		Telegram, // allow telegram
		PunchTape, // send lochstreifen
		Lines, // max lines per day

		Pruef, // Prueftexte
	}

	public class CommandInterpreterBase
	{
		/// <summary>
		/// All available command tokens
		/// </summary>
		///
		protected AvailTokenItem[] _availTokens;

		protected AvailCommandItem[] _availCommands;

		public int MinLen { get; protected set; } = 0;

		public CommandInterpreterBase()
		{
		}

		public List<CmdTokenItem> Parse(string cmdLine)
		{
			if (string.IsNullOrWhiteSpace(cmdLine)) return null;

			// insert blank after '+' oder '-'
			if (cmdLine[0] == '+' || cmdLine[0] == '-')
			{
				cmdLine = cmdLine.Insert(1, " ");
			}

			string[] words = cmdLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			List<CmdTokenItem> tokenList = new List<CmdTokenItem>();
			for (int i = 0; i < words.Length; i++)
			{
				AvailCommandItem[] possCmds = GetPossibleCommands(tokenList, words.Length);
				CmdTokenItem token = GetPossibleTokens(possCmds, words[i], i);
				if (token == null)
				{
					return null; // invalid command
				}
				tokenList.Add(token);
			}

			// check length
			foreach (AvailCommandItem item in _availCommands)
			{
				if (item.PossibleTokens[0] != tokenList[0].TokenType) continue;
				if (item.PossibleTokens.Length > tokenList.Count) continue;
				return tokenList;
			}

			return null;
		}

		protected virtual CmdTokenItem GetPossibleTokens(AvailCommandItem[] possCmds, string tokenStr, int index)
		{
			return null;
		}

		private AvailCommandItem[] GetPossibleCommands(List<CmdTokenItem> tokenList, int tokenCnt)
		{
			if (tokenList.Count == 0) return _availCommands;

			List<AvailCommandItem> possCmds = new List<AvailCommandItem>();

			foreach (AvailCommandItem cv in _availCommands)
			{
				bool found = true;
				for (int t = 0; t < tokenList.Count; t++)
				{
					if (tokenCnt > cv.Length || tokenList.Count > cv.Length || tokenList[t].TokenType != cv.PossibleTokens[t])
					{
						found = false;
						break;
					}
				}
				if (found)
				{
					possCmds.Add(cv);
				}
			}
			return possCmds.ToArray();
		}

		protected void CheckTokenPrefixes()
		{
			List<string> prefixes = new List<string>();
			foreach (AvailTokenItem token in _availTokens)
			{
				foreach (string name in token.Names)
				{
					string shortName = name.Length >= MinLen ? name.Substring(0, MinLen) : name;
					if (prefixes.Contains(shortName))
					{
						Debug.WriteLine($"availToken conflict: {shortName}");
					}
					else
					{
						prefixes.Add(shortName);
					}
				}
			}
			//Debug.WriteLine("no token/category naming conflicts");
		}

		public static bool IsOnValue(string value)
		{
			string[] values = new string[] { "on", "yes", "ein", "ja" };
			return values.Contains(value.ToLower());
		}

		public static bool IsOffValue(string value)
		{
			string[] values = new string[] { "off", "no", "aus", "nein" };
			return values.Contains(value.ToLower());
		}
	}

}
