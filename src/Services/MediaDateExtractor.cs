using System;
using System.Buffers.Binary;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace FileOrganizer.Services
{
    public static class MediaDateExtractor
    {
        private static readonly System.Collections.Generic.HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".heic", ".tif", ".tiff", ".webp"
        };

        private static readonly System.Collections.Generic.HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mov", ".m4v"
        };

        public static bool IsSupportedMedia(string extension) =>
            ImageExtensions.Contains(extension) || VideoExtensions.Contains(extension);

        public static DateTime? TryGetDateTaken(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                string ext = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(ext)) return null;

                if (VideoExtensions.Contains(ext))
                    return TryGetVideoCreationDate(filePath);

                if (ImageExtensions.Contains(ext))
                    return TryGetImageDateTaken(filePath);
            }
            catch { }

            return null;
        }

        private static DateTime? TryGetImageDateTaken(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);

                // EXIF Property Tags:
                // 0x9003 = PropertyTagExifDTOrig (Date Time Original / Date Taken)
                // 0x9004 = PropertyTagExifDTDigitized
                // 0x0132 = PropertyTagDateTime
                int[] tags = { 0x9003, 0x9004, 0x0132 };
                foreach (int tag in tags)
                {
                    try
                    {
                        var prop = image.GetPropertyItem(tag);
                        if (prop?.Value != null && prop.Value.Length >= 19)
                        {
                            string raw = Encoding.ASCII.GetString(prop.Value).Trim('\0', ' ', '\r', '\n');
                            if (DateTime.TryParseExact(raw, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                            {
                                if (dt.Year >= 1980 && dt.Year <= DateTime.Now.Year + 1)
                                    return dt;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }

        private static DateTime? TryGetVideoCreationDate(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new BinaryReader(fs);

                byte[] buffer = new byte[8];
                long length = fs.Length;

                while (fs.Position + 8 <= length)
                {
                    if (fs.Read(buffer, 0, 8) < 8) break;
                    uint size = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(0, 4));
                    string type = Encoding.ASCII.GetString(buffer, 4, 4);

                    long boxDataSize = size >= 8 ? size - 8 : (size == 1 ? (reader.ReadInt64() - 16) : 0);
                    if (boxDataSize < 0) break;

                    if (type == "moov")
                    {
                        long moovEnd = fs.Position + boxDataSize;
                        while (fs.Position + 8 <= moovEnd)
                        {
                            if (fs.Read(buffer, 0, 8) < 8) break;
                            uint childSize = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(0, 4));
                            string childType = Encoding.ASCII.GetString(buffer, 4, 4);

                            if (childType == "mvhd")
                            {
                                int version = fs.ReadByte();
                                if (version < 0) break;
                                fs.Seek(3, SeekOrigin.Current); // skip flags

                                ulong creationSeconds;
                                if (version == 1)
                                {
                                    byte[] timeBuf = new byte[8];
                                    if (fs.Read(timeBuf, 0, 8) < 8) break;
                                    creationSeconds = BinaryPrimitives.ReadUInt64BigEndian(timeBuf);
                                }
                                else
                                {
                                    byte[] timeBuf = new byte[4];
                                    if (fs.Read(timeBuf, 0, 4) < 4) break;
                                    creationSeconds = BinaryPrimitives.ReadUInt32BigEndian(timeBuf);
                                }

                                if (creationSeconds > 0)
                                {
                                    var epoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                    if (creationSeconds < (ulong)(DateTime.MaxValue - epoch).TotalSeconds)
                                    {
                                        var dt = epoch.AddSeconds(creationSeconds).ToLocalTime();
                                        if (dt.Year >= 1980 && dt.Year <= DateTime.Now.Year + 1)
                                            return dt;
                                    }
                                }
                                return null;
                            }

                            long skip = childSize >= 8 ? childSize - 8 : 0;
                            if (skip > 0) fs.Seek(skip, SeekOrigin.Current);
                            else break;
                        }
                        break;
                    }

                    if (boxDataSize > 0) fs.Seek(boxDataSize, SeekOrigin.Current);
                    else break;
                }
            }
            catch { }

            return null;
        }
    }
}
