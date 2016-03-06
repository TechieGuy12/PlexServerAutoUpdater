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
		public delegate void UpdateMessageHandler(string message);
		#endregion
		
		#region Events
		/// <summary>
		/// Occurs whenever a message is created during the update.
		/// </summary>
		public event UpdateMessageHandler UpdateMessage;
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
		private const string PlexUpdatesFolder = @"Plex Media Server\Updates\";
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
		private const string PlexInstallLogFolder = @"PlexUpdater\";
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
		/// The SID for the Plex service log on user.
		/// </summary>
		private string serviceUserSid;
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
			this.Initialize(false);
		}
		
		/// <summary>
		/// Creates an instance of the <see cref="TE.Plex.MediaServer"/> class
		/// when provided with the value indicating if the install is to be
		/// silent.
		/// </summary>		
		public MediaServer(bool isSilent)
		{
			this.Initialize(isSilent);
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
			UpdateMessage("Deleting Run keys in registry.");
			
			try
			{
				// Delete the run keys from the registry for the current user
				Registry.CurrentUser.DeleteSubKeyTree(RegistryRunKey);
				
				if (!string.IsNullOrEmpty(serviceUserSid))
				{
					// Delete the run keys from the registry for the user
					// associated with the Plex service
					Registry.Users.DeleteSubKeyTree(serviceUserSid + @"\" + RegistryRunKey);
				}
			}
			catch (ArgumentException)
			{
				UpdateMessage("The run key in the registry doesn't have a valid subkey.");
			}
			catch (IOException)
			{
				UpdateMessage("Couldn't delete the run key from the registry because there was an I/O problem.");
			}
			catch (System.Security.SecurityException)
			{
				UpdateMessage("The user does not have permission to delete the run key in the registry.");
			}
			catch (UnauthorizedAccessException)
			{
				UpdateMessage("The user does not have the necessary registry rights.");
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
					fileVersion = parts[0] + "." + parts[1];
					
					if (parts[2].Length > 2)
					{
						fileVersion += "." + parts[2].Substring(0, 2);
						fileVersion += "." + parts[2].Substring(2);
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
			if (string.IsNullOrEmpty(this.UpdatesFolder))
			{
				this.UpdateMessage(
					"The Plex updates folder was not specified.");
				return string.Empty;
			}
			
			if (!Directory.Exists(this.UpdatesFolder))
			{
				this.UpdateMessage(
					"The Plex updates folder, " + this.UpdatesFolder + " could not be found.");
				return string.Empty;
			}
			
			// Check to see if at least one update folder is available
			if (!Directory.EnumerateFileSystemEntries(this.UpdatesFolder).Any())
			{
				return string.Empty;
			}

			DirectoryInfo latestFolder =
				new DirectoryInfo(this.UpdatesFolder).GetDirectories()
					.OrderByDescending(d=>d.LastWriteTimeUtc).First();

			string packagesFullPath = latestFolder.FullName;
			if (!packagesFullPath.EndsWith(@"\", StringComparison.OrdinalIgnoreCase))
			{
				packagesFullPath += @"\";
			}
			packagesFullPath += PlexPackagesFolder + @"\";
		
			if (!Directory.Exists(packagesFullPath))
			{
				this.UpdateMessage(
					"The latest Plex packages folder " + packagesFullPath + " could not be found.");
				return string.Empty;
			}
			
			DirectoryInfo packagesFolder = new DirectoryInfo(packagesFullPath);
			
			FileInfo file = packagesFolder.GetFiles()
         		.OrderByDescending(f => f.LastWriteTime)
         		.First();
		
			return file.FullName;
		}
		
		/// <summary>
		/// Gets the local Plex data folder used by the Plex service.
		/// </summary>
		/// <param name="service">
		/// The Plex service object.
		/// </param>
		/// <returns>
		/// The full path to the local Plex data folder.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// The Plex service ID could not be found.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		/// The local data folder could not be found.
		/// </exception>
		private string GetLocalDataFolder(ServerService service)
		{
			// Get the unique user SID for the Plex service user
			serviceUserSid = service.LogOnUser.GetSid();
			
			if (string.IsNullOrEmpty(serviceUserSid))
			{
				throw new InvalidOperationException(
					"The Plex service user ID could not be found.");
			}
			
			// Get the Plex local data folder from the users registry hive
			// for the user ID associated with the Plex service
			string folder = Registry.GetValue(
				RegistryUsersRoot + @"\" + serviceUserSid + RegistryPlexKey, 
				RegistryPlexDataPathValueName, 
				string.Empty).ToString();
			
			if (string.IsNullOrEmpty(folder))
			{
				throw new DirectoryNotFoundException(
					"The Plex local data folder could not be determined.");
			}
			
			// Append a slash character to the end of the folder path if
			// one doesn't exist
			if (!folder.EndsWith(@"\", StringComparison.OrdinalIgnoreCase))
			{
				folder += @"\";
			}
			
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
			
			Match match = Regex.Match(fileName, @"\d+.\d+.\d+");		
			return this.FormatFileNameVersion(match.Groups[0].Value);
		}
		
		/// <summary>
		/// Gets the Plex Media Server's installation path.
		/// </summary>
		/// <returns>
		/// The installation path of Plex Media Server.
		/// </returns>
		/// <exception cref=" TE.Plex.AppNotInstalledException">
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
					
					if (!installPath.EndsWith(
						@"\",
						StringComparison.OrdinalIgnoreCase))
					{
						installPath += @"\";
					}
				}
			}
			
			return installPath;
		}
		
		/// <summary>
		/// Initializes the <see cref="TE.Plex.MediaServer"/> class.
		/// </summary>
		/// <exception cref="TE.Plex.AppNotInstalledException">
		/// The Plex Media Server is not installed.
		/// </exception>
		/// <exception cref="TE.Plex.ServiceNotInstalledException">
		/// The Plex Media Server service is not installed.
		/// </exception>
		private void Initialize(bool isSilent)
		{
			if (this.IsInstalled())
			{
				throw new AppNotInstalledException(
					"The Plex Media Server is not installed.");
			}
			
			this.IsSilent = isSilent;
			this.CurrentVersion = new Version(0, 0, 0, 0);
			this.LatestVersion = new Version(0, 0, 0, 0);
			this.InstallFolder = this.GetInstallPath();
					
			// Populate a service object with information about the Plex
			// service
			ServerService service = new ServerService();
			
			// Get the Plex folders
			this.LocalDataFolder = GetLocalDataFolder(service);
			this.UpdatesFolder = this.LocalDataFolder + PlexUpdatesFolder;
			
			// Get the currently installed Plex Media Server version
			this.CurrentVersion = this.ConvertFromStringToVersion(
				this.GetVersionFromFile(this.InstallFolder + PlexExecutable));
			
			// Get the latest Plex Media Server version that has been 
			// downloaded
			this.LatestInstallPackage = this.GetLatestInstallPackage();	
			if (!string.IsNullOrEmpty(this.LatestInstallPackage))
			{
				this.LatestVersion = this.ConvertFromStringToVersion(
					this.GetVersionFromFileName(
						Path.GetFileName(this.LatestInstallPackage)));
			}
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
			string logFile = this.GetInstallLogFilePath();
			if (File.Exists(logFile))
			{
				File.Delete(logFile);
			}
						
			ProcessStartInfo startInfo = new ProcessStartInfo(
				this.LatestInstallPackage,
				PlexInstallParameters + logFile);
			
			Process install = Process.Start(startInfo);
			install.WaitForExit();
		}
		
		/// <summary>
		/// Stops all processes that match a specific name.
		/// </summary>
		/// <param name="processName">
		/// The name of the process to stop.
		/// </param>
		private void StopProcess(string processName)
		{
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
			string logFolder = Environment.GetFolderPath(
					Environment.SpecialFolder.CommonApplicationData);
			
			if (!logFolder.EndsWith(@"\", StringComparison.OrdinalIgnoreCase))
			{
				logFolder += @"\";
			}
			
			string installLogFolder = logFolder + PlexInstallLogFolder;
			
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
			
			if (!installLogFolder.EndsWith(@"\", StringComparison.OrdinalIgnoreCase))
			{
				installLogFolder += @"\";
			}
			
			return  installLogFolder + PlexInstallLogFile;
		}
		
		/// <summary>
		/// Gets the full path to the message log file.
		/// </summary>
		/// <returns>
		/// The message log file path.
		/// </returns>
		public string GetMessageLogFilePath()
		{
			string logFolder = Environment.GetFolderPath(
					Environment.SpecialFolder.CommonApplicationData);
			
			if (!logFolder.EndsWith(@"\", StringComparison.OrdinalIgnoreCase))
			{
				logFolder += @"\";
			}
			
			return logFolder + PlexInstallLogFolder + PlexMessageLogFile;			
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
			return (this.CurrentVersion.CompareTo(this.LatestVersion) < 0);
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
						
 			for (int i = 0; i <= processes.Length-1; i++) 
 			{ 
 				this.StopProcess(processes[i]);
 			} 			
		}
		
		/// <summary>
		/// Performs the Plex Media Server update.
		/// </summary>
		public void Update()
		{
			ServerService service = new ServerService();
			
			this.UpdateMessage("START: Stopping the Plex service.");
			service.Stop();
			this.UpdateMessage("END: Stopping the Plex service.");
				
			this.UpdateMessage("START: Stopping the Plex Server processes.");
			this.StopProcesses();
			this.UpdateMessage("END: Stopping the Plex Server processes.");
			
			this.UpdateMessage("START: Running update: " + this.LatestInstallPackage + ".");
			this.RunInstall();
			this.UpdateMessage("END: Running update.");
			
			this.UpdateMessage("START: Stopping the Plex Server processes.");
			this.StopProcesses();
			this.UpdateMessage("END: Stopping the Plex Server processes.");
			
			this.UpdateMessage("START: Restarting the Plex service.");
			service.Start();
			this.UpdateMessage("END: Restarting the Plex service.");			
		}
		#endregion
	}
}
