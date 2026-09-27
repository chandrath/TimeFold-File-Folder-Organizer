using System;
using System.Collections.Generic;
using System.IO;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer.Services
{
    /// <summary>
    /// Pure resolver for determining target folders based on OrganizationMode (Date, Category, Extension, Hybrid).
    /// Houses the HTML Companion Pairing logic to ensure web assets remain intact.
    /// </summary>
    public static class TargetFolderResolver
    {
        public static string Resolve(
            FileItem item,
            OrganizationMode mode,
            FolderFormat folderFormat,
            string folderPrefix,
            string folderSuffix,
            string categoryPrefix = "",
            string categorySuffix = "",
            bool groupGitRepositories = true)
        {
            DateTime itemDate = (item.IsMediaDateActive && item.MediaDateTaken.HasValue)
                ? item.MediaDateTaken.Value
                : (item.IsCreatedDateActive ? item.CreatedDate : item.ModifiedDate);
            string dateFolder = AppConstants.FormatFolderDate(itemDate, folderFormat, folderPrefix, folderSuffix);

            string GetFormattedCategory()
            {
                string baseCat = item.IsDirectory
                    ? FileTypeService.Instance.GetFolderCategory(item.IsGitRepository && groupGitRepositories)
                    : FileTypeService.Instance.GetCategory(item.Extension);
                return AppConstants.FormatCategoryFolder(baseCat, categoryPrefix, categorySuffix);
            }

            switch (mode)
            {
                case OrganizationMode.Date:
                    return dateFolder;

                case OrganizationMode.Category:
                    return GetFormattedCategory();

                case OrganizationMode.Extension:
                    if (item.IsDirectory)
                    {
                        return FileTypeService.Instance.GetFolderCategory(item.IsGitRepository && groupGitRepositories);
                    }
                    string ext = item.Extension.TrimStart('.').ToUpperInvariant();
                    return string.IsNullOrWhiteSpace(ext) ? "No Extension" : ext;

                case OrganizationMode.CategoryAndDate:
                    return Path.Combine(GetFormattedCategory(), dateFolder);

                case OrganizationMode.DateAndCategory:
                    return Path.Combine(dateFolder, GetFormattedCategory());

                default:
                    return dateFolder;
            }
        }

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".flv", ".m4v", ".ts", ".m2ts", ".3gp"
        };

        private static readonly HashSet<string> SubtitleExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".srt", ".vtt", ".sub", ".ass", ".ssa", ".idx", ".smi"
        };

        public static void ApplySubtitleCompanionPairing(List<FileItem> items, bool packageIntoDedicatedFolder = false)
        {
            if (items == null || items.Count == 0) return;

            var videoMap = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (!item.IsDirectory && VideoExtensions.Contains(item.Extension))
                {
                    string baseName = Path.GetFileNameWithoutExtension(item.Name);
                    videoMap[baseName] = item;
                }
            }

            if (videoMap.Count == 0) return;

            var packagedVideos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (!item.IsDirectory && SubtitleExtensions.Contains(item.Extension))
                {
                    string subBase = Path.GetFileNameWithoutExtension(item.Name);
                    FileItem? parentVideo = null;
                    string matchKey = "";

                    if (videoMap.TryGetValue(subBase, out parentVideo))
                    {
                        matchKey = subBase;
                    }
                    else if (subBase.Contains('.'))
                    {
                        string candidate = Path.GetFileNameWithoutExtension(subBase);
                        if (videoMap.TryGetValue(candidate, out parentVideo))
                        {
                            matchKey = candidate;
                        }
                    }

                    if (parentVideo != null)
                    {
                        if (packageIntoDedicatedFolder)
                        {
                            if (packagedVideos.Add(matchKey))
                            {
                                parentVideo.TargetFolder = Path.Combine(parentVideo.TargetFolder, matchKey);
                            }
                            item.TargetFolder = parentVideo.TargetFolder;
                        }
                        else
                        {
                            item.TargetFolder = parentVideo.TargetFolder;
                        }
                    }
                }
            }
        }

        public static void ApplySubtitleCompanionPairing(List<FileItem> items, bool packageIntoDedicatedFolder, bool isCategoryMode) =>
            ApplySubtitleCompanionPairing(items, packageIntoDedicatedFolder);

        public static void ApplyHtmlCompanionPairing(List<FileItem> items)
        {
            if (items == null || items.Count == 0) return;

            var htmlFiles = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (!item.IsDirectory)
                {
                    string ext = item.Extension;
                    if (string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ext, ".htm", StringComparison.OrdinalIgnoreCase))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(item.Name);
                        htmlFiles[baseName] = item;
                    }
                }
            }

            if (htmlFiles.Count == 0) return;

            foreach (var item in items)
            {
                if (item.IsDirectory && !item.IsExcludedByRule)
                {
                    string dirName = item.Name;
                    string? candidateBase = null;

                    if (dirName.EndsWith("_files", StringComparison.OrdinalIgnoreCase))
                        candidateBase = dirName.Substring(0, dirName.Length - 6);
                    else if (dirName.EndsWith(" files", StringComparison.OrdinalIgnoreCase))
                        candidateBase = dirName.Substring(0, dirName.Length - 6);
                    else if (dirName.EndsWith("_data", StringComparison.OrdinalIgnoreCase))
                        candidateBase = dirName.Substring(0, dirName.Length - 5);

                    if (!string.IsNullOrEmpty(candidateBase) && htmlFiles.TryGetValue(candidateBase, out var parentHtml))
                    {
                        item.TargetFolder = parentHtml.TargetFolder;
                    }
                }
            }
        }
    }
}
