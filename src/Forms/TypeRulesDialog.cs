using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Controls;
using FileOrganizer.Models;
using FileOrganizer.Services;

namespace FileOrganizer.Forms
{
    public class TypeRulesDialog : Form
    {
        private readonly bool _darkMode;
        private readonly FileTypeService _service = FileTypeService.Instance;
        private ListView _lstRules = null!;
        private TextBox _txtNewExt = null!;
        private ComboBox _cboTargetCategory = null!;
        private ModernButton _btnReset = null!;
        private ModernButton _btnImport = null!;
        private ModernButton _btnExport = null!;
        private ModernButton _btnClose = null!;

        private TextBox _txtSearch = null!;
        private TextBox _txtPrefix = null!;
        private TextBox _txtSuffix = null!;
        private AppSettings _settings = null!;

        public TypeRulesDialog(bool darkMode)
        {
            _darkMode = darkMode;
            _settings = AppSettings.LoadFromFile();
            InitializeComponent();
            ApplyTheme();
            LoadRulesList();
        }

        private void InitializeComponent()
        {
            Text = "File Type & Category Rules";
            Size = new Size(720, 560);
            MinimumSize = new Size(620, 460);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            ShowIcon = false;
            MaximizeBox = false;
            MinimizeBox = false;

            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

            // Top Header & Search Bar
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 68, Padding = new Padding(0, 0, 0, 8) };
            var lblHeader = new Label
            {
                Text = "Customize how file extensions map to organizational categories.",
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9.5F)
            };

            var pnlSearch = new Panel { Dock = DockStyle.Bottom, Height = 34 };
            var lblSearch = new Label { Text = "🔍 Search Extension / Category:", AutoSize = true, Location = new Point(0, 7) };
            _txtSearch = new TextBox { Location = new Point(200, 4), Width = 220, PlaceholderText = "e.g. .psd, .c3p, 3D, Code" };
            _txtSearch.TextChanged += (s, e) => LoadRulesList();

            var lblPre = new Label { Text = "Prefix:", AutoSize = true, Location = new Point(440, 7) };
            _txtPrefix = new TextBox { Text = _settings.CategoryPrefix, Location = new Point(485, 4), Width = 70, MaxLength = AppConstants.MaxPrefixSuffixLength, PlaceholderText = "e.g. Cat_" };
            _txtPrefix.TextChanged += OnPrefixSuffixChanged;

            var lblSuf = new Label { Text = "Suffix:", AutoSize = true, Location = new Point(568, 7) };
            _txtSuffix = new TextBox { Text = _settings.CategorySuffix, Location = new Point(612, 4), Width = 70, MaxLength = AppConstants.MaxPrefixSuffixLength, PlaceholderText = "e.g. _Files" };
            _txtSuffix.TextChanged += OnPrefixSuffixChanged;

            pnlSearch.Controls.AddRange(new Control[] { lblSearch, _txtSearch, lblPre, _txtPrefix, lblSuf, _txtSuffix });
            pnlTop.Controls.AddRange(new Control[] { lblHeader, pnlSearch });

            _lstRules = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                CheckBoxes = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            _lstRules.Columns.Add("Extension", 150);
            _lstRules.Columns.Add("Target Category", 240);
            _lstRules.Columns.Add("Status", 160);
            _lstRules.ItemChecked += LstRules_ItemChecked;

            var ctxRules = new ContextMenuStrip { ShowImageMargin = false };
            if (_darkMode)
            {
                ctxRules.Renderer = AppTheme.DarkMenuRenderer;
                ctxRules.BackColor = AppTheme.GetPalette(_darkMode).MenuBg;
            }

            var mnuRenameCat = new ToolStripMenuItem("🏷️ Rename Category...", null, (s, e) => RenameSelectedCategory());
            var mnuResetCat = new ToolStripMenuItem("↺ Revert Category to Factory Name", null, (s, e) => ResetSelectedCategoryName());
            var mnuRemap = new ToolStripMenuItem("🔀 Remap Extension to Another Category...", null, (s, e) => RemapSelectedExtension());
            var mnuResetExt = new ToolStripMenuItem("↺ Reset Extension Override", null, (s, e) => ResetSelectedExtensionOverride());

            ctxRules.Items.AddRange(new ToolStripItem[]
            {
                mnuRenameCat,
                mnuResetCat,
                new ToolStripSeparator(),
                mnuRemap,
                mnuResetExt
            });

            ctxRules.Opening += (s, e) =>
            {
                if (_lstRules.SelectedItems.Count == 0)
                {
                    e.Cancel = true;
                    return;
                }

                var item = _lstRules.SelectedItems[0];
                string ext = item.Tag as string ?? "";
                string cat = item.SubItems[1].Text;
                bool isCatRenamed = _service.IsCategoryRenamed(cat, out string origCat);
                bool hasOverride = _service.Delta.ExtensionCategoryOverrides.ContainsKey(ext);

                mnuRenameCat.Text = $"🏷️ Rename Category '{cat}'...";
                mnuResetCat.Visible = isCatRenamed;
                if (isCatRenamed) mnuResetCat.Text = $"↺ Revert Category to Factory Name ('{origCat}')";

                mnuRemap.Text = $"🔀 Remap {ext} to Another Category...";
                mnuResetExt.Visible = hasOverride;
            };

            _lstRules.ContextMenuStrip = ctxRules;

            // Bottom Add / Override Bar
            var pnlAdd = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 8, 0, 8) };
            var lblAdd = new Label { Text = "Extension:", AutoSize = true, Location = new Point(0, 14) };
            _txtNewExt = new TextBox { Location = new Point(68, 11), Width = 90, PlaceholderText = ".ext" };
            var lblCat = new Label { Text = "Category:", AutoSize = true, Location = new Point(170, 14) };
            _cboTargetCategory = new ComboBox { Location = new Point(236, 11), Width = 170, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAssign = new ModernButton { Text = "Add / Remap", Location = new Point(420, 10), Size = new Size(110, 28), BorderRadius = 6 };
            btnAssign.Click += BtnAssign_Click;

            pnlAdd.Controls.AddRange(new Control[] { lblAdd, _txtNewExt, lblCat, _cboTargetCategory, btnAssign });

            // Bottom Button Bar
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 8, 0, 0) };
            _btnReset = new ModernButton { Text = "↺ Reset to Defaults", Size = new Size(140, 32), Dock = DockStyle.Left, BorderRadius = 6 };
            _btnReset.Click += BtnReset_Click;

            var btnSmartPairing = new ModernButton { Text = "🔗 Smart Pairing...", Size = new Size(130, 32), Dock = DockStyle.Left, BorderRadius = 6, Margin = new Padding(6, 0, 0, 0) };
            btnSmartPairing.Click += (s, e) =>
            {
                using var dlg = new SmartPairingDialog(_settings, _darkMode);
                dlg.ShowDialog(this);
            };

            _btnImport = new ModernButton { Text = "📥 Import...", Size = new Size(95, 32), Dock = DockStyle.Right, BorderRadius = 6, Margin = new Padding(0, 0, 6, 0) };
            _btnImport.Click += BtnImport_Click;

            _btnExport = new ModernButton { Text = "📤 Export...", Size = new Size(95, 32), Dock = DockStyle.Right, BorderRadius = 6, Margin = new Padding(0, 0, 6, 0) };
            _btnExport.Click += BtnExport_Click;

            _btnClose = new ModernButton { Text = "Close", Size = new Size(90, 32), Dock = DockStyle.Right, BorderRadius = 6 };
            _btnClose.Click += (s, e) => Close();

            pnlFooter.Controls.AddRange(new Control[] { _btnReset, btnSmartPairing, _btnExport, _btnImport, _btnClose });

            pnlMain.Controls.Add(_lstRules);
            pnlMain.Controls.Add(pnlAdd);
            pnlMain.Controls.Add(pnlFooter);
            pnlMain.Controls.Add(pnlTop);
            Controls.Add(pnlMain);
        }

        private void OnPrefixSuffixChanged(object? sender, EventArgs e)
        {
            _settings.CategoryPrefix = AppConstants.SanitizeFolderName(_txtPrefix.Text);
            _settings.CategorySuffix = AppConstants.SanitizeFolderName(_txtSuffix.Text);
            _settings.SaveToFile();
        }

        private void ApplyTheme()
        {
            var palette = AppTheme.GetPalette(_darkMode);
            BackColor = palette.CanvasBg;
            ForeColor = palette.TextPrimary;
            AppTheme.SetWindowDarkTitleBar(this.Handle, _darkMode);

            _lstRules.BackColor = palette.CardBg;
            _lstRules.ForeColor = palette.TextPrimary;
            _txtNewExt.BackColor = palette.InputBg;
            _txtNewExt.ForeColor = palette.TextPrimary;
            _cboTargetCategory.BackColor = palette.InputBg;
            _cboTargetCategory.ForeColor = palette.TextPrimary;

            if (_txtSearch != null)
            {
                _txtSearch.BackColor = palette.InputBg;
                _txtSearch.ForeColor = palette.TextPrimary;
            }
            if (_txtPrefix != null)
            {
                _txtPrefix.BackColor = palette.InputBg;
                _txtPrefix.ForeColor = palette.TextPrimary;
            }
            if (_txtSuffix != null)
            {
                _txtSuffix.BackColor = palette.InputBg;
                _txtSuffix.ForeColor = palette.TextPrimary;
            }

            _btnReset.BackColor = palette.SecondaryButtonBg;
            _btnReset.ForeColor = palette.SecondaryButtonText;
            _btnImport.BackColor = palette.SecondaryButtonBg;
            _btnImport.ForeColor = palette.SecondaryButtonText;
            _btnExport.BackColor = palette.SecondaryButtonBg;
            _btnExport.ForeColor = palette.SecondaryButtonText;
            _btnClose.BackColor = AppConstants.ColorPrimary;
            _btnClose.ForeColor = Color.White;
        }

        private bool _isPopulating = false;

        private void LoadRulesList()
        {
            _isPopulating = true;
            _lstRules.Items.Clear();
            _cboTargetCategory.Items.Clear();

            var categories = new HashSet<string>(FileTypeService.FactoryCategories.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (var customCat in _service.Delta.CustomCategories.Keys) categories.Add(customCat);
            foreach (var cat in categories.OrderBy(c => c)) _cboTargetCategory.Items.Add(cat);
            if (_cboTargetCategory.Items.Count > 0) _cboTargetCategory.SelectedIndex = 0;

            // Collect all known extensions
            var allExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var extList in FileTypeService.FactoryCategories.Values)
                foreach (var ext in extList) allExtensions.Add(ext);
            foreach (var ext in _service.Delta.ExtensionCategoryOverrides.Keys) allExtensions.Add(ext);
            foreach (var extList in _service.Delta.CustomCategories.Values)
                foreach (var ext in extList) allExtensions.Add(ext);

            string query = _txtSearch?.Text.Trim().ToLowerInvariant() ?? string.Empty;

            foreach (var ext in allExtensions.OrderBy(e => e))
            {
                bool isFactory = FileTypeService.FactoryCategories.Values.Any(list => list.Contains(ext, StringComparer.OrdinalIgnoreCase));
                bool isDisabled = _service.Delta.DisabledFactoryExtensions.Contains(ext);
                string currentCat = _service.GetCategory(ext);

                // Filter by search query if present
                if (!string.IsNullOrEmpty(query))
                {
                    bool matchExt = ext.ToLowerInvariant().Contains(query);
                    bool matchCat = currentCat.ToLowerInvariant().Contains(query);
                    if (!matchExt && !matchCat) continue;
                }

                string status = isFactory ? (isDisabled ? "Disabled" : "Factory Default") : "Custom User Rule";
                if (_service.Delta.ExtensionCategoryOverrides.ContainsKey(ext)) status = "User Override";

                var lvi = new ListViewItem(ext) { Checked = !isDisabled };
                lvi.SubItems.Add(currentCat);
                lvi.SubItems.Add(status);
                lvi.Tag = ext;

                if (isDisabled)
                {
                    lvi.ForeColor = Color.Gray;
                }

                _lstRules.Items.Add(lvi);
            }

            _isPopulating = false;
        }

        private void LstRules_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (_isPopulating || e.Item == null || e.Item.Tag is not string ext) return;

            if (e.Item.Checked)
            {
                _service.Delta.DisabledFactoryExtensions.Remove(ext);
                e.Item.ForeColor = _darkMode ? Color.White : AppConstants.ColorTextDark;
                e.Item.SubItems[2].Text = "Factory Default";
            }
            else
            {
                _service.Delta.DisabledFactoryExtensions.Add(ext);
                e.Item.ForeColor = Color.Gray;
                e.Item.SubItems[2].Text = "Disabled";
            }

            _service.SaveDelta();
            _service.RebuildLookupTable();
        }

        private void BtnAssign_Click(object? sender, EventArgs e)
        {
            string ext = _txtNewExt.Text.Trim();
            string cat = _cboTargetCategory.Text.Trim();

            if (string.IsNullOrWhiteSpace(ext) || string.IsNullOrWhiteSpace(cat))
            {
                MessageBox.Show("Please enter an extension and category.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ext.StartsWith('.')) ext = "." + ext;
            _service.Delta.ExtensionCategoryOverrides[ext] = cat;
            _service.Delta.DisabledFactoryExtensions.Remove(ext);
            _service.SaveDelta();
            _service.RebuildLookupTable();

            _txtNewExt.Clear();
            LoadRulesList();
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            var res = MessageBox.Show(
                "Reset all file type and category rules back to built-in factory defaults?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                _service.ResetToFactoryDefaults();
                _settings.OrgMode = OrganizationMode.Date;
                _settings.CategoryPrefix = AppConstants.DefaultCategoryPrefix;
                _settings.CategorySuffix = AppConstants.DefaultCategorySuffix;
                _settings.SaveToFile();
                if (_txtPrefix != null) _txtPrefix.Text = "";
                if (_txtSuffix != null) _txtSuffix.Text = "";
                LoadRulesList();
            }
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Title = "Export File Type Rules",
                Filter = "JSON Files (*.json)|*.json",
                FileName = "timefold_custom_types.json"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _service.ExportRules(sfd.FileName);
                    MessageBox.Show("Rules exported successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnImport_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Import File Type Rules",
                Filter = "JSON Files (*.json)|*.json"
            };

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _service.ImportRules(ofd.FileName);
                    LoadRulesList();
                    MessageBox.Show("Rules imported successfully!", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RenameSelectedCategory()
        {
            if (_lstRules.SelectedItems.Count == 0) return;
            string currentCat = _lstRules.SelectedItems[0].SubItems[1].Text;

            var palette = AppTheme.GetPalette(_darkMode);
            using var prompt = new Form
            {
                Text = "Rename Category",
                Size = new Size(420, 175),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = palette.CanvasBg,
                ForeColor = palette.TextPrimary,
                Font = new Font("Segoe UI", 9F)
            };
            AppTheme.SetWindowDarkTitleBar(prompt.Handle, _darkMode);

            var lbl = new Label { Text = $"Enter new name for category '{currentCat}':", Location = new Point(18, 14), AutoSize = true, ForeColor = palette.TextPrimary };
            var txt = new TextBox { Text = currentCat, Location = new Point(20, 38), Width = 365, Font = new Font("Segoe UI", 9.5F), BackColor = palette.InputBg, ForeColor = palette.TextPrimary };
            var btnOk = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(210, 85), Size = new Size(85, 32), BackColor = AppConstants.ColorPrimary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnOk.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(300, 85), Size = new Size(85, 32), BackColor = palette.SecondaryButtonBg, ForeColor = palette.SecondaryButtonText, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;

            prompt.Controls.AddRange([lbl, txt, btnOk, btnCancel]);
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCancel;

            if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text))
            {
                _service.RenameCategory(currentCat, txt.Text.Trim());
                LoadRulesList();
            }
        }

        private void ResetSelectedCategoryName()
        {
            if (_lstRules.SelectedItems.Count == 0) return;
            string currentCat = _lstRules.SelectedItems[0].SubItems[1].Text;
            if (_service.ResetCategoryName(currentCat))
            {
                LoadRulesList();
            }
        }

        private void RemapSelectedExtension()
        {
            if (_lstRules.SelectedItems.Count == 0) return;
            string ext = _lstRules.SelectedItems[0].Tag as string ?? "";
            string currentCat = _lstRules.SelectedItems[0].SubItems[1].Text;

            _txtNewExt.Text = ext;
            _cboTargetCategory.Text = currentCat;
            _cboTargetCategory.Focus();
        }

        private void ResetSelectedExtensionOverride()
        {
            if (_lstRules.SelectedItems.Count == 0) return;
            string ext = _lstRules.SelectedItems[0].Tag as string ?? "";
            _service.RemoveCategoryOverride(ext);
            LoadRulesList();
        }
    }
}
