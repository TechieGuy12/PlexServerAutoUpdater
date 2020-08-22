using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public static class Defs
    {

    }
}
