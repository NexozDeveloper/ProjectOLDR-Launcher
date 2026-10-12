using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProjectOLDR.UI
{
    public enum LauncherTheme
    {
        DefaultDark,
        FullDark,
        FullWhite,
        Transparent,
        Rainbow,
        Red,
        Blue,
        Purple,
        Gold
    }

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
    }

    public static class MainUi
    {
        public static LauncherTheme CurrentTheme { get; private set; } = LauncherTheme.DefaultDark;

        public static Color BgColor { get; private set; } = Color.FromArgb(10, 12, 16);
        public static Color SurfaceColor { get; private set; } = Color.FromArgb(17, 20, 28);
        public static Color SurfaceHighlight { get; private set; } = Color.FromArgb(26, 31, 44);
        public static Color BorderColor { get; private set; } = Color.FromArgb(38, 44, 62);

        public static Color Accent { get; private set; } = Color.FromArgb(0, 230, 153);
        public static Color AccentHover { get; private set; } = Color.FromArgb(0, 255, 170);
        public static Color Danger { get; private set; } = Color.FromArgb(255, 77, 77);
        public static Color Warning { get; private set; } = Color.FromArgb(255, 179, 25);

        public static Color TextPrimary { get; private set; } = Color.FromArgb(245, 247, 250);
        public static Color TextSecondary { get; private set; } = Color.FromArgb(130, 140, 160);
        public static Color TextMuted { get; private set; } = Color.FromArgb(85, 95, 115);

        public static readonly Font HeaderLarge = new Font("Segoe UI", 16, FontStyle.Bold);
        public static readonly Font HeaderMedium = new Font("Segoe UI", 11, FontStyle.Bold);
        public static readonly Font Regular = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font Small = new Font("Segoe UI", 8.5f, FontStyle.Regular);

        public static event Action OnThemeChanged;
        private static Timer rainbowTimer;
        private static float rainbowHue = 0f;

        static MainUi()
        {
            rainbowTimer = new Timer { Interval = 40 };
            rainbowTimer.Tick += (s, e) =>
            {
                if (CurrentTheme == LauncherTheme.Rainbow)
                {
                    rainbowHue = (rainbowHue + 4f) % 360f;
                    Accent = ColorFromHsv(rainbowHue, 0.85, 1.0);
                    AccentHover = ColorFromHsv((rainbowHue + 20f) % 360f, 0.85, 1.0);
                    BorderColor = ColorFromHsv(rainbowHue, 0.5, 0.5);
                    OnThemeChanged?.Invoke();
                }
            };
        }

        public static void SetTheme(LauncherTheme theme, Form activeForm = null)
        {
            CurrentTheme = theme;

            if (theme != LauncherTheme.Rainbow)
                rainbowTimer.Stop();

            if (activeForm != null)
            {
                if (theme == LauncherTheme.Transparent)
                    activeForm.Opacity = 0.82;
                else
                    activeForm.Opacity = 1.0;
            }

            switch (theme)
            {
                case LauncherTheme.FullDark:
                    BgColor = Color.FromArgb(2, 2, 4);
                    SurfaceColor = Color.FromArgb(10, 10, 14);
                    SurfaceHighlight = Color.FromArgb(18, 18, 24);
                    BorderColor = Color.FromArgb(32, 32, 42);
                    Accent = Color.FromArgb(120, 120, 140);
                    AccentHover = Color.FromArgb(160, 160, 180);
                    TextPrimary = Color.FromArgb(240, 240, 245);
                    TextSecondary = Color.FromArgb(150, 150, 165);
                    TextMuted = Color.FromArgb(85, 85, 100);
                    break;

                case LauncherTheme.FullWhite:
                    BgColor = Color.FromArgb(242, 244, 248);
                    SurfaceColor = Color.FromArgb(255, 255, 255);
                    SurfaceHighlight = Color.FromArgb(232, 236, 242);
                    BorderColor = Color.FromArgb(205, 212, 224);
                    Accent = Color.FromArgb(0, 165, 110);
                    AccentHover = Color.FromArgb(0, 195, 130);
                    TextPrimary = Color.FromArgb(18, 22, 30);
                    TextSecondary = Color.FromArgb(80, 92, 110);
                    TextMuted = Color.FromArgb(135, 145, 160);
                    break;

                case LauncherTheme.Rainbow:
                    BgColor = Color.FromArgb(12, 12, 16);
                    SurfaceColor = Color.FromArgb(20, 20, 28);
                    SurfaceHighlight = Color.FromArgb(30, 30, 42);
                    TextPrimary = Color.FromArgb(250, 250, 250);
                    TextSecondary = Color.FromArgb(180, 180, 200);
                    TextMuted = Color.FromArgb(110, 110, 130);
                    rainbowTimer.Start();
                    break;

                case LauncherTheme.Red:
                    BgColor = Color.FromArgb(18, 10, 12);
                    SurfaceColor = Color.FromArgb(28, 15, 18);
                    SurfaceHighlight = Color.FromArgb(42, 22, 28);
                    BorderColor = Color.FromArgb(70, 30, 38);
                    Accent = Color.FromArgb(255, 55, 85);
                    AccentHover = Color.FromArgb(255, 85, 115);
                    TextPrimary = Color.FromArgb(250, 245, 245);
                    TextSecondary = Color.FromArgb(180, 140, 145);
                    TextMuted = Color.FromArgb(115, 85, 90);
                    break;

                case LauncherTheme.Blue:
                    BgColor = Color.FromArgb(10, 14, 22);
                    SurfaceColor = Color.FromArgb(16, 22, 34);
                    SurfaceHighlight = Color.FromArgb(24, 34, 52);
                    BorderColor = Color.FromArgb(38, 54, 82);
                    Accent = Color.FromArgb(0, 160, 255);
                    AccentHover = Color.FromArgb(40, 180, 255);
                    TextPrimary = Color.FromArgb(245, 248, 255);
                    TextSecondary = Color.FromArgb(140, 160, 190);
                    TextMuted = Color.FromArgb(90, 105, 130);
                    break;

                case LauncherTheme.Purple:
                    BgColor = Color.FromArgb(14, 10, 20);
                    SurfaceColor = Color.FromArgb(22, 16, 32);
                    SurfaceHighlight = Color.FromArgb(34, 24, 48);
                    BorderColor = Color.FromArgb(60, 40, 85);
                    Accent = Color.FromArgb(185, 75, 255);
                    AccentHover = Color.FromArgb(205, 105, 255);
                    TextPrimary = Color.FromArgb(250, 245, 255);
                    TextSecondary = Color.FromArgb(170, 150, 190);
                    TextMuted = Color.FromArgb(115, 95, 130);
                    break;

                case LauncherTheme.Gold:
                    BgColor = Color.FromArgb(16, 14, 10);
                    SurfaceColor = Color.FromArgb(26, 22, 16);
                    SurfaceHighlight = Color.FromArgb(40, 34, 24);
                    BorderColor = Color.FromArgb(70, 60, 40);
                    Accent = Color.FromArgb(255, 195, 0);
                    AccentHover = Color.FromArgb(255, 215, 50);
                    TextPrimary = Color.FromArgb(255, 250, 240);
                    TextSecondary = Color.FromArgb(190, 175, 150);
                    TextMuted = Color.FromArgb(125, 115, 95);
                    break;

                case LauncherTheme.Transparent:
                case LauncherTheme.DefaultDark:
                default:
                    BgColor = Color.FromArgb(10, 12, 16);
                    SurfaceColor = Color.FromArgb(17, 20, 28);
                    SurfaceHighlight = Color.FromArgb(26, 31, 44);
                    BorderColor = Color.FromArgb(38, 44, 62);
                    Accent = Color.FromArgb(0, 230, 153);
                    AccentHover = Color.FromArgb(0, 255, 170);
                    TextPrimary = Color.FromArgb(245, 247, 250);
                    TextSecondary = Color.FromArgb(130, 140, 160);
                    TextMuted = Color.FromArgb(85, 95, 115);
                    break;
            }

            OnThemeChanged?.Invoke();
        }

        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0) return Color.FromArgb(255, v, t, p);
            if (hi == 1) return Color.FromArgb(255, q, v, p);
            if (hi == 2) return Color.FromArgb(255, p, v, t);
            if (hi == 3) return Color.FromArgb(255, p, q, v);
            if (hi == 4) return Color.FromArgb(255, t, p, v);
            return Color.FromArgb(255, v, p, q);
        }

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
                bar.BackColor = SurfaceColor;
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
            closeBtn.MouseEnter += (s, e) => { closeBtn.BackColor = Danger; closeBtn.ForeColor = Color.White; };
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
                card.BackColor = SurfaceColor;
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
