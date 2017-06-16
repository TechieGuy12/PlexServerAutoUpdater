using System;
using System.Collections.Generic;
using System.Drawing;
using static System.Environment;
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


        #region Properties
        /// <summary>
        /// Gets a flag indicating if the form is to be closed.
        /// </summary>
        public bool ToBeClosed { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes the form.
        /// </summary>
        public MainForm()
		{
            ToBeClosed = false;

            InitializeComponent();			
			Initialize();
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
			Close();
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
			try
			{
				server.Update();
				Initialize();
			}
			catch (Exception ex)
			{
				txtUpdateStatus.Text += $"ERROR: {ex.Message}{NewLine}";
			}
		}

        /// <summary>
        /// The form is shown.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// Arguments associated with the event.
        /// </param>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            // If the form is to be closed, then close the form.
            if (ToBeClosed)
            {
                Close();
            }
        }

        /// <summary>
        /// The messages from the update execution.
        /// </summary>
        /// <param name="message">
        /// The message to display on the form.
        /// </param>
        private void ServerUpdateMessage(string message)
		{
			txtUpdateStatus.Text += $"{message}{NewLine}";			
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
				server = new MediaServer();
				
				if (server == null)
				{
                    ToBeClosed = true;
                    return;
                }
				
				server.UpdateMessage += 
					new MediaServer.UpdateMessageHandler(ServerUpdateMessage);

				lblInstalledVersion.Text = server.CurrentVersion.ToString();
				lblLatestVersion.Text = server.LatestVersion.ToString();
				
				btnUpdate.Enabled = 
					(server.LatestVersion > server.CurrentVersion);
			}
			catch (LocalSystem.Msi.MSIException ex)
			{
				MessageBox.Show(
					$"MSI: {ex.Message}",
					"Plex Updater Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
                ToBeClosed = true;

            }
			catch(AppNotInstalledException ex)
			{
				MessageBox.Show(
					ex.Message,
					"Plex Updater Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
                ToBeClosed = true;
            }
			catch (ServiceNotInstalledException ex)
			{
				MessageBox.Show(
					ex.Message,
					"Plex Updater Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
                ToBeClosed = true;
            }
			catch (PlexDataFolderNotFoundException ex)
			{
				MessageBox.Show(
					ex.Message,
					"Plex Updater Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
                ToBeClosed = true;
            }			
			catch (LocalSystem.WindowsUserSidNotFound ex)
			{
				MessageBox.Show(
					ex.Message,
					"Plex Updater Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
                ToBeClosed = true;
            }

			catch (Exception ex)
			{
#if DEBUG
                MessageBox.Show(
                    $"{ex.Message}{NewLine}{NewLine}Inner Exception:{NewLine}{ex.InnerException}{NewLine}{NewLine}StackTrace:{NewLine}{ex.StackTrace}",
                    "Plex Updater Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop);
#else
                MessageBox.Show(
					ex.Message,
					"Plex Updater Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
#endif
                ToBeClosed = true;
            }
		}
        #endregion
    }
}
