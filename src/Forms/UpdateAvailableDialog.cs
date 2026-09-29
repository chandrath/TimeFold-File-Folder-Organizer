using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Services;

namespace FileOrganizer.Forms
{
    /// <summary>
    /// Non-modal dialog shown when a newer version of TimeFold is available.
    /// Provides Download (opens browser), Skip This Version, and Remind Me Later actions.
    /// </summary>
    public class UpdateAvailableDialog : Form
    {
        public enum UpdateAction { Download, Skip, Later }

        public UpdateAction ChosenAction { get; private set; } = UpdateAction.Later;

        private readonly UpdateService.UpdateInfo _info;
        private readonly bool _isDark;

        public UpdateAvailableDialog(UpdateService.UpdateInfo info, bool isDark = false)
        {
            _info = info;
            _isDark = isDark;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var palette = AppTheme.GetPalette(_isDark);

            this.Text = "Update Available — TimeFold";
            if (AppConstants.AppIcon != null) this.Icon = AppConstants.AppIcon;
            this.Size = new Size(480, 320);
            this.MinimumSize = new Size(420, 280);
            this.MaximumSize = new Size(620, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = palette.CanvasBg;
            this.ForeColor = palette.TextPrimary;
            this.Shown += (s, e) => AppTheme.SetWindowDarkTitleBar(this.Handle, _isDark);
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { ChosenAction = UpdateAction.Later; Close(); } };

            // Header strip
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = AppConstants.ColorPrimary,
                Padding = new Padding(16, 0, 16, 0)
            };
            var lblTitle = new Label
            {
                Text = $"✨  TimeFold {_info.Version} is available",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            var lblCurrent = new Label
            {
                Text = $"You have {AppConstants.AppVersion}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(200, 220, 255),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 0)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblCurrent);

            // Summary area
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 4)
            };

            string summaryText = string.IsNullOrWhiteSpace(_info.Summary)
                ? "A new version is available. See the release page for details."
                : _info.Summary;

            var lblSummary = new Label
            {
                Text = summaryText,
                Font = new Font("Segoe UI", 9F),
                ForeColor = palette.TextPrimary,
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft
            };

            var lnkFullNotes = new LinkLabel
            {
                Text = "View full release notes on GitHub →",
                Font = new Font("Segoe UI", 8.5F),
                Dock = DockStyle.Bottom,
                Height = 22,
                LinkColor = _isDark ? Color.FromArgb(147, 197, 253) : AppConstants.ColorPrimary,
                ActiveLinkColor = AppConstants.ColorPrimaryHover,
                Visible = _info.HasFullNotes
            };
            lnkFullNotes.LinkClicked += (s, e) => OpenReleasePage();

            pnlBody.Controls.Add(lblSummary);
            pnlBody.Controls.Add(lnkFullNotes);

            // Footer buttons
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(16, 10, 16, 10),
                BackColor = palette.CardBg
            };
            pnlFooter.Paint += (s, pe) =>
            {
                using var pen = new Pen(palette.CardBorder, 1);
                pe.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var btnDownload = new Button
            {
                Text = "⬇  Download Update",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(160, 36),
                Dock = DockStyle.Right,
                BackColor = AppConstants.ColorPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDownload.FlatAppearance.BorderSize = 0;
            btnDownload.Click += (s, e) => { ChosenAction = UpdateAction.Download; OpenReleasePage(); Close(); };

            var btnSkip = new Button
            {
                Text = "Skip This Version",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(130, 36),
                Dock = DockStyle.Left,
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSkip.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;
            btnSkip.Click += (s, e) => { ChosenAction = UpdateAction.Skip; Close(); };

            var btnLater = new Button
            {
                Text = "Remind Me Later",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(130, 36),
                Dock = DockStyle.Left,
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnLater.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;
            btnLater.Click += (s, e) => { ChosenAction = UpdateAction.Later; Close(); };

            pnlFooter.Controls.Add(btnDownload);
            pnlFooter.Controls.Add(btnSkip);
            pnlFooter.Controls.Add(btnLater);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        private void OpenReleasePage()
        {
            try { Process.Start(new ProcessStartInfo(_info.ReleasePageUrl) { UseShellExecute = true }); }
            catch { }
        }
    }
}
