using System;
using System.Drawing;
using System.Windows.Forms;

namespace FileOrganizer
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.Text = $"About {Config.AppConstants.ShortAppName}";
            this.Size = new Size(520, 410);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            var lblTitle = new Label
            {
                Text = Config.AppConstants.AppName,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138),
                Location = new Point(20, 18),
                AutoSize = true
            };
            
            var lblVersion = new Label
            {
                Text = $"Version {Config.AppConstants.AppVersion} (Build {Config.AppConstants.BuildNumber})",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(75, 85, 99),
                Location = new Point(20, 54),
                AutoSize = true
            };
            
            var lblDescription = new Label
            {
                Text = Config.AppConstants.AppDescription,
                Font = new Font("Segoe UI", 9),
                ForeColor = Config.AppConstants.ColorTextDark,
                Location = new Point(20, 84),
                Size = new Size(460, 50),
                AutoSize = false
            };
            
            var lblDeveloperHeader = new Label
            {
                Text = "Developer:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(20, 144),
                AutoSize = true
            };

            var lblDeveloperName = new Label
            {
                Text = Config.AppConstants.Author,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Config.AppConstants.ColorTextDark,
                Location = new Point(20, 164),
                AutoSize = true
            };
            
            var lblSourceCode = new Label
            {
                Text = "Project URL:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(20, 196),
                AutoSize = true
            };
            
            var linkSource = new LinkLabel
            {
                Text = Config.AppConstants.RepositoryUrl,
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 216),
                AutoSize = true,
                ActiveLinkColor = Color.FromArgb(37, 99, 235),
                LinkColor = Color.FromArgb(37, 99, 235),
                VisitedLinkColor = Color.FromArgb(37, 99, 235)
            };
            linkSource.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = linkSource.Text,
                        UseShellExecute = true
                    });
                }
                catch { }
            };
            
            var lblCopyright = new Label
            {
                Text = Config.AppConstants.CopyrightText,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(20, 260),
                AutoSize = true
            };
            
            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(100, 32),
                Location = new Point(380, 310),
                DialogResult = DialogResult.OK
            };
            
            this.Controls.AddRange([
                lblTitle, lblVersion, lblDescription, lblDeveloperHeader,
                lblDeveloperName, lblSourceCode, linkSource, lblCopyright, btnClose
            ]);
        }
    }
}

