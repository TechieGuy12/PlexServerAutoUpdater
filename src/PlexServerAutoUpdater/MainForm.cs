using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TE.LocalSystem;

namespace TE.Plex
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		private MediaServer server = null;
		
		public MainForm()
		{
			InitializeComponent();			
			this.Initialize();
		}
		#region Events
		void BtnCancelClick(object sender, EventArgs e)
		{
			this.Close();
		}

		void BtnUpdateClick(object sender, EventArgs e)
		{
			this.server.Update();
		}
		
		private void ServerUpdateMessage(string message)
		{
			this.txtUpdateStatus.Text += message + Environment.NewLine;
		}
		#endregion
		
		#region Private Functions
		private void Initialize()
		{			
			try
			{					
				this.server = new MediaServer();
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
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}			
		}

		#endregion
	}
}
