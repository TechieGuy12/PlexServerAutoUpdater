using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TE.Plex.Update
{
    /// <summary>
    /// A release of the Plex Media Server.
    /// </summary>
    public class Release
    {
        /// <summary>
        /// The label associated with the release.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// The name of the release.
        /// </summary>
        [JsonProperty("build")]
        public string Build { get; set; }

        /// <summary>
        /// The distribution of the release.
        /// </summary>
        [JsonProperty("distro")]
        public string Distro { get; set; }

        /// <summary>
        /// The download URL for the build.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// The checksum of the build.
        /// </summary>
        [JsonProperty("checksum")]
        public string CheckSum { get; set; }


    }
}
