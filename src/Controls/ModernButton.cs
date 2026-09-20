using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FileOrganizer.Controls
{
    public class ModernButton : Button
    {
        private int _borderRadius = 8;
        private Color _borderColor = Color.Transparent;
        private bool _isHovered = false;
        private bool _isPressed = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                UpdateRegion();
                this.Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                this.Invalidate();
            }
        }

        public ModernButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Size = new Size(150, 40);
            this.BackColor = Color.FromArgb(37, 99, 235);
            this.ForeColor = Color.White;
            this.Cursor = Cursors.Hand;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
            this.Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            this.Cursor = this.Enabled ? Cursors.Hand : Cursors.Default;
            this.Invalidate();
        }

        private void UpdateRegion()
        {
            if (!this.IsHandleCreated || this.Width <= 0 || this.Height <= 0)
                return;

            int effectiveRadius = Math.Min(_borderRadius, this.Height);
            RectangleF rectSurface = new RectangleF(0, 0, this.Width, this.Height);

            var oldRegion = this.Region;
            if (effectiveRadius > 2)
            {
                using GraphicsPath pathSurface = GetFigurePath(rectSurface, effectiveRadius);
                this.Region = new Region(pathSurface);
            }
            else
            {
                this.Region = new Region(rectSurface);
            }
            oldRegion?.Dispose();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int effectiveRadius = Math.Min(_borderRadius, this.Height);
            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            RectangleF rectBorder = new RectangleF(0.5f, 0.5f, this.Width - 1, this.Height - 1);

            Color currentBg = !this.Enabled
                ? Color.FromArgb(241, 245, 249) // Slate 100
                : (_isPressed ? Darken(this.BackColor, 0.12f) : (_isHovered ? Lighten(this.BackColor, 0.08f) : this.BackColor));

            Color currentBorder = !this.Enabled
                ? Color.FromArgb(226, 232, 240) // Slate 200
                : (_isHovered && _borderColor != Color.Transparent ? Darken(_borderColor, 0.15f) : _borderColor);

            Color currentText = !this.Enabled
                ? Color.FromArgb(148, 163, 184) // Slate 400 (Always clean light gray, NEVER black!)
                : this.ForeColor;

            // Paint background surface
            if (effectiveRadius > 2)
            {
                using GraphicsPath pathSurface = GetFigurePath(rect, effectiveRadius);
                using Brush brushBg = new SolidBrush(currentBg);
                g.FillPath(brushBg, pathSurface);

                if (currentBorder != Color.Transparent)
                {
                    using GraphicsPath pathBorder = GetFigurePath(rectBorder, Math.Max(1f, effectiveRadius - 1f));
                    using Pen penBorder = new Pen(currentBorder, 1.2f) { Alignment = PenAlignment.Inset };
                    g.DrawPath(penBorder, pathBorder);
                }
            }
            else
            {
                using Brush brushBg = new SolidBrush(currentBg);
                g.FillRectangle(brushBg, rect);

                if (currentBorder != Color.Transparent)
                {
                    using Pen penBorder = new Pen(currentBorder, 1f);
                    g.DrawRectangle(penBorder, 0, 0, this.Width - 1, this.Height - 1);
                }
            }

            // Paint text centered with high-quality rendering
            TextRenderer.DrawText(
                g,
                this.Text,
                this.Font,
                rect,
                currentText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.WordEllipsis);
        }

        private static GraphicsPath GetFigurePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Lighten(Color color, float factor)
        {
            int r = Math.Min(255, (int)(color.R + (255 - color.R) * factor));
            int g = Math.Min(255, (int)(color.G + (255 - color.R) * factor));
            int b = Math.Min(255, (int)(color.B + (255 - color.B) * factor));
            return Color.FromArgb(color.A, r, g, b);
        }

        private static Color Darken(Color color, float factor)
        {
            int r = Math.Max(0, (int)(color.R * (1 - factor)));
            int g = Math.Max(0, (int)(color.G * (1 - factor)));
            int b = Math.Max(0, (int)(color.B * (1 - factor)));
            return Color.FromArgb(color.A, r, g, b);
        }
    }
}
