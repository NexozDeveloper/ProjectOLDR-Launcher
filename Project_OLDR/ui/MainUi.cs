using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProjectOLDR.UI
{
    public static class IconRenderer
    {
        public static void DrawUser(Graphics g, Rectangle r, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush brush = new SolidBrush(color))
            {
                int headSize = r.Width / 2;
                g.FillEllipse(brush, r.X + (r.Width - headSize) / 2, r.Y, headSize, headSize);

                int bodyY = r.Y + headSize + 2;
                int bodyHeight = r.Height - headSize - 2;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(r.X, bodyY, r.Width, bodyHeight * 2, 180, 180);
                    path.CloseFigure();
                    g.FillPath(brush, path);
                }
            }
        }

        public static void DrawGamepad(Graphics g, Rectangle r, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 2f))
            using (SolidBrush brush = new SolidBrush(color))
            {
                Rectangle body = new Rectangle(r.X + 2, r.Y + 3, r.Width - 4, r.Height - 6);
                using (GraphicsPath path = MainUi.GetRoundedPath(body, 4))
                    g.DrawPath(pen, path);

                g.FillRectangle(brush, r.X + 6, r.Y + r.Height / 2 - 1, 6, 2);
                g.FillRectangle(brush, r.X + 8, r.Y + r.Height / 2 - 3, 2, 6);

                g.FillEllipse(brush, r.Right - 11, r.Y + r.Height / 2 - 2, 3, 3);
                g.FillEllipse(brush, r.Right - 7, r.Y + r.Height / 2 - 2, 3, 3);
            }
        }

        public static void DrawPlay(Graphics g, Rectangle r, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush brush = new SolidBrush(color))
            {
                Point[] pts = new Point[]
                {
                    new Point(r.X + 2, r.Y),
                    new Point(r.Right - 1, r.Y + r.Height / 2),
                    new Point(r.X + 2, r.Bottom)
                };
                g.FillPolygon(brush, pts);
            }
        }

        public static void DrawRefresh(Graphics g, Rectangle r, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 2f))
            {
                g.DrawArc(pen, r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4, 45, 270);
                Point[] arrow = new Point[]
                {
                    new Point(r.Right - 1, r.Y + 1),
                    new Point(r.Right - 5, r.Y + 6),
                    new Point(r.Right - 6, r.Y + 1)
                };
                using (SolidBrush b = new SolidBrush(color))
                    g.FillPolygon(b, arrow);
            }
        }
    }

    public static class MainUi
    {
        public static readonly Color BgColor = Color.FromArgb(10, 12, 16);
        public static readonly Color SurfaceColor = Color.FromArgb(17, 20, 28);
        public static readonly Color SurfaceHighlight = Color.FromArgb(26, 31, 44);
        public static readonly Color BorderColor = Color.FromArgb(38, 44, 62);

        public static readonly Color Accent = Color.FromArgb(0, 230, 153);
        public static readonly Color AccentHover = Color.FromArgb(0, 255, 170);
        public static readonly Color Danger = Color.FromArgb(255, 77, 77);
        public static readonly Color Warning = Color.FromArgb(255, 179, 25);

        public static readonly Color TextPrimary = Color.FromArgb(245, 247, 250);
        public static readonly Color TextSecondary = Color.FromArgb(130, 140, 160);
        public static readonly Color TextMuted = Color.FromArgb(85, 95, 115);

        public static readonly Font HeaderLarge = new Font("Segoe UI", 16, FontStyle.Bold);
        public static readonly Font HeaderMedium = new Font("Segoe UI", 11, FontStyle.Bold);
        public static readonly Font Regular = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font Small = new Font("Segoe UI", 8.5f, FontStyle.Regular);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public static void DragWindow(IntPtr handle)
        {
            ReleaseCapture();
            SendMessage(handle, 0xA1, 0x2, 0);
        }

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Panel CreateTitleBar(Form parent, string titleText)
        {
            Panel bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(13, 15, 20)
            };

            bar.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(BorderColor, 1f))
                    e.Graphics.DrawLine(borderPen, 0, bar.Height - 1, bar.Width, bar.Height - 1);

                IconRenderer.DrawGamepad(e.Graphics, new Rectangle(14, 13, 16, 16), Accent);
            };

            bar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) DragWindow(parent.Handle); };

            Label lbl = new Label
            {
                Text = titleText.ToUpper(),
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(38, 13),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            lbl.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) DragWindow(parent.Handle); };
            bar.Controls.Add(lbl);

            Button closeBtn = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(42, 42),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.MouseEnter += (s, e) => { closeBtn.BackColor = Danger; closeBtn.ForeColor = TextPrimary; };
            closeBtn.MouseLeave += (s, e) => { closeBtn.BackColor = Color.Transparent; closeBtn.ForeColor = TextSecondary; };
            closeBtn.Click += (s, e) => parent.Close();
            bar.Controls.Add(closeBtn);

            Button minBtn = new Button
            {
                Text = "—",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(42, 42),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            minBtn.FlatAppearance.BorderSize = 0;
            minBtn.MouseEnter += (s, e) => { minBtn.BackColor = SurfaceHighlight; minBtn.ForeColor = TextPrimary; };
            minBtn.MouseLeave += (s, e) => { minBtn.BackColor = Color.Transparent; minBtn.ForeColor = TextSecondary; };
            minBtn.Click += (s, e) => parent.WindowState = FormWindowState.Minimized;
            bar.Controls.Add(minBtn);

            return bar;
        }

        public static Panel CreateContainer(int x, int y, int width, int height)
        {
            Panel card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = SurfaceColor
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = GetRoundedPath(rect, 8))
                using (Pen pen = new Pen(BorderColor, 1.2f))
                {
                    card.Region = new Region(path);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            return card;
        }

        public static Button CreateStyledButton(string text, int x, int y, int width, int height, Color bg, Color hover)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = bg,
                ForeColor = TextPrimary,
                Font = HeaderMedium,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, btn.Width, btn.Height);
                using (GraphicsPath path = GetRoundedPath(rect, 6))
                {
                    btn.Region = new Region(path);
                }
            };

            btn.MouseEnter += (s, e) => btn.BackColor = hover;
            btn.MouseLeave += (s, e) => btn.BackColor = bg;

            return btn;
        }

        public static TextBox CreateStyledTextBox(int x, int y, int width, int height, bool password = false)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = SurfaceHighlight,
                ForeColor = TextPrimary,
                Font = Regular,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = password ? '●' : '\0'
            };
        }
    }
}
