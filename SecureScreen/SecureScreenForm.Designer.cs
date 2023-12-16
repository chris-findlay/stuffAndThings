namespace SecureScreen
{
	partial class SecureScreenForm
	{
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
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

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// SecureScreenForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.ClientSize = new System.Drawing.Size(16, 9);
			this.ControlBox = false;
			this.Cursor = System.Windows.Forms.Cursors.No;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Opacity = 0.5D;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "SecureScreen";
			this.TopMost = true;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnClosing);
			this.Shown += new System.EventHandler(this.OnShown);
			this.LocationChanged += new System.EventHandler(this.OnLocationChanged);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseDown);
			this.MouseEnter += new System.EventHandler(this.OnMouseEnter);
			this.MouseLeave += new System.EventHandler(this.OnMouseLeave);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMove);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnMouseUp);
			this.ResumeLayout(false);

		}

		#endregion
	}
}
