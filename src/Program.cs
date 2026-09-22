using System;
using System.Windows.Forms;

namespace FileOrganizer
{
    internal static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args != null && args.Length > 0 && args[0] == "--test")
            {
                return RunSanityTests();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.Run(new MainForm());
            return 0;
        }

        private static int RunSanityTests()
        {
            Console.WriteLine("=== Running TimeFold Sanity Verification ===");
            var service = Services.FileTypeService.Instance;

            var testMap = new System.Collections.Generic.Dictionary<string, string>
            {
                [".json"] = "JSON Files",
                [".jsonc"] = "JSON Files",
                [".xml"] = "Data & Config Files",
                [".yaml"] = "Data & Config Files",
                [".env"] = "Data & Config Files",
                [".c3p"] = "Game Dev Files",
                [".godot"] = "Game Dev Files",
                [".unitypackage"] = "Game Dev Files",
                [".yyp"] = "Game Dev Files",
                [".blend"] = "3D Files",
                [".3mf"] = "3D Files",
                [".gltf"] = "3D Files",
                [".usdz"] = "3D Files",
                [".flatpak"] = "App Installers",
                [".appimage"] = "App Installers",
                [".deb"] = "App Installers",
                [".dmg"] = "App Installers",
                [".ipa"] = "App Installers",
                [".ipk"] = "App Installers",
                [".apk"] = "App Installers",
                [".exe"] = "App Installers",
                [".psd"] = "Photoshop Files",
                [".psb"] = "Photoshop Files",
                [".indd"] = "Publishing Files",
                [".svg"] = "Vector Files",
                [".ai"] = "Vector Files",
                [".cs"] = "Code Files",
                [".mp4"] = "Video Files",
                [".mp3"] = "Audio Files",
                [".zip"] = "Zip & Archives",
                [".ttf"] = "Font Files",
                [".docx"] = "Office Files",
                [".pdf"] = "PDF Files",
                [".epub"] = "Reader Files",
                [".txt"] = "Text & Notes",
                [".ps1"] = "Script Files",
                [".bat"] = "Script Files",
                [".sh"] = "Script Files",
                [".lnk"] = "Shortcuts",
                [".url"] = "Web Links"
            };

            foreach (var kvp in testMap)
            {
                string cat = service.GetCategory(kvp.Key);
                if (cat != kvp.Value)
                {
                    Console.WriteLine($"[FAIL] {kvp.Key}: got '{cat}', expected '{kvp.Value}'");
                    return 1;
                }
            }

            // Subtitle Pairing Test
            var movie = new Models.FileItem { Name = "Film.mkv", FullPath = @"C:\Film.mkv", ModifiedDate = DateTime.Now };
            movie.TargetFolder = Services.TargetFolderResolver.Resolve(movie, Models.OrganizationMode.Category, Models.FolderFormat.YearMonth, "", "");
            var sub = new Models.FileItem { Name = "Film.en.srt", FullPath = @"C:\Film.en.srt", ModifiedDate = DateTime.Now };
            sub.TargetFolder = Services.TargetFolderResolver.Resolve(sub, Models.OrganizationMode.Category, Models.FolderFormat.YearMonth, "", "");

            var items = new System.Collections.Generic.List<Models.FileItem> { movie, sub };
            Services.TargetFolderResolver.ApplySubtitleCompanionPairing(items);
            if (sub.TargetFolder != movie.TargetFolder)
            {
                Console.WriteLine($"[FAIL] Subtitle pairing failed: {sub.TargetFolder} != {movie.TargetFolder}");
                return 2;
            }

            // Prefix/Suffix Test
            var jsonItem = new Models.FileItem { Name = "app.json", FullPath = @"C:\app.json", ModifiedDate = DateTime.Now };
            string prefCat = Services.TargetFolderResolver.Resolve(jsonItem, Models.OrganizationMode.Category, Models.FolderFormat.YearMonth, "", "", "Pre_", "_Post");
            if (prefCat != "Pre_JSON Files_Post")
            {
                Console.WriteLine($"[FAIL] Prefix/suffix failed: got '{prefCat}'");
                return 3;
            }

            Console.WriteLine("[PASS] All category, subtitle pairing, and prefix/suffix tests passed!");
            return 0;
        }
    }
}

