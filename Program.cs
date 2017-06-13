using System;
using System.ComponentModel;
using static System.Environment;
using System.Windows.Forms;
using TE.LocalSystem;
using TE;
using static TE.SystemExitCodes;

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
					
					Exit(ERROR_ACCESS_DENIED);
				}				
			}
			catch (Exception ex)
			{
				if (!isSilent)
				{
					MessageBox.Show(
						$"{ex.Message}{NewLine}Inner Exception:{NewLine}{ex.InnerException}",
						"Plex Server Updater",
						MessageBoxButtons.OK,
						MessageBoxIcon.Stop);					
				}
				
				Exit(-1);
			}
			
			if (isSilent)
			{
				try
				{
					// Run the update silently
					SilentUpdate silentUpdate = new SilentUpdate();
					silentUpdate.Run();
					Exit(ERROR_SUCCESS);
					
				}
				catch
				{
					Exit(-1);
				}
			}
			else
			{
				// Display the main form
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				
				MainForm mainForm = new MainForm();
				
				// Check to see if the form is disposed becase there was an
				// issue with initializing the form
				if (!mainForm.IsDisposed)
				{
					Application.Run(mainForm);
				}
			}
		}			
	}
}
