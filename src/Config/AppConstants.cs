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
        // Application Metadata
        public const string AppName = "File Organizer by Date";
        public const string AppVersion = "1.0.0";
        public const string AppDescription = "A Windows application that automatically organizes files into month-year folders based on their Modified Date.";
        public const string Author = "chandrath";
        public const string RepositoryUrl = "https://github.com/chandrath/OrganizeFiles-ByDate";
        public const string CopyrightText = "© 2024-2026 - Free and Open Source Software";

        // Output & Logging Prefixes (SSoT)
        public const string SortedFolderPrefix = "Sorted_";
        public const string CsvLogPrefix = "OrganizationLog_";

        // Default User Preferences
        public const bool DefaultIncludeTopLevelFolders = true;
        public const bool DefaultIgnoreSystemFiles = true;
        public const bool DefaultShowDetailedProgress = true;
        public const bool DefaultShowOnTop = false;
        public const bool DefaultGenerateCsvLog = true;
        public const FolderFormat DefaultFolderFormat = FolderFormat.YearMonth;
        public const string DefaultFolderPrefix = "";
        public const string DefaultFolderSuffix = "";

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

        // UI Theme & Metrics
        public const int DefaultPadding = 12;
        public static readonly Color ColorPrimary = Color.FromArgb(37, 99, 235);
        public static readonly Color ColorPrimaryHover = Color.FromArgb(29, 78, 216);
        public static readonly Color ColorSuccess = Color.FromArgb(16, 185, 129);
        public static readonly Color ColorDanger = Color.FromArgb(220, 38, 38);
        public static readonly Color ColorDangerBg = Color.FromArgb(254, 242, 242);
        public static readonly Color ColorSurfaceBg = Color.FromArgb(245, 247, 250);
        public static readonly Color ColorTextDark = Color.FromArgb(31, 41, 55);
        public static readonly Color ColorTextMuted = Color.FromArgb(75, 85, 99);
        public static readonly Color ColorBorder = Color.FromArgb(229, 231, 235);

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
