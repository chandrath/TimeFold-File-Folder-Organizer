using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FileOrganizer.Config
{
    /// <summary>
    /// Single Source of Truth (SSoT) for Light and Dark UI Themes.
    /// Uses modern Tailwind Slate & Windows 11 Fluent design palettes.
    /// </summary>
    public static class AppTheme
    {
        public class ThemePalette
        {
            public bool IsDark { get; init; }
            public Color CanvasBg { get; init; }
            public Color CardBg { get; init; }
            public Color CardBorder { get; init; }
            public Color TextPrimary { get; init; }
            public Color TextMuted { get; init; }
            public Color InputBg { get; init; }
            public Color InputDisabledBg { get; init; }
            public Color InputBorder { get; init; }
            public Color DropZoneBg { get; init; }
            public Color DropZoneBorder { get; init; }
            public Color DropZoneText { get; init; }
            public Color DropZoneHoverBg { get; init; }
            public Color ListBg { get; init; }
            public Color ListText { get; init; }
            public Color MenuBg { get; init; }
            public Color MenuText { get; init; }
            public Color SecondaryButtonBg { get; init; }
            public Color SecondaryButtonBorder { get; init; }
            public Color SecondaryButtonText { get; init; }
            public Color BadgeBg { get; init; }
            public Color BadgeText { get; init; }
            public Color BadgeBorder { get; init; }
            public Color WarningBg { get; init; }
            public Color WarningText { get; init; }
            public Color DangerBg { get; init; }
            public Color DangerText { get; init; }
            public Color ListDateActive { get; init; }
            public Color ListDateMuted { get; init; }
            public Color ListTargetFolder { get; init; }
        }

        public static readonly ThemePalette Light = new()
        {
            IsDark = false,
            CanvasBg = Color.FromArgb(248, 250, 252),        // Slate 50
            CardBg = Color.White,
            CardBorder = Color.FromArgb(226, 232, 240),      // Slate 200
            TextPrimary = Color.FromArgb(15, 23, 42),        // Slate 900
            TextMuted = Color.FromArgb(100, 116, 139),       // Slate 500
            InputBg = Color.White,
            InputDisabledBg = Color.FromArgb(241, 245, 249), // Slate 100
            InputBorder = Color.FromArgb(209, 213, 219),     // Gray 300
            DropZoneBg = Color.FromArgb(240, 247, 255),      // Soft Blue Tint
            DropZoneBorder = Color.FromArgb(147, 197, 253),  // Blue 300
            DropZoneText = Color.FromArgb(37, 99, 235),      // Blue 600
            DropZoneHoverBg = Color.FromArgb(219, 234, 254), // Blue 100
            ListBg = Color.White,
            ListText = Color.FromArgb(15, 23, 42),
            MenuBg = Color.White,
            MenuText = Color.FromArgb(15, 23, 42),
            SecondaryButtonBg = Color.White,
            SecondaryButtonBorder = Color.FromArgb(209, 213, 219),
            SecondaryButtonText = Color.FromArgb(30, 41, 59),
            BadgeBg = Color.FromArgb(238, 242, 255),
            BadgeText = Color.FromArgb(67, 56, 202),
            BadgeBorder = Color.FromArgb(199, 210, 254),
            WarningBg = Color.FromArgb(254, 243, 199),
            WarningText = Color.FromArgb(146, 64, 14),
            DangerBg = Color.FromArgb(254, 242, 242),
            DangerText = Color.FromArgb(220, 38, 38),
            ListDateActive = Color.FromArgb(29, 78, 216),    // Royal Blue 700
            ListDateMuted = Color.FromArgb(100, 116, 139),   // Slate 500
            ListTargetFolder = Color.FromArgb(67, 56, 202)   // Deep Indigo 700
        };

        public static readonly ThemePalette Dark = new()
        {
            IsDark = true,
            CanvasBg = Color.FromArgb(15, 23, 42),           // Slate 900
            CardBg = Color.FromArgb(30, 41, 59),            // Slate 800
            CardBorder = Color.FromArgb(51, 65, 85),         // Slate 700
            TextPrimary = Color.FromArgb(248, 250, 252),     // Slate 50
            TextMuted = Color.FromArgb(148, 163, 184),       // Slate 400
            InputBg = Color.FromArgb(30, 41, 59),
            InputDisabledBg = Color.FromArgb(15, 23, 42),
            InputBorder = Color.FromArgb(71, 85, 105),       // Slate 600
            DropZoneBg = Color.FromArgb(30, 41, 59),
            DropZoneBorder = Color.FromArgb(59, 130, 246),   // Blue 500
            DropZoneText = Color.FromArgb(96, 165, 250),     // Blue 400
            DropZoneHoverBg = Color.FromArgb(30, 58, 138),   // Blue 900
            ListBg = Color.FromArgb(30, 41, 59),
            ListText = Color.FromArgb(248, 250, 252),
            MenuBg = Color.FromArgb(30, 41, 59),
            MenuText = Color.FromArgb(248, 250, 252),
            SecondaryButtonBg = Color.FromArgb(30, 41, 59),
            SecondaryButtonBorder = Color.FromArgb(71, 85, 105),
            SecondaryButtonText = Color.FromArgb(248, 250, 252),
            BadgeBg = Color.FromArgb(49, 46, 129),           // Indigo 900
            BadgeText = Color.FromArgb(165, 180, 252),       // Indigo 300
            BadgeBorder = Color.FromArgb(67, 56, 202),
            WarningBg = Color.FromArgb(69, 26, 3),           // Dark Amber
            WarningText = Color.FromArgb(252, 211, 77),
            DangerBg = Color.FromArgb(69, 10, 10),           // Dark Red
            DangerText = Color.FromArgb(252, 165, 165),
            ListDateActive = Color.FromArgb(56, 189, 248),   // Radiant Sky 400
            ListDateMuted = Color.FromArgb(203, 213, 225),   // Crisp Slate 300
            ListTargetFolder = Color.FromArgb(251, 191, 36)  // Radiant Amber Gold 400
        };

        public static ThemePalette GetPalette(bool isDark) => isDark ? Dark : Light;

        // Desktop Window Manager API for Windows 10/11 Dark Title Bar
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void SetWindowDarkTitleBar(IntPtr hWnd, bool isDark)
        {
            try
            {
                int val = isDark ? 1 : 0;
                DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref val, sizeof(int));
            }
            catch { }
        }

        // Dark Menu Table & Renderer for MenuStrip & ContextMenuStrip
        public class DarkColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(51, 65, 85);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(51, 65, 85);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(51, 65, 85);
            public override Color MenuBorder => Color.FromArgb(71, 85, 105);
            public override Color MenuItemBorder => Color.Transparent;
            public override Color ToolStripDropDownBackground => Color.FromArgb(30, 41, 59);
            public override Color ImageMarginGradientBegin => Color.FromArgb(30, 41, 59);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 41, 59);
            public override Color ImageMarginGradientEnd => Color.FromArgb(30, 41, 59);
            public override Color CheckBackground => Color.FromArgb(37, 99, 235);
            public override Color CheckSelectedBackground => Color.FromArgb(29, 78, 216);
            public override Color CheckPressedBackground => Color.FromArgb(30, 58, 138);
            public override Color SeparatorDark => Color.FromArgb(51, 65, 85);
            public override Color SeparatorLight => Color.Transparent;
            public override Color MenuStripGradientBegin => Color.FromArgb(30, 41, 59);
            public override Color MenuStripGradientEnd => Color.FromArgb(30, 41, 59);
        }

        public static readonly ToolStripRenderer DarkMenuRenderer = new ToolStripProfessionalRenderer(new DarkColorTable());
    }
}
