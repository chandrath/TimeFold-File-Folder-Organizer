using System;
using System.Buffers;
using System.Buffers.Binary;
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
                    return TryGetImageDateTaken(filePath, ext);
            }
            catch { }

            return null;
        }

        private static DateTime? TryGetImageDateTaken(string filePath, string ext)
        {
            if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                var jpegDate = TryGetJpegApp1Date(filePath);
                if (jpegDate.HasValue) return jpegDate;
            }

            return TryGetTiffDateFromStream(filePath);
        }

        private static DateTime? TryGetJpegApp1Date(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
                byte[] markerBuf = new byte[4];
                if (fs.Read(markerBuf, 0, 2) < 2 || markerBuf[0] != 0xFF || markerBuf[1] != 0xD8)
                    return null;

                while (fs.Position + 4 <= fs.Length)
                {
                    if (fs.Read(markerBuf, 0, 4) < 4) break;
                    if (markerBuf[0] != 0xFF) break;

                    byte marker = markerBuf[1];
                    if (marker == 0xDA || marker == 0xD9) break;

                    int payloadLen = (markerBuf[2] << 8) | markerBuf[3];
                    if (payloadLen < 2) break;
                    int dataLen = payloadLen - 2;

                    if (marker == 0xE1)
                    {
                        if (dataLen < 14) break;
                        byte[] app1 = ArrayPool<byte>.Shared.Rent(dataLen);
                        try
                        {
                            if (fs.Read(app1, 0, dataLen) >= dataLen &&
                                app1[0] == 0x45 && app1[1] == 0x78 && app1[2] == 0x69 && app1[3] == 0x66 && app1[4] == 0 && app1[5] == 0)
                            {
                                bool isLE = app1[6] == 0x49 && app1[7] == 0x49 && app1[8] == 0x2A && app1[9] == 0;
                                bool isBE = app1[6] == 0x4D && app1[7] == 0x4D && app1[8] == 0 && app1[9] == 0x2A;
                                if (isLE || isBE)
                                {
                                    return TryParseTiffAt(app1, 6, dataLen, isLE);
                                }
                            }
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(app1);
                        }
                        return null;
                    }

                    if (dataLen > 0) fs.Seek(dataLen, SeekOrigin.Current);
                    else break;
                }
            }
            catch { }
            return null;
        }

        private static DateTime? TryGetTiffDateFromStream(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
                int toRead = (int)Math.Min(fs.Length, 48 * 1024);
                if (toRead < 16) return null;

                byte[] buffer = ArrayPool<byte>.Shared.Rent(toRead);
                try
                {
                    int read = fs.Read(buffer, 0, toRead);
                    if (read < 16) return null;

                    for (int i = 0; i <= read - 16; i++)
                    {
                        bool isLE = buffer[i] == 0x49 && buffer[i + 1] == 0x49 && buffer[i + 2] == 0x2A && buffer[i + 3] == 0x00;
                        bool isBE = buffer[i] == 0x4D && buffer[i + 1] == 0x4D && buffer[i + 2] == 0x00 && buffer[i + 3] == 0x2A;

                        if (isLE || isBE)
                        {
                            var dt = TryParseTiffAt(buffer, i, read, isLE);
                            if (dt.HasValue) return dt;
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch { }

            return null;
        }

        private static DateTime? TryParseTiffAt(byte[] buffer, int tiffStart, int length, bool isLE)
        {
            try
            {
                if (tiffStart + 8 > length) return null;
                uint ifdOffset = ReadUInt32(buffer, tiffStart + 4, isLE);
                if (ifdOffset < 8 || tiffStart + ifdOffset + 2 > length) return null;

                return ScanIfd(buffer, tiffStart, (int)(tiffStart + ifdOffset), length, isLE);
            }
            catch { return null; }
        }

        private static DateTime? ScanIfd(byte[] buffer, int tiffStart, int ifdPos, int length, bool isLE)
        {
            if (ifdPos + 2 > length) return null;
            ushort count = ReadUInt16(buffer, ifdPos, isLE);
            if (count == 0 || count > 500) return null;

            int current = ifdPos + 2;
            DateTime? backupDate = null;

            for (int i = 0; i < count; i++)
            {
                if (current + 12 > length) break;
                ushort tag = ReadUInt16(buffer, current, isLE);
                uint valOffset = ReadUInt32(buffer, current + 8, isLE);

                if (tag == 0x9003 || tag == 0x0132)
                {
                    var dt = ReadDateString(buffer, tiffStart, valOffset, length);
                    if (dt.HasValue)
                    {
                        if (tag == 0x9003) return dt;
                        backupDate ??= dt;
                    }
                }
                else if (tag == 0x8769) // Exif Sub-IFD
                {
                    if (tiffStart + valOffset + 2 <= length)
                    {
                        var subDt = ScanIfd(buffer, tiffStart, (int)(tiffStart + valOffset), length, isLE);
                        if (subDt.HasValue) return subDt;
                    }
                }

                current += 12;
            }

            return backupDate;
        }

        private static DateTime? ReadDateString(byte[] buffer, int tiffStart, uint offset, int length)
        {
            int strStart = (int)(tiffStart + offset);
            if (strStart < 0 || strStart + 19 > length) return null;

            string raw = Encoding.ASCII.GetString(buffer, strStart, 19);
            if (DateTime.TryParseExact(raw, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                if (dt.Year >= 1980 && dt.Year <= DateTime.Now.Year + 1)
                    return dt;
            }
            return null;
        }

        private static ushort ReadUInt16(byte[] b, int offset, bool isLE) =>
            isLE ? BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(offset, 2))
                 : BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(offset, 2));

        private static uint ReadUInt32(byte[] b, int offset, bool isLE) =>
            isLE ? BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(offset, 4))
                 : BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(offset, 4));

        private static DateTime? TryGetVideoCreationDate(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
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
                                fs.Seek(3, SeekOrigin.Current);

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
