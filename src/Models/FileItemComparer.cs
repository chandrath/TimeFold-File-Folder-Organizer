using System;
using System.Collections;
using System.Windows.Forms;

namespace FileOrganizer.Models
{
    public class FileItemComparer : IComparer
    {
        private readonly int _column;
        private readonly bool _ascending;

        public FileItemComparer(int column, bool ascending)
        {
            _column = column;
            _ascending = ascending;
        }

        public int Compare(object? x, object? y)
        {
            if (x is not ListViewItem itemX || y is not ListViewItem itemY) return 0;
            if (itemX.Tag is not FileItem f1 || itemY.Tag is not FileItem f2) return 0;

            int result = _column switch
            {
                0 => string.Compare(f1.Name, f2.Name, StringComparison.CurrentCultureIgnoreCase),
                1 => CompareType(f1, f2),
                2 => string.Compare(f1.TargetFolder, f2.TargetFolder, StringComparison.CurrentCultureIgnoreCase),
                3 => DateTime.Compare(f1.ModifiedDate, f2.ModifiedDate),
                4 => CompareSize(f1, f2),
                _ => 0
            };

            return _ascending ? result : -result;
        }

        private static int CompareType(FileItem f1, FileItem f2)
        {
            if (f1.IsDirectory != f2.IsDirectory)
                return f1.IsDirectory ? -1 : 1;
            return string.Compare(f1.TypeDisplay, f2.TypeDisplay, StringComparison.CurrentCultureIgnoreCase);
        }

        private static int CompareSize(FileItem f1, FileItem f2)
        {
            if (f1.IsDirectory && f2.IsDirectory) return string.Compare(f1.Name, f2.Name, StringComparison.CurrentCultureIgnoreCase);
            if (f1.IsDirectory) return -1;
            if (f2.IsDirectory) return 1;
            return f1.Size.CompareTo(f2.Size);
        }
    }
}
