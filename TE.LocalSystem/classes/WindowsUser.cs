using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace TE.LocalSystem
{
	/// <summary>
	/// Description of WindowsUser.
	/// </summary>
	public class WindowsUser
	{
		#region Constants
		/// <summary>
		/// The root key for the users registry hive.
		/// </summary>
		private const string RegistryUsersRoot = "HKEY_USERS";		
		/// <summary>
		/// The registry key that stores the local application data folder for
		/// the Windows user.
		/// </summary>
		private const string RegistryLocalAppDataKey = 
			@"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders\";
		/// <summary>
		/// The name of the local Plex data path registry value.
		/// </summary>
		private const string RegistryLocalAppDataValueName = "Local AppData";
		#endregion
		
		#region Private Variables
		/// <summary>
		/// The Windows identity of the user.
		/// </summary>
		private WindowsIdentity userIdentity = null;
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets the local application data folder for the user.
		/// </summary>
		public string LocalAppDataFolder {get; private set; }
		
		/// <summary>
		/// Gets or sets the name of the Windows user.
		/// </summary>
		public string Name { get; set; }	
		
		/// <summary>
		/// Gets the SID associated with the user.
		/// </summary>
		public string Sid { get; set; }
		#endregion
		
		#region Constructors
		/// <summary>
		/// Creates an instance of the <see cref="TE.LocalSystem.WindowsUser"/>
		/// class.
		/// </summary>
		/// <exception cref="TE.LocalSystem.WindowsUserSidNotFound">
		/// Thrown when the SID for the user is null or empty.
		/// </exception>
		public WindowsUser() 
		{ 
			try
			{
				this.Initialize(string.Empty);
			}
			catch
			{
				throw;
			}
		}
		
		/// <summary>
		/// Creates an instance of the <see cref="TE.LocalSystem.WindowsUser"/>
		/// class when provided with the Windows user's name.
		/// </summary>
		/// <exception cref="TE.LocalSystem.WindowsUserSidNotFound">
		/// Thrown when the SID for the user is null or empty.
		/// </exception>		
		public WindowsUser(string name)
		{
			try
			{
				this.Initialize(name);
			}
			catch
			{
				throw;
			}
		}
		#endregion
		
		#region Private Functions
		/// <summary>
		/// Gets the Windows identity for the user.
		/// </summary>
		/// <returns>
		/// A <see cref="System.Security.Principal.WindowsIdentity"/> object
		/// of the Windows user, or null if the <see cref="System.Security.Principal.WindowsIdentity"/>
		/// object could not be created.
		/// </returns>
		private WindowsIdentity GetIdentity()
		{
			WindowsIdentity identity = null;
			
			try
			{
				if (string.IsNullOrEmpty(this.Name))
				{
					// If no Windows user name is specified, just get the identity
					// for the current user
					identity = WindowsIdentity.GetCurrent();
				}
				else
				{
					// Get the identity for the user associated with the user name
					identity = new WindowsIdentity(this.Name);
				}
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}
			catch (System.Security.SecurityException)
			{
				return null;
			}
			
			return identity;
		}
		
		/// <summary>
		/// Gets the local application data path for the Windows user.
		/// </summary>
		/// <exception cref="TE.LocalSystem.WindowsUserSidNotFound">
		/// Thrown when the SID for the user is null or empty.
		/// </exception>
		/// <returns>
		/// The path of the local application data folder for the Windows user.
		/// </returns>
		private string GetLocalAppDataFolder()
		{	
			// Check to ensure a SID value for the user is set			
			if (string.IsNullOrEmpty(this.Sid))
			{
				throw new WindowsUserSidNotFound(
					"The SID for the user was not specified.");
			}
						
			try
			{
			// Get the local application data folder path for the user
				return Registry.GetValue(
				RegistryUsersRoot + @"\" + this.Sid + @"\" + RegistryLocalAppDataKey, 
				RegistryLocalAppDataValueName, 
				string.Empty).ToString();			
			}
			catch				
			{
				return string.Empty;
			}

		}
		
		/// <summary>
		/// Gets the SID for the associated Windows user.
		/// </summary>
		/// <returns>
		/// The SID for the Windows user.
		/// </returns>
		private string GetSid()
		{
			// Verify that the username was provided
			if (string.IsNullOrEmpty(this.Name))
			{
				return string.Empty;
			}
			
			try
			{
				// Get the account for the username
				NTAccount account = new NTAccount(this.Name);
				
				// Try to get the security identifier for the username
				SecurityIdentifier identifier = (SecurityIdentifier)account.Translate(
						typeof(SecurityIdentifier));
				
				// Return the string value of the identifier
				return identifier.Value;
			}			
			catch (IdentityNotMappedException)
			{
				// If the identity could not be mapped, just return an empty string
				return string.Empty;
			}					
		}
		
		/// <summary>
		/// Gets the SID for the associated Windows user using the Windows API.
		/// </summary>
		/// <returns>
		/// The SID for the Windows user.
		/// </returns>
		private string GetSidApi()
		{
			// Verify that the username was provided
			if (string.IsNullOrEmpty(this.Name))
			{
				return string.Empty;
			}
			
			// The byte array that will hold the SID
			byte [] sid = null;
			// The SID buffer size
			uint cbSid = 0;
			// Then domain name
			StringBuilder referencedDomainName = new StringBuilder();
			// The buffer size for the domain name
			uint cchReferencedDomainName = (uint)referencedDomainName.Capacity;
			// The type of SID
			WinApi.SID_NAME_USE sidUse;
			
			// Default the return value to indicate no error
			int err = WinApi.NO_ERROR;
			
			// Attempt to get the SID for the account name
			if (!WinApi.LookupAccountName(null, this.Name, sid, ref cbSid, referencedDomainName, ref cchReferencedDomainName, out sidUse))
			{
				// Get the error from the LookupAccountName API call
				err = Marshal.GetLastWin32Error();
				
				// Check to see if either the buffer wasn't sufficient or the
				// invalid flags error was returned
				if (err == WinApi.ERROR_INSUFFICIENT_BUFFER || err == WinApi.ERROR_INVALID_FLAGS)
				{
					// Create a new byte buffer with the size returned from the
					// first LookupAccountName request
					sid = new byte[cbSid];
					
					// Make sure that the capacity of the StringBuilder object
					// for the domain name is at the correct buffer size
					referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
					
					// Reset the return value to indicate no error
					err = WinApi.NO_ERROR;
			
					// Attempt to get the account name after the correct buffer
					// sizes have been set
					if (!WinApi.LookupAccountName(null, this.Name, sid,ref cbSid, referencedDomainName, ref cchReferencedDomainName, out sidUse))
					{
						// Return an empty string if the SID could not be
						// retrieved
						return string.Empty;
					}
				}
			}
			// Any other error that occured when trying to get the SID for the
			// account name
			else
			{
				// Return an empty string if the SID could not be
				// retrieved
				return string.Empty;
			}
			
			// No error occurred
			if (err == 0)
			{
				// Create the SID pointer
				IntPtr ptrSid;
				
				// Attempt to convert the SID byte array to a string
				if (!WinApi.ConvertSidToStringSid(sid, out ptrSid))
				{
					// Return an empty string if the SID could not be
					// retrieved
					return string.Empty;
				}
				else
				{
					// Copy all characters from an unmanaged memory string to
					// a manage string
					string sidString = Marshal.PtrToStringAuto(ptrSid);
					// Free up the memory
					WinApi.LocalFree(ptrSid);
					
					// Return the SID for the account name
					return sidString;
				}
			}
			else
			{
				// Return an empty string if the SID could not be
				// retrieved
				return string.Empty;
			}
		}
		
		/// <summary>
		/// Gets the SID from the registry for the associated Windows user.
		/// </summary>
		/// <returns>
		/// The SID For the Windows user.
		/// </returns>
		private string GetSidRegistry()
		{
			// Verify that the username was provided
			if (string.IsNullOrEmpty(this.Name))
			{
				return string.Empty;
			}
			
        	// Default to a blank SID
        	string sid = string.Empty;

			// Remove the domain name from the Windows user name
			string name = RemoveDomainFromName(this.Name);
        	
        	// Open the registry key that contains the profiles
        	RegistryKey key = Registry.LocalMachine.OpenSubKey(
        		@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
        	
        	// Loop through each of the subkeys that contain the profiles
        	foreach(string keyName in key.GetSubKeyNames())
        	{
        		// Open the key
        		RegistryKey sidKey = key.OpenSubKey(keyName);
        		
        		// Check to ensure the key was opened
        		if (sidKey != null)
        		{
        			// Get the profile path value from the registry
        			string profilePath = sidKey.GetValue("ProfileImagePath").ToString();

        			// Check to see if the account name is in the profile path
        			if (profilePath.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
        			{
        				// If the account name was in the profile path, store
        				// the SID
        				sid = System.IO.Path.GetFileName(sidKey.Name);

        			}
        		}
        	}

        	// Return the SID
        	return sid;
		}	

		/// <summary>
		/// Gets the SID of a well-known account. Such accounts are built into
		/// Windows and the SID for these accounts are the same on all
		/// computers.
		/// </summary>
		/// <returns>
		/// The SID for the Windows user.
		/// </returns>
		private string GetSidKnown()
		{
			// Verify that the username was provided
			if (string.IsNullOrEmpty(this.Name))
			{
				return string.Empty;
			}

			// Remove the domain name from the Windows user name
			string name = RemoveDomainFromName(this.Name);
        	
			SecurityIdentifier identifier = null;
			
			try
			{

				if (name == "LocalService")
				{
					// Get the security identifier for the LocalService
					identifier = new SecurityIdentifier(
						WellKnownSidType.LocalServiceSid,
						null);					
				}
				if (name == "LocalSystem")
				{
					// Get the security identifier for the LocalSystem
					identifier = new SecurityIdentifier(
						WellKnownSidType.LocalSystemSid,
						null);					
				}
				else if (name == "NetworkService")
				{					
					// Get the security identifier for the NetworkServer
					identifier = new SecurityIdentifier(
						WellKnownSidType.NetworkServiceSid,
						null);
				}
		
				return identifier.Value;
			
			}
			catch
			{
				return string.Empty;
			}
		}
        		
		/// <summary>
		/// Initializes the objects and properties in the class.
		/// </summary>
		/// <exception cref="TE.LocalSystem.WindowsUserSidNotFound">
		/// Thrown when the SID for the user is null or empty.
		/// </exception>
		private void Initialize(string name)
		{
			this.Name = name;
			this.userIdentity = this.GetIdentity();
			
			if (string.IsNullOrEmpty(this.Name))
			{
				this.Name = (this.userIdentity == null) ? WindowsIdentity.GetCurrent().Name : this.Name = this.userIdentity.Name;
			}

			// Try to see if the account is a well-known account and get the
			// SID for the account
			this.Sid = this.GetSidKnown();
			
			// If the the account name doesn't match a well-known account,
			// then try to find the SID for the account
			if (string.IsNullOrEmpty(this.Sid))
			{
				this.Sid = this.GetSid();
			
				// If no SID was returned, try to get the SID using the
				// Windows API
				if (string.IsNullOrEmpty(this.Sid))
				{
					this.Sid = this.GetSidApi();				
					
					// If still no SID was returned, try to find it in the
					// registry
					if (string.IsNullOrEmpty(this.Sid))
					{
						this.Sid = this.GetSidRegistry();
					}					
				}	
			}
			
			// Throw an exception is the SID was not found
			if (string.IsNullOrEmpty(this.Sid))
			{
				throw new WindowsUserSidNotFound(
					"The SID for the user was not specified.");
			}
			
			this.LocalAppDataFolder = this.GetLocalAppDataFolder();
		}
		
		/// <summary>
		/// Removes the domain name from the Windows user.
		/// </summary>
		/// <param name="name">
		/// The name of the Windows user
		/// </param>
		/// <returns>
		/// The name of the Windows user without the domain name.
		/// </returns>
		private string RemoveDomainFromName(string name)
		{        
			// Check to see if the name contains a slash			
        	if (name.Contains(@"\"))
        	{
        		// Remove the domain name
        		name = Regex.Replace(
        			name, 
        			@".*\\(.*)", 
        			"$1",
        			RegexOptions.None);
        	}

        	// Return the name without the domain name
        	return name;
		}
		#endregion
		
		#region Public Functions
		/// <summary>
		/// Checks to see if the user context running the application is an
		/// administrator.
		/// </summary>
		/// <returns>
		/// True if the user is an administrator, false if they are not an
		/// administrator.
		/// </returns>
		public bool IsAdministrator()
		{
			WindowsPrincipal principal = new WindowsPrincipal(this.userIdentity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);		
		}
		#endregion
	}
}
