using System;
using static System.Environment;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using TE.LocalSystem;
using System.Configuration;

namespace TE.Plex
{
    /// <summary>
    /// The properties and methods associated with the Plex Media Server
    /// service.
    /// </summary>
    public class ServerService
    {
        #region Constants
        /// <summary>
        /// The name of the Plex service.
        /// </summary>
        private static string ServiceName = ConfigurationManager.AppSettings["PlexServiceName"];
        #endregion

        // Flag indicating the service display name has been specified instead
        // of the service name
        private static bool _usingDisplayName = false;

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
                LogOnUser = GetServiceUser();
            }
            catch (WindowsUserSidNotFound)
            {
                throw new WindowsUserSidNotFound(
                    "The Plex Media Server service account SID could not be found.");
            }

            // If a WindowsUser object was not returned, throw an exception
            // indicating the service does not exist
            if (LogOnUser == null)
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
            Log.Write("Getting the service user.");
            WindowsUser user = null;

            if (IsInstalled())
            {
                Log.Write("The Plex service is installed. Let's get the user associated with the service.");

                // Set the service property to be used to find the service
                // using the ManagementObject
                string serviceProperty = "Name";
                if (_usingDisplayName)
                {
                    serviceProperty = "DisplayName";
                }

                try
                {
                    // The query to get the service that matches either the name or display name
                    SelectQuery sQuery = 
                        new SelectQuery(
                            $"SELECT StartName FROM Win32_Service where {serviceProperty} = '{ServiceName}'");

                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(sQuery))
                    {
                        if (searcher == null)
                        {
                            Log.Write("The service user could not be found.");
                            return null;
                        }
                        
                        using (ManagementObjectCollection services = searcher.Get())
                        {
                            if (services == null || services.Count == 0)
                            {
                                Log.Write("The service user could not be found.");
                                return null;
                            }

                            foreach (ManagementObject service in searcher.Get())
                            {
                                user = new WindowsUser(
                                        service["startname"].ToString().Replace(@".\", $"{MachineName}\\"));

                                Log.Write($"The Plex service user: {user.Name}.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Write($"Could not get the service user. Reason: {e.Message}");
                }
            }
            else
            {
                Log.Write("The Plex service is not installed.");
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
            try
            {
                bool isInstalled =
                    ServiceController.GetServices().Any(s => s.ServiceName == ServiceName);

                // If the service could not be found using the service name,
                // try to get the service using the display name, and set the
                // display name flag depending on the result
                if (!isInstalled)
                {
                    isInstalled = 
                        ServiceController.GetServices().Any(s => s.DisplayName == ServiceName);
                    _usingDisplayName = isInstalled;
                }

                return isInstalled;

            }
            catch (System.ComponentModel.Win32Exception e)
            {
                Log.Write($"Couldn't check if service is installed. Reason: {e.Message}");
                return false;
            }
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
