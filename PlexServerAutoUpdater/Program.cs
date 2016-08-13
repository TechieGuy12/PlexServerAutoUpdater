using System;
using System.ComponentModel;
using System.Windows.Forms;
using TE.LocalSystem;
using TE;

namespace TE.Plex
{
	/// <summary>
	/// Class with program entry point.
	/// </summary>
	internal sealed class Program
	{				
		/// <summary>
		/// Program entry point.
		/// </summary>
		[STAThread]
		private static void Main(string[] args)
		{
			Arguments arguments = new Arguments(args);
			
			bool isSilent = (arguments["silent"] != null);
			
			try
			{
				WindowsUser user = new WindowsUser();

				// Check if the user running this application is an administrator
				if (!user.IsAdministrator())
				{
					if (!isSilent)
					{
						// If the user is not an administrator, then exit
						MessageBox.Show(
							"This application must be run from an administrative account.",
							"Plex Server Updater",
							MessageBoxButtons.OK,
							MessageBoxIcon.Stop);
					}
					
					Environment.Exit(SystemExitCodes.ERROR_ACCESS_DENIED);
				}				
			}
			catch (Win32Exception ex)
			{
				if (!isSilent)
				{
					MessageBox.Show(
						"Couldn't get Windows user information.\n\n" + ex.Message,
						"Plex Server Updater",
						MessageBoxButtons.OK,
						MessageBoxIcon.Stop);					
				}
				
				Environment.Exit(ex.NativeErrorCode);
			}
			
			if (isSilent)
			{
				try
				{
					// Run the update silently
					SilentUpdate silentUpdate = new SilentUpdate();
					silentUpdate.Run();
					Environment.Exit(SystemExitCodes.ERROR_SUCCESS);
					
				}
				catch
				{
					Environment.Exit(-1);
				}
			}
			else
			{
				// Display the main form
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new MainForm());
			}
		}			
	}
}
