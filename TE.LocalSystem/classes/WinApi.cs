using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TE.LocalSystem
{
	/// <summary>
	/// API and constant definitions for using the Windows API.
	/// </summary>
	public static class WinApi
	{
		#region Public Constants
		/// <summary>
		/// No error.
		/// </summary>
		public const int NO_ERROR = 0;
        
        /// <summary>
        /// The buffer isn't sufficient to store the result.
        /// </summary>
		public const int ERROR_INSUFFICIENT_BUFFER = 122;
        
        /// <summary>
        /// Invalid flags.
        /// </summary>
        /// <remarks>
        /// On Windows Server 2003 this error is/can be returned, but processing can still continue.
        /// </remarks>
		public const int ERROR_INVALID_FLAGS = 1004; 
		#endregion
		
		#region Public Enumerations
		/// <summary>
		/// contains values that specify the type of a security identifier (SID).
		/// </summary>
		public enum SID_NAME_USE 
		{
			/// <summary>
			/// A user SID.
			/// </summary>
			SidTypeUser = 1,
			/// <summary>
			/// A group SID.
			/// </summary>
			SidTypeGroup,
			/// <summary>
			/// A domain SID.
			/// </summary>
			SidTypeDomain,
			/// <summary>
			/// An alias SID.
			/// </summary>
			SidTypeAlias,
			/// <summary>
			/// A SID for a well-known group.
			/// </summary>
			SidTypeWellKnownGroup,
			/// <summary>
			/// A SID for a deleted account.
			/// </summary>
			SidTypeDeletedAccount,
			/// <summary>
			/// A SID That is not valid.
			/// </summary>
			SidTypeInvalid,
			/// <summary>
			/// A SID of unknown type.
			/// </summary>
			SidTypeUnknown,
			/// <summary>
			/// A SID for a computer.
			/// </summary>
			SidTypeComputer,
			/// <summary>
			/// A mandatory integrity label SID.
			/// </summary>
			SidTypeLabel
		}
		#endregion
		
		#region Public API Functions
		/// <summary>
		/// The LookupAccountName function accepts the name of a system and an 
		/// account as input. It retrieves a security identifier (SID) for the 
		/// account and the name of the domain on which the account was found.
		/// </summary>
		/// <param name="lpSystemName">
		/// The name of the system.
		/// </param>
		/// <param name="lpAccountName">
		/// The name of the account on the system.
		/// </param>
		/// <param name="Sid">
		/// The pointer to a buffer that receives the SID structure.
		/// </param>
		/// <param name="cbSid">
		/// The size of the SID buffer.
		/// </param>
		/// <param name="ReferencedDomainName">
		/// A buffer that received the name of the domain associated with
		/// the account.
		/// </param>
		/// <param name="cchReferencedDomainName">
		/// The size of the domain name buffer.
		/// </param>
		/// <param name="peUse">
		/// A pointer to a <see cref="TE.LocalSystem.WinApi.SID_NAME_USE">SID_NAME_USER</see>
		/// enum.
		/// </param>
		/// <returns>
		/// If the function succeeds, the function returns nonzero.
		/// If the function fails, it returns zero.
		/// </returns>
        [DllImport("advapi32.dll", CharSet=CharSet.Auto, SetLastError = true)]
        public static extern bool LookupAccountName (
			string lpSystemName,
			string lpAccountName,
			[MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
			ref uint cbSid,
			StringBuilder ReferencedDomainName,
			ref uint cchReferencedDomainName,
			out SID_NAME_USE peUse);        

		/// <summary>
		///  converts a security identifier (SID) to a string format suitable
		/// for display, storage, or transmission.
		/// </summary>
		/// <param name="pSID">
		/// A pointer to the SID structure to be converted.
		/// </param>
		/// <param name="ptrSid">
		/// A pointer to a variable that receives a pointer to a null-terminated
		///	SID string.
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero. 
		/// </returns>
		[DllImport("advapi32", CharSet=CharSet.Auto, SetLastError=true)]
		public static extern bool ConvertSidToStringSid(
			[MarshalAs(UnmanagedType.LPArray)] byte [] pSID, 
			out IntPtr ptrSid);
		
		/// <summary>
		/// Frees the specified local memory object and invalidates its handle.
		/// </summary>
		/// <param name="hMem">
		/// A handle to the local memory object. 
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is NULL.
		/// If the function fails, the return value is equal to a handle to the
		/// local memory object.
		/// </returns>
		[DllImport("kernel32.dll")]
		public static extern IntPtr LocalFree(IntPtr hMem);
        #endregion
	}
}
