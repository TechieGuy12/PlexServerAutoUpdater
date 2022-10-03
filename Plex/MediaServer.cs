using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Msi = TE.LocalSystem.Msi;
using TE.Plex.Update;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security;
using System.Configuration;

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
    public class MediaServer : EventSource
    {
        #region Delegates
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
        /// Occurs whenever the play count changes.
        /// </summary>
        public event PlayCountChangedHandler PlayCountChanged;

        /// <summary>
        /// Occurs whenever the in progress recording count changes.
        /// </summary>
        public event InProgressRecordingCountChangedHandler InProgressRecordingCountChanged;

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
        private static string DisplayName = ConfigurationManager.AppSettings["PlexServiceName"];
        /// <summary>
        /// The name of the Plex Media Server executable.
        /// </summary>
        private static string PlexExecutable = ConfigurationManager.AppSettings["PlexExecutable"];
        /// <summary>
        /// The name of the Plex updates folder.
        /// </summary>
        private static string PlexUpdatesFolder = ConfigurationManager.AppSettings["PlexUpdatesFolder"];
        /// <summary>
        /// The name of the installation packages folder.
        /// </summary>
        private static string PlexPackagesFolder = ConfigurationManager.AppSettings["PlexPackagesFolder"];
        /// <summary>
        /// Plex Media Server installation parameters.
        /// </summary>
        private static string PlexInstallParameters = ConfigurationManager.AppSettings["PlexInstallParameters"];
        /// <summary>
        /// Plex Media Server installation log subfolder.
        /// </summary>
        private static string PlexInstallLogFolder = ConfigurationManager.AppSettings["PlexInstallLogFolder"];
        /// <summary>
        /// Plex Media Server installation log file name.
        /// </summary>
        private static string PlexInstallLogFile = ConfigurationManager.AppSettings["PlexInstallLogFile"];
        /// <summary>
        /// The root Plex installation folder.
        /// </summary>
        private static string PlexFolder = "Plex";
        /// <summary>
        /// The subfolder in the Plex installation folder.
        /// </summary>
        private static string PlexSubFolder = "Plex Media Server";
        /// <summary>
        /// Maxiumum path length.
        /// </summary>
        private const int MaxPathSize = 256;
        #endregion

        #region Private Variables
        /// <summary>
        /// The user-specified log path.
        /// </summary>
        private string _logPath;

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
        public Package LatestInstallPackage { get; private set; }

        /// <summary>
        /// Gets the currently installed version of Plex Media Server.
        /// </summary>
        public Version CurrentVersion { get; private set; } = new Version(0, 0, 0, 0);

        /// <summary>
        /// Gets the latest downloaded version of Plex Media Server.
        /// </summary>
        public Version LatestVersion { get; private set; } = new Version(0, 0, 0, 0);

        /// <summary>
        /// Gets the update channel used to update Plex.
        /// </summary>
        public UpdateChannel UpdateChannel { get; private set; }

        /// <summary>
        /// Gets the string name of the update channel.
        /// </summary>
        public string UpdateChannelName {
            get
            {
                return UpdateChannel == UpdateChannel.PlexPass ? "Plex Pass" : "Public";
            }
        }

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
        /// Creates an instance of the <see cref="MediaServer"/> class.
        /// </summary>
        public MediaServer()
        {
            Initialize();
        }

        /// <summary>
        /// Creates an instance of the <see cref="MediaServer"/> class
        /// when provided with the <see cref="UpdateMessageHandler"/>.
        /// </summary>
        /// <param name="handler">
        /// The log message handler.
        /// </param>
        public MediaServer(MessageChangedEventHandler handler)
        {
            MessageChanged += handler ?? throw new ArgumentNullException(nameof(handler));
            Initialize();
        }

        /// <summary>
        /// Creates an instance of the <see cref="MediaServer"/> class
        /// when provided with the <see cref="UpdateMessageHandler"/>.
        /// </summary>
        /// <param name="logPath">
        /// Path to the installation log file.
        /// </param>
        /// <param name="handler">
        /// The log message handler.
        /// </param>
        public MediaServer(string logPath, MessageChangedEventHandler handler)
        {
            _logPath = logPath;
            MessageChanged += handler ?? throw new ArgumentNullException(nameof(handler));
            Initialize();
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
            OnMessageChanged(message);
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Gets the latest installation package download for the server.
        /// </summary>
        /// <returns>
        /// The full path to the latest installation package that has been
        /// downloaded or <c>null</c> if the package could not be determined.
        /// </returns>
        private Package GetLatestInstallPackage()
        {
            OnMessageChanged("Verify the updates folder is specified.");
            if (string.IsNullOrWhiteSpace(UpdatesFolder))
            {
                OnMessageChanged(
                    "The Plex updates folder was not specified.");
                return null;
            }

            OnMessageChanged($"Verify the updates folder, {UpdatesFolder} exists.");
            if (!Directory.Exists(UpdatesFolder))
            {
                OnMessageChanged(
                    $"The Plex updates folder, {UpdatesFolder} could not be found.");
                return null;
            }

            string token = plexRegistry.GetToken();
            if (token == null)
            {
                OnMessageChanged("Could not get the latest install package.");
                return null;
            }

            // Create the Plex installation package object
            Package availableVersion = new Package(
                UpdatesFolder, 
                UpdateChannel,
                token);
            availableVersion.MessageChanged += Message_Changed;

            if (availableVersion != null)
            {
                // Download the latest package for Plex
                bool result = availableVersion.Download().Result;
                if (!result)
                {
                    OnMessageChanged("The latest available installation could not be downloaded.");
                    return null;
                }
            }

            OnMessageChanged($"Latest packages file: {availableVersion.FilePath}");
            return availableVersion;
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
        private Version GetVersionFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return default;
            }

            try
            {
                FileVersionInfo version =
                    FileVersionInfo.GetVersionInfo(filePath);

                Version.TryParse(version.FileVersion, out Version convertedVersion);
                return convertedVersion;
            }
            catch (FileNotFoundException)
            {
                return default;
            }
        }

        /// <summary>
        /// Gets the Plex Media Server's installation path.
        /// </summary>
        /// <returns>
        /// The installation path of Plex Media Server.
        /// </returns>
        private string GetInstallPath()
        {
            string installPath = null;

            // To avoid using the Windows Installer API, let's first check well
            // known paths to find the Plex install location
            List<string> defaultLocations = new List<string>();
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrWhiteSpace(programFilesX86))
            {
                defaultLocations.Add(Path.Combine(programFilesX86, PlexFolder, PlexSubFolder));
            }
            
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                defaultLocations.Add(Path.Combine(programFiles, PlexFolder, PlexSubFolder));
            }

            foreach (string location in defaultLocations)
            {
                string path = Path.Combine(location, PlexExecutable);
                if (File.Exists(path))
                {
                    installPath = Path.GetDirectoryName(path);
                    break;
                }
            }

            // If the install path does not contain a value, then if the Plex
            // Media Server is running, let's try and get the path from the
            // process
            if (string.IsNullOrWhiteSpace(installPath))
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(PlexExecutable);
                    if (processes.Length > 0)
                    {
                        string filePath = processes[0].MainModule.FileName;
                        installPath = Path.GetDirectoryName(filePath);
                    }
                }
                catch (Exception)
                {
                    // Let's just swallow the execption as it isn't necessary
                    // since there is still another method of getting the
                    // the install path for Plex, so just set the path to 
                    // null
                    installPath = null;
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

            // If an install path has been found, then do one last check to
            // verify the directory exists before return the path
            if (!string.IsNullOrWhiteSpace(installPath))
            {
                // If the directory does not exist, then set the instsall path
                // to null as it doesn't appear to be valid
                if (!Directory.Exists(installPath))
                {
                    installPath = null;
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
            CurrentVersion = GetVersionFromFile(
                    Path.Combine(InstallFolder, PlexExecutable));

            // Get the latest Plex Media Server version that has been
            // downloaded
            LatestInstallPackage = GetLatestInstallPackage();
            if (LatestInstallPackage != null)
            {
                LatestVersion = LatestInstallPackage.GetVersion();
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
        private void Initialize()
        {
            if (IsInstalled())
            {
                throw new AppNotInstalledException(
                    "The Plex Media Server is not installed.");
            }

            InstallFolder = GetInstallPath();
            if (string.IsNullOrWhiteSpace(InstallFolder))
            {
                throw new AppNotInstalledException(
                    "Plex does not appear to be installed as the Plex installation folder could not be determined.");
            }
            OnMessageChanged($"Plex install folder: {InstallFolder}.");

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
                try
                {
                    LocalDataFolder = ConfigurationManager.AppSettings["PlexLocalAppDataFolder"];
                }
                catch
                {
                    LocalDataFolder = null;
                }

                if (string.IsNullOrWhiteSpace(LocalDataFolder))
                {
                    throw new PlexDataFolderNotFoundException(
                        "The Plex local application data folder could not be found for the Plex Windows account.");
                }
            }
            OnMessageChanged($"Plex local data folder: {LocalDataFolder}.");

            UpdatesFolder =
                Path.Combine(LocalDataFolder, PlexUpdatesFolder);
            OnMessageChanged($"Updates folder: {UpdatesFolder}.");

            UpdateChannel = plexRegistry.GeUpdateChannel();
            OnMessageChanged($"Update channel: {UpdateChannelName}.");

            GetVersions();
            OnMessageChanged($"Current version: {CurrentVersion}.");
            OnMessageChanged($"Latest version: {LatestVersion}.");

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
            if (!File.Exists(LatestInstallPackage.FilePath))
            {
                OnMessageChanged("The latest Plex installation package does not exist on the local machine.");
                return;
            }

            OnMessageChanged("Starting Plex installation.");            
            string logFile = GetInstallLogFilePath();
            if (string.IsNullOrWhiteSpace(logFile))
            {
                OnMessageChanged("The install log path could not be set. Aborting installation.");
                return;
            }

            OnMessageChanged("Delete any previous installation logs.");
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(
                LatestInstallPackage.FilePath,
                $"{PlexInstallParameters.Trim()} \"{logFile}\"");

            OnMessageChanged($"Run Plex installation - '{startInfo.FileName} {startInfo.Arguments}'.");
            using (Process install = Process.Start(startInfo))
            {
                install.WaitForExit();
            }
            OnMessageChanged("Plex install has finished.");
        }

        /// <summary>
        /// Stops all processes that match a specific name.
        /// </summary>
        /// <param name="processName">
        /// The name of the process to stop.
        /// </param>
        private void StopProcess(string processName)
        {
            OnMessageChanged($"Stopping {processName} processes.");

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
        /// The installation log file path, or <c>null</c> if the path could not
        /// be set.
        /// </returns>
        public string GetInstallLogFilePath()
        {
            try
            {
                OnMessageChanged("Setting the installation log path.");
                string defaultPath = Path.Combine(Path.GetTempPath(), PlexInstallLogFile);
                string logPath = defaultPath;
                if (!string.IsNullOrWhiteSpace(_logPath))
                {
                    try
                    {
                        logPath = Path.GetFullPath(_logPath);

                        string directory = Path.GetDirectoryName(logPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                    }
                    catch (Exception ex)
                    {
                        OnMessageChanged($"Could not use the specified log path '{_logPath}', so using default path of '{defaultPath}'. Reason: {ex.Message}");
                        logPath = defaultPath;
                    }
                }

                logPath = Path.Combine(logPath, PlexInstallLogFile);
                OnMessageChanged($"Installation log path: {logPath}.");

                return logPath;
            }
            catch (SecurityException)
            {
                OnMessageChanged("The user does not have permission to get the TEMP path.");
                return null;
            }
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
                OnMessageChanged("The token could not be found.");
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
                OnMessageChanged("The token could not be found.");
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

            OnMessageChanged("Stopping the Plex Media Server processes.");
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

            OnMessageChanged("START: Stopping the Plex service.");
            plexService.Stop();
            OnMessageChanged("END: Stopping the Plex service.");

            OnMessageChanged("START: Stopping the Plex Server processes.");
            StopProcesses();
            OnMessageChanged("END: Stopping the Plex Server processes.");

            try
            {
                OnMessageChanged($"START: Running update: {LatestInstallPackage}.");
                RunInstall();
                OnMessageChanged("END: Running update.");

                OnMessageChanged("START: Deleting Plex Run registry value.");
                plexRegistry.DeleteRunValue();
                OnMessageChanged("END: Deleting Plex Run registry value.");
                GetVersions();
            }
            catch
            {
                throw;
            }
            finally
            {
                OnMessageChanged("START: Stopping the Plex Server processes.");
                StopProcesses();
                OnMessageChanged("END: Stopping the Plex Server processes.");

                OnMessageChanged("START: Restarting the Plex service.");
                plexService.Start();
                OnMessageChanged("END: Restarting the Plex service.");
            }
        }
        #endregion
    }
}
