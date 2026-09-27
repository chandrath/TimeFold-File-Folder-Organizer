using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Controls;
using FileOrganizer.Models;

namespace FileOrganizer.Forms
{
    public class FolderExclusionDialog : Form
    {
        private readonly bool _darkMode;
        private readonly AppSettings _settings;
        private readonly string? _sourceFolder;
        private CheckBox _chkEnable = null!;
        private TextBox _txtNewFolder = null!;
        private ModernButton _btnAdd = null!;
        private ListBox _lstFolders = null!;
        private ModernButton _btnRemove = null!;
        private ModernButton _btnClear = null!;
        private ModernButton _btnSave = null!;
        private ModernButton _btnCancel = null!;

        public bool RulesChanged { get; private set; }

        public FolderExclusionDialog(AppSettings settings, bool darkMode, string? sourceFolder = null)
        {
            _settings = settings;
            _darkMode = darkMode;
            _sourceFolder = sourceFolder;
            InitializeComponent();
            ApplyTheme();
            LoadCurrentRules();
        }

        private void InitializeComponent()
        {
            Text = "Folder Exclusion Rules";
            Size = new Size(520, 480);
            MinimumSize = new Size(460, 400);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            ShowIcon = false;
            MaximizeBox = false;
            MinimizeBox = false;

            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 16) };

            var lblHeader = new Label
            {
                Text = "🛡 Excluded Folders",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 14)
            };

            var lblDesc = new Label
            {
                Text = "Folders matching these names or containing a .timefold-ignore file will be preserved intact and skipped during organization.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.Gray,
                Size = new Size(460, 36),
                Location = new Point(20, 38)
            };

            _chkEnable = new CheckBox
            {
                Text = "Enable Folder Exclusions",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 80),
                Checked = _settings.EnableFolderExclusions
            };

            var lblAdd = new Label
            {
                Text = "Folder Name to Exclude:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 112)
            };

            _txtNewFolder = new TextBox
            {
                Location = new Point(20, 134),
                Size = new Size(360, 26),
                Font = new Font("Segoe UI", 9F)
            };
            _txtNewFolder.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddFolder(); } };

            _btnAdd = new ModernButton
            {
                Text = "➕ Add",
                Location = new Point(390, 132),
                Size = new Size(90, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                BorderRadius = 6,
                Cursor = Cursors.Hand
            };
            _btnAdd.Click += (s, e) => AddFolder();

            _lstFolders = new ListBox
            {
                Location = new Point(20, 172),
                Size = new Size(460, 170),
                Font = new Font("Segoe UI", 9.5F),
                IntegralHeight = false,
                SelectionMode = SelectionMode.MultiExtended
            };

            _btnRemove = new ModernButton
            {
                Text = "🗑 Remove Selected",
                Location = new Point(20, 350),
                Size = new Size(140, 28),
                Font = new Font("Segoe UI", 8.5F),
                BorderRadius = 6,
                Cursor = Cursors.Hand
            };
            _btnRemove.Click += (s, e) => RemoveSelected();

            _btnClear = new ModernButton
            {
                Text = "Clear All",
                Location = new Point(170, 350),
                Size = new Size(90, 28),
                Font = new Font("Segoe UI", 8.5F),
                BorderRadius = 6,
                Cursor = Cursors.Hand
            };
            _btnClear.Click += (s, e) => { if (_lstFolders.Items.Count > 0 && MessageBox.Show(this, "Clear all excluded folder rules?", "Clear Exclusions", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) _lstFolders.Items.Clear(); };

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 8, 20, 8) };
            _btnSave = new ModernButton
            {
                Text = "Save & Apply",
                Size = new Size(110, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                BorderRadius = 6,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            _btnSave.Click += (s, e) => SaveAndClose();

            _btnCancel = new ModernButton
            {
                Text = "Cancel",
                Size = new Size(90, 32),
                Font = new Font("Segoe UI", 9F),
                BorderRadius = 6,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right,
                Margin = new Padding(0, 0, 10, 0)
            };
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.AddRange(new Control[] { _btnSave, _btnCancel });
            pnlMain.Controls.AddRange(new Control[] { lblHeader, lblDesc, _chkEnable, lblAdd, _txtNewFolder, _btnAdd, _lstFolders, _btnRemove, _btnClear });
            Controls.AddRange(new Control[] { pnlMain, pnlFooter });

            Shown += (s, e) => AppTheme.SetWindowDarkTitleBar(Handle, _darkMode);
        }

        private void LoadCurrentRules()
        {
            _lstFolders.Items.Clear();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(_sourceFolder) && System.IO.Directory.Exists(_sourceFolder))
            {
                try
                {
                    foreach (var dir in System.IO.Directory.GetDirectories(_sourceFolder))
                    {
                        if (System.IO.File.Exists(System.IO.Path.Combine(dir, AppConstants.TimefoldIgnoreFileName)))
                            names.Add(System.IO.Path.GetFileName(dir));
                    }
                }
                catch { }
            }
            foreach (var n in names.OrderBy(x => x)) _lstFolders.Items.Add(n);
        }

        private void AddFolder()
        {
            string name = _txtNewFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            foreach (var item in _lstFolders.Items)
            {
                if (string.Equals(item.ToString(), name, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, $"'{name}' is already in the exclusion list.", "Duplicate Name", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _txtNewFolder.SelectAll();
                    return;
                }
            }

            _lstFolders.Items.Add(name);
            _txtNewFolder.Clear();
            _txtNewFolder.Focus();
        }

        private void RemoveSelected()
        {
            var selected = _lstFolders.SelectedItems.Cast<object>().ToList();
            foreach (var item in selected) _lstFolders.Items.Remove(item);
        }

        private void SaveAndClose()
        {
            var list = _lstFolders.Items.Cast<object>().Select(o => o.ToString()?.Trim().TrimEnd('/', '\\') ?? "").Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            _settings.ExcludedFolderNames = new();
            _settings.EnableFolderExclusions = _chkEnable.Checked;
            _settings.SaveToFile();

            if (!string.IsNullOrWhiteSpace(_sourceFolder) && System.IO.Directory.Exists(_sourceFolder))
            {
                try
                {
                    string rootIgnore = System.IO.Path.Combine(_sourceFolder, AppConstants.TimefoldIgnoreFileName);
                    if (System.IO.File.Exists(rootIgnore)) { try { System.IO.File.SetAttributes(rootIgnore, System.IO.FileAttributes.Normal); } catch { } System.IO.File.Delete(rootIgnore); }

                    var set = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
                    foreach (var dir in System.IO.Directory.GetDirectories(_sourceFolder))
                    {
                        string dirName = System.IO.Path.GetFileName(dir);
                        string marker = System.IO.Path.Combine(dir, AppConstants.TimefoldIgnoreFileName);
                        if (set.Contains(dirName))
                        {
                            if (!System.IO.File.Exists(marker))
                            {
                                System.IO.File.WriteAllText(marker, AppConstants.TimefoldIgnoreFileContent);
                                try { System.IO.File.SetAttributes(marker, System.IO.FileAttributes.Hidden); } catch { }
                            }
                        }
                        else
                        {
                            if (System.IO.File.Exists(marker))
                            {
                                try { System.IO.File.SetAttributes(marker, System.IO.FileAttributes.Normal); } catch { }
                                System.IO.File.Delete(marker);
                            }
                        }
                    }
                }
                catch { }
            }

            RulesChanged = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyTheme()
        {
            var palette = AppTheme.GetPalette(_darkMode);
            BackColor = palette.CanvasBg;
            ForeColor = palette.TextPrimary;
            _lstFolders.BackColor = palette.CardBg;
            _lstFolders.ForeColor = palette.TextPrimary;
            _txtNewFolder.BackColor = palette.CardBg;
            _txtNewFolder.ForeColor = palette.TextPrimary;
            _btnRemove.BackColor = palette.SecondaryButtonBg;
            _btnRemove.ForeColor = palette.SecondaryButtonText;
            _btnClear.BackColor = palette.SecondaryButtonBg;
            _btnClear.ForeColor = palette.SecondaryButtonText;
            _btnCancel.BackColor = palette.SecondaryButtonBg;
            _btnCancel.ForeColor = palette.SecondaryButtonText;
        }
    }
}
