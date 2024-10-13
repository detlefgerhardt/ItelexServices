using ItelexCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItelexChatServer.Actions
{
	abstract class ActionBase : IAction
	{
		public enum ActionCallTypes { Direct, FromCmd};

		protected ItelexLogger _itelexLogger;

		protected IncomingChatConnection _chatConnection;

		protected bool _debug;

		protected ActionCallTypes _actionCallType;

		public enum Actions { None, CommandMode, NotifySetup, Hamurabi, Biorhythmus };

		public Actions Action { get; set; }

		public Language Language { get; set; }

		public ActionBase(Actions action, Language language, ActionCallTypes actionCallType, ItelexLogger itelexLogger)
		{
			_actionCallType = actionCallType;
			Action = action;
			Language = language;
			_itelexLogger = itelexLogger;
		}

		public string ChatAction
		{
			get
			{
				switch (Action)
				{
					case Actions.CommandMode:
						return "command";
					case Actions.NotifySetup:
						return "notifyset";
					case Actions.Hamurabi:
						return "hamurabi";
					default:
						return null;
				}
			}
		}

		public virtual void Run(IncomingChatConnection chatConnection, bool debug)
		{
			_chatConnection = chatConnection;
			_debug = debug;
		}
	}
}
