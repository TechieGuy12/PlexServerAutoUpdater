using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using TE.LocalSystem.Msi;

namespace TE.Plex
{
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
		#endregion
		
		#region Events
		/// <summary>
		/// Occurs whenever a message is created during the update.
		/// </summary>
		public event UpdateMessageHandler UpdateMessage;

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
        #endregion

        #region Private Constants		
        /// <summary>
        /// The DisplayName of the Plex Media Server installation.
        /// </summary>
        private const string DisplayName = "Plex Media Server";
		/// <summary>
		/// The root key for the users registry hive.
		/// </summary>
		private const string RegistryUsersRoot = "HKEY_USERS";
		/// <summary>
		/// The registry key tree for the Plex information.
		/// </summary>
		private const string RegistryPlexKey = @"\SOFTWARE\Plex, Inc.\Plex Media Server\";
		/// <summary>
		/// The registry run key that starts Plex Media Server at Windows
		/// startup.
		/// </summary>
		private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run\Plex Media Server";
		/// <summary>
		/// The name of the local Plex data path registry value.
		/// </summary>
		private const string RegistryPlexDataPathValueName = "LocalAppDataPath";
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
		/// Plex Media Server message log file name.
		/// </summary>
		private const string PlexMessageLogFile = "PlexMediaServerMessage.log";
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
		/// Gets or sets the flag indicating the installation is silent.
		/// </summary>
		public bool IsSilent { get; set; }
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
		/// when provided with the value indicating if the install is to be
		/// silent.
		/// </summary>		
		public MediaServer(bool isSilent)
		{
			Initialize(isSilent);
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
		/// Delete the Plex Server run keys for both the user that performed
		/// the installation, and the user associated with the Plex Service.
		/// </summary>
		private void DeleteRunKey()
		{
            OnUpdateMessage("Deleting Run keys in registry.");
			
			try
			{
				// Delete the run keys from the registry for the current user
				Registry.CurrentUser.DeleteSubKeyTree(RegistryRunKey);
				
				if (!string.IsNullOrEmpty(serviceUserSid))
				{
					// Delete the run keys from the registry for the user
					// associated with the Plex service
					Registry.Users.DeleteSubKeyTree(
                        $"{serviceUserSid}\\{RegistryRunKey}");
				}
			}
			catch (ArgumentException)
			{
                OnUpdateMessage("The run key in the registry doesn't have a valid subkey.");
			}
			catch (IOException)
			{
                OnUpdateMessage("Couldn't delete the run key from the registry because there was an I/O problem.");
			}
			catch (System.Security.SecurityException)
			{
                OnUpdateMessage("The user does not have permission to delete the run key in the registry.");
			}
			catch (UnauthorizedAccessException)
			{
                OnUpdateMessage("The user does not have the necessary registry rights.");
			}
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
            Log.Write($"Verify the updates folder is specified.");
			if (string.IsNullOrEmpty(UpdatesFolder))
			{
                OnUpdateMessage(
					"The Plex updates folder was not specified.");
				return string.Empty;
			}

            Log.Write($"Verify the updates folder, {UpdatesFolder} exists.");
            if (!Directory.Exists(UpdatesFolder))
			{
                OnUpdateMessage(
					$"The Plex updates folder, {UpdatesFolder} could not be found.");
				return string.Empty;
			}

            Log.Write("Checking to see if updates folder exists.");
			// Check to see if at least one update folder is available
			if (!Directory.EnumerateFileSystemEntries(UpdatesFolder).Any())
			{
                Log.Write("Updates folder does not exist. Looks like a new install.");
                return string.Empty;
			}

            Log.Write("Getting the latest update folder.");
            DirectoryInfo latestFolder =
				new DirectoryInfo(UpdatesFolder).GetDirectories()
					.OrderByDescending(d=>d.LastWriteTimeUtc).FirstOrDefault();

            if (latestFolder == null)
            {
                Log.Write("Couldn't get the latest update folder.");
                return string.Empty;
            }

            Log.Write("Checking for the latest Plex packages folder.");
            string packagesFullPath = 
				Path.Combine(latestFolder.FullName, PlexPackagesFolder);
		
			if (!Directory.Exists(packagesFullPath))
			{
                OnUpdateMessage(
					$"The latest Plex packages folder {packagesFullPath} could not be found.");
				return string.Empty;
			}

			DirectoryInfo packagesFolder = new DirectoryInfo(packagesFullPath);

            Log.Write("Get the latest packages file.");
            FileInfo file = packagesFolder.GetFiles()
				.OrderByDescending(f => f.LastWriteTime)
				.FirstOrDefault();
		
            if (file == null)
            {
                Log.Write("Couldn't get the latest packages file.");
                return string.Empty;
            }

            Log.Write($"Latest packages file: {file.FullName}");
			return file.FullName;
		}
		
		/// <summary>
		/// Gets the local Plex data folder used by the Plex service.
		/// </summary>
		/// <returns>
		/// The full path to the local Plex data folder.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// The Plex service ID could not be found.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		/// The local data folder could not be found.
		/// </exception>
		private string GetLocalDataFolder()
		{
			// Get the unique user SID for the Plex service user
			serviceUserSid = plexService.LogOnUser.Sid;
			
			if (string.IsNullOrEmpty(serviceUserSid))
			{
				throw new InvalidOperationException(
					"The Plex service user ID could not be found.");
			}
			
			string folder;
			
			try
			{
                // Get the Plex local data folder from the users registry hive
                // for the user ID associated with the Plex service
                Log.Write("Get the local data folder for Plex.");
				folder = Registry.GetValue(
					$"{RegistryUsersRoot}\\{serviceUserSid}{RegistryPlexKey}", 
					RegistryPlexDataPathValueName, 
					string.Empty).ToString();
			}
			catch
			{
				folder = string.Empty;
			}
			
			if (string.IsNullOrEmpty(folder))
			{
                // Default to the standard local application data folder
                // for the Plex service user is the LocalAppDataPath value
                // is missing from the registry
                Log.Write(
                    "Couldn't find the local data folder in the registry, so defaulting to the logged in user's local application data folder.");
                folder = plexService.LogOnUser.LocalAppDataFolder;
			}

            Log.Write($"Plex local data folder: {folder}");
            return folder;
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
			string installPath = Api.GetComponentPathByFile(PlexExecutable);
			
			if (!string.IsNullOrEmpty(installPath))
			{
				// Verify the path length does not exceed the allowable
				// length of the operating system
				if (installPath.Length < MaxPathSize)
				{
					installPath = Path.GetDirectoryName(installPath);					
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
			try
			{
				plexService = new ServerService();
			}
			catch
			{
				throw;
			}
			
			if (plexService == null)
			{
				throw new InvalidOperationException(
					"The Plex service could not be found.");
			}
			
			// Get the Plex folders
			LocalDataFolder = GetLocalDataFolder();
			
			if (string.IsNullOrEmpty(LocalDataFolder))
			{
				throw new PlexDataFolderNotFoundException(
					"The Plex local application data folder could not be found for the Plex Windows account.");
			}
			
			UpdatesFolder = 
				Path.Combine(LocalDataFolder, PlexUpdatesFolder);
			
			GetVersions();
		}
		
		/// <summary>
		/// Checks to see if Plex Media Server is installed.
		/// </summary>
		/// <returns>
		/// True if it is installed, false if it isn't.
		/// </returns>
		private bool IsInstalled()
		{
			return ((InstalledProduct.Enumerate()
			         .Where(product=>product.DisplayName == DisplayName)).Any());
		}
		
		/// <summary>
		/// Run the Plex Media Server installation.
		/// </summary>
		private void RunInstall()
		{
            Log.Write("Staring Plex installation.");
            Log.Write("Delete any previous installation logs.");
			string logFile = GetInstallLogFilePath();
			if (File.Exists(logFile))
			{
				File.Delete(logFile);
			}
						
			ProcessStartInfo startInfo = new ProcessStartInfo(
				LatestInstallPackage,
				PlexInstallParameters + logFile);

            Log.Write("Run Plex installation.");
            using (Process install = Process.Start(startInfo))
            {
                install.WaitForExit();
            }
            Log.Write("Plex install has finished.");
		}
		
		/// <summary>
		/// Stops all processes that match a specific name.
		/// </summary>
		/// <param name="processName">
		/// The name of the process to stop.
		/// </param>
		private void StopProcess(string processName)
		{
            Log.Write($"Stopping {processName} processes.");
			foreach (Process proc in Process.GetProcessesByName(processName))
			{
				proc.Kill();
				proc.WaitForExit();
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
            Log.Write("Setting the installation log path.");
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
				catch (IOException)
				{
                    
                    installLogFolder = Path.GetTempPath();
				}
				catch (UnauthorizedAccessException)
				{
					installLogFolder = Path.GetTempPath();
				}
				catch (NotSupportedException)
				{
					installLogFolder = Path.GetTempPath();
				}
			}

            string logPath = Path.Combine(installLogFolder, PlexInstallLogFile);
            Log.Write($"Installation log path: {logPath}.");

            return logPath;
		}
		
		/// <summary>
		/// Gets the full path to the message log file.
		/// </summary>
		/// <returns>
		/// The message log file path.
		/// </returns>
		public string GetMessageLogFilePath()
		{
			// Create the log file path
			string logFolder = Path.Combine(
				Environment.GetFolderPath(
					Environment.SpecialFolder.CommonApplicationData),
				PlexInstallLogFolder);
			
			return Path.Combine(logFolder, PlexMessageLogFile);
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
				"Plex Media Scanner",
				"PlexDlnaServer",
				"PlexNewTranscoder",
				"PlexScriptHost",
				"PlexTranscoder"
			};

            Log.Write("Stopping the Plex Media Server processes.");
 			for (int i = 0; i <= processes.Length-1; i++) 
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
