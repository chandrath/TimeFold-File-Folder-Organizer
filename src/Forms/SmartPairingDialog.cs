using System;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer.Forms
{
    public partial class SmartPairingDialog : Form
    {
        private readonly AppSettings _settings;
        private readonly bool _isDark;

        private CheckBox _chkSubtitles = null!;
        private CheckBox _chkHtml = null!;
        private Button _btnDefaults = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public bool KeepSubtitleCompanionsTogether => _chkSubtitles.Checked;
        public bool KeepHtmlCompanionsTogether => _chkHtml.Checked;

        public SmartPairingDialog(AppSettings settings, bool isDark = false)
        {
            _settings = settings;
            _isDark = isDark;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var palette = AppTheme.GetPalette(_isDark);
            this.Text = "Smart Companion Pairing Rules";
            if (AppConstants.AppIcon != null) this.Icon = AppConstants.AppIcon;
            this.Size = new Size(540, 390);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = palette.CanvasBg;
            this.ForeColor = palette.TextPrimary;
            this.Shown += (s, e) => AppTheme.SetWindowDarkTitleBar(this.Handle, _isDark);

            const int leftMargin = 22;
            int contentWidth = this.ClientSize.Width - (leftMargin * 2);
            int currentY = 18;

            // Header Section
            var lblTitle = new Label
            {
                Text = "🔗 Smart Companion Pairing",
                Font = new Font("Segoe UI Emoji", 12F, FontStyle.Bold),
                ForeColor = _isDark ? Color.FromArgb(147, 197, 253) : Color.FromArgb(30, 58, 138),
                Location = new Point(leftMargin, currentY),
                AutoSize = true
            };
            currentY += 28;

            var lblSubtitle = new Label
            {
                Text = "Preserve relationships between parent files and auxiliary assets across all organization modes.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = palette.TextMuted,
                Location = new Point(leftMargin, currentY),
                Size = new Size(contentWidth, 34)
            };
            currentY += 40;

            // Card 1: Subtitle Pairing
            var pnlSubtitles = CreateCardPanel(leftMargin, currentY, contentWidth, 86, palette);
            _chkSubtitles = new CheckBox
            {
                Text = "Pair video subtitles alongside movie files",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = palette.TextPrimary,
                Location = new Point(14, 12),
                Size = new Size(contentWidth - 28, 24),
                Checked = _settings.KeepSubtitleCompanionsTogether
            };
            var lblSubtitlesDesc = new Label
            {
                Text = "Automatically routes .srt, .vtt, .sub, .ass, .ssa, and .smi files into the same target folder as the matching movie, preventing orphaned subtitles.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = palette.TextMuted,
                Location = new Point(34, 38),
                Size = new Size(contentWidth - 52, 38)
            };
            pnlSubtitles.Controls.AddRange(new Control[] { _chkSubtitles, lblSubtitlesDesc });
            currentY += 98;

            // Card 2: HTML Page Companion Pairing
            var pnlHtml = CreateCardPanel(leftMargin, currentY, contentWidth, 86, palette);
            _chkHtml = new CheckBox
            {
                Text = "Keep saved HTML pages & asset folders paired",
                UseMnemonic = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = palette.TextPrimary,
                Location = new Point(14, 12),
                Size = new Size(contentWidth - 28, 24),
                Checked = _settings.KeepHtmlCompanionsTogether
            };
            var lblHtmlDesc = new Label
            {
                Text = "Ensures browser-saved web pages (.html, .htm) and their accompanying resource folders (_files, _data) remain together in the same destination folder.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = palette.TextMuted,
                Location = new Point(34, 38),
                Size = new Size(contentWidth - 52, 38)
            };
            pnlHtml.Controls.AddRange(new Control[] { _chkHtml, lblHtmlDesc });
            currentY += 104;

            // Bottom Buttons
            int buttonY = this.ClientSize.Height - 48;

            _btnDefaults = new Button
            {
                Text = "↺ Defaults",
                UseMnemonic = false,
                Size = new Size(95, 30),
                Location = new Point(leftMargin, buttonY),
                Font = new Font("Segoe UI Emoji", 8.5F),
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText
            };
            _btnDefaults.Click += (s, e) =>
            {
                _chkSubtitles.Checked = AppConstants.DefaultKeepSubtitleCompanionsTogether;
                _chkHtml.Checked = AppConstants.DefaultKeepHtmlCompanionsTogether;
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(85, 30),
                Location = new Point(this.ClientSize.Width - leftMargin - 85, buttonY),
                DialogResult = DialogResult.Cancel,
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText
            };

            _btnSave = new Button
            {
                Text = "Save",
                Size = new Size(85, 30),
                Location = new Point(this.ClientSize.Width - leftMargin - 85 - 10 - 85, buttonY),
                DialogResult = DialogResult.OK,
                BackColor = AppConstants.ColorPrimary,
                ForeColor = Color.White
            };
            _btnSave.Click += (s, e) =>
            {
                _settings.KeepSubtitleCompanionsTogether = _chkSubtitles.Checked;
                _settings.KeepHtmlCompanionsTogether = _chkHtml.Checked;
                _settings.SaveToFile();
            };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblSubtitle,
                pnlSubtitles, pnlHtml,
                _btnDefaults, _btnSave, _btnCancel
            });

            this.AcceptButton = _btnSave;
            this.CancelButton = _btnCancel;
        }

        private Panel CreateCardPanel(int x, int y, int width, int height, AppTheme.ThemePalette palette)
        {
            return new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = _isDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(249, 250, 251)
            };
        }
    }
}
