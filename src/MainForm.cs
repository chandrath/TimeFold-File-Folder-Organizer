using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FileOrganizer.Models;
using FileOrganizer.Services;

namespace FileOrganizer
{
    public partial class MainForm : Form
    {
        private FileOrganizerService? _organizerService;
        private List<FileItem> _filesToOrganize = new List<FileItem>();
        private CancellationTokenSource? _cancellationTokenSource;
        private string _executablePath;
        private string _executableDirectory;
        
        // UI Controls
        private MenuStrip _menuStrip = null!;
        private ToolStripMenuItem _menuFile = null!;
        private ToolStripMenuItem _menuFileNew = null!;
        private ToolStripMenuItem _menuFileExit = null!;
        private ToolStripMenuItem _menuHelp = null!;
        private ToolStripMenuItem _menuAbout = null!;
        
        private Label _lblTitle = null!;
        private TextBox _txtSourceFolder = null!;
        private Button _btnBrowseSource = null!;
        private Button _btnUseCurrentFolder = null!;
        private Panel _pnlSourceDrop = null!;
        private CheckBox _chkUseSourceAsOutput = null!;
        private TextBox _txtOutputFolder = null!;
        private Button _btnBrowseOutput = null!;
        private Button _btnStart = null!;
        private CheckBox _chkIncludeFolders = null!;
        private CheckBox _chkShowProgress = null!;
        private ListView _lstFiles = null!;
        private Label _lblSummary = null!;
        private Label _lblConflicts = null!;
        private Panel _pnlConflicts = null!;
        private ProgressBar _progressBar = null!;
        private Label _lblProgress = null!;
        private TextBox _txtStatus = null!;
        private Panel _pnlMain = null!;
        private Panel _pnlProgress = null!;
        private Panel _pnlComplete = null!;
        private Label _lblCompleteSummary = null!;
        private Button _btnOpenFolder = null!;
        private Button _btnStartNewProject = null!;
        private Button _btnClose = null!;
        private Button _btnCancel = null!;
        
        private const int PADDING = 15;
        
        public MainForm()
        {
            _executablePath = Application.ExecutablePath;
            _executableDirectory = Path.GetDirectoryName(_executablePath) ?? Environment.CurrentDirectory;
            InitializeComponent();
            InitializeOrganizer();
            LoadPreview();
        }
        
        private void InitializeComponent()
        {
            this.Text = "File Organizer by Date";
            this.Size = new Size(900, 800);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.Padding = new Padding(PADDING);
            
            // Menu Bar
            _menuStrip = new MenuStrip();
            
            // File Menu
            _menuFile = new ToolStripMenuItem("File");
            _menuFileNew = new ToolStripMenuItem("New", null, MenuFileNew_Click);
            _menuFile.DropDownItems.Add(_menuFileNew);
            _menuFile.DropDownItems.Add(new ToolStripSeparator());
            _menuFileExit = new ToolStripMenuItem("Exit", null, MenuFileExit_Click);
            _menuFile.DropDownItems.Add(_menuFileExit);
            
            // Help Menu
            _menuHelp = new ToolStripMenuItem("Help");
            _menuAbout = new ToolStripMenuItem("About");
            _menuAbout.Click += MenuAbout_Click;
            _menuHelp.DropDownItems.Add(_menuAbout);
            
            _menuStrip.Items.Add(_menuFile);
            _menuStrip.Items.Add(_menuHelp);
            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip);
            
            // Main panel with scroll support
            _pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(PADDING)
            };
            
            // Title
            _lblTitle = new Label
            {
                Text = "📁 File Organizer by Date",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102),
                AutoSize = true,
                Location = new Point(PADDING, PADDING + 25)
            };
            
            // Source Folder Selection
            var lblSourceFolderTitle = new Label
            {
                Text = "Source Folder (to organize):",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(PADDING, _lblTitle.Bottom + 20),
                AutoSize = true
            };
            
            _txtSourceFolder = new TextBox
            {
                Location = new Point(PADDING, lblSourceFolderTitle.Bottom + 8),
                Size = new Size(650, 25),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            _btnBrowseSource = new Button
            {
                Text = "Browse...",
                Size = new Size(85, 27),
                Location = new Point(_txtSourceFolder.Right + 8, _txtSourceFolder.Top - 1),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            _btnBrowseSource.FlatAppearance.BorderColor = Color.Gray;
            _btnBrowseSource.Click += BtnBrowseSource_Click;
            
            _btnUseCurrentFolder = new Button
            {
                Text = "📂 Use Current Folder",
                Size = new Size(140, 27),
                Location = new Point(_btnBrowseSource.Right + 8, _txtSourceFolder.Top - 1),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnUseCurrentFolder.FlatAppearance.BorderSize = 0;
            _btnUseCurrentFolder.Click += BtnUseCurrentFolder_Click;
            
            // Drag & Drop Panel for Source
            _pnlSourceDrop = new Panel
            {
                Location = new Point(PADDING, _txtSourceFolder.Bottom + 10),
                Size = new Size(850, 45),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 248, 248),
                AllowDrop = true
            };
            
            var lblDropHint = new Label
            {
                Text = "📁 Drag and drop a folder here (folders only)",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(10, 12),
                AutoSize = true
            };
            _pnlSourceDrop.Controls.Add(lblDropHint);
            _pnlSourceDrop.DragEnter += PnlSourceDrop_DragEnter;
            _pnlSourceDrop.DragDrop += PnlSourceDrop_DragDrop;
            
            // Output Folder Selection
            var lblOutputFolderTitle = new Label
            {
                Text = "Output Folder (where Sorted folder will be created):",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(PADDING, _pnlSourceDrop.Bottom + 20),
                AutoSize = true
            };
            
            _chkUseSourceAsOutput = new CheckBox
            {
                Text = "Use source folder as output (default)",
                Font = new Font("Segoe UI", 9),
                Location = new Point(PADDING, lblOutputFolderTitle.Bottom + 10),
                AutoSize = true,
                Checked = true
            };
            _chkUseSourceAsOutput.CheckedChanged += ChkUseSourceAsOutput_CheckedChanged;
            
            _txtOutputFolder = new TextBox
            {
                Location = new Point(PADDING, _chkUseSourceAsOutput.Bottom + 10),
                Size = new Size(650, 25),
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };
            
            _btnBrowseOutput = new Button
            {
                Text = "Browse...",
                Size = new Size(85, 27),
                Location = new Point(_txtOutputFolder.Right + 8, _txtOutputFolder.Top - 1),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 240, 240),
                Enabled = false
            };
            _btnBrowseOutput.FlatAppearance.BorderColor = Color.Gray;
            _btnBrowseOutput.Click += BtnBrowseOutput_Click;
            
            // Options panel
            var pnlOptions = new Panel
            {
                Location = new Point(PADDING, _txtOutputFolder.Bottom + 20),
                Size = new Size(850, 70),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(252, 252, 252)
            };
            
            var lblOptions = new Label
            {
                Text = "Options",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            
            _chkIncludeFolders = new CheckBox
            {
                Text = "Include Top-Level Folders (Move entire folders, contents inside will not be touched)",
                Location = new Point(10, 35),
                AutoSize = true,
                Checked = false
            };
            
            _chkShowProgress = new CheckBox
            {
                Text = "Show Detailed Progress",
                Location = new Point(10, 55),
                AutoSize = true,
                Checked = true
            };
            
            pnlOptions.Controls.Add(lblOptions);
            pnlOptions.Controls.Add(_chkIncludeFolders);
            pnlOptions.Controls.Add(_chkShowProgress);
            
            // Files list
            var lblFiles = new Label
            {
                Text = "Files to Organize (Preview)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(PADDING, pnlOptions.Bottom + 20),
                AutoSize = true
            };
            
            _lstFiles = new ListView
            {
                Location = new Point(PADDING, lblFiles.Bottom + 8),
                Size = new Size(850, 220),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            _lstFiles.Columns.Add("File Name", 380);
            _lstFiles.Columns.Add("Month Folder", 220);
            _lstFiles.Columns.Add("Modified Date", 150);
            _lstFiles.Columns.Add("Size", 100);
            
            // Summary label
            _lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                Location = new Point(PADDING, _lstFiles.Bottom + 12),
                AutoSize = true
            };
            
            // Conflicts panel
            _pnlConflicts = new Panel
            {
                Location = new Point(PADDING, _lblSummary.Bottom + 10),
                Size = new Size(850, 70),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(255, 250, 240),
                Visible = false
            };
            
            _lblConflicts = new Label
            {
                Text = "⚠ Conflicts Detected:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.OrangeRed,
                Location = new Point(10, 10),
                Size = new Size(830, 50),
                AutoSize = false
            };
            
            _pnlConflicts.Controls.Add(_lblConflicts);
            
            // Buttons
            _btnStart = new Button
            {
                Text = "Start Organization",
                Size = new Size(160, 38),
                Location = new Point(700, _pnlConflicts.Bottom + 15),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += BtnStart_Click;
            
            _pnlMain.Controls.Add(_lblTitle);
            _pnlMain.Controls.Add(lblSourceFolderTitle);
            _pnlMain.Controls.Add(_txtSourceFolder);
            _pnlMain.Controls.Add(_btnBrowseSource);
            _pnlMain.Controls.Add(_btnUseCurrentFolder);
            _pnlMain.Controls.Add(_pnlSourceDrop);
            _pnlMain.Controls.Add(lblOutputFolderTitle);
            _pnlMain.Controls.Add(_chkUseSourceAsOutput);
            _pnlMain.Controls.Add(_txtOutputFolder);
            _pnlMain.Controls.Add(_btnBrowseOutput);
            _pnlMain.Controls.Add(pnlOptions);
            _pnlMain.Controls.Add(lblFiles);
            _pnlMain.Controls.Add(_lstFiles);
            _pnlMain.Controls.Add(_lblSummary);
            _pnlMain.Controls.Add(_pnlConflicts);
            _pnlMain.Controls.Add(_btnStart);
            
            // Progress panel
            _pnlProgress = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(PADDING)
            };
            
            var lblProgressTitle = new Label
            {
                Text = "📁 Organizing Files...",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(PADDING, PADDING + 25),
                AutoSize = true
            };
            
            _progressBar = new ProgressBar
            {
                Location = new Point(PADDING, lblProgressTitle.Bottom + 20),
                Size = new Size(850, 35),
                Style = ProgressBarStyle.Continuous
            };
            
            _lblProgress = new Label
            {
                Text = "Initializing...",
                Font = new Font("Segoe UI", 10),
                Location = new Point(PADDING, _progressBar.Bottom + 12),
                AutoSize = true
            };
            
            _txtStatus = new TextBox
            {
                Location = new Point(PADDING, _lblProgress.Bottom + 15),
                Size = new Size(850, 450),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 35),
                Location = new Point(740, _txtStatus.Bottom + 15),
                BackColor = Color.OrangeRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += BtnCancel_Click;
            
            _pnlProgress.Controls.Add(lblProgressTitle);
            _pnlProgress.Controls.Add(_progressBar);
            _pnlProgress.Controls.Add(_lblProgress);
            _pnlProgress.Controls.Add(_txtStatus);
            _pnlProgress.Controls.Add(_btnCancel);
            
            // Complete panel
            _pnlComplete = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Padding = new Padding(PADDING)
            };
            
            var lblCompleteTitle = new Label
            {
                Text = "✅ Organization Complete!",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.Green,
                Location = new Point(PADDING, PADDING + 25),
                AutoSize = true
            };
            
            _lblCompleteSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10),
                Location = new Point(PADDING, lblCompleteTitle.Bottom + 20),
                Size = new Size(850, 450),
                AutoSize = false
            };
            
            _btnOpenFolder = new Button
            {
                Text = "Open Folder",
                Size = new Size(130, 38),
                Location = new Point(450, _lblCompleteSummary.Bottom + 20),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOpenFolder.FlatAppearance.BorderSize = 0;
            _btnOpenFolder.Click += BtnOpenFolder_Click;
            
            _btnStartNewProject = new Button
            {
                Text = "Start New Project",
                Size = new Size(150, 38),
                Location = new Point(590, _lblCompleteSummary.Bottom + 20),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnStartNewProject.FlatAppearance.BorderSize = 0;
            _btnStartNewProject.Click += BtnStartNewProject_Click;
            
            _btnClose = new Button
            {
                Text = "Close",
                Size = new Size(110, 38),
                Location = new Point(750, _lblCompleteSummary.Bottom + 20),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => this.Close();
            
            _pnlComplete.Controls.Add(lblCompleteTitle);
            _pnlComplete.Controls.Add(_lblCompleteSummary);
            _pnlComplete.Controls.Add(_btnOpenFolder);
            _pnlComplete.Controls.Add(_btnStartNewProject);
            _pnlComplete.Controls.Add(_btnClose);
            
            this.Controls.Add(_pnlMain);
            this.Controls.Add(_pnlProgress);
            this.Controls.Add(_pnlComplete);
            
            // Handle window resize
            this.Resize += MainForm_Resize;
        }
        
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            // Adjust controls on resize for better responsiveness
            if (_pnlMain.Visible)
            {
                var availableWidth = this.ClientSize.Width - (PADDING * 2);
                _txtSourceFolder.Width = Math.Max(400, availableWidth - 250);
                _txtOutputFolder.Width = Math.Max(400, availableWidth - 250);
                _pnlSourceDrop.Width = Math.Max(400, availableWidth);
                _lstFiles.Width = Math.Max(400, availableWidth);
                _pnlConflicts.Width = Math.Max(400, availableWidth);
                _btnStart.Left = Math.Max(600, this.ClientSize.Width - 180);
            }
        }
        
        private void MenuFileNew_Click(object? sender, EventArgs e)
        {
            ResetForm();
        }
        
        private void MenuFileExit_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
        
        private void ResetForm()
        {
            _txtSourceFolder.Text = "";
            _txtOutputFolder.Text = "";
            _chkUseSourceAsOutput.Checked = true;
            _chkIncludeFolders.Checked = false;
            _chkShowProgress.Checked = true;
            _filesToOrganize.Clear();
            _lstFiles.Items.Clear();
            _lblSummary.Text = "";
            _pnlConflicts.Visible = false;
            _organizerService = null;
            _pnlMain.Visible = true;
            _pnlProgress.Visible = false;
            _pnlComplete.Visible = false;
        }
        
        private void InitializeOrganizer()
        {
            // Default to executable directory
            SetSourceFolder(_executableDirectory);
            UpdateOutputFolder();
        }
        
        private void SetSourceFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                _txtSourceFolder.Text = folder;
                _organizerService = new FileOrganizerService(_executablePath, folder);
                UpdateOutputFolder();
                LoadPreview();
            }
        }
        
        private void UpdateOutputFolder()
        {
            if (_chkUseSourceAsOutput.Checked)
            {
                if (!string.IsNullOrEmpty(_txtSourceFolder.Text) && Directory.Exists(_txtSourceFolder.Text))
                {
                    _txtOutputFolder.Text = _txtSourceFolder.Text;
                    _txtOutputFolder.BackColor = Color.FromArgb(240, 240, 240);
                    _txtOutputFolder.Enabled = false;
                    _btnBrowseOutput.Enabled = false;
                }
            }
            else
            {
                _txtOutputFolder.BackColor = Color.White;
                _txtOutputFolder.Enabled = true;
                _btnBrowseOutput.Enabled = true;
            }
            
            if (_organizerService != null && !string.IsNullOrEmpty(_txtOutputFolder.Text))
            {
                _organizerService.OutputDirectory = _txtOutputFolder.Text;
            }
        }
        
        private void ChkUseSourceAsOutput_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateOutputFolder();
        }
        
        private void BtnBrowseSource_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select folder to organize",
                ShowNewFolderButton = false
            };
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SetSourceFolder(dialog.SelectedPath);
            }
        }
        
        private void BtnUseCurrentFolder_Click(object? sender, EventArgs e)
        {
            SetSourceFolder(_executableDirectory);
        }
        
        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select output folder (where Sorted folder will be created)",
                ShowNewFolderButton = true
            };
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _txtOutputFolder.Text = dialog.SelectedPath;
                if (_organizerService != null)
                {
                    _organizerService.OutputDirectory = dialog.SelectedPath;
                }
            }
        }
        
        private void PnlSourceDrop_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length == 1 && Directory.Exists(files[0]))
                {
                    e.Effect = DragDropEffects.Copy;
                    _pnlSourceDrop.BackColor = Color.FromArgb(200, 230, 255);
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
            _pnlSourceDrop.BackColor = Color.FromArgb(255, 200, 200);
        }
        
        private void PnlSourceDrop_DragDrop(object? sender, DragEventArgs e)
        {
            _pnlSourceDrop.BackColor = Color.FromArgb(248, 248, 248);
            
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    if (Directory.Exists(path))
                    {
                        SetSourceFolder(path);
                    }
                    else
                    {
                        MessageBox.Show("Please drop a folder, not a file.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
        
        private void MenuAbout_Click(object? sender, EventArgs e)
        {
            var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }
        
        private void LoadPreview()
        {
            if (_organizerService == null) return;
            
            try
            {
                _chkIncludeFolders.CheckedChanged -= ChkIncludeFolders_CheckedChanged;
                _filesToOrganize = _organizerService.ScanFiles(_chkIncludeFolders.Checked);
                _chkIncludeFolders.CheckedChanged += ChkIncludeFolders_CheckedChanged;
                
                UpdateFileList();
                UpdateSummary();
                CheckConflicts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UpdateFileList()
        {
            _lstFiles.Items.Clear();
            
            foreach (var file in _filesToOrganize.Take(1000))
            {
                var item = new ListViewItem(file.Name);
                item.SubItems.Add(file.MonthYear);
                item.SubItems.Add(file.ModifiedDate.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(file.IsDirectory ? "<Folder>" : FormatFileSize(file.Size));
                item.Tag = file;
                _lstFiles.Items.Add(item);
            }
            
            if (_filesToOrganize.Count > 1000)
            {
                var item = new ListViewItem($"... and {_filesToOrganize.Count - 1000} more files");
                item.ForeColor = Color.Gray;
                _lstFiles.Items.Add(item);
            }
        }
        
        private void UpdateSummary()
        {
            if (_filesToOrganize.Count == 0)
            {
                _lblSummary.Text = "No files found to organize.";
                _btnStart.Enabled = false;
                return;
            }
            
            var grouped = _organizerService?.GroupByMonthYear(_filesToOrganize) ?? new Dictionary<string, List<FileItem>>();
            var fileCount = _filesToOrganize.Count(f => !f.IsDirectory);
            var folderCount = _filesToOrganize.Count(f => f.IsDirectory);
            
            _lblSummary.Text = $"Summary: • Total Items: {_filesToOrganize.Count} " +
                              $"(Files: {fileCount}, Folders: {folderCount}) • " +
                              $"Month Folders: {grouped.Count}";
            _btnStart.Enabled = true;
        }
        
        private void CheckConflicts()
        {
            if (_organizerService == null || _filesToOrganize.Count == 0)
            {
                _pnlConflicts.Visible = false;
                return;
            }
            
            var grouped = _organizerService.GroupByMonthYear(_filesToOrganize);
            var conflicts = _organizerService.DetectConflicts(grouped);
            
            if (conflicts.Count > 0)
            {
                _lblConflicts.Text = $"⚠ Conflicts Detected: {conflicts.Count}\n";
                foreach (var conflict in conflicts.Take(5))
                {
                    _lblConflicts.Text += $"• {conflict}\n";
                }
                if (conflicts.Count > 5)
                {
                    _lblConflicts.Text += $"• ... and {conflicts.Count - 5} more\n";
                }
                _lblConflicts.Text += "→ Conflicting files will be automatically renamed";
                _pnlConflicts.Visible = true;
            }
            else
            {
                _pnlConflicts.Visible = false;
            }
        }
        
        private void ChkIncludeFolders_CheckedChanged(object? sender, EventArgs e)
        {
            LoadPreview();
        }
        
        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            if (_organizerService == null || _filesToOrganize.Count == 0)
                return;
            
            if (string.IsNullOrEmpty(_txtSourceFolder.Text) || !Directory.Exists(_txtSourceFolder.Text))
            {
                MessageBox.Show("Please select a valid source folder.", "Invalid Source", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string outputDir = _chkUseSourceAsOutput.Checked ? _txtSourceFolder.Text : _txtOutputFolder.Text;
            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                MessageBox.Show("Please select a valid output folder.", "Invalid Output", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var result = MessageBox.Show(
                $"You are about to organize {_filesToOrganize.Count} item(s) into month-year folders.\n\n" +
                $"Source: {_txtSourceFolder.Text}\n" +
                $"Output: {outputDir}\n\n" +
                "This will move files from the source location to a Sorted folder in the output location.\n\n" +
                "Continue?",
                "Confirm Organization",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes)
                return;
            
            // Switch to progress panel
            _pnlMain.Visible = false;
            _pnlProgress.Visible = true;
            _pnlComplete.Visible = false;
            
            _progressBar.Value = 0;
            _txtStatus.Clear();
            _txtStatus.AppendText("Starting organization...\r\n");
            _txtStatus.AppendText($"Source directory: {_organizerService.WorkingDirectory}\r\n");
            _txtStatus.AppendText($"Output directory: {_organizerService.OutputDirectory}\r\n\r\n");
            
            _cancellationTokenSource = new CancellationTokenSource();
            var progress = new Progress<(int current, int total, string currentFile)>(p =>
            {
                _progressBar.Maximum = p.total;
                _progressBar.Value = p.current;
                _lblProgress.Text = $"Processing: {p.current} of {p.total} files";
                
                if (_chkShowProgress.Checked)
                {
                    _txtStatus.AppendText($"✓ {p.currentFile}\r\n");
                    _txtStatus.SelectionStart = _txtStatus.Text.Length;
                    _txtStatus.ScrollToCaret();
                }
            });
            
            try
            {
                var orgResult = await _organizerService.OrganizeFilesAsync(
                    _filesToOrganize,
                    progress,
                    _cancellationTokenSource.Token);
                
                ShowCompletion(orgResult);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Organization was cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _pnlMain.Visible = true;
                _pnlProgress.Visible = false;
                LoadPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during organization: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _pnlMain.Visible = true;
                _pnlProgress.Visible = false;
            }
        }
        
        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to cancel? Partial progress will be saved.",
                    "Cancel Organization",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }
        
        private void ShowCompletion(OrganizationResult result)
        {
            _pnlMain.Visible = false;
            _pnlProgress.Visible = false;
            _pnlComplete.Visible = true;
            
            var summary = "Summary:\r\n" +
                         "─────────────────────────────────────\r\n" +
                         $"✓ Files Moved: {result.FilesMoved}\r\n" +
                         $"✓ Month Folders Created: {result.MonthFoldersCreated}\r\n" +
                         $"✓ Conflicts Resolved: {result.ConflictsResolved}\r\n";
            
            if (result.Errors > 0)
            {
                summary += $"⚠ Errors: {result.Errors}\r\n";
            }
            
            summary += $"\r\nOutput Folder:\r\n{result.SortedFolderPath}\r\n\r\n" +
                      $"CSV Log:\r\n{result.CsvLogPath}";
            
            _lblCompleteSummary.Text = summary;
        }
        
        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            if (_organizerService != null && !string.IsNullOrEmpty(_organizerService.OutputDirectory) && Directory.Exists(_organizerService.OutputDirectory))
            {
                System.Diagnostics.Process.Start("explorer.exe", _organizerService.OutputDirectory);
            }
        }
        
        private void BtnStartNewProject_Click(object? sender, EventArgs e)
        {
            ResetForm();
        }
        
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
