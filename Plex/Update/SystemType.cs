using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TE.Plex.Update
{
    /// <summary>
    /// Information about the release for a specified system type.
    /// </summary>
    public class SystemType
    {
        /// <summary>
        /// Name of the 32-bit build.
        /// </summary>
        private const string BUILD32BIT = "windows-x86";
        /// <summary>
        /// Name of the 64-bit build.
        /// </summary>
        private const string BUILD64BIT = "windows-x86_64";

        /// <summary>
        /// The ID of the system type.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The name of the system type.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The release date of the Plex Media Server.
        /// </summary>
        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; }

        /// <summary>
        /// The version number of the Plex Media Server.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// The URL to the requirements of the Plex Media Server.
        /// </summary>
        [JsonProperty("requirements")]
        public string Requirements { get; set; }

        /// <summary>
        /// Any additional information associated with this verison of the Plex
        /// Media Server.
        /// </summary>
        [JsonProperty("extra_info")]
        public string ExtraInfo { get; set; }

        /// <summary>
        /// A list of items added to this version of the Plex Media Server.
        /// </summary>
        [JsonProperty("items_added")]
        public string ItemsAdded { get; set; }

        /// <summary>
        /// A list of items that were fixed with this version of the Plex Media
        /// Server.
        /// </summary>
        [JsonProperty("items_fixed")]
        public string ItemsFixed { get; set; }

        /// <summary>
        /// A <see cref="List{T}"/> object of <see cref="Release"/> objects for
        /// each release of the Plex Media Server for this system type.
        /// </summary>
        [JsonProperty("releases")]
        public List<Release> Releases { get; set; } = new List<Release>();

        /// <summary>
        /// Gets the download URL based on whether the 32-bit or 64-bit
        /// version of Plex Media Server is to be downloaded.
        /// </summary>
        /// <param name="is64Bit">
        /// Flag indicating which version of Plex Media Server is installed.
        /// </param>
        /// <returns>
        /// The URL for the version of Plex Media Server, otherwise <c>null</c>.
        /// </returns>
        public string GetUrl(bool is64Bit)
        {
            foreach (Release release in Releases)
            {
                if (release.Build.Equals(BUILD32BIT, StringComparison.OrdinalIgnoreCase)
                    && !is64Bit)
                {
                    return release.Url;
                }

                if (release.Build.Equals(BUILD64BIT, StringComparison.OrdinalIgnoreCase)
                    && is64Bit)
                {
                    return release.Url;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the checksum based on whether the 32-bit or 64-bit version of
        /// Plex Media Server is to be downloaded.
        /// </summary>
        /// <param name="is64Bit">
        /// Flag indicating which version of Plex Media Server is installed.
        /// </param>
        /// <returns>
        /// The checksum for the installation file of Plex Media Server,
        /// otherwise <c>null</c>.
        /// </returns>
        public string GetCheckSum(bool is64Bit)
        {
            foreach (Release release in Releases)
            {
                if (release.Build.Equals(BUILD32BIT, StringComparison.OrdinalIgnoreCase)
                    && !is64Bit)
                {
                    return release.CheckSum;
                }

                if (release.Build.Equals(BUILD64BIT, StringComparison.OrdinalIgnoreCase)
                    && is64Bit)
                {
                    return release.CheckSum;
                }
            }

            return null;
        }
    }
}
