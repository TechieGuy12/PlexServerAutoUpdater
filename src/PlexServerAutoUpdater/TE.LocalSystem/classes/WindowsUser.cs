using System;
using System.Security.Principal;

namespace TE.LocalSystem
{
	/// <summary>
	/// Description of WindowsUser.
	/// </summary>
	public class WindowsUser
	{
		#region Properties
		/// <summary>
		/// Gets or sets the name of the Windows user.
		/// </summary>
		public string Name { get; set; }
		#endregion
		
		#region Constructors
		/// <summary>
		/// Creates an instance of the <see cref="TE.LocalSystem.WindowsUser"/>
		/// class.
		/// </summary>
		public WindowsUser() { }
		
		/// <summary>
		/// Creates an instance of the <see cref="TE.LocalSystem.WindowsUser"/>
		/// class when provided with the Windows user's name.
		/// </summary>		
		public WindowsUser(string name)
		{
			this.Name = name;
		}
		#endregion
		
		#region Public Functions
		/// <summary>
		/// Gets the SID for the associated Windows user.
		/// </summary>
		/// <returns>
		/// The SID for the Windows user.
		/// </returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown when the Windows user name is not specified.
		/// </exception>
		public string GetSid()
		{
			if (string.IsNullOrEmpty(this.Name))
			{
				throw new System.ArgumentException(
					"The Windows user name cannot be null or blank.");
			}
			
			try
			{
				NTAccount account = new NTAccount(this.Name);
				SecurityIdentifier identifier = 
					(SecurityIdentifier)account.Translate(
						typeof(SecurityIdentifier));
				return identifier.Value;
			}
			catch
			{
				throw;
			}
		}
		
		public bool IsAdministrator()
		{
			WindowsIdentity identity = null;
			
			if (string.IsNullOrEmpty(this.Name))
			{
				identity = WindowsIdentity.GetCurrent();
			}
			else
			{
				identity = new WindowsIdentity(this.Name);
			}

			WindowsPrincipal principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);		
		}
		#endregion
	}
}
