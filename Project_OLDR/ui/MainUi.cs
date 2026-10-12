using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
        Emerald,
        OceanBlue,
        Cyan,
        CrimsonRed,
        CyberPink,
        ElectricViolet,
        SunsetOrange,
        SolarGold,
        Mint,
        Custom
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

        public static void DrawPaintBrush(Graphics g, Rectangle r, Color tipColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Matrix m = new Matrix())
            {
                PointF center = new PointF(r.X + r.Width / 2f, r.Y + r.Height / 2f);
                m.RotateAt(-45, center);
                g.Transform = m;

                int cx = (int)center.X;
                int cy = (int)center.Y;

                using (SolidBrush handleBrush = new SolidBrush(Color.FromArgb(175, 140, 105)))
                {
                    Point[] handle = new Point[]
                    {
                        new Point(cx - 3, cy + 2),
                        new Point(cx + 3, cy + 2),
                        new Point(cx + 1, cy + 16),
                        new Point(cx - 1, cy + 16)
                    };
                    g.FillPolygon(handleBrush, handle);
                }

                using (SolidBrush ferruleBrush = new SolidBrush(Color.FromArgb(200, 205, 215)))
                {
                    g.FillRectangle(ferruleBrush, cx - 4, cy - 4, 8, 6);
                }

                using (SolidBrush bristleBrush = new SolidBrush(tipColor))
                {
                    Point[] bristles = new Point[]
                    {
                        new Point(cx - 4, cy - 4),
                        new Point(cx + 4, cy - 4),
                        new Point(cx + 2, cy - 13),
                        new Point(cx, cy - 15),
                        new Point(cx - 2, cy - 13)
                    };
                    g.FillPolygon(bristleBrush, bristles);
                }

                g.ResetTransform();
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

        public static Color CustomColorValue { get; private set; } = Color.FromArgb(0, 230, 153);

        public static readonly Font HeaderLarge = new Font("Segoe UI", 16, FontStyle.Bold);
        public static readonly Font HeaderMedium = new Font("Segoe UI", 11, FontStyle.Bold);
        public static readonly Font Regular = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font Small = new Font("Segoe UI", 8.5f, FontStyle.Regular);

        public static event Action OnThemeChanged;
        public static event Action OnRainbowTick;

        private static Timer rainbowTimer;
        private static float rainbowHue = 0f;

        private static string ThemeSavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectOLDR", "theme.dat");

        static MainUi()
        {
            rainbowTimer = new Timer { Interval = 60 };
            rainbowTimer.Tick += (s, e) =>
            {
                if (CurrentTheme == LauncherTheme.Rainbow)
                {
                    rainbowHue = (rainbowHue + 1.5f) % 360f;
                    Accent = ColorFromHsv(rainbowHue, 0.85, 1.0);
                    AccentHover = ColorFromHsv((rainbowHue + 12f) % 360f, 0.85, 1.0);
                    BorderColor = ColorFromHsv(rainbowHue, 0.45, 0.45);
                    OnRainbowTick?.Invoke();
                }
            };

            LoadSavedTheme();
        }

        public static void SetTheme(LauncherTheme theme, Form activeForm = null, Color? customColor = null)
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

            if (customColor.HasValue)
                CustomColorValue = customColor.Value;

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

                case LauncherTheme.Emerald:
                    BgColor = Color.FromArgb(8, 16, 12);
                    SurfaceColor = Color.FromArgb(14, 26, 20);
                    SurfaceHighlight = Color.FromArgb(20, 38, 28);
                    BorderColor = Color.FromArgb(32, 60, 44);
                    Accent = Color.FromArgb(16, 212, 128);
                    AccentHover = Color.FromArgb(40, 235, 150);
                    TextPrimary = Color.FromArgb(245, 252, 248);
                    TextSecondary = Color.FromArgb(135, 165, 150);
                    TextMuted = Color.FromArgb(85, 110, 98);
                    break;

                case LauncherTheme.OceanBlue:
                    BgColor = Color.FromArgb(8, 14, 24);
                    SurfaceColor = Color.FromArgb(14, 22, 36);
                    SurfaceHighlight = Color.FromArgb(20, 32, 52);
                    BorderColor = Color.FromArgb(34, 52, 82);
                    Accent = Color.FromArgb(30, 144, 255);
                    AccentHover = Color.FromArgb(65, 170, 255);
                    TextPrimary = Color.FromArgb(245, 248, 255);
                    TextSecondary = Color.FromArgb(135, 155, 185);
                    TextMuted = Color.FromArgb(85, 105, 130);
                    break;

                case LauncherTheme.Cyan:
                    BgColor = Color.FromArgb(8, 18, 20);
                    SurfaceColor = Color.FromArgb(12, 28, 32);
                    SurfaceHighlight = Color.FromArgb(18, 42, 48);
                    BorderColor = Color.FromArgb(30, 68, 76);
                    Accent = Color.FromArgb(0, 225, 235);
                    AccentHover = Color.FromArgb(40, 245, 255);
                    TextPrimary = Color.FromArgb(245, 254, 255);
                    TextSecondary = Color.FromArgb(130, 165, 175);
                    TextMuted = Color.FromArgb(80, 115, 125);
                    break;

                case LauncherTheme.CrimsonRed:
                    BgColor = Color.FromArgb(18, 8, 10);
                    SurfaceColor = Color.FromArgb(28, 14, 18);
                    SurfaceHighlight = Color.FromArgb(42, 20, 26);
                    BorderColor = Color.FromArgb(68, 30, 38);
                    Accent = Color.FromArgb(255, 55, 75);
                    AccentHover = Color.FromArgb(255, 90, 105);
                    TextPrimary = Color.FromArgb(255, 245, 246);
                    TextSecondary = Color.FromArgb(180, 140, 145);
                    TextMuted = Color.FromArgb(120, 85, 90);
                    break;

                case LauncherTheme.CyberPink:
                    BgColor = Color.FromArgb(18, 8, 16);
                    SurfaceColor = Color.FromArgb(28, 14, 24);
                    SurfaceHighlight = Color.FromArgb(42, 20, 36);
                    BorderColor = Color.FromArgb(68, 30, 58);
                    Accent = Color.FromArgb(255, 60, 170);
                    AccentHover = Color.FromArgb(255, 100, 195);
                    TextPrimary = Color.FromArgb(255, 245, 252);
                    TextSecondary = Color.FromArgb(180, 140, 170);
                    TextMuted = Color.FromArgb(120, 85, 110);
                    break;

                case LauncherTheme.ElectricViolet:
                    BgColor = Color.FromArgb(14, 8, 22);
                    SurfaceColor = Color.FromArgb(22, 14, 34);
                    SurfaceHighlight = Color.FromArgb(34, 22, 52);
                    BorderColor = Color.FromArgb(56, 34, 84);
                    Accent = Color.FromArgb(170, 75, 255);
                    AccentHover = Color.FromArgb(195, 115, 255);
                    TextPrimary = Color.FromArgb(250, 245, 255);
                    TextSecondary = Color.FromArgb(170, 145, 190);
                    TextMuted = Color.FromArgb(115, 90, 135);
                    break;

                case LauncherTheme.SunsetOrange:
                    BgColor = Color.FromArgb(20, 10, 6);
                    SurfaceColor = Color.FromArgb(30, 16, 10);
                    SurfaceHighlight = Color.FromArgb(46, 26, 16);
                    BorderColor = Color.FromArgb(74, 42, 24);
                    Accent = Color.FromArgb(255, 115, 30);
                    AccentHover = Color.FromArgb(255, 145, 65);
                    TextPrimary = Color.FromArgb(255, 248, 242);
                    TextSecondary = Color.FromArgb(185, 150, 130);
                    TextMuted = Color.FromArgb(125, 95, 80);
                    break;

                case LauncherTheme.SolarGold:
                    BgColor = Color.FromArgb(18, 14, 6);
                    SurfaceColor = Color.FromArgb(28, 22, 10);
                    SurfaceHighlight = Color.FromArgb(44, 34, 16);
                    BorderColor = Color.FromArgb(72, 56, 26);
                    Accent = Color.FromArgb(255, 195, 0);
                    AccentHover = Color.FromArgb(255, 215, 50);
                    TextPrimary = Color.FromArgb(255, 252, 240);
                    TextSecondary = Color.FromArgb(185, 170, 135);
                    TextMuted = Color.FromArgb(120, 110, 85);
                    break;

                case LauncherTheme.Mint:
                    BgColor = Color.FromArgb(8, 18, 16);
                    SurfaceColor = Color.FromArgb(12, 28, 24);
                    SurfaceHighlight = Color.FromArgb(18, 42, 36);
                    BorderColor = Color.FromArgb(28, 66, 56);
                    Accent = Color.FromArgb(45, 230, 180);
                    AccentHover = Color.FromArgb(85, 245, 205);
                    TextPrimary = Color.FromArgb(245, 255, 252);
                    TextSecondary = Color.FromArgb(135, 175, 165);
                    TextMuted = Color.FromArgb(85, 120, 110);
                    break;

                case LauncherTheme.Custom:
                    int r = CustomColorValue.R;
                    int g = CustomColorValue.G;
                    int b = CustomColorValue.B;
                    BgColor = Color.FromArgb(Math.Max(6, r / 16), Math.Max(6, g / 16), Math.Max(8, b / 16));
                    SurfaceColor = Color.FromArgb(Math.Max(12, r / 10), Math.Max(12, g / 10), Math.Max(16, b / 10));
                    SurfaceHighlight = Color.FromArgb(Math.Max(20, r / 7), Math.Max(20, g / 7), Math.Max(26, b / 7));
                    BorderColor = Color.FromArgb(Math.Max(35, r / 4), Math.Max(35, g / 4), Math.Max(45, b / 4));
                    Accent = CustomColorValue;
                    AccentHover = ControlPaint.Light(CustomColorValue, 0.25f);
                    TextPrimary = Color.FromArgb(245, 247, 250);
                    TextSecondary = Color.FromArgb(160, 165, 180);
                    TextMuted = Color.FromArgb(100, 105, 120);
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

            SaveCurrentTheme();
            OnThemeChanged?.Invoke();
        }

        private static void SaveCurrentTheme()
        {
            try
            {
                string dir = Path.GetDirectoryName(ThemeSavePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string data = $"{CurrentTheme};{CustomColorValue.ToArgb()}";
                File.WriteAllText(ThemeSavePath, data);
            }
            catch
            {
            }
        }

        private static void LoadSavedTheme()
        {
            try
            {
                if (File.Exists(ThemeSavePath))
                {
                    string content = File.ReadAllText(ThemeSavePath).Trim();
                    string[] parts = content.Split(';');
                    if (parts.Length >= 2 && Enum.TryParse(parts[0], out LauncherTheme theme) && int.TryParse(parts[1], out int argb))
                    {
                        CustomColorValue = Color.FromArgb(argb);
                        SetTheme(theme, null, CustomColorValue);
                    }
                }
            }
            catch
            {
            }
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
                BackColor = SurfaceColor
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
