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
            this.Text = "Theme Settings";
            this.Size = new Size(620, 520);
            this.BackColor = MainUi.BgColor;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;

            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                using (GraphicsPath path = MainUi.GetRoundedPath(rect, 8))
                using (Pen pen = new Pen(MainUi.BorderColor, 1.2f))
                {
                    this.Region = new Region(path);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            this.Controls.Add(MainUi.CreateTitleBar(this, "Theme Customizer"));

            Panel card = MainUi.CreateContainer(20, 54, 580, 446);

            Label title = new Label
            {
                Text = "SELECT APPLICATION THEME",
                Font = MainUi.HeaderMedium,
                ForeColor = MainUi.TextPrimary,
                Location = new Point(20, 16),
                AutoSize = true
            };
            card.Controls.Add(title);

            FlowLayoutPanel grid = new FlowLayoutPanel
            {
                Location = new Point(18, 48),
                Size = new Size(544, 380),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            AddCustomBrushOption(grid);

            AddThemeOption(grid, "Default Dark", LauncherTheme.DefaultDark, Color.FromArgb(0, 230, 153), Color.FromArgb(17, 20, 28));
            AddThemeOption(grid, "Full Dark", LauncherTheme.FullDark, Color.FromArgb(120, 120, 140), Color.FromArgb(10, 10, 14));
            AddThemeOption(grid, "Full White", LauncherTheme.FullWhite, Color.FromArgb(0, 165, 110), Color.FromArgb(242, 244, 248));
            AddThemeOption(grid, "Transparent", LauncherTheme.Transparent, Color.FromArgb(0, 230, 153), Color.FromArgb(26, 31, 44));
            AddThemeOption(grid, "Rainbow Flow", LauncherTheme.Rainbow, Color.FromArgb(255, 90, 180), Color.FromArgb(20, 20, 28));

            AddThemeOption(grid, "Emerald", LauncherTheme.Emerald, Color.FromArgb(16, 212, 128), Color.FromArgb(14, 26, 20));
            AddThemeOption(grid, "Ocean Blue", LauncherTheme.OceanBlue, Color.FromArgb(30, 144, 255), Color.FromArgb(14, 22, 36));
            AddThemeOption(grid, "Neon Cyan", LauncherTheme.Cyan, Color.FromArgb(0, 225, 235), Color.FromArgb(12, 28, 32));
            AddThemeOption(grid, "Crimson Red", LauncherTheme.CrimsonRed, Color.FromArgb(255, 55, 75), Color.FromArgb(28, 14, 18));
            AddThemeOption(grid, "Cyber Pink", LauncherTheme.CyberPink, Color.FromArgb(255, 60, 170), Color.FromArgb(28, 14, 24));
            AddThemeOption(grid, "Electric Violet", LauncherTheme.ElectricViolet, Color.FromArgb(170, 75, 255), Color.FromArgb(22, 14, 34));
            AddThemeOption(grid, "Sunset Orange", LauncherTheme.SunsetOrange, Color.FromArgb(255, 115, 30), Color.FromArgb(30, 16, 10));
            AddThemeOption(grid, "Solar Gold", LauncherTheme.SolarGold, Color.FromArgb(255, 195, 0), Color.FromArgb(28, 22, 10));
            AddThemeOption(grid, "Mint Fresh", LauncherTheme.Mint, Color.FromArgb(45, 230, 180), Color.FromArgb(12, 28, 24));

            card.Controls.Add(grid);
            this.Controls.Add(card);
        }

        private void AddCustomBrushOption(FlowLayoutPanel panel)
        {
            Panel card = new Panel
            {
                Size = new Size(125, 96),
                Margin = new Padding(4),
                Cursor = Cursors.Hand,
                BackColor = MainUi.SurfaceHighlight
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle previewRect = new Rectangle(8, 8, 109, 46);
                using (SolidBrush bgB = new SolidBrush(Color.FromArgb(17, 20, 28)))
                using (GraphicsPath p = MainUi.GetRoundedPath(previewRect, 4))
                {
                    e.Graphics.FillPath(bgB, p);
                }

                IconRenderer.DrawPaintBrush(e.Graphics, new Rectangle(46, 12, 34, 34), MainUi.CustomColorValue);

                Color frameColor = (MainUi.CurrentTheme == LauncherTheme.Custom) ? MainUi.Accent : MainUi.BorderColor;
                float borderSize = (MainUi.CurrentTheme == LauncherTheme.Custom) ? 2f : 1f;

                using (Pen pen = new Pen(frameColor, borderSize))
                using (GraphicsPath path = MainUi.GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 6))
                {
                    card.Region = new Region(path);
                    e.Graphics.DrawPath(pen, path);
                }

                using (SolidBrush textB = new SolidBrush(MainUi.TextPrimary))
                {
                    e.Graphics.DrawString("Brush Picker", MainUi.Small, textB, new Rectangle(4, 62, 117, 24), new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
                }
            };

            card.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.FullOpen = true;
                    cd.Color = MainUi.CustomColorValue;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        MainUi.SetTheme(LauncherTheme.Custom, parentForm, cd.Color);
                        panel.Invalidate(true);
                    }
                }
            };

            panel.Controls.Add(card);
        }

        private void AddThemeOption(FlowLayoutPanel panel, string name, LauncherTheme theme, Color accentPreview, Color bgPreview)
        {
            Panel card = new Panel
            {
                Size = new Size(125, 96),
                Margin = new Padding(4),
                Cursor = Cursors.Hand,
                BackColor = MainUi.SurfaceHighlight
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle previewRect = new Rectangle(8, 8, 109, 46);
                using (SolidBrush bgB = new SolidBrush(bgPreview))
                using (GraphicsPath p = MainUi.GetRoundedPath(previewRect, 4))
                {
                    e.Graphics.FillPath(bgB, p);
                }

                using (SolidBrush dotB = new SolidBrush(accentPreview))
                {
                    e.Graphics.FillEllipse(dotB, 18, 22, 18, 18);
                }

                Color frameColor = (MainUi.CurrentTheme == theme) ? MainUi.Accent : MainUi.BorderColor;
                float borderSize = (MainUi.CurrentTheme == theme) ? 2f : 1f;

                using (Pen pen = new Pen(frameColor, borderSize))
                using (GraphicsPath path = MainUi.GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 6))
                {
                    card.Region = new Region(path);
                    e.Graphics.DrawPath(pen, path);
                }

                using (SolidBrush textB = new SolidBrush(MainUi.TextPrimary))
                {
                    e.Graphics.DrawString(name, MainUi.Small, textB, new Rectangle(4, 62, 117, 24), new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
                }
            };

            card.Click += (s, e) =>
            {
                MainUi.SetTheme(theme, parentForm);
                if (theme == LauncherTheme.Transparent)
                    this.Opacity = 0.85;
                else
                    this.Opacity = 1.0;

                panel.Invalidate(true);
            };

            panel.Controls.Add(card);
        }
    }
}
