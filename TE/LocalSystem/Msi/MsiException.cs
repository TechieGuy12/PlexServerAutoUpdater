using System;
using System.Runtime.Serialization;

namespace TE.LocalSystem.Msi
{
    /// <summary>
    /// An issue occurred with the Windows Installer.
    /// </summary>
    [Serializable]
    internal class MSIException : Exception
    {
        /// <summary>
        /// Gets or sets the return value.
        /// </summary>
        public int ReturnValue { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSIException"/> class.
        /// </summary>
        public MSIException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSIException"/> class
        /// when provided with the execption message.
        /// </summary>
        /// <param name="message">
        /// The exception message.
        /// </param>
        public MSIException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSIException"/> class
        /// when provided with the return value.
        /// </summary>
        /// <param name="returnValue">
        /// The return value.
        /// </param>
        public MSIException(int returnValue)
            : this($"MSIError : {((MsiExitCodes)returnValue).ToString()}")
        {
            ReturnValue = returnValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSIException"/> class
        /// when provided with the execption message and inner exception.
        /// </summary>
        /// <param name="message">
        /// The exception message.
        /// </param>
        /// <param name="innerException">
        /// The inner exception.
        /// </param>
        public MSIException(string message, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSIException"/> class
        /// when provided with the serialization information and streaming
        /// context.
        /// </summary>
        /// <param name="info">
        /// The serialization information.
        /// </param>      
        /// <param name="context">
        /// The streaming context.
        /// </param>
        protected MSIException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
