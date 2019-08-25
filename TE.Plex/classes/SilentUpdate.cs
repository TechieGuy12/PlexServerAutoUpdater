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
        /// The log file that contains the messages regarding the update.
        /// </summary>
        private string _messageLogFile;

        /// <summary>
        /// Flag indicating that there was an issue writing a message to the
        /// message log file.
        /// </summary>
        private bool _isMessageError;

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
        public SilentUpdate()
        {
            Initialize();
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
            // If an error occurred when writing an update message to the log
            // file, just return from the function without trying again
            if (_isMessageError)
            {
                return;
            }

            if (_server != null)
            {
                _isMessageError = true;
                Log.Write("There was an issue connecting to the media server.");
            }

            if (!string.IsNullOrWhiteSpace(_messageLogFile))
            {
                _isMessageError = true;
                Log.Write(
                    "The message log file was not specified. The installation log will still be written.");
            }

            try
            {
                using (StreamWriter sw =
                       new StreamWriter(_messageLogFile, true))
                {
                    sw.WriteLine(message);
                    Log.Write(message);
                }
            }
            catch (UnauthorizedAccessException)
            {
                _isMessageError = true;
                Log.Write(
                    $"Access to the message log file is denied.{NewLine}Message log path: {_messageLogFile}");
            }
            catch (DirectoryNotFoundException)
            {
                _isMessageError = true;
                Log.Write(
                    $"The message file directory could not be found.{NewLine}Message log path: {_messageLogFile}");
            }
            catch (PathTooLongException)
            {
                _isMessageError = true;
                Log.Write(
                    $"The message log path is too long. The total length of the path must be less than 260 characters.{NewLine}Message log path: {_messageLogFile}");
            }
            catch (IOException)
            {
                _isMessageError = true;
                Log.Write(
                    $"The message log path is invalid.{NewLine}Message log path: {_messageLogFile}");
            }
            catch (System.Security.SecurityException)
            {
                _isMessageError = true;
                Log.Write(
                    $"The user does not have the required permissions to write to the message file.{NewLine}Message log path: {_messageLogFile}");
            }
            catch (Exception ex)
            {
                _isMessageError = true;
                Log.Write(
                    $"An error occurred trying to write to the message log:{NewLine}{ex.Message}.{NewLine}Message log path: {_messageLogFile}");
            }
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
        private void Initialize()
        {
            try
            {
                _server = new MediaServer(true);
                _timer = new Timer(DefaultWaitTime * 1000);
                _timer.Elapsed += OnTimedEvent;
                _timer.Enabled = false;
            }
            catch (AppNotInstalledException)
            {
                Log.Write(
                    "The Plex Media Server is not installed.");
                return;
            }
            catch (ServiceNotInstalledException)
            {
                Log.Write(
                    "The Plex Media Server service is not installed.");
                return;
            }
            catch (WindowsUserSidNotFound ex)
            {
                Log.Write(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                return;
            }

            _server.UpdateMessage +=
                new MediaServer.UpdateMessageHandler(ServerUpdateMessage);

            _messageLogFile = _server.GetMessageLogFilePath();
            _isMessageError = (_messageLogFile.Length > 0);

            if (!string.IsNullOrWhiteSpace(_messageLogFile))
            {
                // If the message log file exists, attempt to delete it
                if (File.Exists(_messageLogFile))
                {
                    try
                    {
                        File.Delete(_messageLogFile);
                    }
                    catch (PathTooLongException)
                    {
                        Log.Write(
                            $"The message log file path is too long.{NewLine}Message log path: {_messageLogFile}");

                        _isMessageError = false;
                        _messageLogFile = string.Empty;
                    }
                    catch (IOException)
                    {
                        Log.Write(
                            $"The message log file is in use. The messages won't be written to the log file but the installation log will still be written.{NewLine}Message log path: {_messageLogFile}");

                        _isMessageError = false;
                        _messageLogFile = string.Empty;
                    }
                    catch (NotSupportedException)
                    {
                        Log.Write(
                            $"The message log path is invalid.{NewLine}Message log path: {_messageLogFile}");

                        _isMessageError = false;
                        _messageLogFile = string.Empty;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Log.Write(
                            $"The message log path cannot be accessed.{NewLine}Message log path: {_messageLogFile}");

                        _isMessageError = false;
                        _messageLogFile = string.Empty;
                    }
                }
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
