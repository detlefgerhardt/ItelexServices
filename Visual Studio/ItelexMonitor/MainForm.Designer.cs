
namespace ItelexMonitor
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.UpdateBtn = new System.Windows.Forms.Button();
			this.UpdateTimerLbl = new System.Windows.Forms.Label();
			this.TimeLbl = new System.Windows.Forms.Label();
			this.PrgmsView = new System.Windows.Forms.DataGridView();
			this.ServiceName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PrgmVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Address = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ItelexNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Status = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Uptime = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.LoginCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.LastLogin = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.LastUser = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.StartAllBtn = new System.Windows.Forms.Button();
			this.ShutdownAllBtn = new System.Windows.Forms.Button();
			this.UpdatingLbl = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.PrgmsView)).BeginInit();
			this.SuspendLayout();
			// 
			// UpdateBtn
			// 
			this.UpdateBtn.Location = new System.Drawing.Point(13, 13);
			this.UpdateBtn.Name = "UpdateBtn";
			this.UpdateBtn.Size = new System.Drawing.Size(75, 23);
			this.UpdateBtn.TabIndex = 1;
			this.UpdateBtn.Text = "Update";
			this.UpdateBtn.UseVisualStyleBackColor = true;
			this.UpdateBtn.Click += new System.EventHandler(this.UpdateBtn_Click);
			// 
			// UpdateTimerLbl
			// 
			this.UpdateTimerLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.UpdateTimerLbl.Location = new System.Drawing.Point(763, 21);
			this.UpdateTimerLbl.Name = "UpdateTimerLbl";
			this.UpdateTimerLbl.Size = new System.Drawing.Size(36, 13);
			this.UpdateTimerLbl.TabIndex = 2;
			this.UpdateTimerLbl.Text = "000";
			this.UpdateTimerLbl.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// TimeLbl
			// 
			this.TimeLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.TimeLbl.Location = new System.Drawing.Point(805, 18);
			this.TimeLbl.Name = "TimeLbl";
			this.TimeLbl.Size = new System.Drawing.Size(59, 18);
			this.TimeLbl.TabIndex = 3;
			this.TimeLbl.Text = "00:00:00";
			this.TimeLbl.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// PrgmsView
			// 
			this.PrgmsView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PrgmsView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.PrgmsView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ServiceName,
            this.PrgmVersion,
            this.Address,
            this.ItelexNumber,
            this.Status,
            this.Uptime,
            this.LoginCount,
            this.LastLogin,
            this.LastUser});
			this.PrgmsView.Location = new System.Drawing.Point(13, 42);
			this.PrgmsView.Name = "PrgmsView";
			this.PrgmsView.Size = new System.Drawing.Size(851, 225);
			this.PrgmsView.TabIndex = 5;
			// 
			// ServiceName
			// 
			this.ServiceName.HeaderText = "Service";
			this.ServiceName.Name = "ServiceName";
			this.ServiceName.Width = 130;
			// 
			// PrgmVersion
			// 
			this.PrgmVersion.HeaderText = "Version";
			this.PrgmVersion.Name = "PrgmVersion";
			this.PrgmVersion.Width = 55;
			// 
			// Address
			// 
			this.Address.HeaderText = "Address";
			this.Address.Name = "Address";
			this.Address.Width = 120;
			// 
			// ItelexNumber
			// 
			this.ItelexNumber.HeaderText = "Number";
			this.ItelexNumber.Name = "ItelexNumber";
			this.ItelexNumber.Width = 60;
			// 
			// Status
			// 
			this.Status.HeaderText = "Status";
			this.Status.Name = "Status";
			// 
			// Uptime
			// 
			this.Uptime.HeaderText = "Uptime";
			this.Uptime.Name = "Uptime";
			this.Uptime.Width = 50;
			// 
			// LoginCount
			// 
			this.LoginCount.HeaderText = "Count";
			this.LoginCount.Name = "LoginCount";
			this.LoginCount.Width = 50;
			// 
			// LastLogin
			// 
			this.LastLogin.HeaderText = "Last login";
			this.LastLogin.Name = "LastLogin";
			this.LastLogin.Width = 80;
			// 
			// LastUser
			// 
			this.LastUser.HeaderText = "Last user";
			this.LastUser.Name = "LastUser";
			this.LastUser.Width = 120;
			// 
			// StartAllBtn
			// 
			this.StartAllBtn.Location = new System.Drawing.Point(272, 16);
			this.StartAllBtn.Name = "StartAllBtn";
			this.StartAllBtn.Size = new System.Drawing.Size(75, 23);
			this.StartAllBtn.TabIndex = 6;
			this.StartAllBtn.Text = "Start all";
			this.StartAllBtn.UseVisualStyleBackColor = true;
			this.StartAllBtn.Click += new System.EventHandler(this.StartAllBtn_Click);
			// 
			// ShutdownAllBtn
			// 
			this.ShutdownAllBtn.Location = new System.Drawing.Point(381, 16);
			this.ShutdownAllBtn.Name = "ShutdownAllBtn";
			this.ShutdownAllBtn.Size = new System.Drawing.Size(75, 23);
			this.ShutdownAllBtn.TabIndex = 7;
			this.ShutdownAllBtn.Text = "Down all";
			this.ShutdownAllBtn.UseVisualStyleBackColor = true;
			this.ShutdownAllBtn.Click += new System.EventHandler(this.ShutdownAllBtn_Click);
			// 
			// UpdatingLbl
			// 
			this.UpdatingLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.UpdatingLbl.Location = new System.Drawing.Point(709, 21);
			this.UpdatingLbl.Name = "UpdatingLbl";
			this.UpdatingLbl.Size = new System.Drawing.Size(60, 13);
			this.UpdatingLbl.TabIndex = 8;
			this.UpdatingLbl.Text = "Updating";
			this.UpdatingLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(876, 279);
			this.Controls.Add(this.UpdatingLbl);
			this.Controls.Add(this.ShutdownAllBtn);
			this.Controls.Add(this.StartAllBtn);
			this.Controls.Add(this.PrgmsView);
			this.Controls.Add(this.TimeLbl);
			this.Controls.Add(this.UpdateTimerLbl);
			this.Controls.Add(this.UpdateBtn);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "MainForm";
			this.Text = "ItelexMonitor";
			((System.ComponentModel.ISupportInitialize)(this.PrgmsView)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button UpdateBtn;
		private System.Windows.Forms.Label UpdateTimerLbl;
		private System.Windows.Forms.Label TimeLbl;
		private System.Windows.Forms.DataGridView PrgmsView;
		private System.Windows.Forms.DataGridViewTextBoxColumn ServiceName;
		private System.Windows.Forms.DataGridViewTextBoxColumn PrgmVersion;
		private System.Windows.Forms.DataGridViewTextBoxColumn Address;
		private System.Windows.Forms.DataGridViewTextBoxColumn ItelexNumber;
		private System.Windows.Forms.DataGridViewTextBoxColumn Status;
		private System.Windows.Forms.DataGridViewTextBoxColumn Uptime;
		private System.Windows.Forms.DataGridViewTextBoxColumn LoginCount;
		private System.Windows.Forms.DataGridViewTextBoxColumn LastLogin;
		private System.Windows.Forms.DataGridViewTextBoxColumn LastUser;
		private System.Windows.Forms.Button StartAllBtn;
		private System.Windows.Forms.Button ShutdownAllBtn;
		private System.Windows.Forms.Label UpdatingLbl;
	}
}

