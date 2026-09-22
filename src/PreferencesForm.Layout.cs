using System;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer
{
    public partial class PreferencesForm
    {
        private CheckBox _chkIncludeFolders = null!;
        private CheckBox _chkIgnoreSystemFiles = null!;
        private CheckBox _chkShowProgress = null!;
        private CheckBox _chkShowOnTop = null!;
        private CheckBox _chkGenerateCsvLog = null!;
        private CheckBox _chkUse24Hour = null!;
        private CheckBox _chkAutoLoadExeDir = null!;
        private Button _btnTypeRules = null!;
        private ComboBox _cmbFileDateSource = null!;
        private ComboBox _cmbFolderDateSource = null!;

        // Appearance Controls
        private Button _btnThemeLight = null!;
        private Button _btnThemeDark = null!;

        // Folder Naming Controls
        private ComboBox _cmbFormat = null!;
        private Button _btnFlipOrder = null!;
        private CheckBox _chkShortMonth = null!;
        private TextBox _txtPrefix = null!;
        private TextBox _txtSuffix = null!;
        private Panel _pnlLivePreview = null!;
        private Label _lblPreviewTitle = null!;
        private Label _lblSample1 = null!;
        private Label _lblSample2 = null!;
        private Label _lblSample3 = null!;
        private Label _lblSample4 = null!;

        // Preview Limit & Performance
        private ComboBox _cmbPreviewLimit = null!;
        private Label _lblPreviewLimitWarning = null!;

        // Action Buttons
        private Button _btnOpenConfig = null!;
        private Button _btnDefaults = null!;
        private Button _btnOK = null!;
        private Button _btnCancel = null!;

        private void InitializeComponent()
        {
            this.Text = "Preferences";
            if (AppConstants.AppIcon != null) this.Icon = AppConstants.AppIcon;
            this.Size = new Size(600, 870);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(16);

            int currentY = 16;
            const int spacing = 26;
            const int leftMargin = 20;
            int contentWidth = this.ClientSize.Width - (leftMargin * 2);
            int halfWidth = (contentWidth / 2) - 8;

            // 1. File Organization Options
            var lblOrgHeader = new Label { Text = "File & Folder Rules:", UseMnemonic = false, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(leftMargin, currentY), AutoSize = true };
            currentY += 24;

            var lblFileDate = new Label { Text = "File Date Source:", Location = new Point(leftMargin, currentY), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            var lblFolderDate = new Label { Text = "Folder Date Source:", Location = new Point(leftMargin + halfWidth + 16, currentY), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            currentY += 18;

            _cmbFileDateSource = new ComboBox { Location = new Point(leftMargin, currentY), Width = halfWidth, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            _cmbFileDateSource.Items.AddRange(["Date Modified (Default)", "Date Created", "Earliest Date (Oldest)"]);
            _cmbFileDateSource.SelectedIndex = (int)_settings.FileDateSource;

            _cmbFolderDateSource = new ComboBox { Location = new Point(leftMargin + halfWidth + 16, currentY), Width = halfWidth, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            _cmbFolderDateSource.Items.AddRange(["Date Modified (Default)", "Date Created", "Earliest Date (Oldest)"]);
            _cmbFolderDateSource.SelectedIndex = (int)_settings.FolderDateSource;
            currentY += 30;

            _chkIncludeFolders = new CheckBox { Text = "Include Folders (Move whole folders alongside files)", Location = new Point(leftMargin, currentY), Size = new Size(contentWidth, 24), Checked = _settings.IncludeTopLevelFolders };
            currentY += spacing;

            _chkIgnoreSystemFiles = new CheckBox { Text = "Ignore Windows system files & protected folders (desktop.ini, Thumbs.db, $RECYCLE.BIN)", UseMnemonic = false, Location = new Point(leftMargin, currentY), Size = new Size(contentWidth, 24), Checked = _settings.IgnoreSystemFiles };
            currentY += 32;

            // Detect initial modifier states
            _isFlipped = _settings.FolderFormat switch
            {
                FolderFormat.MonthYear or FolderFormat.ShortMonthYear or FolderFormat.DayMonthYear or FolderFormat.DayShortMonthYear
                or FolderFormat.QuarterYear or FolderFormat.HalfYear or FolderFormat.MonthIso
                or FolderFormat.YearWithMonthFlipped or FolderFormat.YearWithShortMonthFlipped
                or FolderFormat.YearWithIsoMonthFlipped
                or FolderFormat.YearWithQuarterFlipped or FolderFormat.YearWithHalfFlipped => true,
                _ => false
            };

            _useShortMonth = _settings.FolderFormat switch
            {
                FolderFormat.YearShortMonth or FolderFormat.ShortMonthYear or FolderFormat.YearShortMonthDay
                or FolderFormat.DayShortMonthYear or FolderFormat.YearQuarterShortMonths
                or FolderFormat.YearWithShortMonth or FolderFormat.YearWithShortMonthFlipped => true,
                _ => false
            };

            // 2. Folder Naming Template
            var lblNamingHeader = new Label { Text = "Date Naming Template:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(leftMargin, currentY), AutoSize = true };
            currentY += 24;

            _cmbFormat = new ComboBox { Location = new Point(leftMargin, currentY), Width = contentWidth, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F), DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 22 };

            int currentYear = DateTime.Now.Year;
            var formatOptions = new FormatItem[]
            {
                new("── Year ─────────────────────", CoreFormat.Header, true),
                new(GetFormatDisplayName(CoreFormat.YearOnly, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearOnly),
                new(GetFormatDisplayName(CoreFormat.YearNestedMonthOnly, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearNestedMonthOnly),
                new(GetFormatDisplayName(CoreFormat.YearNestedMonth, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearNestedMonth),
                new(GetFormatDisplayName(CoreFormat.YearNestedIso, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearNestedIso),
                new(GetFormatDisplayName(CoreFormat.YearNestedQuarter, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearNestedQuarter),
                new(GetFormatDisplayName(CoreFormat.YearNestedHalf, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearNestedHalf),
                new("── Monthly ──────────────────", CoreFormat.Header, true),
                new(GetFormatDisplayName(CoreFormat.YearMonth, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearMonth),
                new(GetFormatDisplayName(CoreFormat.IsoMonth, _isFlipped, _useShortMonth, currentYear), CoreFormat.IsoMonth),
                new("── Daily ────────────────────", CoreFormat.Header, true),
                new(GetFormatDisplayName(CoreFormat.Daily, _isFlipped, _useShortMonth, currentYear), CoreFormat.Daily),
                new("── Quarters & Half-Years ────", CoreFormat.Header, true),
                new(GetFormatDisplayName(CoreFormat.YearQuarter, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearQuarter),
                new(GetFormatDisplayName(CoreFormat.YearQuarterMonths, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearQuarterMonths),
                new(GetFormatDisplayName(CoreFormat.YearHalf, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearHalf),
            };

            _cmbFormat.Items.AddRange(formatOptions);
            _cmbFormat.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= _cmbFormat.Items.Count || _cmbFormat.Items[e.Index] is not FormatItem item) return;
                var baseFont = e.Font ?? _cmbFormat.Font ?? this.Font;
                bool isDark = _isDarkMode;
                var palette = AppTheme.GetPalette(isDark);

                if (item.IsHeader)
                {
                    using var bgBrush = new SolidBrush(isDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(243, 244, 246));
                    using var textBrush = new SolidBrush(isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(107, 114, 128));
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                    using var headerFont = new Font(baseFont, FontStyle.Bold);
                    e.Graphics.DrawString(item.DisplayName, headerFont, textBrush, e.Bounds.X + 4, e.Bounds.Y + 3);
                }
                else
                {
                    bool isSelected = (e.State & DrawItemState.Selected) != 0;
                    Color bg = isSelected ? (isDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(224, 231, 255)) : (isDark ? palette.InputBg : Color.White);
                    Color fg = isSelected ? (isDark ? Color.White : Color.FromArgb(30, 41, 59)) : palette.TextPrimary;

                    using var bgBrush = new SolidBrush(bg);
                    using var textBrush = new SolidBrush(fg);
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                    e.Graphics.DrawString("   " + item.DisplayName, baseFont, textBrush, e.Bounds.X + 4, e.Bounds.Y + 3);
                }
            };

            // Select active format
            CoreFormat targetCore = _settings.FolderFormat switch
            {
                FolderFormat.YearMonth or FolderFormat.MonthYear or FolderFormat.YearShortMonth or FolderFormat.ShortMonthYear => CoreFormat.YearMonth,
                FolderFormat.IsoMonth or FolderFormat.MonthIso => CoreFormat.IsoMonth,
                FolderFormat.YearMonthDay or FolderFormat.DayMonthYear or FolderFormat.YearShortMonthDay or FolderFormat.DayShortMonthYear => CoreFormat.Daily,
                FolderFormat.YearQuarter or FolderFormat.QuarterYear => CoreFormat.YearQuarter,
                FolderFormat.YearQuarterMonths or FolderFormat.YearQuarterShortMonths => CoreFormat.YearQuarterMonths,
                FolderFormat.YearHalf or FolderFormat.HalfYear => CoreFormat.YearHalf,
                FolderFormat.YearOnly => CoreFormat.YearOnly,
                FolderFormat.YearWithMonth or FolderFormat.YearWithShortMonth
                or FolderFormat.YearWithMonthFlipped or FolderFormat.YearWithShortMonthFlipped => CoreFormat.YearNestedMonth,
                FolderFormat.YearWithMonthOnly or FolderFormat.YearWithShortMonthOnly => CoreFormat.YearNestedMonthOnly,
                FolderFormat.YearWithIsoMonth or FolderFormat.YearWithIsoMonthFlipped => CoreFormat.YearNestedIso,
                FolderFormat.YearWithQuarter or FolderFormat.YearWithQuarterFlipped => CoreFormat.YearNestedQuarter,
                FolderFormat.YearWithHalf or FolderFormat.YearWithHalfFlipped => CoreFormat.YearNestedHalf,
                _ => CoreFormat.YearMonth
            };

            for (int i = 0; i < _cmbFormat.Items.Count; i++)
            {
                if (_cmbFormat.Items[i] is FormatItem item && !item.IsHeader && item.Core == targetCore)
                {
                    _cmbFormat.SelectedIndex = i;
                    break;
                }
            }
            if (_cmbFormat.SelectedIndex < 0) _cmbFormat.SelectedIndex = 1;
            _cmbFormat.SelectedIndexChanged += (s, e) => { UpdateOptionsState(); UpdateLivePreview(); };
            currentY += 30;

            // Modifiers Row
            _btnFlipOrder = new Button { Location = new Point(leftMargin, currentY), Size = new Size(160, 28), Font = new Font("Segoe UI Emoji", 8.5F) };
            _btnFlipOrder.Click += (s, e) => { _isFlipped = !_isFlipped; UpdateOptionsState(); UpdateLivePreview(); };

            _chkShortMonth = new CheckBox { Text = "Short month names (e.g. 'Mar' instead of 'March')", Location = new Point(leftMargin + 172, currentY + 2), AutoSize = true, Checked = _useShortMonth };
            _chkShortMonth.CheckedChanged += (s, e) => { _useShortMonth = _chkShortMonth.Checked; UpdateOptionsState(); UpdateLivePreview(); };
            currentY += 34;

            // Prefix & Suffix Customization
            var lblPrefix = new Label { Text = "Folder Prefix:", Location = new Point(leftMargin, currentY), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            var lblSuffix = new Label { Text = "Folder Suffix:", Location = new Point(leftMargin + halfWidth + 16, currentY), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            currentY += 18;

            _txtPrefix = new TextBox { Location = new Point(leftMargin, currentY), Width = halfWidth, Font = new Font("Segoe UI", 9F), MaxLength = AppConstants.MaxPrefixSuffixLength, PlaceholderText = "e.g. Photos_ or Project_", Text = _settings.FolderPrefix };
            _txtPrefix.TextChanged += (s, e) => UpdateLivePreview();

            _txtSuffix = new TextBox { Location = new Point(leftMargin + halfWidth + 16, currentY), Width = halfWidth, Font = new Font("Segoe UI", 9F), MaxLength = AppConstants.MaxPrefixSuffixLength, PlaceholderText = "e.g. _Archive or _Sorted", Text = _settings.FolderSuffix };
            _txtSuffix.TextChanged += (s, e) => UpdateLivePreview();
            currentY += 32;

            // Live Multi-Folder Preview Card
            _pnlLivePreview = new Panel { Location = new Point(leftMargin, currentY), Width = contentWidth, Height = 120, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(249, 250, 251), Padding = new Padding(12, 8, 12, 8) };
            _lblPreviewTitle = new Label { Text = "👁 Live Multi-Folder Preview:", UseMnemonic = false, Font = new Font("Segoe UI Emoji", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(75, 85, 99), Dock = DockStyle.Top, Height = 18 };
            _lblSample1 = new Label { Text = "", UseMnemonic = false, Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular), ForeColor = AppConstants.ColorTextDark, Dock = DockStyle.Top, Height = 22 };
            _lblSample2 = new Label { Text = "", UseMnemonic = false, Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular), ForeColor = AppConstants.ColorTextDark, Dock = DockStyle.Top, Height = 22 };
            _lblSample3 = new Label { Text = "", UseMnemonic = false, Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular), ForeColor = AppConstants.ColorTextDark, Dock = DockStyle.Top, Height = 22 };
            _lblSample4 = new Label { Text = "", UseMnemonic = false, Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular), ForeColor = AppConstants.ColorTextDark, Dock = DockStyle.Top, Height = 22 };

            _pnlLivePreview.Controls.AddRange([_lblSample4, _lblSample3, _lblSample2, _lblSample1, _lblPreviewTitle]);
            currentY += 128;

            // 3. Appearance & Theme
            var lblAppearanceHeader = new Label { Text = "Appearance & Theme:", UseMnemonic = false, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(leftMargin, currentY), AutoSize = true };
            currentY += 24;

            _btnThemeLight = new Button
            {
                Text = "☀️  Light Mode (Classic)",
                UseMnemonic = false,
                Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular),
                Location = new Point(leftMargin, currentY),
                Size = new Size(halfWidth, 34),
                Cursor = Cursors.Hand
            };
            _btnThemeLight.Click += (s, e) => SetTheme(false);

            _btnThemeDark = new Button
            {
                Text = "🌙  Dark Mode (Fluent Slate)",
                UseMnemonic = false,
                Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular),
                Location = new Point(leftMargin + halfWidth + 16, currentY),
                Size = new Size(halfWidth, 34),
                Cursor = Cursors.Hand
            };
            _btnThemeDark.Click += (s, e) => SetTheme(true);
            currentY += 42;

            // 4. Application & Window Behavior
            var lblBehaviorHeader = new Label { Text = "General Behavior & Performance:", UseMnemonic = false, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(leftMargin, currentY), AutoSize = true };
            currentY += 24;

            _chkShowProgress = new CheckBox { Text = "Show Detailed Progress during organization", Location = new Point(leftMargin, currentY), AutoSize = true, Checked = _settings.ShowDetailedProgress };
            currentY += spacing;

            _chkGenerateCsvLog = new CheckBox { Text = "Generate CSV Audit Log in the output directory", Location = new Point(leftMargin, currentY), AutoSize = true, Checked = _settings.GenerateCsvLog };
            currentY += spacing;

            _chkShowOnTop = new CheckBox { Text = "Keep application window always on top", Location = new Point(leftMargin, currentY), AutoSize = true, Checked = _settings.ShowOnTop };
            currentY += spacing;

            _chkUse24Hour = new CheckBox { Text = "Use 24-hour time for output folder (e.g. 19-13 instead of 07-13PM)", Location = new Point(leftMargin, currentY), AutoSize = true, Checked = _settings.Use24HourTimestamp };
            currentY += spacing;

            _chkAutoLoadExeDir = new CheckBox { Text = "Auto-load application folder on startup", Location = new Point(leftMargin, currentY), AutoSize = true, Checked = _settings.AutoLoadExeDirectoryOnStartup };
            currentY += spacing + 4;

            // Preview Limit Row
            var lblPreviewLimit = new Label { Text = "Preview Items Limit:", Location = new Point(leftMargin, currentY + 3), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            _cmbPreviewLimit = new ComboBox { Location = new Point(leftMargin + 140, currentY), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9F) };
            _cmbPreviewLimit.Items.AddRange(PreviewLimitLabels);
            SelectPreviewLimitIndex(_settings.MaxPreviewItems);
            _cmbPreviewLimit.SelectedIndexChanged += (s, e) => PreviewLimitChanged();

            _lblPreviewLimitWarning = new Label
            {
                Text = "⚠ High preview limits (> 2,000) may cause list lag on massive folders.",
                Location = new Point(leftMargin, currentY + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9),
                Visible = GetSelectedPreviewLimit() > 2000
            };
            currentY += 48;

            // Buttons: Left (Config Location, Defaults, File Types), Right (Save / Cancel)
            int buttonY = this.ClientSize.Height - 52;
            _btnOpenConfig = new Button { Text = "📂 Config Location", UseMnemonic = false, Size = new Size(125, 32), Location = new Point(leftMargin, buttonY), Font = new Font("Segoe UI Emoji", 8.5F) };
            _btnOpenConfig.Click += (s, e) => AppConstants.OpenConfigLocation();

            _btnDefaults = new Button { Text = "↺ Defaults", UseMnemonic = false, Size = new Size(85, 32), Location = new Point(leftMargin + 130, buttonY), Font = new Font("Segoe UI Emoji", 8.5F) };
            _btnDefaults.Click += (s, e) => RestoreDefaults();

            _btnTypeRules = new Button { Text = "🗂️ File Types...", UseMnemonic = false, Size = new Size(100, 32), Location = new Point(leftMargin + 220, buttonY), Font = new Font("Segoe UI Emoji", 8.5F) };
            _btnTypeRules.Click += (s, e) =>
            {
                using var dlg = new Forms.TypeRulesDialog(_isDarkMode);
                dlg.ShowDialog(this);
                var fresh = AppSettings.LoadFromFile();
                _settings.CategoryPrefix = fresh.CategoryPrefix;
                _settings.CategorySuffix = fresh.CategorySuffix;
            };

            _btnCancel = new Button { Text = "Cancel", Size = new Size(90, 32), Location = new Point(this.ClientSize.Width - leftMargin - 90, buttonY), DialogResult = DialogResult.Cancel };
            _btnOK = new Button { Text = "Save", Size = new Size(90, 32), Location = new Point(this.ClientSize.Width - leftMargin - 90 - 10 - 90, buttonY), DialogResult = DialogResult.OK };
            _btnOK.Click += BtnOK_Click;

            this.Controls.AddRange([
                lblOrgHeader, _chkIncludeFolders, _chkIgnoreSystemFiles, lblFileDate, lblFolderDate, _cmbFileDateSource, _cmbFolderDateSource,
                lblNamingHeader, _cmbFormat, _btnFlipOrder, _chkShortMonth, lblPrefix, lblSuffix, _txtPrefix, _txtSuffix,
                _pnlLivePreview,
                lblAppearanceHeader, _btnThemeLight, _btnThemeDark,
                lblBehaviorHeader, _chkShowProgress, _chkGenerateCsvLog, _chkShowOnTop, _chkUse24Hour, _chkAutoLoadExeDir,
                lblPreviewLimit, _cmbPreviewLimit, _lblPreviewLimitWarning,
                _btnOpenConfig, _btnDefaults, _btnTypeRules, _btnOK, _btnCancel
            ]);

            this.AcceptButton = _btnOK;
            this.CancelButton = _btnCancel;

            this.Shown += (s, e) => ApplyDialogTheme(_settings.DarkMode);
            UpdateLivePreview();
        }
    }
}
