using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Command
{
	public enum Commands
	{
		Help, End, Hold, List, History, Notifier, Run,
		None, On, Off, Tln, RunHelp, RunHamurabi, RunBiorhythmus
	}

	class CommandManager
	{
		private const string TAG = nameof(CommandManager);

		/// <summary>
		/// singleton pattern
		/// </summary>
		private static CommandManager instance;

		public static CommandManager Instance => instance ?? (instance = new CommandManager());

		private CommandManager()
		{
		}

		private CommandItem[] CommandList = new CommandItem[]
		{
			new CommandItem(Commands.Help, new string[] {"hilfe", "help","?" }, 0, 0),
			new CommandItem(Commands.End, new string[] {"end_e" }, 0, 0),
			new CommandItem(Commands.Hold, new string[] {"halten", "hold" }, 1, 0),
			new CommandItem(Commands.List, new string[] {"list" }, 1, 0),
			new CommandItem(Commands.History, new string[] {"hist_orie", "hist_ory" }, 0, 1),
			new CommandItem(Commands.Notifier, new string[] {"notif_y", "notif_ier", "benach_richtigung" }, 0, 0),
			new CommandItem(Commands.Run, new string[] {"run" }, 1, 0),
		};

		private CommandItem[] ParameterList = new CommandItem[]
		{
			new CommandItem(Commands.On, new string[] {"ein","on" }),
			new CommandItem(Commands.Off, new string[] {"aus","off" }),
			new CommandItem(Commands.Tln, new string[] {"teiln_ehmer","tln","member_s" }),
			new CommandItem(Commands.RunHelp, new string[] {"hilfe","help" }),
			new CommandItem(Commands.RunHamurabi, new string[] {"hamu_rabi","hammu_rabi" }),
			new CommandItem(Commands.RunBiorhythmus, new string[] {"bio_rhythmus", "bio_rhythm" }),
		};

		public CommandResult Parse(string line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				return null;
			}

			string[] list = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			CommandItem cmdItem = (from c in CommandList where c.CmdEquals(list[0]) select c).FirstOrDefault();
			CommandItem prmItem = null;
			if (cmdItem == null)
			{
				return null;
			}

			if (cmdItem.ParamCnt > 0)
			{
				if (list.Length < 2)
				{
					return null;
				}
				prmItem = (from p in ParameterList where p.CmdEquals(list[1]) select p).FirstOrDefault();
				if (prmItem == null)
				{
					return null;
				}
			}

			string freeParam = null;
			if (cmdItem.FreeParamCnt > 0)
			{
				if (list.Length<2)
				{
					return null;
				}

				freeParam = list[1].Trim();
			}

			return new CommandResult
			{
				Cmd = cmdItem.Cmd,
				Param = prmItem == null ? Commands.None : prmItem.Cmd,
				FreeParam = freeParam
			};
		}
	}
}
