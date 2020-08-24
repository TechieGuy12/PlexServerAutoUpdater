using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TE.Plex.Update
{
    /// <summary>
    /// Properties and methods for downloading the latest version of the Plex
    /// Media Server for Windows.
    /// </summary>
    public class Package : EventSource
    {
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
        /// The local application data folder for Plex.
        /// </summary>
        private string _updatesFolder;

        /// <summary>
        /// The update channel used to update Plex.
        /// </summary>
        private UpdateChannel _updateChannel;

        /// <summary>
        /// The Plex user's token.
        /// </summary>
        private string _token;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the latest Windows version of the Plex Media Server.
        /// </summary>
        public SystemType LatestWindowsVersion { get; private set; }

        /// <summary>
        /// Gets the path to the installation file once it has been downloaded
        /// from the Plex server. This value remains null, unless the <see cref="Download"/>
        /// method is called, and the file has been downloaded successfully.
        /// </summary>
        public string FilePath { get; private set; } = null;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an instance of the <see cref="Package"/> class
        /// when provided with the Plex user's registry key and the user's token.
        /// </summary>
        /// <param name="localAppDataFolder">
        /// The local application data folder for Plex.
        /// </param>
        /// <param name="updateChannel">
        /// The update channel used to update Plex.
        /// </param>
        /// <param name="token">
        /// The Plex user's token.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// An argument provided is <c>null</c>.
        /// </exception>
        public Package(
            string updatesFolder,
            UpdateChannel updateChannel,
            string token)
        {
            _updatesFolder =
               updatesFolder ?? throw new ArgumentNullException(nameof(updatesFolder));
            _updateChannel = updateChannel;
            _token = token ?? throw new ArgumentNullException(nameof(token));
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
        /// The checksum for the file or <c>null</c> if the checksum could not
        /// be determined.
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
        /// The filename of the latest install file or <c>null</c> if the latest
        /// install file could not be determined.
        /// </returns>
        private string GetFileName()
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
        /// The full path for the install package, or <c>null</c> if the full
        /// path could not be determined.
        /// </returns>
        private string GetFullPath()
        {
            if (LatestWindowsVersion == null)
            {
                return null;
            }

            // Get the local path for the latest install and verify the folder
            // exists
            string folder = GetPath();
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            // Get the full path to the latest install and then verify the file
            // exists
            string name = GetFileName();
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
        /// The downloaded package local path or <c>null</c> if the path could not
        /// be determined.
        /// </returns>
        private string GetPath()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_updatesFolder))
                {
                    return null;
                }

                if (LatestWindowsVersion == null)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(LatestWindowsVersion.Version))
                {
                    return null;
                }

                return Path.Combine(
                    _updatesFolder, 
                    $@"{LatestWindowsVersion.Version}\packages");
            }
            catch (Exception ex)
                when (ex is ArgumentException || ex is System.Security.SecurityException)
            {
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
        private string GetUrl()
        {
            if (_updateChannel == UpdateChannel.PlexPass)
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
        private string GetJson()
        {
            string content;

            if (_token != null)
            {
                _client.DefaultRequestHeaders.Add("X-Plex-Token", _token);
            }

            try
            {
                // Get the URL for the package specified by the update channel
                // set in the Plex server
                string url = GetUrl();
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
            string json = GetJson();

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

            FilePath = GetFullPath();
            if (string.IsNullOrEmpty(FilePath))
            {
                OnMessageChanged(
                    "The path to the local downloaded install file could not be determined.");
                return false;
            }

            string directory = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directory))
            {
                try
                {
                    OnMessageChanged($"Creating folder: {directory}.");
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    OnMessageChanged($"Could not create download folder. Reason: {ex.Message}");
                    return false;
                }
            }
            else
            {
                // If the directory already exists, check to see if the file
                // also exists
                if (File.Exists(FilePath))
                {
                    OnMessageChanged($"The file, {FilePath}, exists. Checking to see if the package is valid.");
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
                            File.Open(FilePath, FileMode.Create))
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
                    return default;
                }                
            }

            string version = LatestWindowsVersion.Version;
            if (string.IsNullOrEmpty(version))
            {
                return default;
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
                    return default;
                }
            }
            catch (Exception ex)
                when (ex is ArgumentOutOfRangeException || ex is RegexMatchTimeoutException)
            {
                return default;
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
            string folder = GetPath();
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
            string name = GetFileName();
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
