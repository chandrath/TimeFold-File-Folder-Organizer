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
            if (_chkUseSourceAsOutput != null) _chkUseSourceAsOutput.ForeColor = palette.TextPrimary;
            if (_chkCreateSubfolder != null) _chkCreateSubfolder.ForeColor = palette.TextPrimary;
            if (_chkIncludeFolders != null) _chkIncludeFolders.ForeColor = palette.TextPrimary;
            UpdateOutputFolder();

            if (_btnBrowseOutput != null)
            {
                _btnBrowseOutput.BackColor = palette.SecondaryButtonBg;
                _btnBrowseOutput.ForeColor = palette.SecondaryButtonText;
                _btnBrowseOutput.BorderColor = palette.SecondaryButtonBorder;
            }

            // 8. Organization Plan Header, Refresh & ListView
            if (_lblPreviewHeader != null) _lblPreviewHeader.ForeColor = palette.TextPrimary;
            if (_btnRefresh != null)
            {
                _btnRefresh.BackColor = palette.SecondaryButtonBg;
                _btnRefresh.ForeColor = palette.SecondaryButtonText;
                _btnRefresh.BorderColor = palette.SecondaryButtonBorder;
            }
            if (_btnModeSelector != null)
            {
                _btnModeSelector.BackColor = palette.SecondaryButtonBg;
                _btnModeSelector.ForeColor = palette.SecondaryButtonText;
                _btnModeSelector.BorderColor = palette.SecondaryButtonBorder;
            }
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
                if (_filesToOrganize.Count > 0)
                {
                    UpdateFileList();
                }
            }

            if (_btnLoadMore != null)
            {
                _btnLoadMore.BackColor = isDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(241, 245, 249);
                _btnLoadMore.ForeColor = isDark ? Color.FromArgb(147, 197, 253) : Color.FromArgb(37, 99, 235);
                _btnLoadMore.BorderColor = isDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(203, 213, 225);
            }

            if (_ctxFileMenu != null)
            {
                _ctxFileMenu.Renderer = isDark ? AppTheme.DarkMenuRenderer : new ToolStripProfessionalRenderer();
                _ctxFileMenu.BackColor = palette.MenuBg;
                SetMenuColors(_ctxFileMenu.Items, palette);
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
            if (_lblSummary != null)
            {
                _lblSummary.ForeColor = (_filesToOrganize.Count > 0)
                    ? (isDark ? Color.FromArgb(52, 211, 153) : Color.FromArgb(4, 120, 87))
                    : palette.TextMuted;
            }

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

            if (_btnThemeToggle != null)
            {
                _btnThemeToggle.BackColor = palette.CardBg;
                _btnThemeToggle.BorderColor = palette.CardBorder;
                _toolTip?.SetToolTip(_btnThemeToggle, isDark ? "Switch to Light Mode" : "Switch to Dark Mode");
                _btnThemeToggle.Invalidate();
            }

            this.Invalidate(true);
        }

        private void BtnThemeToggle_Click(object? sender, EventArgs e)
        {
            _settings.DarkMode = !_settings.DarkMode;
            _settings.SaveToFile();
            ApplyTheme(_settings.DarkMode);
        }

        private void DrawThemeToggleIcon(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            float cx = bounds.X + bounds.Width / 2f;
            float cy = bounds.Y + bounds.Height / 2f;

            if (_settings.DarkMode)
            {
                // Crisp Vector Sun icon (radiant center + 8 rays)
                using var sunPen = new Pen(Color.FromArgb(251, 191, 36), 1.8f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
                using var sunBrush = new SolidBrush(Color.FromArgb(251, 191, 36));
                g.FillEllipse(sunBrush, cx - 4.5f, cy - 4.5f, 9f, 9f);
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4.0;
                    float x1 = cx + (float)(Math.Cos(angle) * 7.0);
                    float y1 = cy + (float)(Math.Sin(angle) * 7.0);
                    float x2 = cx + (float)(Math.Cos(angle) * 10.5);
                    float y2 = cy + (float)(Math.Sin(angle) * 10.5);
                    g.DrawLine(sunPen, x1, y1, x2, y2);
                }
            }
            else
            {
                // Crisp Vector Crescent Moon icon
                using var moonBrush = new SolidBrush(Color.FromArgb(71, 85, 105));
                using var pathOuter = new System.Drawing.Drawing2D.GraphicsPath();
                pathOuter.AddEllipse(cx - 7.5f, cy - 7.5f, 15f, 15f);
                using var pathInner = new System.Drawing.Drawing2D.GraphicsPath();
                pathInner.AddEllipse(cx - 3.2f, cy - 8f, 13.5f, 13.5f);
                using var moonRegion = new Region(pathOuter);
                moonRegion.Exclude(pathInner);
                g.FillRegion(moonBrush, moonRegion);
            }
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
