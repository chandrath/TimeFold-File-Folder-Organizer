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
        private string _folderPrefix = AppConstants.DefaultFolderPrefix;
        private string _folderSuffix = AppConstants.DefaultFolderSuffix;
        private string _categoryPrefix = AppConstants.DefaultCategoryPrefix;
        private string _categorySuffix = AppConstants.DefaultCategorySuffix;
        private bool _use24HourTimestamp = AppConstants.DefaultUse24HourTimestamp;
        private OrganizationMode _organizationMode = OrganizationMode.Date;
        private bool _keepHtmlCompanionsTogether = true;
        private bool _keepSubtitleCompanionsTogether = true;
        private bool _createSortedSubfolder = AppConstants.DefaultCreateSortedSubfolder;
        public bool CreateSortedSubfolder { get => _createSortedSubfolder; set => _createSortedSubfolder = value; }

        public FileOrganizerService(string executablePath, string? workingDirectory = null, string? outputDirectory = null)
        {
            _executablePath = executablePath;
            _workingDirectory = workingDirectory ?? Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory;
            _outputDirectory = outputDirectory ?? _workingDirectory;
        }

        public string WorkingDirectory => _workingDirectory;
        public string OutputDirectory { get => _outputDirectory; set => _outputDirectory = value; }

        public List<FileItem> ScanFiles(bool includeTopLevelFolders, bool ignoreSystemFiles = true, DateSource fileDateSource = DateSource.Modified, DateSource folderDateSource = DateSource.Modified)
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
                        if ((itemName.StartsWith(AppConstants.CsvLogPrefix, StringComparison.OrdinalIgnoreCase) ||
                             itemName.StartsWith(AppConstants.LegacyCsvLogPrefix, StringComparison.OrdinalIgnoreCase)) &&
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
                            var (itemDate, isCreatedActive) = ResolveItemDate(dirInfo.LastWriteTime, dirInfo.CreationTime, folderDateSource);
                            var fileItem = new FileItem
                            {
                                FullPath = itemPath,
                                Name = dirInfo.Name,
                                IsDirectory = true,
                                ModifiedDate = dirInfo.LastWriteTime,
                                CreatedDate = dirInfo.CreationTime,
                                IsCreatedDateActive = isCreatedActive,
                                Size = 0
                            };
                            fileItem.TargetFolder = TargetFolderResolver.Resolve(fileItem, _organizationMode, _folderFormat, _folderPrefix, _folderSuffix, _categoryPrefix, _categorySuffix);
                            files.Add(fileItem);
                        }
                        else if (File.Exists(itemPath))
                        {
                            var fileInfo = new FileInfo(itemPath);
                            var (itemDate, isCreatedActive) = ResolveItemDate(fileInfo.LastWriteTime, fileInfo.CreationTime, fileDateSource);
                            var fileItem = new FileItem
                            {
                                FullPath = itemPath,
                                Name = fileInfo.Name,
                                IsDirectory = false,
                                ModifiedDate = fileInfo.LastWriteTime,
                                CreatedDate = fileInfo.CreationTime,
                                IsCreatedDateActive = isCreatedActive,
                                Size = fileInfo.Length
                            };
                            fileItem.TargetFolder = TargetFolderResolver.Resolve(fileItem, _organizationMode, _folderFormat, _folderPrefix, _folderSuffix, _categoryPrefix, _categorySuffix);
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

            if (_keepHtmlCompanionsTogether)
            {
                TargetFolderResolver.ApplyHtmlCompanionPairing(files);
            }

            if (_keepSubtitleCompanionsTogether)
            {
                TargetFolderResolver.ApplySubtitleCompanionPairing(files);
            }

            return files.OrderByDescending(f => f.ModifiedDate).ToList();
        }

        public Dictionary<string, List<FileItem>> GroupByMonthYear(List<FileItem> files)
        {
            return files.GroupBy(f => f.TargetFolder)
                        .ToDictionary(g => g.Key, g => g.ToList());
        }

        public List<string> DetectConflicts(Dictionary<string, List<FileItem>> grouped) => new();

        public async Task<OrganizationResult> OrganizeFilesAsync(
            List<FileItem> files,
            IProgress<(int current, int total, string currentFile)> progress,
            CancellationToken cancellationToken,
            bool generateCsvLog = true,
            ConflictResolutionStrategy conflictStrategy = ConflictResolutionStrategy.AutoRename,
            HashSet<string>? collidingFilePaths = null)
        {
            var result = new OrganizationResult { TotalFiles = files.Count };
            if (files.Count == 0) return result;

            var sortedFolder = _createSortedSubfolder
                ? AppConstants.GetSortedFolderName(_outputDirectory, _use24HourTimestamp)
                : _outputDirectory;
            string sortedFolderName = Path.GetFileName(sortedFolder);
            string logTimestamp = sortedFolderName.StartsWith(AppConstants.SortedFolderPrefix, StringComparison.OrdinalIgnoreCase)
                ? sortedFolderName.Substring(AppConstants.SortedFolderPrefix.Length)
                : DateTime.Now.ToString("yyyy-MM-dd_HHmmss");

            var processedFiles = new List<FileItem>();
            int currentIndex = 0;

            try
            {
                Directory.CreateDirectory(sortedFolder);
                var grouped = GroupByMonthYear(files);

                var preExistingDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (Directory.Exists(sortedFolder))
                {
                    try
                    {
                        foreach (var d in Directory.GetDirectories(sortedFolder))
                        {
                            string name = Path.GetFileName(d);
                            if (!name.StartsWith(AppConstants.SortedFolderPrefix, StringComparison.OrdinalIgnoreCase) &&
                                !AppConstants.KnownSystemFilesAndDirs.Contains(name))
                            {
                                preExistingDirs.Add(name);
                            }
                        }
                    }
                    catch { }
                }

                var remappedTopSegments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var group in grouped)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    string effectiveGroupKey = group.Key;
                    string topSegment = effectiveGroupKey.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];

                    if (preExistingDirs.Contains(topSegment) && conflictStrategy == ConflictResolutionStrategy.AutoRename)
                    {
                        if (!remappedTopSegments.TryGetValue(topSegment, out var safeTop))
                        {
                            safeTop = GetSafeDirectoryName(sortedFolder, topSegment);
                            remappedTopSegments[topSegment] = safeTop;
                        }
                        effectiveGroupKey = safeTop + effectiveGroupKey.Substring(topSegment.Length);
                    }

                    var monthFolder = Path.Combine(sortedFolder, effectiveGroupKey);
                    bool monthFolderCreated = false;

                    foreach (var file in group.Value.OrderByDescending(f => f.IsDirectory))
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        if (file.IsDirectory && (preExistingDirs.Contains(file.Name) || remappedTopSegments.ContainsKey(file.Name)))
                        {
                            file.DestinationPath = file.FullPath;
                            file.ErrorMessage = string.Empty;
                            processedFiles.Add(file);
                            currentIndex++;
                            progress?.Report((currentIndex, result.TotalFiles, $"{file.Name} (Preserved untouched)"));
                            continue;
                        }

                        if (conflictStrategy == ConflictResolutionStrategy.Skip && collidingFilePaths?.Contains(file.FullPath) == true)
                        {
                            file.DestinationPath = string.Empty;
                            file.ErrorMessage = "Skipped (Collision with existing destination item)";
                            processedFiles.Add(file);
                            currentIndex++;
                            progress?.Report((currentIndex, result.TotalFiles, $"{file.Name} (Skipped)"));
                            continue;
                        }

                        try
                        {
                            if (file.IsDirectory && string.Equals(file.Name, group.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.Equals(Path.GetFullPath(file.FullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                                  Path.GetFullPath(monthFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                                  StringComparison.OrdinalIgnoreCase))
                                {
                                    file.DestinationPath = monthFolder;
                                    file.ErrorMessage = string.Empty;
                                    processedFiles.Add(file);
                                    result.FilesMoved++;
                                    currentIndex++;
                                    progress?.Report((currentIndex, result.TotalFiles, file.Name));
                                    continue;
                                }

                                if (!monthFolderCreated && !Directory.Exists(monthFolder))
                                {
                                    MoveDirectorySafely(file.FullPath, monthFolder);
                                    monthFolderCreated = true;
                                    result.MonthFoldersCreated++;
                                    file.DestinationPath = monthFolder;
                                    file.ErrorMessage = string.Empty;
                                    processedFiles.Add(file);
                                    result.FilesMoved++;
                                    currentIndex++;
                                    progress?.Report((currentIndex, result.TotalFiles, file.Name));
                                    continue;
                                }
                            }

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
                        var csvCancelPath = Path.Combine(_outputDirectory, $"{AppConstants.CsvLogPrefix}{logTimestamp}.csv");
                        CsvLogger.WriteLog(csvCancelPath, processedFiles, result);
                        result.CsvLogPath = csvCancelPath;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Create CSV log in output directory (if enabled)
                if (generateCsvLog)
                {
                    var csvPath = Path.Combine(_outputDirectory, $"{AppConstants.CsvLogPrefix}{logTimestamp}.csv");
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
            if (string.Equals(Path.GetFullPath(sourceDir).TrimEnd('\\', '/'), Path.GetFullPath(destDir).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase)) return;
            string sourceRoot = Path.GetPathRoot(Path.GetFullPath(sourceDir)) ?? "";
            string destRoot = Path.GetPathRoot(Path.GetFullPath(destDir)) ?? "";
            if (string.Equals(sourceRoot, destRoot, StringComparison.OrdinalIgnoreCase)) Directory.Move(sourceDir, destDir);
            else { CopyDirectoryRecursively(sourceDir, destDir); Directory.Delete(sourceDir, true); }
        }

        private static void CopyDirectoryRecursively(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(sourceDir))
                CopyDirectoryRecursively(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }

        private static string GetSafeDirectoryName(string parentDir, string baseName)
        {
            int counter = 1;
            string candidate;
            do { candidate = $"{baseName} ({counter++})"; }
            while (Directory.Exists(Path.Combine(parentDir, candidate)));
            return candidate;
        }

        private static string GetSafePath(string directory, string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            int counter = 1;
            string newPath;
            do
            {
                newPath = Path.Combine(directory, $"{nameWithoutExt}_({counter++}){extension}");
            } while (File.Exists(newPath) || Directory.Exists(newPath));
            return newPath;
        }

        public FolderFormat FolderFormat
        {
            get => _folderFormat;
            set => _folderFormat = value;
        }

        public string FolderPrefix
        {
            get => _folderPrefix;
            set => _folderPrefix = value ?? string.Empty;
        }

        public string FolderSuffix
        {
            get => _folderSuffix;
            set => _folderSuffix = value ?? string.Empty;
        }

        public void ApplyNamingSettings(
            FolderFormat format,
            string prefix,
            string suffix,
            bool use24Hour = false,
            OrganizationMode mode = OrganizationMode.Date,
            bool keepHtmlCompanions = true,
            string categoryPrefix = "",
            string categorySuffix = "",
            bool keepSubtitleCompanions = true,
            bool createSortedSubfolder = true)
        {
            _folderFormat = format;
            _folderPrefix = prefix ?? string.Empty;
            _folderSuffix = suffix ?? string.Empty;
            _use24HourTimestamp = use24Hour;
            _organizationMode = mode;
            _keepHtmlCompanionsTogether = keepHtmlCompanions;
            _categoryPrefix = categoryPrefix ?? string.Empty;
            _categorySuffix = categorySuffix ?? string.Empty;
            _keepSubtitleCompanionsTogether = keepSubtitleCompanions;
            _createSortedSubfolder = createSortedSubfolder;
        }

        public string FormatTargetFolder(DateTime date)
        {
            return AppConstants.FormatFolderDate(date, _folderFormat, _folderPrefix, _folderSuffix);
        }

        public string FormatMonthYear(DateTime date) => FormatTargetFolder(date);

        private static (DateTime resolvedDate, bool isCreatedActive) ResolveItemDate(DateTime modified, DateTime created, DateSource source)
        {
            static bool IsValidDate(DateTime dt) => dt.Year >= 1980 && dt.Year <= DateTime.Now.Year + 10;

            bool validMod = IsValidDate(modified);
            bool validCre = IsValidDate(created);

            if (!validMod && !validCre) return (DateTime.Now, false);
            if (!validMod) return (created, true);
            if (!validCre) return (modified, false);

            return source switch
            {
                DateSource.Created => (created, true),
                DateSource.Earliest => (created < modified) ? (created, true) : (modified, false),
                _ => (modified, false)
            };
        }
    }
}
