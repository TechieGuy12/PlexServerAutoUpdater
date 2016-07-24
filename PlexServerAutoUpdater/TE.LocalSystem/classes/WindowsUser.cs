using System;
using System.Security.Principal;
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
		public WindowsUser() 
		{ 
			this.Initialize(string.Empty);
		}
		
		/// <summary>
		/// Creates an instance of the <see cref="TE.LocalSystem.WindowsUser"/>
		/// class when provided with the Windows user's name.
		/// </summary>		
		public WindowsUser(string name)
		{
			this.Initialize(name);
		}
		#endregion
		
		#region Private Functions
		/// <summary>
		/// Gets the local application data path for the Windows user.
		/// </summary>
		/// <exception cref="System.InvalidOperationException">
		/// Thrown when the SID for the user is null or empty.
		/// </exception>
		/// <returns></returns>
		private string GetLocalAppDataFolder()
		{	
			// Check to ensure a SID value for the user is set			
			if (string.IsNullOrEmpty(this.Sid))
			{
				throw new InvalidOperationException(
					"The SID for the user was not specified.");
			}

			// Get the local application data folder path for the user
			return Registry.GetValue(
				RegistryUsersRoot + @"\" + this.Sid + @"\" + RegistryLocalAppDataKey, 
				RegistryLocalAppDataValueName, 
				string.Empty).ToString();			
		}
		
		/// <summary>
		/// Gets the SID for the associated Windows user.
		/// </summary>
		/// <returns>
		/// The SID for the Windows user.
		/// </returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the Windows user name is not specified.
		/// </exception>
		private string GetSid()
		{
			if (string.IsNullOrEmpty(this.Name))
			{
				throw new System.ArgumentException(
					"The Windows user name cannot be null or blank.");
			}
			
			NTAccount account = new NTAccount(this.Name);
			
			SecurityIdentifier identifier = 
				(SecurityIdentifier)account.Translate(
					typeof(SecurityIdentifier));
			
			return identifier.Value;		
		}
		
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
		/// Initializes the objects and properties in the class.
		/// </summary>
		private void Initialize(string name)
		{
			this.Name = name;
			this.userIdentity = this.GetIdentity();
			
			if (string.IsNullOrEmpty(this.Name))
			{
				this.Name = (this.userIdentity == null) ? WindowsIdentity.GetCurrent().Name : this.Name = this.userIdentity.Name;
			}
			
			this.Sid = this.GetSid();
			this.LocalAppDataFolder = this.GetLocalAppDataFolder();
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
