using System;
using System.Diagnostics;

namespace TE.LocalSystem
{
	/// <summary>
	/// Manages the logging of information.
	/// </summary>
	public class Logger
	{
		#region Properties
		/// <summary>
		/// The event source that is registered on the machine.
		/// </summary>
		public string EventSource { get; set; }
		/// <summary>
		/// The name of the even log.
		/// </summary>
		public string LogName { get; set; }
		/// <summary>
		/// The type of event entry.
		/// </summary>
		public EventLogEntryType EntryType { get; set; }
		#endregion
		
		#region Constructors
		/// <summary>
		/// Initializes an instance of the <see cref="Logger"/>
		/// class when provided with the event source..
		/// </summary>
		public Logger(string eventSource)
		{
			Initialize();
			EventSource = eventSource;
		}
		#endregion
		
		#region Private Functions
		/// <summary>
		/// Initializes the values in the class.
		/// </summary>
		private void Initialize()
		{
			EventSource = string.Empty;
			LogName = "Application";
			EntryType = EventLogEntryType.Information;
		}
		#endregion
		
		#region Public Functions
		public void Write(string message)
		{
			if (string.IsNullOrEmpty(EventSource))
			{
				return;
			}
			
			if (string.IsNullOrEmpty(LogName))
			{
				return;
			}
			
			try
			{
				if (!EventLog.SourceExists(EventSource))
				{
					EventLog.CreateEventSource(EventSource, LogName);
				}
				
				EventLog.WriteEntry(EventSource, message, EntryType);
			}
			catch (System.Security.SecurityException)
			{
				return;
			}
		}
		#endregion
	}
}
