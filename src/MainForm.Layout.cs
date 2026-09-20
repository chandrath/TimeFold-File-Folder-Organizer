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
            _menuFileExit = new ToolStripMenuItem("Exit", null, MenuFileExit_Click);
            _menuFile.DropDownItems.AddRange(new ToolStripItem[] { _menuFileNew, new ToolStripSeparator(), _menuFileExit });

            _menuPreferences = new ToolStripMenuItem("Preferences");
            _menuPreferences.Click += MenuPreferences_Click;

            _menuHelp = new ToolStripMenuItem("Help");
            _menuAbout = new ToolStripMenuItem("About");
            _menuAbout.Click += MenuAbout_Click;
            _menuHelp.DropDownItems.Add(_menuAbout);

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
                using var pen = new Pen(AppConstants.ColorBorder, 1);
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
                Padding = new Padding(AppConstants.DefaultPadding + 6, 18, AppConstants.DefaultPadding + 6, 8),
                BackColor = Color.Transparent
            };

            var topLayout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _lblTitle = new Label
            {
                Text = AppConstants.AppName,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppConstants.ColorTextDark,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 8)
            };
            var lblSourceTitle = new Label { Text = "Source Folder", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(55, 65, 81), AutoSize = true, Margin = new Padding(0, 0, 0, 3) };

            var pnlSourceRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, Height = 36, Margin = new Padding(0, 0, 0, 4) };
            pnlSourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlSourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            pnlSourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114));

            _txtSourceFolder = new TextBox { Dock = DockStyle.Fill, Height = 32, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 2, 8, 2) };
            _btnBrowseSource = new ModernButton { Text = "Browse", Dock = DockStyle.Fill, BackColor = AppConstants.ColorBorder, ForeColor = Color.Black, BorderRadius = 6, Margin = new Padding(0, 0, 6, 0) };
            _btnBrowseSource.Click += BtnBrowseSource_Click;
            _btnUseCurrentFolder = new ModernButton { Text = "Use Current", Dock = DockStyle.Fill, BackColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White, BorderRadius = 6, Margin = Padding.Empty };
            _btnUseCurrentFolder.Click += BtnUseCurrentFolder_Click;
            pnlSourceRow.Controls.Add(_txtSourceFolder, 0, 0);
            pnlSourceRow.Controls.Add(_btnBrowseSource, 1, 0);
            pnlSourceRow.Controls.Add(_btnUseCurrentFolder, 2, 0);

            _pnlSourceDrop = new Panel { Dock = DockStyle.Top, Height = 34, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, AllowDrop = true, Margin = new Padding(0, 0, 0, 8) };
            var lblDropHint = new Label { Text = "📂 Drag and drop any folder here to select it as the source", Font = new Font("Segoe UI", 9F, FontStyle.Italic), ForeColor = Color.FromArgb(107, 114, 128), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
            _pnlSourceDrop.Controls.Add(lblDropHint);
            _pnlSourceDrop.DragEnter += PnlSourceDrop_DragEnter;
            _pnlSourceDrop.DragDrop += PnlSourceDrop_DragDrop;

            _chkUseSourceAsOutput = new CheckBox { Text = "Use source folder as output destination (default)", Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(55, 65, 81), AutoSize = true, Checked = true, Margin = new Padding(0, 0, 0, 4) };
            _chkUseSourceAsOutput.CheckedChanged += ChkUseSourceAsOutput_CheckedChanged;

            var pnlOutputRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, Height = 34, Margin = new Padding(0, 0, 0, 4) };
            pnlOutputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlOutputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            _txtOutputFolder = new TextBox { Dock = DockStyle.Fill, Height = 32, ReadOnly = true, BackColor = Color.FromArgb(243, 244, 246), BorderStyle = BorderStyle.FixedSingle, Enabled = false, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 1, 8, 1) };
            _btnBrowseOutput = new ModernButton { Text = "Browse", Dock = DockStyle.Fill, BackColor = AppConstants.ColorBorder, ForeColor = Color.Black, Enabled = false, BorderRadius = 6, Margin = Padding.Empty };
            _btnBrowseOutput.Click += BtnBrowseOutput_Click;
            pnlOutputRow.Controls.Add(_txtOutputFolder, 0, 0);
            pnlOutputRow.Controls.Add(_btnBrowseOutput, 1, 0);

            topLayout.Controls.Add(_lblTitle);
            topLayout.Controls.Add(lblSourceTitle);
            topLayout.Controls.Add(pnlSourceRow);
            topLayout.Controls.Add(_pnlSourceDrop);
            topLayout.Controls.Add(_chkUseSourceAsOutput);
            topLayout.Controls.Add(pnlOutputRow);
            _pnlTopSection.Controls.Add(topLayout);

            // 3. Center Elastic Preview Table
            _pnlCenterSection = new Panel { Dock = DockStyle.Fill, Padding = new Padding(AppConstants.DefaultPadding + 6, 4, AppConstants.DefaultPadding + 6, 8), BackColor = Color.Transparent };
            
            var pnlPreviewHeader = new Panel { Dock = DockStyle.Top, Height = 28, Margin = new Padding(0, 0, 0, 4) };
            var lblPreviewHeader = new Label
            {
                Text = "File Preview",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(55, 65, 81),
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lnkChangeFormat = new LinkLabel
            {
                Text = "Change format in Preferences",
                Font = new Font("Segoe UI", 8.5F),
                LinkColor = AppConstants.ColorPrimary,
                Dock = DockStyle.Right,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Cursor = Cursors.Hand,
                Padding = new Padding(4, 4, 0, 0)
            };
            _lnkChangeFormat.LinkClicked += (s, e) => MenuPreferences_Click(s, e);

            _lblFormatBadge = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = AppConstants.ColorPrimary,
                BackColor = Color.FromArgb(238, 242, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Right,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(0, 0, 8, 0)
            };

            pnlPreviewHeader.Controls.Add(_lblFormatBadge);
            pnlPreviewHeader.Controls.Add(_lnkChangeFormat);
            pnlPreviewHeader.Controls.Add(lblPreviewHeader);

            _pnlConflicts = new Panel { Dock = DockStyle.Top, Height = 42, BorderStyle = BorderStyle.FixedSingle, BackColor = AppConstants.ColorDangerBg, Visible = false, Padding = new Padding(8, 4, 8, 4), Margin = new Padding(0, 2, 0, 6) };
            _lblConflicts = new Label { Text = "⚠ Conflicts Detected:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = AppConstants.ColorDanger, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            _pnlConflicts.Controls.Add(_lblConflicts);

            _lstFiles = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            _lstFiles.Columns.Add("File Name", 320);
            _lstFiles.Columns.Add("Type", 85);
            _lstFiles.Columns.Add("Target Folder", 170);
            _lstFiles.Columns.Add("Modified Date", 135);
            _lstFiles.Columns.Add("Size", 85);
            _lstFiles.ColumnClick += LstFiles_ColumnClick;

            _pnlCenterSection.Controls.Add(_lstFiles);
            _pnlCenterSection.Controls.Add(_pnlConflicts);
            _pnlCenterSection.Controls.Add(pnlPreviewHeader);

            _pnlMain.Controls.Add(_pnlCenterSection);
            _pnlMain.Controls.Add(_pnlTopSection);
            _pnlMain.Controls.Add(_pnlFooterSection);
            _pnlCenterSection.BringToFront();

            // ==========================================
            // PROGRESS PANEL
            // ==========================================
            _pnlProgress = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(AppConstants.DefaultPadding + 20, 24, AppConstants.DefaultPadding + 20, 20), BackColor = AppConstants.ColorSurfaceBg };
            var lblProgressTitle = new Label { Text = "Organizing Files...", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = AppConstants.ColorTextDark, Dock = DockStyle.Top, Height = 40 };
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
            _pnlProgress.Controls.Add(lblProgressTitle);

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

            var lblCompleteTitle = new Label
            {
                Text = "✔ Organization Complete!",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = AppConstants.ColorSuccess,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            var lblCompleteSubtitle = new Label
            {
                Text = "All selected files and folders have been categorized into their respective date-based folders.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppConstants.ColorTextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 18)
            };

            // Details Card
            var pnlDetailsCard = new Panel
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
            pnlDetailsCard.Controls.Add(_lblCompleteSummary);

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

            completeContainer.Controls.Add(lblCompleteTitle);
            completeContainer.Controls.Add(lblCompleteSubtitle);
            completeContainer.Controls.Add(pnlDetailsCard);
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

            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        private void MainForm_Load(object? sender, EventArgs e) => UpdateMainLayout();

        private void UpdateMainLayout()
        {
            if (_lstFiles != null && _lstFiles.Columns.Count >= 5)
            {
                int scrollbarWidth = SystemInformation.VerticalScrollBarWidth;
                int otherCols = 85 + 170 + 135 + 85 + scrollbarWidth + 6;
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
