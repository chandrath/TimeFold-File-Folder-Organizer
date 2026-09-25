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
            using var dialog = new FolderBrowserDialog { Description = "Select folder to organize", ShowNewFolderButton = false };
            if (dialog.ShowDialog() == DialogResult.OK) SetSourceFolder(dialog.SelectedPath);
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
            using var dialog = new FolderBrowserDialog { Description = "Select output folder (where Sorted folder will be created)", ShowNewFolderButton = true };
            if (dialog.ShowDialog() == DialogResult.OK) { _customOutputFolder = dialog.SelectedPath; UpdateOutputFolder(); }
        }

        private void ChkUseSourceAsOutput_CheckedChanged(object? sender, EventArgs e)
        {
            if (_settings.UseSourceAsOutput != _chkUseSourceAsOutput.Checked) { _settings.UseSourceAsOutput = _chkUseSourceAsOutput.Checked; _settings.SaveToFile(); }
            UpdateOutputFolder();
        }

        private void ChkCreateSubfolder_CheckedChanged(object? sender, EventArgs e)
        {
            if (_settings.CreateSortedSubfolder == _chkCreateSubfolder.Checked) return;
            _settings.CreateSortedSubfolder = _chkCreateSubfolder.Checked;
            _settings.SaveToFile();
            if (_organizerService != null) _organizerService.CreateSortedSubfolder = _chkCreateSubfolder.Checked;
            UpdateOutputFolder();
        }

        private void ChkIncludeFolders_CheckedChanged(object? sender, EventArgs e)
        {
            if (_settings.IncludeTopLevelFolders == _chkIncludeFolders.Checked) return;
            _settings.IncludeTopLevelFolders = _chkIncludeFolders.Checked;
            if (_btnFolderRules != null) _btnFolderRules.Enabled = _chkIncludeFolders.Checked;
            _settings.SaveToFile();
            LoadPreview();
        }

        private void PnlSourceDrop_DragEnter(object? sender, DragEventArgs e)
        {
            if (_pnlProgress.Visible) { e.Effect = DragDropEffects.None; return; }
            bool isValid = e.Data?.GetDataPresent(DataFormats.FileDrop) == true && ((string[]?)e.Data.GetData(DataFormats.FileDrop))?.Length >= 1;
            e.Effect = isValid ? DragDropEffects.Copy : DragDropEffects.None;
            var palette = AppTheme.GetPalette(_settings.DarkMode);
            if (_pnlSourceDrop != null) _pnlSourceDrop.BackColor = isValid ? palette.DropZoneHoverBg : palette.DangerBg;
        }

        private void PnlSourceDrop_DragLeave(object? sender, EventArgs e) => _pnlSourceDrop.BackColor = AppTheme.GetPalette(_settings.DarkMode).DropZoneBg;

        private void PnlSourceDrop_DragDrop(object? sender, DragEventArgs e)
        {
            if (_pnlProgress.Visible) return;
            if (_pnlSourceDrop != null) _pnlSourceDrop.BackColor = AppTheme.GetPalette(_settings.DarkMode).DropZoneBg;
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length > 0)
                {
                    string path = files[0];
                    if (_pnlComplete.Visible) ResetForm();
                    if (Directory.Exists(path)) SetSourceFolder(path);
                    else if (File.Exists(path))
                    {
                        string? dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) SetSourceFolder(dir);
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
                CheckConflicts();
                UpdateFileList();
                UpdateSummary();
                CheckTimestampSimilarity();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int _sortColumn = 2;
        private bool _sortAscending = false;
        private static readonly string[] ColumnBaseHeaders = { "File Name", "Type", "Modified Date", "Created Date", "📁 Target Folder", "Status", "Size" };

        private void UpdateFileList()
        {
            _lstFiles.ListViewItemSorter = null;
            _lstFiles.Items.Clear();

            var palette = AppTheme.GetPalette(_settings.DarkMode);
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
                subMod.ForeColor = file.IsCreatedDateActive ? palette.ListDateMuted : palette.ListDateActive;
                if (!file.IsCreatedDateActive) subMod.Font = boldFont;

                var subCre = item.SubItems.Add(creText);
                subCre.ForeColor = file.IsCreatedDateActive ? palette.ListDateActive : palette.ListDateMuted;
                if (file.IsCreatedDateActive) subCre.Font = boldFont;

                var subTarget = item.SubItems.Add($"📁 {file.TargetFolder}");
                subTarget.ForeColor = palette.ListTargetFolder;
                subTarget.Font = boldFont;

                var conflict = _currentConflicts.FirstOrDefault(c => c.Item == file);
                var subStatus = item.SubItems.Add(conflict?.ShortStatus ?? "✓ Ready");
                subStatus.ForeColor = conflict != null
                    ? (_settings.DarkMode ? Color.FromArgb(248, 113, 113) : Color.FromArgb(220, 38, 38))
                    : (_settings.DarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(22, 101, 52));
                if (conflict != null) subStatus.Font = boldFont;

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
                }
            }
            else if (_pnlLoadMore != null) _pnlLoadMore.Visible = false;

            UpdatePreviewHeaderCount(displayedFiles.Count, total);
        }

        private void LstFiles_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (_sortColumn == e.Column) _sortAscending = !_sortAscending;
            else { _sortColumn = e.Column; _sortAscending = (e.Column != 2 && e.Column != 3 && e.Column != 5); }

            UpdateColumnHeaderSortIndicators();
            FileItemComparer.Sort(_filesToOrganize, _sortColumn, _sortAscending);
            UpdateFileList();
        }

        private void UpdateColumnHeaderSortIndicators()
        {
            for (int i = 0; i < _lstFiles.Columns.Count && i < ColumnBaseHeaders.Length; i++)
                _lstFiles.Columns[i].Text = (i == _sortColumn) ? $"{ColumnBaseHeaders[i]} {(_sortAscending ? "▲" : "▼")}" : ColumnBaseHeaders[i];
        }

        private bool _isAdjustingColumns;

        private void AutoFitColumns()
        {
            if (_isAdjustingColumns || _lstFiles == null || _lstFiles.Columns.Count < 7 || _filesToOrganize.Count == 0) return;
            _isAdjustingColumns = true;
            _lstFiles.BeginUpdate();
            try
            {
                var boldFont = GetBoldListFont();
                _lstFiles.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                _lstFiles.Columns[1].Width = Math.Max(_lstFiles.Columns[1].Width + 8, TextRenderer.MeasureText(ColumnBaseHeaders[1], _lstFiles.Font).Width + 14);

                _lstFiles.Columns[2].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                _lstFiles.Columns[2].Width = Math.Max(_lstFiles.Columns[2].Width + 10, TextRenderer.MeasureText(ColumnBaseHeaders[2] + " ▼", _lstFiles.Font).Width + 14);

                _lstFiles.Columns[3].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                _lstFiles.Columns[3].Width = Math.Max(_lstFiles.Columns[3].Width + 10, TextRenderer.MeasureText(ColumnBaseHeaders[3], _lstFiles.Font).Width + 14);

                int maxTargetW = TextRenderer.MeasureText(ColumnBaseHeaders[4], _lstFiles.Font).Width + 16;
                foreach (var f in _filesToOrganize)
                {
                    int w = TextRenderer.MeasureText($"📁 {f.TargetFolder}", boldFont).Width + 16;
                    if (w > maxTargetW) maxTargetW = w;
                }
                _lstFiles.Columns[4].Width = Math.Max(maxTargetW, 90);

                _lstFiles.Columns[5].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                _lstFiles.Columns[5].Width = Math.Max(_lstFiles.Columns[5].Width + 10, TextRenderer.MeasureText(ColumnBaseHeaders[5], _lstFiles.Font).Width + 16);

                _lstFiles.Columns[6].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                _lstFiles.Columns[6].Width = Math.Max(_lstFiles.Columns[6].Width + 8, TextRenderer.MeasureText(ColumnBaseHeaders[6], _lstFiles.Font).Width + 14);

                int otherW = _lstFiles.Columns[1].Width + _lstFiles.Columns[2].Width + _lstFiles.Columns[3].Width + _lstFiles.Columns[4].Width + _lstFiles.Columns[5].Width + _lstFiles.Columns[6].Width;
                int availW = _lstFiles.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
                _lstFiles.Columns[0].Width = Math.Max(180, availW - otherW);
            }
            finally
            {
                _lstFiles.EndUpdate();
                _isAdjustingColumns = false;
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
            string label = _settings.OrgMode == OrganizationMode.Category ? "category folder(s)" : (_settings.OrgMode == OrganizationMode.Extension ? "extension folder(s)" : (_settings.OrgMode == OrganizationMode.Date ? "date folder(s)" : "folder(s)"));
            _lblSummary.Text = folderCount > 0
                ? $"✔ Ready: {total:N0} items ({fileCount:N0} files, {folderCount:N0} folders) → {grouped.Count:N0} target {label}"
                : $"✔ Ready: {fileCount:N0} file(s) → {grouped.Count:N0} target {label}";
            _btnStart.Enabled = true;
            if (_shouldAutoFitColumns)
            {
                _shouldAutoFitColumns = false;
                AutoFitColumns();
            }
        }

        private void ShowConflictDialog()
        {
            if (_currentConflicts.Count == 0) return;
            using var dlg = new FileOrganizer.Forms.ConflictDialog(_currentConflicts, _settings.DarkMode);
            dlg.ShowDialog(this);
        }

        private void CheckConflicts()
        {
            if (_organizerService == null || _filesToOrganize.Count == 0)
            {
                _currentConflicts.Clear();
                _pnlConflicts.Visible = false;
                return;
            }

            var grouped = _organizerService.GroupByMonthYear(_filesToOrganize);
            string outputDir = GetBaseOutputFolder();
            _currentConflicts = FileOrganizer.Services.ConflictDetector.Detect(
                _filesToOrganize, grouped, outputDir, _settings.CreateSortedSubfolder, _settings.Use24HourTimestamp);

            _pnlConflicts.Visible = _currentConflicts.Count > 0;
            if (_pnlConflicts.Visible)
            {
                _lblConflicts.Text = $"⚠ {_currentConflicts.Count} Collision/Conflict(s) Detected. Click here to review details →";
            }
            _pnlCenterSection.PerformLayout();
        }

        private void CheckTimestampSimilarity()
        {
            if (_organizerService == null || _filesToOrganize.Count < 3 || _settings.OrgMode == OrganizationMode.Category || _settings.OrgMode == OrganizationMode.Extension)
            {
                _pnlTimestampWarning.Visible = false;
                _pnlCenterSection.PerformLayout();
                return;
            }

            var grouped = _organizerService.GroupByMonthYear(_filesToOrganize);
            if (grouped.Count == 0)
            {
                _pnlTimestampWarning.Visible = false;
                _pnlCenterSection.PerformLayout();
                return;
            }

            var dominant = grouped.OrderByDescending(g => g.Value.Count).First();
            double ratio = (double)dominant.Value.Count / _filesToOrganize.Count;
            bool isSimilar = ratio >= AppConstants.TimestampSimilarityThreshold;
            _lblTimestampWarning.Text = isSimilar ? $"⚠ Notice: {(int)(ratio * 100)}% of items share the same date/period and will be organized into '{dominant.Key}'." : "";
            _pnlTimestampWarning.Visible = isSimilar;
            _pnlCenterSection.PerformLayout();
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            if (_isOperationRunning || _pnlProgress.Visible) return;
            if (_organizerService == null) return;
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

            if (!AppConstants.CheckDirectoryWritePermission(outputDir, this)) return;

            LoadPreview();
            if (_filesToOrganize.Count == 0) return;

            ConflictResolutionStrategy conflictStrategy = ConflictResolutionStrategy.AutoRename;
            if (_currentConflicts.Count > 0)
            {
                using var dlg = new FileOrganizer.Forms.ConflictDialog(_currentConflicts, _settings.DarkMode);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                conflictStrategy = dlg.SelectedStrategy;
            }

            string warningExtra = _pnlTimestampWarning.Visible
                ? "\n\n⚠ Warning: Almost all items share identical or similar timestamps (common with downloaded ZIPs or chat media) and will be organized into a single folder."
                : "";

            string previewOutputDir = _settings.CreateSortedSubfolder
                ? AppConstants.GetSortedFolderPreviewPath(outputDir, _settings.Use24HourTimestamp)
                : outputDir;
            string destDesc = _settings.CreateSortedSubfolder ? "a Sorted folder in the output location" : "the output location directly";
            var result = MessageBox.Show(
                $"You are about to organize {_filesToOrganize.Count} item(s) into date-based folders.\n\n" +
                $"Source: {_txtSourceFolder.Text}\n" +
                $"Output: {previewOutputDir}" +
                warningExtra +
                $"\n\nThis will move files from the source location to {destDesc}.\n\nContinue?",
                "Confirm Organization",
                MessageBoxButtons.YesNo,
                _pnlTimestampWarning.Visible ? MessageBoxIcon.Warning : MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            _isOperationRunning = true;
            _pnlMain.Visible = false; _pnlProgress.Visible = true; _pnlComplete.Visible = false;

            _progressBar.Value = 0;
            _txtStatus.Clear();
            _txtStatus.AppendText($"Starting organization...\r\nSource: {_organizerService.WorkingDirectory}\r\nOutput: {_organizerService.OutputDirectory}\r\n\r\n");

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
                var collidingPaths = new HashSet<string>(_currentConflicts.Select(c => c.Item.FullPath), StringComparer.OrdinalIgnoreCase);
                var orgResult = await _organizerService.OrganizeFilesAsync(
                    _filesToOrganize, progress, _cancellationTokenSource.Token,
                    _settings.GenerateCsvLog, conflictStrategy, collidingPaths);

                RecordUndoSession(orgResult);
                ShowCompletion(orgResult);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Organization was cancelled. Any processed files have been logged.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during organization: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isOperationRunning = false;
                if (_pnlProgress.Visible) { _pnlProgress.Visible = false; _pnlMain.Visible = true; }
                UpdateUndoUIState();
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (_cancellationTokenSource != null && MessageBox.Show("Are you sure you want to cancel? Partial progress will be saved.", "Cancel Organization", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _cancellationTokenSource.Cancel();
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
            if (result.ConflictsResolved > 0) summaryBuilder.AppendLine($"  🛡 Name Conflicts:      {result.ConflictsResolved} safely auto-renamed");
            if (result.Errors > 0) summaryBuilder.AppendLine($"  ⚠ Errors Encountered:  {result.Errors}");
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
            if (!string.IsNullOrWhiteSpace(targetPath) && Directory.Exists(targetPath)) { Process.Start("explorer.exe", targetPath); return; }
            var fallback = _organizerService?.OutputDirectory;
            if (!string.IsNullOrWhiteSpace(fallback) && Directory.Exists(fallback)) { Process.Start("explorer.exe", fallback); return; }
            MessageBox.Show("Output folder is not available yet.", "Folder Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStartNewProject_Click(object? sender, EventArgs e) => ResetForm();
    }
}
