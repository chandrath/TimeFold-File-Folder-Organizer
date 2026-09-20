using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FileOrganizer.Models;

namespace FileOrganizer.Services
{
    public class CsvLogger
    {
        public static void WriteLog(string csvPath, List<FileItem> processedFiles, OrganizationResult result)
        {
            try
            {
                using var writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
                
                // Write header
                writer.WriteLine("Timestamp,OriginalPath,DestinationPath,MonthFolder,Action,ErrorMessage,FileSize,ModifiedDate,WasRenamed");
                
                // Write data
                foreach (var file in processedFiles)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var action = !string.IsNullOrEmpty(file.ErrorMessage) ? "ERROR" : (string.IsNullOrEmpty(file.DestinationPath) ? "CANCELLED" : "MOVED");
                    var errorMsg = !string.IsNullOrEmpty(file.ErrorMessage) ? file.ErrorMessage : "";
                    var modifiedDate = file.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var wasRenamed = file.WasRenamed ? "Yes" : "No";
                    
                    writer.WriteLine($"{timestamp},{EscapeCsv(file.FullPath)},{EscapeCsv(file.DestinationPath)},{EscapeCsv(file.MonthYear)},{action},{EscapeCsv(errorMsg)},{file.Size},{modifiedDate},{wasRenamed}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write CSV log: {ex.Message}", ex);
            }
        }
        
        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            if (value.Contains(',') || value.Contains('\"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            
            return value;
        }
    }
}

