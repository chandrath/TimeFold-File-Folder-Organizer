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
        private AppSettings _settings = new AppSettings();
        private OrganizationResult? _lastResult;

        // UI Controls - Menus
        private MenuStrip _menuStrip = null!;
        private ToolStripMenuItem _menuFile = null!;
        private ToolStripMenuItem _menuFileNew = null!;
        private ToolStripMenuItem _menuFileExit = null!;
        private ToolStripMenuItem _menuPreferences = null!;
        private ToolStripMenuItem _menuHelp = null!;
        private ToolStripMenuItem _menuAbout = null!;

        // UI Controls - Main Panel
        private Panel _pnlMain = null!;
        private Label _lblTitle = null!;
        private TextBox _txtSourceFolder = null!;
        private ModernButton _btnBrowseSource = null!;
        private ModernButton _btnUseCurrentFolder = null!;
        private Panel _pnlSourceDrop = null!;
        private CheckBox _chkUseSourceAsOutput = null!;
        private TextBox _txtOutputFolder = null!;
        private ModernButton _btnBrowseOutput = null!;
        private ListView _lstFiles = null!;
        private Label _lblSummary = null!;
        private Panel _pnlConflicts = null!;
        private Label _lblConflicts = null!;
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

        public MainForm()
        {
            _executablePath = Application.ExecutablePath;
            _executableDirectory = Path.GetDirectoryName(_executablePath) ?? Environment.CurrentDirectory;
            InitializeComponent();
            InitializeOrganizer();
            LoadPreview();
        }

        private void InitializeOrganizer()
        {
            ApplySettings();
            SetSourceFolder(_executableDirectory);
            UpdateOutputFolder();
        }

        private void ApplySettings()
        {
            this.TopMost = _settings.ShowOnTop;

            if (_organizerService != null)
            {
                _organizerService.FolderFormat = _settings.FolderFormat;
            }
        }

        private void SetSourceFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                _txtSourceFolder.Text = folder;
                _organizerService = new FileOrganizerService(_executablePath, folder);
                _organizerService.FolderFormat = _settings.FolderFormat;
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

        private void ResetForm()
        {
            _txtSourceFolder.Text = "";
            _txtOutputFolder.Text = "";
            _chkUseSourceAsOutput.Checked = true;
            _filesToOrganize.Clear();
            _lstFiles.Items.Clear();
            _lblSummary.Text = "";
            _pnlConflicts.Visible = false;
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
