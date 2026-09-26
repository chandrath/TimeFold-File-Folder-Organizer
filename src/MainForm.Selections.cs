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

        public void ShowFolderExclusionDialog()
        {
            using var dlg = new FolderExclusionDialog(_settings, _settings.DarkMode);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _settings.SaveToFile();
                LoadPreview();
            }
        }

        public void ToggleFolderExclusions()
        {
            _settings.EnableFolderExclusions = !_settings.EnableFolderExclusions;
            _settings.SaveToFile();
            LoadPreview();
        }

        public void AddFolderExclusionRule(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName)) return;
            string clean = folderName.Trim();
            if (_settings.ExcludedFolderNames == null) _settings.ExcludedFolderNames = new();
            if (!_settings.ExcludedFolderNames.Any(f => string.Equals(f.Trim(), clean, StringComparison.OrdinalIgnoreCase)))
            {
                _settings.ExcludedFolderNames.Add(clean);
                _settings.EnableFolderExclusions = true;
                _settings.SaveToFile();
                LoadPreview();
                MessageBox.Show($"Folder '{clean}' has been added to your Exclusion List ('Never Touch').\nIt will now be preserved untouched in all organization modes.", "Folder Excluded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void SetAllItemsSelected(bool selected)
        {
            if (_filesToOrganize.Count == 0) return;
            _isUpdatingList = true;
            _lstFiles.BeginUpdate();
            try
            {
                foreach (var file in _filesToOrganize)
                {
                    if (!file.IsExcludedByRule) file.IsSelected = selected;
                }

                var palette = AppTheme.GetPalette(_settings.DarkMode);
                foreach (ListViewItem item in _lstFiles.Items)
                {
                    if (item.Tag is FileItem file && !file.IsExcludedByRule)
                    {
                        item.Checked = selected;
                        UpdateItemStatusDisplay(item, file, palette);
                    }
                }
            }
            finally
            {
                _lstFiles.EndUpdate();
                _isUpdatingList = false;
            }

            CheckConflicts();
            UpdateSummary();
            int maxPreview = _currentPreviewLimit > 0 ? _currentPreviewLimit : (_settings.MaxPreviewItems > 0 ? _settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems);
            UpdatePreviewHeaderCount(Math.Min(maxPreview, _filesToOrganize.Count), _filesToOrganize.Count);
        }

        public void InvertItemSelection()
        {
            if (_filesToOrganize.Count == 0) return;
            _isUpdatingList = true;
            _lstFiles.BeginUpdate();
            try
            {
                foreach (var file in _filesToOrganize)
                {
                    if (!file.IsExcludedByRule) file.IsSelected = !file.IsSelected;
                }

                var palette = AppTheme.GetPalette(_settings.DarkMode);
                foreach (ListViewItem item in _lstFiles.Items)
                {
                    if (item.Tag is FileItem file && !file.IsExcludedByRule)
                    {
                        item.Checked = file.IsSelected;
                        UpdateItemStatusDisplay(item, file, palette);
                    }
                }
            }
            finally
            {
                _lstFiles.EndUpdate();
                _isUpdatingList = false;
            }

            CheckConflicts();
            UpdateSummary();
            int maxPreview = _currentPreviewLimit > 0 ? _currentPreviewLimit : (_settings.MaxPreviewItems > 0 ? _settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems);
            UpdatePreviewHeaderCount(Math.Min(maxPreview, _filesToOrganize.Count), _filesToOrganize.Count);
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
                            $"'{file.Name}' is excluded by your Folder Exclusion Rules ('Never Touch').\n\nTo organize this folder, click 'Also organize folders ▼' > '🛡 Exclude / Ignore Folders...' or disable exclusion rules.",
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
            if (item.SubItems.Count <= 5) return;
            var boldFont = GetBoldListFont();
            var conflict = _currentConflicts.FirstOrDefault(c => c.Item == file);

            if (file.IsExcludedByRule)
            {
                item.SubItems[5].Text = "🛡 Excluded (Rule)";
                item.SubItems[5].ForeColor = _settings.DarkMode ? Color.FromArgb(251, 191, 36) : Color.FromArgb(217, 119, 6);
                item.SubItems[5].Font = boldFont;
                item.ForeColor = palette.TextMuted;
            }
            else if (!file.IsSelected)
            {
                item.SubItems[5].Text = "○ Unchecked";
                item.SubItems[5].ForeColor = palette.TextMuted;
                item.SubItems[5].Font = _lstFiles.Font;
                item.ForeColor = palette.TextMuted;
            }
            else
            {
                item.SubItems[5].Text = conflict?.ShortStatus ?? "✓ Ready";
                item.SubItems[5].ForeColor = conflict != null
                    ? (_settings.DarkMode ? Color.FromArgb(248, 113, 113) : Color.FromArgb(220, 38, 38))
                    : (_settings.DarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(22, 101, 52));
                item.SubItems[5].Font = conflict != null ? boldFont : _lstFiles.Font;
                item.ForeColor = palette.TextPrimary;
            }
        }

        private int _sortColumn = 2;
        private bool _sortAscending = false;
        private static readonly string[] ColumnBaseHeaders = { "File Name", "Type", "Modified Date", "Created Date", "📁 Target Folder", "Status", "Size" };

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
    }
}
