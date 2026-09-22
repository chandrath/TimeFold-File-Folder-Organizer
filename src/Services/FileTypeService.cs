using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FileOrganizer.Config;
using FileOrganizer.Models;

namespace FileOrganizer.Services
{
    /// <summary>
    /// Service managing factory file type definitions, user overrides (deltas), and fast O(1) category resolution.
    /// </summary>
    public class FileTypeService
    {
        private static FileTypeService? _instance;
        public static FileTypeService Instance => _instance ??= new FileTypeService();

        public const string FallbackCategory = "Other";

        // Factory Built-in Categories (SSoT)
        public static readonly IReadOnlyDictionary<string, string[]> FactoryCategories = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Images"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico", ".tiff", ".tif", ".psd", ".ai", ".raw", ".cr2", ".nef", ".heic", ".avif", ".eps" },
            ["Videos"] = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp", ".ts", ".m2ts" },
            ["Audio"] = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".alac", ".opus", ".mid", ".midi" },
            ["Documents"] = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".csv", ".odt", ".ods", ".odp", ".epub", ".md", ".log" },
            ["Archives"] = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".dmg", ".cab" },
            ["Executables"] = new[] { ".exe", ".msi", ".bat", ".cmd", ".ps1", ".vbs", ".apk", ".appx", ".wsf" },
            ["Code & Web"] = new[] { ".cs", ".js", ".ts", ".html", ".htm", ".css", ".scss", ".json", ".xml", ".yaml", ".yml", ".sql", ".py", ".java", ".cpp", ".c", ".h", ".php", ".rb", ".go", ".rs", ".sh" },
            ["Fonts"] = new[] { ".ttf", ".otf", ".woff", ".woff2", ".eot" }
        };

        private UserTypeDelta _delta = new();
        private Dictionary<string, string> _runtimeLookup = new(StringComparer.OrdinalIgnoreCase);

        public UserTypeDelta Delta => _delta;

        public FileTypeService()
        {
            LoadDelta();
            RebuildLookupTable();
        }

        public string GetCategory(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return FallbackCategory;
            string cleanExt = extension.StartsWith('.') ? extension : "." + extension;
            if (_runtimeLookup.TryGetValue(cleanExt, out var category))
            {
                return category;
            }
            return FallbackCategory;
        }

        public void RebuildLookupTable()
        {
            var table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1. Load factory defaults (unless disabled by user)
            foreach (var kvp in FactoryCategories)
            {
                string catName = kvp.Key;
                foreach (string ext in kvp.Value)
                {
                    if (!_delta.DisabledFactoryExtensions.Contains(ext))
                    {
                        table[ext] = catName;
                    }
                }
            }

            // 2. Apply user custom categories
            foreach (var kvp in _delta.CustomCategories)
            {
                string catName = kvp.Key;
                foreach (string ext in kvp.Value)
                {
                    string cleanExt = ext.StartsWith('.') ? ext : "." + ext;
                    table[cleanExt] = catName;
                }
            }

            // 3. Apply individual extension overrides
            foreach (var kvp in _delta.ExtensionCategoryOverrides)
            {
                string cleanExt = kvp.Key.StartsWith('.') ? kvp.Key : "." + kvp.Key;
                table[cleanExt] = kvp.Value;
            }

            _runtimeLookup = table;
        }

        public void LoadDelta()
        {
            try
            {
                string path = AppConstants.GetCustomTypesFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var delta = JsonSerializer.Deserialize<UserTypeDelta>(json);
                    if (delta != null)
                    {
                        _delta = delta;
                        return;
                    }
                }
            }
            catch { }
            _delta = new UserTypeDelta();
        }

        public void SaveDelta()
        {
            try
            {
                string dir = AppConstants.GetConfigDirectoryPath();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = AppConstants.GetCustomTypesFilePath();
                string tempPath = path + ".tmp";
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_delta, options);
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, path, overwrite: true);
            }
            catch { }
        }

        public void ResetToFactoryDefaults()
        {
            _delta = new UserTypeDelta();
            SaveDelta();
            RebuildLookupTable();
        }

        public void ExportRules(string destinationPath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_delta, options);
            File.WriteAllText(destinationPath, json);
        }

        public void ImportRules(string sourcePath)
        {
            string json = File.ReadAllText(sourcePath);
            var delta = JsonSerializer.Deserialize<UserTypeDelta>(json);
            if (delta != null)
            {
                _delta = delta;
                SaveDelta();
                RebuildLookupTable();
            }
        }
    }
}
