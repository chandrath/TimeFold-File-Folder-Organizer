using System;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Controls;

namespace FileOrganizer
{
    public partial class MainForm
    {
        private Panel _pnlTopSection = null!;
        private Panel _pnlCenterSection = null!;
        private Panel _pnlFooterSection = null!;

        private void InitializeComponent()
        {
            this.Text = AppConstants.AppName;
            if (AppConstants.AppIcon != null) this.Icon = AppConstants.AppIcon;
            this.Size = new Size(960, 720);
            this.MinimumSize = new Size(820, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.BackColor = AppConstants.ColorSurfaceBg;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            // Menu Bar (Pinned to Top)
            _menuStrip = new MenuStrip
            {
                BackColor = Color.White,
                RenderMode = ToolStripRenderMode.System,
                Dock = DockStyle.Top
            };
            _menuFile = new ToolStripMenuItem("File");
            _menuFileNew = new ToolStripMenuItem("New", null, MenuFileNew_Click);
            _menuFileRecent = new ToolStripMenuItem("Recent Folders");
            _menuFileExit = new ToolStripMenuItem("Exit", null, MenuFileExit_Click);
            _menuFile.DropDownItems.AddRange(new ToolStripItem[] { _menuFileNew, _menuFileRecent, new ToolStripSeparator(), _menuFileExit });

            _menuPreferences = new ToolStripMenuItem("Preferences");
            _menuPreferences.Click += MenuPreferences_Click;

            _menuHelp = new ToolStripMenuItem("Help");
            var menuTour = new ToolStripMenuItem("💡 Quick Tour & Guide...", null, MenuWelcomeTour_Click);
            _menuAbout = new ToolStripMenuItem("About", null, MenuAbout_Click);
            _menuHelp.DropDownItems.AddRange(new ToolStripItem[] { menuTour, new ToolStripSeparator(), _menuAbout });

            _menuStrip.Items.AddRange(new ToolStripItem[] { _menuFile, _menuPreferences, _menuHelp });
            this.MainMenuStrip = _menuStrip;

            // ==========================================
            // MAIN PANEL (Contains Top, Center, Bottom)
            // ==========================================
            _pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = AppConstants.ColorSurfaceBg };

            // 1. Pinned Footer Section
            _pnlFooterSection = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                Padding = new Padding(AppConstants.DefaultPadding + 6, 10, AppConstants.DefaultPadding + 6, 10),
                BackColor = Color.White
            };
            _pnlFooterSection.Paint += (s, pe) =>
            {
                var palette = AppTheme.GetPalette(_settings.DarkMode);
                using var pen = new Pen(palette.CardBorder, 1);
                pe.Graphics.DrawLine(pen, 0, 0, _pnlFooterSection.Width, 0);
            };

            _lblSummary = new Label
            {
                Text = "Select a source folder to preview files.",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = AppConstants.ColorTextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            _btnStart = new ModernButton
            {
                Text = "Start Organization",
                Size = new Size(220, 46),
                Dock = DockStyle.Right,
                BackColor = AppConstants.ColorPrimary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                BorderRadius = 10,
                Cursor = Cursors.Hand
            };
            _btnStart.Click += BtnStart_Click;
            _pnlFooterSection.Controls.Add(_lblSummary);
            _pnlFooterSection.Controls.Add(_btnStart);

            // 2. Top Header & Folder Configuration
            _pnlTopSection = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(AppConstants.DefaultPadding + 6, 14, AppConstants.DefaultPadding + 6, 8),
                BackColor = Color.Transparent
            };

            var topLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Brand Header with App Logo, App Name, and Tagline
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 46, Margin = new Padding(0, 0, 0, 8) };
            var picLogo = new PictureBox
            {
                Size = new Size(38, 38),
                Location = new Point(0, 3),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = AppConstants.AppLogo
            };
            _lblTitle = new Label
            {
                Text = AppConstants.AppName,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = AppConstants.ColorTextDark,
                Location = new Point(44, 0),
                AutoSize = true
            };
            _lblSubtitle = new Label
            {
                Text = AppConstants.AppTagline,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = AppConstants.ColorTextMuted,
                Location = new Point(46, 25),
                AutoSize = true
            };
            pnlHeader.Controls.AddRange([picLogo, _lblTitle, _lblSubtitle]);

            _lblSourceTitle = new Label { Text = "Source Folder", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(55, 65, 81), AutoSize = true, Margin = new Padding(0, 0, 0, 3) };

            var pnlSourceRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, Height = 36, Margin = new Padding(0, 0, 0, 4) };
            pnlSourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlSourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            pnlSourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102));
            pnlSourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            _txtSourceFolder = new TextBox { Dock = DockStyle.Fill, Height = 32, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5F), PlaceholderText = "Select, drop, or pick a recent folder...", Margin = new Padding(0, 2, 8, 2) };
            _btnBrowseSource = new ModernButton { Text = "📁 Browse", Dock = DockStyle.Fill, BackColor = Color.White, BorderColor = Color.FromArgb(209, 213, 219), ForeColor = Color.FromArgb(30, 41, 59), BorderRadius = 6, Margin = new Padding(0, 0, 6, 0) };
            _btnBrowseSource.Click += BtnBrowseSource_Click;
            _btnRecentFolders = new ModernButton { Text = "🕒 Recent ▾", Dock = DockStyle.Fill, BackColor = Color.White, BorderColor = Color.FromArgb(209, 213, 219), ForeColor = Color.FromArgb(30, 41, 59), BorderRadius = 6, Margin = new Padding(0, 0, 6, 0) };
            _btnRecentFolders.Click += BtnRecentFolders_Click;
            _btnUseCurrentFolder = new ModernButton { Text = "⚡ Use Current", Dock = DockStyle.Fill, BackColor = AppConstants.ColorPrimary, BorderColor = AppConstants.ColorPrimary, ForeColor = Color.White, BorderRadius = 6, Margin = Padding.Empty };
            _btnUseCurrentFolder.Click += BtnUseCurrentFolder_Click;
            pnlSourceRow.Controls.Add(_txtSourceFolder, 0, 0);
            pnlSourceRow.Controls.Add(_btnBrowseSource, 1, 0);
            pnlSourceRow.Controls.Add(_btnRecentFolders, 2, 0);
            pnlSourceRow.Controls.Add(_btnUseCurrentFolder, 3, 0);

            _pnlSourceDrop = new Panel { Dock = DockStyle.Top, Height = 48, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(240, 247, 255), AllowDrop = true, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 8) };
            _lblDropHint = new Label { Text = "📥 Drag & Drop any folder or files here to inspect  (or click Browse)", UseMnemonic = false, Font = new Font("Segoe UI Emoji", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Cursor = Cursors.Hand };
            _pnlSourceDrop.Controls.Add(_lblDropHint);
            _pnlSourceDrop.Click += (s, e) => BtnBrowseSource_Click(s, e);
            _lblDropHint.Click += (s, e) => BtnBrowseSource_Click(s, e);
            _pnlSourceDrop.DragEnter += PnlSourceDrop_DragEnter;
            _pnlSourceDrop.DragLeave += PnlSourceDrop_DragLeave;
            _pnlSourceDrop.DragDrop += PnlSourceDrop_DragDrop;
            _pnlSourceDrop.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var pal = AppTheme.GetPalette(_settings.DarkMode);
                using var pen = new Pen(pal.DropZoneBorder, 1.75f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                pe.Graphics.DrawRectangle(pen, 1, 1, _pnlSourceDrop.Width - 3, _pnlSourceDrop.Height - 3);
            };

            _chkUseSourceAsOutput = new CheckBox { Text = "Use source folder as output destination (default)", Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(55, 65, 81), AutoSize = true, Checked = true, Margin = new Padding(0, 0, 0, 4) };
            _chkUseSourceAsOutput.CheckedChanged += ChkUseSourceAsOutput_CheckedChanged;

            var pnlOutputRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, Height = 34, Margin = new Padding(0, 0, 0, 4) };
            pnlOutputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlOutputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            _txtOutputFolder = new TextBox { Dock = DockStyle.Fill, Height = 32, ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), BorderStyle = BorderStyle.FixedSingle, Enabled = true, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 1, 8, 1) };
            _btnBrowseOutput = new ModernButton { Text = "📁 Browse", Dock = DockStyle.Fill, BackColor = Color.White, BorderColor = Color.FromArgb(209, 213, 219), ForeColor = Color.FromArgb(30, 41, 59), Enabled = false, BorderRadius = 6, Margin = Padding.Empty };
            _btnBrowseOutput.Click += BtnBrowseOutput_Click;
            pnlOutputRow.Controls.Add(_txtOutputFolder, 0, 0);
            pnlOutputRow.Controls.Add(_btnBrowseOutput, 1, 0);

            _chkIncludeFolders = new CheckBox { Text = "Include folders (move whole folders alongside files)", Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(55, 65, 81), AutoSize = true, Checked = _settings.IncludeTopLevelFolders, Margin = new Padding(0, 2, 0, 4) };
            _chkIncludeFolders.CheckedChanged += ChkIncludeFolders_CheckedChanged;

            topLayout.Controls.AddRange(new Control[] { pnlHeader, _lblSourceTitle, pnlSourceRow, _pnlSourceDrop, _chkUseSourceAsOutput, pnlOutputRow, _chkIncludeFolders });
            _pnlTopSection.Controls.Add(topLayout);

            // 3. Center Elastic Preview Table
            _pnlCenterSection = new Panel { Dock = DockStyle.Fill, Padding = new Padding(AppConstants.DefaultPadding + 6, 4, AppConstants.DefaultPadding + 6, 8), BackColor = Color.Transparent };
            
            var pnlPreviewHeader = new Panel { Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 0, 0, 4) };
            _lblPreviewHeader = new Label
            {
                Text = "Organization Plan",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblFormatBadge = new ModernButton
            {
                Text = "📁 Format ⚙",
                Font = new Font("Segoe UI Emoji", 8.5F, FontStyle.Bold),
                ForeColor = AppConstants.ColorBadgeText,
                BackColor = AppConstants.ColorBadgeBg,
                BorderColor = AppConstants.ColorBadgeBorder,
                BorderRadius = 12,
                Dock = DockStyle.Right,
                AutoSize = true,
                Height = 26,
                Padding = new Padding(8, 0, 8, 0),
                Cursor = Cursors.Hand,
                Visible = (_settings.OrgMode != Models.OrganizationMode.Category && _settings.OrgMode != Models.OrganizationMode.Extension)
            };
            _lblFormatBadge.Click += (s, e) => MenuPreferences_Click(s, e);

            _btnModeSelector = new ModernButton
            {
                Text = GetModeSelectorText(),
                Font = new Font("Segoe UI Emoji", 8.5F, FontStyle.Bold),
                ForeColor = AppConstants.ColorBadgeText,
                BackColor = AppConstants.ColorBadgeBg,
                BorderColor = AppConstants.ColorBadgeBorder,
                BorderRadius = 12,
                Dock = DockStyle.Right,
                AutoSize = true,
                Height = 26,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            _btnModeSelector.Click += BtnModeSelector_Click;

            _btnRefresh = new ModernButton
            {
                Text = "🔄 Refresh",
                Font = new Font("Segoe UI Emoji", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                BackColor = Color.White,
                BorderColor = Color.FromArgb(209, 213, 219),
                BorderRadius = 12,
                Dock = DockStyle.Right,
                AutoSize = true,
                Height = 26,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            _btnRefresh.Click += (s, e) => LoadPreview();

            pnlPreviewHeader.Controls.AddRange([_btnRefresh, _btnModeSelector, _lblFormatBadge, _lblPreviewHeader]);

            _pnlConflicts = new Panel { Dock = DockStyle.Top, Height = 42, BorderStyle = BorderStyle.FixedSingle, BackColor = AppConstants.ColorDangerBg, Visible = false, Padding = new Padding(8, 4, 8, 4), Margin = new Padding(0, 2, 0, 6) };
            _lblConflicts = new Label { Text = "⚠ Conflicts Detected:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = AppConstants.ColorDanger, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            _pnlConflicts.Controls.Add(_lblConflicts);

            _pnlTimestampWarning = new Panel { Dock = DockStyle.Top, Height = 36, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(254, 243, 199), Visible = false, Padding = new Padding(8, 4, 8, 4), Margin = new Padding(0, 2, 0, 4) };
            _lblTimestampWarning = new Label { Text = "", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(146, 64, 14), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            _pnlTimestampWarning.Controls.Add(_lblTimestampWarning);

            _lstFiles = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Font = new Font("Segoe UI", 9F), Visible = false };
            _lstFiles.Columns.AddRange([new ColumnHeader { Text = "File Name", Width = 260 }, new ColumnHeader { Text = "Type", Width = 70 }, new ColumnHeader { Text = "Modified Date", Width = 140 }, new ColumnHeader { Text = "Created Date", Width = 140 }, new ColumnHeader { Text = "Target Folder", Width = 160 }, new ColumnHeader { Text = "Size", Width = 75 }]);
            _lstFiles.ColumnClick += LstFiles_ColumnClick;
            _lstFiles.Click += LstFiles_Click;

            _pnlLoadMore = new Panel { Dock = DockStyle.Bottom, Height = 34, Visible = false, Padding = new Padding(0, 4, 0, 0) };
            _btnLoadMore = new ModernButton { Text = "➕ Load 1,000 More", Dock = DockStyle.Fill, BorderRadius = 6, Cursor = Cursors.Hand };
            _btnLoadMore.Click += BtnLoadMore_Click;
            _pnlLoadMore.Controls.Add(_btnLoadMore);

            // Empty State Card
            _pnlEmptyState = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Visible = true };
            var pnlEmptyCenter = new Panel { Size = new Size(540, 130), BackColor = Color.Transparent };
            _lblEmptyIcon = new Label { Text = "📂", Font = new Font("Segoe UI Emoji", 26F), ForeColor = Color.FromArgb(148, 163, 184), Dock = DockStyle.Top, Height = 48, TextAlign = ContentAlignment.BottomCenter };
            _lblEmptyTitle = new Label { Text = "No Files Loaded Yet", Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter };
            _lblEmptyDesc = new Label { Text = "Choose a source folder above or drag and drop a folder to preview file organization.", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.TopCenter };
            pnlEmptyCenter.Controls.AddRange([_lblEmptyDesc, _lblEmptyTitle, _lblEmptyIcon]);
            _pnlEmptyState.Controls.Add(pnlEmptyCenter);
            _pnlEmptyState.Resize += (s, e) =>
            {
                pnlEmptyCenter.Location = new Point(
                    Math.Max(0, (_pnlEmptyState.Width - pnlEmptyCenter.Width) / 2),
                    Math.Max(20, (_pnlEmptyState.Height - pnlEmptyCenter.Height) / 2 - 20)
                );
            };

            _pnlCenterSection.Controls.AddRange([_pnlEmptyState, _lstFiles, _pnlLoadMore, _pnlTimestampWarning, _pnlConflicts, pnlPreviewHeader]);
            _pnlLoadMore.SendToBack();
            _lstFiles.BringToFront();
            _pnlEmptyState.BringToFront();

            _pnlMain.Controls.Add(_pnlCenterSection);
            _pnlMain.Controls.Add(_pnlTopSection);
            _pnlMain.Controls.Add(_pnlFooterSection);
            _pnlCenterSection.BringToFront();

            // ==========================================
            // PROGRESS PANEL
            // ==========================================
            _pnlProgress = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(AppConstants.DefaultPadding + 20, 24, AppConstants.DefaultPadding + 20, 20), BackColor = AppConstants.ColorSurfaceBg };
            _lblProgressTitle = new Label { Text = "Organizing Files...", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = AppConstants.ColorTextDark, Dock = DockStyle.Top, Height = 40 };
            _progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 22, Style = ProgressBarStyle.Continuous, Margin = new Padding(0, 10, 0, 10) };
            _lblProgress = new Label { Text = "Initializing...", Font = new Font("Segoe UI", 10F), Dock = DockStyle.Top, Height = 30 };
            _txtStatus = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9F), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };

            var pnlProgressFooter = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(0, 8, 0, 0) };
            _btnCancel = new ModernButton { Text = "Cancel", Size = new Size(120, 38), Dock = DockStyle.Right, BackColor = AppConstants.ColorDanger, ForeColor = Color.White, BorderRadius = 8 };
            _btnCancel.Click += BtnCancel_Click;
            pnlProgressFooter.Controls.Add(_btnCancel);

            _pnlProgress.Controls.Add(_txtStatus);
            _pnlProgress.Controls.Add(pnlProgressFooter);
            _pnlProgress.Controls.Add(_lblProgress);
            _pnlProgress.Controls.Add(_progressBar);
            _pnlProgress.Controls.Add(_lblProgressTitle);

            // ==========================================
            // COMPLETE PANEL (Modern Executive Dashboard)
            // ==========================================
            _pnlComplete = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(AppConstants.DefaultPadding + 24, 28, AppConstants.DefaultPadding + 24, 20), BackColor = AppConstants.ColorSurfaceBg, AutoScroll = true };

            var completeContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            completeContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _lblCompleteTitle = new Label
            {
                Text = "✔ Organization Complete!",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = AppConstants.ColorSuccess,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            _lblCompleteSubtitle = new Label
            {
                Text = "All selected files and folders have been categorized into their respective date-based folders.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppConstants.ColorTextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 18)
            };

            // Details Card
            _pnlDetailsCard = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Padding = new Padding(16, 14, 16, 14),
                Margin = new Padding(0, 0, 0, 20)
            };

            _lblCompleteSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppConstants.ColorTextDark,
                AutoSize = true,
                Dock = DockStyle.Top
            };
            _pnlDetailsCard.Controls.Add(_lblCompleteSummary);

            // Action Buttons Container (Side-by-Side)
            _pnlCompleteActions = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Margin = new Padding(0, 4, 0, 10)
            };

            _btnOpenFolder = new ModernButton
            {
                Text = "📂 Open Output Folder",
                Size = new Size(230, 46),
                BackColor = AppConstants.ColorPrimary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BorderRadius = 10,
                Cursor = Cursors.Hand
            };
            _btnOpenFolder.Click += BtnOpenFolder_Click;

            _btnStartNewProject = new ModernButton
            {
                Text = "🔄 Organize Another Folder",
                Size = new Size(230, 46),
                BackColor = AppConstants.ColorSuccess,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BorderRadius = 10,
                Cursor = Cursors.Hand
            };
            _btnStartNewProject.Click += BtnStartNewProject_Click;

            _pnlCompleteActions.Resize += (s, e) => CenterCompletionButtons();
            _pnlCompleteActions.Controls.Add(_btnOpenFolder);
            _pnlCompleteActions.Controls.Add(_btnStartNewProject);

            completeContainer.Controls.Add(_lblCompleteTitle);
            completeContainer.Controls.Add(_lblCompleteSubtitle);
            completeContainer.Controls.Add(_pnlDetailsCard);
            completeContainer.Controls.Add(_pnlCompleteActions);

            _pnlComplete.Controls.Add(completeContainer);

            // Form Controls Registration & Z-Ordering
            this.Controls.Add(_pnlMain);
            this.Controls.Add(_pnlProgress);
            this.Controls.Add(_pnlComplete);
            this.Controls.Add(_menuStrip);

            // Send MenuStrip to back so it reserves the top 24px and never overlaps content
            _menuStrip.SendToBack();
            _pnlMain.BringToFront();

            InitializeContextMenu();
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F5)
                {
                    e.Handled = true;
                    LoadPreview();
                }
            };

            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        private void MainForm_Load(object? sender, EventArgs e) => UpdateMainLayout();

        private void UpdateMainLayout()
        {
            if (_lstFiles != null && _lstFiles.Columns.Count >= 6)
            {
                int scrollbarWidth = SystemInformation.VerticalScrollBarWidth;
                int otherCols = 70 + 140 + 140 + 160 + 75 + scrollbarWidth + 6;
                int availableNameWidth = Math.Max(150, _lstFiles.ClientSize.Width - otherCols);
                _lstFiles.Columns[0].Width = availableNameWidth;
            }

            CenterCompletionButtons();
        }

        private void CenterCompletionButtons()
        {
            if (_pnlCompleteActions == null || _btnOpenFolder == null || _btnStartNewProject == null) return;
            int totalWidth = _btnOpenFolder.Width + 16 + _btnStartNewProject.Width;
            int availableWidth = _pnlCompleteActions.ClientSize.Width;
            if (availableWidth <= 0) return;

            if (availableWidth >= totalWidth)
            {
                int startX = Math.Max(0, (availableWidth - totalWidth) / 2);
                _btnOpenFolder.Location = new Point(startX, 6);
                _btnStartNewProject.Location = new Point(startX + _btnOpenFolder.Width + 16, 6);
                _pnlCompleteActions.Height = 58;
            }
            else
            {
                int startX = Math.Max(0, (availableWidth - _btnOpenFolder.Width) / 2);
                _btnOpenFolder.Location = new Point(startX, 0);
                _btnStartNewProject.Location = new Point(startX, _btnOpenFolder.Bottom + 10);
                _pnlCompleteActions.Height = 110;
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e) => UpdateMainLayout();
    }
}
