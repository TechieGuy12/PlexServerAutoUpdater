using System;
using System.Runtime.Serialization;

namespace TE.Plex
{
    /// <summary>
    /// An application is not installed.
    /// </summary>
    [Serializable]
    public class PlexDataFolderNotFoundException : Exception
    {
        public PlexDataFolderNotFoundException() { }

        public PlexDataFolderNotFoundException(string message)
            : base(message) { }

        public PlexDataFolderNotFoundException(
            string message,
            Exception innerException)
            : base(message, innerException) { }

        protected PlexDataFolderNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
