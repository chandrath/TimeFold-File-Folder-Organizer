using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Models;
using FileOrganizer.Services;
using Microsoft.VisualBasic.FileIO;

namespace FileOrganizer
{
    public partial class MainForm
    {
        private ToolStripMenuItem _menuItemOpen = null!;
        private ToolStripMenuItem _menuItemExplorer = null!;
        private ToolStripMenuItem _menuItemCopy = null!;
        private ToolStripMenuItem _menuItemRenameCategory = null!;
        private ToolStripMenuItem _menuItemResetCategoryName = null!;
        private ToolStripMenuItem _menuItemChangeCategory = null!;
        private ToolStripMenuItem _menuItemRename = null!;
        private ToolStripMenuItem _menuItemDelete = null!;
        private ToolStripMenuItem _menuItemAutoFit = null!, _menuItemToggleStatus = null!;
        private ToolStripMenuItem _menuItemSelectAll = null!, _menuItemDeselectAll = null!, _menuItemInvertSelection = null!, _menuItemExcludeFolder = null!;
        private ToolStripSeparator _sepTargetFolder = null!;
        private ToolStripSeparator _sepFileOps = null!;
        private ToolStripSeparator _sepAutoFit = null!;
        private ToolStripSeparator _sepSelection = null!;
        private Point _lastRightClickPoint;

        private void InitializeContextMenu()
        {
            _ctxFileMenu = new ContextMenuStrip { ShowImageMargin = false };

            _menuItemSelectAll = new ToolStripMenuItem("☑ Select All", null, (s, e) => SetAllItemsSelected(true));
            _menuItemDeselectAll = new ToolStripMenuItem("☐ Deselect All", null, (s, e) => SetAllItemsSelected(false));
            _menuItemInvertSelection = new ToolStripMenuItem("🔀 Invert Selection", null, (s, e) => InvertItemSelection());
            _menuItemExcludeFolder = new ToolStripMenuItem("🛡 Ignore Folder...", null, (s, e) =>
            {
                if (_lstFiles.SelectedItems.Count > 0 && _lstFiles.SelectedItems[0].Tag is FileItem file && file.IsDirectory)
                    ToggleFolderIgnore(file.Name);
            });
            _sepSelection = new ToolStripSeparator();

            _menuItemOpen = new ToolStripMenuItem("📄 Open", null, (s, e) => OpenSelectedFile());
            _menuItemExplorer = new ToolStripMenuItem("📂 Open in File Explorer", null, (s, e) => OpenSelectedInExplorer());
            _menuItemCopy = new ToolStripMenuItem("📋 Copy File Path", null, (s, e) => CopySelectedPath());
            _menuItemRenameCategory = new ToolStripMenuItem("🏷️ Rename Category...", null, (s, e) => RenameCurrentCategory());
            _menuItemResetCategoryName = new ToolStripMenuItem("↺ Revert Category to Factory Name", null, (s, e) => ResetCurrentCategoryName());
            _menuItemChangeCategory = new ToolStripMenuItem("📁 Set target folder for all files...", null, (s, e) => ChangeCategoryForSelectedExtension());
            _menuItemRename = new ToolStripMenuItem("✏ Rename File...", null, (s, e) => RenameSelectedFile());
            _menuItemDelete = new ToolStripMenuItem("🗑 Delete (Recycle Bin)", null, (s, e) => DeleteSelectedFile());
            _menuItemAutoFit = new ToolStripMenuItem("↔ Auto-fit All Columns", null, (s, e) => AutoFitColumns());
            _menuItemToggleStatus = new ToolStripMenuItem("👁 Toggle Status Column", null, (s, e) => ToggleStatusColumn());

            _sepTargetFolder = new ToolStripSeparator();
            _sepFileOps = new ToolStripSeparator();
            _sepAutoFit = new ToolStripSeparator();

            _ctxFileMenu.Items.AddRange(new ToolStripItem[]
            {
                _menuItemExcludeFolder,
                _menuItemOpen,
                _menuItemExplorer,
                _menuItemCopy,
                _sepFileOps,
                _menuItemRename,
                _menuItemDelete,
                _sepTargetFolder,
                _menuItemRenameCategory,
                _menuItemResetCategoryName,
                _menuItemChangeCategory,
                _sepSelection,
                _menuItemSelectAll,
                _menuItemDeselectAll,
                _menuItemInvertSelection,
                _sepAutoFit,
                _menuItemAutoFit,
                _menuItemToggleStatus
            });

            _lstFiles.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    _lastRightClickPoint = e.Location;
                }
            };

            _ctxFileMenu.Opening += (s, e) =>
            {
                Point pt = _lstFiles.PointToClient(Cursor.Position);
                var hit = _lstFiles.HitTest(pt);
                if (hit.Item == null) hit = _lstFiles.HitTest(_lastRightClickPoint);

                if (hit.Item == null)
                {
                    if (_filesToOrganize.Count == 0) { e.Cancel = true; return; }
                    foreach (ToolStripItem it in _ctxFileMenu.Items) it.Visible = false;
                    _menuItemSelectAll.Visible = true;
                    _menuItemDeselectAll.Visible = true;
                    _menuItemInvertSelection.Visible = true;
                    _sepSelection.Visible = true;
                    _menuItemAutoFit.Visible = true;
                    _menuItemToggleStatus.Visible = true;
                    return;
                }

                if (!hit.Item.Selected)
                {
                    _lstFiles.SelectedItems.Clear();
                    hit.Item.Selected = true;
                }

                var selItem = hit.Item.Tag as FileItem;
                bool isFileWithExt = selItem != null && !selItem.IsDirectory && !string.IsNullOrEmpty(selItem.Extension);
                bool isDirectory = selItem != null && selItem.IsDirectory;
                bool isCatMode = _settings.OrgMode == OrganizationMode.Category
                    || _settings.OrgMode == OrganizationMode.CategoryAndDate
                    || _settings.OrgMode == OrganizationMode.DateAndCategory
                    || _settings.OrgMode == OrganizationMode.Extension;

                _menuItemExcludeFolder.Visible = isDirectory;
                if (isDirectory && selItem != null)
                {
                    bool hasMarker = File.Exists(Path.Combine(selItem.FullPath, AppConstants.TimefoldIgnoreFileName));
                    bool isIgnored = hasMarker || selItem.IsExcludedByRule;
                    _menuItemExcludeFolder.Text = isIgnored
                        ? $"✓ Stop ignoring folder '{selItem.Name}'"
                        : $"🛡 Ignore folder '{selItem.Name}'...";
                }

                _menuItemOpen.Visible = true;
                _menuItemExplorer.Visible = true;
                _menuItemCopy.Visible = true;
                _sepFileOps.Visible = true;
                _menuItemRename.Visible = !isDirectory;
                _menuItemDelete.Visible = true;

                string currentCategory = isFileWithExt ? FileTypeService.Instance.GetCategory(selItem!.Extension) : "";
                string originalFactoryName = "";
                bool isRenamed = isFileWithExt && isCatMode && FileTypeService.Instance.IsCategoryRenamed(currentCategory, out originalFactoryName);

                _sepTargetFolder.Visible = isFileWithExt && isCatMode;
                _menuItemRenameCategory.Visible = isFileWithExt && isCatMode;
                if (_menuItemRenameCategory.Visible) _menuItemRenameCategory.Text = $"🏷️ Rename Category '{currentCategory}'...";

                _menuItemResetCategoryName.Visible = isRenamed && isCatMode;
                if (_menuItemResetCategoryName.Visible) _menuItemResetCategoryName.Text = $"↺ Revert Category to Factory Name ('{originalFactoryName}')";

                _menuItemChangeCategory.Visible = isFileWithExt && isCatMode;
                if (_menuItemChangeCategory.Visible) _menuItemChangeCategory.Text = $"📁 Set target folder for all {selItem!.Extension.ToLowerInvariant()} files...";

                _sepSelection.Visible = true;
                _menuItemSelectAll.Visible = true;
                _menuItemDeselectAll.Visible = true;
                _menuItemInvertSelection.Visible = true;
                _sepAutoFit.Visible = true;
                _menuItemAutoFit.Visible = true;
                _menuItemToggleStatus.Visible = true;
            };

            _lstFiles.ContextMenuStrip = _ctxFileMenu;
            _lstFiles.DoubleClick += (s, e) => OpenSelectedFile();
        }

        private FileItem? GetSelectedFileItem()
        {
            if (_lstFiles.SelectedItems.Count == 0) return null;
            return _lstFiles.SelectedItems[0].Tag as FileItem;
        }

        private void OpenSelectedFile()
        {
            var item = GetSelectedFileItem();
            if (item == null) return;
            try
            {
                Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open file:\n\n{ex.Message}", "Error Opening Item", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSelectedInExplorer()
        {
            var item = GetSelectedFileItem();
            if (item == null) return;
            try
            {
                if (File.Exists(item.FullPath))
                {
                    Process.Start("explorer.exe", $"/select,\"{item.FullPath}\"");
                }
                else if (Directory.Exists(item.FullPath))
                {
                    Process.Start("explorer.exe", $"\"{item.FullPath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open Explorer:\n\n{ex.Message}", "Error Opening Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopySelectedPath()
        {
            var item = GetSelectedFileItem();
            if (item == null) return;
            try
            {
                Clipboard.SetText(item.FullPath);
            }
            catch { }
        }

        private void RenameSelectedFile()
        {
            var item = GetSelectedFileItem();
            if (item == null) return;

            string oldName = item.Name;
            string? newName = ShowRenamePrompt(oldName, _settings.DarkMode);
            if (string.IsNullOrWhiteSpace(newName) || string.Equals(newName.Trim(), oldName, StringComparison.Ordinal))
            {
                return;
            }

            newName = newName.Trim();
            string? parentDir = Path.GetDirectoryName(item.FullPath);
            if (string.IsNullOrEmpty(parentDir)) return;

            string newPath = Path.Combine(parentDir, newName);

            try
            {
                if (item.IsDirectory)
                {
                    if (Directory.Exists(newPath))
                    {
                        MessageBox.Show($"A folder named '{newName}' already exists in this location.", "Name Collision", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    Directory.Move(item.FullPath, newPath);
                }
                else
                {
                    if (File.Exists(newPath))
                    {
                        MessageBox.Show($"A file named '{newName}' already exists in this location.", "Name Collision", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    File.Move(item.FullPath, newPath);
                }

                LoadPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to rename item:\n\n{ex.Message}", "Rename Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelectedFile()
        {
            var item = GetSelectedFileItem();
            if (item == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to send this {(item.IsDirectory ? "folder" : "file")} to the Recycle Bin?\n\n{item.Name}",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                if (item.IsDirectory)
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(item.FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                else
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(item.FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }

                LoadPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete item:\n\n{ex.Message}", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string? ShowRenamePrompt(string currentName, bool isDark)
        {
            var palette = AppTheme.GetPalette(isDark);
            using var prompt = new Form
            {
                Text = "Rename Item",
                Size = new Size(420, 175),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = palette.CanvasBg,
                ForeColor = palette.TextPrimary
            };
            AppTheme.SetWindowDarkTitleBar(prompt.Handle, isDark);

            var lbl = new Label { Text = "Enter new name:", Location = new Point(18, 14), AutoSize = true, ForeColor = palette.TextPrimary };
            var txt = new TextBox { Text = currentName, Location = new Point(20, 38), Width = 365, Font = new Font("Segoe UI", 9.5F), BackColor = palette.InputBg, ForeColor = palette.TextPrimary };
            var btnOk = new Button { Text = "Rename", DialogResult = DialogResult.OK, Location = new Point(210, 85), Size = new Size(85, 32), BackColor = AppConstants.ColorPrimary, ForeColor = Color.White };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(300, 85), Size = new Size(85, 32), BackColor = palette.SecondaryButtonBg, ForeColor = palette.SecondaryButtonText };

            prompt.Controls.AddRange([lbl, txt, btnOk, btnCancel]);
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCancel;

            // Highlight filename without extension
            txt.Select();
            int dotIndex = currentName.LastIndexOf('.');
            if (dotIndex > 0) txt.Select(0, dotIndex);
            else txt.SelectAll();

            return prompt.ShowDialog() == DialogResult.OK ? txt.Text : null;
        }

        private void RenameCurrentCategory()
        {
            var item = GetSelectedFileItem();
            if (item == null || item.IsDirectory || string.IsNullOrWhiteSpace(item.Extension)) return;

            string ext = item.Extension.ToLowerInvariant();
            string currentCategory = FileTypeService.Instance.GetCategory(ext);

            string? newName = ShowRenamePrompt(currentCategory, _settings.DarkMode);
            if (!string.IsNullOrWhiteSpace(newName) && !string.Equals(newName.Trim(), currentCategory, StringComparison.OrdinalIgnoreCase))
            {
                FileTypeService.Instance.RenameCategory(currentCategory, newName.Trim());
                ReapplyOrganizationMode();
            }
        }

        private void ResetCurrentCategoryName()
        {
            var item = GetSelectedFileItem();
            if (item == null || item.IsDirectory || string.IsNullOrWhiteSpace(item.Extension)) return;
            if (FileTypeService.Instance.ResetCategoryName(FileTypeService.Instance.GetCategory(item.Extension.ToLowerInvariant())))
                ReapplyOrganizationMode();
        }

        private void ChangeCategoryForSelectedExtension()
        {
            var item = GetSelectedFileItem();
            if (item == null || item.IsDirectory || string.IsNullOrWhiteSpace(item.Extension)) return;

            string ext = item.Extension.ToLowerInvariant();
            bool hasCustomOverride = FileTypeService.Instance.IsCustomRoute(ext);
            string currentCategory = _settings.OrgMode == OrganizationMode.Extension
                ? (FileTypeService.Instance.TryGetExtensionOverride(ext, out var ovr) ? ovr : ext.TrimStart('.').ToUpperInvariant())
                : FileTypeService.Instance.GetCategory(ext);
            var categories = FileTypeService.Instance.GetAllCategories();

            var (chosenCategory, resetRequested) = ShowCategoryPickerPrompt(ext, currentCategory, categories, hasCustomOverride, _settings.DarkMode);
            if (resetRequested)
            {
                FileTypeService.Instance.RemoveCategoryOverride(ext);
                ReapplyOrganizationMode();
            }
            else if (!string.IsNullOrWhiteSpace(chosenCategory))
            {
                if (_settings.OrgMode == OrganizationMode.Extension && string.Equals(chosenCategory, ext.TrimStart('.'), StringComparison.OrdinalIgnoreCase))
                    FileTypeService.Instance.RemoveCategoryOverride(ext);
                else
                    FileTypeService.Instance.SetCategoryOverride(ext, chosenCategory);
                ReapplyOrganizationMode();
            }
        }

        private static (string? category, bool resetRequested) ShowCategoryPickerPrompt(string ext, string currentCategory, List<string> categories, bool hasCustomOverride, bool isDark)
        {
            var palette = AppTheme.GetPalette(isDark);
            using var prompt = new Form
            {
                Text = $"Set Target Folder for {ext}",
                Size = new Size(490, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = palette.CanvasBg,
                ForeColor = palette.TextPrimary,
                Font = new Font("Segoe UI", 9F)
            };
            AppTheme.SetWindowDarkTitleBar(prompt.Handle, isDark);

            var lblDesc = new Label
            {
                Text = $"All '{ext}' files are currently routed to: {currentCategory}\r\nSelect an existing category or type a custom destination folder:",
                Location = new Point(20, 16),
                Size = new Size(435, 38),
                ForeColor = palette.TextPrimary
            };

            var cbo = new ComboBox
            {
                Location = new Point(22, 64),
                Width = 430,
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 10F),
                BackColor = palette.InputBg,
                ForeColor = palette.TextPrimary,
                MaxLength = 50
            };
            foreach (var cat in categories) cbo.Items.Add(cat);
            cbo.Text = currentCategory;

            bool resetClicked = false;
            var btnReset = new Button
            {
                Text = "↺ Reset to Default",
                Location = new Point(22, 125),
                Size = new Size(130, 32),
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText,
                FlatStyle = FlatStyle.Flat,
                Visible = hasCustomOverride,
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;
            btnReset.Click += (s, a) => { resetClicked = true; prompt.DialogResult = DialogResult.OK; prompt.Close(); };

            var btnOk = new Button
            {
                Text = "Apply",
                Location = new Point(266, 125),
                Size = new Size(90, 32),
                BackColor = AppConstants.ColorPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, a) =>
            {
                string text = cbo.Text.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show(prompt, "Please enter a destination folder name.", "Empty Folder Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                char[] invalid = Path.GetInvalidFileNameChars();
                if (text.IndexOfAny(invalid) >= 0)
                {
                    MessageBox.Show(prompt, "Folder name cannot contain any of the following characters:\n\\ / : * ? \" < > |", "Invalid Characters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                prompt.DialogResult = DialogResult.OK;
                prompt.Close();
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(362, 125),
                Size = new Size(90, 32),
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;

            prompt.Controls.AddRange([lblDesc, cbo, btnReset, btnOk, btnCancel]);
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCancel;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                if (resetClicked) return (null, true);
                return (cbo.Text.Trim(), false);
            }
            return (null, false);
        }

        private void ToggleStatusColumn()
        {
            if (_lstFiles.Columns.Count > 5)
            {
                bool isHidden = _lstFiles.Columns[5].Width == 0;
                _lstFiles.Columns[5].Width = isHidden ? 100 : 0;
                _menuItemToggleStatus.Text = isHidden ? "👁 Hide Status Column" : "👁 Show Status Column";
            }
        }
    }
}
