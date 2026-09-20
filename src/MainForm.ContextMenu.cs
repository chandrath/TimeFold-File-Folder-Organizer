using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Models;
using Microsoft.VisualBasic.FileIO;

namespace FileOrganizer
{
    public partial class MainForm
    {
        private ToolStripMenuItem _menuItemOpen = null!;
        private ToolStripMenuItem _menuItemExplorer = null!;
        private ToolStripMenuItem _menuItemCopy = null!;
        private ToolStripMenuItem _menuItemRename = null!;
        private ToolStripMenuItem _menuItemDelete = null!;

        private void InitializeContextMenu()
        {
            _ctxFileMenu = new ContextMenuStrip { ShowImageMargin = false };

            _menuItemOpen = new ToolStripMenuItem("📄 Open", null, (s, e) => OpenSelectedFile());
            _menuItemExplorer = new ToolStripMenuItem("📂 Open in File Explorer", null, (s, e) => OpenSelectedInExplorer());
            _menuItemCopy = new ToolStripMenuItem("📋 Copy File Path", null, (s, e) => CopySelectedPath());
            _menuItemRename = new ToolStripMenuItem("✏ Rename...", null, (s, e) => RenameSelectedFile());
            _menuItemDelete = new ToolStripMenuItem("🗑 Delete (Recycle Bin)", null, (s, e) => DeleteSelectedFile());

            _ctxFileMenu.Items.AddRange(new ToolStripItem[]
            {
                _menuItemOpen,
                _menuItemExplorer,
                _menuItemCopy,
                new ToolStripSeparator(),
                _menuItemRename,
                _menuItemDelete
            });

            _ctxFileMenu.Opening += (s, e) =>
            {
                bool hasSelection = _lstFiles.SelectedItems.Count > 0;
                _menuItemOpen.Enabled = hasSelection;
                _menuItemExplorer.Enabled = hasSelection;
                _menuItemCopy.Enabled = hasSelection;
                _menuItemRename.Enabled = hasSelection;
                _menuItemDelete.Enabled = hasSelection;
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
    }
}
