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
        
        private Label _lblTitle = null!;
        private Label _lblLocation = null!;
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
            InitializeComponent();
            InitializeOrganizer();
            LoadPreview();
        }
        
        private void InitializeComponent()
        {
            this.Text = "File Organizer by Date";
            this.Size = new Size(800, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Title
            _lblTitle = new Label
            {
                Text = "📁 File Organizer by Date",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            
            // Location label
            _lblLocation = new Label
            {
                Text = "Current Location: ",
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(20, 50)
            };
            
            // Options panel
            var pnlOptions = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(740, 80),
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
                Location = new Point(10, 60),
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
                Location = new Point(20, 170),
                AutoSize = true
            };
            
            _lstFiles = new ListView
            {
                Location = new Point(20, 195),
                Size = new Size(740, 200),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };
            
            _lstFiles.Columns.Add("File Name", 300);
            _lstFiles.Columns.Add("Month Folder", 200);
            _lstFiles.Columns.Add("Modified Date", 150);
            _lstFiles.Columns.Add("Size", 100);
            
            // Summary label
            _lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 405),
                AutoSize = true
            };
            
            // Conflicts panel
            _pnlConflicts = new Panel
            {
                Location = new Point(20, 430),
                Size = new Size(740, 80),
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
                Location = new Point(610, 520),
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
            _pnlMain.Controls.Add(_lblLocation);
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
                Size = new Size(740, 30),
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
                Size = new Size(740, 400),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };
            
            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 30),
                Location = new Point(660, 550),
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
                Size = new Size(740, 400),
                AutoSize = false
            };
            
            _btnOpenFolder = new Button
            {
                Text = "Open Folder",
                Size = new Size(120, 35),
                Location = new Point(400, 480),
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
                Location = new Point(530, 480),
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
                Location = new Point(660, 480),
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
            var executablePath = Application.ExecutablePath;
            _organizerService = new FileOrganizerService(executablePath);
            _lblLocation.Text = $"Current Location: {_organizerService.WorkingDirectory}";
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
            
            foreach (var file in _filesToOrganize.Take(1000)) // Limit display to 1000 items
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
            
            var result = MessageBox.Show(
                $"You are about to organize {_filesToOrganize.Count} item(s) into month-year folders.\n\n" +
                "This will move files from the current location to a Sorted folder.\n\n" +
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
            _txtStatus.AppendText($"Working directory: {_organizerService.WorkingDirectory}\r\n\r\n");
            
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
            if (_organizerService != null && Directory.Exists(_organizerService.WorkingDirectory))
            {
                System.Diagnostics.Process.Start("explorer.exe", _organizerService.WorkingDirectory);
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

