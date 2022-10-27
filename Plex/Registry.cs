﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Win32 = Microsoft.Win32;
using TE.LocalSystem;

namespace TE.Plex
{
    /// <summary>
    /// Contains the methods used to get the Plex data values from the Windows
    /// registry.
    /// </summary>
    internal class Registry : EventSource
    {
        /// <summary>
        /// The registry key tree for the Plex information.
        /// </summary>
        private const string RegistryPlexKey = @"SOFTWARE\Plex, Inc.\Plex Media Server\";

        /// <summary>
        /// The registry run key that starts Plex Media Server at Windows
        /// startup.
        /// </summary>
        private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// The registry value that starts Plex Media Server at Windows 
        /// startup.
        /// </summary>
        private const string RegistryRunValue = "Plex Media Server";

        /// <summary>
        /// The name of the local Plex data path registry value.
        /// </summary>
        private const string RegistryPlexDataPathValueName = "LocalAppDataPath";

        /// <summary>
        /// The location of the Plex installation.
        /// </summary>
        private const string RegistryInstallFolder = "InstallFolder";

        /// <summary>
        /// The user running the Plex server application.
        /// </summary>
        private WindowsUser user;

        /// <summary>
        /// The path to the Plex registry keys.
        /// </summary>
        private string plexRegistryPath;

        /// <summary>
        /// Creates an instance of the <see cref="Registry"/> class when
        /// provided with the user that is running the Plex server.
        /// </summary>
        /// <param name="plexUser">
        /// The user that is running Plex.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="plexUser"/> parameter is null.
        /// </exception>
        internal Registry(WindowsUser plexUser)
        {
            user = plexUser ?? throw new ArgumentNullException(nameof(plexUser));
            plexRegistryPath = $"{user.Sid}\\{RegistryPlexKey}";
        }

        /// <summary>
        /// Gets the value from the registry.
        /// </summary>
        /// <param name="name">
        /// The name of the value.
        /// </param>
        /// <returns>
        /// The value associated with the <paramref name="name"/>, or null if
        /// name is not found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="name"/> parameter is null or not provided.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The RegistryKey that contains the specified value is closed (closed keys cannot be accessed).
        /// </exception>
        /// <exception cref="SecurityException">
        /// The user does not have the permissions required to read from the registry key.
        /// </exception>
        /// <exception cref="IOException">
        /// The RegistryKey that contains the specified value has been marked for deletion.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// The user does not have the necessary registry rights.
        /// </exception>
        private object GetValue(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            using (Win32.RegistryKey plexRegistry =
                    Win32.Registry.Users.OpenSubKey(plexRegistryPath))
            {
                return plexRegistry.GetValue(name);
            }
        }

        /// <summary>
        /// Delete the Plex Server run keys for both the user that performed
        /// the installation, and the user associated with the Plex Service.
        /// </summary>
        /// <returns>
        /// True if the registry value has been deleted, false if the value
        /// could not be deleted.
        /// </returns>
        internal bool DeleteRunValue()
        {
            try
            {
                using (Win32.RegistryKey key = Win32.Registry.CurrentUser.OpenSubKey(RegistryRunKey, true))
                {
                    if (key != null)
                    {
                        try
                        {
                            if (key.GetValue(RegistryRunValue) == null)
                            {
                                return true;
                            }

                            key.DeleteValue(RegistryRunValue);
                            return (key.GetValue(RegistryRunValue) == null);
                        }
                        catch (ArgumentException)
                        {
                            return true;
                        }
                        catch (Exception ex)
                            when (ex is ObjectDisposedException || ex is IOException || ex is SecurityException || ex is UnauthorizedAccessException)
                        {
                            OnMessageChanged($"The Run key value could not be deleted. Reason: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
                when (ex is ObjectDisposedException || ex is SecurityException)
            {
                OnMessageChanged($"The Run key value could not be deleted. Reason: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Gets the location of the Plex installation folder.
        /// </summary>
        /// <returns>
        /// The location of the Plex installation folder, otherwise <c>null</c>.
        /// </returns>
        internal string GetInstallFolder()
        {
            try
            {
                // Get the Plex local data folder from the users registry hive
                // for the user ID associated with the Plex service
                return (string)GetValue(RegistryInstallFolder);
            }
            catch (Exception ex)
                when (ex is ArgumentNullException || ex is ObjectDisposedException || ex is SecurityException || ex is IOException || ex is UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the local Plex data folder used by the Plex service.
        /// </summary>
        /// <returns>
        /// The full path to the local Plex data folder.
        /// </returns>
        internal string GetLocalDataFolder()
        {
            string folder;

            try
            {
                // Get the Plex local data folder from the users registry hive
                // for the user ID associated with the Plex service
                folder = (string)GetValue(RegistryPlexDataPathValueName);               
            }
            catch (Exception ex)
                when (ex is ArgumentNullException || ex is ObjectDisposedException || ex is SecurityException || ex is IOException || ex is UnauthorizedAccessException)
            {
                OnMessageChanged($"The Plex local data folder could not be determined. Reason: {ex.Message}");
                folder = null;
            }

            if (string.IsNullOrEmpty(folder))
            {
                // Default to the standard local application data folder
                // for the Plex service user is the LocalAppDataPath value
                // is missing from the registry
                folder = user.LocalAppDataFolder;
            }

            return folder;
        }

        /// <summary>
        /// Gets the Plex token for the logged in Plex user.
        /// </summary>
        /// <returns>
        /// A Plex token or null if the token could not be retrieved.
        /// </returns>
        internal string GetToken()
        {
            try
            {
                return (string)GetValue("PlexOnlineToken");
            }
            catch (Exception ex)
                when (ex is ArgumentNullException || ex is ObjectDisposedException || ex is SecurityException || ex is IOException || ex is UnauthorizedAccessException)
            {
                OnMessageChanged($"The user token could not be determined. Reason: {ex.Message}");
                return null;
            }

        }

        /// <summary>
        /// Gets the update channel specified in Plex.
        /// </summary>
        /// <returns>
        /// An <see cref="UpdateChannel"/> value indicating which channel to
        /// use to download the update.
        /// </returns>
        internal UpdateChannel GeUpdateChannel()
        {
            string value = null;
            try
            {
                value = (string)GetValue("ButlerUpdateChannel");
            }
            catch (Exception ex)
                when (ex is ArgumentNullException || ex is ObjectDisposedException || ex is SecurityException || ex is IOException || ex is UnauthorizedAccessException)
            {
                return UpdateChannel.Public;
            }

            if (value == null)
            {
                return UpdateChannel.Public;
            }

            int updateChannel;
            if (!int.TryParse(value, out updateChannel))
            {
                return UpdateChannel.Public;
            }

            return updateChannel == (int)UpdateChannel.PlexPass ? UpdateChannel.PlexPass : UpdateChannel.Public;
        }
    }
}
