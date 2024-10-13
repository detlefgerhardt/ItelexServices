
namespace ItelexCommon
{
	partial class DebugForm
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
			this.TextRtb = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// TextRtb
			// 
			this.TextRtb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TextRtb.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.TextRtb.Location = new System.Drawing.Point(12, 12);
			this.TextRtb.Name = "TextRtb";
			this.TextRtb.Size = new System.Drawing.Size(499, 465);
			this.TextRtb.TabIndex = 1;
			this.TextRtb.Text = "";
			// 
			// DebugForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(523, 489);
			this.Controls.Add(this.TextRtb);
			this.Name = "DebugForm";
			this.Text = "DebugForm";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.RichTextBox TextRtb;
	}
}