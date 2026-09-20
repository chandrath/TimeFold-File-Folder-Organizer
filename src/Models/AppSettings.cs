namespace FileOrganizer.Models
{
    public class AppSettings
    {
        public bool IncludeTopLevelFolders { get; set; } = Config.AppConstants.DefaultIncludeTopLevelFolders;
        public bool IgnoreSystemFiles { get; set; } = Config.AppConstants.DefaultIgnoreSystemFiles;
        public bool ShowDetailedProgress { get; set; } = Config.AppConstants.DefaultShowDetailedProgress;
        public bool ShowOnTop { get; set; } = Config.AppConstants.DefaultShowOnTop;
        public bool GenerateCsvLog { get; set; } = Config.AppConstants.DefaultGenerateCsvLog;
        public FolderFormat FolderFormat { get; set; } = Config.AppConstants.DefaultFolderFormat;
        public string FolderPrefix { get; set; } = Config.AppConstants.DefaultFolderPrefix;
        public string FolderSuffix { get; set; } = Config.AppConstants.DefaultFolderSuffix;
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
        YearQuarter,         // "2026 Q1"
        QuarterYear,         // "Q1 2026"
        YearHalf,            // "2026 H1"
        HalfYear,            // "H1 2026"
        YearOnly             // "2026"
    }
}
