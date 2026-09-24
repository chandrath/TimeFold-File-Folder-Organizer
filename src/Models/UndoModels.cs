using System;
using System.Collections.Generic;

namespace FileOrganizer.Models
{
    public class UndoEntry
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public bool WasRenamed { get; set; }
    }

    public class UndoSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SourceDirectory { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public string SortedFolderPath { get; set; } = string.Empty;
        public string CsvLogPath { get; set; } = string.Empty;
        public List<UndoEntry> MovedItems { get; set; } = new();
        public List<string> CreatedFolders { get; set; } = new();
    }

    public class UndoPreflightReport
    {
        public int TotalItems { get; set; }
        public int ReadyToRestore { get; set; }
        public int MissingAtDestination { get; set; }
        public int OriginalPathCollisions { get; set; }
        public List<string> Warnings { get; set; } = new();
        public bool CanProceed => ReadyToRestore > 0;
    }

    public class UndoExecutionResult
    {
        public int RestoredCount { get; set; }
        public int CollisionRenamedCount { get; set; }
        public int FailureCount { get; set; }
        public int PrunedFoldersCount { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
    }
}
