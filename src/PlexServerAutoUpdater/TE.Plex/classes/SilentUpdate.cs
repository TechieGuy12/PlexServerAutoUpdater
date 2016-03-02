using System;
using System.IO;

namespace TE.Plex
{
	/// <summary>
	/// Description of SilentUpdate.
	/// </summary>
	public class SilentUpdate
	{
		private MediaServer server = null;
		
		public SilentUpdate()
		{
			this.Initialize();
		}
		
		/// <summary>
		/// Writes any messages from the Plex Media Server update to a log
		/// file.
		/// </summary>
		/// <param name="message">
		/// The message to write to the log file.
		/// </param>
		private void ServerUpdateMessage(string message)
		{
			if (this.server != null)
			{
				string messageFile = this.server.GetMessageLogFilePath();
				
				if (!string.IsNullOrEmpty(messageFile))
				{					
					try
					{
						using (StreamWriter sw = new StreamWriter(messageFile, true))
						{
							sw.WriteLine(message);
						}
					}
					catch {}
				}
			}
		}

		private void Initialize()
		{
			this.server = new MediaServer(true);
			this.server.UpdateMessage += 
				new MediaServer.UpdateMessageHandler(ServerUpdateMessage);							
		}
		
		public void Run()
		{
			if (this.server.IsUpdateAvailable())
			{						
				this.server.Update();
			}	
		}
	}
}
