namespace FileOrganizer.Models
{
    public class AppSettings
    {
        public bool IncludeTopLevelFolders { get; set; } = Config.AppConstants.DefaultIncludeTopLevelFolders;
        public bool ShowDetailedProgress { get; set; } = Config.AppConstants.DefaultShowDetailedProgress;
        public bool ShowOnTop { get; set; } = Config.AppConstants.DefaultShowOnTop;
        public bool GenerateCsvLog { get; set; } = Config.AppConstants.DefaultGenerateCsvLog;
        public FolderFormat FolderFormat { get; set; } = Config.AppConstants.DefaultFolderFormat;
    }
    
    public enum FolderFormat
    {
        MonthYear,  // "January 2024"
        YearMonth   // "2024 January"
    }
}

