using System;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using TE.LocalSystem;

namespace TE.Plex
{
	/// <summary>
	/// Description of ServerService.
	/// </summary>
	public class ServerService
	{
		#region Constants
		/// <summary>
		/// The name of the Plex service.
		/// </summary>
		private const string ServiceName = "PlexService";
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets the user ID used to run the service.
		/// </summary>
		public WindowsUser LogOnUser { get; private set; }
		#endregion
		
		#region Constructors
		/// <summary>
		/// Creates an instance of the <see cref="TE.Plex.ServerService"/>
		/// class.
		/// </summary>
		/// <exception cref="TE.Plex.ServiceNotInstalledException">
		/// The Plex Media Server service is not installed.
		/// </exception>
		/// <exception cref="TE.LocalSystem.WindowsUserSidNotFound">
		/// The Plex Media Server service account SID could not be found.
		/// </exception>
		public ServerService()
		{
			try
			{
				// Get the LogOnUser for the Plex Media Server service
				this.LogOnUser = GetServiceUser();
			}
			catch (WindowsUserSidNotFound)
			{
				throw new WindowsUserSidNotFound(
					"The Plex Media Server service account SID could not be found.");
			}
			
			// If a WindowsUser object was not returned, throw an exception
			// indicating the service does not exist
			if (this.LogOnUser == null)
			{
				throw new ServiceNotInstalledException(
					"The Plex Media Server service is not installed.");
			}
		}
		#endregion
		
		#region Private Functions
		/// <summary>
		/// Gets the name of the Plex service log on user name.
		/// </summary>
		/// <returns>
		/// A <see cref="TE.LocalSystem.WindowsUser"/> object of the service
		/// log on user.
		/// </returns>
		private WindowsUser GetServiceUser()
		{
			WindowsUser user = null;
			
			if (IsInstalled())
			{
				ManagementObject service = 
					new ManagementObject(
						"Win32_Service.Name='" + ServiceName + "'");
				service.Get();
				user = new WindowsUser(service["startname"].ToString().Replace(
					@".\", System.Environment.MachineName + @"\"));
			}
			
			return user;
		}
		#endregion
		
		#region Public Functions
		/// <summary>
		/// Checks to see if the Plex service is installed.
		/// </summary>
		/// <returns>
		/// True if the service is installed, false if it isn't installed.
		/// </returns>
		public static bool IsInstalled()
		{
			return ServiceController.GetServices().Any(
				s => s.ServiceName == ServiceName);
		}
		
		/// <summary>
		/// Stops the Plex Media Server service.
		/// </summary>
		public void Stop()
		{
			if (IsInstalled())
			{
				using (ServiceController sc = new ServiceController(ServiceName))
				{
					if (sc.Status == ServiceControllerStatus.Running)
					{
						sc.Stop();
						sc.WaitForStatus(ServiceControllerStatus.Stopped);
					}
				}
			}
		}
		
		/// <summary>
		/// Starts the Plex Media Server service.
		/// </summary>
		public void Start()
		{
			if (IsInstalled())
			{
				using (ServiceController sc = new ServiceController(ServiceName))
				{
					sc.Start();
					sc.WaitForStatus(ServiceControllerStatus.Running);
				}
			}
		}
		#endregion
	}
}
