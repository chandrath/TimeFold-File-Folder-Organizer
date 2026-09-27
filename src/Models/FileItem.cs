using System;
using System.IO;

namespace FileOrganizer.Models
{
    public class FileItem
    {
        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TargetFolder { get; set; } = string.Empty;
        public string MonthYear { get => TargetFolder; set => TargetFolder = value; }
        public DateTime ModifiedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsCreatedDateActive { get; set; }
        public DateTime? MediaDateTaken { get; set; }
        public bool IsMediaDateActive { get; set; }
        public long Size { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsGitRepository { get; set; }
        public string DestinationPath { get; set; } = string.Empty;
        public bool WasRenamed { get; set; }
        public string OriginalName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
        public bool IsExcludedByRule { get; set; }

        public string Extension => Path.GetExtension(Name);
        public string TypeDisplay => IsDirectory ? (IsExcludedByRule ? "📁 Folder (Ignored)" : (IsGitRepository ? "📁 Git Repo (Folder)" : "📁 Folder")) : (string.IsNullOrEmpty(Path.GetExtension(Name)) ? "File" : Path.GetExtension(Name).TrimStart('.').ToUpperInvariant());
    }
}
