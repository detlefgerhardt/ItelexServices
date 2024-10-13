namespace ItelexBaudotArtServer
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.NotifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
			this.MainServiceCtrl = new ItelexCommon.Controls.MainServiceControl();
			this.SuspendLayout();
			// 
			// NotifyIcon1
			// 
			this.NotifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("NotifyIcon1.Icon")));
			this.NotifyIcon1.Text = "notifyIcon1";
			this.NotifyIcon1.Visible = true;
			// 
			// MainServiceCtrl
			// 
			this.MainServiceCtrl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainServiceCtrl.Location = new System.Drawing.Point(13, 13);
			this.MainServiceCtrl.Name = "MainServiceCtrl";
			this.MainServiceCtrl.Size = new System.Drawing.Size(919, 582);
			this.MainServiceCtrl.TabIndex = 0;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(942, 607);
			this.Controls.Add(this.MainServiceCtrl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(915, 400);
			this.Name = "MainForm";
			this.Text = "i-Telex Baudort-Art Server";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.NotifyIcon NotifyIcon1;
		private ItelexCommon.Controls.MainServiceControl MainServiceCtrl;
	}
}

