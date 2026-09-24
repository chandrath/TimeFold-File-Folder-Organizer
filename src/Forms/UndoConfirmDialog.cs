using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Controls;
using FileOrganizer.Models;

namespace FileOrganizer.Forms
{
    public class UndoConfirmDialog : Form
    {
        private readonly UndoSession _session;
        private readonly UndoPreflightReport _preflight;
        private readonly string _safeFolderName;
        private readonly string _safeFolderPath;

        private RadioButton _rbOriginal = null!;
        private RadioButton _rbDedicated = null!;
        private Label _lblDedicatedPath = null!;

        public bool UseDedicatedFolder => _rbDedicated.Checked;
        public string DedicatedFolderPath => _safeFolderPath;

        public UndoConfirmDialog(UndoSession session, UndoPreflightReport preflight, bool isDarkMode)
        {
            _session = session;
            _preflight = preflight;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_hh-mmtt");
            _safeFolderName = $"Restored_{timestamp}";
            _safeFolderPath = Path.Combine(_session.SourceDirectory, _safeFolderName);

            InitializeComponent(isDarkMode);
        }

        private void InitializeComponent(bool isDarkMode)
        {
            var palette = AppTheme.GetPalette(isDarkMode);

            this.Text = "Undo Organization (Beta)";
            this.Size = new Size(540, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = palette.CanvasBg;
            this.ForeColor = palette.TextPrimary;
            this.Font = new Font("Segoe UI", 9F);

            // Windows native dark title bar
            AppTheme.SetWindowDarkTitleBar(this.Handle, isDarkMode);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(20, 16, 20, 16),
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Preflight Stats Card
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: Choice GroupBox
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 3: Advisory Note
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // 4: Actions

            // 0. Title & Subtitle
            var pnlTitle = new Panel { AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
            var lblTitle = new Label
            {
                Text = "↩ Undo Organization (Beta)",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = palette.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var lblSubtitle = new Label
            {
                Text = "Review and choose how you would like to restore your files.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = palette.TextMuted,
                AutoSize = true,
                Location = new Point(0, 24)
            };
            pnlTitle.Controls.AddRange([lblTitle, lblSubtitle]);
            mainLayout.Controls.Add(pnlTitle, 0, 0);

            // 1. Stats Card
            var pnlStats = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = palette.CardBg,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 0, 0, 12)
            };
            var lblStats = new Label
            {
                Text = $"✔ Items Ready to Restore:  {_preflight.ReadyToRestore} of {_preflight.TotalItems}\n" +
                       $"📂 Source Directory:        {_session.SourceDirectory}\n" +
                       (_preflight.MissingAtDestination > 0 ? $"⚠ Missing at Destination:  {_preflight.MissingAtDestination} (will be skipped)\n" : "") +
                       (_preflight.OriginalPathCollisions > 0 ? $"🛡 Collisions at Source:   {_preflight.OriginalPathCollisions} (will append '(Restored)')\n" : ""),
                Font = new Font("Segoe UI", 9F),
                ForeColor = palette.TextPrimary,
                AutoSize = true,
                Dock = DockStyle.Top
            };
            pnlStats.Controls.Add(lblStats);
            mainLayout.Controls.Add(pnlStats, 0, 1);

            // 2. Location Choice Group
            var grpLocation = new GroupBox
            {
                Text = " Restoration Destination ",
                Dock = DockStyle.Top,
                AutoSize = true,
                ForeColor = palette.TextPrimary,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(14, 12, 14, 14),
                Margin = new Padding(0, 0, 0, 10)
            };

            var pnlRadio = new Panel { Dock = DockStyle.Top, AutoSize = true };

            _rbOriginal = new RadioButton
            {
                Text = "Restore back to original locations (Default / In-place Undo)",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = palette.TextPrimary,
                Checked = true,
                AutoSize = true,
                Location = new Point(4, 4)
            };

            _rbDedicated = new RadioButton
            {
                Text = "Restore into a safe, dedicated folder (Prevents source clutter)",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = palette.TextPrimary,
                AutoSize = true,
                Location = new Point(4, 30)
            };

            _lblDedicatedPath = new Label
            {
                Text = $"📁 {_safeFolderPath}",
                Font = new Font("Segoe UI", 8F),
                ForeColor = palette.TextMuted,
                AutoSize = true,
                Location = new Point(24, 52)
            };

            pnlRadio.Controls.AddRange([_rbOriginal, _rbDedicated, _lblDedicatedPath]);
            grpLocation.Controls.Add(pnlRadio);
            mainLayout.Controls.Add(grpLocation, 0, 2);

            // 3. Advisory Note
            var lblAdvisory = new Label
            {
                Text = "ℹ Advisory: Please ensure files are closed in other programs (e.g., Office, PDF readers). Any empty folders created by TimeFold will be safely cleaned up.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = palette.TextMuted,
                Dock = DockStyle.Fill
            };
            mainLayout.Controls.Add(lblAdvisory, 0, 3);

            // 4. Action Buttons
            var pnlActions = new Panel { Dock = DockStyle.Fill };
            var btnConfirm = new ModernButton
            {
                Text = "↩ Start Undo",
                Size = new Size(125, 34),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BorderRadius = 6,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            btnConfirm.Location = new Point(pnlActions.Width - 235, 4);
            btnConfirm.Anchor = AnchorStyles.Right | AnchorStyles.Top;

            var btnCancel = new ModernButton
            {
                Text = "Cancel",
                Size = new Size(95, 34),
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText,
                BorderColor = palette.SecondaryButtonBorder,
                Font = new Font("Segoe UI", 9F),
                BorderRadius = 6,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Location = new Point(pnlActions.Width - 100, 4);
            btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;

            pnlActions.Controls.AddRange([btnConfirm, btnCancel]);
            mainLayout.Controls.Add(pnlActions, 0, 4);

            this.Controls.Add(mainLayout);
            this.AcceptButton = btnConfirm;
            this.CancelButton = btnCancel;
        }
    }
}
