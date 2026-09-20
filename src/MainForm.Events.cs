using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer
{
    public partial class MainForm
    {
        private void MenuFileNew_Click(object? sender, EventArgs e)
        {
            ResetForm();
        }

        private void MenuFileExit_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuPreferences_Click(object? sender, EventArgs e)
        {
            using var prefsForm = new PreferencesForm(_settings);
            if (prefsForm.ShowDialog() == DialogResult.OK)
            {
                _settings = prefsForm.Settings;
                _settings.SaveToFile();
                ApplySettings();
                ApplyTheme(_settings.DarkMode);
                LoadPreview();
            }
        }

        private void MenuAbout_Click(object? sender, EventArgs e)
        {
            using var aboutForm = new AboutForm(_settings.DarkMode);
            aboutForm.ShowDialog();
        }

        private void BtnBrowseSource_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select folder to organize",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SetSourceFolder(dialog.SelectedPath);
            }
        }

        private void BtnUseCurrentFolder_Click(object? sender, EventArgs e)
        {
            SetSourceFolder(_executableDirectory);
        }

        private void BtnRecentFolders_Click(object? sender, EventArgs e)
        {
            bool isDark = _settings.DarkMode;
            var palette = AppTheme.GetPalette(isDark);
            var menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                Renderer = isDark ? AppTheme.DarkMenuRenderer : new ToolStripProfessionalRenderer(),
                BackColor = palette.MenuBg
            };
            PopulateRecentMenu(menu.Items);
            SetMenuColors(menu.Items, palette);
            menu.Show(_btnRecentFolders, new Point(0, _btnRecentFolders.Height + 2));
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select output folder (where Sorted folder will be created)",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _txtOutputFolder.Text = dialog.SelectedPath;
                if (_organizerService != null)
                {
                    _organizerService.OutputDirectory = dialog.SelectedPath;
                }
            }
        }

        private void ChkUseSourceAsOutput_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateOutputFolder();
        }

        private void PnlSourceDrop_DragEnter(object? sender, DragEventArgs e)
        {
            var palette = AppTheme.GetPalette(_settings.DarkMode);
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length >= 1)
                {
                    e.Effect = DragDropEffects.Copy;
                    _pnlSourceDrop.BackColor = palette.DropZoneHoverBg;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
            _pnlSourceDrop.BackColor = palette.DangerBg;
        }

        private void PnlSourceDrop_DragLeave(object? sender, EventArgs e)
        {
            var palette = AppTheme.GetPalette(_settings.DarkMode);
            _pnlSourceDrop.BackColor = palette.DropZoneBg;
        }

        private void PnlSourceDrop_DragDrop(object? sender, DragEventArgs e)
        {
            var palette = AppTheme.GetPalette(_settings.DarkMode);
            _pnlSourceDrop.BackColor = palette.DropZoneBg;

            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    if (Directory.Exists(path))
                    {
                        SetSourceFolder(path);
                    }
                    else if (File.Exists(path))
                    {
                        string? dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            SetSourceFolder(dir);
                        }
                    }
                }
            }
        }

        private void LoadPreview()
        {
            if (_organizerService == null) return;

            try
            {
                _filesToOrganize = _organizerService.ScanFiles(_settings.IncludeTopLevelFolders, _settings.IgnoreSystemFiles);
                UpdateFileList();
                UpdateSummary();
                CheckConflicts();
                CheckTimestampSimilarity();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int _sortColumn = -1;
        private bool _sortAscending = true;
        private static readonly string[] ColumnBaseHeaders = { "File Name", "Type", "Target Folder", "Modified Date", "Size" };

        private void UpdateFileList()
        {
            _lstFiles.ListViewItemSorter = null;
            _lstFiles.Items.Clear();

            foreach (var file in _filesToOrganize.Take(1000))
            {
                var item = new ListViewItem(file.Name);
                item.SubItems.Add(file.TypeDisplay);
                item.SubItems.Add(file.TargetFolder);
                item.SubItems.Add(file.ModifiedDate.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(file.IsDirectory ? "—" : FormatFileSize(file.Size));
                item.Tag = file;
                _lstFiles.Items.Add(item);
            }

            if (_filesToOrganize.Count > 1000)
            {
                var item = new ListViewItem($"... and {_filesToOrganize.Count - 1000} more files")
                {
                    ForeColor = Color.Gray
                };
                _lstFiles.Items.Add(item);
            }

            if (_sortColumn >= 0)
            {
                _lstFiles.ListViewItemSorter = new FileItemComparer(_sortColumn, _sortAscending);
                _lstFiles.Sort();
            }
        }

        private void LstFiles_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (_sortColumn == e.Column)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = e.Column;
                _sortAscending = true;
            }

            for (int i = 0; i < _lstFiles.Columns.Count && i < ColumnBaseHeaders.Length; i++)
            {
                _lstFiles.Columns[i].Text = (i == _sortColumn)
                    ? $"{ColumnBaseHeaders[i]} {(_sortAscending ? "▲" : "▼")}"
                    : ColumnBaseHeaders[i];
            }

            _lstFiles.ListViewItemSorter = new FileItemComparer(_sortColumn, _sortAscending);
            _lstFiles.Sort();
        }

        private void UpdateSummary()
        {
            if (_filesToOrganize.Count == 0)
            {
                _lblSummary.Text = "No files found to organize.";
                _btnStart.Enabled = false;
                if (_pnlEmptyState != null) _pnlEmptyState.Visible = true;
                _lstFiles.Visible = false;
                return;
            }

            if (_pnlEmptyState != null) _pnlEmptyState.Visible = false;
            _lstFiles.Visible = true;

            var grouped = _organizerService?.GroupByMonthYear(_filesToOrganize) ?? new Dictionary<string, List<FileItem>>();
            var fileCount = _filesToOrganize.Count(f => !f.IsDirectory);
            var folderCount = _filesToOrganize.Count(f => f.IsDirectory);

            _lblSummary.Text = $"Ready: {fileCount} file(s), {folderCount} folder(s) → {grouped.Count} target date folder(s)";
            _btnStart.Enabled = true;
        }

        private void CheckConflicts()
        {
            if (_organizerService == null || _filesToOrganize.Count == 0)
            {
                _pnlConflicts.Visible = false;
                return;
            }

            var grouped = _organizerService.GroupByMonthYear(_filesToOrganize);
            var conflicts = _organizerService.DetectConflicts(grouped);

            if (conflicts.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"⚠ Conflicts Detected: {conflicts.Count}");
                foreach (var conflict in conflicts.Take(5))
                {
                    sb.AppendLine($"• {conflict}");
                }
                if (conflicts.Count > 5)
                {
                    sb.AppendLine($"• ... and {conflicts.Count - 5} more");
                }
                sb.Append("→ Conflicting files will be automatically renamed");
                _lblConflicts.Text = sb.ToString();
                _pnlConflicts.Visible = true;
            }
            else
            {
                _pnlConflicts.Visible = false;
            }
        }

        private void CheckTimestampSimilarity()
        {
            if (_organizerService == null || _filesToOrganize.Count < 3)
            {
                _pnlTimestampWarning.Visible = false;
                return;
            }

            var grouped = _organizerService.GroupByMonthYear(_filesToOrganize);
            if (grouped.Count == 0)
            {
                _pnlTimestampWarning.Visible = false;
                return;
            }

            var dominant = grouped.OrderByDescending(g => g.Value.Count).First();
            double ratio = (double)dominant.Value.Count / _filesToOrganize.Count;

            if (ratio >= AppConstants.TimestampSimilarityThreshold)
            {
                int percent = (int)(ratio * 100);
                _lblTimestampWarning.Text = $"⚠ Notice: {percent}% of items share the same date/period and will be organized into '{dominant.Key}'.";
                _pnlTimestampWarning.Visible = true;
            }
            else
            {
                _pnlTimestampWarning.Visible = false;
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            if (_organizerService == null || _filesToOrganize.Count == 0)
                return;

            if (string.IsNullOrEmpty(_txtSourceFolder.Text) || !Directory.Exists(_txtSourceFolder.Text))
            {
                MessageBox.Show("Please select a valid source folder.", "Invalid Source", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string outputDir = _chkUseSourceAsOutput.Checked ? _txtSourceFolder.Text : _txtOutputFolder.Text;
            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                MessageBox.Show("Please select a valid output folder.", "Invalid Output", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string warningExtra = _pnlTimestampWarning.Visible
                ? "\n\n⚠ Warning: Almost all items share identical or similar timestamps (common with downloaded ZIPs or chat media) and will be organized into a single folder."
                : "";

            var result = MessageBox.Show(
                $"You are about to organize {_filesToOrganize.Count} item(s) into date-based folders.\n\n" +
                $"Source: {_txtSourceFolder.Text}\n" +
                $"Output: {outputDir}" +
                warningExtra +
                "\n\nThis will move files from the source location to a Sorted folder in the output location.\n\n" +
                "Continue?",
                "Confirm Organization",
                MessageBoxButtons.YesNo,
                _pnlTimestampWarning.Visible ? MessageBoxIcon.Warning : MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            _pnlMain.Visible = false;
            _pnlProgress.Visible = true;
            _pnlComplete.Visible = false;

            _progressBar.Value = 0;
            _txtStatus.Clear();
            _txtStatus.AppendText("Starting organization...\r\n");
            _txtStatus.AppendText($"Source directory: {_organizerService.WorkingDirectory}\r\n");
            _txtStatus.AppendText($"Output directory: {_organizerService.OutputDirectory}\r\n\r\n");

            _cancellationTokenSource = new CancellationTokenSource();
            var progress = new Progress<(int current, int total, string currentFile)>(p =>
            {
                _progressBar.Maximum = p.total;
                _progressBar.Value = p.current;
                _lblProgress.Text = $"Processing: {p.current} of {p.total} files";

                if (_settings.ShowDetailedProgress)
                {
                    _txtStatus.AppendText($"✓ {p.currentFile}\r\n");
                    _txtStatus.SelectionStart = _txtStatus.Text.Length;
                    _txtStatus.ScrollToCaret();
                }
            });

            try
            {
                var orgResult = await _organizerService.OrganizeFilesAsync(
                    _filesToOrganize,
                    progress,
                    _cancellationTokenSource.Token,
                    _settings.GenerateCsvLog);

                ShowCompletion(orgResult);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Organization was cancelled. Any processed files have been logged.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _pnlMain.Visible = true;
                _pnlProgress.Visible = false;
                LoadPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during organization: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _pnlMain.Visible = true;
                _pnlProgress.Visible = false;
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to cancel? Partial progress will be saved.",
                    "Cancel Organization",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }

        private void ShowCompletion(OrganizationResult result)
        {
            _pnlMain.Visible = false;
            _pnlProgress.Visible = false;
            _pnlComplete.Visible = true;
            _lastResult = result;

            var summaryBuilder = new StringBuilder();
            summaryBuilder.AppendLine("ORGANIZATION RESULTS");
            summaryBuilder.AppendLine($"  ✔ Items Organized:     {result.FilesMoved} of {result.TotalFiles} successfully processed");
            summaryBuilder.AppendLine($"  📁 Folders Created:     {result.MonthFoldersCreated} date-based folder(s)");
            if (result.ConflictsResolved > 0)
            {
                summaryBuilder.AppendLine($"  🛡 Name Conflicts:      {result.ConflictsResolved} safely auto-renamed");
            }
            if (result.Errors > 0)
            {
                summaryBuilder.AppendLine($"  ⚠ Errors Encountered:  {result.Errors}");
            }
            summaryBuilder.AppendLine();
            summaryBuilder.AppendLine("OUTPUT FOLDER");
            summaryBuilder.AppendLine($"  📂 {(string.IsNullOrWhiteSpace(result.SortedFolderPath) ? "Not available" : result.SortedFolderPath)}");
            if (!string.IsNullOrWhiteSpace(result.CsvLogPath))
            {
                summaryBuilder.AppendLine();
                summaryBuilder.AppendLine("AUDIT LOG");
                summaryBuilder.AppendLine($"  📊 {result.CsvLogPath}");
            }

            _lblCompleteSummary.Text = summaryBuilder.ToString();

            bool outputExists = !string.IsNullOrWhiteSpace(result.SortedFolderPath) && Directory.Exists(result.SortedFolderPath);
            _btnOpenFolder.Enabled = outputExists;
            CenterCompletionButtons();
            UpdateMainLayout();
        }

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            var targetPath = _lastResult?.SortedFolderPath;
            if (!string.IsNullOrWhiteSpace(targetPath) && Directory.Exists(targetPath))
            {
                Process.Start("explorer.exe", targetPath);
                return;
            }

            var fallback = _organizerService?.OutputDirectory;
            if (!string.IsNullOrWhiteSpace(fallback) && Directory.Exists(fallback))
            {
                Process.Start("explorer.exe", fallback);
                return;
            }

            MessageBox.Show("Output folder is not available yet.", "Folder Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStartNewProject_Click(object? sender, EventArgs e)
        {
            ResetForm();
        }
    }
}
