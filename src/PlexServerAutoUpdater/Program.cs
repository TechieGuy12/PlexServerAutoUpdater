using System;
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
			
			if (isSilent)
			{
				try
				{
					MediaServer server = new MediaServer(true);
					
					if (server.IsUpdateAvailable())
					{						
						server.Update();
					}
				}
				catch
				{
					Environment.Exit(-1);
				}
			}
			else
			{
				// Display the main form if the user is an administrator
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new MainForm());
			}
		}
		
	}
}
