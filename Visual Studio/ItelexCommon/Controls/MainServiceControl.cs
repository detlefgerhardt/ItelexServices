using ItelexCommon.Connection;
using ItelexCommon.Logger;
using ItelexCommon.Monitor;
using ItelexCommon.Utility;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ItelexCommon.Controls
{
	public partial class MainServiceControl : UserControl
	{
		private static readonly string TAG = nameof(MainServiceControl);

		public delegate void LoginLogoffEventHandler(string message);
		public event LoginLogoffEventHandler LoginLogoffEvent;

		public delegate void ShutDownEventHandler();
		public event ShutDownEventHandler ShutDownEvent;

		private readonly Logging _logger;

		private readonly MonitorManager _monitorManager;

		private readonly MessageDispatcher _messageDispatcher;

		private IncomingConnectionManagerAbstract _incomingManager;

		private IncomingConnectionManagerConfig _incomingManagerConfig;

		private OutgoingConnectionManagerAbstract _outgoingManager;

		private Action _button1Action;
		private Action _button2Action;

		public MainServiceControl()
		{
			InitializeComponent();

			_logger = LogManager.Instance.Logger;
			_messageDispatcher = MessageDispatcher.Instance;
			_messageDispatcher.Message += MessageDispatcher_Message;

			_messageDispatcher.Dispatch("--- start ---");

			Button1.Visible = false;
			Button2.Visible = false;

			IncomingView.View = View.Details;
			IncomingView.HideSelection = true;
			IncomingView.FullRowSelect = true;
			IncomingView.Sorting = SortOrder.None;
			IncomingView.HeaderStyle = ColumnHeaderStyle.None;
			IncomingView.Columns[0].Width = IncomingView.Width - 20;

			OutgoingView.View = View.Details;
			OutgoingView.HideSelection = true;
			OutgoingView.FullRowSelect = true;
			OutgoingView.Sorting = SortOrder.None;
			OutgoingView.HeaderStyle = ColumnHeaderStyle.None;
			OutgoingView.Columns[0].Width = OutgoingView.Width - 20;

			MessageView.View = View.Details;
			MessageView.HideSelection = true;
			MessageView.FullRowSelect = true;
			MessageView.Sorting = SortOrder.None;
			MessageView.HeaderStyle = ColumnHeaderStyle.None;
			MessageView.Columns[0].Width = MessageView.Width - 20;

			_monitorManager = MonitorManager.Instance;
			_monitorManager.Action += MonitorManager_Action;
		}

		public void StartIncomingConnections<T>(IncomingConnectionManagerConfig config) where T : IncomingConnectionManagerAbstract, new()
		{
			_incomingManagerConfig = config;
			_incomingManager = new T();
			GlobalData.Instance.IncomingConnectionManager = _incomingManager;
			GlobalData.Instance.ItelexValidExtensions = config.ItelexExtensions;
			_incomingManager.UpdateIncoming += IncomingManager_UpdateIncoming;

			_monitorManager.Start(_logger, config.MonitorPort, config.MonitorServerType,
				config.PrgmVersionStr, config.ExePath, config.ItelexNumber, config.IncomingLocalPort,
				config.IncomingPublicPort);

			_incomingManager.SetRecvOn(config);

			UpdateIpButton.Enabled = !config.FixDns;
			NumberTb.Text = config.ItelexNumber.ToString();
			ExtensionsTb.Text = GlobalData.Instance.GetValidExtensionsStr();
			if (!config.FixDns)
			{
				IpAddressTb.Text = $"{_incomingManager.OurIpAddressAndPort}";
			}
			InternPortTb.Text = config.IncomingLocalPort.ToString();

			IncomingView.Enabled = true;
		}

		public void StartOutgoingConnections<T>() where T : OutgoingConnectionManagerAbstract, new()
		{
			_outgoingManager = new T();
			GlobalData.Instance.OutgoingConnectionManager = _outgoingManager;
			_outgoingManager.UpdateOutgoing += OutgoingManager_UpdateOutgoing;
			OutgoingView.Enabled = true;
		}

		private void MonitorManager_Action(MonitorCmds cmd)
		{
			switch (cmd)
			{
				case MonitorCmds.Shutdown:
					ShutDown();
					//_exit = true;
					//FormsHelper.ControlInvokeRequired(this, () => Close());
					break;
			}
		}

		private void IncomingManager_UpdateIncoming()
		{
			FormsHelper.ControlInvokeRequired(IncomingView, () =>
			{
				List<ItelexIncoming> conns = _incomingManager.CloneConnections();
				//LogManager.Instance.Logger.Debug(TAG, nameof(IncomingManager_UpdateIncoming), $"{conns.Count} incoming connections");
				IncomingView.Items.Clear();
				foreach (ItelexIncoming conn in conns)
				{
					IncomingView.Items.Add(new ListViewItem(conn.ConnectionNameWithTime));
				};
			});
		}

		private void OutgoingManager_UpdateOutgoing()
		{
			FormsHelper.ControlInvokeRequired(OutgoingView, () =>
			{
				OutgoingView.Items.Clear();
				List<ItelexOutgoing> conns = _outgoingManager.CloneConnections();
				foreach (ItelexOutgoing conn in conns)
				{
					OutgoingView.Items.Add(new ListViewItem(conn.ConnectionNameWithTime));
				};
			});
		}

		private void MessageDispatcher_Message(string msg)
		{
			string[] list = msg.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

			FormsHelper.ControlInvokeRequired(MessageView, () =>
			{
				for (int i = 0; i < list.Length; i++)
				{
					MessageView.Items.Add(new ListViewItem($"{DateTime.Now:dd.MM.yy HH:mm:ss} {list[i].TrimEnd()}"));
				}
				if (MessageView.Items.Count > 0)
				{
					MessageView.EnsureVisible(MessageView.Items.Count - 1);
				}
				MessageView.Refresh();
			});
		}

		private void ShutDownBtn_Click(object sender, EventArgs e)
		{
			ShutDown();
		}

		private void ShutDown()
		{
			if (_incomingManager != null) _incomingManager.UpdateIncoming -= IncomingManager_UpdateIncoming;
			if (_outgoingManager != null) _outgoingManager.UpdateOutgoing -= OutgoingManager_UpdateOutgoing;
			_monitorManager.Action -= MonitorManager_Action;

			GlobalData.Instance.IncomingConnectionManager.Shutdown();
			_monitorManager.ShutDownPrgm();
			ShutDownEvent?.Invoke();
		}

		private void UpdateIpButton_Click(object sender, EventArgs e)
		{
			UpdateIpButton.Enabled = false;
			_incomingManager.UpdateIpAddress();
			UpdateIpButton.Enabled = true;
		}

		public void SetButton1(string text, Action action)
		{
			Button1.Text = text;
			Button1.Visible = true;
			_button1Action = action;
			Button1.Click += Button1_Click;
		}

		public void Button1_Enable(bool enable)
		{
			FormsHelper.ControlInvokeRequired(Button1, () => Button1.Enabled = enable);
		}

		private void Button1_Click(object sender, EventArgs e)
		{
			Button1.Enabled = false;
			_button1Action();
			Button1.Enabled = true;
		}

		public void SetButton2(string text, Action action)
		{
			Button2.Text = text;
			Button2.Visible = true;
			_button2Action = action;
			Button2.Click += Button2_Click;
		}

		public void Button2_Enable(bool enable)
		{
			FormsHelper.ControlInvokeRequired(Button2, () => Button2.Enabled = enable);
		}

		private void Button2_Click(object sender, EventArgs e)
		{
			Button2.Enabled = false;
			_button2Action();
			Button2.Enabled = true;
		}
	}
}
