using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Microsoft.Win32;

namespace SecureScreen
{
	public partial class SecureScreenForm : Form
	{
		const int SAFE_TIMEOUT = 20 * 60;
		const int LOCK_TIMEOUT = 60;
		const string REG_PATH = @"Control Panel\Desktop";
		const string REG_VALUE = "ScreenSaverIsSecure";

		const int SPI_GET_SCREENSAVER_TIMEOUT = 0x0E;
		const int SPI_SET_SCREENSAVER_TIMEOUT = 0x0F;
		const int SPI_GET_SCREENSAVER_ACTIVE = 0x10;
		const int SPI_SET_SCREENSAVER_ACTIVE = 0x11;


		const int SPIF_SEND_WIN_INI_CHANGE = 0x2;

		const int WM_NCLBUTTONDOWN = 0xA1;

		const int HT_CAPTION = 0x2;
		bool _potentialClick;


		public SecureScreenForm()
		{
			InitializeComponent();
			ReleaseDisplay(ignoreFirstState: true);
		}


		int ScreenSaverTimeoutSeconds {
			get {
				int result = 0;

				SystemParametersInfo(SPI_GET_SCREENSAVER_TIMEOUT, 0, ref result, 0)
					.GetLastError(this, $"GetScreenSaverTimeout: {result}");

				return result;
			}
			set {
				SystemParametersInfo(SPI_SET_SCREENSAVER_TIMEOUT, 0, ref value, SPIF_SEND_WIN_INI_CHANGE)
					.GetLastError(this, $"SetScreenSaverTimeout: {value}");
			}
		}

		bool IsSafe {
			get => ScreenSaverTimeoutSeconds == SAFE_TIMEOUT && "1".Equals((string)Registry.CurrentUser.OpenSubKey(REG_PATH)?.GetValue(REG_VALUE));
			set {
				//NOTE: Do reg first so SPIF_SEND_WIN_INI_CHANGE catches both changes.
				var subKey = Registry.CurrentUser.OpenSubKey(REG_PATH, writable: true);
				subKey?.SetValue(REG_VALUE, value ? "0" : "1", RegistryValueKind.String);

				ScreenSaverTimeoutSeconds = value
					? SAFE_TIMEOUT
					: LOCK_TIMEOUT;
			}
		}

		void OnShown(object sender, EventArgs e)
		{
			Size = new Size(32, 18);
			Location = new Point(Location.X - Size.Width / 2, Location.Y);
		}


		void OnMouseEnter(object sender, EventArgs e)
		{
			BackColor = Color.Magenta;
			SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.DisplayRequired);
			SetScreenSaverActive(false);
		}

		void OnMouseLeave(object sender, EventArgs e)
		{
			ShowState();
			ReleaseDisplay();
		}

		void ShowState()
		{
			BackColor = IsSafe
				? Color.Blue
				: SystemColors.ControlDarkDark;
		}

		void ToggleState()
		{
			IsSafe = !IsSafe;
			ShowState();
		}


		void OnMouseDown(object sender, MouseEventArgs e)
		{
			switch (e.Button) {
				case MouseButtons.Left:
					Capture = false;
					var msg = Message.Create(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
					WndProc(ref msg);
					break;

				case MouseButtons.Middle:
					Close();
					break;

				default:
					_potentialClick = true;
					break;
			}
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (_potentialClick) {
				ToggleState();
				_potentialClick = false;
			}
		}

		void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (_potentialClick) {
				ReleaseDisplay();

				LockWorkStation()
					.GetLastError(this, "LockWorkStation");

				_potentialClick = false;
			}
		}


		void OnLocationChanged(object sender, EventArgs e)
		{
			var screen = Screen.GetBounds(this);
			var quantised = new Point(
				Location.X - (Location.X + (Size.Width >> 1)) % Size.Width,
				(Location.Y > screen.Height >> 1)
					? screen.Height - Size.Height
					: 0
			);
			if (Location != quantised) {
				Location = quantised;
			}
		}


		void OnClosing(object sender, FormClosingEventArgs e)
		{
			ReleaseDisplay();
		}


		void ReleaseDisplay(bool ignoreFirstState = false)
		{
			SetThreadExecutionState(ExecutionState.Continuous);
			SetScreenSaverActive(true, ignoreFirstState);
		}


		void SetScreenSaverActive(bool active, bool ignoreFirstState = false)
		{
			int result = 0;

			SystemParametersInfo(SPI_GET_SCREENSAVER_ACTIVE, 0, ref result, 0)
				.GetLastError(this, $"GetScreenSaver: {result} :: {active}");

			if ((result != 0) == active && !ignoreFirstState) {
				//MessageBox.Show(this, "GetScreenSaver::already set to " + active);
				BackColor = Color.Yellow;
			}

			SystemParametersInfo(SPI_SET_SCREENSAVER_ACTIVE, active ? 1 : 0, ref result, SPIF_SEND_WIN_INI_CHANGE)
				.GetLastError(this, "SetScreenSaver:" + active, 0x149);

			SystemParametersInfo(SPI_GET_SCREENSAVER_ACTIVE, 0, ref result, 0)
				.GetLastError(this, $"GetScreenSaver: {result} :: {active}");
			if ((result != 0) != active)
			{
				//MessageBox.Show(this, "SetScreenSaver::failed to set to " + active);
				BackColor = Color.Red;
			}
		}


		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool LockWorkStation();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int flags);
	}


	[Flags]
	public enum ExecutionState : uint
	{
		//SystemRequired	= 0x00000001,
		DisplayRequired		= 0x00000002,
		//AwayModeRequired	= 0x00000040,
		Continuous			= 0x80000000
	}


	public static class GetLastErrorExtension
	{
		public static void GetLastError(this bool value, IWin32Window form, string message, params int[] ignoredErrorCodes)
		{
			if (!value) {
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (!ignoredErrorCodes.Contains(lastWin32Error)) {
					MessageBox.Show(form, $"{message}: {lastWin32Error:X8}");
				}
			}
		}
	}
}
