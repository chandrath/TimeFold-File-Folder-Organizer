using System;
using System.IO;

namespace FileOrganizer.Models
{
    public class FileItem
    {
        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MonthYear { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
        public long Size { get; set; }
        public bool IsDirectory { get; set; }
        public string DestinationPath { get; set; } = string.Empty;
        public bool WasRenamed { get; set; }
        public string OriginalName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public string TypeDisplay => IsDirectory ? "Folder" : (string.IsNullOrEmpty(Path.GetExtension(Name)) ? "File" : Path.GetExtension(Name).TrimStart('.').ToUpperInvariant());
    }
}

