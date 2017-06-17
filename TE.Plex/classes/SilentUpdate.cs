using System;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using TE.LocalSystem;

namespace TE.Plex
{
    /// <summary>
    /// Executes a silent Plex Media Server update.
    /// </summary>
    public class SilentUpdate
    {
        #region Private Variables
        /// <summary>
        /// The media server object.
        /// </summary>
        private MediaServer server = null;
        /// <summary>
        /// The log file that contains the messages regarding the update.
        /// </summary>
        private string messageLogFile;
        /// <summary>
        /// Flag indicating that there was an issue writing a message to the
        /// message log file.
        /// </summary>
        private bool isMessageError;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes an instance of the <see cref="TE.Plex.SilentUpdate"/> class.
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
            if (isMessageError)
            {
                return;
            }

            if (server != null)
            {
                isMessageError = true;
                Log.Write("There was an issue connecting to the media server.");
            }

            if (!string.IsNullOrEmpty(messageLogFile))
            {
                isMessageError = true;
                Log.Write(
                    "The message log file was not specified. The installation log will still be written.");
            }

            try
            {
                using (StreamWriter sw =
                       new StreamWriter(messageLogFile, true))
                {
                    sw.WriteLine(message);
                    Log.Write(message);
                }
            }
            catch (UnauthorizedAccessException)
            {
                isMessageError = true;
                Log.Write(
                    $"Access to the message log file is denied.{NewLine}Message log path: {messageLogFile}");
            }
            catch (DirectoryNotFoundException)
            {
                isMessageError = true;
                Log.Write(
                    $"The message file directory could not be found.{NewLine}Message log path: {messageLogFile}");
            }
            catch (PathTooLongException)
            {
                isMessageError = true;
                Log.Write(
                    $"The message log path is too long. The total length of the path must be less than 260 characters.{NewLine}Message log path: {messageLogFile}");
            }
            catch (IOException)
            {
                isMessageError = true;
                Log.Write(
                    $"The message log path is invalid.{NewLine}Message log path: {messageLogFile}");
            }
            catch (System.Security.SecurityException)
            {
                isMessageError = true;
                Log.Write(
                    $"The user does not have the required permissions to write to the message file.{NewLine}Message log path: {messageLogFile}");
            }
            catch (Exception ex)
            {
                isMessageError = true;
                Log.Write(
                    $"An error occurred trying to write to the message log:{NewLine}{ex.Message}.{NewLine}Message log path: {messageLogFile}");
            }
        }
        #endregion

        #region Private Functions		
        /// <summary>
        /// Initializes the properties and variables for the class.
        /// </summary>
        private void Initialize()
        {
            try
            {
                server = new MediaServer(true);
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

            server.UpdateMessage +=
                new MediaServer.UpdateMessageHandler(ServerUpdateMessage);

            messageLogFile = server.GetMessageLogFilePath();
            isMessageError = (messageLogFile.Length > 0);

            if (!string.IsNullOrEmpty(messageLogFile))
            {
                // If the message log file exists, attempt to delete it
                if (File.Exists(messageLogFile))
                {
                    try
                    {
                        File.Delete(messageLogFile);
                    }
                    catch (PathTooLongException)
                    {
                        Log.Write(
                            $"The message log file path is too long.{NewLine}Message log path: {messageLogFile}");

                        isMessageError = false;
                        messageLogFile = string.Empty;
                    }
                    catch (IOException)
                    {
                        Log.Write(
                            $"The message log file is in use. The messages won't be written to the log file but the installation log will still be written.{NewLine}Message log path: {messageLogFile}");

                        isMessageError = false;
                        messageLogFile = string.Empty;
                    }
                    catch (NotSupportedException)
                    {
                        Log.Write(
                            $"The message log path is invalid.{NewLine}Message log path: {messageLogFile}");

                        isMessageError = false;
                        messageLogFile = string.Empty;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Log.Write(
                            $"The message log path cannot be accessed.{NewLine}Message log path: {messageLogFile}");

                        isMessageError = false;
                        messageLogFile = string.Empty;
                    }
                }
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
                if (server.IsUpdateAvailable())
                {
                    Log.Write("Update is available");
                    server.Update();
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
