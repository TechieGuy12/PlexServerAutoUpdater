# Plex Server Auto Updater

The Plex Server Auto Updater application allows the Plex Media server to be updated automatically when it is [run as a Windows service].

## What does it do?
When the Plex Server Auto Updater performs an update, the following tasks are done:
  
- Stops the Plex service.
- Stops any Plex processes that are running.
- Installs the latest update.
- Deletes the Run keys from the registry to prevent Plex from running outside of the service.
- Stops any Plex processes that are running after the update.
- Restarts the Plex service.

## Installation
The auto updater is easy to install, in fact, there isn't an install. It is a portable application and can be run from anywhere on the machine that has the Plex service installed. 

To use the Plex Server Auto Updater, use the following steps:

- Download the [latest release].
- Extract the psupdate.exe from the zip file into any directory.
- Double-click the executable and click the "Update" button to update the Plex Media Server.

## Scheduling a silent, automatic update
The Plex Server Auto Updater can be run silently from any commandline using the following:

    psupdate.exe -silent

The easiest way to keep Plex Media Server updated is to schedule the Plex Server Auto Updater from the Windows task scheduler. You can find information about how to do this from the [How to Update Plex Automatically When Run as a Service] post on [Technically Easy].

Of course, you can use any scheduling application with Plex Server Auto Updater by running the psupdate.exe with the -silent argument.

[run as a Windows service]: https://forums.plex.tv/discussion/93994/pms-as-a-service/
[latest release]: https://github.com/TechieGuy12/PlexServerAutoUpdater/releases/latest
[How to Update Plex Automatically When Run as a Service]: http://technicallyeasy.net/2016/03/update-plex-automatically-running-plex-service/
[Technically Easy]: http://technicallyeasy.net
