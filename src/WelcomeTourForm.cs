using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FileOrganizer.Config;
using FileOrganizer.Controls;

namespace FileOrganizer
{
    public class WelcomeTourForm : Form
    {
        private readonly bool _isDark;
        private readonly AppTheme.ThemePalette _palette;
        private int _currentSlideIndex = 0;
        private readonly TourSlide[] _slides;

        // UI Controls
        private Label _lblStepIndicator = null!;
        private Label[] _dotLabels = null!;
        private Label _lblBadge = null!;
        private Label _lblSlideTitle = null!;
        private Label _lblSlideSubtitle = null!;
        private TableLayoutPanel _pnlBullets = null!;
        private CheckBox _chkDontShowAgain = null!;
        private ModernButton _btnPrev = null!;
        private ModernButton _btnNext = null!;

        public bool DontShowOnStartup => _chkDontShowAgain.Checked;

        public WelcomeTourForm(bool isDark = false)
        {
            _isDark = isDark;
            _palette = AppTheme.GetPalette(_isDark);
            _slides = InitializeSlides();

            InitializeComponent();
            ShowSlide(0);
        }

        private TourSlide[] InitializeSlides()
        {
            var now = DateTime.Now;
            var lastMonth = now.AddMonths(-1);
            var threeMonthsAgo = now.AddMonths(-3);
            int currentQuarter = ((now.Month - 1) / 3) + 1;

            return new[]
            {
                new TourSlide(
                    "⚠️",
                    "THE PROBLEM",
                    Color.FromArgb(239, 68, 68),
                    "Is Your Folder Slow, Cluttered, and Freezing?",
                    "Thousands of unorganized files in one place ruin productivity.",
                    new[]
                    {
                        "Sluggish Windows Explorer: Folders like Downloads, Screenshots, Photos, or Desktop take forever to load and can freeze your PC.",
                        "Lost in the Chaos: Scrolling through hundreds or thousands of loose files to locate one specific document wastes time.",
                        "Zero Structure: Years of mixed downloads, media, and receipts pile up into an unmanageable mountain of clutter.",
                        "The Solution: TimeFold was built to solve this exact headache cleanly in just one single click."
                    }
                ),
                new TourSlide(
                    "⚡",
                    "THE 1-CLICK SOLUTION",
                    Color.FromArgb(14, 165, 233),
                    "Instant Order: Smart Date-Wise Organization",
                    "Intelligently examines true file timestamps and groups items into dedicated folders.",
                    new[]
                    {
                        $"Dedicated Monthly Folders: Files from last month automatically move into their own folder (e.g., {lastMonth:yyyy-MM} or {lastMonth:yyyy MMM}).",
                        $"Older Archives: Files from three months ago move cleanly into {threeMonthsAgo:yyyy-MM} ({threeMonthsAgo:yyyy MMM}).",
                        $"Day-by-Day Organization: Want daily sorting? Choose day-wise folders (e.g., {now:yyyy-MM-dd}) — ideal for daily screenshots or camera photos.",
                        $"Quarterly Grouping: Group by quarter (e.g., {now.Year}-Q{currentQuarter}) for invoices, tax receipts, and quarterly archives.",
                        $"Custom Naming: Add custom prefixes and suffixes (e.g., Photos_{lastMonth:yyyy-MM}) to match your exact naming preferences."
                    }
                ),
                new TourSlide(
                    "🛡️",
                    "SAFE & REVERSIBLE",
                    Color.FromArgb(16, 185, 129),
                    "Preview First, Move with Confidence",
                    "Nothing is moved blindly. You stay in 100% control at every step.",
                    new[]
                    {
                        "Full Interactive Preview: Inspect every item and see its exact destination folder before moving a single file.",
                        "100% Non-Destructive: TimeFold only reorganizes your files into neat date folders — it never deletes, alters, or compresses your original files.",
                        "Automatic CSV Audit Log: Every organization creates a detailed timestamped CSV log so you always know where files went.",
                        "In-Place or Custom Output: Organize directly inside the source folder or route sorted files to an external backup drive."
                    }
                ),
                new TourSlide(
                    "🚀",
                    "SCALE & SPEED",
                    Color.FromArgb(168, 85, 247),
                    "Heavy-Duty Speed for 100,000+ Items",
                    "Engineered to effortlessly handle massive personal and professional libraries.",
                    new[]
                    {
                        "Blazing Fast Engine: Seamlessly browse 10,000 to 100,000+ items with smooth paginated loading without freezing your PC.",
                        "Smart Timestamp Detection: Automatically warns you if files share identical timestamps (common with downloaded ZIPs or chat media).",
                        "Include Folders Option: Optionally organize entire loose folders alongside files with a single checkbox.",
                        "Drag & Drop Ready: Simply drag and drop any folder into TimeFold to start organizing immediately!"
                    }
                )
            };
        }

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
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(20, 10, 20, 10),
                BackColor = _palette.CardBg
            };
            pnlTop.Paint += (s, pe) =>
            {
                using var pen = new Pen(_palette.CardBorder);
                pe.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            var picLogo = new PictureBox
            {
                Image = AppConstants.AppLogo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(32, 32),
                Location = new Point(20, 12)
            };

            var lblAppTitle = new Label
            {
                Text = AppConstants.AppName,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = _palette.TextPrimary,
                Location = new Point(58, 16),
                AutoSize = true
            };

            _lblStepIndicator = new Label
            {
                Text = "Step 1 of 4",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = _palette.TextMuted,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(460, 18),
                Size = new Size(80, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            var pnlDots = new FlowLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(546, 17),
                Size = new Size(96, 22),
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

            pnlTop.Controls.AddRange(new Control[] { picLogo, lblAppTitle, _lblStepIndicator, pnlDots });

            // 2. Center Slide Card
            var pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 10),
                BackColor = Color.Transparent
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _palette.CardBg,
                Padding = new Padding(20, 16, 20, 16)
            };
            card.Paint += (s, pe) =>
            {
                using var pen = new Pen(_palette.CardBorder);
                pe.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            _lblBadge = new Label
            {
                Text = "THE PROBLEM",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(239, 68, 68),
                Padding = new Padding(6, 2, 6, 2),
                AutoSize = true,
                Location = new Point(20, 14)
            };

            _lblSlideTitle = new Label
            {
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = _palette.TextPrimary,
                Location = new Point(18, 42),
                Size = new Size(590, 30),
                AutoEllipsis = true
            };

            _lblSlideSubtitle = new Label
            {
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = _palette.TextMuted,
                Location = new Point(20, 72),
                Size = new Size(590, 22),
                AutoEllipsis = true
            };

            _pnlBullets = new TableLayoutPanel
            {
                Location = new Point(20, 100),
                Size = new Size(590, 240),
                ColumnCount = 1,
                RowCount = 5,
                AutoScroll = true
            };
            _pnlBullets.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            card.Controls.AddRange(new Control[] { _lblBadge, _lblSlideTitle, _lblSlideSubtitle, _pnlBullets });
            pnlCenter.Controls.Add(card);

            // 3. Bottom Action Bar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(20, 12, 20, 12),
                BackColor = _palette.CardBg
            };
            pnlBottom.Paint += (s, pe) =>
            {
                using var pen = new Pen(_palette.CardBorder);
                pe.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
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

            var btnSkip = new ModernButton
            {
                Text = "Skip Tour",
                Size = new Size(86, 34),
                Location = new Point(310, 13),
                BackColor = _palette.CardBg,
                BorderColor = _palette.SecondaryButtonBorder,
                ForeColor = _palette.TextMuted,
                BorderRadius = 6
            };
            btnSkip.Click += (s, e) => this.Close();

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

            pnlBottom.Controls.AddRange(new Control[] { _chkDontShowAgain, btnSkip, _btnPrev, _btnNext });

            this.Controls.AddRange(new Control[] { pnlCenter, pnlBottom, pnlTop });
        }

        private void ShowSlide(int index)
        {
            if (index < 0 || index >= _slides.Length) return;
            _currentSlideIndex = index;

            var slide = _slides[index];
            _lblStepIndicator.Text = $"Step {index + 1} of {_slides.Length}";

            // Update dots
            for (int i = 0; i < _dotLabels.Length; i++)
            {
                if (i == index)
                {
                    _dotLabels[i].ForeColor = Color.FromArgb(37, 99, 235);
                    _dotLabels[i].Text = "●";
                }
                else
                {
                    _dotLabels[i].ForeColor = _isDark ? Color.FromArgb(75, 85, 99) : Color.FromArgb(209, 213, 219);
                    _dotLabels[i].Text = "○";
                }
            }

            _lblBadge.Text = $"{slide.BadgeIcon} {slide.BadgeText}";
            _lblBadge.BackColor = slide.BadgeColor;
            _lblSlideTitle.Text = slide.Title;
            _lblSlideSubtitle.Text = slide.Subtitle;

            // Rebuild bullet list
            _pnlBullets.SuspendLayout();
            _pnlBullets.Controls.Clear();
            _pnlBullets.RowCount = slide.BulletPoints.Length;

            for (int i = 0; i < slide.BulletPoints.Length; i++)
            {
                var lblBullet = new Label
                {
                    Text = slide.BulletPoints[i],
                    Font = new Font("Segoe UI", 9.25F),
                    ForeColor = _palette.TextPrimary,
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 10),
                    MaximumSize = new Size(570, 0)
                };
                _pnlBullets.Controls.Add(lblBullet, 0, i);
            }
            _pnlBullets.ResumeLayout();

            // Button states
            _btnPrev.Enabled = index > 0;
            if (index == _slides.Length - 1)
            {
                _btnNext.Text = "🎉 Start Organizing";
                _btnNext.BackColor = Color.FromArgb(16, 185, 129); // Emerald
            }
            else
            {
                _btnNext.Text = "Next >";
                _btnNext.BackColor = Color.FromArgb(37, 99, 235); // Blue
            }
        }

        private void WelcomeTourForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right)
            {
                if (_currentSlideIndex < _slides.Length - 1) ShowSlide(_currentSlideIndex + 1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (_currentSlideIndex > 0) ShowSlide(_currentSlideIndex - 1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }

        private record TourSlide(
            string BadgeIcon,
            string BadgeText,
            Color BadgeColor,
            string Title,
            string Subtitle,
            string[] BulletPoints
        );
    }
}
