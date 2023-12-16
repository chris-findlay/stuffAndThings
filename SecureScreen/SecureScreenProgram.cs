using System;
using System.Windows.Forms;

namespace SecureScreen
{
	internal static class SecureScreenProgram
	{
		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			Application.Run(new SecureScreenForm());
		}
	}
}
