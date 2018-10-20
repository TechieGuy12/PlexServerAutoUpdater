using System;
using System.Collections.Generic;
using System.Drawing;
using static System.Environment;
using System.Threading;
using System.Threading.Tasks;
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

        /// <summary>
        /// The cancellation token source.
        /// </summary>
        private CancellationTokenSource cts = null;
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
            cts = new CancellationTokenSource();

            InitializeComponent();
            Initialize();
        }
        #endregion

        #region Events
        /// <summary>
        /// Cancels the Plex update..
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// Event-related arguments.
        /// </param>
        void BtnCancelClick(object sender, EventArgs e)
        {
            cts?.Cancel();
        }

        /// <summary>
        /// Close the form.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// Event-related arguments.
        /// </param>
        private void btnExit_Click(object sender, EventArgs e)
        {
            Log.Write("Closing the application.");
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
                btnUpdate.Enabled = false;
                btnCancel.Visible = false;
                btnExit.Enabled = false;

                if (cts == null)
                {
                    cts = new CancellationTokenSource();
                }

                CancellationToken ct = cts.Token;

                Task plexUpdate = Task.Factory.StartNew(() =>
                {
                    // Throw an exception if already cancelled
                    ct.ThrowIfCancellationRequested();

                    server.Update();
                }, cts.Token);

                plexUpdate.Wait();
            }
            catch (Exception ex)
            {
                txtUpdateStatus.Text += $"ERROR: {ex.Message}{NewLine}";
                Log.Write(ex);
            }
            finally
            {
                cts?.Dispose();
                Initialize();
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
                Log.Write("Closing the application.");
                Close();
            }
        }

        /// <summary>
        /// The messages from the update execution.
        /// </summary>
        /// <param name="sender">
        /// The sender object.
        /// </param>
        /// <param name="message">
        /// The message to display on the form.
        /// </param>
        private void ServerUpdateMessage(object sender, string message)
        {
            txtUpdateStatus.Text += $"{message}{NewLine}";
            Log.Write(message);
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
                Log.Write("Initializing the Plex media server object.");
                server = new MediaServer();

                if (server == null)
                {
                    Log.Write(
                        "The Plex media server object could not be initialized. Setting the flag to close the application.");
                    ToBeClosed = true;
                    return;
                }

                server.UpdateMessage += ServerUpdateMessage;

                lblInstalledVersion.Text = server.CurrentVersion.ToString();
                lblLatestVersion.Text = server.LatestVersion.ToString();

                if (server.PlayCount >= 0)
                {
                    lblPlayCount.Text = server.PlayCount.ToString();
                }
                else
                {
                    lblPlayCount.Text = "Unknown";
                }

                if (server.LatestVersion > server.CurrentVersion)
                {
                    btnUpdate.Visible = true;
                    btnCancel.Visible = false;
                    btnExit.Enabled = true;
            }
                else
                {
                    btnUpdate.Visible = false;
                    btnCancel.Visible = false;
                    btnExit.Enabled = true;
                }
            }
            catch (LocalSystem.Msi.MSIException ex)
            {
                MessageBox.Show(
                    $"MSI exception: {ex.Message}",
                    "Plex Updater Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Log.Write(ex);
                ToBeClosed = true;

            }
            catch (AppNotInstalledException ex)
            {
                MessageBox.Show(
                    "The Plex Server application is not installed.",
                    "Plex Updater Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Log.Write(ex);
                ToBeClosed = true;
            }
            catch (ServiceNotInstalledException ex)
            {
                MessageBox.Show(
                    "The Plex service is not installed.",
                    "Plex Updater Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Log.Write(ex);
                ToBeClosed = true;
            }
            catch (PlexDataFolderNotFoundException ex)
            {
                MessageBox.Show(
                    "The Plex data folder could not be found.",
                    "Plex Updater Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Log.Write(ex);
                ToBeClosed = true;
            }
            catch (LocalSystem.WindowsUserSidNotFound ex)
            {
                MessageBox.Show(
                    "The SID for the Plex service user could not be found.",
                    "Plex Updater Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Log.Write(ex);
                ToBeClosed = true;
            }

            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Plex Updater Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Log.Write(ex);
                ToBeClosed = true;
            }
        }
        #endregion
    }
}
