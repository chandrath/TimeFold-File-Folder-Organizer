using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Controls;
using FileOrganizer.Forms;
using FileOrganizer.Models;
using FileOrganizer.Services;

namespace FileOrganizer
{
    public partial class MainForm
    {
        private ModernButton _btnUndo = null!;
        private ToolStripMenuItem _menuUndo = null!;
        private Panel _pnlUndoBar = null!;

        private void InitializeUndoUI()
        {
            // 1. Place Undo in File menu (Standard, clean, avoids extra Edit menu)
            _menuUndo = new ToolStripMenuItem("Undo Last Organization (Beta)", null, async (s, e) => await PerformUndoAsync())
            {
                ShortcutKeys = Keys.Control | Keys.Z,
                Enabled = UndoService.HasActiveSession()
            };

            // Insert into File menu right after 'New'
            if (_menuFile.DropDownItems.Count > 1)
            {
                _menuFile.DropDownItems.Insert(1, _menuUndo);
            }
            else
            {
                _menuFile.DropDownItems.Add(_menuUndo);
            }

            // 2. Decouple Undo into the Results Card on Completion Screen
            _pnlUndoBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Margin = new Padding(0, 8, 0, 0),
                BackColor = Color.Transparent
            };

            var lblUndoHint = new Label
            {
                Text = "Made a mistake?",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = AppConstants.ColorTextMuted,
                AutoSize = true,
                Location = new Point(0, 7)
            };

            _btnUndo = new ModernButton
            {
                Text = "↩ Undo Organization (Beta)",
                Size = new Size(185, 28),
                Location = new Point(115, 3),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                BorderRadius = 6,
                Cursor = Cursors.Hand,
                Enabled = UndoService.HasActiveSession()
            };
            _btnUndo.Click += async (s, e) => await PerformUndoAsync();
            _toolTip?.SetToolTip(_btnUndo, "Roll back this organization and restore files (Beta)");

            _pnlUndoBar.Controls.AddRange([lblUndoHint, _btnUndo]);
            _pnlDetailsCard.Controls.Add(_pnlUndoBar);
        }

        private void RecordUndoSession(OrganizationResult result)
        {
            if (_organizerService == null) return;
            UndoService.RecordSession(
                _organizerService.WorkingDirectory,
                _organizerService.OutputDirectory,
                result.SortedFolderPath,
                _filesToOrganize,
                null,
                result.CsvLogPath);
            UpdateUndoUIState();
        }

        private void UpdateUndoUIState()
        {
            bool hasSession = UndoService.HasActiveSession();
            if (_menuUndo != null) _menuUndo.Enabled = hasSession;
            if (_btnUndo != null) _btnUndo.Enabled = hasSession;
            if (_pnlUndoBar != null) _pnlUndoBar.Visible = hasSession;
        }

        private async Task PerformUndoAsync()
        {
            var session = UndoService.LoadSession();
            if (session == null || session.MovedItems.Count == 0)
            {
                MessageBox.Show(this, "No previous organization run found to undo.", "Undo (Beta)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateUndoUIState();
                return;
            }

            var preflight = UndoService.PreflightCheck(session);
            if (!preflight.CanProceed)
            {
                var askClear = MessageBox.Show(
                    this,
                    "Cannot perform undo:\n\nNone of the organized files were found at their destination folders.\nThey may have already been moved, renamed, or deleted outside TimeFold.\n\nWould you like to clear this undo history?",
                    "No Files Found to Restore",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (askClear == DialogResult.Yes)
                {
                    UndoService.ClearSession();
                    UpdateUndoUIState();
                }
                return;
            }

            // Show Confirmation & Location Choice Dialog
            using var dlg = new UndoConfirmDialog(session, preflight, _settings.DarkMode);
            var res = dlg.ShowDialog(this);
            if (dlg.DiscardRequested || res == DialogResult.Abort)
            {
                UndoService.ClearSession();
                UpdateUndoUIState();
                return;
            }
            if (res != DialogResult.OK) return;

            string? customRestoreDir = dlg.UseDedicatedFolder ? dlg.DedicatedFolderPath : null;

            // Switch to progress view
            _pnlMain.Visible = false;
            _pnlComplete.Visible = false;
            _pnlProgress.Visible = true;
            _progressBar.Value = 0;
            _txtStatus.Clear();

            string restoreTargetDesc = customRestoreDir != null
                ? $"Safe Folder ({dlg.DedicatedFolderPath})"
                : $"Original Locations ({session.SourceDirectory})";

            _txtStatus.AppendText($"Starting Undo Operation (Beta)...\r\nRestoring files to: {restoreTargetDesc}\r\n\r\n");

            _cancellationTokenSource = new CancellationTokenSource();
            var progress = new Progress<(int current, int total, string currentFile)>(p =>
            {
                _progressBar.Maximum = p.total;
                _progressBar.Value = p.current;
                _lblProgress.Text = $"Restoring: {p.current} of {p.total} files";

                if (_settings.ShowDetailedProgress)
                {
                    _txtStatus.AppendText($"↩ {p.currentFile}\r\n");
                    _txtStatus.SelectionStart = _txtStatus.Text.Length;
                    _txtStatus.ScrollToCaret();
                }
            });

            try
            {
                var result = await UndoService.UndoAsync(session, progress, _cancellationTokenSource.Token, customRestoreDir);

                _pnlProgress.Visible = false;
                _pnlMain.Visible = true;

                var summary = new StringBuilder();
                summary.AppendLine("✔ Undo Completed Successfully!\n");
                summary.AppendLine($"• Restored:      {result.RestoredCount} file(s)");
                summary.AppendLine($"• Cleaned Up:    {result.PrunedFoldersCount} empty folder(s)");
                if (result.CollisionRenamedCount > 0)
                {
                    summary.AppendLine($"• Auto-Renamed:  {result.CollisionRenamedCount} item(s) preserved with '(Restored)'");
                }
                if (result.FailureCount > 0)
                {
                    summary.AppendLine($"• Skipped/Error: {result.FailureCount} item(s)");
                }

                MessageBox.Show(this, summary.ToString(), "Undo Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Refresh source folder view
                string folderToView = customRestoreDir != null && Directory.Exists(customRestoreDir)
                    ? customRestoreDir
                    : session.SourceDirectory;

                SetSourceFolder(folderToView);
                UpdateUndoUIState();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this, "Undo operation was cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _pnlProgress.Visible = false;
                _pnlMain.Visible = true;
                UpdateUndoUIState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error during undo: {ex.Message}", "Undo Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _pnlProgress.Visible = false;
                _pnlMain.Visible = true;
                UpdateUndoUIState();
            }
        }
    }
}
