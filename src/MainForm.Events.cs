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

        private void BtnUseCurrentFolder_Click(object? sender, EventArgs e) => SetSourceFolder(_executableDirectory);

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
                _customOutputFolder = dialog.SelectedPath;
                UpdateOutputFolder();
            }
        }

        private void ChkUseSourceAsOutput_CheckedChanged(object? sender, EventArgs e) => UpdateOutputFolder();

        private void ChkIncludeFolders_CheckedChanged(object? sender, EventArgs e)
        {
            if (_settings.IncludeTopLevelFolders == _chkIncludeFolders.Checked) return;
            _settings.IncludeTopLevelFolders = _chkIncludeFolders.Checked;
            _settings.SaveToFile();
            LoadPreview();
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
                _currentPreviewLimit = _settings.MaxPreviewItems > 0 ? _settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems;
                _filesToOrganize = _organizerService.ScanFiles(_settings.IncludeTopLevelFolders, _settings.IgnoreSystemFiles, _settings.FileDateSource, _settings.FolderDateSource);
                _sortColumn = 2;
                _sortAscending = false;
                UpdateColumnHeaderSortIndicators();
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

        private int _sortColumn = 2;
        private bool _sortAscending = false;
        private static readonly string[] ColumnBaseHeaders = { "File Name", "Type", "Modified Date", "Created Date", "Target Folder", "Size" };

        private void UpdateFileList()
        {
            _lstFiles.ListViewItemSorter = null;
            _lstFiles.Items.Clear();

            Color activeColor = _settings.DarkMode ? Color.FromArgb(96, 165, 250) : Color.FromArgb(29, 78, 216);
            Color mutedColor = _settings.DarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
            var boldFont = GetBoldListFont();

            int maxPreview = _currentPreviewLimit > 0 ? _currentPreviewLimit : (_settings.MaxPreviewItems > 0 ? _settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems);
            var displayedFiles = _filesToOrganize.Take(maxPreview).ToList();

            foreach (var file in displayedFiles)
            {
                var item = new ListViewItem(file.Name) { UseItemStyleForSubItems = false };
                item.SubItems.Add(file.TypeDisplay);

                string modText = (file.IsCreatedDateActive ? "   " : "✔ ") + file.ModifiedDate.ToString("yyyy-MM-dd HH:mm");
                string creText = (file.IsCreatedDateActive ? "✔ " : "   ") + file.CreatedDate.ToString("yyyy-MM-dd HH:mm");

                var subMod = item.SubItems.Add(modText);
                subMod.ForeColor = file.IsCreatedDateActive ? mutedColor : activeColor;
                if (!file.IsCreatedDateActive) subMod.Font = boldFont;

                var subCre = item.SubItems.Add(creText);
                subCre.ForeColor = file.IsCreatedDateActive ? activeColor : mutedColor;
                if (file.IsCreatedDateActive) subCre.Font = boldFont;

                item.SubItems.Add(file.TargetFolder);
                item.SubItems.Add(file.IsDirectory ? "—" : FormatFileSize(file.Size));
                item.Tag = file;
                _lstFiles.Items.Add(item);
            }

            int total = _filesToOrganize.Count;
            int remaining = total - displayedFiles.Count;

            if (remaining > 0)
            {
                var item = new ListViewItem($"➕ Load 1,000 more files... ({remaining:N0} remaining)")
                {
                    ForeColor = _settings.DarkMode ? Color.FromArgb(147, 197, 253) : Color.FromArgb(37, 99, 235)
                };
                item.Tag = "LOAD_MORE";
                _lstFiles.Items.Add(item);

                if (_btnLoadMore != null)
                {
                    _btnLoadMore.Text = $"➕ Load 1,000 More ({remaining:N0} remaining)";
                    _pnlLoadMore.Visible = true;
                    _pnlLoadMore.SendToBack();
                    _lstFiles.BringToFront();
                }
            }
            else if (_pnlLoadMore != null) _pnlLoadMore.Visible = false;

            UpdatePreviewHeaderCount(displayedFiles.Count, total);
        }

        private void LstFiles_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (_sortColumn == e.Column) _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = e.Column;
                _sortAscending = (e.Column != 2 && e.Column != 3 && e.Column != 5);
            }

            UpdateColumnHeaderSortIndicators();
            FileItemComparer.Sort(_filesToOrganize, _sortColumn, _sortAscending);
            UpdateFileList();
        }

        private void UpdateColumnHeaderSortIndicators()
        {
            for (int i = 0; i < _lstFiles.Columns.Count && i < ColumnBaseHeaders.Length; i++)
            {
                _lstFiles.Columns[i].Text = (i == _sortColumn)
                    ? $"{ColumnBaseHeaders[i]} {(_sortAscending ? "▲" : "▼")}"
                    : ColumnBaseHeaders[i];
            }
        }

        private void UpdateSummary()
        {
            if (_filesToOrganize.Count == 0)
            {
                _lblSummary.Text = "No files found to organize.";
                _lblSummary.Font = new Font(_lblSummary.Font, FontStyle.Regular);
                _lblSummary.ForeColor = AppTheme.GetPalette(_settings.DarkMode).TextMuted;
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
            int total = _filesToOrganize.Count;

            _lblSummary.Font = new Font(_lblSummary.Font, FontStyle.Bold);
            _lblSummary.ForeColor = _settings.DarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(4, 120, 87);
            _lblSummary.Text = folderCount > 0
                ? $"✔ Ready: {total:N0} items ({fileCount:N0} files, {folderCount:N0} folders) → {grouped.Count:N0} target date folder(s)"
                : $"✔ Ready: {fileCount:N0} file(s) → {grouped.Count:N0} target date folder(s)";
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

            string outputDir = GetBaseOutputFolder();
            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                MessageBox.Show("Please select a valid output folder.", "Invalid Output", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string warningExtra = _pnlTimestampWarning.Visible
                ? "\n\n⚠ Warning: Almost all items share identical or similar timestamps (common with downloaded ZIPs or chat media) and will be organized into a single folder."
                : "";

            string previewOutputDir = AppConstants.GetSortedFolderPreviewPath(outputDir, _settings.Use24HourTimestamp);
            var result = MessageBox.Show(
                $"You are about to organize {_filesToOrganize.Count} item(s) into date-based folders.\n\n" +
                $"Source: {_txtSourceFolder.Text}\n" +
                $"Output: {previewOutputDir}" +
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

        private void BtnStartNewProject_Click(object? sender, EventArgs e) => ResetForm();
    }
}
