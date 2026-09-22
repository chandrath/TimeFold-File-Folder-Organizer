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
        private ToolStripSeparator _sepTargetFolder = null!;
        private ToolStripSeparator _sepFileOps = null!;
        private ToolStripSeparator _sepAutoFit = null!;
        private Point _lastRightClickPoint;

        private void InitializeContextMenu()
        {
            _ctxFileMenu = new ContextMenuStrip { ShowImageMargin = false };

            _menuItemOpen = new ToolStripMenuItem("📄 Open", null, (s, e) => OpenSelectedFile());
            _menuItemExplorer = new ToolStripMenuItem("📂 Open in File Explorer", null, (s, e) => OpenSelectedInExplorer());
            _menuItemCopy = new ToolStripMenuItem("📋 Copy File Path", null, (s, e) => CopySelectedPath());
            _menuItemRenameCategory = new ToolStripMenuItem("🏷️ Rename Category...", null, (s, e) => RenameCurrentCategory());
            _menuItemResetCategoryName = new ToolStripMenuItem("↺ Revert Category to Factory Name", null, (s, e) => ResetCurrentCategoryName());
            _menuItemChangeCategory = new ToolStripMenuItem("🔀 Remap all files...", null, (s, e) => ChangeCategoryForSelectedExtension());
            _menuItemRename = new ToolStripMenuItem("✏ Rename File...", null, (s, e) => RenameSelectedFile());
            _menuItemDelete = new ToolStripMenuItem("🗑 Delete (Recycle Bin)", null, (s, e) => DeleteSelectedFile());
            _menuItemAutoFit = new ToolStripMenuItem("↔ Auto-fit All Columns", null, (s, e) => AutoFitColumns());
            _menuItemToggleStatus = new ToolStripMenuItem("👁 Toggle Status Column", null, (s, e) => ToggleStatusColumn());

            _sepTargetFolder = new ToolStripSeparator();
            _sepFileOps = new ToolStripSeparator();
            _sepAutoFit = new ToolStripSeparator();

            _ctxFileMenu.Items.AddRange(new ToolStripItem[]
            {
                _menuItemAutoFit,
                _menuItemToggleStatus,
                _sepAutoFit,
                _menuItemRenameCategory,
                _menuItemResetCategoryName,
                _menuItemChangeCategory,
                _sepTargetFolder,
                _menuItemOpen,
                _menuItemExplorer,
                _menuItemCopy,
                _sepFileOps,
                _menuItemRename,
                _menuItemDelete
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
                var hit = _lstFiles.HitTest(_lastRightClickPoint);
                if (hit.Item == null)
                {
                    if (_filesToOrganize.Count == 0) { e.Cancel = true; return; }
                    foreach (ToolStripItem it in _ctxFileMenu.Items) it.Visible = false;
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
                int colIndex = (hit.SubItem != null) ? hit.Item.SubItems.IndexOf(hit.SubItem) : 0;
                bool isTargetFolderCell = (colIndex == 4);

                string currentCategory = isFileWithExt ? FileTypeService.Instance.GetCategory(selItem!.Extension) : "";
                string originalFactoryName = "";
                bool isRenamed = isFileWithExt && FileTypeService.Instance.IsCategoryRenamed(currentCategory, out originalFactoryName);

                if (isTargetFolderCell && isFileWithExt)
                {
                    // Target Folder cell right-click: focus on category & destination actions
                    _menuItemRenameCategory.Visible = true;
                    _menuItemRenameCategory.Text = $"🏷️ Rename Category '{currentCategory}'...";

                    _menuItemResetCategoryName.Visible = isRenamed;
                    if (isRenamed)
                    {
                        _menuItemResetCategoryName.Text = $"↺ Revert Category to Factory Name ('{originalFactoryName}')";
                    }

                    _menuItemChangeCategory.Visible = true;
                    _menuItemChangeCategory.Text = $"🔀 Remap all {selItem!.Extension.ToLowerInvariant()} files to another category...";

                    _sepTargetFolder.Visible = true;
                    _menuItemOpen.Visible = true;
                    _menuItemExplorer.Visible = true;
                    _menuItemCopy.Visible = true;
                    _sepFileOps.Visible = false;
                    _menuItemRename.Visible = false;
                    _menuItemDelete.Visible = false;
                }
                else
                {
                    // File Name / General Row right-click: focus on file operations
                    _menuItemOpen.Visible = true;
                    _menuItemExplorer.Visible = true;
                    _menuItemCopy.Visible = true;
                    _sepFileOps.Visible = true;
                    _menuItemRename.Visible = true;
                    _menuItemDelete.Visible = true;

                    _sepTargetFolder.Visible = isFileWithExt;
                    _menuItemRenameCategory.Visible = isFileWithExt;
                    if (isFileWithExt)
                    {
                        _menuItemRenameCategory.Text = $"🏷️ Rename Category '{currentCategory}'...";
                    }

                    _menuItemResetCategoryName.Visible = isRenamed;
                    if (isRenamed)
                    {
                        _menuItemResetCategoryName.Text = $"↺ Revert Category to Factory Name ('{originalFactoryName}')";
                    }

                    _menuItemChangeCategory.Visible = isFileWithExt;
                    if (isFileWithExt)
                    {
                        _menuItemChangeCategory.Text = $"🔀 Remap all {selItem!.Extension.ToLowerInvariant()} files...";
                    }
                }
                _sepAutoFit.Visible = true;
                _menuItemAutoFit.Visible = true;
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

            string ext = item.Extension.ToLowerInvariant();
            string currentCategory = FileTypeService.Instance.GetCategory(ext);

            if (FileTypeService.Instance.ResetCategoryName(currentCategory))
            {
                ReapplyOrganizationMode();
            }
        }

        private void ChangeCategoryForSelectedExtension()
        {
            var item = GetSelectedFileItem();
            if (item == null || item.IsDirectory || string.IsNullOrWhiteSpace(item.Extension)) return;

            string ext = item.Extension.ToLowerInvariant();
            string currentCategory = FileTypeService.Instance.GetCategory(ext);
            var categories = FileTypeService.Instance.GetAllCategories();

            string? chosenCategory = ShowCategoryPickerPrompt(ext, currentCategory, categories, _settings.DarkMode);
            if (!string.IsNullOrWhiteSpace(chosenCategory) && !string.Equals(chosenCategory, currentCategory, StringComparison.OrdinalIgnoreCase))
            {
                FileTypeService.Instance.SetCategoryOverride(ext, chosenCategory);
                ReapplyOrganizationMode();
            }
        }

        private static string? ShowCategoryPickerPrompt(string ext, string currentCategory, List<string> categories, bool isDark)
        {
            var palette = AppTheme.GetPalette(isDark);
            using var prompt = new Form
            {
                Text = $"Change Destination for {ext}",
                Size = new Size(460, 220),
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
                Size = new Size(405, 38),
                ForeColor = palette.TextPrimary
            };

            var cbo = new ComboBox
            {
                Location = new Point(22, 64),
                Width = 400,
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 10F),
                BackColor = palette.InputBg,
                ForeColor = palette.TextPrimary
            };
            foreach (var cat in categories)
            {
                cbo.Items.Add(cat);
            }
            cbo.Text = currentCategory;

            var btnOk = new Button
            {
                Text = "Apply",
                DialogResult = DialogResult.OK,
                Location = new Point(236, 125),
                Size = new Size(90, 32),
                BackColor = AppConstants.ColorPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(332, 125),
                Size = new Size(90, 32),
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;

            prompt.Controls.AddRange([lblDesc, cbo, btnOk, btnCancel]);
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCancel;

            return prompt.ShowDialog() == DialogResult.OK ? cbo.Text.Trim() : null;
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
