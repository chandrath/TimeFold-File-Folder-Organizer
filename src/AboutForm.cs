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
            this.Text = "About File Organizer by Date";
            this.Size = new Size(500, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            var lblTitle = new Label
            {
                Text = "File Organizer by Date",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                Location = new Point(20, 20),
                AutoSize = true
            };
            
            var lblVersion = new Label
            {
                Text = "Version 1.0.0",
                Font = new Font("Segoe UI", 10),
                Location = new Point(20, 60),
                AutoSize = true
            };
            
            var lblDescription = new Label
            {
                Text = "A Windows application that automatically organizes files into month-year folders based on their Modified Date.",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 90),
                Size = new Size(440, 60),
                AutoSize = false
            };
            
            var lblCreatedBy = new Label
            {
                Text = "Created by: [Your Name]",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 160),
                AutoSize = true
            };
            
            var lblSourceCode = new Label
            {
                Text = "Source Code:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(20, 190),
                AutoSize = true
            };
            
            var linkSource = new LinkLabel
            {
                Text = "https://github.com/yourusername/file-organizer",
                Location = new Point(20, 210),
                AutoSize = true,
                ActiveLinkColor = Color.Blue,
                LinkColor = Color.Blue,
                VisitedLinkColor = Color.Blue
            };
            linkSource.LinkClicked += (s, e) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = linkSource.Text,
                    UseShellExecute = true
                });
            };
            
            var lblCopyright = new Label
            {
                Text = "© 2024 - Free and Open Source Software",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(20, 250),
                AutoSize = true
            };
            
            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(100, 30),
                Location = new Point(380, 280),
                DialogResult = DialogResult.OK
            };
            
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblVersion);
            this.Controls.Add(lblDescription);
            this.Controls.Add(lblCreatedBy);
            this.Controls.Add(lblSourceCode);
            this.Controls.Add(linkSource);
            this.Controls.Add(lblCopyright);
            this.Controls.Add(btnClose);
        }
    }
}

