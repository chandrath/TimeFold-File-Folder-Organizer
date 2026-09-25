using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer.Services
{
    public static class UndoService
    {
        private static UndoSession? _currentSession;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static bool IsSessionExpired(UndoSession? session)
        {
            if (session == null) return true;
            return (DateTime.Now - session.Timestamp).TotalDays > AppConstants.UndoSessionMaxAgeDays;
        }

        public static bool HasActiveSession()
        {
            if (_currentSession != null && _currentSession.MovedItems.Count > 0)
            {
                if (IsSessionExpired(_currentSession))
                {
                    ClearSession();
                    return false;
                }
                return true;
            }

            try
            {
                string path = AppConstants.GetUndoManifestFilePath();
                if (File.Exists(path))
                {
                    var loaded = LoadSession();
                    return loaded != null && loaded.MovedItems.Count > 0;
                }
            }
            catch { }

            return false;
        }

        public static UndoSession? LoadSession()
        {
            if (_currentSession != null)
            {
                if (IsSessionExpired(_currentSession))
                {
                    ClearSession();
                    return null;
                }
                return _currentSession;
            }

            try
            {
                string path = AppConstants.GetUndoManifestFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    _currentSession = JsonSerializer.Deserialize<UndoSession>(json, JsonOptions);
                    if (_currentSession != null && IsSessionExpired(_currentSession))
                    {
                        ClearSession();
                        return null;
                    }
                    return _currentSession;
                }
            }
            catch { }

            return null;
        }

        public static void SaveSession(UndoSession session)
        {
            _currentSession = session;
            try
            {
                string dir = AppConstants.GetConfigDirectoryPath();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string path = AppConstants.GetUndoManifestFilePath();
                string json = JsonSerializer.Serialize(session, JsonOptions);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        public static void ClearSession()
        {
            _currentSession = null;
            try
            {
                string path = AppConstants.GetUndoManifestFilePath();
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        public static UndoSession RecordSession(
            string sourceDirectory,
            string outputDirectory,
            string sortedFolderPath,
            IEnumerable<FileItem> processedFiles,
            IEnumerable<string>? createdFolders = null,
            string? csvLogPath = null)
        {
            var movedItems = new List<UndoEntry>();
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (createdFolders != null)
            {
                foreach (var f in createdFolders)
                {
                    if (!string.IsNullOrWhiteSpace(f)) folders.Add(f);
                }
            }

            if (!string.IsNullOrWhiteSpace(sortedFolderPath))
            {
                folders.Add(sortedFolderPath);
            }

            foreach (var f in processedFiles)
            {
                if (!string.IsNullOrEmpty(f.DestinationPath) &&
                    string.IsNullOrEmpty(f.ErrorMessage) &&
                    !string.Equals(Path.GetFullPath(f.FullPath).TrimEnd('\\', '/'),
                                   Path.GetFullPath(f.DestinationPath).TrimEnd('\\', '/'),
                                   StringComparison.OrdinalIgnoreCase))
                {
                    movedItems.Add(new UndoEntry
                    {
                        OriginalPath = f.FullPath,
                        DestinationPath = f.DestinationPath,
                        IsDirectory = f.IsDirectory,
                        WasRenamed = f.WasRenamed
                    });

                    // Track destination parent directory for safe pruning if empty
                    string? parent = Path.GetDirectoryName(f.DestinationPath);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        folders.Add(parent);
                    }
                }
            }

            var session = new UndoSession
            {
                SourceDirectory = sourceDirectory,
                OutputDirectory = outputDirectory,
                SortedFolderPath = sortedFolderPath,
                CsvLogPath = csvLogPath ?? string.Empty,
                MovedItems = movedItems,
                CreatedFolders = folders.ToList()
            };

            SaveSession(session);
            return session;
        }

        public static UndoPreflightReport PreflightCheck(UndoSession session, string? customRestoreDir = null)
        {
            var report = new UndoPreflightReport { TotalItems = session.MovedItems.Count };

            foreach (var item in session.MovedItems)
            {
                bool destExists = item.IsDirectory
                    ? Directory.Exists(item.DestinationPath)
                    : File.Exists(item.DestinationPath);

                if (!destExists)
                {
                    report.MissingAtDestination++;
                    report.Warnings.Add($"Missing: {Path.GetFileName(item.DestinationPath)} (not found at destination)");
                    continue;
                }

                report.ReadyToRestore++;

                string checkTarget = item.OriginalPath;
                if (!string.IsNullOrEmpty(customRestoreDir))
                {
                    string rel = Path.GetRelativePath(session.SourceDirectory, item.OriginalPath);
                    checkTarget = Path.Combine(customRestoreDir, rel);
                }

                if (string.Equals(Path.GetFullPath(item.DestinationPath).TrimEnd('\\', '/'),
                                  Path.GetFullPath(checkTarget).TrimEnd('\\', '/'),
                                  StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool origExists = item.IsDirectory
                    ? Directory.Exists(checkTarget)
                    : File.Exists(checkTarget);

                if (origExists)
                {
                    report.OriginalPathCollisions++;
                    string safeName = Path.GetFileName(GetSafeRestorePath(checkTarget));
                    report.Warnings.Add($"Collision: '{Path.GetFileName(checkTarget)}' already exists. Will restore safely as '{safeName}'.");
                }
            }

            return report;
        }

        public static async Task<UndoExecutionResult> UndoAsync(
            UndoSession session,
            IProgress<(int current, int total, string currentFile)>? progress,
            CancellationToken cancellationToken,
            string? customRestoreDir = null)
        {
            var result = new UndoExecutionResult();
            var movedBack = new List<UndoEntry>();
            int total = session.MovedItems.Count;
            int current = 0;

            // Step 1: Move items back in reverse order (LIFO)
            for (int i = session.MovedItems.Count - 1; i >= 0; i--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = session.MovedItems[i];
                current++;

                bool destExists = item.IsDirectory
                    ? Directory.Exists(item.DestinationPath)
                    : File.Exists(item.DestinationPath);

                if (!destExists)
                {
                    result.FailureCount++;
                    result.ErrorMessages.Add($"Item not found at destination: {item.DestinationPath}");
                    progress?.Report((current, total, $"{Path.GetFileName(item.DestinationPath)} (Skipped: Not Found)"));
                    continue;
                }

                try
                {
                    string targetPath = item.OriginalPath;
                    if (!string.IsNullOrEmpty(customRestoreDir))
                    {
                        string rel = Path.GetRelativePath(session.SourceDirectory, item.OriginalPath);
                        targetPath = Path.Combine(customRestoreDir, rel);
                    }

                    if (string.Equals(Path.GetFullPath(item.DestinationPath).TrimEnd('\\', '/'),
                                      Path.GetFullPath(targetPath).TrimEnd('\\', '/'),
                                      StringComparison.OrdinalIgnoreCase))
                    {
                        result.RestoredCount++;
                        continue;
                    }

                    bool origOccupied = item.IsDirectory ? Directory.Exists(targetPath) : File.Exists(targetPath);
                    if (origOccupied)
                    {
                        targetPath = GetSafeRestorePath(targetPath);
                        result.CollisionRenamedCount++;
                    }

                    string? parentDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    if (item.IsDirectory)
                    {
                        MoveDirectorySafely(item.DestinationPath, targetPath);
                    }
                    else
                    {
                        File.Move(item.DestinationPath, targetPath);
                    }

                    result.RestoredCount++;
                    movedBack.Add(item);
                    progress?.Report((current, total, Path.GetFileName(targetPath)));
                    await Task.Delay(5, cancellationToken);
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.ErrorMessages.Add($"{Path.GetFileName(item.DestinationPath)}: {ex.Message}");
                    progress?.Report((current, total, $"{Path.GetFileName(item.DestinationPath)} (Error: {ex.Message})"));
                }
            }

            // Step 2: Clean up empty folders (prune deepest folders first)
            var candidateFolders = new HashSet<string>(session.CreatedFolders, StringComparer.OrdinalIgnoreCase);
            foreach (var item in movedBack)
            {
                string? p = Path.GetDirectoryName(item.DestinationPath);
                while (!string.IsNullOrEmpty(p) && !string.Equals(p, session.OutputDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    candidateFolders.Add(p);
                    p = Path.GetDirectoryName(p);
                }
            }

            var sortedCandidates = candidateFolders
                .OrderByDescending(f => f.Length)
                .ThenByDescending(f => f)
                .ToList();

            foreach (var folder in sortedCandidates)
            {
                try
                {
                    if (Directory.Exists(folder))
                    {
                        if (Directory.GetFileSystemEntries(folder).Length == 0)
                        {
                            Directory.Delete(folder, false);
                            result.PrunedFoldersCount++;
                        }
                    }
                }
                catch { }
            }

            // Step 3: Remove the specific generated CSV log file if all items were restored
            if (result.RestoredCount > 0 && result.FailureCount == 0)
            {
                if (!string.IsNullOrEmpty(session.CsvLogPath) && File.Exists(session.CsvLogPath))
                {
                    try { File.Delete(session.CsvLogPath); } catch { }
                }

                // If sorted folder was holding only the log and is now empty, prune it
                if (!string.IsNullOrEmpty(session.SortedFolderPath) && Directory.Exists(session.SortedFolderPath))
                {
                    try
                    {
                        if (Directory.GetFileSystemEntries(session.SortedFolderPath).Length == 0)
                        {
                            Directory.Delete(session.SortedFolderPath, false);
                            result.PrunedFoldersCount++;
                        }
                    }
                    catch { }
                }

                ClearSession();
            }

            return result;
        }

        private static string GetSafeRestorePath(string originalPath)
        {
            string dir = Path.GetDirectoryName(originalPath) ?? "";
            string nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
            string ext = Path.GetExtension(originalPath);

            string candidate = Path.Combine(dir, $"{nameWithoutExt} (Restored){ext}");
            int counter = 1;
            while (File.Exists(candidate) || Directory.Exists(candidate))
            {
                candidate = Path.Combine(dir, $"{nameWithoutExt} (Restored {counter++}){ext}");
            }
            return candidate;
        }

        private static void MoveDirectorySafely(string sourceDir, string destDir)
        {
            try
            {
                Directory.Move(sourceDir, destDir);
            }
            catch (IOException)
            {
                CopyDirectoryRecursive(sourceDir, destDir);
                Directory.Delete(sourceDir, true);
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                CopyDirectoryRecursive(subDir, destSubDir);
            }
        }
    }
}
