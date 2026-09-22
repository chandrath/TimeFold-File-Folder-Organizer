using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer.Services
{
    public static class ConflictDetector
    {
        public static List<ConflictInfo> Detect(
            List<FileItem> files,
            Dictionary<string, List<FileItem>> grouped,
            string baseOutputDir,
            bool createSortedSubfolder,
            bool use24HourTimestamp)
        {
            var conflicts = new List<ConflictInfo>();
            if (files.Count == 0 || string.IsNullOrWhiteSpace(baseOutputDir))
                return conflicts;

            string targetRoot = createSortedSubfolder
                ? AppConstants.GetSortedFolderPreviewPath(baseOutputDir, use24HourTimestamp)
                : baseOutputDir;

            var existingPreRunDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(targetRoot))
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(targetRoot))
                    {
                        string dirName = Path.GetFileName(dir);
                        if (!dirName.StartsWith(AppConstants.SortedFolderPrefix, StringComparison.OrdinalIgnoreCase) &&
                            !AppConstants.KnownSystemFilesAndDirs.Contains(dirName))
                        {
                            existingPreRunDirs.Add(dirName);
                        }
                    }
                }
                catch { }
            }

            var safeTopMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string GetSafeTop(string seg)
            {
                if (!safeTopMap.TryGetValue(seg, out var s))
                {
                    int counter = 1;
                    do { s = $"{seg} ({counter++})"; }
                    while (Directory.Exists(Path.Combine(targetRoot, s)));
                    safeTopMap[seg] = s;
                }
                return s;
            }

            foreach (var group in grouped)
            {
                string groupKey = group.Key;
                string groupTargetDir = Path.Combine(targetRoot, groupKey);
                string topSegment = groupKey.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];

                // 1. Target Folder Collision: Pre-existing user folder matches the target category/date
                if (existingPreRunDirs.Contains(topSegment))
                {
                    string safeTop = GetSafeTop(topSegment);
                    string proposedTarget = safeTop + groupKey.Substring(topSegment.Length);
                    foreach (var item in group.Value)
                    {
                        conflicts.Add(new ConflictInfo
                        {
                            Item = item,
                            TargetFolder = groupKey,
                            DestinationPath = groupTargetDir,
                            Type = ConflictType.TargetFolderMatchesExisting,
                            Description = $"Output folder '{topSegment}' already exists",
                            ProposedAction = $"Save to '{proposedTarget}' (old '{topSegment}' untouched)"
                        });
                    }
                }

                // 2. Self-target directory collisions (e.g. folder '2024' into target '2024')
                foreach (var item in group.Value.Where(f => f.IsDirectory))
                {
                    if (string.Equals(item.Name, groupKey, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!conflicts.Any(c => c.Item == item))
                        {
                            conflicts.Add(new ConflictInfo
                            {
                                Item = item,
                                TargetFolder = groupKey,
                                DestinationPath = groupTargetDir,
                                Type = ConflictType.TargetFolderMatchesSelf,
                                Description = $"Folder '{item.Name}' matches target name '{groupKey}'.",
                                ProposedAction = "Keep in-place (already organized)"
                            });
                        }
                    }
                }

                // 3. Intra-batch duplicates (multiple files/folders with the same name in the same target folder)
                var nameGroups = group.Value.GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase);
                foreach (var nameGroup in nameGroups)
                {
                    var items = nameGroup.ToList();
                    if (items.Count > 1)
                    {
                        for (int i = 1; i < items.Count; i++)
                        {
                            var item = items[i];
                            if (!conflicts.Any(c => c.Item == item && c.Type == ConflictType.BatchDuplicate))
                            {
                                conflicts.Add(new ConflictInfo
                                {
                                    Item = item,
                                    TargetFolder = groupKey,
                                    DestinationPath = Path.Combine(groupTargetDir, item.Name),
                                    Type = ConflictType.BatchDuplicate,
                                    Description = $"Duplicate item name '{item.Name}' in target '{groupKey}' ({items.Count} items).",
                                    ProposedAction = "Auto-rename with suffix (e.g. _(1))"
                                });
                            }
                        }
                    }
                }

                // 4. Existing destination file collisions
                if (Directory.Exists(groupTargetDir))
                {
                    foreach (var item in group.Value)
                    {
                        if (item.IsDirectory && string.Equals(item.Name, groupKey, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string expectedPath = Path.Combine(groupTargetDir, item.Name);
                        if (!string.Equals(item.FullPath, expectedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            if (File.Exists(expectedPath) || Directory.Exists(expectedPath))
                            {
                                if (!conflicts.Any(c => c.Item == item))
                                {
                                    conflicts.Add(new ConflictInfo
                                    {
                                        Item = item,
                                        TargetFolder = groupKey,
                                        DestinationPath = expectedPath,
                                        Type = ConflictType.DestinationAlreadyExists,
                                        Description = $"Target already contains an item named '{item.Name}'.",
                                        ProposedAction = "Auto-rename with suffix (e.g. _(1))"
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return conflicts;
        }
    }
}
