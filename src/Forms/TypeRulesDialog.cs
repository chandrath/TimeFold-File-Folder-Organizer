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

        public TypeRulesDialog(bool darkMode)
        {
            _darkMode = darkMode;
            InitializeComponent();
            ApplyTheme();
            LoadRulesList();
        }

        private void InitializeComponent()
        {
            Text = "File Type & Category Rules";
            Size = new Size(680, 520);
            MinimumSize = new Size(580, 420);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            ShowIcon = false;
            MaximizeBox = false;
            MinimizeBox = false;

            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

            var lblHeader = new Label
            {
                Text = "Customize how file extensions map to organizational categories.",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9.5F)
            };

            _lstRules = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                CheckBoxes = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            _lstRules.Columns.Add("Extension", 140);
            _lstRules.Columns.Add("Target Category", 220);
            _lstRules.Columns.Add("Status", 160);
            _lstRules.ItemChecked += LstRules_ItemChecked;

            // Bottom Add / Override Bar
            var pnlAdd = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 8, 0, 8) };
            var lblAdd = new Label { Text = "Extension:", AutoSize = true, Location = new Point(0, 14) };
            _txtNewExt = new TextBox { Location = new Point(68, 11), Width = 90, PlaceholderText = ".ext" };
            var lblCat = new Label { Text = "Category:", AutoSize = true, Location = new Point(170, 14) };
            _cboTargetCategory = new ComboBox { Location = new Point(236, 11), Width = 160, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAssign = new ModernButton { Text = "Add / Remap", Location = new Point(410, 10), Size = new Size(110, 28), BorderRadius = 6 };
            btnAssign.Click += BtnAssign_Click;

            pnlAdd.Controls.AddRange(new Control[] { lblAdd, _txtNewExt, lblCat, _cboTargetCategory, btnAssign });

            // Bottom Button Bar
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 8, 0, 0) };
            _btnReset = new ModernButton { Text = "↺ Reset to Defaults", Size = new Size(140, 32), Dock = DockStyle.Left, BorderRadius = 6 };
            _btnReset.Click += BtnReset_Click;

            _btnImport = new ModernButton { Text = "📥 Import...", Size = new Size(95, 32), Dock = DockStyle.Right, BorderRadius = 6, Margin = new Padding(0, 0, 6, 0) };
            _btnImport.Click += BtnImport_Click;

            _btnExport = new ModernButton { Text = "📤 Export...", Size = new Size(95, 32), Dock = DockStyle.Right, BorderRadius = 6, Margin = new Padding(0, 0, 6, 0) };
            _btnExport.Click += BtnExport_Click;

            _btnClose = new ModernButton { Text = "Close", Size = new Size(90, 32), Dock = DockStyle.Right, BorderRadius = 6 };
            _btnClose.Click += (s, e) => Close();

            pnlFooter.Controls.AddRange(new Control[] { _btnReset, _btnExport, _btnImport, _btnClose });

            pnlMain.Controls.Add(_lstRules);
            pnlMain.Controls.Add(pnlAdd);
            pnlMain.Controls.Add(pnlFooter);
            pnlMain.Controls.Add(lblHeader);
            Controls.Add(pnlMain);
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

            foreach (var ext in allExtensions.OrderBy(e => e))
            {
                bool isFactory = FileTypeService.FactoryCategories.Values.Any(list => list.Contains(ext, StringComparer.OrdinalIgnoreCase));
                bool isDisabled = _service.Delta.DisabledFactoryExtensions.Contains(ext);
                string currentCat = _service.GetCategory(ext);

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
    }
}
