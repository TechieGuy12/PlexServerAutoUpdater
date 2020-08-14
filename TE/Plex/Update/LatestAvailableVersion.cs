using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace TE.Plex.Update
{
    /// <summary>
    /// Properties and methods for downloading the latest version of the Plex
    /// Media Server for Windows.
    /// </summary>
    public class LatestAvailableVersion
    {
        #region Event Delegates
        /// <summary>
        /// The delegate for the Message event handler.
        /// </summary>
        /// <param name="sender">
        /// The object that triggered the event.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public delegate void MessageChangedEventHandler(object sender, string messagee);
        #endregion

        #region Events
        /// <summary>
        /// The MessageChanged event member.
        /// </summary>
        public event MessageChangedEventHandler MessageChanged;

        /// <summary>
        /// Triggered when the message has changed.
        /// </summary>
        protected virtual void OnMessageChanged(string message)
        {
            MessageChanged?.Invoke(this, message);
        }
        #endregion

        #region Private Enumerations
        /// <summary>
        /// The type of installation package.
        /// </summary>
        private enum PackageType
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
        #endregion

        #region Private Constants
        /// <summary>
        /// The public URL to the JSON data that contains information about the
        /// latest Plex Media Server installs.
        /// </summary>
        private const string PlexPackagePublicJsonUrl =
            "https://plex.tv/api/downloads/1.json";

        /// <summary>
        /// The URL to the JSON data that contains information about the
        /// latest Plex Media Server installs.
        /// </summary>
        private const string PlexPackageJsonUrl =
            "https://plex.tv/api/downloads/5.json";

        /// <summary>
        /// The additional querystring to add to the URL to request the Plex
        /// Pass edition of the Plex install.
        /// </summary>
        private const string PlexPackageJsonUrlBeta = "?channel=plexpass";
        #endregion

        #region Private Variables
        /// <summary>
        /// The HTTP client used to connect to the Plex website.
        /// </summary>
        private HttpClient _client = new HttpClient();

        /// <summary>
        /// The Plex user's registry key.
        /// </summary>
        private string _plexUserRegistryKey;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the latest Windows version of the Plex Media Server.
        /// </summary>
        public SystemType LatestWindowsVersion { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an instance of the <see cref="Download"/> class.
        /// </summary>
        public LatestAvailableVersion(string plexUserRegistryKey)
        {
            _plexUserRegistryKey = plexUserRegistryKey;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Get the checksum for a specified file.
        /// </summary>
        /// <param name="filePath">
        /// The full path to the file.
        /// </param>
        /// <returns>
        /// The checksum for the file.
        /// </returns>
        private string GetChecksum(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            if (!File.Exists(filePath))
            {
                return null;
            }

            using (SHA1 sha = SHA1.Create())
            {
                try
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        byte[] hash = sha.ComputeHash(stream);

                        if (hash == null || hash.Length == 0)
                        {
                            return null;
                        }

                        return BitConverter.ToString(hash).Replace("-", "").ToLower();
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the filename for the latest install file.
        /// </summary>
        /// <returns>
        /// The filename of the latest install file.
        /// </returns>
        private string GetPackageFileName()
        {
            if (LatestWindowsVersion == null)
            {
                return null;
            }

            if (LatestWindowsVersion.Releases.Count == 0)
            {
                OnMessageChanged("WARN: There were no releases specified from Plex for Windows.");
                return null;
            }

            if (LatestWindowsVersion.Releases[0] == null)
            {
                OnMessageChanged("WARN: There were no releases specified from Plex for Windows.");
                return null;
            }

            string url = LatestWindowsVersion.Releases[0].Url;
            if (string.IsNullOrEmpty(url))
            {
                OnMessageChanged("WARN: The URL for the Windows release was not specified.");
                return null;
            }

            return Path.GetFileName(url);
        }

        /// <summary>
        /// Gets the full local path for the install package.
        /// </summary>
        /// <returns>
        /// The full path for the install package, or null if the full path
        /// could not be determined.
        /// </returns>
        private string GetPackageFullPath()
        {
            if (LatestWindowsVersion == null)
            {
                return null;
            }

            // Get the local path for the latest install and verify the folder
            // exists
            string folder = GetPackagePath();
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            // Get the full path to the latest install and then verify the file
            // exists
            string name = GetPackageFileName();
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return Path.Combine(folder, name);
        }

        /// <summary>
        /// Gets the download package local path.
        /// </summary>
        /// <returns>
        /// The downloaded package local path or null if the path could not
        /// be determined.
        /// </returns>
        private string GetPackagePath()
        {
            try
            {
                if (LatestWindowsVersion == null)
                {
                    return null;
                }

                string version = LatestWindowsVersion.Version;
                string appPath = (string)Registry.GetValue(
                    _plexUserRegistryKey,
                    "LocalAppDataPath",
                    null);

                if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(appPath))
                {
                    OnMessageChanged($"The registry value from '{_plexUserRegistryKey}\\LocalAppDataPath' could not be retrieved. Verify that the registry value exists.");
                    return null;
                }

                return Path.Combine(appPath, $@"Plex Media Server\Updates\{version}\packages");
            }
            catch (Exception ex)
                when (ex is IOException || ex is System.Security.SecurityException)
            {
                OnMessageChanged($"The registry value from '{_plexUserRegistryKey}\\LocalAppDataPath' could not be retrieved. Reason: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the URL for the specified update channel that is specified
        /// on the Plex server.
        /// </summary>
        /// <returns>
        /// The URL for the specified update channel package, or the public URL
        /// if the URL could not be determined.
        /// </returns>
        private string GetPackageUrl()
        {
            string value = null;
            try
            {
                value = (string)Registry.GetValue(
                    _plexUserRegistryKey,
                    "ButlerUpdateChannel",
                    null);
            }
            catch (Exception ex)
                when (ex is System.Security.SecurityException || ex is IOException)
            {
                OnMessageChanged($"WARN: The registry value from '{_plexUserRegistryKey}\\ButlerUpdateChannel' could not be retrieved. Reason: {ex.Message}");
                return PlexPackageJsonUrl;
            }

            if (value == null)
            {
                OnMessageChanged($"WARN: The registry value from '{_plexUserRegistryKey}\\ButlerUpdateChannel' could not be retrieved. Defaulting to the Public Plex update.");
                return PlexPackagePublicJsonUrl;
            }

            int updateChannel;
            if (!int.TryParse(value, out updateChannel))
            {
                OnMessageChanged($"WARN: The registry value from '{_plexUserRegistryKey}\\ButlerUpdateChannel' needs to be an numeric value, and it was '{value}' instead. Defaulting to the Public Plex update");
                return PlexPackagePublicJsonUrl;
            }

            if (updateChannel == (int)PackageType.PlexPass)
            {
                OnMessageChanged("The update channel is set for Plex Pass.");
                return PlexPackageJsonUrl + PlexPackageJsonUrlBeta;
            }
            else
            {
                OnMessageChanged("The update channel is set for public.");
                return PlexPackagePublicJsonUrl;
            }
        }

        /// <summary>
        /// Gets the JSON string value for the latest versions of Plex from
        /// the Plex download site.
        /// </summary>
        /// <returns>
        /// The JSON string value if the request was successful, or null if
        /// the request was not successful.
        /// </returns>
        private string GetPlexPackageJson()
        {
            string content;

            // Get the Plex token for the Plex user
            string token = MediaServer.GetToken(_plexUserRegistryKey);

            if (token != null)
            {
                _client.DefaultRequestHeaders.Add("X-Plex-Token", token);
            }

            try
            {
                // Get the URL for the package specified by the update channel
                // set in the Plex server
                string url = GetPackageUrl();
                if (url == null)
                {
                    return null;
                }

                using (HttpResponseMessage response = _client.GetAsync(url).Result)
                {
                    content = response.Content.ReadAsStringAsync().Result;
                }

                return content;
            }
            catch (HttpRequestException ex)
            {
                OnMessageChanged($"Could not get Plex package information. Message: {ex.Message}.");
                return null;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions)
                {
                    OnMessageChanged($"Could not get Plex package information. Message: {e.Message}.");                    
                }
                return null;
            }
        }

        /// <summary>
        /// Initialize the latest available of Plex Media Server to download.
        /// </summary>
        private void Initialize()
        {
            OnMessageChanged("Checking for the latest version from Plex.");
            string json = GetPlexPackageJson();

            if (string.IsNullOrEmpty(json))
            {
                OnMessageChanged("Could not get the latest version information from Plex.");
                return;
            }

            OnMessageChanged("Parsing the information from Plex.");
            CurrentVersion versions =
                JsonConvert.DeserializeObject<CurrentVersion>(json);

            LatestWindowsVersion = versions.Computer["Windows"];
            if (LatestWindowsVersion == null)
            {
                OnMessageChanged("Could not get the latest version information from Plex.");
                return;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Downloads the latest Plex Media Server installation file for
        /// Windows.
        /// </summary>
        /// <param name="url">
        /// The URL for the installation.
        /// </param>
        /// <param name="filePath">
        /// The full path where the file is to be saved.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> of the download.
        /// </returns>
        /// <remarks>
        /// If the file has already been downloaded, and the file is valid,
        /// then the file won't be downloaded again.
        /// </remarks>
        public async Task<bool> Download()
        {
            OnMessageChanged("Getting ready to download the latest package.");

            if (LatestWindowsVersion == null)
            {
                Initialize();
                if (LatestWindowsVersion == null)
                {
                    OnMessageChanged(
                        "The latest Windows version has not been specified.");
                    return false;
                }
            }

            // Verify that the URL for the latest release has been stored
            string url = LatestWindowsVersion.Releases[0].Url;
            if (string.IsNullOrEmpty(url))
            {
                OnMessageChanged(
                    "The URL for the latest version was not specified.");
                return false;
            }

            string filePath = GetPackageFullPath();
            if (string.IsNullOrEmpty(filePath))
            {
                OnMessageChanged(
                    "The path to the local downloaded install file could not be determined.");
                return false;
            }

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                OnMessageChanged($"Creating folder: {directory}.");
                Directory.CreateDirectory(directory);
            }
            else
            {
                // If the directory already exists, check to see if the file
                // also exists
                if (File.Exists(filePath))
                {
                    OnMessageChanged($"The file, {filePath}, exists. Checking to see if the package is valid.");
                    // If the file is valid - meaning the checksum matches the
                    // checksum of the file to be downloaded, then return true
                    // to avoid redownloading the same file a second time
                    if (IsValid())
                    {
                        OnMessageChanged("Since the package is valid - not downloading again.");
                        return true;
                    }
                }
            }

            try
            {
                OnMessageChanged("Downloading the latest installation package from Plex.");
                // Get the response once it is available and the headers are read
                using (HttpResponseMessage response =
                    await _client.GetAsync(
                        url,
                        HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(
                            false))
                {
                    // Get the stream content
                    using (Stream streamToReadFrom =
                        await response.Content.ReadAsStreamAsync())
                    {
                        // Write the stream to the local file path
                        using (Stream streamToWriteTo =
                            File.Open(filePath, FileMode.Create))
                        {
                            await streamToReadFrom.CopyToAsync(streamToWriteTo);
                        }
                    }
                }
            }
            catch (Exception ex)
                when (ex is HttpRequestException || ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            {
                OnMessageChanged($"Could not download update. Message: {ex.Message}.");
                return false;
            }

            // Check to see if the downloaded file is valid            
            return IsValid();
        }

        /// <summary>
        /// Converts the string value of the downloaded file version into 
        /// a <see cref="Version"/> object.
        /// </summary>
        /// <returns>
        /// A <see cref="Version"/> object that represents the version of
        /// the downloaded file.
        /// </returns>
        public Version GetVersion()
        {
            if (LatestWindowsVersion == null)
            {
                Initialize();
                if (LatestWindowsVersion == null)
                {
                    return default(Version);
                }                
            }

            string version = LatestWindowsVersion.Version;
            if (string.IsNullOrEmpty(version))
            {
                return default(Version);
            }

            // The regular expression used to parse the file version
            Regex regEx = new Regex(
                @"^(?<Major>\d+)\.(?<Minor>\d+)\.(?<Build>\d+)\.(?<Revision>\d+)\-\S+$");

            try
            {
                // Ensure that a match is made
                if (regEx.IsMatch(version))
                {
                    // Find the first match for the regular expression in the value
                    Match match = regEx.Match(version);

                    // Return the version object
                    Version fileVersion = new Version(
                        Convert.ToInt32(match.Groups["Major"].Value),
                        Convert.ToInt32(match.Groups["Minor"].Value),
                        Convert.ToInt32(match.Groups["Build"].Value),
                        Convert.ToInt32(match.Groups["Revision"].Value));

                    OnMessageChanged(
                        $"The latest file version available for download is {fileVersion.ToString()}.");

                    return fileVersion;
                }
                else
                {
                    return default(Version);
                }
            }
            catch (Exception ex)
                when (ex is ArgumentOutOfRangeException || ex is RegexMatchTimeoutException)
            {
                return default(Version);
            }
        }

        /// <summary>
        /// Checks to see if the downloaded install package is a valid package.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            if (LatestWindowsVersion == null)
            {
                Initialize();
                if (LatestWindowsVersion == null)
                {
                    OnMessageChanged(
                        "The package is not valid. Could not get the latest version information from Plex.");
                    return false;
                }
            }

            // Get the local path for the latest install and verify the folder
            // exists            
            string folder = GetPackagePath();
            if (string.IsNullOrEmpty(folder))
            {
                OnMessageChanged("The package folder could not be determined.");
                return false;
            }

            if (!Directory.Exists(folder))
            {
                OnMessageChanged(
                    $"The package is not valid. The folder, {folder}, does not exist.");
                return false;
            }

            // Get the full path to the latest install and then verify the file
            // exists
            string name = GetPackageFileName();
            if (string.IsNullOrEmpty(name))
            {
                OnMessageChanged(
                    $"The package is not valid. Could not get the file name {name}.");
                return false;
            }

            string filePath = Path.Combine(folder, name);
            if (!File.Exists(filePath))
            {
                OnMessageChanged(
                    $"The package is not valid. Could not find {filePath}.");
                return false;
            }


            // Get the checksum for the latest install and the validate that
            // the checksum matches the checksum from the Plex site
            string checksum = GetChecksum(filePath);

            OnMessageChanged("Checking if the installation package is valid.");
            bool isValid =
                checksum.Equals(LatestWindowsVersion.Releases[0].CheckSum);

            if (isValid)
            {
                OnMessageChanged("The package is valid. The checksums match.");
            }
            else
            {
                OnMessageChanged("The package is not valid. The checksums match.");
            }
            return isValid;
        }
        #endregion
    }
}
