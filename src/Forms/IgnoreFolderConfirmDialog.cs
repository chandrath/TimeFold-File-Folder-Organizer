using System;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;

namespace FileOrganizer.Forms
{
    public class IgnoreFolderConfirmDialog : Form
    {
        private readonly CheckBox _chkDoNotShow;

        public bool DoNotShowAgain => _chkDoNotShow.Checked;

        public IgnoreFolderConfirmDialog(string folderName, bool isDarkMode)
        {
            var palette = AppTheme.GetPalette(isDarkMode);

            Text = "Ignore Folder Protection";
            Size = new Size(500, 310);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = palette.CanvasBg;
            ForeColor = palette.TextPrimary;
            Font = new Font("Segoe UI", 9F);
            AppTheme.SetWindowDarkTitleBar(Handle, isDarkMode);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(22, 18, 22, 16)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblHeader = new Label
            {
                Text = $"🛡 Ignore Folder '{folderName}'?",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = palette.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };

            var lblMessage = new Label
            {
                Text = $"TimeFold will place a small '{AppConstants.TimefoldIgnoreFileName}' marker file inside this folder so TimeFold can detect which folders to skip during organization.\n\n" +
                       $"💡 Tip: You can also manually paste a '{AppConstants.TimefoldIgnoreFileName}' file into any folder at any time to exclude it from organization.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = isDarkMode ? Color.FromArgb(203, 213, 225) : Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12)
            };

            _chkDoNotShow = new CheckBox
            {
                Text = "Do not show this warning again",
                Checked = false,
                AutoSize = true,
                Cursor = Cursors.Hand,
                ForeColor = palette.TextPrimary,
                Margin = new Padding(0, 4, 0, 16)
            };

            var btnPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                Height = 38,
                Margin = Padding.Empty
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(90, 32),
                BackColor = palette.SecondaryButtonBg,
                ForeColor = palette.SecondaryButtonText,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = palette.SecondaryButtonBorder;

            var btnIgnore = new Button
            {
                Text = "🛡 Ignore Folder",
                DialogResult = DialogResult.OK,
                Size = new Size(130, 32),
                BackColor = AppConstants.ColorPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnIgnore.FlatAppearance.BorderSize = 0;

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnIgnore);

            mainPanel.Controls.Add(lblHeader, 0, 0);
            mainPanel.Controls.Add(lblMessage, 0, 1);
            mainPanel.Controls.Add(_chkDoNotShow, 0, 2);
            mainPanel.Controls.Add(btnPanel, 0, 3);

            Controls.Add(mainPanel);
            AcceptButton = btnIgnore;
            CancelButton = btnCancel;
        }
    }
}
