/*
 * Created by SharpDevelop.
 * User: Paul
 * Date: 2/22/2016
 * Time: 7:50 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace TE.Plex
{
	partial class MainForm
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnUpdate;
		private System.Windows.Forms.GroupBox grpUpdateStatus;
		private System.Windows.Forms.TextBox txtUpdateStatus;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label lblLatestVersion;
		private System.Windows.Forms.Label lblInstalledVersion;
		private System.Windows.Forms.Label lblLatestVersionLabel;
		private System.Windows.Forms.Label lblInstalledVersionLabel;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnUpdate = new System.Windows.Forms.Button();
			this.grpUpdateStatus = new System.Windows.Forms.GroupBox();
			this.txtUpdateStatus = new System.Windows.Forms.TextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lblLatestVersion = new System.Windows.Forms.Label();
			this.lblInstalledVersion = new System.Windows.Forms.Label();
			this.lblLatestVersionLabel = new System.Windows.Forms.Label();
			this.lblInstalledVersionLabel = new System.Windows.Forms.Label();
			this.grpUpdateStatus.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.Location = new System.Drawing.Point(475, 398);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
			// 
			// btnUpdate
			// 
			this.btnUpdate.Location = new System.Drawing.Point(394, 398);
			this.btnUpdate.Name = "btnUpdate";
			this.btnUpdate.Size = new System.Drawing.Size(75, 23);
			this.btnUpdate.TabIndex = 2;
			this.btnUpdate.Text = "Update";
			this.btnUpdate.UseVisualStyleBackColor = true;
			this.btnUpdate.Click += new System.EventHandler(this.BtnUpdateClick);
			// 
			// grpUpdateStatus
			// 
			this.grpUpdateStatus.Controls.Add(this.txtUpdateStatus);
			this.grpUpdateStatus.Location = new System.Drawing.Point(12, 103);
			this.grpUpdateStatus.Name = "grpUpdateStatus";
			this.grpUpdateStatus.Size = new System.Drawing.Size(538, 289);
			this.grpUpdateStatus.TabIndex = 3;
			this.grpUpdateStatus.TabStop = false;
			this.grpUpdateStatus.Text = "Update Status:";
			// 
			// txtUpdateStatus
			// 
			this.txtUpdateStatus.Location = new System.Drawing.Point(7, 20);
			this.txtUpdateStatus.Multiline = true;
			this.txtUpdateStatus.Name = "txtUpdateStatus";
			this.txtUpdateStatus.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtUpdateStatus.Size = new System.Drawing.Size(525, 263);
			this.txtUpdateStatus.TabIndex = 0;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.lblLatestVersion);
			this.groupBox1.Controls.Add(this.lblInstalledVersion);
			this.groupBox1.Controls.Add(this.lblLatestVersionLabel);
			this.groupBox1.Controls.Add(this.lblInstalledVersionLabel);
			this.groupBox1.Location = new System.Drawing.Point(13, 13);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(537, 84);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Plex Media Server Information";
			// 
			// lblLatestVersion
			// 
			this.lblLatestVersion.Location = new System.Drawing.Point(112, 43);
			this.lblLatestVersion.Name = "lblLatestVersion";
			this.lblLatestVersion.Size = new System.Drawing.Size(100, 23);
			this.lblLatestVersion.TabIndex = 3;
			this.lblLatestVersion.Text = "[]";
			// 
			// lblInstalledVersion
			// 
			this.lblInstalledVersion.Location = new System.Drawing.Point(112, 20);
			this.lblInstalledVersion.Name = "lblInstalledVersion";
			this.lblInstalledVersion.Size = new System.Drawing.Size(100, 23);
			this.lblInstalledVersion.TabIndex = 2;
			this.lblInstalledVersion.Text = "[]";
			// 
			// lblLatestVersionLabel
			// 
			this.lblLatestVersionLabel.Location = new System.Drawing.Point(6, 43);
			this.lblLatestVersionLabel.Name = "lblLatestVersionLabel";
			this.lblLatestVersionLabel.Size = new System.Drawing.Size(100, 23);
			this.lblLatestVersionLabel.TabIndex = 1;
			this.lblLatestVersionLabel.Text = "Latest Version:";
			// 
			// lblInstalledVersionLabel
			// 
			this.lblInstalledVersionLabel.Location = new System.Drawing.Point(7, 20);
			this.lblInstalledVersionLabel.Name = "lblInstalledVersionLabel";
			this.lblInstalledVersionLabel.Size = new System.Drawing.Size(99, 23);
			this.lblInstalledVersionLabel.TabIndex = 0;
			this.lblInstalledVersionLabel.Text = "Installed Version:";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(563, 431);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.grpUpdateStatus);
			this.Controls.Add(this.btnUpdate);
			this.Controls.Add(this.btnCancel);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MainForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Plex Server Updater";
			this.grpUpdateStatus.ResumeLayout(false);
			this.grpUpdateStatus.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
	}
}
