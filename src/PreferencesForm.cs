using System;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer
{
    public partial class PreferencesForm : Form
    {
        private AppSettings _settings;
        private CheckBox _chkIncludeFolders = null!;
        private CheckBox _chkIgnoreSystemFiles = null!;
        private CheckBox _chkShowProgress = null!;
        private CheckBox _chkShowOnTop = null!;
        private CheckBox _chkGenerateCsvLog = null!;
        private CheckBox _chkUse24Hour = null!;
        private CheckBox _chkAutoLoadExeDir = null!;
        private CheckBox _chkDarkMode = null!;
        private ComboBox _cmbFileDateSource = null!;
        private ComboBox _cmbFolderDateSource = null!;

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
        private Button _btnOpenConfig = null!;
        private Button _btnDefaults = null!;
        private Button _btnOK = null!;
        private Button _btnCancel = null!;

        // Order & Style Modifiers
        private bool _isFlipped = false;
        private bool _useShortMonth = false;

        public AppSettings Settings => _settings;

        private enum CoreFormat { Header, YearMonth, IsoMonth, Daily, YearQuarter, YearQuarterMonths, YearHalf, YearOnly }

        private class FormatItem(string displayName, CoreFormat core, bool isHeader = false)
        {
            public string DisplayName { get; set; } = displayName;
            public CoreFormat Core { get; } = core;
            public bool IsHeader { get; } = isHeader;
            public override string ToString() => DisplayName;
        }

        private static string GetFormatDisplayName(CoreFormat core, bool flipped, bool shortMonth, int year)
        {
            string m = shortMonth ? "Mar" : "March";
            string qm = shortMonth ? "Jan, Feb & Mar" : "January, February & March";
            return core switch
            {
                CoreFormat.YearMonth => flipped ? $"Month & Year (e.g. {m} {year})" : $"Year & Month (e.g. {year} {m})",
                CoreFormat.IsoMonth => flipped ? $"ISO 8601 (e.g. 03-{year})" : $"ISO 8601 (e.g. {year}-03)",
                CoreFormat.Daily => flipped ? $"Day, Month & Year (e.g. 15 {m} {year})" : $"Year, Month & Day (e.g. {year} {m} 15)",
                CoreFormat.YearQuarter => flipped ? $"Quarter & Year (e.g. Q1 {year})" : $"Year & Quarter (e.g. {year} Q1)",
                CoreFormat.YearQuarterMonths => $"Year & Quarter with Months (e.g. {year} Q1 ({qm}))",
                CoreFormat.YearHalf => flipped ? $"Half & Year (e.g. H1 {year})" : $"Year & Half (e.g. {year} H1)",
                CoreFormat.YearOnly => $"Year (e.g. {year})",
                _ => ""
            };
        }

        public PreferencesForm(AppSettings currentSettings)
        {
            _settings = new AppSettings
            {
                IncludeTopLevelFolders = currentSettings.IncludeTopLevelFolders,
                IgnoreSystemFiles = currentSettings.IgnoreSystemFiles,
                ShowDetailedProgress = currentSettings.ShowDetailedProgress,
                ShowOnTop = currentSettings.ShowOnTop,
                GenerateCsvLog = currentSettings.GenerateCsvLog,
                Use24HourTimestamp = currentSettings.Use24HourTimestamp,
                AutoLoadExeDirectoryOnStartup = currentSettings.AutoLoadExeDirectoryOnStartup,
                FolderFormat = currentSettings.FolderFormat,
                FolderPrefix = currentSettings.FolderPrefix,
                FolderSuffix = currentSettings.FolderSuffix,
                FileDateSource = currentSettings.FileDateSource,
                FolderDateSource = currentSettings.FolderDateSource,
                DarkMode = currentSettings.DarkMode
            };
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Preferences";
            if (AppConstants.AppIcon != null) this.Icon = AppConstants.AppIcon;
            this.Size = new Size(580, 840);
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

            // Date Source Row: Files & Folders
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

            // Detect initial modifier states from _settings.FolderFormat
            _isFlipped = _settings.FolderFormat switch
            {
                FolderFormat.MonthYear or FolderFormat.ShortMonthYear or FolderFormat.DayMonthYear or FolderFormat.DayShortMonthYear or FolderFormat.QuarterYear or FolderFormat.HalfYear or FolderFormat.MonthIso => true,
                _ => false
            };

            _useShortMonth = _settings.FolderFormat switch
            {
                FolderFormat.YearShortMonth or FolderFormat.ShortMonthYear or FolderFormat.YearShortMonthDay or FolderFormat.DayShortMonthYear or FolderFormat.YearQuarterShortMonths => true,
                _ => false
            };

            // 2. Folder Naming Template
            var lblNamingHeader = new Label { Text = "Date Naming Template:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(leftMargin, currentY), AutoSize = true };
            currentY += 24;

            _cmbFormat = new ComboBox { Location = new Point(leftMargin, currentY), Width = contentWidth, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F), DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 22 };

            int currentYear = DateTime.Now.Year;
            var formatOptions = new FormatItem[]
            {
                new("── Monthly ──────────────────", CoreFormat.Header, true),
                new(GetFormatDisplayName(CoreFormat.YearMonth, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearMonth),
                new(GetFormatDisplayName(CoreFormat.IsoMonth, _isFlipped, _useShortMonth, currentYear), CoreFormat.IsoMonth),
                new("── Daily ────────────────────", CoreFormat.Header, true),
                new(GetFormatDisplayName(CoreFormat.Daily, _isFlipped, _useShortMonth, currentYear), CoreFormat.Daily),
                new("── Quarters & Half-Years ────", CoreFormat.Header, true),
                new(GetFormatDisplayName(CoreFormat.YearQuarter, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearQuarter),
                new(GetFormatDisplayName(CoreFormat.YearQuarterMonths, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearQuarterMonths),
                new(GetFormatDisplayName(CoreFormat.YearHalf, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearHalf),
                new("── Year Only ────────────────", CoreFormat.Header, true),
                new(GetFormatDisplayName(CoreFormat.YearOnly, _isFlipped, _useShortMonth, currentYear), CoreFormat.YearOnly)
            };

            _cmbFormat.Items.AddRange(formatOptions);
            _cmbFormat.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= _cmbFormat.Items.Count || _cmbFormat.Items[e.Index] is not FormatItem item) return;
                var baseFont = e.Font ?? _cmbFormat.Font ?? this.Font;
                bool isDark = _chkDarkMode?.Checked ?? _settings.DarkMode;
                var palette = AppTheme.GetPalette(isDark);

                if (item.IsHeader)
                {
                    using var bgBrush = new SolidBrush(isDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(243, 244, 246));
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                    using var textBrush = new SolidBrush(isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(107, 114, 128));
                    using var headerFont = new Font(baseFont, FontStyle.Bold);
                    e.Graphics.DrawString(item.DisplayName, headerFont, textBrush, e.Bounds.X + 4, e.Bounds.Y + 3);
                }
                else
                {
                    e.DrawBackground();
                    using var textBrush = new SolidBrush((e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : palette.TextPrimary);
                    e.Graphics.DrawString($"  {item.DisplayName}", baseFont, textBrush, e.Bounds.X + 8, e.Bounds.Y + 3);
                    e.DrawFocusRectangle();
                }
            };

            currentY += 30;

            // Modifiers Row: Flip Button + Short Month Checkbox
            _btnFlipOrder = new Button { Text = "⇄ Order: Standard", Location = new Point(leftMargin, currentY), Size = new Size(160, 30), Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = Color.FromArgb(243, 244, 246), Cursor = Cursors.Hand };
            _btnFlipOrder.Click += (s, e) => { _isFlipped = !_isFlipped; UpdateOptionsState(); UpdateLivePreview(); };

            _chkShortMonth = new CheckBox { Text = "Short month name (e.g. Jan)", Location = new Point(leftMargin + 172, currentY + 4), AutoSize = true, Font = new Font("Segoe UI", 9F), Checked = _useShortMonth };
            _chkShortMonth.CheckedChanged += (s, e) => { _useShortMonth = _chkShortMonth.Checked; UpdateOptionsState(); UpdateLivePreview(); };

            CoreFormat initialCore = _settings.FolderFormat switch
            {
                FolderFormat.YearMonth or FolderFormat.MonthYear or FolderFormat.YearShortMonth or FolderFormat.ShortMonthYear => CoreFormat.YearMonth,
                FolderFormat.IsoMonth or FolderFormat.MonthIso or FolderFormat.IsoDate => CoreFormat.IsoMonth,
                FolderFormat.YearMonthDay or FolderFormat.DayMonthYear or FolderFormat.YearShortMonthDay or FolderFormat.DayShortMonthYear => CoreFormat.Daily,
                FolderFormat.YearQuarter or FolderFormat.QuarterYear => CoreFormat.YearQuarter,
                FolderFormat.YearQuarterMonths or FolderFormat.YearQuarterShortMonths => CoreFormat.YearQuarterMonths,
                FolderFormat.YearHalf or FolderFormat.HalfYear => CoreFormat.YearHalf,
                FolderFormat.YearOnly => CoreFormat.YearOnly,
                _ => CoreFormat.YearMonth
            };

            for (int i = 0; i < formatOptions.Length; i++)
            {
                if (!formatOptions[i].IsHeader && formatOptions[i].Core == initialCore)
                {
                    _cmbFormat.SelectedIndex = i;
                    break;
                }
            }
            if (_cmbFormat.SelectedIndex < 0) _cmbFormat.SelectedIndex = 1;

            _cmbFormat.SelectedIndexChanged += (s, e) =>
            {
                if (_cmbFormat.SelectedItem is FormatItem item && item.IsHeader)
                {
                    int nextIndex = _cmbFormat.SelectedIndex + 1;
                    if (nextIndex < _cmbFormat.Items.Count) { _cmbFormat.SelectedIndex = nextIndex; return; }
                }
                UpdateOptionsState();
                UpdateLivePreview();
            };

            UpdateOptionsState();
            currentY += 38;

            // Custom Prefix & Suffix
            var lblPrefix = new Label { Text = "Custom Prefix (Optional):", Location = new Point(leftMargin, currentY), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            var lblSuffix = new Label { Text = "Custom Suffix (Optional):", Location = new Point(leftMargin + halfWidth + 16, currentY), AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
            currentY += 18;

            _txtPrefix = new TextBox { Location = new Point(leftMargin, currentY), Width = halfWidth, Text = _settings.FolderPrefix, Font = new Font("Segoe UI", 9.5F) };
            _txtPrefix.TextChanged += (s, e) => UpdateLivePreview();

            _txtSuffix = new TextBox { Location = new Point(leftMargin + halfWidth + 16, currentY), Width = halfWidth, Text = _settings.FolderSuffix, Font = new Font("Segoe UI", 9.5F) };
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

            // 3. Application & Window Behavior
            var lblBehaviorHeader = new Label { Text = "General Behavior:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(leftMargin, currentY), AutoSize = true };
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
            currentY += spacing;

            _chkDarkMode = new CheckBox { Text = "Enable Dark Mode (Fluent Slate theme)", Location = new Point(leftMargin, currentY), AutoSize = true, Checked = _settings.DarkMode };
            _chkDarkMode.CheckedChanged += (s, e) => ApplyDialogTheme(_chkDarkMode.Checked);

            // Buttons: Left (Config Location, Defaults), Right (Save / Cancel)
            int buttonY = this.ClientSize.Height - 52;
            _btnOpenConfig = new Button { Text = "📂 Config Location", UseMnemonic = false, Size = new Size(130, 32), Location = new Point(leftMargin, buttonY), Font = new Font("Segoe UI Emoji", 8.5F) };
            _btnOpenConfig.Click += (s, e) => AppConstants.OpenConfigLocation();

            _btnDefaults = new Button { Text = "↺ Defaults", UseMnemonic = false, Size = new Size(90, 32), Location = new Point(leftMargin + 138, buttonY), Font = new Font("Segoe UI Emoji", 8.5F) };
            _btnDefaults.Click += (s, e) => RestoreDefaults();

            _btnCancel = new Button { Text = "Cancel", Size = new Size(90, 32), Location = new Point(this.ClientSize.Width - leftMargin - 90, buttonY), DialogResult = DialogResult.Cancel };
            _btnOK = new Button { Text = "Save", Size = new Size(90, 32), Location = new Point(this.ClientSize.Width - leftMargin - 90 - 10 - 90, buttonY), DialogResult = DialogResult.OK };
            _btnOK.Click += BtnOK_Click;

            this.Controls.AddRange([
                lblOrgHeader, _chkIncludeFolders, _chkIgnoreSystemFiles, lblFileDate, lblFolderDate, _cmbFileDateSource, _cmbFolderDateSource,
                lblNamingHeader, _cmbFormat, _btnFlipOrder, _chkShortMonth, lblPrefix, lblSuffix, _txtPrefix, _txtSuffix,
                _pnlLivePreview, lblBehaviorHeader, _chkShowProgress, _chkGenerateCsvLog, _chkShowOnTop, _chkUse24Hour, _chkAutoLoadExeDir, _chkDarkMode,
                _btnOpenConfig, _btnDefaults, _btnOK, _btnCancel
            ]);

            this.AcceptButton = _btnOK;
            this.CancelButton = _btnCancel;

            this.Shown += (s, e) => ApplyDialogTheme(_settings.DarkMode);
            UpdateLivePreview();
        }

        private void UpdateOptionsState()
        {
            if (_cmbFormat.SelectedItem is not FormatItem item) return;

            bool canFlip = item.Core != CoreFormat.YearOnly && item.Core != CoreFormat.YearQuarterMonths;
            bool canShortMonth = item.Core is CoreFormat.YearMonth or CoreFormat.Daily or CoreFormat.YearQuarterMonths;

            bool isDark = _chkDarkMode?.Checked ?? _settings.DarkMode;
            var palette = AppTheme.GetPalette(isDark);
            _btnFlipOrder.Enabled = canFlip;
            _btnFlipOrder.Text = canFlip ? (_isFlipped ? "⇄ Order: Flipped" : "⇄ Order: Standard") : "⇄ Flip (N/A)";
            _btnFlipOrder.BackColor = (canFlip && _isFlipped) ? (isDark ? Color.FromArgb(67, 56, 202) : Color.FromArgb(224, 231, 255)) : palette.SecondaryButtonBg;
            _btnFlipOrder.ForeColor = (canFlip && _isFlipped && isDark) ? Color.White : palette.SecondaryButtonText;
            _chkShortMonth.Enabled = canShortMonth;

            int currentYear = DateTime.Now.Year;
            for (int i = 0; i < _cmbFormat.Items.Count; i++)
            {
                if (_cmbFormat.Items[i] is FormatItem fItem && !fItem.IsHeader)
                {
                    fItem.DisplayName = GetFormatDisplayName(fItem.Core, _isFlipped, _useShortMonth, currentYear);
                }
            }
            _cmbFormat.Invalidate();
        }

        private FolderFormat ResolveFolderFormat()
        {
            if (_cmbFormat.SelectedItem is not FormatItem item) return FolderFormat.YearMonth;

            return item.Core switch
            {
                CoreFormat.YearMonth => (_isFlipped, _useShortMonth) switch
                {
                    (false, false) => FolderFormat.YearMonth,
                    (true, false) => FolderFormat.MonthYear,
                    (false, true) => FolderFormat.YearShortMonth,
                    (true, true) => FolderFormat.ShortMonthYear
                },
                CoreFormat.IsoMonth => _isFlipped ? FolderFormat.MonthIso : FolderFormat.IsoMonth,
                CoreFormat.Daily => (_isFlipped, _useShortMonth) switch
                {
                    (false, false) => FolderFormat.YearMonthDay,
                    (true, false) => FolderFormat.DayMonthYear,
                    (false, true) => FolderFormat.YearShortMonthDay,
                    (true, true) => FolderFormat.DayShortMonthYear
                },
                CoreFormat.YearQuarter => _isFlipped ? FolderFormat.QuarterYear : FolderFormat.YearQuarter,
                CoreFormat.YearQuarterMonths => _useShortMonth ? FolderFormat.YearQuarterShortMonths : FolderFormat.YearQuarterMonths,
                CoreFormat.YearHalf => _isFlipped ? FolderFormat.HalfYear : FolderFormat.YearHalf,
                CoreFormat.YearOnly => FolderFormat.YearOnly,
                _ => FolderFormat.YearMonth
            };
        }

        private void UpdateLivePreview()
        {
            if (_cmbFormat == null || _lblSample1 == null || _lblSample4 == null) return;

            FolderFormat format = ResolveFolderFormat();
            string prefix = _txtPrefix?.Text ?? "";
            string suffix = _txtSuffix?.Text ?? "";

            int currentYear = DateTime.Now.Year;
            var core = (_cmbFormat.SelectedItem is FormatItem item) ? item.Core : CoreFormat.YearMonth;

            var (d1, d2, d3, d4) = core switch
            {
                CoreFormat.Daily => (new DateTime(currentYear, 3, 13), new DateTime(currentYear, 3, 14), new DateTime(currentYear, 3, 15), new DateTime(currentYear, 3, 16)),
                CoreFormat.YearQuarter or CoreFormat.YearQuarterMonths => (new DateTime(currentYear, 2, 1), new DateTime(currentYear, 5, 1), new DateTime(currentYear, 8, 1), new DateTime(currentYear, 11, 1)),
                CoreFormat.YearHalf => (new DateTime(currentYear - 1, 3, 1), new DateTime(currentYear - 1, 9, 1), new DateTime(currentYear, 3, 1), new DateTime(currentYear, 9, 1)),
                CoreFormat.YearOnly => (new DateTime(currentYear - 3, 1, 1), new DateTime(currentYear - 2, 1, 1), new DateTime(currentYear - 1, 1, 1), new DateTime(currentYear, 1, 1)),
                _ => (new DateTime(currentYear, 1, 15), new DateTime(currentYear, 2, 15), new DateTime(currentYear, 3, 15), new DateTime(currentYear, 4, 15))
            };

            _lblSample1.Text = $"  📁 {AppConstants.FormatFolderDate(d1, format, prefix, suffix)}";
            _lblSample2.Text = $"  📁 {AppConstants.FormatFolderDate(d2, format, prefix, suffix)}";
            _lblSample3.Text = $"  📁 {AppConstants.FormatFolderDate(d3, format, prefix, suffix)}";
            _lblSample4.Text = $"  📁 {AppConstants.FormatFolderDate(d4, format, prefix, suffix)}";
        }

        private void RestoreDefaults()
        {
            _chkIncludeFolders.Checked = AppConstants.DefaultIncludeTopLevelFolders;
            _chkIgnoreSystemFiles.Checked = AppConstants.DefaultIgnoreSystemFiles;
            _chkShowProgress.Checked = AppConstants.DefaultShowDetailedProgress;
            _chkShowOnTop.Checked = AppConstants.DefaultShowOnTop;
            _chkGenerateCsvLog.Checked = AppConstants.DefaultGenerateCsvLog;
            _chkUse24Hour.Checked = AppConstants.DefaultUse24HourTimestamp;
            _chkAutoLoadExeDir.Checked = AppConstants.DefaultAutoLoadExeDirectoryOnStartup;
            _chkDarkMode.Checked = AppConstants.DefaultDarkMode;

            _cmbFileDateSource.SelectedIndex = (int)AppConstants.DefaultFileDateSource;
            _cmbFolderDateSource.SelectedIndex = (int)AppConstants.DefaultFolderDateSource;
            _txtPrefix.Text = AppConstants.DefaultFolderPrefix;
            _txtSuffix.Text = AppConstants.DefaultFolderSuffix;

            _isFlipped = false;
            _useShortMonth = false;
            _chkShortMonth.Checked = false;

            for (int i = 0; i < _cmbFormat.Items.Count; i++)
            {
                if (_cmbFormat.Items[i] is FormatItem item && !item.IsHeader && item.Core == CoreFormat.YearMonth)
                {
                    _cmbFormat.SelectedIndex = i;
                    break;
                }
            }

            UpdateOptionsState();
            UpdateLivePreview();
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            _settings.IncludeTopLevelFolders = _chkIncludeFolders.Checked;
            _settings.IgnoreSystemFiles = _chkIgnoreSystemFiles.Checked;
            _settings.FileDateSource = (DateSource)_cmbFileDateSource.SelectedIndex;
            _settings.FolderDateSource = (DateSource)_cmbFolderDateSource.SelectedIndex;
            _settings.ShowDetailedProgress = _chkShowProgress.Checked;
            _settings.ShowOnTop = _chkShowOnTop.Checked;
            _settings.GenerateCsvLog = _chkGenerateCsvLog.Checked;
            _settings.Use24HourTimestamp = _chkUse24Hour.Checked;
            _settings.AutoLoadExeDirectoryOnStartup = _chkAutoLoadExeDir.Checked;
            _settings.DarkMode = _chkDarkMode.Checked;
            _settings.FolderFormat = ResolveFolderFormat();
            _settings.FolderPrefix = AppConstants.SanitizeFolderName(_txtPrefix.Text);
            _settings.FolderSuffix = AppConstants.SanitizeFolderName(_txtSuffix.Text);
        }

        private void ApplyDialogTheme(bool isDark)
        {
            var palette = AppTheme.GetPalette(isDark);
            this.BackColor = palette.CanvasBg;
            this.ForeColor = palette.TextPrimary;
            AppTheme.SetWindowDarkTitleBar(this.Handle, isDark);

            foreach (Control c in this.Controls)
            {
                if (c is CheckBox chk) chk.ForeColor = palette.TextPrimary;
                else if (c is Label lbl) lbl.ForeColor = palette.TextPrimary;
                else if (c is TextBox txt)
                {
                    txt.BackColor = palette.InputBg;
                    txt.ForeColor = palette.TextPrimary;
                }
                else if (c is Button btn && btn != _btnOK && btn != _btnFlipOrder)
                {
                    btn.BackColor = palette.SecondaryButtonBg;
                    btn.ForeColor = palette.SecondaryButtonText;
                }
            }

            if (_pnlLivePreview != null)
            {
                _pnlLivePreview.BackColor = palette.CardBg;
                _lblPreviewTitle.ForeColor = palette.TextMuted;
                _lblSample1.ForeColor = palette.TextPrimary;
                _lblSample2.ForeColor = palette.TextPrimary;
                _lblSample3.ForeColor = palette.TextPrimary;
                _lblSample4.ForeColor = palette.TextPrimary;
            }

            foreach (var cmb in new[] { _cmbFormat, _cmbFileDateSource, _cmbFolderDateSource })
            {
                if (cmb != null) { cmb.BackColor = palette.InputBg; cmb.ForeColor = palette.TextPrimary; }
            }
            if (_btnOK != null)
            {
                _btnOK.BackColor = AppConstants.ColorPrimary;
                _btnOK.ForeColor = Color.White;
            }
            UpdateOptionsState();
        }
    }
}
