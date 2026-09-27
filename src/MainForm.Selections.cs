using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Forms;
using FileOrganizer.Models;

namespace FileOrganizer
{
    public partial class MainForm
    {
        private bool _isUpdatingList;
        private FlowLayoutPanel _flowPreviewHeader = null!;
        private Label _lblIgnoredHeader = null!;
        private Panel _pnlExclusionsPausedWarning = null!;
        private Label _lblExclusionsPaused = null!;

        private void InitializeExclusionControls(Panel pnlPreviewHeader, Panel pnlCenterSection)
        {
            _flowPreviewHeader = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            pnlPreviewHeader.Controls.Remove(_lblPreviewHeader);
            _lblPreviewHeader.Dock = DockStyle.None;
            _lblPreviewHeader.AutoSize = true;
            _lblPreviewHeader.Margin = new Padding(0, 3, 0, 0);

            _lblIgnoredHeader = new Label
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0, 3, 0, 0)
            };
            _lblIgnoredHeader.Click += (s, e) => ShowFolderExclusionDialog();
            _toolTip?.SetToolTip(_lblIgnoredHeader, "Click to view or edit folder exclusion rules");

            _flowPreviewHeader.Controls.AddRange([_lblPreviewHeader, _lblIgnoredHeader]);
            pnlPreviewHeader.Controls.Add(_flowPreviewHeader);

            _pnlExclusionsPausedWarning = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Padding = new Padding(8, 2, 8, 2),
                Margin = new Padding(0, 2, 0, 4),
                Cursor = Cursors.Hand
            };
            _lblExclusionsPaused = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            _pnlExclusionsPausedWarning.Click += (s, e) => ResumeExclusions();
            _lblExclusionsPaused.Click += (s, e) => ResumeExclusions();
            _pnlExclusionsPausedWarning.Controls.Add(_lblExclusionsPaused);
            pnlCenterSection.Controls.Add(_pnlExclusionsPausedWarning);
        }

        private void ResumeExclusions() { _settings.EnableFolderExclusions = true; _settings.SaveToFile(); LoadPreview(); }

        public void ShowFolderExclusionDialog()
        {
            using var dlg = new FolderExclusionDialog(_settings, _settings.DarkMode, _txtSourceFolder.Text);
            if (dlg.ShowDialog(this) == DialogResult.OK) { _settings.SaveToFile(); LoadPreview(); }
        }

        public void ToggleFolderExclusions() { _settings.EnableFolderExclusions = !_settings.EnableFolderExclusions; _settings.SaveToFile(); LoadPreview(); }

        public void ToggleFolderIgnore(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName) || string.IsNullOrWhiteSpace(_txtSourceFolder.Text)) return;
            string clean = folderName.Trim().TrimEnd('/', '\\');
            string folderPath = System.IO.Path.Combine(_txtSourceFolder.Text, clean);
            if (!System.IO.Directory.Exists(folderPath)) return;

            string markerPath = System.IO.Path.Combine(folderPath, AppConstants.TimefoldIgnoreFileName);
            bool isIgnoredNow = System.IO.File.Exists(markerPath);

            try
            {
                if (isIgnoredNow)
                {
                    if (System.IO.File.Exists(markerPath))
                    {
                        try { System.IO.File.SetAttributes(markerPath, System.IO.FileAttributes.Normal); } catch { }
                        System.IO.File.Delete(markerPath);
                    }
                }
                else
                {
                    if (_settings.ShowIgnoreMarkerWarning)
                    {
                        using var dlg = new Forms.IgnoreFolderConfirmDialog(clean, _settings.DarkMode);
                        if (dlg.ShowDialog(this) != DialogResult.OK) return;
                        if (dlg.DoNotShowAgain) { _settings.ShowIgnoreMarkerWarning = false; _settings.SaveToFile(); }
                    }
                    System.IO.File.WriteAllText(markerPath, AppConstants.TimefoldIgnoreFileContent);
                    try { System.IO.File.SetAttributes(markerPath, System.IO.FileAttributes.Hidden); } catch { }
                    _settings.EnableFolderExclusions = true;
                    _settings.SaveToFile();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to update folder ignore marker: {ex.Message}", "Ignore Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            LoadPreview();
        }

        private void CheckExclusionsPausedWarning()
        {
            if (_pnlExclusionsPausedWarning == null || _lblExclusionsPaused == null) return;
            if (_settings.EnableFolderExclusions || !_settings.IncludeTopLevelFolders) { _pnlExclusionsPausedWarning.Visible = false; return; }

            int markerCount = 0;
            if (_filesToOrganize.Count > 0)
            {
                markerCount = _filesToOrganize.Count(f => f.IsDirectory &&
                    System.IO.File.Exists(System.IO.Path.Combine(f.FullPath, AppConstants.TimefoldIgnoreFileName)));
            }

            if (markerCount > 0 && _filesToOrganize.Count > 0)
            {
                bool isDark = _settings.DarkMode;
                _pnlExclusionsPausedWarning.BackColor = isDark ? Color.FromArgb(69, 45, 10) : Color.FromArgb(254, 243, 199);
                _lblExclusionsPaused.ForeColor = isDark ? Color.FromArgb(252, 211, 77) : Color.FromArgb(146, 64, 14);
                _lblExclusionsPaused.Text = $"⏸ Notice: Folder exclusions are paused. {markerCount:N0} ignored folder(s) will be organized. Click here to resume protection →";
                _pnlExclusionsPausedWarning.Visible = true;
            }
            else
            {
                _pnlExclusionsPausedWarning.Visible = false;
            }
        }

        private void UpdateTwoToneHeader(int displayed, int total)
        {
            if (_lblPreviewHeader == null) return;
            if (total == 0)
            {
                _lblPreviewHeader.Text = "Live Preview";
                if (_lblIgnoredHeader != null) _lblIgnoredHeader.Visible = false;
                _toolTip?.SetToolTip(_lblPreviewHeader, null);
                return;
            }

            int ignoredCount = _filesToOrganize.Count(f => f.IsExcludedByRule);
            int selected = _filesToOrganize.Count(f => f.IsSelected && !f.IsExcludedByRule);
            bool isDark = _settings.DarkMode;

            if (ignoredCount > 0)
            {
                if (_lblIgnoredHeader != null)
                {
                    _lblIgnoredHeader.ForeColor = isDark ? Color.FromArgb(248, 113, 113) : Color.FromArgb(220, 38, 38);
                    _lblIgnoredHeader.Visible = true;
                }

                if (displayed < total)
                {
                    _lblPreviewHeader.Text = $"Live Preview (Showing {displayed:N0} of {total:N0} · ";
                    if (_lblIgnoredHeader != null) _lblIgnoredHeader.Text = $"{ignoredCount:N0} ignored)";
                }
                else
                {
                    _lblPreviewHeader.Text = $"Live Preview ({selected:N0} items · ";
                    if (_lblIgnoredHeader != null) _lblIgnoredHeader.Text = $"{ignoredCount:N0} folder{(ignoredCount == 1 ? "" : "s")} ignored)";
                }
            }
            else
            {
                if (_lblIgnoredHeader != null) _lblIgnoredHeader.Visible = false;
                _lblPreviewHeader.Text = displayed < total
                    ? $"Live Preview (Showing {displayed:N0} of {total:N0})"
                    : $"Live Preview ({total:N0} items)";
            }

            _toolTip?.SetToolTip(_lblPreviewHeader, $"Showing {displayed:N0} of {total:N0} items ({selected:N0} selected for organization, {ignoredCount:N0} ignored).\nTip: Check/uncheck boxes to include/exclude specific items.");
        }

        private void MutateSelection(Action<FileItem> mutate, Func<FileItem, bool> isChecked)
        {
            if (_filesToOrganize.Count == 0) return;
            _isUpdatingList = true;
            try
            {
                _lstFiles.BeginUpdate();
                try
                {
                    foreach (var file in _filesToOrganize)
                    {
                        if (!file.IsExcludedByRule) mutate(file);
                    }
                    var palette = AppTheme.GetPalette(_settings.DarkMode);
                    foreach (ListViewItem item in _lstFiles.Items)
                    {
                        if (item.Tag is FileItem file && !file.IsExcludedByRule)
                        {
                            item.Checked = isChecked(file);
                            UpdateItemStatusDisplay(item, file, palette);
                        }
                    }
                }
                finally { _lstFiles.EndUpdate(); }
                CheckConflicts();
                UpdateSummary();
                int maxPreview = _currentPreviewLimit > 0 ? _currentPreviewLimit : (_settings.MaxPreviewItems > 0 ? _settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems);
                UpdatePreviewHeaderCount(Math.Min(maxPreview, _filesToOrganize.Count), _filesToOrganize.Count);
            }
            finally { _isUpdatingList = false; }
        }

        public void SetAllItemsSelected(bool selected) => MutateSelection(f => f.IsSelected = selected, _ => selected);
        public void InvertItemSelection() => MutateSelection(f => f.IsSelected = !f.IsSelected, f => f.IsSelected);

        private bool _preventItemCheck = false;

        private void HookListCheckEvents()
        {
            _lstFiles.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    var hit = _lstFiles.HitTest(e.Location);
                    _preventItemCheck = !hit.Location.HasFlag(ListViewHitTestLocations.StateImage);
                }
            };
            _lstFiles.MouseUp += (s, e) => _preventItemCheck = false;
            _lstFiles.ItemCheck += (s, e) => HandleListItemCheck(e);
            _lstFiles.ItemChecked += (s, e) => HandleListItemChecked(e);
        }

        private void HandleListItemCheck(ItemCheckEventArgs e)
        {
            if (_isUpdatingList) return;
            if (_preventItemCheck)
            {
                e.NewValue = e.CurrentValue;
                _preventItemCheck = false;
                return;
            }
            if (Control.MouseButtons != MouseButtons.None)
            {
                Point pt = _lstFiles.PointToClient(Cursor.Position);
                var hit = _lstFiles.HitTest(pt);
                if (!hit.Location.HasFlag(ListViewHitTestLocations.StateImage))
                {
                    e.NewValue = e.CurrentValue;
                }
            }
        }

        private void HandleListItemChecked(ItemCheckedEventArgs e)
        {
            if (_isUpdatingList || e.Item == null) return;

            if (e.Item.Tag is FileItem file)
            {
                if (file.IsExcludedByRule)
                {
                    if (e.Item.Checked)
                    {
                        _isUpdatingList = true;
                        e.Item.Checked = false;
                        _isUpdatingList = false;
                        MessageBox.Show(
                            $"'{file.Name}' is excluded by your Folder Exclusion Rules.\n\nTo organize this folder, click 'Also organize folders ▼' > '🛡 Folder Exclusion Rules...' or right-click to stop ignoring.",
                            "Folder Excluded by Rule",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    return;
                }

                file.IsSelected = e.Item.Checked;
                var palette = AppTheme.GetPalette(_settings.DarkMode);
                UpdateItemStatusDisplay(e.Item, file, palette);

                CheckConflicts();
                UpdateSummary();
                int maxPreview = _currentPreviewLimit > 0 ? _currentPreviewLimit : (_settings.MaxPreviewItems > 0 ? _settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems);
                UpdatePreviewHeaderCount(Math.Min(maxPreview, _filesToOrganize.Count), _filesToOrganize.Count);
            }
            else if (e.Item.Tag is "LOAD_MORE" && e.Item.Checked)
            {
                _isUpdatingList = true;
                e.Item.Checked = false;
                _isUpdatingList = false;
            }
        }

        private void UpdateItemStatusDisplay(ListViewItem item, FileItem file, AppTheme.ThemePalette palette)
        {
            int statusIndex = _hasMediaDateColumn ? 6 : 5;
            int targetIndex = _hasMediaDateColumn ? 5 : 4;
            if (item.SubItems.Count <= statusIndex) return;
            var boldFont = GetBoldListFont();
            var conflict = _currentConflicts.FirstOrDefault(c => c.Item == file);

            if (file.IsExcludedByRule)
            {
                item.SubItems[statusIndex].Text = "🛡 Excluded (Rule)";
                item.SubItems[statusIndex].ForeColor = _settings.DarkMode ? Color.FromArgb(251, 191, 36) : Color.FromArgb(217, 119, 6);
                item.SubItems[statusIndex].Font = boldFont;
                item.SubItems[targetIndex].Text = "— (Ignored)";
                item.SubItems[targetIndex].ForeColor = palette.TextMuted;
                item.SubItems[targetIndex].Font = _lstFiles.Font;
                item.ForeColor = _settings.DarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
                MuteItemSubItems(item, file, statusIndex);
            }
            else if (!file.IsSelected)
            {
                item.SubItems[statusIndex].Text = "○ Unchecked";
                item.SubItems[statusIndex].ForeColor = palette.TextMuted;
                item.SubItems[statusIndex].Font = _lstFiles.Font;
                item.SubItems[targetIndex].Text = "— (Skipped)";
                item.SubItems[targetIndex].ForeColor = palette.TextMuted;
                item.SubItems[targetIndex].Font = _lstFiles.Font;
                item.ForeColor = palette.TextMuted;
                MuteItemSubItems(item, file, statusIndex);
            }
            else
            {
                item.SubItems[statusIndex].Text = conflict?.ShortStatus ?? "✓ Ready";
                item.SubItems[statusIndex].ForeColor = conflict != null
                    ? (_settings.DarkMode ? Color.FromArgb(248, 113, 113) : Color.FromArgb(220, 38, 38))
                    : (_settings.DarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(22, 101, 52));
                item.SubItems[statusIndex].Font = conflict != null ? boldFont : _lstFiles.Font;
                bool isCustom = !file.IsDirectory && Services.FileTypeService.Instance.IsCustomRoute(file.Extension);
                item.SubItems[targetIndex].Text = isCustom ? $"📁 {file.TargetFolder} (Custom)" : $"📁 {file.TargetFolder}";
                item.SubItems[targetIndex].ForeColor = isCustom ? palette.ListCustomTargetFolder : palette.ListTargetFolder;
                item.SubItems[targetIndex].Font = boldFont;
                RestoreItemSubItems(item, file, palette);
            }
        }

        private void MuteItemSubItems(ListViewItem item, FileItem file, int statusIndex)
        {
            for (int i = 1; i < item.SubItems.Count; i++)
            {
                if (i != statusIndex) item.SubItems[i].ForeColor = Color.FromArgb(156, 163, 175);
            }
            if (item.SubItems.Count > 2) { item.SubItems[2].Text = "   " + AppConstants.FormatDisplayDate(file.ModifiedDate); item.SubItems[2].Font = _lstFiles.Font; }
            if (item.SubItems.Count > 3) { item.SubItems[3].Text = "   " + AppConstants.FormatDisplayDate(file.CreatedDate); item.SubItems[3].Font = _lstFiles.Font; }
            if (_hasMediaDateColumn && item.SubItems.Count > 4)
            {
                item.SubItems[4].Text = file.MediaDateTaken.HasValue ? "   " + AppConstants.FormatDisplayDate(file.MediaDateTaken.Value) : "—";
                item.SubItems[4].Font = _lstFiles.Font;
            }
        }

        private void RestoreItemSubItems(ListViewItem item, FileItem file, AppTheme.ThemePalette palette)
        {
            var boldFont = GetBoldListFont();
            item.ForeColor = palette.TextPrimary;
            if (item.SubItems.Count > 1) item.SubItems[1].ForeColor = palette.ListText;
            bool isMediaActive = file.IsMediaDateActive && file.MediaDateTaken.HasValue;
            bool isModActive = !file.IsCreatedDateActive && !isMediaActive;
            bool isCreActive = file.IsCreatedDateActive && !isMediaActive;

            if (item.SubItems.Count > 2)
            {
                item.SubItems[2].Text = (isModActive ? "✔ " : "   ") + AppConstants.FormatDisplayDate(file.ModifiedDate);
                item.SubItems[2].Font = isModActive ? boldFont : _lstFiles.Font;
                item.SubItems[2].ForeColor = isModActive ? palette.ListDateActive : palette.ListDateMuted;
            }
            if (item.SubItems.Count > 3)
            {
                item.SubItems[3].Text = (isCreActive ? "✔ " : "   ") + AppConstants.FormatDisplayDate(file.CreatedDate);
                item.SubItems[3].Font = isCreActive ? boldFont : _lstFiles.Font;
                item.SubItems[3].ForeColor = isCreActive ? palette.ListDateActive : palette.ListDateMuted;
            }
            if (_hasMediaDateColumn && item.SubItems.Count > 4)
            {
                string mText = isMediaActive ? $"✔ {AppConstants.FormatDisplayDate(file.MediaDateTaken!.Value)}" : (file.MediaDateTaken.HasValue ? $"   {AppConstants.FormatDisplayDate(file.MediaDateTaken.Value)}" : "—");
                item.SubItems[4].Text = mText;
                item.SubItems[4].Font = isMediaActive ? boldFont : _lstFiles.Font;
                item.SubItems[4].ForeColor = isMediaActive ? palette.ListDateActive : palette.ListDateMuted;
            }
        }

        private int _sortColumn = 2;
        private bool _sortAscending = false;
        private static readonly string[] ColumnBaseHeadersStandard = { "File Name", "Type", "Modified Date", "Created Date", "📁 Target Folder", "Status", "Size" };
        private static readonly string[] ColumnBaseHeadersWithMedia = { "File Name", "Type", "Modified Date", "Created Date", "📷 Date Taken", "📁 Target Folder", "Status", "Size" };
        private string[] CurrentHeaders => _hasMediaDateColumn ? ColumnBaseHeadersWithMedia : ColumnBaseHeadersStandard;

        private void LstFiles_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (_sortColumn == e.Column) _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = e.Column;
                bool isDesc = e.Column == 2 || e.Column == 3 || (_hasMediaDateColumn ? (e.Column == 4 || e.Column == 6) : (e.Column == 5));
                _sortAscending = !isDesc;
            }

            UpdateColumnHeaderSortIndicators();
            FileItemComparer.Sort(_filesToOrganize, _sortColumn, _sortAscending, _hasMediaDateColumn);
            _isUpdatingList = true;
            try
            {
                UpdateFileList();
            }
            finally
            {
                _isUpdatingList = false;
            }
        }

        private void UpdateColumnHeaderSortIndicators()
        {
            var headers = CurrentHeaders;
            for (int i = 0; i < _lstFiles.Columns.Count && i < headers.Length; i++)
                _lstFiles.Columns[i].Text = (i == _sortColumn) ? $"{headers[i]} {(_sortAscending ? "▲" : "▼")}" : headers[i];
        }

        private bool _isAdjustingColumns;

        private void AutoFitColumns()
        {
            int minCols = _hasMediaDateColumn ? 8 : 7;
            if (_isAdjustingColumns || _lstFiles == null || _lstFiles.Columns.Count < minCols || _filesToOrganize.Count == 0) return;
            using (FileOrganizer.Services.PerfLogger.Measure("AutoFitColumns"))
            {
                _isAdjustingColumns = true;
                _lstFiles.BeginUpdate();
                try
                {
                    var boldFont = GetBoldListFont();
                    var headers = CurrentHeaders;

                    _lstFiles.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    _lstFiles.Columns[1].Width = Math.Max(_lstFiles.Columns[1].Width + 8, TextRenderer.MeasureText(headers[1], _lstFiles.Font).Width + 14);

                    _lstFiles.Columns[2].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    _lstFiles.Columns[2].Width = Math.Max(_lstFiles.Columns[2].Width + 10, TextRenderer.MeasureText(headers[2] + " ▼", _lstFiles.Font).Width + 14);

                    _lstFiles.Columns[3].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    _lstFiles.Columns[3].Width = Math.Max(_lstFiles.Columns[3].Width + 10, TextRenderer.MeasureText(headers[3], _lstFiles.Font).Width + 14);

                    int targetCol = _hasMediaDateColumn ? 5 : 4;
                    int statusCol = _hasMediaDateColumn ? 6 : 5;
                    int sizeCol = _hasMediaDateColumn ? 7 : 6;

                    if (_hasMediaDateColumn)
                    {
                        _lstFiles.Columns[4].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                        _lstFiles.Columns[4].Width = Math.Max(_lstFiles.Columns[4].Width + 10, TextRenderer.MeasureText(headers[4], _lstFiles.Font).Width + 16);
                    }

                    int maxTargetW = TextRenderer.MeasureText(headers[targetCol], _lstFiles.Font).Width + 16;
                    foreach (var f in _filesToOrganize)
                    {
                        string tgt = (!f.IsDirectory && Services.FileTypeService.Instance.IsCustomRoute(f.Extension)) ? $"📁 {f.TargetFolder} (Custom)" : $"📁 {f.TargetFolder}";
                        int w = TextRenderer.MeasureText(tgt, boldFont).Width + 16;
                        if (w > maxTargetW) maxTargetW = w;
                    }
                    _lstFiles.Columns[targetCol].Width = Math.Max(maxTargetW, 90);

                    _lstFiles.Columns[statusCol].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    _lstFiles.Columns[statusCol].Width = Math.Max(_lstFiles.Columns[statusCol].Width + 10, TextRenderer.MeasureText(headers[statusCol], _lstFiles.Font).Width + 16);

                    _lstFiles.Columns[sizeCol].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    _lstFiles.Columns[sizeCol].Width = Math.Max(_lstFiles.Columns[sizeCol].Width + 8, TextRenderer.MeasureText(headers[sizeCol], _lstFiles.Font).Width + 14);

                    int otherW = 0;
                    for (int i = 1; i < _lstFiles.Columns.Count; i++) otherW += _lstFiles.Columns[i].Width;
                    int availW = _lstFiles.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
                    _lstFiles.Columns[0].Width = Math.Max(180, availW - otherW);
                }
                finally
                {
                    _lstFiles.EndUpdate();
                    _isAdjustingColumns = false;
                }
            }
        }
    }
}
