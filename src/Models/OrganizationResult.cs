using System.Collections.Generic;

namespace FileOrganizer.Models
{
    public class OrganizationResult
    {
        public int TotalFiles { get; set; }
        public int FilesMoved { get; set; }
        public int FilesSkipped { get; set; }
        public int Errors { get; set; }
        public int ConflictsResolved { get; set; }
        public int MonthFoldersCreated { get; set; }
        public string SortedFolderPath { get; set; } = string.Empty;
        public string CsvLogPath { get; set; } = string.Empty;
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}

