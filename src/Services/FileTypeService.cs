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
            ["Office Files"] = new[] { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".odp", ".rtf", ".dot", ".dotx", ".xlt", ".xltx", ".pot", ".potx" },
            ["PDF Files"] = new[] { ".pdf" },
            ["Reader Files"] = new[] { ".epub", ".mobi", ".azw", ".azw3", ".djvu", ".xps", ".oxps", ".cbr", ".cbz" },
            ["Text & Notes"] = new[] { ".txt", ".md", ".markdown", ".log", ".rst", ".tex" },
            ["JSON Files"] = new[] { ".json", ".jsonc", ".jsonl", ".geojson" },
            ["Data & Config Files"] = new[] { ".xml", ".yaml", ".yml", ".toml", ".ini", ".env", ".properties", ".config", ".plist", ".csv", ".tsv" },
            ["Image Files"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".jxl", ".bmp", ".ico", ".tiff", ".tif", ".raw", ".cr2", ".cr3", ".nef", ".arw", ".dng", ".heic", ".heif" },
            ["Photoshop Files"] = new[] { ".psd", ".psb" },
            ["Vector Files"] = new[] { ".svg", ".ai", ".eps", ".fig", ".sketch", ".xd" },
            ["Publishing Files"] = new[] { ".indd", ".idml", ".indt", ".pub", ".afpub" },
            ["3D Files"] = new[] { ".blend", ".c4d", ".gltf", ".glb", ".obj", ".fbx", ".3mf", ".stl", ".ply", ".usdz", ".usdc", ".dae", ".3ds", ".step", ".stp" },
            ["CAD Drawings"] = new[] { ".dwg", ".dxf", ".dwf" },
            ["Game Dev Files"] = new[] { ".c3p", ".c2proj", ".godot", ".tscn", ".tres", ".unity", ".unitypackage", ".prefab", ".asset", ".meta", ".uproject", ".umap", ".uasset", ".yyp", ".yy", ".gmx" },
            ["Code Files"] = new[] { ".cs", ".cpp", ".c", ".h", ".hpp", ".rs", ".go", ".java", ".py", ".ts", ".tsx", ".js", ".jsx", ".vue", ".svelte", ".html", ".css", ".scss", ".less", ".php", ".rb", ".swift", ".kt", ".dart", ".lua", ".sql", ".r", ".m", ".asm" },
            ["Script Files"] = new[] { ".bat", ".cmd", ".ps1", ".psm1", ".vbs", ".wsf", ".sh", ".bash", ".zsh" },
            ["Video Files"] = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".flv", ".m4v", ".ts", ".m2ts", ".3gp" },
            ["Audio Files"] = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".alac", ".opus", ".mid", ".midi" },
            ["Zip & Archives"] = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".cab", ".tgz" },
            ["App Installers"] = new[] { ".exe", ".msi", ".msix", ".appx", ".dmg", ".pkg", ".appimage", ".flatpak", ".deb", ".rpm", ".snap", ".apk", ".aab", ".xapk", ".ipa", ".ipk" },
            ["Font Files"] = new[] { ".ttf", ".otf", ".woff", ".woff2", ".eot" }
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

            // 4. Apply category renames
            if (_delta.CategoryRenames.Count > 0)
            {
                var keys = new List<string>(table.Keys);
                foreach (var ext in keys)
                {
                    string currentCat = table[ext];
                    if (_delta.CategoryRenames.TryGetValue(currentCat, out var renamedCat) && !string.IsNullOrWhiteSpace(renamedCat))
                    {
                        table[ext] = renamedCat;
                    }
                }
            }

            _runtimeLookup = table;
        }

        public List<string> GetAllCategories()
        {
            var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in FactoryCategories.Keys)
            {
                if (_delta.CategoryRenames.TryGetValue(k, out var renamed) && !string.IsNullOrWhiteSpace(renamed))
                    set.Add(renamed);
                else
                    set.Add(k);
            }
            foreach (var k in _delta.CustomCategories.Keys)
            {
                if (_delta.CategoryRenames.TryGetValue(k, out var renamed) && !string.IsNullOrWhiteSpace(renamed))
                    set.Add(renamed);
                else
                    set.Add(k);
            }
            foreach (var v in _delta.ExtensionCategoryOverrides.Values) set.Add(v);
            return new List<string>(set);
        }

        public void RenameCategory(string currentName, string newName)
        {
            if (string.IsNullOrWhiteSpace(currentName) || string.IsNullOrWhiteSpace(newName)) return;
            string cleanNew = newName.Trim();
            if (string.Equals(currentName.Trim(), cleanNew, StringComparison.OrdinalIgnoreCase)) return;

            // Find canonical key if already renamed
            string canonical = currentName.Trim();
            foreach (var kvp in _delta.CategoryRenames)
            {
                if (string.Equals(kvp.Value, currentName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    canonical = kvp.Key;
                    break;
                }
            }

            _delta.CategoryRenames[canonical] = cleanNew;
            RebuildLookupTable();
            SaveDelta();
        }

        public bool ResetCategoryName(string currentName)
        {
            if (string.IsNullOrWhiteSpace(currentName)) return false;
            string target = currentName.Trim();
            string? foundKey = null;

            foreach (var kvp in _delta.CategoryRenames)
            {
                if (string.Equals(kvp.Key, target, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Value, target, StringComparison.OrdinalIgnoreCase))
                {
                    foundKey = kvp.Key;
                    break;
                }
            }

            if (foundKey != null)
            {
                _delta.CategoryRenames.Remove(foundKey);
                RebuildLookupTable();
                SaveDelta();
                return true;
            }
            return false;
        }

        public bool IsCategoryRenamed(string currentName, out string originalName)
        {
            originalName = currentName;
            if (string.IsNullOrWhiteSpace(currentName)) return false;
            string target = currentName.Trim();

            foreach (var kvp in _delta.CategoryRenames)
            {
                if (string.Equals(kvp.Value, target, StringComparison.OrdinalIgnoreCase))
                {
                    originalName = kvp.Key;
                    return true;
                }
            }
            return false;
        }

        public void SetCategoryOverride(string extension, string targetCategory)
        {
            if (string.IsNullOrWhiteSpace(extension) || string.IsNullOrWhiteSpace(targetCategory)) return;
            string cleanExt = extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
            _delta.ExtensionCategoryOverrides[cleanExt] = targetCategory.Trim();
            _delta.DisabledFactoryExtensions.Remove(cleanExt);
            RebuildLookupTable();
            SaveDelta();
        }

        public void RemoveCategoryOverride(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return;
            string cleanExt = extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
            _delta.ExtensionCategoryOverrides.Remove(cleanExt);
            RebuildLookupTable();
            SaveDelta();
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
