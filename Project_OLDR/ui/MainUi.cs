using System;
using System.Collections.Generic;
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

    public enum ButtonKind
    {
        Primary,
        Secondary,
        Danger,
        Subtle
    }

    public static class IconRenderer
    {
        public static void DrawUser(Graphics g, Rectangle r, Color color)
        {
            FluentIcons.Draw(g, FluentIcon.Person, r, color, 1.15f);
        }

        public static void DrawGamepad(Graphics g, Rectangle r, Color color)
        {
            FluentIcons.Draw(g, FluentIcon.Gamepad, r, color, 1.15f);
        }

        public static void DrawPaintBrush(Graphics g, Rectangle r, Color tipColor)
        {
            FluentIcons.Draw(g, FluentIcon.Palette, r, tipColor, 1.15f);
        }
    }

    public static class MainUi
    {
        private sealed class ButtonRole
        {
            public ButtonKind Kind;
            public FluentIcon? Icon;
        }

        public const int IconSize = 14;

        public static LauncherTheme CurrentTheme { get; private set; } = LauncherTheme.DefaultDark;

        public static Color BgColor { get; private set; } = Color.FromArgb(10, 10, 10);
        public static Color SurfaceColor { get; private set; } = Color.FromArgb(20, 20, 20);
        public static Color SurfaceHighlight { get; private set; } = Color.FromArgb(28, 28, 28);
        public static Color BorderColor { get; private set; } = Color.FromArgb(42, 42, 42);

        public static Color Accent { get; private set; } = Color.FromArgb(255, 255, 255);
        public static Color AccentHover { get; private set; } = Color.FromArgb(230, 230, 230);
        public static Color Danger { get; private set; } = Color.FromArgb(154, 154, 154);
        public static Color Warning { get; private set; } = Color.FromArgb(176, 176, 176);
        public static readonly Color Success = Color.FromArgb(74, 222, 128);

        public static Color TextPrimary { get; private set; } = Color.FromArgb(255, 255, 255);
        public static Color TextSecondary { get; private set; } = Color.FromArgb(184, 184, 184);
        public static Color TextMuted { get; private set; } = Color.FromArgb(124, 124, 124);

        public static Color CustomColorValue { get; private set; } = Color.FromArgb(255, 255, 255);

        public static readonly Font Display = new Font("Segoe UI Variable Display", 28, FontStyle.Bold);
        public static readonly Font HeaderLarge = new Font("Segoe UI Variable Display", 22, FontStyle.Bold);
        public static readonly Font HeaderMedium = new Font("Segoe UI Variable Text", 13, FontStyle.Bold);
        public static readonly Font BodyStrong = new Font("Segoe UI Variable Text", 12, FontStyle.Bold);
        public static readonly Font Caption = new Font("Segoe UI Variable Text", 12, FontStyle.Regular);
        public static readonly Font Regular = new Font("Segoe UI Variable Text", 11, FontStyle.Regular);
        public static readonly Font Small = new Font("Segoe UI Variable Text", 10, FontStyle.Regular);

        public static event Action OnThemeChanged;
        public static event Action OnRainbowTick;

        private static readonly Timer rainbowTimer;
        private static readonly Dictionary<Button, ButtonRole> buttonRoles = new Dictionary<Button, ButtonRole>();

        private static string ThemeSavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectOLDR", "theme.dat");

        static MainUi()
        {
            rainbowTimer = new Timer { Interval = 120 };
            rainbowTimer.Tick += (s, e) =>
            {
                if (CurrentTheme != LauncherTheme.Rainbow) return;
                OnRainbowTick?.Invoke();
            };
            LoadSavedTheme();
        }

        public static void SetupForm(Form form, string title, Size size)
        {
            form.Text = title;
            form.Size = size;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.None;
            form.BackColor = BgColor;
            form.KeyPreview = true;
            form.ShowInTaskbar = true;

            form.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, form.Width - 1, form.Height - 1);
                using (GraphicsPath path = GetRoundedPath(rect, 12))
                using (SolidBrush brush = new SolidBrush(BgColor))
                {
                    form.Region = new Region(path);
                    e.Graphics.FillPath(brush, path);
                    using (Pen pen = new Pen(BorderColor, 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            form.Resize += (s, e) =>
            {
                if (form.Width < 32 || form.Height < 32) return;
                form.Region = new Region(GetRoundedPath(new Rectangle(0, 0, form.Width - 1, form.Height - 1), 12));
                form.Invalidate();
            };

            SetDropShadow(form);

            Action listener = () =>
            {
                form.BackColor = BgColor;
                form.Invalidate(true);
            };
            OnThemeChanged += listener;
            form.FormClosed += (s, e) => OnThemeChanged -= listener;
        }

        public static void SetTheme(LauncherTheme theme, Form activeForm = null, Color? customColor = null)
        {
            CurrentTheme = theme;

            if (theme != LauncherTheme.Rainbow) rainbowTimer.Stop();
            else rainbowTimer.Start();

            if (activeForm != null)
            {
                activeForm.Opacity = theme == LauncherTheme.Transparent ? 0.94 : 1.0;
            }

            if (customColor.HasValue)
            {
                Color source = customColor.Value;
                int gray = (source.R * 299 + source.G * 587 + source.B * 114) / 1000;
                CustomColorValue = Color.FromArgb(gray, gray, gray);
            }

            switch (theme)
            {
                case LauncherTheme.FullDark:
                    ApplyPalette(Color.FromArgb(5, 5, 5), Color.FromArgb(13, 13, 13), Color.FromArgb(21, 21, 21));
                    break;

                case LauncherTheme.FullWhite:
                    BgColor = Color.FromArgb(245, 245, 245);
                    SurfaceColor = Color.FromArgb(255, 255, 255);
                    SurfaceHighlight = Color.FromArgb(236, 236, 236);
                    BorderColor = Color.FromArgb(212, 212, 212);
                    Accent = Color.FromArgb(10, 10, 10);
                    AccentHover = Color.FromArgb(51, 51, 51);
                    Danger = Color.FromArgb(85, 85, 85);
                    Warning = Color.FromArgb(102, 102, 102);
                    TextPrimary = Color.FromArgb(10, 10, 10);
                    TextSecondary = Color.FromArgb(51, 51, 51);
                    TextMuted = Color.FromArgb(102, 102, 102);
                    break;

                case LauncherTheme.Custom:
                    int level = CustomColorValue.R;
                    ApplyPalette(
                        Color.FromArgb(Math.Max(6, level / 12), Math.Max(6, level / 12), Math.Max(6, level / 12)),
                        Color.FromArgb(Math.Max(12, level / 8), Math.Max(12, level / 8), Math.Max(12, level / 8)),
                        Color.FromArgb(Math.Max(18, level / 5), Math.Max(18, level / 5), Math.Max(18, level / 5)));
                    break;

                case LauncherTheme.Emerald:
                    ApplyPalette(Color.FromArgb(9, 9, 9), Color.FromArgb(17, 17, 17), Color.FromArgb(25, 25, 25));
                    break;

                case LauncherTheme.OceanBlue:
                    ApplyPalette(Color.FromArgb(8, 8, 8), Color.FromArgb(15, 15, 15), Color.FromArgb(23, 23, 23));
                    break;

                case LauncherTheme.Cyan:
                    ApplyPalette(Color.FromArgb(9, 9, 9), Color.FromArgb(16, 16, 16), Color.FromArgb(24, 24, 24));
                    break;

                case LauncherTheme.CrimsonRed:
                    ApplyPalette(Color.FromArgb(11, 11, 11), Color.FromArgb(19, 19, 19), Color.FromArgb(27, 27, 27));
                    break;

                case LauncherTheme.CyberPink:
                    ApplyPalette(Color.FromArgb(10, 10, 10), Color.FromArgb(18, 18, 18), Color.FromArgb(26, 26, 26));
                    break;

                case LauncherTheme.ElectricViolet:
                    ApplyPalette(Color.FromArgb(9, 9, 9), Color.FromArgb(16, 16, 16), Color.FromArgb(24, 24, 24));
                    break;

                case LauncherTheme.SunsetOrange:
                    ApplyPalette(Color.FromArgb(11, 11, 11), Color.FromArgb(19, 19, 19), Color.FromArgb(27, 27, 27));
                    break;

                case LauncherTheme.SolarGold:
                    ApplyPalette(Color.FromArgb(10, 10, 10), Color.FromArgb(18, 18, 18), Color.FromArgb(26, 26, 26));
                    break;

                case LauncherTheme.Mint:
                    ApplyPalette(Color.FromArgb(8, 8, 8), Color.FromArgb(16, 16, 16), Color.FromArgb(24, 24, 24));
                    break;

                case LauncherTheme.Transparent:
                case LauncherTheme.Rainbow:
                case LauncherTheme.DefaultDark:
                default:
                    ApplyPalette(Color.FromArgb(10, 10, 10), Color.FromArgb(20, 20, 20), Color.FromArgb(28, 28, 28));
                    break;
            }

            SaveCurrentTheme();
            RefreshThemedButtons();
            OnThemeChanged?.Invoke();
        }

        private static void ApplyPalette(Color bg, Color surface, Color highlight)
        {
            BgColor = bg;
            SurfaceColor = surface;
            SurfaceHighlight = highlight;
            BorderColor = blendColor(highlight, Color.White, 0.10f);
            Accent = Color.FromArgb(255, 255, 255);
            AccentHover = Color.FromArgb(230, 230, 230);
            Danger = Color.FromArgb(154, 154, 154);
            Warning = Color.FromArgb(176, 176, 176);
            TextPrimary = Color.FromArgb(255, 255, 255);
            TextSecondary = Color.FromArgb(184, 184, 184);
            TextMuted = Color.FromArgb(124, 124, 124);
        }

        private static void RefreshThemedButtons()
        {
            foreach (KeyValuePair<Button, ButtonRole> pair in buttonRoles)
            {
                Button btn = pair.Key;
                if (btn == null || btn.IsDisposed) continue;
                Color baseColor = RoleColor(pair.Value.Kind);
                btn.BackColor = btn.Enabled ? baseColor : blendColor(baseColor, BgColor, 0.52f);
                btn.ForeColor = pair.Value.Kind == ButtonKind.Primary ? Color.FromArgb(10, 10, 10) : TextPrimary;
                btn.Invalidate();
            }
        }

        private static Color RoleColor(ButtonKind kind)
        {
            switch (kind)
            {
                case ButtonKind.Primary: return Accent;
                case ButtonKind.Danger: return Color.FromArgb(26, 26, 26);
                case ButtonKind.Subtle: return Color.Transparent;
                default: return SurfaceHighlight;
            }
        }

        private static void SaveCurrentTheme()
        {
            try
            {
                string dir = Path.GetDirectoryName(ThemeSavePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(ThemeSavePath, CurrentTheme + ";" + CustomColorValue.ToArgb());
            }
            catch
            {
            }
        }

        private static void LoadSavedTheme()
        {
            try
            {
                if (!File.Exists(ThemeSavePath)) return;
                string[] parts = File.ReadAllText(ThemeSavePath).Trim().Split(';');
                if (parts.Length >= 2 && Enum.TryParse(parts[0], out LauncherTheme theme) && int.TryParse(parts[1], out int argb))
                {
                    Color source = Color.FromArgb(argb);
                    int gray = (source.R * 299 + source.G * 587 + source.B * 114) / 1000;
                    CustomColorValue = Color.FromArgb(gray, gray, gray);
                    SetTheme(theme, null, CustomColorValue);
                }
            }
            catch
            {
            }
        }

        public static Color blendColor(Color from, Color to, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            int r = (int)(from.R + (to.R - from.R) * amount);
            int g = (int)(from.G + (to.G - from.G) * amount);
            int b = (int)(from.B + (to.B - from.B) * amount);
            return Color.FromArgb(r, g, b);
        }

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private const int GclStyle = -26;
        private const int CsDropShadow = 0x00020000;

        public static void SetDropShadow(Form form)
        {
            try
            {
                IntPtr handle = form.Handle;
                IntPtr style = GetWindowLongPtr(handle, GclStyle);
                long value = style.ToInt64() | CsDropShadow;
                SetWindowLongPtr(handle, GclStyle, new IntPtr(value));
            }
            catch
            {
            }
        }

        public static void DragWindow(IntPtr handle)
        {
            ReleaseCapture();
            SendMessage(handle, 0xA1, 0x2, 0);
        }

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(1, radius * 2);
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;
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
                Height = 44,
                BackColor = SurfaceColor
            };

            bar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush bg = new SolidBrush(SurfaceColor))
                {
                    e.Graphics.FillRectangle(bg, 0, 0, bar.Width, bar.Height);
                }
                using (Pen border = new Pen(BorderColor, 1f))
                {
                    e.Graphics.DrawLine(border, 0, bar.Height - 1, bar.Width, bar.Height - 1);
                }
                FluentIcons.Draw(e.Graphics, FluentIcon.Gamepad,
                    new Rectangle(16, (bar.Height - IconSize) / 2, IconSize, IconSize), Accent, 1.15f);
            };

            bar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) DragWindow(parent.Handle); };

            Label lbl = new Label
            {
                Text = titleText,
                Font = BodyStrong,
                ForeColor = TextSecondary,
                Location = new Point(42, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            lbl.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) DragWindow(parent.Handle); };
            bar.Controls.Add(lbl);

            bar.Controls.Add(CreateWindowButton(parent, FluentIcon.Close, true));
            bar.Controls.Add(CreateWindowButton(parent, FluentIcon.Minimize, false));

            Action listener = () => bar.Invalidate();
            OnThemeChanged += listener;
            bar.Disposed += (s, e) => OnThemeChanged -= listener;

            return bar;
        }

        private static Button CreateWindowButton(Form parent, FluentIcon icon, bool isClose)
        {
            Button btn = new Button
            {
                Dock = DockStyle.Right,
                Width = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextMuted,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (btn.BackColor != Color.Transparent)
                {
                    using (SolidBrush bg = new SolidBrush(btn.BackColor))
                    {
                        e.Graphics.FillRectangle(bg, 0, 0, btn.Width, btn.Height);
                    }
                }
                FluentIcons.Draw(e.Graphics, icon,
                    new Rectangle((btn.Width - IconSize) / 2, (btn.Height - IconSize) / 2, IconSize, IconSize),
                    btn.ForeColor, 1.15f);
            };

            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = isClose ? Color.FromArgb(48, 48, 48) : SurfaceHighlight;
                btn.ForeColor = Color.White;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = TextMuted;
            };
            btn.Click += (s, e) =>
            {
                if (isClose) parent.Close();
                else parent.WindowState = FormWindowState.Minimized;
            };

            return btn;
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
                using (GraphicsPath path = GetRoundedPath(rect, 10))
                {
                    card.Region = new Region(path);
                    using (SolidBrush brush = new SolidBrush(SurfaceColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen pen = new Pen(BorderColor, 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            card.Resize += (s, e) =>
            {
                if (card.Width < 8 || card.Height < 8) return;
                card.Region = new Region(GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10));
            };

            Action listener = () => card.Invalidate();
            OnThemeChanged += listener;
            card.Disposed += (s, e) => OnThemeChanged -= listener;

            return card;
        }

        public static Panel CreateCard(int width, int height)
        {
            return CreateContainer(0, 0, width, height);
        }

        public static Button CreateStyledButton(string text, int x, int y, int width, int height, Color bg, Color hover, string role = "")
        {
            return CreateButton(text, null, x, y, width, height, ButtonKind.Secondary);
        }

        public static Button CreateButton(string text, FluentIcon? icon, int x, int y, int width, int height, ButtonKind kind)
        {
            Color bg = RoleColor(kind);
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = bg,
                ForeColor = kind == ButtonKind.Primary ? Color.FromArgb(10, 10, 10) : TextPrimary,
                Font = BodyStrong,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = bg;
            btn.FlatAppearance.MouseOverBackColor = bg;
            buttonRoles[btn] = new ButtonRole { Kind = kind, Icon = icon };

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, btn.Width, btn.Height);
                using (GraphicsPath path = GetRoundedPath(rect, 6))
                {
                    btn.Region = new Region(path);
                    Color fill = btn.Enabled ? btn.BackColor : blendColor(btn.BackColor, BgColor, 0.52f);
                    if (kind == ButtonKind.Subtle && btn.BackColor == Color.Transparent) fill = Color.Transparent;
                    if (fill != Color.Transparent)
                    {
                        using (SolidBrush brush = new SolidBrush(fill))
                        {
                            e.Graphics.FillPath(brush, path);
                        }
                    }
                    if (kind != ButtonKind.Primary && kind != ButtonKind.Subtle)
                    {
                        using (Pen pen = new Pen(BorderColor, 1f))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }
                }

                Color textColor = btn.Enabled ? btn.ForeColor : TextMuted;
                if (icon.HasValue)
                {
                    Size textSize = TextRenderer.MeasureText(btn.Text, btn.Font);
                    int totalWidth = IconSize + 8 + textSize.Width;
                    int start = Math.Max(8, (btn.Width - totalWidth) / 2);
                    FluentIcons.DrawGlyph(e.Graphics, icon.Value,
                        new Rectangle(start, (btn.Height - IconSize) / 2, IconSize, IconSize), textColor, IconSize);
                    Rectangle textRect = new Rectangle(start + IconSize + 8, 0, textSize.Width + 4, btn.Height);
                    TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, textRect, textColor,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                }
                else
                {
                    TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle, textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                }
            };

            btn.MouseEnter += (s, e) =>
            {
                if (!btn.Enabled) return;
                if (kind == ButtonKind.Primary) btn.BackColor = AccentHover;
                else if (kind == ButtonKind.Danger) btn.BackColor = Color.FromArgb(42, 42, 42);
                else btn.BackColor = SurfaceHighlight;
            };
            btn.MouseLeave += (s, e) => btn.BackColor = btn.Enabled ? RoleColor(kind) : blendColor(RoleColor(kind), BgColor, 0.52f);
            btn.EnabledChanged += (s, e) => btn.BackColor = btn.Enabled ? RoleColor(kind) : blendColor(RoleColor(kind), BgColor, 0.52f);
            btn.Disposed += (s, e) => buttonRoles.Remove(btn);

            return btn;
        }

        public static Button CreatePrimaryButton(string text, int x, int y, int width, int height)
        {
            return CreateButton(text, null, x, y, width, height, ButtonKind.Primary);
        }

        public static Button CreateSecondaryButton(string text, int x, int y, int width, int height)
        {
            return CreateButton(text, null, x, y, width, height, ButtonKind.Secondary);
        }

        public static Button CreateDangerButton(string text, int x, int y, int width, int height)
        {
            return CreateButton(text, FluentIcon.Delete, x, y, width, height, ButtonKind.Danger);
        }

        public static Button CreateNavButton(FluentIcon icon, string text, int x, int y, int width, int height, bool selected = false)
        {
            Button btn = new Button
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = selected ? TextPrimary : TextMuted,
                Font = Caption,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;

            bool isSelected = selected;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (isSelected)
                {
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                    using (SolidBrush bg = new SolidBrush(SurfaceHighlight))
                    {
                        e.Graphics.FillPath(bg, path);
                    }
                    using (SolidBrush bar = new SolidBrush(Accent))
                    {
                        e.Graphics.FillRectangle(bar, 0, 10, 2, btn.Height - 20);
                    }
                }
                else if (btn.BackColor != Color.Transparent)
                {
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                    using (SolidBrush bg = new SolidBrush(btn.BackColor))
                    {
                        e.Graphics.FillPath(bg, path);
                    }
                }
                FluentIcons.DrawGlyph(e.Graphics, icon,
                    new Rectangle(16, (btn.Height - IconSize) / 2, IconSize, IconSize), btn.ForeColor, IconSize);
                TextRenderer.DrawText(e.Graphics, text, btn.Font, new Rectangle(38, 0, btn.Width - 46, btn.Height), btn.ForeColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
            };

            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = SurfaceHighlight;
                btn.ForeColor = TextPrimary;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = isSelected ? TextPrimary : TextMuted;
            };

            return btn;
        }

        public static Button CreateIconButton(FluentIcon icon, int x, int y, int size, string tooltip = "")
        {
            Button btn = new Button
            {
                Location = new Point(x, y),
                Size = new Size(size, size),
                BackColor = Color.Transparent,
                ForeColor = TextMuted,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            if (!string.IsNullOrEmpty(tooltip)) btn.Tag = tooltip;

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (btn.BackColor != Color.Transparent)
                {
                    using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                    using (SolidBrush bg = new SolidBrush(btn.BackColor))
                    {
                        e.Graphics.FillPath(bg, path);
                    }
                }
                FluentIcons.Draw(e.Graphics, icon,
                    new Rectangle((btn.Width - IconSize) / 2, (btn.Height - IconSize) / 2, IconSize, IconSize),
                    btn.ForeColor, 1.15f);
            };

            btn.MouseEnter += (s, e) => { btn.BackColor = SurfaceHighlight; btn.ForeColor = Accent; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = TextMuted; };

            return btn;
        }

        public static TextBox CreateStyledTextBox(int x, int y, int width, int height, bool password = false)
        {
            TextBox box = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = SurfaceHighlight,
                ForeColor = TextPrimary,
                Font = Caption,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = password ? '\u25CF' : '\0'
            };

            Action listener = () =>
            {
                box.BackColor = SurfaceHighlight;
                box.ForeColor = TextPrimary;
            };
            OnThemeChanged += listener;
            box.Disposed += (s, e) => OnThemeChanged -= listener;

            return box;
        }

        public static Label CreateLabel(string text, int x, int y, int width, int height, Font font = null, Color? color = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                AutoSize = false,
                Font = font ?? Regular,
                ForeColor = color ?? TextSecondary,
                BackColor = Color.Transparent
            };
        }

        public static Panel CreatePill(string text, Color color, int x, int y, int width, int height)
        {
            Panel pill = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.Transparent
            };
            pill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color fill = Color.FromArgb(30, color.R, color.G, color.B);
                using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, pill.Width - 1, pill.Height - 1), height / 2))
                {
                    using (SolidBrush bg = new SolidBrush(fill))
                    {
                        e.Graphics.FillPath(bg, path);
                    }
                    using (Pen pen = new Pen(Color.FromArgb(100, color.R, color.G, color.B), 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
                TextRenderer.DrawText(e.Graphics, text, Small, pill.ClientRectangle, TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            };
            return pill;
        }

        private sealed class ToastPanel : Panel
        {
            public ToastPanel()
            {
                SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            }
        }

        public static void ShowToast(Form owner, string message, bool success = true)
        {
            if (owner == null || owner.IsDisposed) return;

            int width = Math.Min(460, owner.Width - 60);
            ToastPanel toast = new ToastPanel
            {
                Size = new Size(width, 54),
                Location = new Point((owner.Width - width) / 2, owner.Height - 88),
                BackColor = Color.Transparent
            };

            Color accent = success ? Accent : Danger;
            FluentIcon icon = success ? FluentIcon.Check : FluentIcon.Warning;

            toast.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, toast.Width - 1, toast.Height - 1);
                using (GraphicsPath path = GetRoundedPath(rect, 8))
                {
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(248, SurfaceColor.R, SurfaceColor.G, SurfaceColor.B)))
                    {
                        e.Graphics.FillPath(bg, path);
                    }
                    using (Pen pen = new Pen(accent, 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
                FluentIcons.Draw(e.Graphics, icon, new Rectangle(16, (toast.Height - IconSize) / 2, IconSize, IconSize), accent, 1.15f);
                TextRenderer.DrawText(e.Graphics, message, Caption, new Rectangle(48, 6, toast.Width - 58, toast.Height - 12), TextPrimary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };

            owner.Controls.Add(toast);
            toast.BringToFront();

            Timer timer = new Timer { Interval = 2800 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                if (!toast.IsDisposed) toast.Dispose();
            };
            timer.Start();
        }
    }
}
