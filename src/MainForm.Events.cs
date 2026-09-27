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

        private int _previewGeneration;
        private async void LoadPreview()
        {
            if (_organizerService == null) return;
            int currentGen = ++_previewGeneration;
            _isUpdatingList = true;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                _currentPreviewLimit = _settings.MaxPreviewItems > 0 ? _settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems;
                var files = await System.Threading.Tasks.Task.Run(() =>
                    _organizerService.ScanFiles(_settings.IncludeTopLevelFolders, _settings.IgnoreSystemFiles, _settings.FileDateSource, _settings.FolderDateSource, null, _settings.EnableFolderExclusions, _settings.UseMediaDateTaken));
                if (currentGen != _previewGeneration) return;
                _filesToOrganize = files;
                _sortColumn = 2; _sortAscending = false;
                UpdateColumnHeaderSortIndicators();
                CheckConflicts();
                UpdateFileList();
                UpdateSummary();
                CheckTimestampSimilarity();
                CheckExclusionsPausedWarning();
                if (_shouldAutoFitColumns) { _shouldAutoFitColumns = false; this.BeginInvoke(new Action(AutoFitColumns)); }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isUpdatingList = false;
                Cursor.Current = Cursors.Default;
            }
        }

        private void UpdateFileList()
        {
            bool wasUpdating = _isUpdatingList;
            _isUpdatingList = true;
            _lstFiles.BeginUpdate();
            try
            {
                _lstFiles.ListViewItemSorter = null;
                _lstFiles.Items.Clear();

                bool shouldHaveMediaColumn = _settings.UseMediaDateTaken && _filesToOrganize.Any(f => f.IsMediaDateActive && f.MediaDateTaken.HasValue);
                if (shouldHaveMediaColumn && !_hasMediaDateColumn)
                {
                    _lstFiles.Columns.Insert(4, new ColumnHeader { Text = "📷 Date Taken", Width = 175 });
                    _hasMediaDateColumn = true;
                    if (_sortColumn >= 4) _sortColumn++;
                    UpdateColumnHeaderSortIndicators();
                }
                else if (!shouldHaveMediaColumn && _hasMediaDateColumn)
                {
                    if (_lstFiles.Columns.Count >= 8) _lstFiles.Columns.RemoveAt(4);
                    _hasMediaDateColumn = false;
                    if (_sortColumn == 4) _sortColumn = 2;
                    else if (_sortColumn > 4) _sortColumn--;
                    UpdateColumnHeaderSortIndicators();
                }

                var palette = AppTheme.GetPalette(_settings.DarkMode);
                var boldFont = GetBoldListFont();

                int maxPreview = _currentPreviewLimit > 0 ? _currentPreviewLimit : (_settings.MaxPreviewItems > 0 ? _settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems);
                var displayedFiles = _filesToOrganize.OrderByDescending(f => f.IsExcludedByRule).Take(maxPreview).ToList();

                var itemsToAdd = new List<ListViewItem>(displayedFiles.Count + 1);
                foreach (var file in displayedFiles)
                {
                    var item = new ListViewItem(file.Name) { UseItemStyleForSubItems = false, Checked = file.IsSelected && !file.IsExcludedByRule };
                    item.SubItems.Add(file.TypeDisplay);

                    bool isExcludedOrUnchecked = file.IsExcludedByRule || !file.IsSelected;
                    bool isMediaActive = !isExcludedOrUnchecked && file.IsMediaDateActive && file.MediaDateTaken.HasValue;
                    bool isModActive = !isExcludedOrUnchecked && !file.IsCreatedDateActive && !isMediaActive;
                    bool isCreActive = !isExcludedOrUnchecked && file.IsCreatedDateActive && !isMediaActive;

                    string modText = (isModActive ? "✔ " : "   ") + AppConstants.FormatDisplayDate(file.ModifiedDate);
                    string creText = (isCreActive ? "✔ " : "   ") + AppConstants.FormatDisplayDate(file.CreatedDate);

                    var subMod = item.SubItems.Add(modText);
                    subMod.ForeColor = isModActive ? palette.ListDateActive : palette.ListDateMuted;
                    if (isModActive) subMod.Font = boldFont;

                    var subCre = item.SubItems.Add(creText);
                    subCre.ForeColor = isCreActive ? palette.ListDateActive : palette.ListDateMuted;
                    if (isCreActive) subCre.Font = boldFont;

                    if (_hasMediaDateColumn)
                    {
                        string mediaText = isMediaActive ? $"✔ {AppConstants.FormatDisplayDate(file.MediaDateTaken!.Value)}" : "—";
                        var subMedia = item.SubItems.Add(mediaText);
                        subMedia.ForeColor = isMediaActive ? palette.ListDateActive : palette.ListDateMuted;
                        if (isMediaActive) subMedia.Font = boldFont;
                    }

                    var subTarget = item.SubItems.Add($"📁 {file.TargetFolder}");
                    subTarget.ForeColor = palette.ListTargetFolder;
                    subTarget.Font = boldFont;

                    item.SubItems.Add(""); // Status column placeholder
                    item.SubItems.Add(file.IsDirectory ? "—" : FormatFileSize(file.Size));
                    item.Tag = file;
                    UpdateItemStatusDisplay(item, file, palette);
                    itemsToAdd.Add(item);
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
                    itemsToAdd.Add(item);

                    if (_btnLoadMore != null)
                    {
                        _btnLoadMore.Text = $"➕ Load 1,000 More ({remaining:N0} remaining)";
                        _pnlLoadMore.Visible = true;
                        _pnlLoadMore.SendToBack();
                    }
                }
                else if (_pnlLoadMore != null) _pnlLoadMore.Visible = false;

                _lstFiles.Items.AddRange(itemsToAdd.ToArray());
                UpdatePreviewHeaderCount(displayedFiles.Count, total);
            }
            finally
            {
                _lstFiles.EndUpdate();
                _isUpdatingList = wasUpdating;
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

            var selectedFiles = _filesToOrganize.Where(f => f.IsSelected && !f.IsExcludedByRule).ToList();
            int selCount = selectedFiles.Count, excludedCount = _filesToOrganize.Count(f => f.IsExcludedByRule), unselCount = _filesToOrganize.Count(f => !f.IsSelected && !f.IsExcludedByRule);

            if (selCount == 0)
            {
                _lblSummary.Font = new Font(_lblSummary.Font, FontStyle.Regular);
                _lblSummary.ForeColor = _settings.DarkMode ? Color.FromArgb(251, 191, 36) : Color.FromArgb(217, 119, 6);
                _lblSummary.Text = "⚠ No items selected to organize (all items are unchecked or excluded).";
                _btnStart.Enabled = false;
                return;
            }

            var grouped = _organizerService?.GroupByMonthYear(selectedFiles) ?? new Dictionary<string, List<FileItem>>();
            var fileCount = selectedFiles.Count(f => !f.IsDirectory);
            var folderCount = selectedFiles.Count(f => f.IsDirectory);

            _lblSummary.Font = new Font(_lblSummary.Font, FontStyle.Bold);
            _lblSummary.ForeColor = _settings.DarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(4, 120, 87);
            string label = _settings.OrgMode == OrganizationMode.Category ? "category folder(s)" : (_settings.OrgMode == OrganizationMode.Extension ? "extension folder(s)" : (_settings.OrgMode == OrganizationMode.Date ? "date folder(s)" : "folder(s)"));
            string baseSummary = folderCount > 0
                ? $"✔ Ready: {selCount:N0} selected ({fileCount:N0} files, {folderCount:N0} folders) → {grouped.Count:N0} target {label}"
                : $"✔ Ready: {selCount:N0} selected file(s) → {grouped.Count:N0} target {label}";
            if (excludedCount > 0 || unselCount > 0) baseSummary += $" ({excludedCount + unselCount:N0} excluded/skipped)";
            _lblSummary.Text = baseSummary;
            _btnStart.Enabled = true;
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
            if (grouped.Count == 0) { _pnlTimestampWarning.Visible = false; _pnlCenterSection.PerformLayout(); return; }

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

            if (_filesToOrganize.Count == 0)
            {
                MessageBox.Show("Please select a folder to preview files first.", "No Files Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information); return;
            }

            var itemsToOrganize = _filesToOrganize.Where(f => f.IsSelected && !f.IsExcludedByRule).ToList();
            if (itemsToOrganize.Count == 0)
            {
                MessageBox.Show("No items are selected to organize. Please check at least one item in the preview list.", "No Items Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
            string skipNotice = _filesToOrganize.Count > itemsToOrganize.Count
                ? $"\n({_filesToOrganize.Count - itemsToOrganize.Count:N0} unchecked/excluded item(s) will remain untouched at source)\n"
                : "";

            var result = MessageBox.Show(
                $"You are about to organize {itemsToOrganize.Count:N0} selected item(s) into date-based folders.\n" +
                skipNotice +
                $"\nSource: {_txtSourceFolder.Text}\n" +
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
                if (_organizerService?.LastResult != null && _organizerService.LastResult.FilesMoved > 0)
                {
                    RecordUndoSession(_organizerService.LastResult);
                }
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
