using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace TE.Plex
{
    /// <summary>
    /// The MediaContaier element in the XML file.
    /// </summary>
    [XmlRoot("MediaContainer")]
    public class MediaContainer
    {
        /// <summary>
        /// The size of the current play list.
        /// </summary>
        [XmlAttribute("size")]
        public string Size { get; set; }
    }
}
