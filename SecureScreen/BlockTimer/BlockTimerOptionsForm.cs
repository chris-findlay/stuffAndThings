using System;
using System.Windows.Forms;

namespace BlockTimer
{
	public partial class BlockTimerOptionsForm : Form
	{
		public BlockTimerOptionsForm()
		{
			InitializeComponent();
			ValidatePasswords();
		}


		public bool FirstRun { private get; set; }


		private void TimeLabel_Enter(object sender, EventArgs e)
		{
			timeLimitComboBox.Focus();
		}

		private void PasswordLabel_Enter(object sender, EventArgs e)
		{
			passwordBox.Focus();
		}

		private void ConfirmPasswordLabel_Enter(object sender, EventArgs e)
		{
			confirmPasswordBox.Focus();
		}


		private void OnBlur(object sender, EventArgs e)
		{
			ValidatePasswords();
		}

		private void OnKeyPress(object sender, KeyPressEventArgs e)
		{
			ValidatePasswords();
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			ValidatePasswords();
		}

		private void ValidatePasswords()
		{
			if (!TimeSpan.TryParse("00:" + timeLimitComboBox.Text, out _)) {
				ShowError("Invalid Time Limit!");
			} else if (passwordBox.Text == "") {
				ShowError("Need a password!");
			} else if (passwordBox.Text != confirmPasswordBox.Text) {
				ShowError("Passwords must match!");
			} else {
				HideError();
			}
		}

		private void ShowError(string message)
		{
			errorLabel.Text = "Error: " + message;
			errorLabel.Visible = true;
			startButton.Enabled = false;
			quitButton.Enabled = confirmPasswordBox.Enabled;
		}

		private void HideError()
		{
			errorLabel.Visible = false;
			startButton.Enabled = true;
			quitButton.Enabled = true;
		}


		private void BlockTimerOptionsForm_Shown(object sender, EventArgs e)
		{
			confirmPasswordLabel.Enabled = confirmPasswordBox.Enabled = FirstRun;
			confirmPasswordBox.Visible = confirmPasswordLabel.Visible = FirstRun;

			cancelButton.Enabled = !FirstRun;
			startButton.Text = FirstRun
				? "Start"
				: "Restart";


			passwordBox.Text = "";
			ValidatePasswords();
		}
	}
}
