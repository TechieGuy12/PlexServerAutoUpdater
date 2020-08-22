using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Msi = TE.LocalSystem.Msi;
using TE.Plex.Update;
using System.Net.Http;
using System.Threading.Tasks;

namespace TE.Plex
{
    /// <summary>
    /// The type of installation package.
    /// </summary>
    public enum UpdateChannel
    {
        /// <summary>
        /// Public released package.
        /// </summary>
        Public = 0,
        /// <summary>
        /// Plex Pass-only package.
        /// </summary>
        PlexPass = 8
    }

    /// <summary>
    /// Description of MediaServer.
    /// </summary>
    public class MediaServer
    {
        #region Delegates
        /// <summary>
        /// The delegate for the update message event.
        /// </summary>
        /// <param name="message">
        /// The message about the update.
        /// </param>
        public delegate void UpdateMessageHandler(object sender, string message);

        /// <summary>
        /// The delegate for the play count changed event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="playCount">
        /// The last play count value.
        /// </param>
        public delegate void PlayCountChangedHandler(object sender, int playCount);

        /// <summary>
        /// The delegate for the in progress recording count changed event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="inProgressRecordingCount">
        /// The last in progress recording count value.
        /// </param>
        public delegate void InProgressRecordingCountChangedHandler(object sender, int inProgressRecordingCount);
        #endregion

        #region Events
        /// <summary>
        /// Occurs whenever a message is created during the update.
        /// </summary>
        public event UpdateMessageHandler UpdateMessage;

        /// <summary>
        /// Occurs whenever the play count changes.
        /// </summary>
        public event PlayCountChangedHandler PlayCountChanged;

        /// <summary>
        /// Occurs whenever the in progress recording count changes.
        /// </summary>
        public event InProgressRecordingCountChangedHandler InProgressRecordingCountChanged;

        /// <summary>
        /// Invoke the UpdateMessage event; called whenever a message is
        /// updated.
        /// </summary>
        /// <param name="message">
        /// The updated message.
        /// </param>
        protected virtual void OnUpdateMessage(string message)
        {
            UpdateMessage?.Invoke(this, message);
        }

        /// <summary>
        /// Invoke the PlayCountChanged event; called whenever the play count
        /// changes.
        /// </summary>
        /// <param name="playCount">
        /// The latest play count value;
        /// </param>
        protected virtual void OnPlayCountChanged(int playCount)
        {
            PlayCountChanged?.Invoke(this, playCount);
        }

        /// <summary>
        /// Invoke the InProgressRecordingCountChanged event; called whenever the in progress recording count changes.
        /// </summary>
        /// <param name="inProgressRecordingCount">
        /// The latest in progress recording count value;
        /// </param>
        protected virtual void OnInProgressRecordingCountChanged(int inProgressRecordingCount)
        {
            InProgressRecordingCountChanged?.Invoke(this, inProgressRecordingCount);
        }
        #endregion

        #region Private Constants
        /// <summary>
        /// The DisplayName of the Plex Media Server installation.
        /// </summary>
        private const string DisplayName = "Plex Media Server";
        /// <summary>
        /// The name of the Plex Media Server executable.
        /// </summary>
        private const string PlexExecutable = "Plex Media Server.exe";
        /// <summary>
        /// The name of the Plex updates folder.
        /// </summary>
        private const string PlexUpdatesFolder = @"Plex Media Server\Updates";
        /// <summary>
        /// The name of the installation packages folder.
        /// </summary>
        private const string PlexPackagesFolder = "packages";
        /// <summary>
        /// Plex Media Server installation parameters.
        /// </summary>
        private const string PlexInstallParameters = "/install /quiet /norestart /log ";
        /// <summary>
        /// Plex Media Server installation log subfolder.
        /// </summary>
        private const string PlexInstallLogFolder = @"PlexUpdater";
        /// <summary>
        /// Plex Media Server installation log file name.
        /// </summary>
        private const string PlexInstallLogFile = "PlexMediaServerInstall.log";
        /// <summary>
        /// Maxiumum path length.
        /// </summary>
        private const int MaxPathSize = 256;
        #endregion

        #region Private Variables
        /// <summary>
        /// The SID of the Plex service user.
        /// </summary>
        private string serviceUserSid;

        /// <summary>
        /// The Plex service.
        /// </summary>
        private ServerService plexService = null;

        /// <summary>
        /// The registry settings for Plex.
        /// </summary>
        private Registry plexRegistry = null;

        /// <summary>
        /// The HTTP client used to connect to the Plex website.
        /// </summary>
        private HttpClient httpClient = new HttpClient();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the path to the Plex Media Server data folder.
        /// </summary>
        public string LocalDataFolder { get; private set; }

        /// <summary>
        /// Gets the path to the Plex Media Server updates folder.
        /// </summary>
        public string UpdatesFolder { get; private set; }

        /// <summary>
        /// Gets the path to the Plex Media Server installation.
        /// </summary>
        public string InstallFolder { get; private set; }

        /// <summary>
        /// Gets the full path to the latest installation package that has
        /// been downloaded.
        /// </summary>
        public string LatestInstallPackage { get; private set; }

        /// <summary>
        /// Gets the currently installed version of Plex Media Server.
        /// </summary>
        public Version CurrentVersion { get; private set; }

        /// <summary>
        /// Gets the latest downloaded version of Plex Media Server.
        /// </summary>
        public Version LatestVersion { get; private set; }

        /// <summary>
        /// Gets the update channel used to update Plex.
        /// </summary>
        public UpdateChannel UpdateChannel { get; private set; }

        /// <summary>
        /// Gets or sets the flag indicating the installation is silent.
        /// </summary>
        public bool IsSilent { get; set; }

        /// <summary>
        /// Gets the current play count from the server.
        /// </summary>
        public int PlayCount { get; private set; }

        /// <summary>
        /// Gets the current in progress recording count from the server.
        /// </summary>
        public int InProgressRecordingCount { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an instance of the <see cref="TE.Plex.MediaServer"/> class.
        /// </summary>
        public MediaServer()
        {
            Initialize(false);
        }

        /// <summary>
        /// Creates an instance of the <see cref="TE.Plex.MediaServer"/> class
        /// when provided with the <see cref="UpdateMessageHandler"/>.
        /// </summary>
        public MediaServer(UpdateMessageHandler handler)
        {
            UpdateMessage += handler;
            Initialize(false);
        }

        /// <summary>
        /// Creates an instance of the <see cref="TE.Plex.MediaServer"/> class
        /// when provided with the value indicating if the install is to be
        /// silent.
        /// </summary>
        public MediaServer(bool isSilent)
        {
            Initialize(isSilent);
        }

        /// <summary>
        /// Creates an instance of the <see cref="TE.Plex.MediaServer"/> class
        /// when provided with the value indicating if the install is to be
        /// silent and the <see cref="UpdateMessageHandler"/>.
        /// </summary>
        public MediaServer(bool isSilent, UpdateMessageHandler handler)
        {
            UpdateMessage += handler;
            Initialize(isSilent);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Writes messages to the log.
        /// </summary>
        /// <param name="sender">
        /// The object that triggered the event.
        /// </param>
        /// <param name="message">
        /// The message to write to the log.
        /// </param>
        private void Message_Changed(object sender, string message)
        {
            OnUpdateMessage(message);
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Converts the version number from a string to a <see cref="System.Version"/>
        /// object.
        /// </summary>
        /// <param name="version">
        /// The string version to convert.
        /// </param>
        /// <returns>
        /// A <see cref="System.Version"/> object of the version, or null if
        /// the conversion failed.
        /// </returns>
        private Version ConvertFromStringToVersion(string version)
        {
            Version converted = null;
            Version.TryParse(version, out converted);
            return converted;
        }

        /// <summary>
        /// Format the version number retrieved from the Plex update file.
        /// </summary>
        /// <param name="version">
        /// The version to format.
        /// </param>
        /// <returns>
        /// The formatted version of the Plex update file.
        /// </returns>
        private string FormatFileNameVersion(string version)
        {
            string fileVersion = string.Empty;

            if (string.IsNullOrEmpty(version))
            {
                return fileVersion;
            }

            string[] parts = version.Split('.');

            if (parts.Length > 0)
            {
                try
                {
                    fileVersion = $"{parts[0]}.{parts[1]}";

                    if (parts[2].Length > 2)
                    {
                        fileVersion += $".{parts[2].Substring(0, 2)}";
                        fileVersion += $".{parts[2].Substring(2)}";
                    }
                    else
                    {
                        fileVersion += parts[2];
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    return fileVersion;
                }
            }

            return fileVersion;
        }

        /// <summary>
        /// Gets the latest installation package download for the server.
        /// </summary>
        /// <returns>
        /// The full path to the latest installation package that has been
        /// downloaded.
        /// </returns>
        private string GetLatestInstallPackage()
        {
            if (UpdateMessage == null)
            {
                Log.Write("UpdateMessage is null.");
            }
            OnUpdateMessage($"Verify the updates folder is specified.");
            if (string.IsNullOrEmpty(UpdatesFolder))
            {
                OnUpdateMessage(
                    "The Plex updates folder was not specified.");
                return string.Empty;
            }

            // Get the unique user SID for the Plex service user
            serviceUserSid = plexService.LogOnUser.Sid;

            string token = plexRegistry.GetToken();
            if (token == null)
            {
                OnUpdateMessage("Could not get the latest install package.");
                return string.Empty;
            }

            Package availableVersion = new Package(
                LocalDataFolder, 
                UpdateChannel,
                token);
            availableVersion.MessageChanged += Message_Changed;

            if (availableVersion != null)
            {
                bool result = availableVersion.Download().Result;
                if (!result)
                {
                    OnUpdateMessage("The latest available installation could not be downloaded.");
                }
            }

            OnUpdateMessage($"Verify the updates folder, {UpdatesFolder} exists.");
            if (!Directory.Exists(UpdatesFolder))
            {
                OnUpdateMessage(
                    $"The Plex updates folder, {UpdatesFolder} could not be found.");
                return string.Empty;
            }

            OnUpdateMessage("Checking to see if updates folder exists.");
            if (!Directory.EnumerateFileSystemEntries(UpdatesFolder).Any())
            {
                OnUpdateMessage("Updates folder does not exist. Looks like a new install.");
                return string.Empty;
            }

            OnUpdateMessage("Getting the latest update folder.");
            DirectoryInfo latestFolder =
                new DirectoryInfo(UpdatesFolder).GetDirectories()
                    .OrderByDescending(d => d.LastWriteTimeUtc).FirstOrDefault();

            if (latestFolder == null)
            {
                OnUpdateMessage("Couldn't get the latest update folder.");
                return string.Empty;
            }

            OnUpdateMessage("Checking for the latest Plex packages folder.");
            string packagesFullPath =
                Path.Combine(latestFolder.FullName, PlexPackagesFolder);

            if (!Directory.Exists(packagesFullPath))
            {
                OnUpdateMessage(
                    $"The latest Plex packages folder {packagesFullPath} could not be found.");
                return string.Empty;
            }

            DirectoryInfo packagesFolder = new DirectoryInfo(packagesFullPath);

            OnUpdateMessage("Get the latest packages file.");
            FileInfo file = packagesFolder.GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (file == null)
            {
                OnUpdateMessage("Couldn't get the latest packages file.");
                return string.Empty;
            }

            OnUpdateMessage($"Latest packages file: {file.FullName}");
            return file.FullName;
        }

        /// <summary>
        /// Gets the version number of a file.
        /// </summary>
        /// <param name="filePath">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The version number of the file, or an empty string if the version
        /// could not be retrieved.
        /// </returns>
        private string GetVersionFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }

            try
            {
                FileVersionInfo version =
                    FileVersionInfo.GetVersionInfo(filePath);
                return version.FileVersion;
            }
            catch (FileNotFoundException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the version number from a file name.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file.
        /// </param>
        /// <returns>
        /// The version number from the name of the file, or a blank string if
        /// the version number could not be determined.
        /// </returns>
        private string GetVersionFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            Match match = Regex.Match(fileName, @"\d+.\d+.\d+.\d+");
            return match.Groups[0].Value;
        }

        /// <summary>
        /// Gets the Plex Media Server's installation path.
        /// </summary>
        /// <returns>
        /// The installation path of Plex Media Server.
        /// </returns>
        /// <exception cref="AppNotInstalledException">
        /// The Plex Media Server is not installed.
        /// </exception>
        private string GetInstallPath()
        {
            string installPath = null;

            // To avoid using the Windows Installer API, let's first check well
            // known paths to find the Plex install location
            List<string> defaultLocations = new List<string>();
            defaultLocations.Add(@"C:\Program Files (x86)\Plex\Plex Media Server");
            defaultLocations.Add(@"C:\Program Files\Plex\Plex Media Server");

            foreach (string location in defaultLocations)
            {
                string path = Path.Combine(location, PlexExecutable);
                if (File.Exists(path))
                {
                    installPath = path;
                    break;
                }
            }

            // If the install path does not contain a value, then use the
            // Windows Installer API to find the Plex server install path
            if (string.IsNullOrWhiteSpace(installPath))
            {
                installPath = Msi.Api.GetComponentPathByFile(PlexExecutable);

                if (!string.IsNullOrWhiteSpace(installPath))
                {
                    // Verify the path length does not exceed the allowable
                    // length of the operating system
                    if (installPath.Length < MaxPathSize)
                    {
                        installPath = Path.GetDirectoryName(installPath);
                    }
                }
            }

            return installPath;
        }

        /// <summary>
        /// Gets the current installed version and latest update version of
        /// the Plex media server.
        /// </summary>
        private void GetVersions()
        {
            // Get the currently installed Plex Media Server version
            CurrentVersion = ConvertFromStringToVersion(
                GetVersionFromFile(
                    Path.Combine(InstallFolder, PlexExecutable)));

            // Get the latest Plex Media Server version that has been
            // downloaded
            LatestInstallPackage = GetLatestInstallPackage();
            if (!string.IsNullOrEmpty(LatestInstallPackage))
            {
                LatestVersion = ConvertFromStringToVersion(
                    GetVersionFromFileName(
                        Path.GetFileName(LatestInstallPackage)));
            }
            else
            {
                // Default the latest version to the currently installed
                // version if an update doesn't exist
                LatestVersion = CurrentVersion;
            }
        }

        /// <summary>
        /// Initializes the <see cref="MediaServer"/> class.
        /// </summary>
        /// <exception cref="AppNotInstalledException">
        /// The Plex Media Server is not installed.
        /// </exception>
        /// <exception cref="ServiceNotInstalledException">
        /// The Plex Media Server service is not installed.
        /// </exception>
        /// <exception cref="PlexDataFolderNotFoundException">
        /// The Plex Media Server service local applicaiton data folder could
        /// not be found.
        /// </exception>
        private void Initialize(bool isSilent)
        {
            if (IsInstalled())
            {
                throw new AppNotInstalledException(
                    "The Plex Media Server is not installed.");
            }

            IsSilent = isSilent;
            CurrentVersion = new Version(0, 0, 0, 0);
            LatestVersion = new Version(0, 0, 0, 0);
            InstallFolder = GetInstallPath();

            // Populate a service object with information about the Plex
            // service
            plexService = new ServerService();
            if (plexService == null)
            {
                throw new InvalidOperationException(
                    "The Plex service could not be found.");
            }

            plexRegistry = new Registry(plexService.LogOnUser);

            // Get the Plex folders
            LocalDataFolder = plexRegistry.GetLocalDataFolder();
            if (string.IsNullOrEmpty(LocalDataFolder))
            {
                throw new PlexDataFolderNotFoundException(
                    "The Plex local application data folder could not be found for the Plex Windows account.");
            }            

            UpdatesFolder =
                Path.Combine(LocalDataFolder, PlexUpdatesFolder);

            UpdateChannel = plexRegistry.GeUpdateChannel();

            GetVersions();

            PlayCount = GetPlayCount();
            InProgressRecordingCount = GetInProgressRecordingCount();
        }

        /// <summary>
        /// Checks to see if Plex Media Server is installed.
        /// </summary>
        /// <returns>
        /// True if it is installed, false if it isn't.
        /// </returns>
        private bool IsInstalled()
        {
            return ((Msi.InstalledProduct.Enumerate()
                     .Where(product => product.DisplayName == DisplayName)).Any());
        }
       
        /// <summary>
        /// Run the Plex Media Server installation.
        /// </summary>
        private void RunInstall()
        {
            OnUpdateMessage("Starting Plex installation.");
            OnUpdateMessage("Delete any previous installation logs.");
            string logFile = GetInstallLogFilePath();
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(
                LatestInstallPackage,
                PlexInstallParameters + logFile);

            OnUpdateMessage("Run Plex installation.");
            using (Process install = Process.Start(startInfo))
            {
                install.WaitForExit();
            }
            OnUpdateMessage("Plex install has finished.");
        }

        /// <summary>
        /// Stops all processes that match a specific name.
        /// </summary>
        /// <param name="processName">
        /// The name of the process to stop.
        /// </param>
        private void StopProcess(string processName)
        {
            OnUpdateMessage($"Stopping {processName} processes.");

            // Drop the extension from the filename to get the process without
            // using the file extension
            string fileName = Path.GetFileNameWithoutExtension(processName);

            Process[] processes = Process.GetProcessesByName(fileName);
            foreach (Process proc in processes)
            {
                proc.Kill();
                proc.WaitForExit();
            }

            // Check to see if any processes are still running
            processes = Process.GetProcessesByName(processName);
            if (processes.Count() > 0)
            {
                // Use the nuclear way of killing the processes
                foreach (Process proc in processes)
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = "taskkill.exe",
                            Arguments = $" /IM {processName} /F"
                        };
                        process.Start();
                        process.WaitForExit();
                    }
                }
            }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Gets the full path to the installation log file.
        /// </summary>
        /// <returns>
        /// The installation log file path.
        /// </returns>
        public string GetInstallLogFilePath()
        {
            OnUpdateMessage("Setting the installation log path.");
            string logFolder = Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData);

            string installLogFolder =
                Path.Combine(logFolder, PlexInstallLogFolder);

            if (!Directory.Exists(installLogFolder))
            {
                try
                {
                    Directory.CreateDirectory(installLogFolder);
                }
                catch (Exception ex)
                    when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
                {
                    installLogFolder = Path.GetTempPath();
                }
            }

            string logPath = Path.Combine(installLogFolder, PlexInstallLogFile);
            OnUpdateMessage($"Installation log path: {logPath}.");

            return logPath;
        }

        /// <summary>
        /// Gets the current play count on the Plex server.
        /// </summary>
        /// <returns>
        /// The current play count or -1 if the count could not be determined.
        /// </returns>
        public int GetPlayCount()
        {
            int playCount = Api.Unknown;
            string token = plexRegistry.GetToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                OnUpdateMessage("The token could not be found.");
                return playCount;
            }

            Api plexApi = new Api("localhost", token);
            plexApi.MessageChanged += Message_Changed;

            playCount = plexApi.GetPlayCount();
            OnPlayCountChanged(playCount);

            return playCount;
        }

        /// <summary>
        /// Gets the number of in progress recordings (i.e. by the DVR) on the Plex server.
        /// </summary>
        /// <returns>
        /// The current in progress recording count or -1 if the count could not be determined.
        /// </returns>
        public int GetInProgressRecordingCount()
        {
            int inProgressRecordingCount = Api.Unknown;
            string token = plexRegistry.GetToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                OnUpdateMessage("The token could not be found.");
                return inProgressRecordingCount;
            }

            Api plexApi = new Api("localhost", token);
            plexApi.MessageChanged += Message_Changed;

            inProgressRecordingCount = plexApi.GetInProgressRecordingCount();
            OnInProgressRecordingCountChanged(inProgressRecordingCount);

            return inProgressRecordingCount;
        }

        /// <summary>
        /// Gets the value indicating if the Plex server is running.
        /// </summary>
        /// <returns>
        /// True if the Plex server is running, false if the Plex server is not
        /// running.
        /// </returns>
        public bool IsRunning()
        {
            try
            {
                HttpResponseMessage checkingResponse = 
                    httpClient.GetAsync("http://localhost:32400/web/index.html").GetAwaiter().GetResult();
                return checkingResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
                when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return false;
            }
        }
        /// <summary>
        /// Indicates if a new update is available.
        /// </summary>
        /// <returns>
        /// True if an update is available, or false if there is no update
        /// available.
        /// </returns>
        public bool IsUpdateAvailable()
        {
            return (CurrentVersion.CompareTo(LatestVersion) < 0);
        }

        /// <summary>
        /// Stops all the Plex Media Server processes.
        /// </summary>
        public void StopProcesses()
        {
            string[] processes =
            {
                "Plex Media Server.exe",
                "Plex Media Scanner.exe",
                "Plex Tuner Service.exe",
                "PlexDlnaServer.exe",
                "PlexNewTranscoder.exe",
                "PlexScriptHost.exe",
                "PlexTranscoder.exe"
            };

            OnUpdateMessage("Stopping the Plex Media Server processes.");
            for (int i = 0; i <= processes.Length - 1; i++)
            {
                StopProcess(processes[i]);
            }
        }

        /// <summary>
        /// Performs the Plex Media Server update.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when an update could not be completed.
        /// </exception>
        public void Update()
        {
            if (plexService == null)
            {
                throw new InvalidOperationException(
                    "The Plex service was not found.");
            }

            OnUpdateMessage("START: Stopping the Plex service.");
            plexService.Stop();
            OnUpdateMessage("END: Stopping the Plex service.");

            OnUpdateMessage("START: Stopping the Plex Server processes.");
            StopProcesses();
            OnUpdateMessage("END: Stopping the Plex Server processes.");

            try
            {
                OnUpdateMessage($"START: Running update: {LatestInstallPackage}.");
                RunInstall();
                OnUpdateMessage("END: Running update.");

                OnUpdateMessage("START: Deleting Plex Run registry value.");
                plexRegistry.DeleteRunValue();
                OnUpdateMessage("END: Deleting Plex Run registry value.");
                GetVersions();
            }
            catch
            {
                throw;
            }
            finally
            {
                OnUpdateMessage("START: Stopping the Plex Server processes.");
                StopProcesses();
                OnUpdateMessage("END: Stopping the Plex Server processes.");

                OnUpdateMessage("START: Restarting the Plex service.");
                plexService.Start();
                OnUpdateMessage("END: Restarting the Plex service.");
            }
        }
        #endregion
    }
}
