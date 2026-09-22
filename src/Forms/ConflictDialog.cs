using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer.Forms
{
    public class ConflictDialog : Form
    {
        private readonly List<ConflictInfo> _conflicts;
        private readonly bool _isDark;
        public ConflictResolutionStrategy SelectedStrategy { get; private set; } = ConflictResolutionStrategy.AutoRename;

        public ConflictDialog(List<ConflictInfo> conflicts, bool isDark = false)
        {
            _conflicts = conflicts;
            _isDark = isDark;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var palette = AppTheme.GetPalette(_isDark);
            this.Text = "Potential Conflicts & Collisions Detected";
            if (AppConstants.AppIcon != null) this.Icon = AppConstants.AppIcon;
            this.Size = new Size(880, 520);
            this.MinimumSize = new Size(720, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = palette.CanvasBg;
            this.ForeColor = palette.TextPrimary;
            this.Shown += (s, e) => AppTheme.SetWindowDarkTitleBar(this.Handle, _isDark);

            // Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                Padding = new Padding(18, 10, 18, 8),
                BackColor = palette.CardBg
            };

            var lblTitle = new Label
            {
                Text = $"⚠ {_conflicts.Count} Collision / Conflict{( _conflicts.Count == 1 ? "" : "s")} Detected",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = _isDark ? Color.FromArgb(252, 211, 77) : Color.FromArgb(180, 83, 9),
                Dock = DockStyle.Top,
                Height = 26
            };

            var lblSubtitle = new Label
            {
                Text = "Existing destination folders found. TimeFold will NEVER touch or overwrite your existing folders.\nNew files will be saved into separate 'Folder (1)' subfolders, keeping your original folders 100% intact.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = palette.TextMuted,
                Dock = DockStyle.Fill
            };

            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblTitle);

            // Bottom Buttons Panel
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = palette.CardBg
            };

            var btnAutoRename = new Button
            {
                Text = "🛡 Create Folder (1) + Safe Rename",
                DialogResult = DialogResult.OK,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(250, 32),
                Location = new Point(pnlBottom.Width - 480, 12),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = AppConstants.ColorPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAutoRename.FlatAppearance.BorderSize = 0;
            btnAutoRename.Click += (s, e) => SelectedStrategy = ConflictResolutionStrategy.AutoRename;

            var btnSkip = new Button
            {
                Text = "⏭ Skip Colliding Files",
                DialogResult = DialogResult.OK,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Size = new Size(160, 32),
                Location = new Point(pnlBottom.Width - 220, 12),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSkip.FlatAppearance.BorderSize = 1;
            btnSkip.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;
            btnSkip.Click += (s, e) => SelectedStrategy = ConflictResolutionStrategy.Skip;

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Size = new Size(80, 32),
                Location = new Point(16, 12),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;

            pnlBottom.Controls.AddRange(new Control[] { btnAutoRename, btnSkip, btnCancel });

            // List View for conflicts
            var lstConflicts = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BackColor = palette.ListBg,
                ForeColor = palette.ListText,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F)
            };

            lstConflicts.Columns.Add("Item Name", 160);
            lstConflicts.Columns.Add("Type", 55);
            lstConflicts.Columns.Add("Target Folder", 130);
            lstConflicts.Columns.Add("Conflict Details", 210);
            lstConflicts.Columns.Add("Proposed Resolution", 280);
            lstConflicts.Resize += (s, e) =>
            {
                int used = lstConflicts.Columns[0].Width + lstConflicts.Columns[1].Width + lstConflicts.Columns[2].Width + lstConflicts.Columns[3].Width + SystemInformation.VerticalScrollBarWidth + 4;
                if (lstConflicts.ClientSize.Width > used)
                    lstConflicts.Columns[4].Width = Math.Max(280, lstConflicts.ClientSize.Width - used);
            };

            foreach (var c in _conflicts)
            {
                var lvi = new ListViewItem(c.Item.Name);
                lvi.SubItems.Add(c.Item.IsDirectory ? "Folder" : "File");
                lvi.SubItems.Add(c.TargetFolder);
                lvi.SubItems.Add(c.Description);
                lvi.SubItems.Add(c.ProposedAction);

                if (_isDark)
                {
                    lvi.ForeColor = c.Type == ConflictType.TargetFolderMatchesExisting
                        ? Color.FromArgb(248, 113, 113)
                        : (c.Type == ConflictType.TargetFolderMatchesSelf
                            ? Color.FromArgb(147, 197, 253)
                            : Color.FromArgb(252, 211, 77));
                }
                else
                {
                    lvi.ForeColor = c.Type == ConflictType.TargetFolderMatchesExisting
                        ? Color.FromArgb(185, 28, 28)
                        : (c.Type == ConflictType.TargetFolderMatchesSelf
                            ? Color.FromArgb(30, 58, 138)
                            : Color.FromArgb(180, 83, 9));
                }

                lstConflicts.Items.Add(lvi);
            }

            this.Controls.Add(lstConflicts);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlBottom);
            this.AcceptButton = btnAutoRename;
            this.CancelButton = btnCancel;
        }
    }
}
