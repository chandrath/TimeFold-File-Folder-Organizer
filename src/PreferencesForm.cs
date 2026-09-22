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
        private bool _isFlipped = false;
        private bool _useShortMonth = false;
        private bool _isDarkMode = false;

        public AppSettings Settings => _settings;

        private enum CoreFormat { Header, YearMonth, IsoMonth, Daily, YearQuarter, YearQuarterMonths, YearHalf, YearOnly, YearNestedMonth, YearNestedMonthOnly, YearNestedIso, YearNestedQuarter, YearNestedHalf }

        private class FormatItem(string displayName, CoreFormat core, bool isHeader = false)
        {
            public string DisplayName { get; set; } = displayName;
            public CoreFormat Core { get; } = core;
            public bool IsHeader { get; } = isHeader;
            public override string ToString() => DisplayName;
        }

        private static readonly int[] PreviewLimitValues = { 1000, 2000, 5000, 10000, 20000, 50000, 100000 };
        private static readonly string[] PreviewLimitLabels =
        {
            "1,000 items (Default)",
            "2,000 items",
            "5,000 items",
            "10,000 items",
            "20,000 items",
            "50,000 items",
            "100,000 items (Max)"
        };

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
                CoreFormat.YearNestedMonth => shortMonth ? $"Year / Year & Month (e.g. {year} > {year} Mar)" : $"Year / Year & Month (e.g. {year} > {year} {m})",
                CoreFormat.YearNestedMonthOnly => shortMonth ? $"Year / Month (e.g. {year} > Mar)" : $"Year / Month (e.g. {year} > {m})",
                CoreFormat.YearNestedIso => $"Year / ISO Month (e.g. {year} > {year}-03)",
                CoreFormat.YearNestedQuarter => flipped ? $"Year / Quarter & Year (e.g. {year} > Q1 {year})" : $"Year / Year & Quarter (e.g. {year} > {year} Q1)",
                CoreFormat.YearNestedHalf => flipped ? $"Year / Half & Year (e.g. {year} > H1 {year})" : $"Year / Year & Half (e.g. {year} > {year} H1)",
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
                MaxPreviewItems = currentSettings.MaxPreviewItems,
                DarkMode = currentSettings.DarkMode,
                HasSeenWelcomeTour = currentSettings.HasSeenWelcomeTour,
                OrgMode = currentSettings.OrgMode,
                CategoryPrefix = currentSettings.CategoryPrefix,
                CategorySuffix = currentSettings.CategorySuffix,
                KeepHtmlCompanionsTogether = currentSettings.KeepHtmlCompanionsTogether,
                KeepSubtitleCompanionsTogether = currentSettings.KeepSubtitleCompanionsTogether,
                RecentFolders = new System.Collections.Generic.List<string>(currentSettings.RecentFolders)
            };
            _isDarkMode = _settings.DarkMode;
            InitializeComponent();
        }

        private void SelectPreviewLimitIndex(int value)
        {
            int selectedIndex = 0;
            for (int i = 0; i < PreviewLimitValues.Length; i++)
            {
                if (PreviewLimitValues[i] == value)
                {
                    selectedIndex = i;
                    break;
                }
            }
            _cmbPreviewLimit.SelectedIndex = selectedIndex;
        }

        private int GetSelectedPreviewLimit()
        {
            int idx = _cmbPreviewLimit?.SelectedIndex ?? 0;
            return (idx >= 0 && idx < PreviewLimitValues.Length) ? PreviewLimitValues[idx] : AppConstants.DefaultMaxPreviewItems;
        }

        private void PreviewLimitChanged()
        {
            if (_lblPreviewLimitWarning == null) return;
            int limit = GetSelectedPreviewLimit();
            _lblPreviewLimitWarning.Visible = limit > 2000;
        }

        private void UpdateOptionsState()
        {
            if (_cmbFormat.SelectedItem is not FormatItem item) return;

            bool canFlip = item.Core is CoreFormat.YearMonth or CoreFormat.IsoMonth or CoreFormat.Daily
                or CoreFormat.YearQuarter or CoreFormat.YearHalf
                or CoreFormat.YearNestedMonth or CoreFormat.YearNestedIso
                or CoreFormat.YearNestedQuarter or CoreFormat.YearNestedHalf;
            bool canShortMonth = item.Core is CoreFormat.YearMonth or CoreFormat.Daily or CoreFormat.YearQuarterMonths
                or CoreFormat.YearNestedMonth or CoreFormat.YearNestedMonthOnly;

            bool isDark = _isDarkMode;
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
                CoreFormat.YearNestedMonth => (_isFlipped, _useShortMonth) switch
                {
                    (false, false) => FolderFormat.YearWithMonth,
                    (true,  false) => FolderFormat.YearWithMonthFlipped,
                    (false, true)  => FolderFormat.YearWithShortMonth,
                    (true,  true)  => FolderFormat.YearWithShortMonthFlipped
                },
                CoreFormat.YearNestedMonthOnly => _useShortMonth ? FolderFormat.YearWithShortMonthOnly : FolderFormat.YearWithMonthOnly,
                CoreFormat.YearNestedIso => _isFlipped ? FolderFormat.YearWithIsoMonthFlipped : FolderFormat.YearWithIsoMonth,
                CoreFormat.YearNestedQuarter => _isFlipped ? FolderFormat.YearWithQuarterFlipped : FolderFormat.YearWithQuarter,
                CoreFormat.YearNestedHalf => _isFlipped ? FolderFormat.YearWithHalfFlipped : FolderFormat.YearWithHalf,
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
                CoreFormat.YearNestedMonth or CoreFormat.YearNestedMonthOnly or CoreFormat.YearNestedIso => (new DateTime(currentYear, 1, 15), new DateTime(currentYear, 3, 15), new DateTime(currentYear, 6, 15), new DateTime(currentYear, 9, 15)),
                CoreFormat.YearNestedQuarter => (new DateTime(currentYear, 2, 1), new DateTime(currentYear, 5, 1), new DateTime(currentYear, 8, 1), new DateTime(currentYear, 11, 1)),
                CoreFormat.YearNestedHalf => (new DateTime(currentYear - 1, 3, 1), new DateTime(currentYear - 1, 9, 1), new DateTime(currentYear, 3, 1), new DateTime(currentYear, 9, 1)),
                _ => (new DateTime(currentYear, 1, 15), new DateTime(currentYear, 2, 15), new DateTime(currentYear, 3, 15), new DateTime(currentYear, 4, 15))
            };

            _lblSample1.Text = $"  📁 {AppConstants.FormatFolderDate(d1, format, prefix, suffix)}";
            _lblSample2.Text = $"  📁 {AppConstants.FormatFolderDate(d2, format, prefix, suffix)}";
            _lblSample3.Text = $"  📁 {AppConstants.FormatFolderDate(d3, format, prefix, suffix)}";
            _lblSample4.Text = $"  📁 {AppConstants.FormatFolderDate(d4, format, prefix, suffix)}";
        }

        private void SetTheme(bool isDark)
        {
            _isDarkMode = isDark;
            ApplyDialogTheme(_isDarkMode);
        }

        private void RestoreDefaults()
        {
            _chkIncludeFolders.Checked = AppConstants.DefaultIncludeTopLevelFolders;
            _chkIgnoreSystemFiles.Checked = AppConstants.DefaultIgnoreSystemFiles;
            _settings.KeepSubtitleCompanionsTogether = AppConstants.DefaultKeepSubtitleCompanionsTogether;
            _settings.KeepHtmlCompanionsTogether = AppConstants.DefaultKeepHtmlCompanionsTogether;
            _chkShowProgress.Checked = AppConstants.DefaultShowDetailedProgress;
            _chkShowOnTop.Checked = AppConstants.DefaultShowOnTop;
            _chkGenerateCsvLog.Checked = AppConstants.DefaultGenerateCsvLog;
            _chkUse24Hour.Checked = AppConstants.DefaultUse24HourTimestamp;
            _chkAutoLoadExeDir.Checked = AppConstants.DefaultAutoLoadExeDirectoryOnStartup;
            _isDarkMode = AppConstants.DefaultDarkMode;

            _cmbFileDateSource.SelectedIndex = (int)AppConstants.DefaultFileDateSource;
            _cmbFolderDateSource.SelectedIndex = (int)AppConstants.DefaultFolderDateSource;
            _txtPrefix.Text = AppConstants.DefaultFolderPrefix;
            _txtSuffix.Text = AppConstants.DefaultFolderSuffix;
            SelectPreviewLimitIndex(AppConstants.DefaultMaxPreviewItems);

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

            _settings.OrgMode = OrganizationMode.Date;
            _settings.CategoryPrefix = AppConstants.DefaultCategoryPrefix;
            _settings.CategorySuffix = AppConstants.DefaultCategorySuffix;
            _settings.HasSeenWelcomeTour = true;

            UpdateOptionsState();
            UpdateLivePreview();
            PreviewLimitChanged();
            ApplyDialogTheme(_isDarkMode);
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
            _settings.DarkMode = _isDarkMode;
            _settings.FolderFormat = ResolveFolderFormat();
            _settings.FolderPrefix = AppConstants.SanitizeFolderName(_txtPrefix.Text);
            _settings.FolderSuffix = AppConstants.SanitizeFolderName(_txtSuffix.Text);
            _settings.MaxPreviewItems = GetSelectedPreviewLimit();
        }

        private void ApplyDialogTheme(bool isDark)
        {
            _isDarkMode = isDark;
            var palette = AppTheme.GetPalette(isDark);
            this.BackColor = palette.CanvasBg;
            this.ForeColor = palette.TextPrimary;
            AppTheme.SetWindowDarkTitleBar(this.Handle, isDark);

            foreach (Control c in this.Controls)
            {
                if (c is CheckBox chk) chk.ForeColor = palette.TextPrimary;
                else if (c is Label lbl && lbl != _lblPreviewLimitWarning) lbl.ForeColor = palette.TextPrimary;
                else if (c is TextBox txt)
                {
                    txt.BackColor = palette.InputBg;
                    txt.ForeColor = palette.TextPrimary;
                }
                else if (c is Button btn && btn != _btnOK && btn != _btnFlipOrder && btn != _btnThemeLight && btn != _btnThemeDark)
                {
                    btn.BackColor = palette.SecondaryButtonBg;
                    btn.ForeColor = palette.SecondaryButtonText;
                }
            }

            // Highlight active theme button
            if (_btnThemeLight != null && _btnThemeDark != null)
            {
                if (!isDark) // Light Mode active
                {
                    _btnThemeLight.BackColor = Color.FromArgb(224, 231, 255);
                    _btnThemeLight.ForeColor = Color.FromArgb(30, 58, 138);
                    _btnThemeLight.Font = new Font("Segoe UI Emoji", 9F, FontStyle.Bold);
                    _btnThemeLight.FlatStyle = FlatStyle.Flat;
                    _btnThemeLight.FlatAppearance.BorderColor = Color.FromArgb(59, 130, 246);
                    _btnThemeLight.FlatAppearance.BorderSize = 2;

                    _btnThemeDark.BackColor = palette.SecondaryButtonBg;
                    _btnThemeDark.ForeColor = palette.TextMuted;
                    _btnThemeDark.Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular);
                    _btnThemeDark.FlatStyle = FlatStyle.Flat;
                    _btnThemeDark.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;
                    _btnThemeDark.FlatAppearance.BorderSize = 1;
                }
                else // Dark Mode active
                {
                    _btnThemeDark.BackColor = Color.FromArgb(30, 58, 138);
                    _btnThemeDark.ForeColor = Color.White;
                    _btnThemeDark.Font = new Font("Segoe UI Emoji", 9F, FontStyle.Bold);
                    _btnThemeDark.FlatStyle = FlatStyle.Flat;
                    _btnThemeDark.FlatAppearance.BorderColor = Color.FromArgb(96, 165, 250);
                    _btnThemeDark.FlatAppearance.BorderSize = 2;

                    _btnThemeLight.BackColor = palette.SecondaryButtonBg;
                    _btnThemeLight.ForeColor = palette.TextMuted;
                    _btnThemeLight.Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular);
                    _btnThemeLight.FlatStyle = FlatStyle.Flat;
                    _btnThemeLight.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;
                    _btnThemeLight.FlatAppearance.BorderSize = 1;
                }
            }

            if (_lblPreviewLimitWarning != null)
            {
                _lblPreviewLimitWarning.ForeColor = isDark ? Color.FromArgb(251, 191, 36) : Color.FromArgb(180, 83, 9);
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

            foreach (var cmb in new[] { _cmbFormat, _cmbFileDateSource, _cmbFolderDateSource, _cmbPreviewLimit })
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
