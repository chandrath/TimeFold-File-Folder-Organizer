using System;
using System.IO;
using System.Text.Json;

namespace FileOrganizer.Models
{
    public class AppSettings
    {
        public bool IncludeTopLevelFolders { get; set; } = Config.AppConstants.DefaultIncludeTopLevelFolders;
        public bool IgnoreSystemFiles { get; set; } = Config.AppConstants.DefaultIgnoreSystemFiles;
        public bool ShowDetailedProgress { get; set; } = Config.AppConstants.DefaultShowDetailedProgress;
        public bool ShowOnTop { get; set; } = Config.AppConstants.DefaultShowOnTop;
        public bool GenerateCsvLog { get; set; } = Config.AppConstants.DefaultGenerateCsvLog;
        public bool Use24HourTimestamp { get; set; } = Config.AppConstants.DefaultUse24HourTimestamp;
        public bool AutoLoadExeDirectoryOnStartup { get; set; } = Config.AppConstants.DefaultAutoLoadExeDirectoryOnStartup;
        public bool DarkMode { get; set; } = Config.AppConstants.DefaultDarkMode;
        public bool HasSeenWelcomeTour { get; set; } = Config.AppConstants.DefaultHasSeenWelcomeTour;
        public System.Collections.Generic.List<string> RecentFolders { get; set; } = new();
        public FolderFormat FolderFormat { get; set; } = Config.AppConstants.DefaultFolderFormat;
        public string FolderPrefix { get; set; } = Config.AppConstants.DefaultFolderPrefix;
        public string FolderSuffix { get; set; } = Config.AppConstants.DefaultFolderSuffix;
        public DateSource FileDateSource { get; set; } = Config.AppConstants.DefaultFileDateSource;
        public DateSource FolderDateSource { get; set; } = Config.AppConstants.DefaultFolderDateSource;
        public int SchemaVersion { get; set; } = 1;
        public OrganizationMode OrgMode { get; set; } = OrganizationMode.Date;
        public string CategoryPrefix { get; set; } = Config.AppConstants.DefaultCategoryPrefix;
        public string CategorySuffix { get; set; } = Config.AppConstants.DefaultCategorySuffix;
        public bool KeepHtmlCompanionsTogether { get; set; } = Config.AppConstants.DefaultKeepHtmlCompanionsTogether;
        public bool KeepSubtitleCompanionsTogether { get; set; } = Config.AppConstants.DefaultKeepSubtitleCompanionsTogether;
        public int MaxPreviewItems { get; set; } = Config.AppConstants.DefaultMaxPreviewItems;
        public bool CreateSortedSubfolder { get; set; } = Config.AppConstants.DefaultCreateSortedSubfolder;
        public bool UseSourceAsOutput { get; set; } = Config.AppConstants.DefaultUseSourceAsOutput;

        public static AppSettings LoadFromFile()
        {
            try
            {
                string path = Config.AppConstants.GetConfigFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) return settings;
                }

                // Check for legacy configuration in %APPDATA%\TimeFold\
                string legacyDir = Config.AppConstants.GetLegacyConfigDirectoryPath();
                if (Directory.Exists(legacyDir))
                {
                    var legacyFiles = Directory.GetFiles(legacyDir, "*_Config.json");
                    if (legacyFiles.Length > 0)
                    {
                        Array.Sort(legacyFiles, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
                        string legacyJson = File.ReadAllText(legacyFiles[0]);
                        var migrated = JsonSerializer.Deserialize<AppSettings>(legacyJson);
                        if (migrated != null)
                        {
                            migrated.SaveToFile();
                            return migrated;
                        }
                    }
                }
            }
            catch
            {
                // Fail-safe: fallback to defaults if config file is invalid or missing
            }
            return new AppSettings();
        }

        public void SaveToFile()
        {
            try
            {
                string dir = Config.AppConstants.GetConfigDirectoryPath();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Config.AppConstants.GetConfigFilePath();
                string tempPath = path + ".tmp";
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, path, overwrite: true);
            }
            catch
            {
                // Fail-safe: ignore disk write errors to prevent application crash
            }
        }

        public void AddRecentFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return;
            try
            {
                string fullPath = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                RecentFolders.RemoveAll(f => string.Equals(f.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), fullPath, StringComparison.OrdinalIgnoreCase));
                RecentFolders.Insert(0, fullPath);
                if (RecentFolders.Count > Config.AppConstants.MaxRecentFolders)
                {
                    RecentFolders.RemoveRange(Config.AppConstants.MaxRecentFolders, RecentFolders.Count - Config.AppConstants.MaxRecentFolders);
                }
                SaveToFile();
            }
            catch { }
        }

        public void RemoveRecentFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return;
            try
            {
                string fullPath = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                int removed = RecentFolders.RemoveAll(f => string.Equals(f.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), fullPath, StringComparison.OrdinalIgnoreCase));
                if (removed > 0) SaveToFile();
            }
            catch { }
        }

        public void ClearRecentFolders()
        {
            RecentFolders.Clear();
            SaveToFile();
        }
    }
    
    public enum FolderFormat
    {
        YearMonth,           // "2026 January" (Default)
        MonthYear,           // "January 2026"
        YearShortMonth,      // "2026 Jan"
        ShortMonthYear,      // "Jan 2026"
        IsoMonth,            // "2026-01"
        MonthIso,            // "01-2026" (Flipped ISO Month)
        IsoDate,             // "2026-01-15" (Daily)
        YearMonthDay,        // "2026 January 15"
        DayMonthYear,        // "15 January 2026"
        YearShortMonthDay,   // "2026 Jan 15"
        DayShortMonthYear,   // "15 Jan 2026"
        YearQuarter,            // "2026 Q1"
        QuarterYear,            // "Q1 2026"
        YearQuarterMonths,      // "2026 Q1 (January, February & March)"
        YearQuarterShortMonths, // "2026 Q1 (Jan, Feb & Mar)"
        YearHalf,               // "2026 H1"
        HalfYear,               // "H1 2026"
        YearOnly,               // "2026"
        // Year-nested (2-level): top = year, sub = date sub-folder
        YearWithMonth,          // "2026" / "2026 January"
        YearWithShortMonth,     // "2026" / "2026 Jan"
        YearWithMonthFlipped,       // "2026" / "January 2026"
        YearWithShortMonthFlipped,  // "2026" / "Jan 2026"
        YearWithMonthOnly,      // "2026" / "January"  (month name only, no year in subfolder)
        YearWithShortMonthOnly, // "2026" / "Jan"
        YearWithIsoMonth,       // "2026" / "2026-01"
        YearWithIsoMonthFlipped,    // "2026" / "01-2026"
        YearWithQuarter,        // "2026" / "2026 Q1"
        YearWithQuarterFlipped, // "2026" / "Q1 2026"
        YearWithHalf,           // "2026" / "2026 H1"
        YearWithHalfFlipped     // "2026" / "H1 2026"
    }

    public enum DateSource
    {
        Modified, // Date Modified (Default)
        Created,  // Date Created
        Earliest  // Earliest / Oldest Date
    }
}
