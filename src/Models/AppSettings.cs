namespace FileOrganizer.Models
{
    public class AppSettings
    {
        public bool IncludeTopLevelFolders { get; set; } = true;
        public bool ShowDetailedProgress { get; set; } = true;
        public bool ShowOnTop { get; set; } = false;
        public FolderFormat FolderFormat { get; set; } = FolderFormat.MonthYear;
    }
    
    public enum FolderFormat
    {
        MonthYear,  // "January 2024"
        YearMonth   // "2024 January"
    }
}

