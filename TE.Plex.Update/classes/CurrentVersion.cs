using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TE.Plex.Update
{
    /// <summary>
    /// The current version of Plex Media Server.
    /// </summary>
    public class CurrentVersion
    {
        /// <summary>
        /// Gets or sets the computer Plex Media Server releases.
        /// </summary>
        [JsonProperty("computer")]
        public Dictionary<string, SystemType> Computer { get; set; }

        /// <summary>
        /// Gets or sets the NAS Plex Server releases.
        /// </summary>
        [JsonProperty("nas")]
        public Dictionary<string, SystemType> Nas { get; set; }
    }
}
