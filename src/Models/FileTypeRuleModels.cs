using System;
using System.Collections.Generic;

namespace FileOrganizer.Models
{
    /// <summary>
    /// User custom overrides for file type classification, saved in custom_types.json.
    /// Only records the delta from built-in factory defaults to keep config minimal and upgradable.
    /// </summary>
    public class UserTypeDelta
    {
        public int SchemaVersion { get; set; } = 1;

        /// <summary>
        /// Extensions from the factory list that the user has disabled (unmapped).
        /// </summary>
        public HashSet<string> DisabledFactoryExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Custom re-mappings for specific extensions to target categories (e.g. ".psd" -> "Design").
        /// </summary>
        public Dictionary<string, string> ExtensionCategoryOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// User-defined custom categories and their associated extensions.
        /// </summary>
        public Dictionary<string, List<string>> CustomCategories { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// User renames for category names (e.g. "Office Files" -> "My Office Files").
        /// </summary>
        public Dictionary<string, string> CategoryRenames { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Information model representing a file type category and its member extensions for UI presentation.
    /// </summary>
    public class CategoryInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "📁";
        public List<string> Extensions { get; set; } = new();
        public bool IsCustom { get; set; }
    }
}
