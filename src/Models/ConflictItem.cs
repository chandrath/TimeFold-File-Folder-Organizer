using System;

namespace FileOrganizer.Models
{
    public enum ConflictType
    {
        TargetFolderMatchesExisting,
        TargetFolderMatchesSelf,
        BatchDuplicate,
        DestinationAlreadyExists
    }

    public enum ConflictResolutionStrategy
    {
        AutoRename,
        Skip
    }

    public class ConflictInfo
    {
        public FileItem Item { get; set; } = null!;
        public string TargetFolder { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public ConflictType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ProposedAction { get; set; } = string.Empty;

        public string ShortStatus => Type switch
        {
            ConflictType.TargetFolderMatchesExisting => "⚠ Folder Exists",
            ConflictType.TargetFolderMatchesSelf => "⚠ Folder Exists",
            ConflictType.BatchDuplicate => "⚠ Duplicate",
            ConflictType.DestinationAlreadyExists => "⚠ File Exists",
            _ => "⚠ Conflict"
        };
    }
}
