using System;
using System.Windows.Forms;

namespace BlockTimer
{
	internal static class BlockTimer
	{
		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			Application.Run(new BlockTimerForm());
		}
	}
}
