using System;
using System.Drawing;
using System.Windows.Forms;
using FileOrganizer.Config;

namespace FileOrganizer
{
    public partial class WelcomeTourForm : Form
    {
        private bool _isDark;
        private AppTheme.ThemePalette _palette;
        private int _currentSlideIndex = 0;
        private readonly TourSlide[] _slides;
        private readonly Action<bool>? _onThemeChanged;

        public bool DontShowOnStartup => _chkDontShowAgain.Checked;
        public bool SelectedDarkMode => _isDark;

        public WelcomeTourForm(bool isDark = false, Action<bool>? onThemeChanged = null)
        {
            _isDark = isDark;
            _palette = AppTheme.GetPalette(_isDark);
            _onThemeChanged = onThemeChanged;
            _slides = InitializeSlides();

            InitializeComponent();
            ShowSlide(0);
        }

        private void SetTourTheme(bool isDark)
        {
            _isDark = isDark;
            ApplyTourTheme(_isDark);
            _onThemeChanged?.Invoke(_isDark);
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
                    "🎨",
                    "APPEARANCE",
                    Color.FromArgb(99, 102, 241),
                    "Personalize Your Look & Experience",
                    "Select your preferred visual theme. You can always change this in Preferences.",
                    new[]
                    {
                        "Instant Live Preview: Notice how TimeFold instantly updates to match your selection above.",
                        "Distraction-Free Productivity: Choose Light or Dark theme tailored for daytime focus or night comfort.",
                        "100% Non-Destructive Guarantee: TimeFold never deletes, overwrites, or alters your personal files.",
                        "Ready to explore? Click 'Next' to see how TimeFold solves messy, freezing folders!"
                    }
                ),
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
                    "🗂️",
                    "SMART CATEGORIES",
                    Color.FromArgb(245, 158, 11),
                    "Organize by File Type, Date, or Both",
                    "Tailor your organization strategy with categories and 2-level hybrid nesting.",
                    new[]
                    {
                        "Smart Categories: Automatically group files into intuitive categories (Images, Documents, Audio, Video, 3D, Code, Archives, etc.).",
                        $"Hybrid 2-Level Folders: Combine both dimensions (e.g., Images\\{now:yyyy-MM} or {now:yyyy-MM}\\Images) for deep organization.",
                        "Custom Type Rules: Remap file extensions, add custom folder prefixes & suffixes, or create your own custom categories.",
                        "Smart Companion Pairing: Intelligently keep HTML files with their asset folders and subtitle files beside movies.",
                        "1-Click Switching: Seamlessly toggle between Date, Category, and Hybrid modes directly from the main toolbar."
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

            // Slide 0 displays Theme Selector, other slides hide it
            bool isThemeSlide = index == 0;
            _pnlThemeChooser.Visible = isThemeSlide;
            _pnlBullets.Location = isThemeSlide ? new Point(20, 152) : new Point(20, 100);
            _pnlBullets.Size = isThemeSlide ? new Size(590, 188) : new Size(590, 240);

            // Rebuild bullet list
            _pnlBullets.SuspendLayout();
            _pnlBullets.Controls.Clear();
            _pnlBullets.RowCount = slide.BulletPoints.Length;

            for (int i = 0; i < slide.BulletPoints.Length; i++)
            {
                var lblBullet = new Label
                {
                    Text = slide.BulletPoints[i],
                    UseMnemonic = false,
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
