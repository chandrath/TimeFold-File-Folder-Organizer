using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FileOrganizer.Models;

namespace FileOrganizer
{
    public partial class MainForm
    {
        private void MenuFileNew_Click(object? sender, EventArgs e)
        {
            ResetForm();
        }

        private void MenuFileExit_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuPreferences_Click(object? sender, EventArgs e)
        {
            using var prefsForm = new PreferencesForm(_settings);
            if (prefsForm.ShowDialog() == DialogResult.OK)
            {
                _settings = prefsForm.Settings;
                ApplySettings();
                LoadPreview();
            }
        }

        private void MenuAbout_Click(object? sender, EventArgs e)
        {
            using var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
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

        private void ChkUseSourceAsOutput_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateOutputFolder();
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

        private void LoadPreview()
        {
            if (_organizerService == null) return;

            try
            {
                _filesToOrganize = _organizerService.ScanFiles(_settings.IncludeTopLevelFolders, _settings.IgnoreSystemFiles);
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
                var item = new ListViewItem($"... and {_filesToOrganize.Count - 1000} more files")
                {
                    ForeColor = Color.Gray
                };
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
                var sb = new StringBuilder();
                sb.AppendLine($"⚠ Conflicts Detected: {conflicts.Count}");
                foreach (var conflict in conflicts.Take(5))
                {
                    sb.AppendLine($"• {conflict}");
                }
                if (conflicts.Count > 5)
                {
                    sb.AppendLine($"• ... and {conflicts.Count - 5} more");
                }
                sb.Append("→ Conflicting files will be automatically renamed");
                _lblConflicts.Text = sb.ToString();
                _pnlConflicts.Visible = true;
            }
            else
            {
                _pnlConflicts.Visible = false;
            }
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

                if (_settings.ShowDetailedProgress)
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
                    _cancellationTokenSource.Token,
                    _settings.GenerateCsvLog);

                ShowCompletion(orgResult);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Organization was cancelled. Any processed files have been logged.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _lastResult = result;

            var summaryBuilder = new StringBuilder();
            summaryBuilder.AppendLine("PERFORMANCE SUMMARY");
            summaryBuilder.AppendLine($"  • Files Moved:             {result.FilesMoved} of {result.TotalFiles}");
            summaryBuilder.AppendLine($"  • Month Folders Created:   {result.MonthFoldersCreated}");
            summaryBuilder.AppendLine($"  • Name Conflicts Renamed:  {result.ConflictsResolved}");

            if (result.Errors > 0)
            {
                summaryBuilder.AppendLine($"  ⚠ Errors Encountered:      {result.Errors}");
            }

            summaryBuilder.AppendLine();
            summaryBuilder.AppendLine("OUTPUT DESTINATION");
            summaryBuilder.AppendLine($"  📂 {(string.IsNullOrWhiteSpace(result.SortedFolderPath) ? "Not available" : result.SortedFolderPath)}");
            summaryBuilder.AppendLine();
            summaryBuilder.AppendLine("AUDIT LOG");
            summaryBuilder.AppendLine($"  📊 {(string.IsNullOrWhiteSpace(result.CsvLogPath) ? "Not generated" : result.CsvLogPath)}");

            _lblCompleteSummary.Text = summaryBuilder.ToString();

            bool outputExists = !string.IsNullOrWhiteSpace(result.SortedFolderPath) && Directory.Exists(result.SortedFolderPath);
            _btnOpenFolder.Enabled = outputExists;
            CenterCompletionButtons();
            UpdateMainLayout();
        }

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            var targetPath = _lastResult?.SortedFolderPath;
            if (!string.IsNullOrWhiteSpace(targetPath) && Directory.Exists(targetPath))
            {
                Process.Start("explorer.exe", targetPath);
                return;
            }

            var fallback = _organizerService?.OutputDirectory;
            if (!string.IsNullOrWhiteSpace(fallback) && Directory.Exists(fallback))
            {
                Process.Start("explorer.exe", fallback);
                return;
            }

            MessageBox.Show("Output folder is not available yet.", "Folder Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStartNewProject_Click(object? sender, EventArgs e)
        {
            ResetForm();
        }
    }
}
