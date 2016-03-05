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
        private int returnValue;

        public MSIException() { }

        public MSIException(string message) : base(message) { }

        public MSIException(int returnValue) 
        	: this("MSIError : " + ((MsiExitCodes)returnValue).ToString())
        {
            this.returnValue = returnValue;
        }

        public MSIException(string message, Exception innerException) 
        	: base(message, innerException) { }

        protected MSIException(
        	SerializationInfo info, 
        	StreamingContext context) 
        	: base(info, context) { }
    }
}
