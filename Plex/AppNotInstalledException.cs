using System;
using System.Runtime.Serialization;

namespace TE.Plex
{
    /// <summary>
    /// An application is not installed.
    /// </summary>
    [Serializable]
    public class AppNotInstalledException : Exception
    {
        public AppNotInstalledException() { }

        public AppNotInstalledException(string message)
            : base(message) { }

        public AppNotInstalledException(
            string message,
            Exception innerException)
            : base(message, innerException) { }

        protected AppNotInstalledException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
