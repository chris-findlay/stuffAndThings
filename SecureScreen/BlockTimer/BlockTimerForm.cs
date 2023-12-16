using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BlockTimer
{
	public partial class BlockTimerForm : Form
	{
		private const int WM_NCLBUTTONDOWN = 0xA1;

		private const int HT_CAPTION = 0x2;


		private static readonly (FormBorderStyle style, int width, int height, int dW, int dH) Params = (FormBorderStyle.None, 313, 126, 24, 18);


		private readonly Dictionary<TimeSpan, (int? x, int? y, int width, int? height, double opacity)> _sizes =
			new Dictionary<TimeSpan, (int? x, int? y, int width, int? height, double opacity)>();

		private readonly bool _resizing;
		private readonly BlockTimerOptionsForm _options = new BlockTimerOptionsForm();

		private DateTime _starTime;
		private TimeSpan _time;
		private double? _opacity;

		private int _minWidth = 90;


		public BlockTimerForm()
		{
			_resizing = true;
			Hide();

			InitializeComponent();
			FormClosing += OnFormClosing;

			FormBorderStyle = Params.style;
			SetBounds(200, 200, Params.width, Params.height, BoundsSpecified.Location | BoundsSpecified.Size);
			_resizing = false;
			Show();

			Start(true);
		}


		private void InitSizes(int screenWidth, int screenHeight)
		{
			const string MIN_WIDTH_PREFIX = "MIN-WIDTH = ";

			_sizes.Clear();

			var lines = File.ReadAllLines("sizing.txt")
				.Where(Enumerable.Any)
				.Where(l => l[0] != '#')
				.ToList();

			var minWidthLine = lines.FirstOrDefault(l => l.StartsWith(MIN_WIDTH_PREFIX, StringComparison.OrdinalIgnoreCase));
			_minWidth = int.TryParse(
				minWidthLine
					?.Substring(MIN_WIDTH_PREFIX.Length),
				out var min
			)
				? min
				: _minWidth;

			lines
				.Where(l => l != minWidthLine)
				.Select(
					l => l.Replace(',', ' ')
						.Split(" \t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
						.Select(
							(f, ix) => double.TryParse(f, out var n)
								? n
								: (double?)null
						)
						.ToList()
				)
				.ToList()
				.ForEach(
					a => _sizes.Add(
// ReSharper disable PossibleInvalidOperationException
						TimeSpan.FromSeconds(a[0].Value),
						(
							a[1].HasValue
								? 0
								: (int?)null,
							a[2].HasValue
								? 0
								: (int?)null,
							(int)(screenWidth * a[3].Value),
							a[4].HasValue
								? screenHeight
								: (int?)null,
							a[5].Value
						)
// ReSharper restore PossibleInvalidOperationException
					)
				);
		}


		private void OnFormResize(object sender, EventArgs e)
		{
			if (_resizing) {
				return;
			}


			timeLabel.Font = new Font(
				timeLabel.Font.FontFamily,
				Math.Max(
					Math.Min(
						72.0f * (Width - Params.dW) / (Params.width - Params.dW),
						72.0f * (Height - Params.dH) / (Params.height - Params.dH)
					),
					val2: 1
				),
				FontStyle.Bold,
				GraphicsUnit.Point
			);
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			var value = _time - (DateTime.Now - _starTime);
			timeLabel.Text = value.ToString(@"mm\:ss");
			timeLabel.ForeColor = value < TimeSpan.Zero
				? Color.Red
				: SystemColors.ControlText;

			SetWindowSize(value);
		}

		private void SetWindowSize(TimeSpan value)
		{
			var previous = _sizes.First();
			var next = _sizes.First();
			foreach (var pair in _sizes) {
				previous = next;
				next = pair;
				if (pair.Key > value) {
					break;
				}
			}

			var result = Lerp(previous, value, next);

			SetBounds(result.x, result.y, result.width, result.height, BoundsSpecified.Location | BoundsSpecified.Size);

			Opacity = Lerp(
				Opacity,
				_opacity.HasValue
					? 0.8
					: 0.2,
				_opacity ?? result.opacity
			);
		}

		private (int x, int y, int width, int height, double opacity) Lerp(
			KeyValuePair<TimeSpan, (int? x, int? y, int width, int? height, double opacity)> previous,
			TimeSpan value,
			KeyValuePair<TimeSpan, (int? x, int? y, int width, int? height, double opacity)> next
		)
		{
			if (next.Key == previous.Key) {
				return (next.Value.x ?? Left, next.Value.y ?? Top, next.Value.width, (int)(next.Value.height ?? next.Value.width * 0.4), next.Value.opacity);
			}


			var t = (value - previous.Key).TotalSeconds / (next.Key - previous.Key).TotalSeconds;

			var x = Lerp(previous.Value.x ?? Left, t, next.Value.x ?? Left);
			var y = Lerp(previous.Value.y ?? Top, t, next.Value.y ?? Top);
			var width = Lerp(Math.Max(previous.Value.width, _minWidth), t, Math.Max(next.Value.width, _minWidth));
			var height = (int)Lerp(previous.Value.height ?? width * 0.4, t, next.Value.height ?? width * 0.4);
			var opacity = Lerp(previous.Value.opacity, t, next.Value.opacity);

			var screenBounds = Screen.FromControl(this).Bounds;
			var over = screenBounds.Width - (x + width);
			if (over < 0) {
				x += over;
			}

			if (x < screenBounds.Left) {
				x = screenBounds.Left;
			}

			over = screenBounds.Height - (y + height);
			if (over < 0) {
				y += over;
			}

			if (y < screenBounds.Top) {
				y = screenBounds.Top;
			}

			return (x, y, width, height, opacity);
		}

		private static int Lerp(int a, double t, int b)
		{
			return (int)(a + (b - a) * t);
		}

		private static double Lerp(double a, double t, double b)
		{
			return a + (b - a) * t;
		}

		private void OnClick(object sender, EventArgs e)
		{
			var mouseArgs = e as MouseEventArgs;
			if (mouseArgs?.Button == MouseButtons.Right) {
				timer.Stop();
				Start(false);
				return;
			}

			_starTime -= TimeSpan.FromSeconds(1);
		}

		private void Start(bool firstRun)
		{
			bool ok;
			do {
				_options.FirstRun = firstRun;
				var result = _options.ShowDialog();
				if (result != DialogResult.OK) {
					if (result == DialogResult.Abort) {
						Environment.Exit(0);
					}

					return;
				}


				ok = TimeSpan.TryParse("00:" + _options.timeLimitComboBox.Text, out _time);
			} while (!ok);

			var screenBounds = Screen.FromControl(this).Bounds;
			InitSizes(screenBounds.Width, screenBounds.Height);

			_starTime = DateTime.Now;
			timer.Start();
		}

		private void OnFormMouseEnter(object sender, EventArgs e)
		{
			_opacity = 1.0;
		}

		private void OnFormMouseLeave(object sender, EventArgs e)
		{
			_opacity = null;
		}

		private void OnFormMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left) {
				Capture = false;
				timeLabel.Capture = false;

				var msg = Message.Create(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
				WndProc(ref msg);
			}
		}

		private static void OnFormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
		}
	}
}
