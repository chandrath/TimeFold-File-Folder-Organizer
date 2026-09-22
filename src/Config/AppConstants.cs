using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using FileOrganizer.Models;

namespace FileOrganizer.Config
{
    /// <summary>
    /// Single Source of Truth (SSoT) for application metadata, default preferences, and UI constants.
    /// </summary>
    public static class AppConstants
    {
        // Application Metadata (SSoT)
        // NOTE: AppVersion is dynamically resolved from the build assembly stamped by TimeFold.csproj (<Version>x.y.z</Version>).
        // To bump the version across the entire app, simply change <Version> in TimeFold.csproj.
        public const string VendorName = "Appsphinx";
        public const string AppName = "TimeFold: File & Folder Organizer";
        public const string ShortAppName = "TimeFold";
        public static string AppVersion => typeof(AppConstants).Assembly.GetName().Version!.ToString(3);
        private static readonly Lazy<string> _lazyBuildNumber = new(() =>
        {
            try
            {
                var attrs = typeof(AppConstants).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false);
                foreach (System.Reflection.AssemblyMetadataAttribute attr in attrs)
                {
                    if (attr.Key == "BuildTimestamp" && !string.IsNullOrWhiteSpace(attr.Value))
                    {
                        return attr.Value;
                    }
                }
            }
            catch { }
            return "20260921";
        });

        public static string BuildNumber => _lazyBuildNumber.Value;
        public const string AppTagline = "Effortlessly organize files & folders into clean date-based timelines or smart categories";
        public const string AppDescription = "Fast, non-destructive file and folder organizer for Windows that sorts messy directories into clean date-based timelines or smart file-type categories.";
        public const string Author = "Shree";
        public const string RepositoryUrl = "https://github.com/chandrath/TimeFold-File-Folder-Organizer";
        public const string LicenseText = "GNU General Public License v3.0 (GPLv3) - Free and Open Source";
        public const string CopyrightText = LicenseText;

        // Application Icon & Logo (SSoT)
        private static readonly Lazy<Icon?> _lazyAppIcon = new(() =>
        {
            try
            {
                using var stream = typeof(AppConstants).Assembly.GetManifestResourceStream("FileOrganizer.Assets.app.ico");
                if (stream != null) return new Icon(stream);
            }
            catch { }
            return null;
        });

        private static readonly Lazy<Image?> _lazyAppLogo = new(() =>
        {
            try
            {
                using var stream = typeof(AppConstants).Assembly.GetManifestResourceStream("FileOrganizer.Assets.app.png");
                if (stream != null) return Image.FromStream(stream);
            }
            catch { }
            return null;
        });

        public static Icon? AppIcon => _lazyAppIcon.Value;
        public static Image? AppLogo => _lazyAppLogo.Value;

        // Configuration File Paths (SSoT)
        public static string GetConfigDirectoryPath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), VendorName, ShortAppName);

        public static string GetLegacyConfigDirectoryPath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ShortAppName);

        public static string GetConfigFilePath() =>
            Path.Combine(GetConfigDirectoryPath(), "settings.json");

        public static string GetCustomTypesFilePath() =>
            Path.Combine(GetConfigDirectoryPath(), "custom_types.json");

        public static void OpenConfigLocation()
        {
            string dir = GetConfigDirectoryPath();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string file = GetConfigFilePath();
            if (File.Exists(file))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{file}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
        }

        public static bool CheckDirectoryWritePermission(string targetDir, IWin32Window? owner = null)
        {
            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                string testFile = Path.Combine(targetDir, $".tf_perm_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                var choice = MessageBox.Show(
                    owner,
                    $"Windows denied write access to folder:\n{targetDir}\n\n" +
                    "This usually occurs when Windows Controlled Folder Access (Ransomware Protection) protects personal folders (Documents, Desktop, etc.).\n\n" +
                    "Would you like to open Windows Security to allow TimeFold?",
                    "Write Permission Denied",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (choice == DialogResult.Yes)
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("windowsdefender://ransomwareprotection") { UseShellExecute = true }); }
                    catch { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:windowsdefender") { UseShellExecute = true }); }
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"Unable to write to destination folder:\n{ex.Message}", "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Output & Logging Prefixes (SSoT)
        public const string SortedFolderPrefix = "Sorted_";
        public const string DefaultGroupedFolderName = "Grouped Folders";
        public const string CsvLogPrefix = ShortAppName + "_Log_";
        public const string LegacyCsvLogPrefix = "OrganizationLog_";

        // Default User Preferences
        public const bool DefaultIncludeTopLevelFolders = true;
        public const bool DefaultIgnoreSystemFiles = true;
        public const bool DefaultShowDetailedProgress = true;
        public const bool DefaultShowOnTop = false;
        public const bool DefaultGenerateCsvLog = true;
        public const bool DefaultUse24HourTimestamp = false;
        public const bool DefaultAutoLoadExeDirectoryOnStartup = false;
        public const bool DefaultDarkMode = false;
        public const bool DefaultHasSeenWelcomeTour = false;
        public const bool DefaultCreateSortedSubfolder = true;
        public const int MaxRecentFolders = 5;
        public const FolderFormat DefaultFolderFormat = FolderFormat.YearMonth;
        public const string DefaultFolderPrefix = "";
        public const string DefaultFolderSuffix = "";
        public const string DefaultCategoryPrefix = "";
        public const string DefaultCategorySuffix = "";
        public const int MaxPrefixSuffixLength = 30;
        public const bool DefaultKeepHtmlCompanionsTogether = true;
        public const bool DefaultKeepSubtitleCompanionsTogether = true;
        public const DateSource DefaultFileDateSource = DateSource.Modified;
        public const DateSource DefaultFolderDateSource = DateSource.Modified;
        public const int DefaultMaxPreviewItems = 1000;
        public const int MaxAllowedPreviewItems = 100000;
        public const int MinAllowedPreviewItems = 100;

        // Detection Thresholds
        public const double TimestampSimilarityThreshold = 0.85; // 85%

        public static string GetSortedFolderName(string outputDirectory, bool use24Hour, DateTime? now = null)
        {
            var dt = now ?? DateTime.Now;
            string timestamp = use24Hour
                ? dt.ToString("yyyy-MM-dd_HH-mm")
                : dt.ToString("yyyy-MM-dd_hh-mmtt");

            string baseFolder = Path.Combine(outputDirectory, $"{SortedFolderPrefix}{timestamp}");
            string folder = baseFolder;
            int counter = 1;
            while (Directory.Exists(folder))
            {
                folder = $"{baseFolder} ({counter++})";
            }
            return folder;
        }

        public static string GetSortedFolderPreviewPattern(bool use24Hour)
        {
            return use24Hour
                ? $"{SortedFolderPrefix}YYYY-MM-DD_HH-mm"
                : $"{SortedFolderPrefix}YYYY-MM-DD_hh-mmtt";
        }

        public static string GetSortedFolderPreviewPath(string outputDirectory, bool use24Hour)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                return string.Empty;

            return Path.Combine(outputDirectory, GetSortedFolderPreviewPattern(use24Hour));
        }

        // Known Windows System Files and Protected Directories
        public static readonly HashSet<string> KnownSystemFilesAndDirs = new(StringComparer.OrdinalIgnoreCase)
        {
            "desktop.ini",
            "thumbs.db",
            "ehthumbs.db",
            "ehthumbs_vista.db",
            "$recycle.bin",
            "system volume information"
        };

        // UI Theme & Metrics (SSoT)
        public const int DefaultPadding = 12;
        public static readonly Color ColorPrimary = Color.FromArgb(37, 99, 235);          // #2563EB Vibrant Blue
        public static readonly Color ColorPrimaryHover = Color.FromArgb(29, 78, 216);     // #1D4ED8
        public static readonly Color ColorSecondary = Color.FromArgb(255, 255, 255);      // #FFFFFF Crisp White
        public static readonly Color ColorSecondaryBorder = Color.FromArgb(209, 213, 219); // #D1D5DB
        public static readonly Color ColorSecondaryText = Color.FromArgb(30, 41, 59);     // #1E293B Slate 800
        public static readonly Color ColorBadgeBg = Color.FromArgb(238, 242, 255);        // #EEF2FF Soft Indigo
        public static readonly Color ColorBadgeText = Color.FromArgb(67, 56, 202);        // #4338CA Deep Indigo
        public static readonly Color ColorBadgeBorder = Color.FromArgb(199, 210, 254);     // #C7D2FE
        public static readonly Color ColorSuccess = Color.FromArgb(16, 185, 129);         // #10B981 Emerald
        public static readonly Color ColorSuccessBg = Color.FromArgb(236, 253, 245);       // #ECFDF5
        public static readonly Color ColorDanger = Color.FromArgb(220, 38, 38);           // #DC2626
        public static readonly Color ColorDangerBg = Color.FromArgb(254, 242, 242);       // #FEF2F2
        public static readonly Color ColorSurfaceBg = Color.FromArgb(248, 250, 252);      // #F8FAFC Canvas
        public static readonly Color ColorTextDark = Color.FromArgb(15, 23, 42);          // #0F172A Slate 900
        public static readonly Color ColorTextMuted = Color.FromArgb(100, 116, 139);      // #64748B Slate 500
        public static readonly Color ColorBorder = Color.FromArgb(226, 232, 240);         // #E2E8F0 Border

        public static string SanitizeFolderName(string input, int maxLength = MaxPrefixSuffixLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new System.Text.StringBuilder(Math.Min(input.Length, maxLength));
            foreach (char c in input)
            {
                if (Array.IndexOf(invalidChars, c) < 0)
                {
                    sanitized.Append(c);
                    if (sanitized.Length >= maxLength)
                        break;
                }
            }
            return sanitized.ToString();
        }

        private static readonly string[] QuarterMonthsFull =
        [
            "January, February & March",
            "April, May & June",
            "July, August & September",
            "October, November & December"
        ];

        private static readonly string[] QuarterMonthsShort =
        [
            "Jan, Feb & Mar",
            "Apr, May & Jun",
            "Jul, Aug & Sep",
            "Oct, Nov & Dec"
        ];

        public static string FormatFolderDate(DateTime date, FolderFormat format, string prefix = "", string suffix = "")
        {
            int year = date.Year;
            int month = date.Month;
            int day = date.Day;
            string monthFull = date.ToString("MMMM", CultureInfo.InvariantCulture);
            string monthShort = date.ToString("MMM", CultureInfo.InvariantCulture);
            int quarter = (month - 1) / 3 + 1;
            int half = (month <= 6) ? 1 : 2;

            string core = format switch
            {
                FolderFormat.YearMonth => $"{year} {monthFull}",
                FolderFormat.MonthYear => $"{monthFull} {year}",
                FolderFormat.YearShortMonth => $"{year} {monthShort}",
                FolderFormat.ShortMonthYear => $"{monthShort} {year}",
                FolderFormat.IsoMonth => $"{year}-{month:D2}",
                FolderFormat.MonthIso => $"{month:D2}-{year}",
                FolderFormat.IsoDate => $"{year}-{month:D2}-{day:D2}",
                FolderFormat.YearMonthDay => $"{year} {monthFull} {day}",
                FolderFormat.DayMonthYear => $"{day} {monthFull} {year}",
                FolderFormat.YearShortMonthDay => $"{year} {monthShort} {day}",
                FolderFormat.DayShortMonthYear => $"{day} {monthShort} {year}",
                FolderFormat.YearQuarter => $"{year} Q{quarter}",
                FolderFormat.QuarterYear => $"Q{quarter} {year}",
                FolderFormat.YearQuarterMonths => $"{year} Q{quarter} ({QuarterMonthsFull[quarter - 1]})",
                FolderFormat.YearQuarterShortMonths => $"{year} Q{quarter} ({QuarterMonthsShort[quarter - 1]})",
                FolderFormat.YearHalf => $"{year} H{half}",
                FolderFormat.HalfYear => $"H{half} {year}",
                FolderFormat.YearOnly => $"{year}",
                // Year-nested: returns "year\subfolder" path
                FolderFormat.YearWithMonth => System.IO.Path.Combine($"{year}", $"{year} {monthFull}"),
                FolderFormat.YearWithShortMonth => System.IO.Path.Combine($"{year}", $"{year} {monthShort}"),
                FolderFormat.YearWithMonthFlipped => System.IO.Path.Combine($"{year}", $"{monthFull} {year}"),
                FolderFormat.YearWithShortMonthFlipped => System.IO.Path.Combine($"{year}", $"{monthShort} {year}"),
                FolderFormat.YearWithMonthOnly => System.IO.Path.Combine($"{year}", $"{monthFull}"),
                FolderFormat.YearWithShortMonthOnly => System.IO.Path.Combine($"{year}", $"{monthShort}"),
                FolderFormat.YearWithIsoMonth => System.IO.Path.Combine($"{year}", $"{year}-{month:D2}"),
                FolderFormat.YearWithIsoMonthFlipped => System.IO.Path.Combine($"{year}", $"{month:D2}-{year}"),
                FolderFormat.YearWithQuarter => System.IO.Path.Combine($"{year}", $"{year} Q{quarter}"),
                FolderFormat.YearWithQuarterFlipped => System.IO.Path.Combine($"{year}", $"Q{quarter} {year}"),
                FolderFormat.YearWithHalf => System.IO.Path.Combine($"{year}", $"{year} H{half}"),
                FolderFormat.YearWithHalfFlipped => System.IO.Path.Combine($"{year}", $"H{half} {year}"),
                _ => $"{year} {monthFull}"
            };

            string cleanPrefix = SanitizeFolderName(prefix);
            string cleanSuffix = SanitizeFolderName(suffix);

            if (!string.IsNullOrEmpty(cleanPrefix) && !cleanPrefix.EndsWith(" ") && !cleanPrefix.EndsWith("_") && !cleanPrefix.EndsWith("-"))
            {
                cleanPrefix += " ";
            }

            if (!string.IsNullOrEmpty(cleanSuffix) && !cleanSuffix.StartsWith(" ") && !cleanSuffix.StartsWith("_") && !cleanSuffix.StartsWith("-"))
            {
                cleanSuffix = " " + cleanSuffix;
            }

            return $"{cleanPrefix}{core}{cleanSuffix}";
        }

        public static string FormatCategoryFolder(string category, string prefix = "", string suffix = "")
        {
            if (string.IsNullOrWhiteSpace(category)) category = DefaultGroupedFolderName;
            string cleanPrefix = SanitizeFolderName(prefix);
            string cleanSuffix = SanitizeFolderName(suffix);

            if (!string.IsNullOrEmpty(cleanPrefix) && !cleanPrefix.EndsWith(" ") && !cleanPrefix.EndsWith("_") && !cleanPrefix.EndsWith("-"))
            {
                cleanPrefix += " ";
            }

            if (!string.IsNullOrEmpty(cleanSuffix) && !cleanSuffix.StartsWith(" ") && !cleanSuffix.StartsWith("_") && !cleanSuffix.StartsWith("-"))
            {
                cleanSuffix = " " + cleanSuffix;
            }

            return $"{cleanPrefix}{category}{cleanSuffix}".Trim();
        }
    }
}
