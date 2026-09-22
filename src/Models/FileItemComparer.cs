using System;
using System.Collections;
using System.Windows.Forms;

namespace FileOrganizer.Models
{
    public class FileItemComparer : IComparer
    {
        private readonly int _column;
        private readonly bool _ascending;

        public static void Sort(System.Collections.Generic.List<FileItem> list, int column, bool ascending)
        {
            if (list == null || list.Count <= 1) return;

            Comparison<FileItem> comparison = column switch
            {
                0 => (x, y) => string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase),
                1 => CompareType,
                2 => (x, y) => DateTime.Compare(x.ModifiedDate, y.ModifiedDate),
                3 => (x, y) => DateTime.Compare(x.CreatedDate, y.CreatedDate),
                4 => (x, y) => string.Compare(x.TargetFolder, y.TargetFolder, StringComparison.CurrentCultureIgnoreCase),
                5 => (x, y) => string.Compare(x.TargetFolder, y.TargetFolder, StringComparison.CurrentCultureIgnoreCase),
                6 => CompareSize,
                _ => (x, y) => DateTime.Compare(x.ModifiedDate, y.ModifiedDate)
            };

            if (ascending) list.Sort(comparison);
            else list.Sort((x, y) => comparison(y, x));
        }

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
                2 => DateTime.Compare(f1.ModifiedDate, f2.ModifiedDate),
                3 => DateTime.Compare(f1.CreatedDate, f2.CreatedDate),
                4 => string.Compare(f1.TargetFolder, f2.TargetFolder, StringComparison.CurrentCultureIgnoreCase),
                5 => string.Compare(f1.TargetFolder, f2.TargetFolder, StringComparison.CurrentCultureIgnoreCase),
                6 => CompareSize(f1, f2),
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
