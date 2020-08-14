using System;
using System.Runtime.Serialization;

namespace TE.Plex
{
    /// <summary>
    /// A service is not installed.
    /// </summary>
    public class ServiceNotInstalledException : Exception
    {
        public ServiceNotInstalledException() { }

        public ServiceNotInstalledException(string message)
            : base(message) { }

        public ServiceNotInstalledException(
            string message,
            Exception innerException)
            : base(message, innerException) { }

        protected ServiceNotInstalledException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
