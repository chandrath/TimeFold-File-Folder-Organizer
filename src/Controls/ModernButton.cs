using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FileOrganizer.Controls
{
    public class ModernButton : Button
    {
        private int _borderRadius = 20;
        private Color _borderColor = Color.PaleVioletRed;

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
            this.BackColor = Color.MediumSlateBlue;
            this.ForeColor = Color.White;
            this.Cursor = Cursors.Hand;
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
            this.Parent?.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int effectiveRadius = Math.Min(_borderRadius, this.Height);
            RectangleF rectSurface = new RectangleF(0, 0, this.Width, this.Height);
            RectangleF rectBorder = new RectangleF(1, 1, this.Width - 1, this.Height - 1);

            if (effectiveRadius > 2)
            {
                using GraphicsPath pathSurface = GetFigurePath(rectSurface, effectiveRadius);
                using GraphicsPath pathBorder = GetFigurePath(rectBorder, Math.Max(1f, effectiveRadius - 1f));
                using Pen penSurface = new Pen(this.Parent?.BackColor ?? Color.White, 2);
                using Pen penBorder = new Pen(_borderColor, 1.6f);

                penBorder.Alignment = PenAlignment.Inset;
                pevent.Graphics.DrawPath(penSurface, pathSurface);

                if (this.FlatAppearance.BorderSize >= 1)
                {
                    pevent.Graphics.DrawPath(penBorder, pathBorder);
                }
            }
            else
            {
                if (this.FlatAppearance.BorderSize >= 1)
                {
                    using Pen penBorder = new Pen(_borderColor, 1f);
                    penBorder.Alignment = PenAlignment.Inset;
                    pevent.Graphics.DrawRectangle(penBorder, 0, 0, this.Width - 1, this.Height - 1);
                }
            }
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
    }
}
