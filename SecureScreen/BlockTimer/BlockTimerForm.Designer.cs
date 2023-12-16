namespace BlockTimer
{
	partial class BlockTimerForm
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
			if (disposing && (components != null)) {
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
			this.timeLabel = new System.Windows.Forms.Label();
			this.timer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// timeLabel
			// 
			this.timeLabel.BackColor = System.Drawing.SystemColors.Control;
			this.timeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.timeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 72F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.timeLabel.ForeColor = System.Drawing.SystemColors.ControlText;
			this.timeLabel.Location = new System.Drawing.Point(4, 4);
			this.timeLabel.Name = "timeLabel";
			this.timeLabel.Size = new System.Drawing.Size(290, 108);
			this.timeLabel.TabIndex = 0;
			this.timeLabel.Text = "00:00";
			this.timeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.timeLabel.Click += new System.EventHandler(this.OnClick);
			this.timeLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnFormMouseDown);
			this.timeLabel.MouseEnter += new System.EventHandler(this.OnFormMouseEnter);
			this.timeLabel.MouseLeave += new System.EventHandler(this.OnFormMouseLeave);
			// 
			// timer
			// 
			this.timer.Tick += new System.EventHandler(this.OnTimerTick);
			// 
			// BlockTimerForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.BackColor = System.Drawing.SystemColors.Highlight;
			this.ClientSize = new System.Drawing.Size(298, 116);
			this.Controls.Add(this.timeLabel);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "BlockTimerForm";
			this.Padding = new System.Windows.Forms.Padding(4);
			this.Text = "TimerForm";
			this.TopMost = true;
			this.Click += new System.EventHandler(this.OnClick);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnFormMouseDown);
			this.MouseEnter += new System.EventHandler(this.OnFormMouseEnter);
			this.MouseLeave += new System.EventHandler(this.OnFormMouseLeave);
			this.Resize += new System.EventHandler(this.OnFormResize);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label timeLabel;
		private System.Windows.Forms.Timer timer;
	}
}
