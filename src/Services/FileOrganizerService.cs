using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileOrganizer.Models;

namespace FileOrganizer.Services
{
    public class FileOrganizerService
    {
        private readonly string _workingDirectory;
        private readonly string _executablePath;
        
        public FileOrganizerService(string executablePath)
        {
            _executablePath = executablePath;
            _workingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory;
        }
        
        public string WorkingDirectory => _workingDirectory;
        
        public List<FileItem> ScanFiles(bool includeTopLevelFolders)
        {
            var files = new List<FileItem>();
            var executableName = Path.GetFileName(_executablePath);
            
            try
            {
                var items = Directory.GetFileSystemEntries(_workingDirectory, "*", SearchOption.TopDirectoryOnly);
                
                foreach (var itemPath in items)
                {
                    try
                    {
                        // Exclude executable itself
                        if (Path.GetFileName(itemPath).Equals(executableName, StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        // Exclude CSV files
                        if (Path.GetExtension(itemPath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        // Exclude Sorted_ folders
                        var itemName = Path.GetFileName(itemPath);
                        if (itemName.StartsWith("Sorted_", StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        // Exclude Unsorted_ folders
                        if (itemName.StartsWith("Unsorted_", StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        var fileInfo = new FileInfo(itemPath);
                        var dirInfo = new DirectoryInfo(itemPath);
                        
                        bool isDirectory = Directory.Exists(itemPath);
                        
                        // Skip directories if not including top-level folders
                        if (isDirectory && !includeTopLevelFolders)
                            continue;
                        
                        // Only process files or top-level folders (not contents inside)
                        if (isDirectory)
                        {
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
                        // Skip items that can't be accessed
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
            CancellationToken cancellationToken)
        {
            var result = new OrganizationResult
            {
                TotalFiles = files.Count
            };
            
            if (files.Count == 0)
                return result;
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var sortedFolder = Path.Combine(_workingDirectory, $"Sorted_{timestamp}");
            
            try
            {
                // Create main sorted folder
                Directory.CreateDirectory(sortedFolder);
                
                // Group files by month-year
                var grouped = GroupByMonthYear(files);
                result.MonthFoldersCreated = grouped.Count;
                
                var processedFiles = new List<FileItem>();
                int currentIndex = 0;
                
                foreach (var group in grouped)
                {
                    var monthFolder = Path.Combine(sortedFolder, group.Key);
                    Directory.CreateDirectory(monthFolder);
                    
                    foreach (var file in group.Value)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        
                        try
                        {
                            var destinationPath = Path.Combine(monthFolder, file.Name);
                            
                            // Handle conflicts
                            if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                            {
                                destinationPath = GetSafePath(monthFolder, file.Name);
                                file.WasRenamed = true;
                                file.OriginalName = file.Name;
                                file.Name = Path.GetFileName(destinationPath);
                                result.ConflictsResolved++;
                            }
                            
                            // Move file or directory
                            if (file.IsDirectory)
                            {
                                Directory.Move(file.FullPath, destinationPath);
                            }
                            else
                            {
                                File.Move(file.FullPath, destinationPath);
                            }
                            
                            file.DestinationPath = destinationPath;
                            processedFiles.Add(file);
                            result.FilesMoved++;
                            
                            currentIndex++;
                            progress?.Report((currentIndex, result.TotalFiles, file.Name));
                        }
                        catch (Exception ex)
                        {
                            result.Errors++;
                            result.ErrorMessages.Add($"{file.Name}: {ex.Message}");
                            file.DestinationPath = "";
                            processedFiles.Add(file);
                            
                            currentIndex++;
                            progress?.Report((currentIndex, result.TotalFiles, file.Name));
                        }
                        
                        // Small delay to keep UI responsive
                        await Task.Delay(10, cancellationToken);
                    }
                }
                
                result.SortedFolderPath = sortedFolder;
                
                // Create CSV log
                var csvPath = Path.Combine(_workingDirectory, $"OrganizationLog_{timestamp}.csv");
                CsvLogger.WriteLog(csvPath, processedFiles, result);
                result.CsvLogPath = csvPath;
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"Critical error: {ex.Message}");
                throw;
            }
            
            return result;
        }
        
        private string GetSafePath(string directory, string fileName)
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
        
        private string FormatMonthYear(DateTime date)
        {
            return date.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

