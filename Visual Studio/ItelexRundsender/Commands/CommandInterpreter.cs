using ItelexCommon.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ItelexRundsender.Commands
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
				new AvailTokenItem(TokenTypes.Pin, new string[] { "pin" }),
				new AvailTokenItem(TokenTypes.List, new string[] { "list", "liste" }),
				new AvailTokenItem(TokenTypes.Groups, new string[] { "groups", "gruppen" }),
				new AvailTokenItem(TokenTypes.Group, new string[] { "group", "gruppe" }),
				new AvailTokenItem(TokenTypes.Select, new string[] { "select", "auswaehlen", "waehle" }),
				new AvailTokenItem(TokenTypes.New, new string[] { "new", "neu" }),
				new AvailTokenItem(TokenTypes.Edit, new string[] { "edit", "editiere", "bearbeiten" }),
				new AvailTokenItem(TokenTypes.Delete, new string[] { "delete", "loeschen" }),
				new AvailTokenItem(TokenTypes.Numbers, new string[] { "numbers", "nummern", }),
				new AvailTokenItem(TokenTypes.Add, new string[] { "add", "zufuegen" }),
				new AvailTokenItem(TokenTypes.Remove, new string[] { "remove", "entferne" }),
				new AvailTokenItem(TokenTypes.End, new string[] { "quit", "ende" }),
			};

			/// <summary>
			/// All available commands and command variants
			/// </summary>
			_availCommands = new AvailCommandItem[]
			{
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Help }), // show help
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Pin }), // set new pin
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.List, TokenTypes.Groups }), // show groups
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Select, TokenTypes.Group }), // select a group
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Select, TokenTypes.Group, TokenTypes.ArgStr }), // select a group
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.New, TokenTypes.Group }), // new group
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.New, TokenTypes.Group, TokenTypes.ArgStr }), // new group
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Delete, TokenTypes.Group }), // remove group
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Edit, TokenTypes.Group }), // edit selected group settings
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Numbers }), // list numbers in selected group
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Add, TokenTypes.ArgInt }), // add a number to the selected group
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Add, TokenTypes.ArgInt, TokenTypes.ArgStr }), // add a number to selected group
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.Remove, TokenTypes.ArgInt }), // remove a number from the selected group
				new AvailCommandItem(new TokenTypes[]{ TokenTypes.End }), // end/quit
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
	}
}
