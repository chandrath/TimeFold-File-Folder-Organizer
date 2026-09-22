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
        private ModernButton _btnRecentFolders = null!, _btnUseCurrentFolder = null!;
        private Panel _pnlSourceDrop = null!;
        private CheckBox _chkUseSourceAsOutput = null!;
        private CheckBox _chkIncludeFolders = null!, _chkCreateSubfolder = null!;
        private TextBox _txtOutputFolder = null!;
        private string _customOutputFolder = "";
        private ModernButton _btnBrowseOutput = null!;
        private ListView _lstFiles = null!;
        private ContextMenuStrip _ctxFileMenu = null!;
        private ModernButton _btnRefresh = null!;
        private bool _shouldAutoFitColumns;
        private ModernButton _lblFormatBadge = null!;
        private ModernButton _btnModeSelector = null!;
        private Panel _pnlEmptyState = null!, _pnlConflicts = null!, _pnlTimestampWarning = null!, _pnlLoadMore = null!;
        private Label _lblSummary = null!, _lblConflicts = null!, _lblTimestampWarning = null!;
        private ModernButton _btnStart = null!, _btnThemeToggle = null!, _btnLoadMore = null!;
        private List<ConflictInfo> _currentConflicts = new();
        private int _currentPreviewLimit;

        // UI Controls - Progress Panel
        private Panel _pnlProgress = null!;
        private ProgressBar _progressBar = null!;
        private Label _lblProgress = null!;
        private TextBox _txtStatus = null!;
        private ModernButton _btnCancel = null!;

        // UI Controls - Complete Panel
        private Panel _pnlComplete = null!, _pnlCompleteActions = null!;
        private Label _lblCompleteSummary = null!;
        private ModernButton _btnOpenFolder = null!, _btnStartNewProject = null!, _btnExitApp = null!;

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

        private ToolTip _toolTip = null!;
        private ToolTip _cellToolTip = null!;

        private void InitializeOrganizer()
        {
            SetupToolTips();
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

            this.Shown += (s, e) =>
            {
                if (!_settings.HasSeenWelcomeTour)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        using var tourForm = new WelcomeTourForm(_settings.DarkMode, isDark =>
                        {
                            _settings.DarkMode = isDark;
                            ApplyTheme(_settings.DarkMode);
                        });
                        tourForm.ShowDialog(this);
                        _settings.DarkMode = tourForm.SelectedDarkMode;
                        _settings.HasSeenWelcomeTour = true;
                        _settings.SaveToFile();
                        ApplyTheme(_settings.DarkMode);
                    }));
                }
            };
        }

        private void SetupToolTips()
        {
            _toolTip = new ToolTip { ShowAlways = true, InitialDelay = 300, AutoPopDelay = 8000, ReshowDelay = 150 };
            _toolTip.SetToolTip(_btnBrowseSource, "Browse your computer to select a folder to organize");
            _toolTip.SetToolTip(_btnRecentFolders, "Quickly select from your recently organized folders");
            _toolTip.SetToolTip(_btnUseCurrentFolder, "Use the folder where this application is currently located");
            _toolTip.SetToolTip(_txtSourceFolder, "Selected source folder to organize");
            _toolTip.SetToolTip(_pnlSourceDrop, "Drag and drop any folder or files here to inspect (or click Browse)");
            _toolTip.SetToolTip(_lblDropHint, _toolTip.GetToolTip(_pnlSourceDrop));
            _toolTip.SetToolTip(_chkUseSourceAsOutput, "Create the sorted date folders directly inside the source folder");
            _toolTip.SetToolTip(_chkIncludeFolders, "Moves whole folders intact into the target date/category. It NEVER extracts or flattens files inside them.");
            _toolTip.SetToolTip(_chkCreateSubfolder, "When checked, creates a timestamped 'Sorted_...' parent folder. When unchecked, organizes directly in the destination");
            _toolTip.SetToolTip(_btnBrowseOutput, "Choose a different destination folder for the sorted date folders");
            _toolTip.SetToolTip(_txtOutputFolder, "Selected destination folder for organized files");
            _toolTip.SetToolTip(_lblFormatBadge, "Current folder naming pattern. Click to customize in Preferences");
            _toolTip.SetToolTip(_btnRefresh, "Scan and refresh the organization plan (F5)");
            _toolTip.SetToolTip(_btnStart, "Move files & folders into their date-based timeline folders");
            _toolTip.SetToolTip(_btnExitApp, "Exit TimeFold application");

            _cellToolTip = new ToolTip { ShowAlways = true, InitialDelay = 150, AutoPopDelay = 6000, ReshowDelay = 100 };
            _lstFiles.MouseMove += LstFiles_MouseMove;
            _lstFiles.MouseLeave += LstFiles_MouseLeave;
        }

        private void ApplySettings()
        {
            this.TopMost = _settings.ShowOnTop;

            if (_organizerService != null)
            {
                _organizerService.ApplyNamingSettings(
                    _settings.FolderFormat, _settings.FolderPrefix, _settings.FolderSuffix,
                    _settings.Use24HourTimestamp, _settings.OrgMode,
                    _settings.KeepHtmlCompanionsTogether, _settings.CategoryPrefix,
                    _settings.CategorySuffix, _settings.KeepSubtitleCompanionsTogether,
                    _settings.CreateSortedSubfolder);
            }
            if (_chkUseSourceAsOutput != null && _chkUseSourceAsOutput.Checked != _settings.UseSourceAsOutput) _chkUseSourceAsOutput.Checked = _settings.UseSourceAsOutput;
            if (_chkIncludeFolders != null && _chkIncludeFolders.Checked != _settings.IncludeTopLevelFolders) _chkIncludeFolders.Checked = _settings.IncludeTopLevelFolders;
            if (_chkCreateSubfolder != null && _chkCreateSubfolder.Checked != _settings.CreateSortedSubfolder) _chkCreateSubfolder.Checked = _settings.CreateSortedSubfolder;
            if (_btnModeSelector != null) _btnModeSelector.Text = GetModeSelectorText();
            UpdateFormatBadge();
            UpdateOutputFolder();
        }

        private void UpdateFormatBadge()
        {
            if (_lblFormatBadge != null)
            {
                _lblFormatBadge.Visible = true;
                bool usesDate = (_settings.OrgMode != Models.OrganizationMode.Category && _settings.OrgMode != Models.OrganizationMode.Extension);
                _lblFormatBadge.Enabled = usesDate;
                var palette = Config.AppTheme.GetPalette(_settings.DarkMode);
                if (usesDate)
                {
                    string sample = Config.AppConstants.FormatFolderDate(DateTime.Now, _settings.FolderFormat, _settings.FolderPrefix, _settings.FolderSuffix);
                    string display = sample.Length > 35 ? sample[..33].TrimEnd() + "…" : sample;
                    _lblFormatBadge.Text = $"📁 {display} ⚙";
                    _lblFormatBadge.BackColor = palette.BadgeBg;
                    _lblFormatBadge.ForeColor = palette.BadgeText;
                    _lblFormatBadge.BorderColor = palette.BadgeBorder;
                    _lblFormatBadge.Cursor = Cursors.Hand;
                    _toolTip?.SetToolTip(_lblFormatBadge, $"Active Format: {sample}\nClick to customize in Preferences");
                }
                else
                {
                    _lblFormatBadge.Text = "📁 Format: N/A ⚙";
                    _lblFormatBadge.BackColor = palette.CardBg;
                    _lblFormatBadge.ForeColor = palette.TextMuted;
                    _lblFormatBadge.BorderColor = palette.CardBorder;
                    _lblFormatBadge.Cursor = Cursors.Default;
                    _toolTip?.SetToolTip(_lblFormatBadge, "Date format is not used in this organization mode.");
                }
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
                _organizerService.ApplyNamingSettings(
                    _settings.FolderFormat,
                    _settings.FolderPrefix,
                    _settings.FolderSuffix,
                    _settings.Use24HourTimestamp,
                    _settings.OrgMode,
                    _settings.KeepHtmlCompanionsTogether,
                    _settings.CategoryPrefix,
                    _settings.CategorySuffix,
                    _settings.KeepSubtitleCompanionsTogether,
                    _settings.CreateSortedSubfolder);
                _shouldAutoFitColumns = true;
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

        private string GetBaseOutputFolder() =>
            _chkUseSourceAsOutput.Checked ? _txtSourceFolder.Text.Trim() : _customOutputFolder.Trim();

        private void UpdateOutputFolder()
        {
            var palette = AppTheme.GetPalette(_settings.DarkMode);
            bool useSource = _chkUseSourceAsOutput.Checked;

            _txtOutputFolder.BackColor = useSource ? palette.InputDisabledBg : palette.InputBg;
            _txtOutputFolder.ForeColor = useSource ? palette.TextMuted : palette.TextPrimary;
            _txtOutputFolder.ReadOnly = true;
            _btnBrowseOutput.Enabled = !useSource;

            string baseDir = GetBaseOutputFolder();
            if (!string.IsNullOrEmpty(baseDir) && Directory.Exists(baseDir))
            {
                string preview = _settings.CreateSortedSubfolder
                    ? AppConstants.GetSortedFolderPreviewPath(baseDir, _settings.Use24HourTimestamp)
                    : baseDir;
                _txtOutputFolder.Text = preview;
                _txtOutputFolder.SelectionStart = _txtOutputFolder.Text.Length;
                _txtOutputFolder.ScrollToCaret();
                _toolTip?.SetToolTip(_txtOutputFolder, _settings.CreateSortedSubfolder ? $"Output Destination (will create):\n{preview}" : $"Output Destination (direct into folder):\n{baseDir}");
                if (_organizerService != null)
                {
                    _organizerService.OutputDirectory = baseDir;
                }
            }
            else
            {
                _txtOutputFolder.Text = "";
                _toolTip?.SetToolTip(_txtOutputFolder, "Selected destination folder for organized files");
            }
            CheckConflicts();
        }

        private void MenuFileNew_Click(object? sender, EventArgs e) => ResetForm();
        private void MenuFileExit_Click(object? sender, EventArgs e) => this.Close();

        private void MenuPreferences_Click(object? sender, EventArgs e)
        {
            using var prefsForm = new PreferencesForm(_settings);
            if (prefsForm.ShowDialog() == DialogResult.OK)
            {
                (_settings = prefsForm.Settings).SaveToFile();
                ApplySettings();
                ApplyTheme(_settings.DarkMode);
                LoadPreview();
            }
        }

        private void MenuAbout_Click(object? sender, EventArgs e)
        {
            using var aboutForm = new AboutForm(_settings.DarkMode);
            aboutForm.ShowDialog();
        }

        private void MenuWelcomeTour_Click(object? sender, EventArgs e)
        {
            using var tourForm = new WelcomeTourForm(_settings.DarkMode, isDark =>
            {
                _settings.DarkMode = isDark;
                ApplyTheme(_settings.DarkMode);
            });
            tourForm.ShowDialog(this);
            _settings.DarkMode = tourForm.SelectedDarkMode;
            if (tourForm.DontShowOnStartup != _settings.HasSeenWelcomeTour)
            {
                _settings.HasSeenWelcomeTour = tourForm.DontShowOnStartup;
            }
            _settings.SaveToFile();
            ApplyTheme(_settings.DarkMode);
        }

        private void ResetForm()
        {
            _txtSourceFolder.Text = _customOutputFolder = _txtOutputFolder.Text = "";
            _chkUseSourceAsOutput.Checked = AppConstants.DefaultUseSourceAsOutput; _chkCreateSubfolder.Checked = AppConstants.DefaultCreateSortedSubfolder; _chkIncludeFolders.Checked = AppConstants.DefaultIncludeTopLevelFolders;
            _filesToOrganize.Clear();
            _lstFiles.Items.Clear();
            _lstFiles.Visible = false;
            if (_pnlEmptyState != null) _pnlEmptyState.Visible = true;
            _lblSummary.Font = new Font(_lblSummary.Font, FontStyle.Regular);
            _lblSummary.ForeColor = AppTheme.GetPalette(_settings.DarkMode).TextMuted;
            _lblSummary.Text = "Select a source folder to preview files.";
            _pnlConflicts.Visible = _pnlTimestampWarning.Visible = _pnlProgress.Visible = _pnlComplete.Visible = false;
            _organizerService = null;
            _pnlMain.Visible = true;
            _lastResult = null;
            _currentPreviewLimit = 0;
            if (_pnlLoadMore != null) _pnlLoadMore.Visible = false;
            _btnStart.Enabled = false;
            UpdatePreviewHeaderCount(0, 0);
            UpdateMainLayout();
        }

        private void BtnLoadMore_Click(object? sender, EventArgs e)
        {
            int current = _currentPreviewLimit > 0 ? _currentPreviewLimit : (_settings.MaxPreviewItems > 0 ? _settings.MaxPreviewItems : AppConstants.DefaultMaxPreviewItems);
            _currentPreviewLimit = current + 1000;
            UpdateFileList();
        }

        private void LstFiles_Click(object? sender, EventArgs e)
        {
            if (_lstFiles.SelectedItems.Count > 0 && _lstFiles.SelectedItems[0].Tag is "LOAD_MORE")
            {
                BtnLoadMore_Click(sender, e);
            }
        }

        private void UpdatePreviewHeaderCount(int displayed, int total)
        {
            if (_lblPreviewHeader == null) return;
            if (total == 0)
            {
                _lblPreviewHeader.Text = "Organization Plan";
                _toolTip?.SetToolTip(_lblPreviewHeader, null);
                return;
            }

            if (displayed < total)
            {
                _lblPreviewHeader.Text = $"Organization Plan ({displayed:N0} / {total:N0})";
                _toolTip?.SetToolTip(_lblPreviewHeader, $"Showing {displayed:N0} of {total:N0} items loaded.\nTip: Change default preview limit in Settings > Preferences");
            }
            else
            {
                _lblPreviewHeader.Text = $"Organization Plan ({total:N0})";
                _toolTip?.SetToolTip(_lblPreviewHeader, $"All {total:N0} items loaded.\nTip: Change default preview limit in Settings > Preferences");
            }
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

        private Font? _boldListFont;
        private Font GetBoldListFont() => _boldListFont ??= new Font(_lstFiles.Font, FontStyle.Bold);
        private string? _currentListToolTipText;
        private (int ItemIndex, int SubIndex) _lastTooltipCell = (-1, -1);

        private void LstFiles_MouseMove(object? sender, MouseEventArgs e)
        {
            var hit = _lstFiles.HitTest(e.Location);
            if (hit.Item?.Tag is not FileItem file || hit.SubItem == null)
            {
                ClearListToolTip();
                return;
            }

            int subIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            int itemIndex = hit.Item.Index;

            if (_lastTooltipCell == (itemIndex, subIndex)) return;
            _lastTooltipCell = (itemIndex, subIndex);

            string? newText = null;
            if (subIndex == 2) // Modified Date
            {
                newText = !file.IsCreatedDateActive
                    ? $"✔ Active Date (Modified)\nUsed to organize this item into target folder '{file.TargetFolder}'.\nTip: Change date source rules in Settings > Preferences"
                    : $"Inactive Date (Modified)\nOriginal file timestamp (not used for folder placement).\nTip: Change date source rules in Settings > Preferences";
            }
            else if (subIndex == 3) // Created Date
            {
                newText = file.IsCreatedDateActive
                    ? $"✔ Active Date (Created)\nUsed to organize this item into target folder '{file.TargetFolder}'.\nTip: Change date source rules in Settings > Preferences"
                    : $"Inactive Date (Created)\nOriginal file timestamp (not used for folder placement).\nTip: Change date source rules in Settings > Preferences";
            }

            if (newText != null)
            {
                _currentListToolTipText = newText;
                _cellToolTip.Show(newText, _lstFiles, e.Location.X + 16, e.Location.Y + 20, 5000);
            }
            else
            {
                ClearListToolTip();
            }
        }

        private void LstFiles_MouseLeave(object? sender, EventArgs e) => ClearListToolTip();

        private void ClearListToolTip()
        {
            if (_lastTooltipCell != (-1, -1) || _currentListToolTipText != null)
            {
                _lastTooltipCell = (-1, -1);
                _currentListToolTipText = null;
                _cellToolTip.Hide(_lstFiles);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _boldListFont?.Dispose();
            _cellToolTip?.Dispose();
            _toolTip?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
