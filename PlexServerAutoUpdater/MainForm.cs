using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TE.Plex
{
	/// <summary>
	/// The Plex Media Server Updater main form.
	/// </summary>
	public partial class MainForm : Form
	{
		#region Private Variables
		/// <summary>
		/// The media server object.
		/// </summary>
		private MediaServer server = null;
		#endregion
		
		#region Constructors
		/// <summary>
		/// Initializes the form.
		/// </summary>
		public MainForm()
		{
			InitializeComponent();			
			this.Initialize();
		}
		#endregion
		
		#region Events
		/// <summary>
		/// Close the form.
		/// </summary>
		/// <param name="sender">
		/// The sender.
		/// </param>
		/// <param name="e">
		/// Event-related arguments.
		/// </param>
		void BtnCancelClick(object sender, EventArgs e)
		{
			this.Close();
		}
		
		/// <summary>
		/// Performs the Plex Media Server update.
		/// </summary>
		/// <param name="sender">
		/// The sender.
		/// </param>
		/// <param name="e">
		/// Event-related arguments.
		/// </param>
		void BtnUpdateClick(object sender, EventArgs e)
		{
			this.server.Update();
			this.Initialize();
		}
		
		/// <summary>
		/// The messages from the update execution.
		/// </summary>
		/// <param name="message">
		/// The message to display on the form.
		/// </param>
		private void ServerUpdateMessage(string message)
		{
			this.txtUpdateStatus.Text += message + Environment.NewLine;			
		}
		#endregion
		
		#region Private Functions
		/// <summary>
		/// Initializes the values on the form.
		/// </summary>
		private void Initialize()
		{			
			try
			{				
				this.server = new MediaServer();
				
				if (this.server == null)
				{
					this.Close();
				}
				
				this.server.UpdateMessage += 
					new MediaServer.UpdateMessageHandler(ServerUpdateMessage);

				lblInstalledVersion.Text = server.CurrentVersion.ToString();
				lblLatestVersion.Text = server.LatestVersion.ToString();
				
				btnUpdate.Enabled = 
					(server.LatestVersion > server.CurrentVersion);
			}
			catch (TE.LocalSystem.Msi.MSIException ex)
			{
				MessageBox.Show("MSI:" + ex.Message);
				this.Close();
			}
			catch(AppNotInstalledException ex)
			{
				MessageBox.Show(ex.Message);
				this.Close();
			}
			catch (ServiceNotInstalledException ex)
			{
				MessageBox.Show(ex.Message);
				this.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				this.Close();
			}
		}
		#endregion
	}
}
