using System.Drawing;
using FileOrganizer.Models;

namespace FileOrganizer.Config
{
    /// <summary>
    /// Single Source of Truth (SSoT) for application metadata, default preferences, and UI constants.
    /// 
    /// FRAMEWORK / MANIFEST GUIDANCE:
    /// - To update the build-time assembly name or version, also update <AssemblyName> and <Version> 
    ///   in FileOrganizer.csproj.
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

        // Known Windows System Files and Protected Directories
        public static readonly HashSet<string> KnownSystemFilesAndDirs = new(System.StringComparer.OrdinalIgnoreCase)
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
        public static readonly Color ColorPrimary = Color.FromArgb(37, 99, 235);        // Blue 600
        public static readonly Color ColorPrimaryHover = Color.FromArgb(29, 78, 216);   // Blue 700
        public static readonly Color ColorSuccess = Color.FromArgb(16, 185, 129);       // Emerald 500
        public static readonly Color ColorDanger = Color.FromArgb(220, 38, 38);         // Red 600
        public static readonly Color ColorDangerBg = Color.FromArgb(254, 242, 242);     // Red 50
        public static readonly Color ColorSurfaceBg = Color.FromArgb(245, 247, 250);    // Light gray canvas
        public static readonly Color ColorTextDark = Color.FromArgb(31, 41, 55);        // Gray 800
        public static readonly Color ColorTextMuted = Color.FromArgb(75, 85, 99);       // Gray 600
        public static readonly Color ColorBorder = Color.FromArgb(229, 231, 235);       // Gray 200
    }
}
