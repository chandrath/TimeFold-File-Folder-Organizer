using System;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;

namespace FileOrganizer
{
    public partial class MainForm
    {
        private void ApplyTheme(bool isDark)
        {
            var palette = AppTheme.GetPalette(isDark);

            // 1. Windows Native Chrome Title Bar
            AppTheme.SetWindowDarkTitleBar(this.Handle, isDark);

            // 2. Window Surface
            this.BackColor = palette.CanvasBg;

            // 3. Menu Bar
            if (_menuStrip != null)
            {
                if (isDark)
                {
                    _menuStrip.Renderer = AppTheme.DarkMenuRenderer;
                    _menuStrip.BackColor = palette.MenuBg;
                    _menuStrip.ForeColor = palette.MenuText;
                }
                else
                {
                    _menuStrip.Renderer = new ToolStripProfessionalRenderer();
                    _menuStrip.BackColor = Color.White;
                    _menuStrip.ForeColor = Color.FromArgb(15, 23, 42);
                }
                SetMenuColors(_menuStrip.Items, palette);
            }

            // 4. Main Panel & Section Containers
            if (_pnlMain != null) _pnlMain.BackColor = palette.CanvasBg;
            if (_pnlTopSection != null) _pnlTopSection.BackColor = Color.Transparent;
            if (_pnlCenterSection != null) _pnlCenterSection.BackColor = Color.Transparent;
            if (_pnlFooterSection != null)
            {
                _pnlFooterSection.BackColor = palette.CardBg;
                _pnlFooterSection.Invalidate();
            }

            // 5. Header Branding & Source Inputs
            if (_lblTitle != null) _lblTitle.ForeColor = palette.TextPrimary;
            if (_lblSubtitle != null) _lblSubtitle.ForeColor = palette.TextMuted;
            if (_lblSourceTitle != null) _lblSourceTitle.ForeColor = palette.TextPrimary;

            if (_txtSourceFolder != null)
            {
                _txtSourceFolder.BackColor = palette.InputBg;
                _txtSourceFolder.ForeColor = palette.TextPrimary;
            }

            if (_btnBrowseSource != null)
            {
                _btnBrowseSource.BackColor = palette.SecondaryButtonBg;
                _btnBrowseSource.ForeColor = palette.SecondaryButtonText;
                _btnBrowseSource.BorderColor = palette.SecondaryButtonBorder;
            }

            if (_btnRecentFolders != null)
            {
                _btnRecentFolders.BackColor = palette.SecondaryButtonBg;
                _btnRecentFolders.ForeColor = palette.SecondaryButtonText;
                _btnRecentFolders.BorderColor = palette.SecondaryButtonBorder;
            }

            // 6. Obvious Drag-and-Drop Zone
            if (_pnlSourceDrop != null)
            {
                _pnlSourceDrop.BackColor = palette.DropZoneBg;
                _pnlSourceDrop.Invalidate();
            }
            if (_lblDropHint != null)
            {
                _lblDropHint.ForeColor = palette.DropZoneText;
            }

            // 7. Output Configuration
            if (_chkUseSourceAsOutput != null)
            {
                _chkUseSourceAsOutput.ForeColor = palette.TextPrimary;
            }
            UpdateOutputFolder();

            if (_btnBrowseOutput != null)
            {
                _btnBrowseOutput.BackColor = palette.SecondaryButtonBg;
                _btnBrowseOutput.ForeColor = palette.SecondaryButtonText;
                _btnBrowseOutput.BorderColor = palette.SecondaryButtonBorder;
            }

            // 8. File Preview Header & ListView
            if (_lblPreviewHeader != null) _lblPreviewHeader.ForeColor = palette.TextPrimary;
            if (_lblFormatBadge != null)
            {
                _lblFormatBadge.BackColor = palette.BadgeBg;
                _lblFormatBadge.ForeColor = palette.BadgeText;
                _lblFormatBadge.BorderColor = palette.BadgeBorder;
            }

            if (_lstFiles != null)
            {
                _lstFiles.BackColor = palette.ListBg;
                _lstFiles.ForeColor = palette.ListText;
            }

            // 9. Empty State
            if (_pnlEmptyState != null) _pnlEmptyState.BackColor = palette.CardBg;
            if (_lblEmptyTitle != null) _lblEmptyTitle.ForeColor = palette.TextPrimary;
            if (_lblEmptyDesc != null) _lblEmptyDesc.ForeColor = palette.TextMuted;

            // 10. Conflict and Timestamp Banners
            if (_pnlConflicts != null) _pnlConflicts.BackColor = palette.DangerBg;
            if (_lblConflicts != null) _lblConflicts.ForeColor = palette.DangerText;
            if (_pnlTimestampWarning != null) _pnlTimestampWarning.BackColor = palette.WarningBg;
            if (_lblTimestampWarning != null) _lblTimestampWarning.ForeColor = palette.WarningText;

            // 11. Footer Summary Label
            if (_lblSummary != null) _lblSummary.ForeColor = palette.TextMuted;

            // 12. Progress Panel
            if (_pnlProgress != null) _pnlProgress.BackColor = palette.CanvasBg;
            if (_lblProgressTitle != null) _lblProgressTitle.ForeColor = palette.TextPrimary;
            if (_lblProgress != null) _lblProgress.ForeColor = palette.TextMuted;
            if (_txtStatus != null)
            {
                _txtStatus.BackColor = palette.InputBg;
                _txtStatus.ForeColor = palette.TextPrimary;
            }

            // 13. Complete Panel
            if (_pnlComplete != null) _pnlComplete.BackColor = palette.CanvasBg;
            if (_lblCompleteSubtitle != null) _lblCompleteSubtitle.ForeColor = palette.TextMuted;
            if (_pnlDetailsCard != null) _pnlDetailsCard.BackColor = palette.CardBg;
            if (_lblCompleteSummary != null) _lblCompleteSummary.ForeColor = palette.TextPrimary;

            this.Invalidate(true);
        }

        private static void SetMenuColors(ToolStripItemCollection items, AppTheme.ThemePalette palette)
        {
            foreach (ToolStripItem item in items)
            {
                item.ForeColor = palette.MenuText;
                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                {
                    menuItem.DropDown.BackColor = palette.MenuBg;
                    SetMenuColors(menuItem.DropDownItems, palette);
                }
            }
        }
    }
}
