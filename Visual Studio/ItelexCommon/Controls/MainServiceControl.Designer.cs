namespace ItelexCommon.Controls
{
	partial class MainServiceControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.ExtensionsTb = new System.Windows.Forms.TextBox();
			this.ExtensionsLbl = new System.Windows.Forms.Label();
			this.UpdateIpButton = new System.Windows.Forms.Button();
			this.InternPortLbl = new System.Windows.Forms.Label();
			this.InternPortTb = new System.Windows.Forms.TextBox();
			this.IpAddressTb = new System.Windows.Forms.TextBox();
			this.NumberTb = new System.Windows.Forms.TextBox();
			this.IpAddressLbl = new System.Windows.Forms.Label();
			this.NumberLbl = new System.Windows.Forms.Label();
			this.MessageView = new System.Windows.Forms.ListView();
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.IncomingView = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.OutgoingView = new System.Windows.Forms.ListView();
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ShutDownBtn = new System.Windows.Forms.Button();
			this.IncomingLbl = new System.Windows.Forms.Label();
			this.OutgoingLbl = new System.Windows.Forms.Label();
			this.Button1 = new System.Windows.Forms.Button();
			this.Button2 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// ExtensionsTb
			// 
			this.ExtensionsTb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ExtensionsTb.Location = new System.Drawing.Point(686, 78);
			this.ExtensionsTb.Name = "ExtensionsTb";
			this.ExtensionsTb.ReadOnly = true;
			this.ExtensionsTb.Size = new System.Drawing.Size(79, 20);
			this.ExtensionsTb.TabIndex = 28;
			// 
			// ExtensionsLbl
			// 
			this.ExtensionsLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ExtensionsLbl.AutoSize = true;
			this.ExtensionsLbl.Location = new System.Drawing.Point(616, 81);
			this.ExtensionsLbl.Name = "ExtensionsLbl";
			this.ExtensionsLbl.Size = new System.Drawing.Size(67, 13);
			this.ExtensionsLbl.TabIndex = 27;
			this.ExtensionsLbl.Text = "Extension(s):";
			// 
			// UpdateIpButton
			// 
			this.UpdateIpButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.UpdateIpButton.Location = new System.Drawing.Point(841, 25);
			this.UpdateIpButton.Name = "UpdateIpButton";
			this.UpdateIpButton.Size = new System.Drawing.Size(75, 23);
			this.UpdateIpButton.TabIndex = 25;
			this.UpdateIpButton.Text = "Update IP";
			this.UpdateIpButton.UseVisualStyleBackColor = true;
			this.UpdateIpButton.Click += new System.EventHandler(this.UpdateIpButton_Click);
			// 
			// InternPortLbl
			// 
			this.InternPortLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.InternPortLbl.AutoSize = true;
			this.InternPortLbl.Location = new System.Drawing.Point(616, 55);
			this.InternPortLbl.Name = "InternPortLbl";
			this.InternPortLbl.Size = new System.Drawing.Size(46, 13);
			this.InternPortLbl.TabIndex = 23;
			this.InternPortLbl.Text = "int. Port:";
			// 
			// InternPortTb
			// 
			this.InternPortTb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.InternPortTb.Location = new System.Drawing.Point(686, 52);
			this.InternPortTb.Name = "InternPortTb";
			this.InternPortTb.ReadOnly = true;
			this.InternPortTb.Size = new System.Drawing.Size(60, 20);
			this.InternPortTb.TabIndex = 22;
			// 
			// IpAddressTb
			// 
			this.IpAddressTb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.IpAddressTb.Location = new System.Drawing.Point(686, 26);
			this.IpAddressTb.Name = "IpAddressTb";
			this.IpAddressTb.ReadOnly = true;
			this.IpAddressTb.Size = new System.Drawing.Size(141, 20);
			this.IpAddressTb.TabIndex = 21;
			// 
			// NumberTb
			// 
			this.NumberTb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.NumberTb.Location = new System.Drawing.Point(686, 0);
			this.NumberTb.Name = "NumberTb";
			this.NumberTb.ReadOnly = true;
			this.NumberTb.Size = new System.Drawing.Size(141, 20);
			this.NumberTb.TabIndex = 20;
			// 
			// IpAddressLbl
			// 
			this.IpAddressLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.IpAddressLbl.AutoSize = true;
			this.IpAddressLbl.Location = new System.Drawing.Point(616, 29);
			this.IpAddressLbl.Name = "IpAddressLbl";
			this.IpAddressLbl.Size = new System.Drawing.Size(64, 13);
			this.IpAddressLbl.TabIndex = 19;
			this.IpAddressLbl.Text = "ext. IP/Port:";
			// 
			// NumberLbl
			// 
			this.NumberLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.NumberLbl.AutoSize = true;
			this.NumberLbl.Location = new System.Drawing.Point(616, 3);
			this.NumberLbl.Name = "NumberLbl";
			this.NumberLbl.Size = new System.Drawing.Size(47, 13);
			this.NumberLbl.TabIndex = 18;
			this.NumberLbl.Text = "Number:";
			// 
			// MessageView
			// 
			this.MessageView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MessageView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2});
			this.MessageView.HideSelection = false;
			this.MessageView.Location = new System.Drawing.Point(0, 0);
			this.MessageView.Name = "MessageView";
			this.MessageView.Size = new System.Drawing.Size(606, 580);
			this.MessageView.TabIndex = 17;
			this.MessageView.UseCompatibleStateImageBehavior = false;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Width = 120;
			// 
			// IncomingView
			// 
			this.IncomingView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.IncomingView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
			this.IncomingView.Enabled = false;
			this.IncomingView.HideSelection = false;
			this.IncomingView.Location = new System.Drawing.Point(612, 120);
			this.IncomingView.Name = "IncomingView";
			this.IncomingView.Size = new System.Drawing.Size(305, 172);
			this.IncomingView.TabIndex = 15;
			this.IncomingView.UseCompatibleStateImageBehavior = false;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Width = 120;
			// 
			// OutgoingView
			// 
			this.OutgoingView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.OutgoingView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3});
			this.OutgoingView.Enabled = false;
			this.OutgoingView.HideSelection = false;
			this.OutgoingView.Location = new System.Drawing.Point(612, 311);
			this.OutgoingView.Name = "OutgoingView";
			this.OutgoingView.Size = new System.Drawing.Size(305, 269);
			this.OutgoingView.TabIndex = 29;
			this.OutgoingView.UseCompatibleStateImageBehavior = false;
			// 
			// columnHeader3
			// 
			this.columnHeader3.Width = 120;
			// 
			// ShutDownBtn
			// 
			this.ShutDownBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ShutDownBtn.Location = new System.Drawing.Point(842, -1);
			this.ShutDownBtn.Name = "ShutDownBtn";
			this.ShutDownBtn.Size = new System.Drawing.Size(75, 23);
			this.ShutDownBtn.TabIndex = 30;
			this.ShutDownBtn.Text = "Shut down";
			this.ShutDownBtn.UseVisualStyleBackColor = true;
			this.ShutDownBtn.Click += new System.EventHandler(this.ShutDownBtn_Click);
			// 
			// IncomingLbl
			// 
			this.IncomingLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.IncomingLbl.AutoSize = true;
			this.IncomingLbl.Location = new System.Drawing.Point(609, 104);
			this.IncomingLbl.Name = "IncomingLbl";
			this.IncomingLbl.Size = new System.Drawing.Size(53, 13);
			this.IncomingLbl.TabIndex = 31;
			this.IncomingLbl.Text = "Incoming:";
			// 
			// OutgoingLbl
			// 
			this.OutgoingLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.OutgoingLbl.AutoSize = true;
			this.OutgoingLbl.Location = new System.Drawing.Point(612, 295);
			this.OutgoingLbl.Name = "OutgoingLbl";
			this.OutgoingLbl.Size = new System.Drawing.Size(53, 13);
			this.OutgoingLbl.TabIndex = 32;
			this.OutgoingLbl.Text = "Outgoing:";
			// 
			// Button1
			// 
			this.Button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.Button1.Location = new System.Drawing.Point(841, 56);
			this.Button1.Name = "Button1";
			this.Button1.Size = new System.Drawing.Size(75, 23);
			this.Button1.TabIndex = 33;
			this.Button1.Text = "Button1";
			this.Button1.UseVisualStyleBackColor = true;
			// 
			// Button2
			// 
			this.Button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.Button2.Location = new System.Drawing.Point(841, 82);
			this.Button2.Name = "Button2";
			this.Button2.Size = new System.Drawing.Size(75, 23);
			this.Button2.TabIndex = 34;
			this.Button2.Text = "Button2";
			this.Button2.UseVisualStyleBackColor = true;
			// 
			// MainServiceControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.Button2);
			this.Controls.Add(this.Button1);
			this.Controls.Add(this.OutgoingLbl);
			this.Controls.Add(this.IncomingLbl);
			this.Controls.Add(this.ShutDownBtn);
			this.Controls.Add(this.OutgoingView);
			this.Controls.Add(this.ExtensionsTb);
			this.Controls.Add(this.ExtensionsLbl);
			this.Controls.Add(this.UpdateIpButton);
			this.Controls.Add(this.InternPortLbl);
			this.Controls.Add(this.InternPortTb);
			this.Controls.Add(this.IpAddressTb);
			this.Controls.Add(this.NumberTb);
			this.Controls.Add(this.IpAddressLbl);
			this.Controls.Add(this.NumberLbl);
			this.Controls.Add(this.MessageView);
			this.Controls.Add(this.IncomingView);
			this.Name = "MainServiceControl";
			this.Size = new System.Drawing.Size(919, 582);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox ExtensionsTb;
		private System.Windows.Forms.Label ExtensionsLbl;
		private System.Windows.Forms.Button UpdateIpButton;
		private System.Windows.Forms.Label InternPortLbl;
		private System.Windows.Forms.TextBox InternPortTb;
		private System.Windows.Forms.TextBox IpAddressTb;
		private System.Windows.Forms.TextBox NumberTb;
		private System.Windows.Forms.Label IpAddressLbl;
		private System.Windows.Forms.Label NumberLbl;
		private System.Windows.Forms.ListView MessageView;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ListView IncomingView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ListView OutgoingView;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.Button ShutDownBtn;
		private System.Windows.Forms.Label IncomingLbl;
		private System.Windows.Forms.Label OutgoingLbl;
		private System.Windows.Forms.Button Button1;
		private System.Windows.Forms.Button Button2;
	}
}
