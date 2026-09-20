using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer.Services
{
    public class FileOrganizerService
    {
        private readonly string _workingDirectory;
        private readonly string _executablePath;
        private string _outputDirectory;
        private FolderFormat _folderFormat = AppConstants.DefaultFolderFormat;

        public FileOrganizerService(string executablePath, string? workingDirectory = null, string? outputDirectory = null)
        {
            _executablePath = executablePath;
            _workingDirectory = workingDirectory ?? Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory;
            _outputDirectory = outputDirectory ?? _workingDirectory;
        }

        public string WorkingDirectory => _workingDirectory;
        public string OutputDirectory
        {
            get => _outputDirectory;
            set => _outputDirectory = value;
        }

        public List<FileItem> ScanFiles(bool includeTopLevelFolders, bool ignoreSystemFiles = true)
        {
            var files = new List<FileItem>();
            var executableName = Path.GetFileName(_executablePath);
            string normalizedOutput = Path.GetFullPath(_outputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            try
            {
                var items = Directory.GetFileSystemEntries(_workingDirectory, "*", SearchOption.TopDirectoryOnly);

                foreach (var itemPath in items)
                {
                    try
                    {
                        var itemName = Path.GetFileName(itemPath);

                        // Exclude executable itself
                        if (itemName.Equals(executableName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Exclude application's own CSV audit logs (preserve all user .csv data files)
                        if (itemName.StartsWith(AppConstants.CsvLogPrefix, StringComparison.OrdinalIgnoreCase) &&
                            Path.GetExtension(itemPath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Exclude application's own Sorted output folders
                        if (itemName.StartsWith(AppConstants.SortedFolderPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Exclude output directory itself if inside working directory
                        string normalizedItem = Path.GetFullPath(itemPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (string.Equals(normalizedItem, normalizedOutput, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Ignore known Windows system files and protected directories
                        if (ignoreSystemFiles)
                        {
                            if (AppConstants.KnownSystemFilesAndDirs.Contains(itemName))
                                continue;

                            try
                            {
                                var attributes = File.GetAttributes(itemPath);
                                if ((attributes & FileAttributes.System) != 0)
                                    continue;
                            }
                            catch
                            {
                                // If attributes cannot be read due to lock/permissions, skip
                                continue;
                            }
                        }

                        bool isDirectory = Directory.Exists(itemPath);

                        // Skip directories if not including top-level folders
                        if (isDirectory && !includeTopLevelFolders)
                            continue;

                        if (isDirectory)
                        {
                            var dirInfo = new DirectoryInfo(itemPath);
                            var fileItem = new FileItem
                            {
                                FullPath = itemPath,
                                Name = dirInfo.Name,
                                IsDirectory = true,
                                ModifiedDate = dirInfo.LastWriteTime,
                                Size = 0
                            };
                            fileItem.MonthYear = FormatMonthYear(fileItem.ModifiedDate);
                            files.Add(fileItem);
                        }
                        else if (File.Exists(itemPath))
                        {
                            var fileInfo = new FileInfo(itemPath);
                            var fileItem = new FileItem
                            {
                                FullPath = itemPath,
                                Name = fileInfo.Name,
                                IsDirectory = false,
                                ModifiedDate = fileInfo.LastWriteTime,
                                Size = fileInfo.Length
                            };
                            fileItem.MonthYear = FormatMonthYear(fileItem.ModifiedDate);
                            files.Add(fileItem);
                        }
                    }
                    catch
                    {
                        // Skip items that cannot be accessed due to permissions or lock
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error scanning files: {ex.Message}", ex);
            }

            return files.OrderBy(f => f.ModifiedDate).ToList();
        }

        public Dictionary<string, List<FileItem>> GroupByMonthYear(List<FileItem> files)
        {
            return files.GroupBy(f => f.MonthYear)
                        .ToDictionary(g => g.Key, g => g.ToList());
        }

        public List<string> DetectConflicts(Dictionary<string, List<FileItem>> grouped)
        {
            var conflicts = new List<string>();

            foreach (var group in grouped.Values)
            {
                var nameGroups = group.GroupBy(f => f.Name);
                foreach (var nameGroup in nameGroups)
                {
                    if (nameGroup.Count() > 1)
                    {
                        conflicts.Add($"{nameGroup.Key} (appears {nameGroup.Count()} times in {group.First().MonthYear})");
                    }
                }
            }

            return conflicts;
        }

        public async Task<OrganizationResult> OrganizeFilesAsync(
            List<FileItem> files,
            IProgress<(int current, int total, string currentFile)> progress,
            CancellationToken cancellationToken,
            bool generateCsvLog = true)
        {
            var result = new OrganizationResult
            {
                TotalFiles = files.Count
            };

            if (files.Count == 0)
                return result;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var sortedFolder = Path.Combine(_outputDirectory, $"{AppConstants.SortedFolderPrefix}{timestamp}");

            var processedFiles = new List<FileItem>();
            int currentIndex = 0;

            try
            {
                Directory.CreateDirectory(sortedFolder);
                var grouped = GroupByMonthYear(files);

                foreach (var group in grouped)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var monthFolder = Path.Combine(sortedFolder, group.Key);
                    bool monthFolderCreated = false;

                    foreach (var file in group.Value)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            if (!monthFolderCreated)
                            {
                                Directory.CreateDirectory(monthFolder);
                                monthFolderCreated = true;
                                result.MonthFoldersCreated++;
                            }

                            var destinationPath = Path.Combine(monthFolder, file.Name);

                            // Handle name conflicts
                            if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                            {
                                destinationPath = GetSafePath(monthFolder, file.Name);
                                file.WasRenamed = true;
                                file.OriginalName = file.Name;
                                file.Name = Path.GetFileName(destinationPath);
                                result.ConflictsResolved++;
                            }

                            // Move file or directory safely
                            if (file.IsDirectory)
                            {
                                MoveDirectorySafely(file.FullPath, destinationPath);
                            }
                            else
                            {
                                File.Move(file.FullPath, destinationPath);
                            }

                            file.DestinationPath = destinationPath;
                            file.ErrorMessage = string.Empty;
                            processedFiles.Add(file);
                            result.FilesMoved++;

                            currentIndex++;
                            progress?.Report((currentIndex, result.TotalFiles, file.Name));
                        }
                        catch (Exception ex)
                        {
                            result.Errors++;
                            result.ErrorMessages.Add($"{file.Name}: {ex.Message}");
                            file.ErrorMessage = ex.Message;
                            file.DestinationPath = string.Empty;
                            processedFiles.Add(file);

                            currentIndex++;
                            progress?.Report((currentIndex, result.TotalFiles, file.Name));
                        }

                        // Small delay to keep UI responsive
                        await Task.Delay(10, cancellationToken);
                    }
                }

                result.SortedFolderPath = sortedFolder;

                if (cancellationToken.IsCancellationRequested)
                {
                    if (generateCsvLog && processedFiles.Count > 0)
                    {
                        var csvCancelPath = Path.Combine(_outputDirectory, $"{AppConstants.CsvLogPrefix}{timestamp}.csv");
                        CsvLogger.WriteLog(csvCancelPath, processedFiles, result);
                        result.CsvLogPath = csvCancelPath;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Create CSV log in output directory (if enabled)
                if (generateCsvLog)
                {
                    var csvPath = Path.Combine(_outputDirectory, $"{AppConstants.CsvLogPrefix}{timestamp}.csv");
                    CsvLogger.WriteLog(csvPath, processedFiles, result);
                    result.CsvLogPath = csvPath;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"Critical error: {ex.Message}");
                throw;
            }

            return result;
        }

        private static void MoveDirectorySafely(string sourceDir, string destDir)
        {
            string sourceRoot = Path.GetPathRoot(Path.GetFullPath(sourceDir)) ?? "";
            string destRoot = Path.GetPathRoot(Path.GetFullPath(destDir)) ?? "";

            if (string.Equals(sourceRoot, destRoot, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Move(sourceDir, destDir);
            }
            else
            {
                CopyDirectoryRecursively(sourceDir, destDir);
                Directory.Delete(sourceDir, true);
            }
        }

        private static void CopyDirectoryRecursively(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                CopyDirectoryRecursively(dir, destSubDir);
            }
        }

        private static string GetSafePath(string directory, string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var counter = 1;

            string newPath;
            do
            {
                var newFileName = $"{nameWithoutExt}_({counter}){extension}";
                newPath = Path.Combine(directory, newFileName);
                counter++;
            } while (File.Exists(newPath) || Directory.Exists(newPath));

            return newPath;
        }

        public FolderFormat FolderFormat
        {
            get => _folderFormat;
            set => _folderFormat = value;
        }

        private string FormatMonthYear(DateTime date)
        {
            return _folderFormat == FolderFormat.MonthYear
                ? date.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture)
                : date.ToString("yyyy MMMM", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
