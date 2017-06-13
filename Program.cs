using System;
using System.ComponentModel;
using static System.Console;
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
#if DEBUG
                WriteLine("Getting windows user.");
#endif
                WindowsUser user = new WindowsUser();

#if DEBUG
                WriteLine("Checking if user is an administrator.");
#endif
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
#if DEBUG
                    MessageBox.Show(
						$"{ex.Message}{NewLine}{NewLine}Inner Exception:{NewLine}{ex.InnerException}{NewLine}{NewLine}StackTrace:{NewLine}{ex.StackTrace}",
						"Plex Server Updater",
						MessageBoxButtons.OK,
						MessageBoxIcon.Stop);					
#else
                    MessageBox.Show(
                        ex.Message,
                        "Plex Server Updater",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
#endif
                }
				
				Exit(-1);
			}
			
			if (isSilent)
			{
				try
				{
#if DEBUG
                    WriteLine("Initializing a silent update.");
#endif
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
                try
                {
                    // Display the main form
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

#if DEBUG
                    WriteLine("Initializing the update window.");
#endif
                    MainForm mainForm = new MainForm();

                    // Check to see if the form is disposed becase there was an
                    // issue with initializing the form
                    if (!mainForm.IsDisposed)
                    {
#if DEBUG
                        WriteLine("Displaying the update window.");
#endif
                        Application.Run(mainForm);
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    MessageBox.Show(
                        $"{ex.Message}{NewLine}{NewLine}Inner Exception:{NewLine}{ex.InnerException}{NewLine}{NewLine}StackTrace:{NewLine}{ex.StackTrace}",
                        "Plex Server Updater",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
#else
                    MessageBox.Show(
                        ex.Message,
                        "Plex Server Updater",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
#endif
                }
            }
		}			
	}
}
