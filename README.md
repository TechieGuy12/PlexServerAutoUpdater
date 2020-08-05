# Plex Server Auto Updater

The Plex Server Auto Updater application allows the Plex Media server to be updated automatically when it is [run as a Windows service].

## What does it do?
When the Plex Server Auto Updater performs an update, the following tasks are done:

- Downloads and verifies the latest update.  
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

The easiest way to keep Plex Media Server updated is to schedule the Plex Server Auto Updater from the Windows task scheduler. You can find information about how to do this from the [How to Update Plex Automatically When Run as a Service] post on [Technically Easy] or [Updating Plex When Plex is Running as a Windows Service] on [Plexopedia].

Of course, you can use any scheduling application with Plex Server Auto Updater by running the psupdate.exe with the -silent argument.

## Waiting for streaming to complete
By default, the updater will only update the Plex server if there is no client streaming media. If there is a client streaming from the Plex server, the update will wait until the server is free.

You have a few options on how Plex is updated when media is streaming:

1. Leave the default and the updater will wait and then check the server every 30 seconds to see if the streaming has completed before performing the update.
2. From the GUI, uncheck the "Only update when not in use" checkbox, and then allow the update the go ahead regardless if Plex is streaming media.
3. You can specify the "-wait [seconds]" argument to specify how many seconds the updater will wait to check to see if the streaming as completed.
4. When running the update silently (using the -silent parameter), you can specify the -force parameter to force the update.

## Log File Location

    %LOCALAPPDATA%\Temp\plex-updater.txt

[run as a Windows service]: https://forums.plex.tv/discussion/93994/pms-as-a-service/
[latest release]: https://github.com/TechieGuy12/PlexServerAutoUpdater/releases/latest
[How to Update Plex Automatically When Run as a Service]: http://technicallyeasy.net/2016/03/update-plex-automatically-running-plex-service/
[Technically Easy]: http://technicallyeasy.net
[Updating Plex When Plex is Running as a Windows Service]: https://www.plexopedia.com/plex-media-server/windows/updating-plex-media-server-service/
[Plexopedia]: https://www.plexopedia.com
