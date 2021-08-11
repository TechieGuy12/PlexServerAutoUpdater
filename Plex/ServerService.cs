﻿using System;
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
                ManagementObject service =
                    new ManagementObject(
                        $"Win32_Service.Name='{ServiceName}'");

                if (service == null)
                {
                    Log.Write("The service user could not be found.");
                    return null;
                }

                service.Get();
                user = new WindowsUser(service["startname"].ToString().Replace(
                    @".\", $"{MachineName}\\"));

                Log.Write($"The Plex service user: {user.Name}.");
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
