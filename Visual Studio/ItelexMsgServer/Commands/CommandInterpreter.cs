using ItelexCommon.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ItelexMsgServer.Commands
{
	class CommandInterpreter: CommandInterpreterBase
	{
		public CommandInterpreter()
		{
			MinLen = 3;

			/// <summary>
			/// All available command tokens
			/// </summary>
			_availTokens = new AvailTokenItem[]
			{
				new AvailTokenItem(TokenTypes.Help, new string[] { "help", "hilfe" }),
				new AvailTokenItem(TokenTypes.Set, new string[] { "settings", "einstellungen" }),
				new AvailTokenItem(TokenTypes.Pin, new string[] { "pin" }),
				new AvailTokenItem(TokenTypes.Timezone, new string[] { "timezone", "zeitzone" }),
				new AvailTokenItem(TokenTypes.Pause, new string[] { "pause" }),
				new AvailTokenItem(TokenTypes.Hours, new string[] { "hours", "stunden" }),
				new AvailTokenItem(TokenTypes.Off, new string[] { "off", "aus" }),
				new AvailTokenItem(TokenTypes.Clear, new string[] { "clear" }),
				new AvailTokenItem(TokenTypes.Max, new string[] { "max" }),
				new AvailTokenItem(TokenTypes.Mails, new string[] { "mails", "emails" }),
				new AvailTokenItem(TokenTypes.Lines, new string[] { "lines", "zeilen" }),
				new AvailTokenItem(TokenTypes.Pending, new string[] { "pending", "wartende" }),
				new AvailTokenItem(TokenTypes.Allowed, new string[] { "allowed", "erlaubte" }),
				new AvailTokenItem(TokenTypes.Telegram, new string[] { "telegram" }),
				new AvailTokenItem(TokenTypes.Show, new string[] { "zeige", "show" }),
				new AvailTokenItem(TokenTypes.Send, new string[] { "sende", "sender", "send", "absender" }), // send / sende / sender
				new AvailTokenItem(TokenTypes.PunchTape, new string[] { "ls", "punchtape" }),
				new AvailTokenItem(TokenTypes.Fax, new string[] { "fax", "fax" }),
				new AvailTokenItem(TokenTypes.End, new string[] { "ende", "quit" }),
				new AvailTokenItem(TokenTypes.List, new string[] { "list", "list" }),
				new AvailTokenItem(TokenTypes.Pruef, new string[] { "pruef", "test" }),
			};

			/// <summary>
			/// All available commands and command variants
			/// </summary>
			_availCommands = new AvailCommandItem[]
			{
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Help }), // show help
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Set }), // show all settings
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Pin }), // set new pin
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Timezone, TokenTypes.ArgInt }), // set timezone
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Pause }), // pause
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Pause, TokenTypes.ArgInt }), // pause <n> hours
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Pause, TokenTypes.Off }), // pause off
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Hours, TokenTypes.ArgInt, TokenTypes.ArgInt }), // set receive hours <from> <to>
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Hours, TokenTypes.Off }), // set receive hours off
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Clear }), // clear all pending news
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Max, TokenTypes.Mails, TokenTypes.ArgInt }), // set max mails per day
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Max, TokenTypes.Lines, TokenTypes.ArgInt }), // set max lines per day
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Max, TokenTypes.Pending, TokenTypes.ArgInt }), // set max pending mails
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Allowed, TokenTypes.Send, TokenTypes.ArgStr }), // set allowed sender
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Allowed, TokenTypes.Mails, TokenTypes.ArgStr }), // set allowed sender
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Allowed, TokenTypes.Telegram, TokenTypes.ArgStr }), // set allowed sender
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.EventCode, TokenTypes.ArgStr }), // set event code
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Show, TokenTypes.Send }), // set show sender address on
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Show, TokenTypes.Send, TokenTypes.Off }), // set show sender address off
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Send, TokenTypes.Mails }), // send mail
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Send, TokenTypes.Fax }), // send fax
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Send, TokenTypes.PunchTape }), // send tape punch
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.List, TokenTypes.Pruef }), // list test texts
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Send, TokenTypes.Pruef, TokenTypes.ArgStr } ), // send test text xxx
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Send, TokenTypes.Pruef, TokenTypes.ArgStr,
						TokenTypes.ArgInt } ), // send test text xxx, n times
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.End }), // end
			};

#if DEBUG
			CheckTokenPrefixes();
#endif
		}

		protected override CmdTokenItem GetPossibleTokens(AvailCommandItem[] possCmds, string tokenStr, int index)
		{
			List<CmdTokenItem> possTokens = new List<CmdTokenItem>();
			foreach (AvailCommandItem cv in possCmds)
			{
				if (cv.Length - 1 < index) continue;

				foreach (AvailTokenItem cmdToken in _availTokens)
				{
					if (cv.PossibleTokens[index] == cmdToken.TokenType && cmdToken.IsCommand(tokenStr, MinLen))
					{
						possTokens.Add(new CmdTokenItem(cmdToken.TokenType));
						continue;
					}
				}

				switch (cv.PossibleTokens[index])
				{
					case TokenTypes.ArgInt:
						if (int.TryParse(tokenStr, out int num))
						{
							possTokens.Add(new CmdTokenItem(TokenTypes.ArgInt, num));
						}
						break;
					case TokenTypes.ArgStr:
						possTokens.Add(new CmdTokenItem(TokenTypes.ArgStr, tokenStr));
						break;
				}
			}
			return possTokens.Count > 0 ? possTokens[0] : null;
		}
	}
}
