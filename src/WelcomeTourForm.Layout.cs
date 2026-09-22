using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Controls;

namespace FileOrganizer
{
    public partial class WelcomeTourForm
    {
        // UI Controls
        private Panel _pnlTop = null!;
        private Label _lblAppTitle = null!;
        private Label _lblStepIndicator = null!;
        private Label[] _dotLabels = null!;
        private Panel _card = null!;
        private Label _lblBadge = null!;
        private Label _lblSlideTitle = null!;
        private Label _lblSlideSubtitle = null!;
        private Panel _pnlThemeChooser = null!;
        private ModernButton _btnTourThemeLight = null!;
        private ModernButton _btnTourThemeDark = null!;
        private TableLayoutPanel _pnlBullets = null!;
        private Panel _pnlBottom = null!;
        private CheckBox _chkDontShowAgain = null!;
        private ModernButton _btnSkip = null!;
        private ModernButton _btnPrev = null!;
        private ModernButton _btnNext = null!;

        private void InitializeComponent()
        {
            this.Text = $"Welcome to {AppConstants.ShortAppName} — Quick Tour";
            if (AppConstants.AppIcon != null) this.Icon = AppConstants.AppIcon;
            this.Size = new Size(670, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = _palette.CanvasBg;
            this.ForeColor = _palette.TextPrimary;
            this.KeyPreview = true;
            this.Shown += (s, e) => AppTheme.SetWindowDarkTitleBar(this.Handle, _isDark);
            this.KeyDown += WelcomeTourForm_KeyDown;

            // 1. Top Header Banner
            _pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(20, 10, 20, 10),
                BackColor = _palette.CardBg
            };
            _pnlTop.Paint += (s, pe) =>
            {
                using var pen = new Pen(_palette.CardBorder);
                pe.Graphics.DrawLine(pen, 0, _pnlTop.Height - 1, _pnlTop.Width, _pnlTop.Height - 1);
            };

            var picLogo = new PictureBox
            {
                Image = AppConstants.AppLogo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(32, 32),
                Location = new Point(20, 12)
            };

            _lblAppTitle = new Label
            {
                Text = AppConstants.AppName,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = _palette.TextPrimary,
                Location = new Point(58, 16),
                AutoSize = true
            };

            _lblStepIndicator = new Label
            {
                Text = $"Step 1 of {_slides.Length}",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = _palette.TextMuted,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(400, 18),
                Size = new Size(110, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            var pnlDots = new FlowLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(516, 17),
                Size = new Size(126, 22),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _dotLabels = new Label[_slides.Length];
            for (int i = 0; i < _slides.Length; i++)
            {
                int index = i;
                var dot = new Label
                {
                    Text = "●",
                    Font = new Font("Segoe UI", 11F),
                    ForeColor = _palette.TextMuted,
                    Size = new Size(18, 20),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                dot.Click += (s, e) => ShowSlide(index);
                _dotLabels[i] = dot;
                pnlDots.Controls.Add(dot);
            }

            _pnlTop.Controls.AddRange(new Control[] { picLogo, _lblAppTitle, _lblStepIndicator, pnlDots });

            // 2. Center Slide Card
            var pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 10),
                BackColor = Color.Transparent
            };

            _card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _palette.CardBg,
                Padding = new Padding(20, 16, 20, 16)
            };
            _card.Paint += (s, pe) =>
            {
                using var pen = new Pen(_palette.CardBorder);
                pe.Graphics.DrawRectangle(pen, 0, 0, _card.Width - 1, _card.Height - 1);
            };

            _lblBadge = new Label
            {
                Text = "APPEARANCE",
                UseMnemonic = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(99, 102, 241),
                Padding = new Padding(6, 2, 6, 2),
                AutoSize = true,
                Location = new Point(20, 14)
            };

            _lblSlideTitle = new Label
            {
                UseMnemonic = false,
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = _palette.TextPrimary,
                Location = new Point(18, 42),
                Size = new Size(590, 30),
                AutoEllipsis = true
            };

            _lblSlideSubtitle = new Label
            {
                UseMnemonic = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = _palette.TextMuted,
                Location = new Point(20, 72),
                Size = new Size(590, 22),
                AutoEllipsis = true
            };

            // Slide 1 Theme Selector Card
            _pnlThemeChooser = new Panel
            {
                Location = new Point(20, 100),
                Size = new Size(590, 44),
                BackColor = Color.Transparent,
                Visible = true
            };

            _btnTourThemeLight = new ModernButton
            {
                Text = "☀️  Light Mode (Classic)",
                Size = new Size(286, 40),
                Location = new Point(0, 2),
                Font = new Font("Segoe UI Emoji", 9.5F, FontStyle.Regular),
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            _btnTourThemeLight.Click += (s, e) => SetTourTheme(false);

            _btnTourThemeDark = new ModernButton
            {
                Text = "🌙  Dark Mode (Fluent Slate)",
                Size = new Size(286, 40),
                Location = new Point(300, 2),
                Font = new Font("Segoe UI Emoji", 9.5F, FontStyle.Regular),
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };
            _btnTourThemeDark.Click += (s, e) => SetTourTheme(true);

            _pnlThemeChooser.Controls.AddRange(new Control[] { _btnTourThemeLight, _btnTourThemeDark });

            _pnlBullets = new TableLayoutPanel
            {
                Location = new Point(20, 152),
                Size = new Size(590, 188),
                ColumnCount = 1,
                RowCount = 5,
                AutoScroll = true
            };
            _pnlBullets.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _card.Controls.AddRange(new Control[] { _lblBadge, _lblSlideTitle, _lblSlideSubtitle, _pnlThemeChooser, _pnlBullets });
            pnlCenter.Controls.Add(_card);

            // 3. Bottom Action Bar
            _pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(20, 12, 20, 12),
                BackColor = _palette.CardBg
            };
            _pnlBottom.Paint += (s, pe) =>
            {
                using var pen = new Pen(_palette.CardBorder);
                pe.Graphics.DrawLine(pen, 0, 0, _pnlBottom.Width, 0);
            };

            _chkDontShowAgain = new CheckBox
            {
                Text = "Don't show this tour on startup",
                Font = new Font("Segoe UI", 9F),
                ForeColor = _palette.TextMuted,
                Checked = true,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            _btnSkip = new ModernButton
            {
                Text = "Skip Tour",
                Size = new Size(86, 34),
                Location = new Point(310, 13),
                BackColor = _palette.CardBg,
                BorderColor = _palette.SecondaryButtonBorder,
                ForeColor = _palette.TextMuted,
                BorderRadius = 6
            };
            _btnSkip.Click += (s, e) => this.Close();

            _btnPrev = new ModernButton
            {
                Text = "< Back",
                Size = new Size(86, 34),
                Location = new Point(404, 13),
                BackColor = _palette.SecondaryButtonBg,
                BorderColor = _palette.SecondaryButtonBorder,
                ForeColor = _palette.SecondaryButtonText,
                BorderRadius = 6,
                Enabled = false
            };
            _btnPrev.Click += (s, e) => ShowSlide(_currentSlideIndex - 1);

            _btnNext = new ModernButton
            {
                Text = "Next >",
                Size = new Size(140, 34),
                Location = new Point(498, 13),
                BackColor = Color.FromArgb(37, 99, 235),
                BorderColor = Color.Transparent,
                ForeColor = Color.White,
                BorderRadius = 6
            };
            _btnNext.Click += (s, e) =>
            {
                if (_currentSlideIndex < _slides.Length - 1)
                    ShowSlide(_currentSlideIndex + 1);
                else
                    this.Close();
            };

            _pnlBottom.Controls.AddRange(new Control[] { _chkDontShowAgain, _btnSkip, _btnPrev, _btnNext });

            this.Controls.AddRange(new Control[] { pnlCenter, _pnlBottom, _pnlTop });
            UpdateThemeButtonsState();
        }

        private void UpdateThemeButtonsState()
        {
            if (_btnTourThemeLight == null || _btnTourThemeDark == null) return;

            if (!_isDark)
            {
                _btnTourThemeLight.BackColor = Color.FromArgb(224, 231, 255);
                _btnTourThemeLight.ForeColor = Color.FromArgb(30, 58, 138);
                _btnTourThemeLight.BorderColor = Color.FromArgb(59, 130, 246);
                _btnTourThemeLight.Font = new Font("Segoe UI Emoji", 9.5F, FontStyle.Bold);

                _btnTourThemeDark.BackColor = _palette.SecondaryButtonBg;
                _btnTourThemeDark.ForeColor = _palette.TextMuted;
                _btnTourThemeDark.BorderColor = _palette.SecondaryButtonBorder;
                _btnTourThemeDark.Font = new Font("Segoe UI Emoji", 9.5F, FontStyle.Regular);
            }
            else
            {
                _btnTourThemeDark.BackColor = Color.FromArgb(30, 58, 138);
                _btnTourThemeDark.ForeColor = Color.White;
                _btnTourThemeDark.BorderColor = Color.FromArgb(96, 165, 250);
                _btnTourThemeDark.Font = new Font("Segoe UI Emoji", 9.5F, FontStyle.Bold);

                _btnTourThemeLight.BackColor = _palette.SecondaryButtonBg;
                _btnTourThemeLight.ForeColor = _palette.TextMuted;
                _btnTourThemeLight.BorderColor = _palette.SecondaryButtonBorder;
                _btnTourThemeLight.Font = new Font("Segoe UI Emoji", 9.5F, FontStyle.Regular);
            }
        }

        private void ApplyTourTheme(bool isDark)
        {
            _isDark = isDark;
            _palette = AppTheme.GetPalette(_isDark);

            this.BackColor = _palette.CanvasBg;
            this.ForeColor = _palette.TextPrimary;
            AppTheme.SetWindowDarkTitleBar(this.Handle, _isDark);

            if (_pnlTop != null)
            {
                _pnlTop.BackColor = _palette.CardBg;
                _lblAppTitle.ForeColor = _palette.TextPrimary;
                _lblStepIndicator.ForeColor = _palette.TextMuted;
            }

            if (_card != null)
            {
                _card.BackColor = _palette.CardBg;
                _lblSlideTitle.ForeColor = _palette.TextPrimary;
                _lblSlideSubtitle.ForeColor = _palette.TextMuted;
            }

            if (_pnlBullets != null)
            {
                foreach (Control c in _pnlBullets.Controls)
                {
                    if (c is Label lbl)
                    {
                        lbl.ForeColor = _palette.TextPrimary;
                    }
                }
            }

            if (_dotLabels != null)
            {
                for (int i = 0; i < _dotLabels.Length; i++)
                {
                    if (i != _currentSlideIndex)
                    {
                        _dotLabels[i].ForeColor = _isDark ? Color.FromArgb(75, 85, 99) : Color.FromArgb(209, 213, 219);
                    }
                }
            }

            if (_pnlBottom != null)
            {
                _pnlBottom.BackColor = _palette.CardBg;
                _chkDontShowAgain.ForeColor = _palette.TextMuted;
                _btnSkip.BackColor = _palette.CardBg;
                _btnSkip.BorderColor = _palette.SecondaryButtonBorder;
                _btnSkip.ForeColor = _palette.TextMuted;

                _btnPrev.BackColor = _palette.SecondaryButtonBg;
                _btnPrev.BorderColor = _palette.SecondaryButtonBorder;
                _btnPrev.ForeColor = _palette.SecondaryButtonText;
            }

            UpdateThemeButtonsState();
            _card?.Invalidate();
            _pnlTop?.Invalidate();
            _pnlBottom?.Invalidate();
        }
    }
}
