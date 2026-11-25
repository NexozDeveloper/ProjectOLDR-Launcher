using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public class ThemeForm : Form
    {
        private Form parentForm;
        private Action themeListener;
        private Action rainbowListener;

        public ThemeForm(Form parent)
        {
            this.parentForm = parent;
            BuildUI();

            themeListener = () =>
            {
                this.BackColor = MainUi.BgColor;
                this.Invalidate(true);
            };
            MainUi.OnThemeChanged += themeListener;

            rainbowListener = () =>
            {
                this.Invalidate(false);
            };
            MainUi.OnRainbowTick += rainbowListener;

            this.FormClosed += (s, e) =>
            {
                if (themeListener != null)
                    MainUi.OnThemeChanged -= themeListener;
                if (rainbowListener != null)
                    MainUi.OnRainbowTick -= rainbowListener;
            };
        }

        private void BuildUI()
        {
            MainUi.SetupForm(this, "Theme Settings", new Size(700, 360));
            this.Controls.Add(MainUi.CreateTitleBar(this, "Appearance"));

            Panel card = MainUi.CreateContainer(20, 68, 660, 240);

            Label title = new Label
            {
                Text = "MONOCHROME THEMES",
                Font = MainUi.BodyStrong,
                ForeColor = MainUi.TextPrimary,
                Location = new Point(20, 16),
                AutoSize = true
            };
            card.Controls.Add(title);

            Label subtitle = new Label
            {
                Text = "Black, white and shades of gray only.",
                Font = MainUi.Small,
                ForeColor = MainUi.TextMuted,
                Location = new Point(20, 38),
                AutoSize = true
            };
            card.Controls.Add(subtitle);

            FlowLayoutPanel grid = new FlowLayoutPanel
            {
                Location = new Point(18, 68),
                Size = new Size(620, 152),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            AddThemeOption(grid, "Default", LauncherTheme.DefaultDark, Color.FromArgb(10, 10, 10), Color.FromArgb(255, 255, 255));
            AddThemeOption(grid, "Darker", LauncherTheme.FullDark, Color.FromArgb(5, 5, 5), Color.FromArgb(255, 255, 255));
            AddThemeOption(grid, "Light", LauncherTheme.FullWhite, Color.FromArgb(245, 245, 245), Color.FromArgb(10, 10, 10));
            AddThemeOption(grid, "Transparent", LauncherTheme.Transparent, Color.FromArgb(10, 10, 10), Color.FromArgb(255, 255, 255));

            card.Controls.Add(grid);
            this.Controls.Add(card);
        }

        private void AddThemeOption(FlowLayoutPanel panel, string name, LauncherTheme theme, Color bgPreview, Color accentPreview)
        {
            Panel card = new Panel
            {
                Size = new Size(140, 112),
                Margin = new Padding(8),
                Cursor = Cursors.Hand,
                BackColor = MainUi.SurfaceHighlight
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle previewRect = new Rectangle(10, 10, 120, 56);
                using (SolidBrush bgB = new SolidBrush(bgPreview))
                using (GraphicsPath previewPath = MainUi.GetRoundedPath(previewRect, 6))
                {
                    e.Graphics.FillPath(bgB, previewPath);
                }

                using (SolidBrush dotB = new SolidBrush(accentPreview))
                {
                    e.Graphics.FillEllipse(dotB, 22, 28, 12, 12);
                    e.Graphics.FillRectangle(dotB, 42, 31, 58, 6);
                    e.Graphics.FillRectangle(dotB, 42, 43, 38, 4);
                }

                bool selected = MainUi.CurrentTheme == theme;
                Color frameColor = selected ? MainUi.Accent : MainUi.BorderColor;
                float borderSize = selected ? 2f : 1f;
                using (Pen pen = new Pen(frameColor, borderSize))
                using (GraphicsPath path = MainUi.GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8))
                {
                    card.Region = new Region(path);
                    e.Graphics.DrawPath(pen, path);
                }

                using (SolidBrush textB = new SolidBrush(MainUi.TextPrimary))
                {
                    e.Graphics.DrawString(name, MainUi.Small, textB, new Rectangle(4, 76, 132, 24), new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
                }
            };

            card.Click += (s, e) =>
            {
                MainUi.SetTheme(theme, parentForm);
                this.Opacity = theme == LauncherTheme.Transparent ? 0.94 : 1.0;
                panel.Invalidate(true);
            };

            panel.Controls.Add(card);
        }
    }
}
