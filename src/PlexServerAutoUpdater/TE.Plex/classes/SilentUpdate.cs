using System;
using System.Diagnostics;
using System.IO;
using TE.LocalSystem;

namespace TE.Plex
{
	/// <summary>
	/// Executes a silent Plex Media Server update.
	/// </summary>
	public class SilentUpdate
	{
		#region Private Variables
		/// <summary>
		/// The media server object.
		/// </summary>
		private MediaServer server = null;
		/// <summary>
		/// The log file that contains the messages regarding the update.
		/// </summary>
		private string messageLogFile;
		/// <summary>
		/// The logger.
		/// </summary>
		private Logger eventLog = null;
		/// <summary>
		/// Flag indicating that there was an issue writing a message to the
		/// message log file.
		/// </summary>
		private bool isMessageError;
		#endregion
				
		#region Constructors
		/// <summary>
		/// Initializes an instance of the <see cref="TE.Plex.SilentUpdate"/> class.
		/// </summary>
		public SilentUpdate()
		{
			this.Initialize();
		}
		#endregion
		
		#region Events
		/// <summary>
		/// Writes any messages from the Plex Media Server update to a log
		/// file.
		/// </summary>
		/// <param name="message">
		/// The message to write to the log file.
		/// </param>
		private void ServerUpdateMessage(string message)
		{
			// If an error occurred when writing an update message to the log
			// file, just return from the function without trying again
			if (isMessageError)
			{
				return;
			}
			
			if (this.server != null)
			{					
				this.isMessageError = true;
				
				this.eventLog.EntryType = EventLogEntryType.Error;
				this.eventLog.Write(
					"There was an issue connecting to the media server.");
			}
			
			if (!string.IsNullOrEmpty(this.messageLogFile))
			{	
				this.isMessageError = true;
				this.eventLog.Write(
					"The message log file was not specified. The installation log will still be written.");
			}
			
			try
			{
				using (StreamWriter sw = 
				       new StreamWriter(this.messageLogFile, true))
				{
					sw.WriteLine(message);
				}
			}
			catch (UnauthorizedAccessException)
			{
				this.isMessageError = true;
				this.eventLog.Write(string.Format(
					"Access to the message log file is denied.{0}Message log path: {1}",
					Environment.NewLine,
					this.messageLogFile));		
			}
			catch (DirectoryNotFoundException)
			{
				this.isMessageError = true;
				this.eventLog.Write(string.Format(
					"The message file directory could not be found.{0}Message log path: {1}",
					Environment.NewLine,
					this.messageLogFile));					
			}
			catch (PathTooLongException)
			{
				this.isMessageError = true;
				this.eventLog.Write(string.Format(
					"The message log path is too long. The total length of the path must be less than 260 characters.{0}Message log path: {1}",
					Environment.NewLine,
					this.messageLogFile));					
			}			
			catch (IOException)
			{
				this.isMessageError = true;
				this.eventLog.Write(string.Format(
					"The message log path is invalid.{0}Message log path: {1}",
					Environment.NewLine,
					this.messageLogFile));					
			}
			catch (System.Security.SecurityException)
			{
				this.isMessageError = true;
				this.eventLog.Write(string.Format(
					"The user does not have the required permissions to write to the message file.{0}Message log path: {1}",
					Environment.NewLine,
					this.messageLogFile));					
			}
			catch (Exception ex)
			{
				this.isMessageError = true;
				this.eventLog.Write(string.Format(
					"An error occurred trying to write to the message log:{0}{1}.{2}Message log path: {3}",
					Environment.NewLine,
					ex.Message,
					Environment.NewLine,
					this.messageLogFile));					
			}
		}
		#endregion
		
		#region Private Functions		
		/// <summary>
		/// Initializes the properties and variables for the class.
		/// </summary>
		private void Initialize()
		{	
			this.eventLog = new Logger("Plex Media Server Updater");
			
			try
			{
				this.server = new MediaServer(true);
			}
			catch (AppNotInstalledException)
			{
				this.eventLog.Write(
					"The Plex Media Server is not installed.");
				return;
			}
			catch (ServiceNotInstalledException)
			{
				this.eventLog.Write(
					"The Plex Media Server service is not installed.");
				return;				
			}
			catch (Exception ex)
			{
				this.eventLog.Write(string.Format(
					"An error occurred:{0}{1}",
					Environment.NewLine,
					ex.Message));
				return;				
			}
			
			this.server.UpdateMessage += 
				new MediaServer.UpdateMessageHandler(ServerUpdateMessage);
			
			this.messageLogFile = this.server.GetMessageLogFilePath();
			this.isMessageError = (this.messageLogFile.Length > 0);
			
			if (!string.IsNullOrEmpty(this.messageLogFile))
			{
				// If the message log file exists, attempt to delete it
				if (File.Exists(this.messageLogFile))
				{
					try
					{
						File.Delete(this.messageLogFile);
					}
					catch (PathTooLongException)
					{
						this.eventLog.Write(string.Format(
							"The message log file path is too long.{0}Message log path:  {1}",
							Environment.NewLine,
							this.messageLogFile));
						
						this.isMessageError = false;
						this.messageLogFile = string.Empty;
					}					
					catch (IOException)
					{
						this.eventLog.Write(string.Format(
							"The message log file is in use. The messages won't be written to the log file but the installation log will still be written.{0}Message log path:  {1}",
							Environment.NewLine,
							this.messageLogFile));

						this.isMessageError = false;						
						this.messageLogFile = string.Empty;
					}
					catch (NotSupportedException)
					{
						this.eventLog.Write(string.Format(
							"The message log path is invalid.{0}Message log path: {1}",
							Environment.NewLine,
							this.messageLogFile));

						this.isMessageError = false;						
						this.messageLogFile = string.Empty;
					}
					catch (UnauthorizedAccessException)
					{
						this.eventLog.Write(string.Format(
							"The message log path cannot be accessed.{0}Message log path: {1}",
							Environment.NewLine,
							this.messageLogFile));
						
						this.isMessageError = false;						
						this.messageLogFile = string.Empty;
					}
				}
			}
		}
		#endregion
		
		#region Public Functions
		/// <summary>
		/// Runs the Plex Media Server update.
		/// </summary>
		public void Run()
		{
			if (this.server.IsUpdateAvailable())
			{						
				this.server.Update();
			}	
		}
		#endregion
	}
}
