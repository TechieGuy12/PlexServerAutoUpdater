using System;
using System.Runtime.Serialization;

namespace TE.LocalSystem
{
    /// <summary>
    /// The SID for the Windows user could not be found.
    /// </summary>
    public class WindowsUserSidNotFound : Exception
    {
        public WindowsUserSidNotFound() { }

        public WindowsUserSidNotFound(string message)
            : base(message) { }

        public WindowsUserSidNotFound(
            string message,
            Exception innerException)
            : base(message, innerException) { }

        protected WindowsUserSidNotFound(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
