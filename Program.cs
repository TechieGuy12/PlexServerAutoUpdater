using System;
using System.ComponentModel;
using static System.Console;
using static System.Environment;
using System.Runtime.InteropServices;
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
        /// The parent process.
        /// </summary>
        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// Attach to the console window.
        /// </summary>
        /// <param name="dwProcessId">
        /// The ID of the process.
        /// </param>
        /// <returns>
        /// True if successful, false if not successful.
        /// </returns>
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);

        /// <summary>
        /// Program entry point.
        /// </summary>
        [STAThread]
        private static int Main(string[] args)
        {
            // redirect console output to parent process;
            // must be before any calls to Console.WriteLine()
            AttachConsole(ATTACH_PARENT_PROCESS);

            Arguments arguments = new Arguments(args);
            

            bool isSilent = (arguments["silent"] != null);
            string logPath = arguments["log"];

            Log.SetFolder(logPath);
            Log.Delete();

            try
            {
                Log.Write("Getting windows user.");
                WindowsUser user = new WindowsUser();

                Log.Write("Checking if user is an administrator.");
                // Check if the user running this application is an administrator
                if (!user.IsAdministrator())
                {
                    string message = "This application must be run from an administrative account.";

                    if (!isSilent)
                    {
                        // If the user is not an administrator, then exit
                        MessageBox.Show(
                            message,
                            "Plex Server Updater",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Stop);
                    }

                    Log.Write(message);

                    ExitCode = ERROR_ACCESS_DENIED;
                    return ERROR_ACCESS_DENIED;
                }
            }
            catch (Exception ex)
            {
                if (!isSilent)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Plex Server Updater",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
                }

                Log.Write(ex);

                ExitCode = 1;
                return 1;
            }

            if (isSilent)
            {
                try
                {
                    bool isForceUpdate = (arguments["force"] != null);

                    int waitTime = SilentUpdate.DefaultWaitTime;
                    if (arguments["wait"] != null)
                    {
                        if (!Int32.TryParse(arguments["wait"], out waitTime))
                        {
                            waitTime = SilentUpdate.DefaultWaitTime;
                        }
                    }

                    // Run the update silently
                    Log.Write("Initializing the silent update.");
                    SilentUpdate silentUpdate = new SilentUpdate(Log.Folder);
                    silentUpdate.ForceUpdate = isForceUpdate;
                    silentUpdate.WaitTime = waitTime;
                    silentUpdate.Run();

                    if (silentUpdate.IsPlexRunning())
                    {
                        ExitCode = ERROR_SUCCESS;
                        return ERROR_SUCCESS;
                    }
                    else
                    {
                        ExitCode = 1;
                        return 1;
                    }
                }
                catch
                {
                    ExitCode = 1;
                    return 1;
                }
            }
            else
            {
                try
                {
                    // Display the main form
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    Log.Write("Initializing the update window.");
                    MainForm mainForm = new MainForm();

                    // Check to see if the form is disposed becase there was an
                    // issue with initializing the form
                    if (!mainForm.IsDisposed)
                    {
                        Log.Write("Displaying the update window.");
                        Application.Run(mainForm);
                    }

                    ExitCode = ERROR_SUCCESS;
                    return ERROR_SUCCESS;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Plex Server Updater",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);

                    Log.Write(ex);

                    ExitCode = 1;
                    return 1;
                }
            }
        }
    }
}
