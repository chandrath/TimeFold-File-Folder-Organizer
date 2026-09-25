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

            // Git Repository Detection & Mode Isolation Tests
            var gitRepo = new Models.FileItem { Name = "my-repo", FullPath = @"C:\Source\my-repo", IsDirectory = true, IsGitRepository = true, ModifiedDate = new DateTime(2026, 9, 24) };
            string gitCat = Services.TargetFolderResolver.Resolve(gitRepo, Models.OrganizationMode.Category, Models.FolderFormat.YearMonth, "", "");
            string gitDate = Services.TargetFolderResolver.Resolve(gitRepo, Models.OrganizationMode.Date, Models.FolderFormat.YearMonth, "", "");
            string gitExt = Services.TargetFolderResolver.Resolve(gitRepo, Models.OrganizationMode.Extension, Models.FolderFormat.YearMonth, "", "");
            string gitHyb1 = Services.TargetFolderResolver.Resolve(gitRepo, Models.OrganizationMode.CategoryAndDate, Models.FolderFormat.YearMonth, "", "");
            string gitHyb2 = Services.TargetFolderResolver.Resolve(gitRepo, Models.OrganizationMode.DateAndCategory, Models.FolderFormat.YearMonth, "", "");
            string gitDisabled = Services.TargetFolderResolver.Resolve(gitRepo, Models.OrganizationMode.Category, Models.FolderFormat.YearMonth, "", "", "", "", false);

            string gitExtDisabled = Services.TargetFolderResolver.Resolve(gitRepo, Models.OrganizationMode.Extension, Models.FolderFormat.YearMonth, "", "", "", "", false);
            var normalFolder = new Models.FileItem { Name = "my-docs", FullPath = @"C:\Source\my-docs", IsDirectory = true, IsGitRepository = false };
            string normalExt = Services.TargetFolderResolver.Resolve(normalFolder, Models.OrganizationMode.Extension, Models.FolderFormat.YearMonth, "", "");

            if (gitCat != "Git Repos" || !gitDate.StartsWith("2026") || gitExt != "Git Repos" || gitExtDisabled != "Grouped Folders" || normalExt != "Grouped Folders" || !gitHyb1.StartsWith("Git Repos") || !gitHyb2.EndsWith("Git Repos") || gitDisabled != "Grouped Folders")
            {
                Console.WriteLine($"[FAIL] Git repo resolver routing failed: Cat='{gitCat}', Date='{gitDate}', Ext='{gitExt}', ExtDis='{gitExtDisabled}', NormalExt='{normalExt}'");
                return 8;
            }

            // Level 2 Single-Wrapper Git Repository Detection Tests
            string gitTestRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TimeFold_GitTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                string dirL1 = System.IO.Path.Combine(gitTestRoot, "RepoL1");
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dirL1, ".github"));

                string dirL2 = System.IO.Path.Combine(gitTestRoot, "WrapperL2");
                string childL2 = System.IO.Path.Combine(dirL2, "InnerRepo");
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(childL2, ".github"));

                string dirMulti = System.IO.Path.Combine(gitTestRoot, "Workspace");
                string multiChild1 = System.IO.Path.Combine(dirMulti, "ProjectA");
                string multiChild2 = System.IO.Path.Combine(dirMulti, "ProjectB");
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(multiChild1, ".github"));
                System.IO.Directory.CreateDirectory(multiChild2);

                bool isL1 = Services.FileOrganizerService.IsGitRepository(dirL1);
                bool isL2 = Services.FileOrganizerService.IsGitRepository(dirL2);
                bool isMulti = Services.FileOrganizerService.IsGitRepository(dirMulti);

                if (!isL1 || !isL2 || isMulti)
                {
                    Console.WriteLine($"[FAIL] Git repo detection test failed: L1={isL1}, L2={isL2}, Multi={isMulti}");
                    return 9;
                }
            }
            finally
            {
                try { if (System.IO.Directory.Exists(gitTestRoot)) System.IO.Directory.Delete(gitTestRoot, true); } catch { }
            }

            // Undo Service Sanity Test
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TimeFold_UndoSanity_" + Guid.NewGuid().ToString("N"));
            string srcDir = System.IO.Path.Combine(tempDir, "Source");
            string outDir = System.IO.Path.Combine(tempDir, "Output");
            string sortedDir = System.IO.Path.Combine(outDir, "Sorted_Test");
            string monthDir = System.IO.Path.Combine(sortedDir, "2026 January");

            try
            {
                System.IO.Directory.CreateDirectory(srcDir);
                System.IO.Directory.CreateDirectory(monthDir);

                string origPath = System.IO.Path.Combine(srcDir, "doc.txt");
                string destPath = System.IO.Path.Combine(monthDir, "doc.txt");
                System.IO.File.WriteAllText(destPath, "TimeFold Undo Test Content");

                var item = new Models.FileItem
                {
                    Name = "doc.txt",
                    FullPath = origPath,
                    DestinationPath = destPath,
                    ModifiedDate = DateTime.Now
                };

                string dummyLog = System.IO.Path.Combine(outDir, "TimeFold_Log_Test.csv");
                System.IO.File.WriteAllText(dummyLog, "timestamp,orig,dest");

                var session = Services.UndoService.RecordSession(srcDir, outDir, sortedDir, new[] { item }, new[] { monthDir, sortedDir }, dummyLog);
                var preflight = Services.UndoService.PreflightCheck(session);
                if (preflight.ReadyToRestore != 1)
                {
                    Console.WriteLine($"[FAIL] Undo preflight failed: ReadyToRestore was {preflight.ReadyToRestore}");
                    return 4;
                }

                var undoResult = Services.UndoService.UndoAsync(session, null, System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                if (undoResult.RestoredCount != 1 || !System.IO.File.Exists(origPath))
                {
                    Console.WriteLine("[FAIL] Undo execution failed: file was not restored to original path");
                    return 5;
                }

                if (System.IO.Directory.Exists(monthDir))
                {
                    Console.WriteLine("[FAIL] Undo pruning failed: empty month directory was not cleaned up");
                    return 6;
                }

                if (System.IO.File.Exists(dummyLog))
                {
                    Console.WriteLine("[FAIL] Undo log cleanup failed: specific CSV log was not deleted");
                    return 7;
                }
            }
            finally
            {
                Services.UndoService.ClearSession();
                try { if (System.IO.Directory.Exists(tempDir)) System.IO.Directory.Delete(tempDir, true); } catch { }
            }

            // Verify Undo 7-Day Expiry TTL
            var expiredSession = new Models.UndoSession { Timestamp = DateTime.Now.AddDays(-8) };
            if (!Services.UndoService.IsSessionExpired(expiredSession))
            {
                Console.WriteLine("[FAIL] Undo TTL check failed: 8-day old session was not marked expired");
                return 8;
            }
            var freshSession = new Models.UndoSession { Timestamp = DateTime.Now.AddDays(-2) };
            if (Services.UndoService.IsSessionExpired(freshSession))
            {
                Console.WriteLine("[FAIL] Undo TTL check failed: 2-day old session was incorrectly marked expired");
                return 9;
            }

            // Verify Untouched Item Undo Exemption (Prevents false self-collision)
            var untouchedItem = new Models.FileItem { FullPath = @"C:\Source\Untouched", DestinationPath = @"C:\Source\Untouched", IsDirectory = true };
            var movedItem = new Models.FileItem { FullPath = @"C:\Source\Moved", DestinationPath = @"C:\Output\Moved", IsDirectory = true };
            var filterSession = Services.UndoService.RecordSession(@"C:\Source", @"C:\Output", "", new[] { untouchedItem, movedItem });
            if (filterSession.MovedItems.Count != 1 || filterSession.MovedItems[0].OriginalPath != @"C:\Source\Moved")
            {
                Console.WriteLine($"[FAIL] Undo untouched item filter failed: count was {filterSession.MovedItems.Count}");
                return 10;
            }
            Services.UndoService.ClearSession();

            Console.WriteLine("[PASS] All category, subtitle pairing, prefix/suffix, and Undo (Beta) tests passed!");
            return 0;
        }
    }
}

