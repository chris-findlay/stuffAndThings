namespace BlockTimer
{
	partial class BlockTimerOptionsForm
	{
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null)) {
				components.Dispose();
			}

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.groupBox = new System.Windows.Forms.GroupBox();
			this.quitButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.startButton = new System.Windows.Forms.Button();
			this.errorLabel = new System.Windows.Forms.Label();
			this.confirmPasswordLabel = new System.Windows.Forms.Label();
			this.passwordLabel = new System.Windows.Forms.Label();
			this.confirmPasswordBox = new System.Windows.Forms.TextBox();
			this.passwordBox = new System.Windows.Forms.TextBox();
			this.timeLimitComboBox = new System.Windows.Forms.ComboBox();
			this.timeLabel = new System.Windows.Forms.Label();
			this.groupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox
			// 
			this.groupBox.AccessibleName = "Options";
			this.groupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox.Controls.Add(this.quitButton);
			this.groupBox.Controls.Add(this.cancelButton);
			this.groupBox.Controls.Add(this.startButton);
			this.groupBox.Controls.Add(this.errorLabel);
			this.groupBox.Controls.Add(this.confirmPasswordLabel);
			this.groupBox.Controls.Add(this.passwordLabel);
			this.groupBox.Controls.Add(this.confirmPasswordBox);
			this.groupBox.Controls.Add(this.passwordBox);
			this.groupBox.Controls.Add(this.timeLimitComboBox);
			this.groupBox.Controls.Add(this.timeLabel);
			this.groupBox.Location = new System.Drawing.Point(12, 12);
			this.groupBox.MaximumSize = new System.Drawing.Size(252, 142);
			this.groupBox.MinimumSize = new System.Drawing.Size(252, 142);
			this.groupBox.Name = "groupBox";
			this.groupBox.Size = new System.Drawing.Size(252, 142);
			this.groupBox.TabIndex = 0;
			this.groupBox.TabStop = false;
			this.groupBox.Text = "Options";
			// 
			// quitButton
			// 
			this.quitButton.DialogResult = System.Windows.Forms.DialogResult.Abort;
			this.quitButton.Location = new System.Drawing.Point(87, 109);
			this.quitButton.Name = "quitButton";
			this.quitButton.Size = new System.Drawing.Size(75, 23);
			this.quitButton.TabIndex = 8;
			this.quitButton.Text = "&Quit!";
			this.quitButton.UseVisualStyleBackColor = true;
			// 
			// cancelButton
			// 
			this.cancelButton.AccessibleName = "Cancel Options Dialog Button";
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(169, 109);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 9;
			this.cancelButton.Text = "&Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// startButton
			// 
			this.startButton.AccessibleName = "Start Timer Button";
			this.startButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.startButton.Location = new System.Drawing.Point(6, 109);
			this.startButton.Name = "startButton";
			this.startButton.Size = new System.Drawing.Size(75, 23);
			this.startButton.TabIndex = 7;
			this.startButton.Text = "&Start";
			this.startButton.UseVisualStyleBackColor = true;
			// 
			// errorLabel
			// 
			this.errorLabel.AccessibleName = "Error Label";
			this.errorLabel.AutoSize = true;
			this.errorLabel.ForeColor = System.Drawing.Color.Red;
			this.errorLabel.Location = new System.Drawing.Point(23, 93);
			this.errorLabel.Name = "errorLabel";
			this.errorLabel.Size = new System.Drawing.Size(35, 13);
			this.errorLabel.TabIndex = 6;
			this.errorLabel.Text = "Error: ";
			this.errorLabel.Visible = false;
			// 
			// confirmPasswordLabel
			// 
			this.confirmPasswordLabel.AccessibleName = "Confirm Password Label";
			this.confirmPasswordLabel.AutoSize = true;
			this.confirmPasswordLabel.Location = new System.Drawing.Point(6, 76);
			this.confirmPasswordLabel.Name = "confirmPasswordLabel";
			this.confirmPasswordLabel.Size = new System.Drawing.Size(91, 13);
			this.confirmPasswordLabel.TabIndex = 4;
			this.confirmPasswordLabel.Text = "&Confirm Password";
			this.confirmPasswordLabel.Enter += new System.EventHandler(this.ConfirmPasswordLabel_Enter);
			// 
			// passwordLabel
			// 
			this.passwordLabel.AccessibleName = "Password Label";
			this.passwordLabel.AutoSize = true;
			this.passwordLabel.Location = new System.Drawing.Point(6, 50);
			this.passwordLabel.Name = "passwordLabel";
			this.passwordLabel.Size = new System.Drawing.Size(53, 13);
			this.passwordLabel.TabIndex = 2;
			this.passwordLabel.Text = "&Password";
			this.passwordLabel.Enter += new System.EventHandler(this.PasswordLabel_Enter);
			// 
			// confirmPasswordBox
			// 
			this.confirmPasswordBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.confirmPasswordBox.Location = new System.Drawing.Point(106, 73);
			this.confirmPasswordBox.Name = "confirmPasswordBox";
			this.confirmPasswordBox.PasswordChar = '●';
			this.confirmPasswordBox.Size = new System.Drawing.Size(138, 20);
			this.confirmPasswordBox.TabIndex = 5;
			this.confirmPasswordBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnKeyPress);
			this.confirmPasswordBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnKeyUp);
			this.confirmPasswordBox.Leave += new System.EventHandler(this.OnBlur);
			// 
			// passwordBox
			// 
			this.passwordBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.passwordBox.Location = new System.Drawing.Point(106, 47);
			this.passwordBox.Name = "passwordBox";
			this.passwordBox.PasswordChar = '●';
			this.passwordBox.Size = new System.Drawing.Size(138, 20);
			this.passwordBox.TabIndex = 3;
			this.passwordBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnKeyPress);
			this.passwordBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnKeyUp);
			this.passwordBox.Leave += new System.EventHandler(this.OnBlur);
			// 
			// timeLimitComboBox
			// 
			this.timeLimitComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.timeLimitComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.timeLimitComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
			this.timeLimitComboBox.Items.AddRange(new object[] {
            "01:00",
            "02:00",
            "05:00",
            "10:00",
            "15:00",
            "20:00",
            "30:00",
            "45:00",
            "50:00",
            "55:00",
            "60:00"});
			this.timeLimitComboBox.Location = new System.Drawing.Point(106, 19);
			this.timeLimitComboBox.Name = "timeLimitComboBox";
			this.timeLimitComboBox.Size = new System.Drawing.Size(138, 21);
			this.timeLimitComboBox.TabIndex = 1;
			this.timeLimitComboBox.Text = "30:00";
			// 
			// timeLabel
			// 
			this.timeLabel.AccessibleName = "Specify Time Limit Label";
			this.timeLabel.AutoSize = true;
			this.timeLabel.Location = new System.Drawing.Point(6, 22);
			this.timeLabel.Name = "timeLabel";
			this.timeLabel.Size = new System.Drawing.Size(92, 13);
			this.timeLabel.TabIndex = 0;
			this.timeLabel.Text = "Specify &Time Limit";
			this.timeLabel.Enter += new System.EventHandler(this.TimeLabel_Enter);
			// 
			// BlockTimerOptionsForm
			// 
			this.AcceptButton = this.startButton;
			this.AccessibleName = "BlockTimer Options Dialog";
			this.AccessibleRole = System.Windows.Forms.AccessibleRole.Dialog;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(276, 166);
			this.ControlBox = false;
			this.Controls.Add(this.groupBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(292, 205);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(292, 205);
			this.Name = "BlockTimerOptionsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "BlockTimer Options";
			this.TopMost = true;
			this.Shown += new System.EventHandler(this.BlockTimerOptionsForm_Shown);
			this.groupBox.ResumeLayout(false);
			this.groupBox.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox;
		private System.Windows.Forms.Label timeLabel;
		private System.Windows.Forms.Label confirmPasswordLabel;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.TextBox confirmPasswordBox;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button startButton;
		private System.Windows.Forms.Label errorLabel;
		public System.Windows.Forms.TextBox passwordBox;
		public System.Windows.Forms.ComboBox timeLimitComboBox;
		private System.Windows.Forms.Button quitButton;
	}
}

