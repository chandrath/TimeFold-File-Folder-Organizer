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
                if (service.TryGetExtensionOverride(kvp.Key, out _)) continue;
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
            if (prefCat != "Pre_JSON Files_Post") { Console.WriteLine($"[FAIL] Prefix/suffix failed: got '{prefCat}'"); return 3; }

            // Extension Mode Custom Remap Test (e.g. .pptx remapped to CAT)
            var pptxItem = new Models.FileItem { Name = "slides.pptx", FullPath = @"C:\slides.pptx", ModifiedDate = DateTime.Now };
            Services.FileTypeService.Instance.RemoveCategoryOverride(".pptx");
            string defExt = Services.TargetFolderResolver.Resolve(pptxItem, Models.OrganizationMode.Extension, Models.FolderFormat.YearMonth, "", "");
            Services.FileTypeService.Instance.SetCategoryOverride(".pptx", "CAT");
            string customExt = Services.TargetFolderResolver.Resolve(pptxItem, Models.OrganizationMode.Extension, Models.FolderFormat.YearMonth, "", "");
            bool isCustom = Services.FileTypeService.Instance.IsCustomRoute(".pptx");
            Services.FileTypeService.Instance.RemoveCategoryOverride(".pptx");
            string revExt = Services.TargetFolderResolver.Resolve(pptxItem, Models.OrganizationMode.Extension, Models.FolderFormat.YearMonth, "", "");
            if (defExt != "PPTX" || customExt != "CAT" || !isCustom || revExt != "PPTX")
            {
                Console.WriteLine($"[FAIL] Extension remap failed: def='{defExt}', custom='{customExt}', isCustom={isCustom}, rev='{revExt}'");
                return 4;
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

            // Verify In-Place Folder Movement during Organization
            string orgTestDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TimeFold_OrgTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                System.IO.Directory.CreateDirectory(orgTestDir);
                string testSubdir = System.IO.Path.Combine(orgTestDir, "TestFolder");
                System.IO.Directory.CreateDirectory(testSubdir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(testSubdir, "data.txt"), "test");

                var orgItem = new Models.FileItem
                {
                    Name = "TestFolder",
                    FullPath = testSubdir,
                    IsDirectory = true,
                    TargetFolder = "Grouped Folders"
                };

                var organizer = new Services.FileOrganizerService(
                    System.IO.Path.Combine(orgTestDir, "TimeFold.exe"),
                    orgTestDir,
                    orgTestDir);
                organizer.ApplyNamingSettings(
                    Models.FolderFormat.YearMonth, "", "", false,
                    Models.OrganizationMode.Extension, createSortedSubfolder: false);

                var orgResult = organizer.OrganizeFilesAsync(
                    new List<Models.FileItem> { orgItem },
                    null!,
                    System.Threading.CancellationToken.None,
                    false,
                    Models.ConflictResolutionStrategy.AutoRename).GetAwaiter().GetResult();

                if (orgResult.FilesMoved != 1 || !System.IO.Directory.Exists(System.IO.Path.Combine(orgTestDir, "Grouped Folders", "TestFolder")))
                {
                    Console.WriteLine("[FAIL] Organization failed: TestFolder was not moved into Grouped Folders");
                    return 11;
                }
            }
            finally
            {
                try { if (System.IO.Directory.Exists(orgTestDir)) System.IO.Directory.Delete(orgTestDir, true); } catch { }
            }

            // Verify Cancellation preserves LastResult for Undo
            string cancelTestDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TimeFold_CancelTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                System.IO.Directory.CreateDirectory(cancelTestDir);
                string testFile1 = System.IO.Path.Combine(cancelTestDir, "test1.txt");
                System.IO.File.WriteAllText(testFile1, "hello");

                var item1 = new Models.FileItem { Name = "test1.txt", FullPath = testFile1, TargetFolder = "TXT" };
                var cts = new System.Threading.CancellationTokenSource();
                cts.Cancel();

                var cancelOrganizer = new Services.FileOrganizerService(
                    System.IO.Path.Combine(cancelTestDir, "TimeFold.exe"),
                    cancelTestDir, cancelTestDir);

                try
                {
                    cancelOrganizer.OrganizeFilesAsync(
                        new List<Models.FileItem> { item1 },
                        null!, cts.Token, false).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { }

                if (cancelOrganizer.LastResult == null)
                {
                    Console.WriteLine("[FAIL] Cancellation test failed: LastResult was null");
                    return 12;
                }
            }
            finally
            {
                try { if (System.IO.Directory.Exists(cancelTestDir)) System.IO.Directory.Delete(cancelTestDir, true); } catch { }
            }

            // Verify Folder Exclusion and Checkbox Selection Behavior
            string exclTestDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TimeFold_ExclTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                System.IO.Directory.CreateDirectory(exclTestDir);
                string myFilesDir = System.IO.Path.Combine(exclTestDir, "MYFILES");
                System.IO.Directory.CreateDirectory(myFilesDir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(myFilesDir, "secret.txt"), "data");

                string normalDir = System.IO.Path.Combine(exclTestDir, "Projects");
                System.IO.Directory.CreateDirectory(normalDir);

                string file1 = System.IO.Path.Combine(exclTestDir, "doc.pdf");
                System.IO.File.WriteAllText(file1, "pdf content");

                string file2 = System.IO.Path.Combine(exclTestDir, "notes.txt");
                System.IO.File.WriteAllText(file2, "notes content");

                var exclOrganizer = new Services.FileOrganizerService(
                    System.IO.Path.Combine(exclTestDir, "TimeFold.exe"), exclTestDir, exclTestDir);
                exclOrganizer.ApplyNamingSettings(
                    Models.FolderFormat.YearMonth, "", "", false,
                    Models.OrganizationMode.Category, createSortedSubfolder: false);

                // Test 1: Scan with exclusion rule with whitespace & case difference
                var rules = new List<string> { "  myfiles  " };
                var scanned = exclOrganizer.ScanFiles(includeTopLevelFolders: true, excludedFolders: rules, enableFolderExclusions: true);

                var exclItem = scanned.FirstOrDefault(f => f.Name.Equals("MYFILES", StringComparison.OrdinalIgnoreCase));
                if (exclItem == null || !exclItem.IsExcludedByRule || exclItem.IsSelected || exclItem.TargetFolder != "— (Ignored)")
                {
                    Console.WriteLine("[FAIL] Exclusion rule test failed: MYFILES was not excluded or target was wrong");
                    return 13;
                }

                // Test 2: Uncheck one file (notes.txt)
                var unselItem = scanned.FirstOrDefault(f => f.Name == "notes.txt");
                if (unselItem != null) unselItem.IsSelected = false;

                // Test 3: Organize files and verify MYFILES and notes.txt remain untouched at source
                var res = exclOrganizer.OrganizeFilesAsync(scanned, null!, System.Threading.CancellationToken.None, false).GetAwaiter().GetResult();

                if (!System.IO.Directory.Exists(myFilesDir) || !System.IO.File.Exists(System.IO.Path.Combine(myFilesDir, "secret.txt")))
                {
                    Console.WriteLine("[FAIL] Exclusion test failed: MYFILES was moved or altered");
                    return 14;
                }

                if (!System.IO.File.Exists(file2))
                {
                    Console.WriteLine("[FAIL] Unchecked item test failed: unselected notes.txt was moved");
                    return 15;
                }

                // Test 4: Temporary toggle disabled (enableFolderExclusions: false)
                var scanDisabled = exclOrganizer.ScanFiles(includeTopLevelFolders: true, excludedFolders: rules, enableFolderExclusions: false);
                var toggledItem = scanDisabled.FirstOrDefault(f => f.Name.Equals("MYFILES", StringComparison.OrdinalIgnoreCase));
                if (toggledItem == null || toggledItem.IsExcludedByRule || !toggledItem.IsSelected)
                {
                    Console.WriteLine("[FAIL] Exclusion toggle test failed: MYFILES should be active when toggle is false");
                    return 16;
                }

                // Test 5: Verify .timefold-ignore subfolder marker auto-detection, self-exemption, and TypeDisplay
                System.IO.Directory.CreateDirectory(normalDir);
                string subMarker = System.IO.Path.Combine(normalDir, Config.AppConstants.TimefoldIgnoreFileName);
                System.IO.File.WriteAllBytes(subMarker, Array.Empty<byte>());

                var scanIgnoreFile = exclOrganizer.ScanFiles(includeTopLevelFolders: true, enableFolderExclusions: true);
                if (scanIgnoreFile.Any(f => f.Name.Equals(Config.AppConstants.TimefoldIgnoreFileName, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("[FAIL] .timefold-ignore self-exemption failed: .timefold-ignore should never be scanned");
                    return 17;
                }

                var projectItem = scanIgnoreFile.FirstOrDefault(f => f.Name.Equals("Projects", StringComparison.OrdinalIgnoreCase));
                if (projectItem == null || !projectItem.IsExcludedByRule || projectItem.TypeDisplay != "Folder (Ignored)" || projectItem.TargetFolder != "— (Ignored)")
                {
                    Console.WriteLine("[FAIL] .timefold-ignore rule test failed: Projects was not excluded or TypeDisplay was incorrect");
                    return 18;
                }

                var pinnedOrder = scanIgnoreFile.OrderByDescending(f => f.IsExcludedByRule).ToList();
                if (!pinnedOrder.First().IsExcludedByRule)
                {
                    Console.WriteLine("[FAIL] Pinned-at-top sort failed: first item was not an excluded folder");
                    return 19;
                }

                // Test 6: Verify MediaDateTaken resolution in TargetFolderResolver
                var photoItem = new Models.FileItem
                {
                    Name = "vacation.jpg",
                    ModifiedDate = new DateTime(2026, 9, 20),
                    CreatedDate = new DateTime(2026, 9, 26),
                    MediaDateTaken = new DateTime(2021, 7, 14),
                    IsMediaDateActive = true
                };
                string photoFolder = Services.TargetFolderResolver.Resolve(photoItem, Models.OrganizationMode.Date, Models.FolderFormat.YearMonth, "", "");
                if (!photoFolder.StartsWith("2021 July", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[FAIL] MediaDateTaken test failed: expected '2021 July', got '{photoFolder}'");
                    return 20;
                }

                // Test 7: Verify FileItemComparer with hasMediaDateColumn
                var itemA = new Models.FileItem { Name = "a.jpg", MediaDateTaken = new DateTime(2020, 1, 1), IsMediaDateActive = true };
                var itemB = new Models.FileItem { Name = "b.jpg", MediaDateTaken = new DateTime(2022, 1, 1), IsMediaDateActive = true };
                var sortList = new System.Collections.Generic.List<Models.FileItem> { itemB, itemA };
                Models.FileItemComparer.Sort(sortList, 4, true, hasMediaDateColumn: true);
                if (sortList[0].Name != "a.jpg")
                {
                    Console.WriteLine("[FAIL] FileItemComparer sort on media date column failed");
                    return 21;
                }

                // Test 8: Verify AppConstants.FormatTooltipDate
                var testDate = new DateTime(2020, 8, 15, 15, 30, 22);
                string formatted = Config.AppConstants.FormatTooltipDate(testDate);
                if (formatted != "15 Aug 2020 03:30:22 PM")
                {
                    Console.WriteLine($"[FAIL] FormatTooltipDate test failed: expected '15 Aug 2020 03:30:22 PM', got '{formatted}'");
                    return 22;
                }

                // Test 9: Verify MP4 video creation date extraction
                string testMp4 = System.IO.Path.Combine(exclTestDir, "test_clip.mp4");
                ulong sec2024 = (ulong)(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) - new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                using (var ms = new System.IO.FileStream(testMp4, System.IO.FileMode.Create))
                {
                    ms.Write(new byte[] { 0, 0, 0, 16, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6f, 0x6d, 0, 0, 0, 0 });
                    ms.Write(new byte[] { 0, 0, 0, 36, 0x6d, 0x6f, 0x6f, 0x76 });
                    ms.Write(new byte[] { 0, 0, 0, 28, 0x6d, 0x76, 0x68, 0x64, 0, 0, 0, 0 });
                    byte[] secBuf = new byte[4];
                    System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(secBuf, (uint)sec2024);
                    ms.Write(secBuf);
                    ms.Write(new byte[12]);
                }
                var mp4Date = Services.MediaDateExtractor.TryGetDateTaken(testMp4);
                if (mp4Date == null || mp4Date.Value.Year != 2024)
                {
                    Console.WriteLine($"[FAIL] MP4 creation date test failed: expected year 2024, got '{mp4Date}'");
                    return 23;
                }

                // Test 10: Verify AppConstants.BuildDateTooltip contains guidance
                var testMediaItem = new Models.FileItem { Name = "photo.jpg", MediaDateTaken = new DateTime(2023, 8, 14, 15, 30, 22), ModifiedDate = new DateTime(2026, 9, 20, 11, 45, 10), CreatedDate = new DateTime(2026, 9, 26, 18, 20, 0), IsMediaDateActive = true, TargetFolder = "2023-08" };
                string mediaTip = Config.AppConstants.BuildDateTooltip(testMediaItem);
                if (!mediaTip.Contains("📷 Date Taken:") || !mediaTip.Contains("disable 'Prioritize original media Date Taken"))
                {
                    Console.WriteLine("[FAIL] BuildDateTooltip media test failed");
                    return 24;
                }
            }
            finally
            {
                try { if (System.IO.Directory.Exists(exclTestDir)) System.IO.Directory.Delete(exclTestDir, true); } catch { }
            }

            Console.WriteLine("[PASS] All category, subtitle pairing, prefix/suffix, Undo, and Folder Organization tests passed!");
            return 0;
        }
    }
}
