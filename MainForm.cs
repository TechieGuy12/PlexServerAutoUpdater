using System;
using System.Collections.Generic;
using System.Drawing;
using static System.Environment;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
        private MediaServer _server = null;

        /// <summary>
        /// The cancellation token source.
        /// </summary>
        private CancellationTokenSource _cts = null;

        /// <summary>
        /// The wait timer.
        /// </summary>
        private System.Timers.Timer _timer = null;
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
            _cts?.Cancel();
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
            if (CheckIfCanUpdate())
            {
                PerformUpdate();
            }
        }

        /// <summary>
        /// Enables or disables the controls and timers on the form.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// Event-related arguments.
        /// </param>
        private void chkWait_CheckedChanged(object sender, EventArgs e)
        {
            lblCheckEveryLabel.Enabled = chkWait.Checked;
            lblCheckSecondsLabel.Enabled = chkWait.Checked;
            numSeconds.Enabled = chkWait.Checked;
            if (_timer != null)
            {
                _timer.Enabled = chkWait.Checked;
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

        /// <summary>
        /// The timer has elapsed so check the play count to see if the server
        /// is in use.
        /// </summary>
        /// <param name="source">
        /// The sender.
        /// </param>
        /// <param name="e"
        /// Elapsed event arguments.
        /// ></param>
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (_server == null)
            {
                _timer.Enabled = false;
                return;
            }

            if (CheckIfCanUpdate())
            {
                PerformUpdate();
            }
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Checks to see if the server can be updated at this time.
        /// </summary>
        private bool CheckIfCanUpdate()
        {
            if (_server == null)
            {
                txtUpdateStatus.Text += "The server was not specified. Cannot perform the update.";
                Log.Write("The server was not specified. Cannot perform the update.");
                _timer.Enabled = false;
                return false;
            }

            int playCount = _server.GetPlayCount();
            int inProgressRecordingCount = _server.GetInProgressRecordingCount();

            // No item is currently being played
            if (playCount == 0 && inProgressRecordingCount == 0)
            {
                txtUpdateStatus.Text += "The server is not in use continuing to perform the update.";
                Log.Write("The server is not in use continuing to perform the update.");
                lblPlayCount.Text = playCount.ToString();
                lblInProgressRecordingCount.Text = inProgressRecordingCount.ToString();
                btnUpdate.Enabled = true;
                _timer.Enabled = false;
                return true;
            }
            // At least one item is being played
            else if (playCount > 0 || inProgressRecordingCount > 0)
            {
                lblPlayCount.Text = playCount.ToString();
                lblInProgressRecordingCount.Text = inProgressRecordingCount.ToString();
                if (chkWait.Checked)
                {
                    txtUpdateStatus.Text += "Waiting for the server to be free has been enabled. Server update can begin. Waiting for all media and/or in progress recordings to be stopped.";
                    Log.Write("The server is in use. Waiting for all media and/or in progress recordings to be stopped before performing the update.");
                    btnUpdate.Enabled = false;
                    _timer.Interval =
                        Convert.ToDouble(Math.Abs(numSeconds.Value) * 1000);
                    _timer.Enabled = true;
                    return false;
                }
                else if (!chkWait.Checked && inProgressRecordingCount > 0)
                {
                    txtUpdateStatus.Text += "The wait option has been disabled, but you cannot update the server while there is a recording in progress. Waiting for all in progress recordings to be stopped.";
                    Log.Write("The server is in use. Waiting for all media and/or in progress recordings to be stopped before performing the update.");
                    btnUpdate.Enabled = false;
                    _timer.Interval =
                        Convert.ToDouble(Math.Abs(numSeconds.Value) * 1000);
                    _timer.Enabled = true;
                    return false;
                }
                else
                {
                    txtUpdateStatus.Text += "The wait option has been disabled. You can go ahead and update the server.";
                    Log.Write("The wait option has been disabled. You can go ahead and update the server.");
                    btnUpdate.Enabled = true;
                    _timer.Enabled = false;
                    return true;
                }
            }
            // Could not determine how many items are being played
            else
            {
                txtUpdateStatus.Text += "The server in use status could not be determined. The server can be updated if you wish.";
                Log.Write("The server in use status could not be determined. The server can be updated if you wish.");
                lblPlayCount.Text = "Unknown";
                lblInProgressRecordingCount.Text = "Unknown";
                btnUpdate.Enabled = true;
                _timer.Enabled = false;
                return true;
            }
        }

        /// <summary>
        /// Initializes the values on the form.
        /// </summary>
        private void Initialize()
        {
            try
            {
                ToBeClosed = false;
                _cts = new CancellationTokenSource();

                Log.Write("Initializing the timer object.");
                _timer = new System.Timers.Timer();
                _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                _timer.Enabled = false;
                _timer.Interval = Convert.ToDouble(numSeconds.Value * 1000);

                Log.Write("Initializing the Plex media server object.");
                _server = new MediaServer();

                if (_server == null)
                {
                    Log.Write(
                        "The Plex media server object could not be initialized. Setting the flag to close the application.");
                    ToBeClosed = true;
                    return;
                }

                _server.UpdateMessage += ServerUpdateMessage;

                lblInstalledVersion.Text = _server.CurrentVersion.ToString();
                lblLatestVersion.Text = _server.LatestVersion.ToString();

                if (_server.LatestVersion > _server.CurrentVersion)
                {
                    btnUpdate.Visible = true;
                    btnCancel.Visible = false;
                    btnExit.Enabled = true;
                    CheckIfCanUpdate();
                }
                else
                {
                    btnUpdate.Visible = false;
                    btnCancel.Visible = false;
                    btnExit.Enabled = true;
                    if (_server.GetPlayCount() >= 0 || _server.GetInProgressRecordingCount() >= 0)
                    {
                        lblPlayCount.Text = _server.PlayCount.ToString();
                        lblInProgressRecordingCount.Text = _server.InProgressRecordingCount.ToString();
                    }
                    else
                    {
                        lblPlayCount.Text = "Unknown";
                        lblInProgressRecordingCount.Text = "Unknown";
                    }
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

        /// <summary>
        /// Perform the server update.
        /// </summary>
        private void PerformUpdate()
        {
            try
            {
                btnUpdate.Enabled = false;
                btnCancel.Visible = false;
                btnExit.Enabled = false;

                if (_cts == null)
                {
                    _cts = new CancellationTokenSource();
                }

                CancellationToken ct = _cts.Token;

                Task plexUpdate = Task.Factory.StartNew(() =>
                {
                    // Throw an exception if already cancelled
                    ct.ThrowIfCancellationRequested();

                    _server.Update();
                }, _cts.Token);

                plexUpdate.Wait();

                if (!_server.IsRunning())
                {
                    Log.Write("The Plex server was not started successfully.");
                }
            }
            catch (Exception ex)
            {
                txtUpdateStatus.Text += $"ERROR: {ex.Message}{NewLine}";
                Log.Write(ex);
            }
            finally
            {
                _cts?.Dispose();
                Initialize();
            }
        }
        #endregion


    }
}
