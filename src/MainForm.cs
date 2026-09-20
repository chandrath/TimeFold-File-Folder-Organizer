using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Controls;
using FileOrganizer.Models;
using FileOrganizer.Services;

namespace FileOrganizer
{
    public partial class MainForm : Form
    {
        // Service & State
        private FileOrganizerService? _organizerService;
        private List<FileItem> _filesToOrganize = new List<FileItem>();
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly string _executablePath;
        private readonly string _executableDirectory;
        private AppSettings _settings = AppSettings.LoadFromFile();
        private OrganizationResult? _lastResult;

        // UI Controls - Menus
        private MenuStrip _menuStrip = null!;
        private ToolStripMenuItem _menuFile = null!;
        private ToolStripMenuItem _menuFileNew = null!;
        private ToolStripMenuItem _menuFileRecent = null!;
        private ToolStripMenuItem _menuFileExit = null!;
        private ToolStripMenuItem _menuPreferences = null!;
        private ToolStripMenuItem _menuHelp = null!;
        private ToolStripMenuItem _menuAbout = null!;

        // UI Controls - Main Panel
        private Panel _pnlMain = null!;
        private Label _lblTitle = null!;
        private TextBox _txtSourceFolder = null!;
        private ModernButton _btnBrowseSource = null!;
        private ModernButton _btnRecentFolders = null!;
        private ModernButton _btnUseCurrentFolder = null!;
        private Panel _pnlSourceDrop = null!;
        private CheckBox _chkUseSourceAsOutput = null!;
        private CheckBox _chkIncludeFolders = null!;
        private TextBox _txtOutputFolder = null!;
        private ModernButton _btnBrowseOutput = null!;
        private ListView _lstFiles = null!;
        private ContextMenuStrip _ctxFileMenu = null!;
        private ModernButton _btnRefresh = null!;
        private ModernButton _lblFormatBadge = null!;
        private Panel _pnlEmptyState = null!;
        private Label _lblSummary = null!;
        private Panel _pnlConflicts = null!;
        private Label _lblConflicts = null!;
        private Panel _pnlTimestampWarning = null!;
        private Label _lblTimestampWarning = null!;
        private ModernButton _btnStart = null!;

        // UI Controls - Progress Panel
        private Panel _pnlProgress = null!;
        private ProgressBar _progressBar = null!;
        private Label _lblProgress = null!;
        private TextBox _txtStatus = null!;
        private ModernButton _btnCancel = null!;

        // UI Controls - Complete Panel
        private Panel _pnlComplete = null!;
        private Label _lblCompleteSummary = null!;
        private Panel _pnlCompleteActions = null!;
        private ModernButton _btnOpenFolder = null!;
        private ModernButton _btnStartNewProject = null!;

        // Theme-responsive Labels & Panels
        private Label _lblSubtitle = null!;
        private Label _lblSourceTitle = null!;
        private Label _lblDropHint = null!;
        private Label _lblPreviewHeader = null!;
        private Label _lblEmptyIcon = null!;
        private Label _lblEmptyTitle = null!;
        private Label _lblEmptyDesc = null!;
        private Label _lblProgressTitle = null!;
        private Label _lblCompleteTitle = null!;
        private Label _lblCompleteSubtitle = null!;
        private Panel _pnlDetailsCard = null!;

        public MainForm()
        {
            _executablePath = Application.ExecutablePath;
            _executableDirectory = Path.GetDirectoryName(_executablePath) ?? Environment.CurrentDirectory;
            InitializeComponent();
            InitializeOrganizer();
        }

        private void InitializeOrganizer()
        {
            ApplySettings();
            ApplyTheme(_settings.DarkMode);
            UpdateRecentMenus();
            if (_settings.AutoLoadExeDirectoryOnStartup && Directory.Exists(_executableDirectory))
            {
                SetSourceFolder(_executableDirectory);
            }
            else
            {
                UpdateOutputFolder();
            }
        }

        private void ApplySettings()
        {
            this.TopMost = _settings.ShowOnTop;

            if (_organizerService != null)
            {
                _organizerService.ApplyNamingSettings(_settings.FolderFormat, _settings.FolderPrefix, _settings.FolderSuffix, _settings.Use24HourTimestamp);
            }
            if (_chkIncludeFolders != null && _chkIncludeFolders.Checked != _settings.IncludeTopLevelFolders)
            {
                _chkIncludeFolders.Checked = _settings.IncludeTopLevelFolders;
            }
            UpdateFormatBadge();
        }

        private void UpdateFormatBadge()
        {
            if (_lblFormatBadge != null)
            {
                string sample = AppConstants.FormatFolderDate(DateTime.Now, _settings.FolderFormat, _settings.FolderPrefix, _settings.FolderSuffix);
                _lblFormatBadge.Text = $"📁 Format: {sample} ⚙";
            }
        }

        private void SetSourceFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                _txtSourceFolder.Text = folder;
                _settings.AddRecentFolder(folder);
                UpdateRecentMenus();
                _organizerService = new FileOrganizerService(_executablePath, folder);
                _organizerService.ApplyNamingSettings(_settings.FolderFormat, _settings.FolderPrefix, _settings.FolderSuffix, _settings.Use24HourTimestamp);
                UpdateOutputFolder();
                LoadPreview();
            }
        }

        private void UpdateRecentMenus()
        {
            if (_menuFileRecent != null)
            {
                PopulateRecentMenu(_menuFileRecent.DropDownItems);
            }
        }

        private void PopulateRecentMenu(ToolStripItemCollection items)
        {
            items.Clear();
            if (_settings.RecentFolders.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("(No recent folders)") { Enabled = false };
                items.Add(emptyItem);
                return;
            }

            for (int i = 0; i < _settings.RecentFolders.Count; i++)
            {
                string path = _settings.RecentFolders[i];
                string name = Path.GetFileName(path);
                if (string.IsNullOrEmpty(name)) name = path;
                string label = $"&{i + 1}. {name}  ({path})";

                var item = new ToolStripMenuItem(label) { ToolTipText = path };
                item.Click += (s, e) =>
                {
                    if (Directory.Exists(path))
                    {
                        SetSourceFolder(path);
                    }
                    else
                    {
                        MessageBox.Show($"The folder could not be found:\n\n{path}\n\nIt may have been moved, deleted, or on an unplugged drive.", "Folder Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _settings.RemoveRecentFolder(path);
                        UpdateRecentMenus();
                    }
                };
                items.Add(item);
            }

            items.Add(new ToolStripSeparator());
            var clearItem = new ToolStripMenuItem("🗑 Clear Recent History");
            clearItem.Click += (s, e) =>
            {
                _settings.ClearRecentFolders();
                UpdateRecentMenus();
            };
            items.Add(clearItem);
        }

        private void UpdateOutputFolder()
        {
            var palette = AppTheme.GetPalette(_settings.DarkMode);
            if (_chkUseSourceAsOutput.Checked)
            {
                _txtOutputFolder.BackColor = palette.InputDisabledBg;
                _txtOutputFolder.ForeColor = palette.TextMuted;
                _txtOutputFolder.Enabled = false;
                _btnBrowseOutput.Enabled = false;

                if (!string.IsNullOrEmpty(_txtSourceFolder.Text) && Directory.Exists(_txtSourceFolder.Text))
                {
                    _txtOutputFolder.Text = _txtSourceFolder.Text;
                }
                else
                {
                    _txtOutputFolder.Text = "";
                }
            }
            else
            {
                _txtOutputFolder.BackColor = palette.InputBg;
                _txtOutputFolder.ForeColor = palette.TextPrimary;
                _txtOutputFolder.Enabled = true;
                _btnBrowseOutput.Enabled = true;
            }

            if (_organizerService != null && !string.IsNullOrEmpty(_txtOutputFolder.Text))
            {
                _organizerService.OutputDirectory = _txtOutputFolder.Text;
            }
        }

        private void ResetForm()
        {
            _txtSourceFolder.Text = "";
            _txtOutputFolder.Text = "";
            _chkUseSourceAsOutput.Checked = true;
            _filesToOrganize.Clear();
            _lstFiles.Items.Clear();
            _lstFiles.Visible = false;
            if (_pnlEmptyState != null) _pnlEmptyState.Visible = true;
            _lblSummary.Text = "Select a source folder to preview files.";
            _pnlConflicts.Visible = false;
            _pnlTimestampWarning.Visible = false;
            _organizerService = null;
            _pnlMain.Visible = true;
            _pnlProgress.Visible = false;
            _pnlComplete.Visible = false;
            _lastResult = null;
            UpdateMainLayout();
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
