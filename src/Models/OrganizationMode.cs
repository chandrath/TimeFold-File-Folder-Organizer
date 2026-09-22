namespace FileOrganizer.Models
{
    /// <summary>
    /// Supported organization strategies for arranging files and folders.
    /// </summary>
    public enum OrganizationMode
    {
        /// <summary>
        /// Organize by file/folder date timeline (e.g. 2026-09).
        /// </summary>
        Date = 0,

        /// <summary>
        /// Organize by smart category (e.g. Images, Documents, Audio, Video, Archives).
        /// </summary>
        Category = 1,

        /// <summary>
        /// Organize directly by file extension (e.g. JPG, PDF, ZIP).
        /// </summary>
        Extension = 2,

        /// <summary>
        /// Hybrid mode: Category primary, Date secondary (e.g. Images\2026-09).
        /// </summary>
        CategoryAndDate = 3,

        /// <summary>
        /// Hybrid mode: Date primary, Category secondary (e.g. 2026-09\Images).
        /// </summary>
        DateAndCategory = 4
    }
}
