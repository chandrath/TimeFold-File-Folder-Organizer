using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using FileOrganizer.Models;

namespace FileOrganizer.Config
{
    /// <summary>
    /// Single Source of Truth (SSoT) for application metadata, default preferences, and UI constants.
    /// </summary>
    public static class AppConstants
    {
        // Application Metadata (SSoT)
        public const string AppName = "TimeFold: File & Folder Organizer";
        public const string ShortAppName = "TimeFold";
        public const string AppVersion = "1.0.0.0";
        public const string BuildNumber = "20260920";
        public const string AppTagline = "Effortlessly organize files & folders into a clean date-based timeline";
        public const string AppDescription = AppTagline;
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
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ShortAppName);

        public static string GetConfigFilePath() =>
            Path.Combine(GetConfigDirectoryPath(), $"{ShortAppName}_V{AppVersion}_Config.json");

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

        // Output & Logging Prefixes (SSoT)
        public const string SortedFolderPrefix = "Sorted_";
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
        public const int MaxRecentFolders = 5;
        public const FolderFormat DefaultFolderFormat = FolderFormat.YearMonth;
        public const string DefaultFolderPrefix = "";
        public const string DefaultFolderSuffix = "";

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

        public static string SanitizeFolderName(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new System.Text.StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (Array.IndexOf(invalidChars, c) < 0)
                    sanitized.Append(c);
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
    }
}
