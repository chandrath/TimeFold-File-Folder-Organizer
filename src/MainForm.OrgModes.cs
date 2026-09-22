using System;
using System.Drawing;
using System.Windows.Forms;
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

            var hdrSimple = new ToolStripMenuItem("── Simple (1-Level Folders) ──") { Enabled = false };
            var itemDate = new ToolStripMenuItem("📅 By Date Timeline (Default)", null, (s, a) => SetOrganizationMode(OrganizationMode.Date)) { Checked = _settings.OrgMode == OrganizationMode.Date };
            var itemCat = new ToolStripMenuItem("🗂️ By Smart Category (Images, Documents...)", null, (s, a) => SetOrganizationMode(OrganizationMode.Category)) { Checked = _settings.OrgMode == OrganizationMode.Category };
            var itemExt = new ToolStripMenuItem("🏷️ By File Extension (JPG, PDF...)", null, (s, a) => SetOrganizationMode(OrganizationMode.Extension)) { Checked = _settings.OrgMode == OrganizationMode.Extension };

            var hdrHybrid = new ToolStripMenuItem("── Hybrid (2-Level Nested) ──") { Enabled = false };
            var itemHyb1 = new ToolStripMenuItem(@"🔀 Category / Date  (e.g. Images\2026-09)", null, (s, a) => SetOrganizationMode(OrganizationMode.CategoryAndDate)) { Checked = _settings.OrgMode == OrganizationMode.CategoryAndDate };
            var itemHyb2 = new ToolStripMenuItem(@"🔄 Date / Category  (e.g. 2026-09\Images)", null, (s, a) => SetOrganizationMode(OrganizationMode.DateAndCategory)) { Checked = _settings.OrgMode == OrganizationMode.DateAndCategory };

            var itemRules = new ToolStripMenuItem("⚙ Customize File Type Rules...", null, (s, a) =>
            {
                using var dlg = new TypeRulesDialog(_settings.DarkMode);
                dlg.ShowDialog(this);
                ReapplyOrganizationMode();
            });

            menu.Items.AddRange(new ToolStripItem[]
            {
                hdrSimple, itemDate, itemCat, itemExt,
                new ToolStripSeparator(),
                hdrHybrid, itemHyb1, itemHyb2,
                new ToolStripSeparator(),
                itemRules
            });

            menu.Show(_btnModeSelector, new Point(0, _btnModeSelector.Height + 2));
        }

        private void SetOrganizationMode(OrganizationMode mode)
        {
            _settings.OrgMode = mode;
            _settings.SaveToFile();
            if (_btnModeSelector != null) _btnModeSelector.Text = GetModeSelectorText();
            if (_lblFormatBadge != null)
            {
                _lblFormatBadge.Visible = (mode != OrganizationMode.Category && mode != OrganizationMode.Extension);
            }
            ReapplyOrganizationMode();
        }

        private void ReapplyOrganizationMode()
        {
            if (_organizerService != null)
            {
                _organizerService.ApplyNamingSettings(_settings.FolderFormat, _settings.FolderPrefix, _settings.FolderSuffix, _settings.Use24HourTimestamp, _settings.OrgMode, _settings.KeepHtmlCompanionsTogether);
            }

            if (_filesToOrganize != null && _filesToOrganize.Count > 0)
            {
                foreach (var file in _filesToOrganize)
                {
                    file.TargetFolder = TargetFolderResolver.Resolve(file, _settings.OrgMode, _settings.FolderFormat, _settings.FolderPrefix, _settings.FolderSuffix);
                }
                if (_settings.KeepHtmlCompanionsTogether)
                {
                    TargetFolderResolver.ApplyHtmlCompanionPairing(_filesToOrganize);
                }
                UpdateFileList();
                UpdateSummary();
                CheckConflicts();
            }
        }
    }
}
