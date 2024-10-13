using ItelexCommon;
using ItelexCommon.Logger;
using ItelexCommon.Monitor;
using ItelexCommon.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ItelexMonitor
{
	public partial class MainForm : Form
	{
		private const string TAG = nameof(MainForm);

		private Logging _logger;

		private MonitorManager _monitorManager;

		List<MonitorServerData> _serverList;

		List<ServiceItem> _serviceList;

		private const int UPDATE_INTERVAL = 30; // seconds
		private System.Timers.Timer _updateTimer;
		private int _updateTimerCount;

		private bool _updateActive = false;

		private bool _firstUpdate = true;
		public MainForm()
		{
			InitializeComponent();

			_logger = LogManager.Instance.Logger;

			this.Text = Helper.GetVersion();

			InitPrgmsView();

			_monitorManager = MonitorManager.Instance;
			_monitorManager.Start(_logger);
			_serverList = _monitorManager.GetServerList;

			_serviceList = new List<ServiceItem>();
			//LoadServiceData();

			_updateTimerCount = UPDATE_INTERVAL;
			_updateTimer = new System.Timers.Timer(1000);
			_updateTimer.Elapsed += UpdateTimer_Elapsed;
			_updateTimer.Start();
		}

		private void InitPrgmsView()
		{
			PrgmsView.BackgroundColor = Color.White;
			PrgmsView.RowHeadersVisible = false;
			PrgmsView.ScrollBars = ScrollBars.Both;
			PrgmsView.AllowUserToAddRows = false;
			PrgmsView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

			typeof(DataGridView).InvokeMember(
				"DoubleBuffered",
				BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
				null,
				PrgmsView,
				new object[] { true });

			var buttonCol = new DataGridViewButtonColumn
			{
				Name = "ButtonStart",
				HeaderText = "Start",
				Text = "Start",
				Width = 40,
				UseColumnTextForButtonValue = true
			};
			PrgmsView.Columns.Add(buttonCol);

			buttonCol = new DataGridViewButtonColumn
			{
				Name = "ButtonShutdown",
				HeaderText = "Down",
				Text = "Down",
				Width = 40,
				UseColumnTextForButtonValue = true
			};
			PrgmsView.Columns.Add(buttonCol);

			foreach (DataGridViewColumn column in PrgmsView.Columns)
			{
				column.SortMode = DataGridViewColumnSortMode.NotSortable;
			}

			PrgmsView.CellClick += PrgmsViews_CellClick;
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
		}

		private void PrgmsViews_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			int colIndex = e.ColumnIndex;
			int rowIndex = e.RowIndex;
			if (rowIndex == -1) return;

			DataGridViewRow row = PrgmsView.Rows[rowIndex];
			ServiceItem serviceItem = (ServiceItem)row.Tag;

			if (colIndex < 9) return;


			if (colIndex==9)
			{   // Start
				RequestStart(serviceItem.PrgmType);
				UpdateAll();
			}
			else if (colIndex==10)
			{   // Shutdown
				RequestShutdown(serviceItem.PrgmType);
				UpdateAll();
			}
		}

		private void UpdateBtn_Click(object sender, EventArgs e)
		{
			UpdateAll();
		}

		private async void StartAllBtn_Click(object sender, EventArgs e)
		{
			StartAllBtn.Enabled = false;

			await Task.Run(() =>
			{
				while (_updateActive)
				{
					Thread.Sleep(100);
				}

				_updateActive = true;
				try
				{
					foreach (MonitorServerData item in _serverList)
					{
						RequestStart(item.PrgmType);
					}
				}
				finally
				{
					_updateActive = false;
				}
			});

			StartAllBtn.Enabled = true;
		}

		private async void ShutdownAllBtn_Click(object sender, EventArgs e)
		{
			ShutdownAllBtn.Enabled = false;

			await Task.Run(() =>
			{
				while (_updateActive)
				{
					Thread.Sleep(100);
				}
				_updateActive = true;

				try
				{
					foreach (MonitorServerData item in _serverList)
					{
						RequestShutdown(item.PrgmType);
					}
					UpdateAll();
				}
				finally
				{
					_updateActive = false;
				}
			});

			ShutdownAllBtn.Enabled = true;
		}

		private void RequestStart(MonitorServerTypes type)
		{
			_monitorManager.StartPrgm(type);
		}

		private void RequestShutdown(MonitorServerTypes type)
		{
			MonitorServerData server = (from s in _serverList where s.PrgmType == type select s).FirstOrDefault();
			MonitorClientConnection monitorClient = new MonitorClientConnection(_logger);
			bool connected = monitorClient.Connect(server.Address, server.Port);
			if (!connected) return;
			MonitorResponseOk ok = monitorClient.RequestShutdown();
			//if (ok == null || !ok.Ok) -> error
			Debug.WriteLine("shutdown");
			//monitorClient.Disconnect();
		}

		private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (_updateActive) return;
			_updateActive = true;

			try
			{
				_updateTimerCount--;

				FormsHelper.ControlInvokeRequired(UpdateTimerLbl, () =>
				{
					UpdateTimerLbl.Text = _updateTimerCount.ToString();
				});

				FormsHelper.ControlInvokeRequired(TimeLbl, () =>
				{
					TimeLbl.Text = $"{DateTime.Now:HH:mm}";
				});

				if (_updateTimerCount <= 0 || _firstUpdate)
				{
					_updateTimerCount = UPDATE_INTERVAL;
					_firstUpdate = false;
					UpdateAll();
				}
			}
			finally
			{
				_updateActive = false;
			}
		}

		private void UpdateAll()
		{
			try
			{
				_updateTimerCount = UPDATE_INTERVAL;

				FormsHelper.ControlInvokeRequired(UpdatingLbl, () =>
				{
					UpdatingLbl.ForeColor = Color.Green;
					UpdatingLbl.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold);
				});

				UpdateAllServices();
				_serviceList.Sort(new ServiceItemComparer());
				//SaveServiceData();
				Update_PrgmsView();
			}
			finally
			{
				FormsHelper.ControlInvokeRequired(UpdatingLbl, () =>
				{
					UpdatingLbl.ForeColor = Color.Black;
					UpdatingLbl.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular);
				});
			}
		}

		private void UpdateAllServices()
		{
			if (_serverList == null) return;

			List<Task> tasks = new List<Task>();
			foreach (MonitorServerData server in _serverList)
			{
				Task<ServiceItem> task = GetServiceInfo(server);
				tasks.Add(task);
			}

			Task.WaitAll(tasks.ToArray());

			_serviceList.Clear();
			foreach (Task<ServiceItem> task in tasks)
			{
				if (task.Result != null) _serviceList.Add(task.Result);
			}
		}

		private Task<ServiceItem> GetServiceInfo(MonitorServerData server)
		{
			return Task.Run(() =>
			{
				MonitorClientConnection monitorClient = null;
				try
				{
					ServiceItem item = new ServiceItem()
					{
						PrgmType = server.PrgmType,
						Name = server.Name,
						Address = $"{server.Address}:{server.Port}",
					};

					monitorClient = new MonitorClientConnection(_logger);
					bool connected = monitorClient.Connect(server.Address, server.Port);
					if (!connected)
					{
						item.Status = MonitorServerStatus.ConnectionTimeout;
						return item;
					}

					MonitorResponseInfo info = monitorClient.RequestInfo();
					if (info == null) return null;

					item.PrgmVersion = info.Version;
					item.ItelexNumber = info.ItelexNumber;
					item.ItelexLocalPort = info.ItelexLocalPort;
					item.ItelexPublicPort = info.ItelexPublicPort;
					item.StartupTime = info.StartupTime;
					item.LastLoginTime = info.LastLoginTime;
					item.LastUser = info.LastUser;
					item.LoginCount = info.LoginCount;
					item.LoginUserCount = info.LoginUserCount;
					item.Status = info.Status;
					return item;
				}
				finally
				{
					if (monitorClient != null)
					{
						_logger.Debug(TAG, nameof(GetServiceInfo), "disconnect");
						monitorClient.Disconnect();
						monitorClient = null;
					}
				}
			});
		}

		private void Update_PrgmsView()
		{
			List<DataGridViewRow> rows = new List<DataGridViewRow>();

			for (int epgIndex = 0; epgIndex < _serviceList.Count; epgIndex++)
			{
				ServiceItem serviceItem = _serviceList[epgIndex];

				DataGridViewRow row = new DataGridViewRow();
				row.CreateCells(PrgmsView);

				DataGridViewCell channelCell = row.Cells[0];
				channelCell.Value = serviceItem.Name;

				channelCell = row.Cells[1];
				channelCell.Value = serviceItem.PrgmVersion;

				channelCell = row.Cells[2];
				channelCell.Value = serviceItem.Address;

				channelCell = row.Cells[3];
				channelCell.Value = serviceItem.ItelexNumber.ToString();

				channelCell = row.Cells[4];
				channelCell.Value = serviceItem.StatusString;

				channelCell = row.Cells[5];
				channelCell.Value = serviceItem.UptimeString;

				channelCell = row.Cells[6];
				channelCell.Value = $"{serviceItem.LoginUserCount}/{serviceItem.LoginCount}";

				channelCell = row.Cells[7];
				channelCell.Value = serviceItem.LastLoginTimeString;

				channelCell = row.Cells[8];
				channelCell.Value = serviceItem.LastUser;

				DataGridViewButtonCell btnCell = (row.Cells[9] as DataGridViewButtonCell);
				btnCell.FlatStyle = FlatStyle.Popup;

				btnCell = (row.Cells[10] as DataGridViewButtonCell);
				btnCell.FlatStyle = FlatStyle.Popup;

				row.Tag = serviceItem;

				rows.Add(row);
			}

			FormsHelper.ControlInvokeRequired(PrgmsView, () =>
			{
				PrgmsView.Rows.Clear();
				PrgmsView.Rows.AddRange(rows.ToArray());
				PrgmsView.ClearSelection();
			});
		}

		/*
		private void LoadServiceData()
		{
			_serviceList = new List<ServiceItem>();
			foreach (MonitorServer server in _serverList)
			{
				string fileName = Path.Combine(Helper.GetExePath(), server.Name) + ".dat";
				if (File.Exists(fileName))
				{
					string jsonStr = File.ReadAllText(fileName);
					ServiceItem fileItem = JsonConvert.DeserializeObject<ServiceItem>(jsonStr);
					if (fileItem != null) _serviceList.Add(fileItem);
				}
			}
			_serviceList.Sort(new ServiceItemComparer());
		}

		private bool SaveServiceData()
		{
			foreach(ServiceItem item in _serviceList)
			{
				if (item.Status == MonitorServerStatus.ConnectionTimeout) continue;

				try
				{
					string jsonStr;
					string fileName = Path.Combine(Helper.GetExePath(), item.Name) + ".dat";
					if (File.Exists(fileName))
					{
						jsonStr = File.ReadAllText(fileName);
						ServiceItem fileItem = JsonConvert.DeserializeObject<ServiceItem>(jsonStr);
						if (fileItem != null && fileItem.Equals(item)) continue;
					}
					jsonStr = JsonConvert.SerializeObject(item);
					File.WriteAllText(fileName, jsonStr);
				}
				catch (Exception)
				{
					return false;
				}
			}
			return true;
		}
		*/

	}

	class ServiceItemComparer : Comparer<ServiceItem>
	{

		public override int Compare(ServiceItem item1, ServiceItem item2)
		{
			return item1.PrgmType.CompareTo(item2.PrgmType);
		}
	}
}