using System;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Forms;
using FileOrganizer.Models;
using FileOrganizer.Services;

namespace FileOrganizer
{
    public partial class MainForm
    {
        private string GetModeSelectorText() => _settings.OrgMode switch
        {
            OrganizationMode.Category => "🗂️ By Category",
            OrganizationMode.Extension => "🏷️ By Extension",
            OrganizationMode.CategoryAndDate => "🔀 Cat + Date",
            OrganizationMode.DateAndCategory => "🔄 Date + Cat",
            _ => "📅 By Date"
        };

        private void BtnModeSelector_Click(object? sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            var palette = AppTheme.GetPalette(_settings.DarkMode);
            if (_settings.DarkMode)
            {
                menu.Renderer = AppTheme.DarkMenuRenderer;
                menu.BackColor = palette.MenuBg;
            }

            var hdrSimple = new ToolStripMenuItem("── Simple (1-Level Folders) ──") { Enabled = false };
            var itemDate = new ToolStripMenuItem("📅 By Date Timeline (Default)", null, (s, a) => SetOrganizationMode(OrganizationMode.Date)) { Checked = _settings.OrgMode == OrganizationMode.Date };
            var itemCat = new ToolStripMenuItem("🗂️ By Smart Category (Images, Documents...)", null, (s, a) => SetOrganizationMode(OrganizationMode.Category)) { Checked = _settings.OrgMode == OrganizationMode.Category };
            var itemExt = new ToolStripMenuItem("🏷️ By File Extension (JPG, PDF...)", null, (s, a) => SetOrganizationMode(OrganizationMode.Extension)) { Checked = _settings.OrgMode == OrganizationMode.Extension };

            var hdrHybrid = new ToolStripMenuItem("── Hybrid (2-Level Nested) ──") { Enabled = false };
            string sampleDate = AppConstants.FormatFolderDate(DateTime.Now, _settings.FolderFormat, _settings.FolderPrefix, _settings.FolderSuffix);
            var itemHyb1 = new ToolStripMenuItem($@"🔀 Category / Date  (e.g. Images\{sampleDate})", null, (s, a) => SetOrganizationMode(OrganizationMode.CategoryAndDate)) { Checked = _settings.OrgMode == OrganizationMode.CategoryAndDate };
            var itemHyb2 = new ToolStripMenuItem($@"🔄 Date / Category  (e.g. {sampleDate}\Images)", null, (s, a) => SetOrganizationMode(OrganizationMode.DateAndCategory)) { Checked = _settings.OrgMode == OrganizationMode.DateAndCategory };

            var itemRules = new ToolStripMenuItem("⚙ Customize File Type Rules...", null, MenuTypeRules_Click);

            menu.Items.AddRange(new ToolStripItem[]
            {
                hdrSimple, itemDate, itemCat, itemExt,
                new ToolStripSeparator(),
                hdrHybrid, itemHyb1, itemHyb2,
                new ToolStripSeparator(),
                itemRules
            });

            if (_settings.DarkMode)
            {
                SetMenuColors(menu.Items, palette);
            }

            menu.Show(_btnModeSelector, new Point(0, _btnModeSelector.Height + 2));
        }

        private void SetOrganizationMode(OrganizationMode mode)
        {
            _settings.OrgMode = mode;
            _settings.SaveToFile();
            if (_btnModeSelector != null) _btnModeSelector.Text = GetModeSelectorText();
            UpdateFormatBadge();
            ReapplyOrganizationMode();
        }

        private void ReapplyOrganizationMode()
        {
            if (_organizerService != null)
            {
                _organizerService.ApplyNamingSettings(
                    _settings.FolderFormat,
                    _settings.FolderPrefix,
                    _settings.FolderSuffix,
                    _settings.Use24HourTimestamp,
                    _settings.OrgMode,
                    _settings.KeepHtmlCompanionsTogether,
                    _settings.CategoryPrefix,
                    _settings.CategorySuffix,
                    _settings.KeepSubtitleCompanionsTogether,
                    _settings.CreateSortedSubfolder,
                    _settings.GroupGitRepositories,
                    _settings.PackageVideoSubtitles);
            }

            if (_filesToOrganize != null && _filesToOrganize.Count > 0)
            {
                foreach (var file in _filesToOrganize)
                {
                    file.TargetFolder = TargetFolderResolver.Resolve(
                        file,
                        _settings.OrgMode,
                        _settings.FolderFormat,
                        _settings.FolderPrefix,
                        _settings.FolderSuffix,
                        _settings.CategoryPrefix,
                        _settings.CategorySuffix,
                        _settings.GroupGitRepositories);
                }
                if (_settings.KeepHtmlCompanionsTogether)
                {
                    TargetFolderResolver.ApplyHtmlCompanionPairing(_filesToOrganize);
                }
                if (_settings.KeepSubtitleCompanionsTogether)
                {
                    bool isCat = _settings.OrgMode == OrganizationMode.Category
                        || _settings.OrgMode == OrganizationMode.CategoryAndDate
                        || _settings.OrgMode == OrganizationMode.DateAndCategory;
                    TargetFolderResolver.ApplySubtitleCompanionPairing(_filesToOrganize, _settings.PackageVideoSubtitles, isCat);
                }
                UpdateFileList();
                UpdateSummary();
                CheckConflicts();
            }
        }

        private void BtnFolderRules_Click(object? sender, EventArgs e)
        {
            if (_btnFolderRules == null) return;
            var menu = new ContextMenuStrip { MinimumSize = new Size(390, 0) };
            var palette = AppTheme.GetPalette(_settings.DarkMode);
            if (_settings.DarkMode)
            {
                menu.Renderer = AppTheme.DarkMenuRenderer;
                menu.BackColor = palette.MenuBg;
            }

            var hdr = new ToolStripLabel("⚙ Folder Organization Rules")
            {
                Font = new Font(this.Font, FontStyle.Bold),
                ForeColor = _settings.DarkMode ? palette.TextPrimary : Color.FromArgb(30, 41, 59)
            };

            int ruleCount = _settings.ExcludedFolderNames?.Count ?? 0;
            string exStatus = !_settings.EnableFolderExclusions ? " (Disabled)" : (ruleCount > 0 ? $" ({ruleCount} defined)" : "");
            var itemExclusions = new ToolStripMenuItem($"🛡 Folder Exclusion Rules{exStatus}...", null, (s, a) => ShowFolderExclusionDialog())
            {
                Padding = new Padding(0, 2, 45, 2)
            };

            var itemGit = new ToolStripMenuItem("Group Git Repositories into dedicated folder", null, (s, a) =>
            {
                _settings.GroupGitRepositories = !_settings.GroupGitRepositories;
                _settings.SaveToFile();
                ReapplyOrganizationMode();
            }) { Checked = _settings.GroupGitRepositories, CheckOnClick = true, Padding = new Padding(0, 2, 45, 2) };

            var itemHtml = new ToolStripMenuItem("Keep HTML companion folders together", null, (s, a) =>
            {
                _settings.KeepHtmlCompanionsTogether = !_settings.KeepHtmlCompanionsTogether;
                _settings.SaveToFile();
                ReapplyOrganizationMode();
            }) { Checked = _settings.KeepHtmlCompanionsTogether, CheckOnClick = true, Padding = new Padding(0, 2, 45, 2) };

            var itemSubtitles = new ToolStripMenuItem("Group Video and Subtitles into dedicated folder", null, (s, a) =>
            {
                _settings.PackageVideoSubtitles = !_settings.PackageVideoSubtitles;
                _settings.SaveToFile();
                ReapplyOrganizationMode();
            }) { Checked = _settings.PackageVideoSubtitles, CheckOnClick = true, Padding = new Padding(0, 2, 45, 2) };

            menu.Items.AddRange(new ToolStripItem[] {
                hdr, new ToolStripSeparator(),
                itemExclusions, new ToolStripSeparator(),
                itemGit, itemHtml, itemSubtitles, new ToolStripSeparator()
            });

            bool isCategoryMode = _settings.OrgMode == OrganizationMode.Category
                || _settings.OrgMode == OrganizationMode.CategoryAndDate
                || _settings.OrgMode == OrganizationMode.DateAndCategory;

            if (_settings.DarkMode) SetMenuColors(menu.Items, palette);

            var activeItems = new List<string>();
            if (isCategoryMode)
            {
                if (_settings.GroupGitRepositories) activeItems.Add("• Git repositories isolated into 'Git Repos'");
                if (_settings.KeepHtmlCompanionsTogether) activeItems.Add("• HTML companion folders kept with HTML files");
                if (_settings.PackageVideoSubtitles) activeItems.Add("• Matching video & subtitle pairs packaged");
            }
            else if (_settings.OrgMode == OrganizationMode.Extension)
            {
                if (_settings.GroupGitRepositories) activeItems.Add("• Git repositories grouped into 'Git Repos'");
                if (_settings.KeepHtmlCompanionsTogether) activeItems.Add("• HTML companion folders kept with HTML files");
                activeItems.Add("• Other loose folders placed into 'Folders'");
            }
            else // Date mode
            {
                if (_settings.KeepHtmlCompanionsTogether) activeItems.Add("• HTML companion folders kept with HTML files");
            }

            string modeName = isCategoryMode ? "Category Mode" : (_settings.OrgMode == OrganizationMode.Extension ? "Extension Mode" : "Date Mode");
            Color headerColor = activeItems.Count > 0
                ? (_settings.DarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(16, 185, 129))
                : (_settings.DarkMode ? Color.FromArgb(156, 163, 175) : Color.FromArgb(100, 116, 139));
            Color textColor = _settings.DarkMode ? Color.FromArgb(156, 163, 175) : Color.FromArgb(100, 116, 139);

            var lblHeader = new ToolStripLabel(activeItems.Count > 0 ? $"  🟢 Active Rules ({modeName})" : $"  ⚪ No Folder Rules Active ({modeName})")
            {
                Font = new Font(this.Font, FontStyle.Bold),
                ForeColor = headerColor
            };
            menu.Items.Add(lblHeader);

            if (activeItems.Count > 0)
            {
                foreach (var text in activeItems)
                {
                    menu.Items.Add(new ToolStripLabel($"    {text}")
                    {
                        Font = new Font(this.Font.FontFamily, this.Font.Size - 0.5f, FontStyle.Regular),
                        ForeColor = textColor
                    });
                }
            }
            else
            {
                menu.Items.Add(new ToolStripLabel("    Default folder routing applied.")
                {
                    Font = new Font(this.Font.FontFamily, this.Font.Size - 0.5f, FontStyle.Regular),
                    ForeColor = textColor
                });
            }

            if (!isCategoryMode)
            {
                var tipSwitch = new ToolStripMenuItem("  👉 Switch to By Smart Category for all rules", null, (s, a) => SetOrganizationMode(OrganizationMode.Category))
                {
                    Font = new Font(this.Font, FontStyle.Bold),
                    ForeColor = _settings.DarkMode ? Color.FromArgb(96, 165, 250) : AppConstants.ColorPrimary
                };
                menu.Items.Add(tipSwitch);
            }

            menu.Show(_btnFolderRules, new Point(0, _btnFolderRules.Height + 2));
        }

        private void MenuTypeRules_Click(object? sender, EventArgs e)
        {
            using var dlg = new TypeRulesDialog(_settings.DarkMode);
            dlg.ShowDialog(this);
            _settings = AppSettings.LoadFromFile();
            if (_btnModeSelector != null) _btnModeSelector.Text = GetModeSelectorText();
            UpdateFormatBadge();
            ReapplyOrganizationMode();
        }
    }
}
