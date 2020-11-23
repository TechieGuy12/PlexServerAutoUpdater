using System;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Timers;
using TE.LocalSystem;

namespace TE.Plex
{
    /// <summary>
    /// Executes a silent Plex Media Server update.
    /// </summary>
    public class SilentUpdate
    {
        #region Constants
        /// <summary>
        /// The default wait time in seconds.
        /// </summary>
        public const int DefaultWaitTime = 30;
        #endregion

        #region Private Variables
        /// <summary>
        /// The media server object.
        /// </summary>
        private MediaServer _server = null;

        /// <summary>
        /// The wait timer.
        /// </summary>
        private Timer _timer = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the flag indicating that the update is forced to be
        /// installed regardless if any item is currently being played.
        /// </summary>
        public bool ForceUpdate { get; set; } = false;

        /// <summary>
        /// Gets or sets the default wait time in seconds.
        /// </summary>
        public int WaitTime { get; set; } = DefaultWaitTime;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes an instance of the <see cref="SilentUpdate"/> class.
        /// </summary>
        /// <exception cref="AppNotInstalledException">
        /// Plex is not installed.
        /// </exception>
        /// <exception cref="ServiceNotInstalledException">
        /// The Plex service is not installed.
        /// </exception>
        /// <exception cref="WindowsUserSidNotFound">
        /// The Windows user SID is not found.
        /// </exception>
        public SilentUpdate()
        {
            Initialize(null);
        }

        /// <summary>
        /// Initializes an instance of the <see cref="SilentUpdate"/> class.
        /// </summary>
        /// <param name="logPath">
        /// Specifies a path to the installation log.
        /// </param>
        /// <exception cref="AppNotInstalledException">
        /// Plex is not installed.
        /// </exception>
        /// <exception cref="ServiceNotInstalledException">
        /// The Plex service is not installed.
        /// </exception>
        /// <exception cref="WindowsUserSidNotFound">
        /// The Windows user SID is not found.
        /// </exception>
        public SilentUpdate(string logPath)
        {
            Initialize(logPath);
        }
        #endregion

        #region Events
        /// <summary>
        /// Writes any messages from the Plex Media Server update to a log
        /// file.
        /// </summary>
        /// <param name="sender">
        /// The sender object.
        /// </param>
        /// <param name="message">
        /// The message to write to the log file.
        /// </param>
        private void ServerUpdateMessage(object sender, string message)
        {
            Log.Write(message);
        }

        /// <summary>
        /// The timer has elapsed so check the play count to see if the server
        /// is in use.
        /// </summary>
        /// <param name="source">
        /// The sender.
        /// </param>
        /// <param name="e"
        /// Elapsed event arguments.
        /// ></param>
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (_server == null)
            {
                _timer.Enabled = false;
                return;
            }

            if (CheckIfCanUpdate())
            {
                PerformUpdate();
            }
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Checks to see if the server can be updated at this time.
        /// </summary>
        private bool CheckIfCanUpdate()
        {
            if (_server == null)
            {
                Log.Write("The server was not specified. Cannot perform the update.");
                _timer.Enabled = false;
                return false;
            }

            int playCount = _server.GetPlayCount();
            int inProgressRecordingCount = _server.GetInProgressRecordingCount();

            // No item is currently being played
            if (playCount == 0 && inProgressRecordingCount == 0)
            {
                Log.Write("The server is not in use continuing to perform the update.");
                _timer.Enabled = false;
                return true;
            }
            // At least one item is being played
            else if (playCount > 0 || inProgressRecordingCount > 0)
            {
                if (!ForceUpdate)
                {
                    Log.Write("The server is in use. Waiting for all media and/or in progress recordings to be stopped before performing the update.");
                    _timer.Interval =
                        Convert.ToDouble(Math.Abs(WaitTime) * 1000);
                    _timer.Enabled = true;
                    return false;
                }
                else if (ForceUpdate && inProgressRecordingCount > 0)
                {
                    Log.Write("The server cannot be forcefully updated while there is a recording in progress.  Waiting for all in progress recordings to be stopped before performing the update.");
                    _timer.Interval =
                        Convert.ToDouble(Math.Abs(WaitTime) * 1000);
                    _timer.Enabled = true;
                    return false;
                }
                else
                {
                    Log.Write("The update is set to be force. The update will continue.");
                    _timer.Enabled = false;
                    return true;
                }
            }
            // Could not determine how many items are being played
            else
            {
                Log.Write("The server in use status could not be determined. The server can be updated if you wish.");
                _timer.Enabled = false;
                return true;
            }
        }

        /// <summary>
        /// Initializes the properties and variables for the class.
        /// </summary>
        /// <exception cref="AppNotInstalledException">
        /// Plex is not installed.
        /// </exception>
        /// <exception cref="ServiceNotInstalledException">
        /// The Plex service is not installed.
        /// </exception>
        /// <exception cref="WindowsUserSidNotFound">
        /// The Windows user SID is not found.
        /// </exception>
        private void Initialize(string logPath)
        {
            try
            {                
                _server = new MediaServer(logPath, ServerUpdateMessage);
                _timer = new Timer(DefaultWaitTime * 1000);
                _timer.Elapsed += OnTimedEvent;
                _timer.Enabled = false;
            }
            catch (AppNotInstalledException)
            {
                Log.Write(
                    "The Plex Media Server is not installed.");
                throw;
            }
            catch (ServiceNotInstalledException)
            {
                Log.Write(
                    "The Plex Media Server service is not installed.");
                throw;
            }
            catch (WindowsUserSidNotFound ex)
            {
                Log.Write(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                throw;
            }
        }

        /// <summary>
        /// Perform the server update.
        /// </summary>
        private void PerformUpdate()
        {
            try
            {
                Log.Write("Update is available");
                _server.Update();
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Gets the value indicating that the Plex server is running.
        /// </summary>
        /// <returns>
        /// True if the Plex server is running, false if the server is not
        /// running.
        /// </returns>
        public bool IsPlexRunning() 
        {
            return _server.IsRunning();
        }
        /// <summary>
        /// Runs the Plex Media Server update.
        /// </summary>
        public void Run()
        {
            try
            {               
                Log.Write("Checking for server update.");
                if (_server.IsUpdateAvailable())
                {
                    if (CheckIfCanUpdate())
                    {
                        Log.Write("Update is available");
                        _server.Update();
                    }
                }
                else
                {
                    Log.Write("No update is available. Exiting.");                
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
        #endregion
    }
}
