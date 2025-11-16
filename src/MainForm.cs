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
        private ToolStripMenuItem _menuHelp = null!;
        private ToolStripMenuItem _menuAbout = null!;
        
        private Label _lblTitle = null!;
        private TextBox _txtSourceFolder = null!;
        private Button _btnBrowseSource = null!;
        private Button _btnUseCurrentFolder = null!;
        private Panel _pnlSourceDrop = null!;
        private TextBox _txtOutputFolder = null!;
        private Button _btnBrowseOutput = null!;
        private Button _btnUseSourceAsOutput = null!;
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
        private Button _btnOrganizeAgain = null!;
        private Button _btnClose = null!;
        private Button _btnCancel = null!;
        
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
            this.Size = new Size(850, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Menu Bar
            _menuStrip = new MenuStrip();
            _menuHelp = new ToolStripMenuItem("Help");
            _menuAbout = new ToolStripMenuItem("About");
            _menuAbout.Click += MenuAbout_Click;
            _menuHelp.DropDownItems.Add(_menuAbout);
            _menuStrip.Items.Add(_menuHelp);
            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip);
            
            // Title
            _lblTitle = new Label
            {
                Text = "📁 File Organizer by Date",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(20, 35)
            };
            
            // Source Folder Selection
            var lblSourceFolderTitle = new Label
            {
                Text = "Source Folder (to organize):",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(20, 70),
                AutoSize = true
            };
            
            _txtSourceFolder = new TextBox
            {
                Location = new Point(20, 95),
                Size = new Size(600, 23),
                ReadOnly = true,
                BackColor = Color.White
            };
            
            _btnBrowseSource = new Button
            {
                Text = "Browse...",
                Size = new Size(80, 25),
                Location = new Point(630, 94),
                FlatStyle = FlatStyle.Flat
            };
            _btnBrowseSource.Click += BtnBrowseSource_Click;
            
            _btnUseCurrentFolder = new Button
            {
                Text = "📂 Use Current Folder",
                Size = new Size(130, 25),
                Location = new Point(720, 94),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnUseCurrentFolder.FlatAppearance.BorderSize = 0;
            _btnUseCurrentFolder.Click += BtnUseCurrentFolder_Click;
            
            // Drag & Drop Panel for Source
            _pnlSourceDrop = new Panel
            {
                Location = new Point(20, 125),
                Size = new Size(830, 50),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 240, 240),
                AllowDrop = true
            };
            
            var lblDropHint = new Label
            {
                Text = "📁 Drag and drop a folder here (folders only)",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(10, 15),
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
                Location = new Point(20, 185),
                AutoSize = true
            };
            
            _txtOutputFolder = new TextBox
            {
                Location = new Point(20, 210),
                Size = new Size(600, 23),
                ReadOnly = true,
                BackColor = Color.White
            };
            
            _btnBrowseOutput = new Button
            {
                Text = "Browse...",
                Size = new Size(80, 25),
                Location = new Point(630, 209),
                FlatStyle = FlatStyle.Flat
            };
            _btnBrowseOutput.Click += BtnBrowseOutput_Click;
            
            _btnUseSourceAsOutput = new Button
            {
                Text = "Use Source Folder",
                Size = new Size(130, 25),
                Location = new Point(720, 209),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnUseSourceAsOutput.FlatAppearance.BorderSize = 0;
            _btnUseSourceAsOutput.Click += BtnUseSourceAsOutput_Click;
            
            // Options panel
            var pnlOptions = new Panel
            {
                Location = new Point(20, 245),
                Size = new Size(830, 60),
                BorderStyle = BorderStyle.FixedSingle
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
                Location = new Point(500, 35),
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
                Location = new Point(20, 315),
                AutoSize = true
            };
            
            _lstFiles = new ListView
            {
                Location = new Point(20, 340),
                Size = new Size(830, 200),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };
            
            _lstFiles.Columns.Add("File Name", 350);
            _lstFiles.Columns.Add("Month Folder", 200);
            _lstFiles.Columns.Add("Modified Date", 150);
            _lstFiles.Columns.Add("Size", 130);
            
            // Summary label
            _lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 550),
                AutoSize = true
            };
            
            // Conflicts panel
            _pnlConflicts = new Panel
            {
                Location = new Point(20, 575),
                Size = new Size(830, 60),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            
            _lblConflicts = new Label
            {
                Text = "⚠ Conflicts Detected:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.OrangeRed,
                Location = new Point(10, 10),
                AutoSize = true
            };
            
            _pnlConflicts.Controls.Add(_lblConflicts);
            
            // Buttons
            _btnStart = new Button
            {
                Text = "Start Organization",
                Size = new Size(150, 35),
                Location = new Point(700, 645),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += BtnStart_Click;
            
            // Main panel
            _pnlMain = new Panel
            {
                Dock = DockStyle.Fill
            };
            
            _pnlMain.Controls.Add(_lblTitle);
            _pnlMain.Controls.Add(lblSourceFolderTitle);
            _pnlMain.Controls.Add(_txtSourceFolder);
            _pnlMain.Controls.Add(_btnBrowseSource);
            _pnlMain.Controls.Add(_btnUseCurrentFolder);
            _pnlMain.Controls.Add(_pnlSourceDrop);
            _pnlMain.Controls.Add(lblOutputFolderTitle);
            _pnlMain.Controls.Add(_txtOutputFolder);
            _pnlMain.Controls.Add(_btnBrowseOutput);
            _pnlMain.Controls.Add(_btnUseSourceAsOutput);
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
                Visible = false
            };
            
            var lblProgressTitle = new Label
            {
                Text = "📁 Organizing Files...",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true
            };
            
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 60),
                Size = new Size(810, 30),
                Style = ProgressBarStyle.Continuous
            };
            
            _lblProgress = new Label
            {
                Text = "Initializing...",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 100),
                AutoSize = true
            };
            
            _txtStatus = new TextBox
            {
                Location = new Point(20, 130),
                Size = new Size(810, 500),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };
            
            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 30),
                Location = new Point(730, 650),
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
                Visible = false
            };
            
            var lblCompleteTitle = new Label
            {
                Text = "✅ Organization Complete!",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.Green,
                Location = new Point(20, 20),
                AutoSize = true
            };
            
            _lblCompleteSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10),
                Location = new Point(20, 60),
                Size = new Size(810, 500),
                AutoSize = false
            };
            
            _btnOpenFolder = new Button
            {
                Text = "Open Folder",
                Size = new Size(120, 35),
                Location = new Point(450, 580),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOpenFolder.FlatAppearance.BorderSize = 0;
            _btnOpenFolder.Click += BtnOpenFolder_Click;
            
            _btnOrganizeAgain = new Button
            {
                Text = "Organize Again",
                Size = new Size(120, 35),
                Location = new Point(580, 580),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOrganizeAgain.FlatAppearance.BorderSize = 0;
            _btnOrganizeAgain.Click += BtnOrganizeAgain_Click;
            
            _btnClose = new Button
            {
                Text = "Close",
                Size = new Size(100, 35),
                Location = new Point(710, 580),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => this.Close();
            
            _pnlComplete.Controls.Add(lblCompleteTitle);
            _pnlComplete.Controls.Add(_lblCompleteSummary);
            _pnlComplete.Controls.Add(_btnOpenFolder);
            _pnlComplete.Controls.Add(_btnOrganizeAgain);
            _pnlComplete.Controls.Add(_btnClose);
            
            this.Controls.Add(_pnlMain);
            this.Controls.Add(_pnlProgress);
            this.Controls.Add(_pnlComplete);
        }
        
        private void InitializeOrganizer()
        {
            // Default to executable directory
            SetSourceFolder(_executableDirectory);
            SetOutputFolder(_executableDirectory);
        }
        
        private void SetSourceFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                _txtSourceFolder.Text = folder;
                _organizerService = new FileOrganizerService(_executablePath, folder);
                if (!string.IsNullOrEmpty(_txtOutputFolder.Text))
                {
                    _organizerService.OutputDirectory = _txtOutputFolder.Text;
                }
                LoadPreview();
            }
        }
        
        private void SetOutputFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                _txtOutputFolder.Text = folder;
                if (_organizerService != null)
                {
                    _organizerService.OutputDirectory = folder;
                }
            }
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
                SetOutputFolder(dialog.SelectedPath);
            }
        }
        
        private void BtnUseSourceAsOutput_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_txtSourceFolder.Text) && Directory.Exists(_txtSourceFolder.Text))
            {
                SetOutputFolder(_txtSourceFolder.Text);
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
            _pnlSourceDrop.BackColor = Color.FromArgb(240, 240, 240);
            
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
            
            if (string.IsNullOrEmpty(_txtOutputFolder.Text) || !Directory.Exists(_txtOutputFolder.Text))
            {
                MessageBox.Show("Please select a valid output folder.", "Invalid Output", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var result = MessageBox.Show(
                $"You are about to organize {_filesToOrganize.Count} item(s) into month-year folders.\n\n" +
                $"Source: {_txtSourceFolder.Text}\n" +
                $"Output: {_txtOutputFolder.Text}\n\n" +
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
        
        private void BtnOrganizeAgain_Click(object? sender, EventArgs e)
        {
            _pnlMain.Visible = true;
            _pnlProgress.Visible = false;
            _pnlComplete.Visible = false;
            LoadPreview();
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
