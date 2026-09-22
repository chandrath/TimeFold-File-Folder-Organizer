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
            string folderSuffix)
        {
            DateTime itemDate = item.IsCreatedDateActive ? item.CreatedDate : item.ModifiedDate;
            string dateFolder = AppConstants.FormatFolderDate(itemDate, folderFormat, folderPrefix, folderSuffix);

            switch (mode)
            {
                case OrganizationMode.Date:
                    return dateFolder;

                case OrganizationMode.Category:
                    if (item.IsDirectory) return "Folders";
                    return FileTypeService.Instance.GetCategory(item.Extension);

                case OrganizationMode.Extension:
                    if (item.IsDirectory) return "Folders";
                    string ext = item.Extension.TrimStart('.').ToUpperInvariant();
                    return string.IsNullOrWhiteSpace(ext) ? "No Extension" : ext;

                case OrganizationMode.CategoryAndDate:
                    string cat = item.IsDirectory ? "Folders" : FileTypeService.Instance.GetCategory(item.Extension);
                    return Path.Combine(cat, dateFolder);

                case OrganizationMode.DateAndCategory:
                    string cat2 = item.IsDirectory ? "Folders" : FileTypeService.Instance.GetCategory(item.Extension);
                    return Path.Combine(dateFolder, cat2);

                default:
                    return dateFolder;
            }
        }

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
                if (item.IsDirectory)
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
