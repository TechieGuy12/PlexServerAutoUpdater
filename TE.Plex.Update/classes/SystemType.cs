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
    }
}
