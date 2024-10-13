using ItelexCommon.Commands;
using ItelexNewsServer.Commands;
using ItelexNewsServer.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ItelexNewsServer.Commands
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
				new AvailTokenItem(TokenTypes.List, new string[] { "list", "liste" }),
				new AvailTokenItem(TokenTypes.Category, new string[] { "category", "kategorie" }),
				new AvailTokenItem(TokenTypes.Subscribed, new string[] { "subscribed", "abonniert" }),
				new AvailTokenItem(TokenTypes.Language, new string[] { "language", "sprache" }),
				new AvailTokenItem(TokenTypes.Subscribe, new string[] { "+" }),
				new AvailTokenItem(TokenTypes.Unsubscribe, new string[] { "-" }),
				new AvailTokenItem(TokenTypes.Set, new string[] { "settings", "einstellungen" }),
				new AvailTokenItem(TokenTypes.Hours, new string[] { "hours", "stunden" }),
				new AvailTokenItem(TokenTypes.Number, new string[] { "number", "nummer" }),
				new AvailTokenItem(TokenTypes.Redirect, new string[] { "redirect", "umleiten" }),
				new AvailTokenItem(TokenTypes.Timezone, new string[] { "timezone", "zeitzone" }),
				new AvailTokenItem(TokenTypes.Format, new string[] { "format" }),
				new AvailTokenItem(TokenTypes.MaxPendMsgs, new string[] { "max" }),
				new AvailTokenItem(TokenTypes.Pin, new string[] { "pin" }),
				new AvailTokenItem(TokenTypes.Pause, new string[] { "pause" }),
				new AvailTokenItem(TokenTypes.Clear, new string[] { "clear" }),
				new AvailTokenItem(TokenTypes.Local, new string[] { "local", "lokal" }),
				new AvailTokenItem(TokenTypes.Select, new string[] { "select", "auswaehlen" }),
				new AvailTokenItem(TokenTypes.New, new string[] { "new", "neu" }),
				new AvailTokenItem(TokenTypes.Edit, new string[] { "edit", "bearbeiten" }),
				new AvailTokenItem(TokenTypes.Delete, new string[] { "delete", "remove", "loeschen" }),
				new AvailTokenItem(TokenTypes.Numbers, new string[] { "numbers", "nummern" }),
				new AvailTokenItem(TokenTypes.Send, new string[] { "send", "senden" }),
				new AvailTokenItem(TokenTypes.Add, new string[] { "add", "zufuegen" }),
				new AvailTokenItem(TokenTypes.Off, new string[] { "off", "aus" }),
				//new AvailTokenItem(TokenTypes.Preview, new string[] { "preview", "vorschau" }),
				new AvailTokenItem(TokenTypes.End, new string[] { "end", "ende", "quit", "x" }),
			};

			/// <summary>
			/// All available commands and command variants
			/// </summary>
			_availCommands = new AvailCommandItem[]
			{
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Help }), // show help
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.End }), // end
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.List }), // list all channels
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.List, TokenTypes.Local}), // list all local channels
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.List, TokenTypes.Subscribed }), // list subscribed channels
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.List, TokenTypes.Category, TokenTypes.ArgCat }), // list channels by category
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.List, TokenTypes.Language, TokenTypes.ArgLng }), // list channels by <language>
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Subscribe }), // subscribe
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Subscribe, TokenTypes.ArgInt }), // subscribe <channelnr>
				//new AvailCommandItem(new TokenTypes[]{ TokenTypes.Subscribe, TokenTypes.ArgInt, TokenTypes.ArgCont }), // subscribe <channelnr> content
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Preview, TokenTypes.ArgInt }), // preview <channelnr>
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Unsubscribe }), // unsubscribe
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Unsubscribe, TokenTypes.ArgInt }), // unsubscribe <channelnr>
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Set }), // show all settings
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Hours, TokenTypes.ArgInt, TokenTypes.ArgInt }), // set receive hours <from> <to>
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Hours, TokenTypes.Off }), // set receive hours off
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Redirect, TokenTypes.ArgInt }), // set redirect number
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Redirect, TokenTypes.Off }), // set redirect off
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Timezone, TokenTypes.ArgInt }), // set timezone
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Format, TokenTypes.ArgMsgFormat }), // set msg format <n>
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.MaxPendMsgs, TokenTypes.ArgInt }), // set max. pending msgs <n>
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Pin }), // set new pin
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Pause }), // pause
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Pause, TokenTypes.ArgInt }), // pause <n> hours
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Pause, TokenTypes.Off }), // pause off
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Clear }), // clear all pending news

				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Help, TokenTypes.Local}), // help for local channels
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Select, TokenTypes.Local}), // select a local channel
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Select, TokenTypes.Local, TokenTypes.ArgInt}), // select a local channel
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.New, TokenTypes.Local }), // add a new local channel
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.New, TokenTypes.Local, TokenTypes.ArgStr }), // add a new local channel <channelnr>
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Edit, TokenTypes.Local }), // edit selected local channel
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Delete, TokenTypes.Local }), // delete selected local channel
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Numbers}), // list numbers/subscribers for selected local channel
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Numbers, TokenTypes.ArgInt}), // list numbers/subscribers for a local channel
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Send, TokenTypes.Local }), // send message to local channel
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Send, TokenTypes.Local, TokenTypes.ArgInt}), // send message to local channel
			};

#if DEBUG
			CheckTokenPrefixes();
			CheckCategoryPrefixes();
#endif
		}

		protected override CmdTokenItem GetPossibleTokens(AvailCommandItem[] possCmds, string tokenStr, int index)
		{
			List<CmdTokenItem> possTokens = new List<CmdTokenItem>();
			foreach (AvailCommandItem cv in possCmds)
			{
				if (cv.Length - 1 < index) continue;

				switch (cv.PossibleTokens[index])
				{
					case TokenTypes.ArgCat:
						ChannelCategoryItem catItem = ChannelItem.GetCategory(tokenStr);
						if (catItem != null)
						{
							possTokens.Add(new CmdTokenItem(TokenTypes.ArgCat, catItem));
						}
						break;
					case TokenTypes.ArgLng:
						ChannelLanguageItem lngItem = ChannelItem.GetLanguage(tokenStr);
						if (lngItem != null)
						{ 
							possTokens.Add(new CmdTokenItem(TokenTypes.ArgLng, lngItem));
						}
						break;
					case TokenTypes.ArgMsgFormat:
						MsgFormatItem msgFormat = UserItem.GetMsgFormat(tokenStr);
						if (msgFormat != null)
						{
							possTokens.Add(new CmdTokenItem(TokenTypes.ArgMsgFormat, msgFormat));
						}
						break;
					/*
				case TokenTypes.ArgCont:
					ChannelContentItem cont = ChannelItem.Contents.Where(r => r.Content == tokenStr).FirstOrDefault();
					if (cont != null)
					{
						possTokens.Add(new CmdTokenItem(TokenTypes.ArgCont, tokenStr));
					}
					break;
					*/
					case TokenTypes.ArgInt:
						if (int.TryParse(tokenStr, out int num))
						{
							possTokens.Add(new CmdTokenItem(TokenTypes.ArgInt, num));
						}
						break;
					case TokenTypes.ArgStr:
						possTokens.Add(new CmdTokenItem(TokenTypes.ArgStr, tokenStr));
						break;
					default:
						foreach (AvailTokenItem cmdToken in _availTokens)
						{
							if (cv.PossibleTokens[index] == cmdToken.TokenType && cmdToken.IsCommand(tokenStr, MinLen))
							{
								possTokens.Add(new CmdTokenItem(cmdToken.TokenType));
							}
						}
						break;
				}
			}
			return possTokens.Count > 0 ? possTokens[0] : null;
		}

		private void CheckCategoryPrefixes()
		{
			List<string> prefixes = new List<string>();
			foreach (ChannelCategoryItem cat in ChannelItem.Categories)
			{
				if (prefixes.Contains(cat.ShortName))
				{
					Debug.WriteLine($"category conflict: {cat.ShortName}");
				}
				else
				{
					prefixes.Add(cat.ShortName);
				}
			}
			Debug.WriteLine("no token/category naming conflicts");
		}
	}

}
