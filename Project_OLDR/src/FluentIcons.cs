using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ProjectOLDR
{
    public enum FluentIcon
    {
        Gamepad,
        Play,
        Download,
        Plus,
        Delete,
        Settings,
        Person,
        People,
        Refresh,
        Search,
        Close,
        Minimize,
        Key,
        Palette,
        Folder,
        ArrowRight,
        Check,
        Info,
        Warning,
        Shield,
        Lock,
        Stop,
        ChevronDown,
        Library,
        SignOut,
        Edit,
        Link,
        Speed,
        View,
        Hide,
        Power
    }

    public static class FluentIcons
    {
        public static string GlyphPlay => string.Empty;
        public static string GlyphDownload => string.Empty;
        public static string GlyphAdd => string.Empty;
        public static string GlyphDelete => string.Empty;
        public static string GlyphSettings => string.Empty;
        public static string GlyphSearch => string.Empty;
        public static string GlyphClose => string.Empty;
        public static string GlyphRefresh => string.Empty;
        public static string GlyphPerson => string.Empty;

        public static string Glyph(FluentIcon icon)
        {
            return string.Empty;
        }

        public static int SnapSize(float requested)
        {
            int size = (int)Math.Round(requested);
            if (size < 10) return 10;
            if (size > 14) return 14;
            return size;
        }

        public static Font GlyphFont(float size)
        {
            return new Font("Segoe UI", SnapSize(size), FontStyle.Regular);
        }

        public static Size MeasureGlyph(FluentIcon icon, int fontSize)
        {
            return new Size(SnapSize(fontSize), SnapSize(fontSize));
        }

        public static void Draw(Graphics g, FluentIcon icon, Rectangle rect, Color color, float stroke = 1.15f)
        {
            if (rect.Width <= 2 || rect.Height <= 2) return;

            int size = Math.Min(14, Math.Min(rect.Width, rect.Height));
            Rectangle box = new Rectangle(
                rect.X + (rect.Width - size) / 2,
                rect.Y + (rect.Height - size) / 2,
                size,
                size);

            float scale = size / 16f;
            float offsetX = box.X;
            float offsetY = box.Y;

            PointF P(float x, float y)
            {
                return new PointF(offsetX + x * scale, offsetY + y * scale);
            }

            RectangleF R(float x, float y, float width, float height)
            {
                return new RectangleF(offsetX + x * scale, offsetY + y * scale, width * scale, height * scale);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, Math.Max(1f, Math.Min(1.15f, stroke))))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                switch (icon)
                {
                    case FluentIcon.Gamepad:
                        using (GraphicsPath gamepadPath = new GraphicsPath())
                        {
                            AddRoundedRect(gamepadPath, R(1.5f, 5f, 13f, 7f), 3f);
                            g.DrawPath(pen, gamepadPath);
                        }
                        g.DrawEllipse(pen, R(4.2f, 7.8f, 1.8f, 1.8f));
                        g.DrawEllipse(pen, R(10f, 7.8f, 1.8f, 1.8f));
                        g.DrawLine(pen, P(4.5f, 8.7f), P(5.7f, 8.7f));
                        g.DrawLine(pen, P(5.1f, 8.1f), P(5.1f, 9.3f));
                        break;

                    case FluentIcon.Play:
                        using (GraphicsPath playPath = new GraphicsPath())
                        {
                            playPath.AddPolygon(new PointF[] { P(5f, 3f), P(13f, 8f), P(5f, 13f) });
                            g.DrawPath(pen, playPath);
                        }
                        break;

                    case FluentIcon.Download:
                        g.DrawLine(pen, P(8f, 2f), P(8f, 10f));
                        g.DrawLine(pen, P(5f, 7.5f), P(8f, 10.5f));
                        g.DrawLine(pen, P(11f, 7.5f), P(8f, 10.5f));
                        g.DrawLine(pen, P(3f, 13f), P(13f, 13f));
                        break;

                    case FluentIcon.Plus:
                        g.DrawLine(pen, P(8f, 3f), P(8f, 13f));
                        g.DrawLine(pen, P(3f, 8f), P(13f, 8f));
                        break;

                    case FluentIcon.Delete:
                        g.DrawLine(pen, P(4f, 4f), P(12f, 4f));
                        g.DrawLine(pen, P(6f, 4f), P(6f, 2f));
                        g.DrawLine(pen, P(10f, 4f), P(10f, 2f));
                        using (GraphicsPath deletePath = new GraphicsPath())
                        {
                            AddRoundedRect(deletePath, R(4f, 5f, 8f, 9f), 1.5f);
                            g.DrawPath(pen, deletePath);
                        }
                        g.DrawLine(pen, P(7f, 7.5f), P(7f, 11f));
                        g.DrawLine(pen, P(9f, 7.5f), P(9f, 11f));
                        break;

                    case FluentIcon.Settings:
                    case FluentIcon.Palette:
                        g.DrawLine(pen, P(2f, 4f), P(14f, 4f));
                        g.DrawLine(pen, P(2f, 8f), P(14f, 8f));
                        g.DrawLine(pen, P(2f, 12f), P(14f, 12f));
                        g.DrawEllipse(pen, R(4.2f, 2.7f, 2.6f, 2.6f));
                        g.DrawEllipse(pen, R(9.2f, 6.7f, 2.6f, 2.6f));
                        g.DrawEllipse(pen, R(5.2f, 10.7f, 2.6f, 2.6f));
                        break;

                    case FluentIcon.Person:
                        g.DrawEllipse(pen, R(6f, 2.5f, 4f, 4f));
                        using (GraphicsPath personPath = new GraphicsPath())
                        {
                            personPath.AddArc(R(4f, 7f, 8f, 8f), 180f, 180f);
                            g.DrawPath(pen, personPath);
                        }
                        break;

                    case FluentIcon.People:
                        g.DrawEllipse(pen, R(5f, 2.5f, 3.5f, 3.5f));
                        using (GraphicsPath peoplePath = new GraphicsPath())
                        {
                            peoplePath.AddArc(R(3f, 7f, 7f, 7f), 180f, 180f);
                            g.DrawPath(pen, peoplePath);
                        }
                        g.DrawEllipse(pen, R(10f, 3f, 3f, 3f));
                        using (GraphicsPath peoplePath2 = new GraphicsPath())
                        {
                            peoplePath2.AddArc(R(9f, 7f, 6f, 6f), 180f, 180f);
                            g.DrawPath(pen, peoplePath2);
                        }
                        break;

                    case FluentIcon.Refresh:
                        using (GraphicsPath refreshPath = new GraphicsPath())
                        {
                            refreshPath.AddArc(R(2.5f, 2.5f, 11f, 11f), 300f, 280f);
                            g.DrawPath(pen, refreshPath);
                        }
                        g.DrawLine(pen, P(13f, 2.5f), P(13f, 6f));
                        g.DrawLine(pen, P(13f, 6f), P(9.5f, 6f));
                        break;

                    case FluentIcon.Search:
                        g.DrawEllipse(pen, R(3f, 3f, 8f, 8f));
                        g.DrawLine(pen, P(9.4f, 9.4f), P(14f, 14f));
                        break;

                    case FluentIcon.Close:
                        g.DrawLine(pen, P(4f, 4f), P(12f, 12f));
                        g.DrawLine(pen, P(12f, 4f), P(4f, 12f));
                        break;

                    case FluentIcon.Minimize:
                        g.DrawLine(pen, P(3f, 8f), P(13f, 8f));
                        break;

                    case FluentIcon.Key:
                        g.DrawEllipse(pen, R(3f, 3f, 5f, 5f));
                        g.DrawLine(pen, P(6.5f, 6.5f), P(14f, 8f));
                        g.DrawLine(pen, P(11f, 8f), P(11f, 10.5f));
                        g.DrawLine(pen, P(13f, 8.5f), P(13f, 11f));
                        break;

                    case FluentIcon.Folder:
                        using (GraphicsPath folderPath = new GraphicsPath())
                        {
                            folderPath.AddLine(P(2f, 4f), P(6f, 4f));
                            folderPath.AddLine(P(7f, 5.5f), P(14f, 5.5f));
                            folderPath.AddLine(P(14f, 5.5f), P(14f, 12f));
                            folderPath.AddLine(P(2f, 12f), P(2f, 4f));
                            g.DrawPath(pen, folderPath);
                        }
                        break;

                    case FluentIcon.ArrowRight:
                        g.DrawLine(pen, P(2f, 8f), P(14f, 8f));
                        g.DrawLine(pen, P(10f, 4f), P(14f, 8f));
                        g.DrawLine(pen, P(10f, 12f), P(14f, 8f));
                        break;

                    case FluentIcon.Check:
                        g.DrawLine(pen, P(3f, 8f), P(7f, 12f));
                        g.DrawLine(pen, P(7f, 12f), P(13f, 4f));
                        break;

                    case FluentIcon.Info:
                        g.DrawEllipse(pen, R(2f, 2f, 12f, 12f));
                        g.DrawLine(pen, P(8f, 7f), P(8f, 11f));
                        g.DrawEllipse(pen, R(7.3f, 4.3f, 1.4f, 1.4f));
                        break;

                    case FluentIcon.Warning:
                        using (GraphicsPath warningPath = new GraphicsPath())
                        {
                            warningPath.AddPolygon(new PointF[] { P(8f, 2f), P(15f, 14f), P(1f, 14f) });
                            g.DrawPath(pen, warningPath);
                        }
                        g.DrawLine(pen, P(8f, 6f), P(8f, 10f));
                        g.DrawEllipse(pen, R(7.3f, 11.6f, 1.4f, 1.4f));
                        break;

                    case FluentIcon.Shield:
                        using (GraphicsPath shieldPath = new GraphicsPath())
                        {
                            shieldPath.AddPolygon(new PointF[]
                            {
                                P(8f, 2f), P(13f, 4f), P(13f, 8f),
                                P(8f, 14f), P(3f, 8f), P(3f, 4f)
                            });
                            g.DrawPath(pen, shieldPath);
                        }
                        break;

                    case FluentIcon.Lock:
                        using (GraphicsPath lockPath = new GraphicsPath())
                        {
                            AddRoundedRect(lockPath, R(3.5f, 7f, 9f, 7f), 1.5f);
                            g.DrawPath(pen, lockPath);
                        }
                        using (GraphicsPath shacklePath = new GraphicsPath())
                        {
                            shacklePath.AddArc(R(5f, 3f, 6f, 6f), 180f, 180f);
                            g.DrawPath(pen, shacklePath);
                        }
                        g.DrawEllipse(pen, R(7.3f, 9.6f, 1.4f, 1.4f));
                        break;

                    case FluentIcon.Stop:
                        using (GraphicsPath stopPath = new GraphicsPath())
                        {
                            AddRoundedRect(stopPath, R(4f, 4f, 8f, 8f), 1.2f);
                            g.DrawPath(pen, stopPath);
                        }
                        break;

                    case FluentIcon.ChevronDown:
                        g.DrawLine(pen, P(4f, 6f), P(8f, 10f));
                        g.DrawLine(pen, P(12f, 6f), P(8f, 10f));
                        break;

                    case FluentIcon.Library:
                        using (GraphicsPath libraryPath = new GraphicsPath())
                        {
                            AddRoundedRect(libraryPath, R(2f, 2f, 11f, 4f), 1f);
                            AddRoundedRect(libraryPath, R(2f, 6f, 11f, 4f), 1f);
                            AddRoundedRect(libraryPath, R(2f, 10f, 11f, 4f), 1f);
                            g.DrawPath(pen, libraryPath);
                        }
                        break;

                    case FluentIcon.SignOut:
                        using (GraphicsPath signOutPath = new GraphicsPath())
                        {
                            AddRoundedRect(signOutPath, R(2f, 2f, 8f, 12f), 1.5f);
                            g.DrawPath(pen, signOutPath);
                        }
                        g.DrawLine(pen, P(9f, 8f), P(15f, 8f));
                        g.DrawLine(pen, P(12f, 5f), P(15f, 8f));
                        g.DrawLine(pen, P(12f, 11f), P(15f, 8f));
                        break;

                    case FluentIcon.Edit:
                        using (GraphicsPath editPath = new GraphicsPath())
                        {
                            editPath.AddPolygon(new PointF[]
                            {
                                P(3f, 13f), P(4f, 10f), P(11.5f, 2.5f),
                                P(13.5f, 4.5f), P(6f, 12f), P(3f, 13f)
                            });
                            g.DrawPath(pen, editPath);
                        }
                        g.DrawLine(pen, P(10.5f, 3.5f), P(12.5f, 5.5f));
                        break;

                    case FluentIcon.Link:
                        g.DrawArc(pen, R(2f, 5f, 8f, 6f), 150f, 250f);
                        g.DrawArc(pen, R(6f, 5f, 8f, 6f), 330f, 250f);
                        g.DrawLine(pen, P(6f, 8f), P(10f, 8f));
                        break;

                    case FluentIcon.Speed:
                        using (GraphicsPath speedPath = new GraphicsPath())
                        {
                            speedPath.AddArc(R(2f, 3f, 12f, 11f), 200f, 140f);
                            g.DrawPath(pen, speedPath);
                        }
                        g.DrawLine(pen, P(8f, 8f), P(11f, 5f));
                        g.DrawEllipse(pen, R(7.2f, 7.2f, 1.6f, 1.6f));
                        break;

                    case FluentIcon.View:
                        using (GraphicsPath viewPath = new GraphicsPath())
                        {
                            viewPath.AddPolygon(new PointF[]
                            {
                                P(1f, 8f), P(4f, 4f), P(8f, 3f),
                                P(12f, 4f), P(15f, 8f), P(12f, 12f),
                                P(8f, 13f), P(4f, 12f), P(1f, 8f)
                            });
                            g.DrawPath(pen, viewPath);
                        }
                        g.DrawEllipse(pen, R(6.5f, 5.5f, 3f, 3f));
                        break;

                    case FluentIcon.Hide:
                        using (GraphicsPath hidePath = new GraphicsPath())
                        {
                            hidePath.AddPolygon(new PointF[]
                            {
                                P(1f, 8f), P(4f, 4f), P(8f, 3f),
                                P(12f, 4f), P(15f, 8f), P(12f, 12f),
                                P(8f, 13f), P(4f, 12f), P(1f, 8f)
                            });
                            g.DrawPath(pen, hidePath);
                        }
                        g.DrawLine(pen, P(2f, 14f), P(14f, 2f));
                        break;

                    case FluentIcon.Power:
                        using (GraphicsPath powerPath = new GraphicsPath())
                        {
                            powerPath.AddArc(R(3f, 3f, 10f, 10f), 210f, 300f);
                            g.DrawPath(pen, powerPath);
                        }
                        g.DrawLine(pen, P(8f, 2f), P(8f, 8f));
                        break;

                    default:
                        g.DrawEllipse(pen, R(3f, 3f, 10f, 10f));
                        break;
                }
            }
        }

        public static void DrawGlyph(Graphics g, FluentIcon icon, Rectangle rect, Color color, float fontSize)
        {
            Draw(g, icon, rect, color, 1.15f);
        }

        private static void AddRoundedRect(GraphicsPath path, RectangleF rect, float radius)
        {
            float diameter = Math.Min(rect.Width, rect.Height);
            if (radius * 2 > diameter) radius = diameter / 2f;
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180f, 90f);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270f, 90f);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90f, 90f);
            path.CloseFigure();
        }
    }

    public sealed class IconBox : Panel
    {
        private FluentIcon icon = FluentIcon.Gamepad;
        private Color iconColor = Color.White;
        private int glyphSize;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FluentIcon Icon
        {
            get { return icon; }
            set { icon = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color IconColor
        {
            get { return iconColor; }
            set { iconColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int GlyphSize
        {
            get { return glyphSize; }
            set { glyphSize = FluentIcons.SnapSize(value); Invalidate(); }
        }

        public IconBox(FluentIcon icon, int size, Color color)
        {
            this.icon = icon;
            iconColor = color;
            glyphSize = FluentIcons.SnapSize(size);
            Size = new Size(size, size);
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Paint += IconBox_Paint;
        }

        private void IconBox_Paint(object sender, PaintEventArgs e)
        {
            FluentIcons.Draw(e.Graphics, icon, new Rectangle(0, 0, Width, Height), iconColor, 1.15f);
        }
    }
}
