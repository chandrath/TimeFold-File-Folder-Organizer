using System;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Models;

namespace FileOrganizer
{
    public partial class PreferencesForm : Form
    {
        private AppSettings _settings;
        private CheckBox _chkIncludeFolders = null!;
        private CheckBox _chkShowProgress = null!;
        private CheckBox _chkShowOnTop = null!;
        private RadioButton _rbMonthYear = null!;
        private RadioButton _rbYearMonth = null!;
        private Button _btnOK = null!;
        private Button _btnCancel = null!;
        
        public AppSettings Settings => _settings;
        
        public PreferencesForm(AppSettings currentSettings)
        {
            _settings = new AppSettings
            {
                IncludeTopLevelFolders = currentSettings.IncludeTopLevelFolders,
                ShowDetailedProgress = currentSettings.ShowDetailedProgress,
                ShowOnTop = currentSettings.ShowOnTop,
                FolderFormat = currentSettings.FolderFormat
            };
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Preferences";
            this.Size = new Size(520, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(15);
            
            int currentY = 20;
            const int spacing = 25;
            const int leftMargin = 20;
            const int rightMargin = 20;
            int contentWidth = this.ClientSize.Width - leftMargin - rightMargin;
            
            // Include Top-Level Folders
            var lblIncludeFolders = new Label
            {
                Text = "File Organization:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(leftMargin, currentY),
                AutoSize = true
            };
            currentY += 22;
            
            _chkIncludeFolders = new CheckBox
            {
                Text = "Include Top-Level Folders (Move entire folders, contents inside will not be touched)",
                Location = new Point(leftMargin, currentY),
                Size = new Size(contentWidth, 50),
                Checked = _settings.IncludeTopLevelFolders
            };
            currentY += 55;
            
            // Progress Display
            var lblProgress = new Label
            {
                Text = "Progress Display:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(leftMargin, currentY),
                AutoSize = true
            };
            currentY += 22;
            
            _chkShowProgress = new CheckBox
            {
                Text = "Show Detailed Progress",
                Location = new Point(leftMargin, currentY),
                AutoSize = true,
                Checked = _settings.ShowDetailedProgress
            };
            currentY += spacing;
            
            // Window Behavior
            var lblWindow = new Label
            {
                Text = "Window Behavior:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(leftMargin, currentY),
                AutoSize = true
            };
            currentY += 22;
            
            _chkShowOnTop = new CheckBox
            {
                Text = "Keep window on top",
                Location = new Point(leftMargin, currentY),
                AutoSize = true,
                Checked = _settings.ShowOnTop
            };
            currentY += spacing;
            
            // Folder Format
            var lblFormat = new Label
            {
                Text = "Folder Name Format:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(leftMargin, currentY),
                AutoSize = true
            };
            currentY += 22;
            
            _rbYearMonth = new RadioButton
            {
                Text = "Year Month (e.g., \"2024 January\")",
                Location = new Point(leftMargin, currentY),
                AutoSize = true,
                Checked = _settings.FolderFormat == FolderFormat.YearMonth
            };
            currentY += 22;
            
            _rbMonthYear = new RadioButton
            {
                Text = "Month Year (e.g., \"January 2024\")",
                Location = new Point(leftMargin, currentY),
                AutoSize = true,
                Checked = _settings.FolderFormat == FolderFormat.MonthYear
            };
            currentY += 35;
            
            // Buttons - centered at bottom
            int buttonY = this.ClientSize.Height - 50;
            int totalButtonWidth = 100 + 10 + 100; // OK + spacing + Cancel
            int buttonStartX = (this.ClientSize.Width - totalButtonWidth) / 2;
            
            _btnOK = new Button
            {
                Text = "OK",
                Size = new Size(100, 32),
                Location = new Point(buttonStartX, buttonY),
                DialogResult = DialogResult.OK
            };
            _btnOK.Click += BtnOK_Click;
            
            _btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 32),
                Location = new Point(buttonStartX + 110, buttonY),
                DialogResult = DialogResult.Cancel
            };
            
            this.Controls.Add(lblIncludeFolders);
            this.Controls.Add(_chkIncludeFolders);
            this.Controls.Add(lblProgress);
            this.Controls.Add(_chkShowProgress);
            this.Controls.Add(lblWindow);
            this.Controls.Add(_chkShowOnTop);
            this.Controls.Add(lblFormat);
            this.Controls.Add(_rbMonthYear);
            this.Controls.Add(_rbYearMonth);
            this.Controls.Add(_btnOK);
            this.Controls.Add(_btnCancel);
            
            this.AcceptButton = _btnOK;
            this.CancelButton = _btnCancel;
        }
        
        private void BtnOK_Click(object? sender, EventArgs e)
        {
            _settings.IncludeTopLevelFolders = _chkIncludeFolders.Checked;
            _settings.ShowDetailedProgress = _chkShowProgress.Checked;
            _settings.ShowOnTop = _chkShowOnTop.Checked;
            _settings.FolderFormat = _rbMonthYear.Checked ? FolderFormat.MonthYear : FolderFormat.YearMonth;
        }
    }
}
