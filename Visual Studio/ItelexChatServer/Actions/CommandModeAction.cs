using ItelexChatServer.Command;
using ItelexChatServer.Languages;
using ItelexCommon;
using ItelexCommon.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Actions
{
	class CommandModeAction : ActionBase
	{
		private const string TAG = nameof(CommandModeAction);

		private bool _exitCommandState;

		public CommandModeAction(Language language, ItelexLogger itelexLogger) : base(Actions.CommandMode, language, ActionCallTypes.Direct, itelexLogger)
		{
		}

		public override void Run(IncomingChatConnection conn, bool debug)
		{
			base.Run(conn, debug);
			CommandMode();
		}

		private void CommandMode()
		{
			_exitCommandState = false;
			while (!_exitCommandState)
			{
				//SendCommandPrompt();
				_chatConnection.SendAscii("\r\n");
				InputResult cmdInput = _chatConnection.InputString("cmd:", ShiftStates.Ltrs, null, 40, 1);
				string cmdStr = cmdInput.InputString;
				if (string.IsNullOrWhiteSpace(cmdStr))
				{
					_chatConnection.ChatState = IncomingChatConnection.ChatStates.Idle;
					break;
				}

				CommandResult cmdResult = CommandManager.Instance.Parse(cmdStr);
				if (cmdResult == null)
				{
					_chatConnection.SendAscii(_chatConnection.LngText(LngKeys.CmdError));
				}
				else
				{
					ExecuteCommand(cmdResult);
				}
			}
		}

		private void ExecuteCommand(CommandResult cmdResult)
		{
			IncomingChatConnectionManager connManager = (IncomingChatConnectionManager)GlobalData.Instance.IncomingConnectionManager;

			switch (cmdResult.Cmd)
			{
				case Commands.End:
					_exitCommandState = true;
					break;
				case Commands.Help:
					_chatConnection.SendAscii($"{_chatConnection.LngText(LngKeys.CmdHelp)}");
					break;
				case Commands.Hold:
					if (cmdResult.Param == Commands.On)
					{
						_chatConnection.BuRefreshActive = true;
						_chatConnection.SendAscii($"{_chatConnection.LngText(LngKeys.HoldConnection)}: {_chatConnection.LngText(LngKeys.On)}");
					}
					else
					{
						_chatConnection.BuRefreshActive = false;
						_chatConnection.SendAscii($"{_chatConnection.LngText(LngKeys.HoldConnection)}: {_chatConnection.LngText(LngKeys.Off)}");
					}
					break;
				case Commands.List:
					_chatConnection.SendAscii($"{connManager.GetSubscribers(_chatConnection.ConnectionLanguage.Id)}");
					break;
				case Commands.History:
					int histCnt;
					if (!int.TryParse(cmdResult.FreeParam, out histCnt))
					{
						return;
					}
					if (histCnt > 30)
					{
						histCnt = 30;
					}
					_chatConnection.SendAscii(connManager.GetActivityHistory(_chatConnection.ConnectionLanguage.Id, histCnt));
					break;
				case Commands.Notifier:
					_chatConnection.StartNotifierSetup(ActionCallTypes.FromCmd);
					_exitCommandState = true;
					break;
				case Commands.Run:
					switch (cmdResult.Param)
					{
						case Commands.RunHelp:
							_chatConnection.SendAscii(_chatConnection.LngText(LngKeys.CmdRunHelp));
							_exitCommandState = false;
							break;
						case Commands.RunHamurabi:
							_chatConnection.StartHamurabi(ActionCallTypes.FromCmd);
							_exitCommandState = true;
							break;
						case Commands.RunBiorhythmus:
							_chatConnection.StartBiorhythmus(ActionCallTypes.FromCmd);
							_exitCommandState = true;
							break;
					}
					break;
			}
		}
	}
}
