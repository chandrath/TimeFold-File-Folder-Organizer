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
            var itemDate = new ToolStripMenuItem("📅 By Date Timeline (Default)", null, (s, a) => SetOrganizationMode(OrganizationMode.Date)) { Checked = _settings.OrgMode == OrganizationMode.Date };
            var itemCat = new ToolStripMenuItem("🗂️ By Smart Category (Images, Documents...)", null, (s, a) => SetOrganizationMode(OrganizationMode.Category)) { Checked = _settings.OrgMode == OrganizationMode.Category };
            var itemExt = new ToolStripMenuItem("🏷️ By File Extension (JPG, PDF...)", null, (s, a) => SetOrganizationMode(OrganizationMode.Extension)) { Checked = _settings.OrgMode == OrganizationMode.Extension };
            var itemHyb1 = new ToolStripMenuItem("🔀 Hybrid: Category / Date", null, (s, a) => SetOrganizationMode(OrganizationMode.CategoryAndDate)) { Checked = _settings.OrgMode == OrganizationMode.CategoryAndDate };
            var itemHyb2 = new ToolStripMenuItem("🔄 Hybrid: Date / Category", null, (s, a) => SetOrganizationMode(OrganizationMode.DateAndCategory)) { Checked = _settings.OrgMode == OrganizationMode.DateAndCategory };
            var itemRules = new ToolStripMenuItem("⚙ Customize File Type Rules...", null, (s, a) =>
            {
                using var dlg = new TypeRulesDialog(_settings.DarkMode);
                dlg.ShowDialog(this);
                ReapplyOrganizationMode();
            });

            menu.Items.AddRange(new ToolStripItem[] { itemDate, itemCat, itemExt, itemHyb1, itemHyb2, new ToolStripSeparator(), itemRules });
            menu.Show(_btnModeSelector, new Point(0, _btnModeSelector.Height + 2));
        }

        private void SetOrganizationMode(OrganizationMode mode)
        {
            _settings.OrgMode = mode;
            _settings.SaveToFile();
            if (_btnModeSelector != null) _btnModeSelector.Text = GetModeSelectorText();
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
