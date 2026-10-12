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

            this.FormClosed += (s, e) =>
            {
                if (themeListener != null)
                    MainUi.OnThemeChanged -= themeListener;
            };
        }

        private void BuildUI()
        {
            this.Text = "Theme Settings";
            this.Size = new Size(500, 460);
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

            Panel card = MainUi.CreateContainer(20, 54, 460, 386);

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
                Location = new Point(20, 48),
                Size = new Size(420, 320),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            AddThemeOption(grid, "Dark Default", LauncherTheme.DefaultDark, Color.FromArgb(0, 230, 153), Color.FromArgb(17, 20, 28));
            AddThemeOption(grid, "Full Dark", LauncherTheme.FullDark, Color.FromArgb(90, 90, 90), Color.FromArgb(8, 8, 8));
            AddThemeOption(grid, "Full White", LauncherTheme.FullWhite, Color.FromArgb(0, 150, 100), Color.FromArgb(240, 242, 246));
            AddThemeOption(grid, "Transparent", LauncherTheme.Transparent, Color.FromArgb(0, 230, 153), Color.FromArgb(30, 35, 45));
            AddThemeOption(grid, "Rainbow", LauncherTheme.Rainbow, Color.FromArgb(255, 90, 180), Color.FromArgb(22, 16, 32));
            AddThemeOption(grid, "Neon Red", LauncherTheme.Red, Color.FromArgb(255, 55, 85), Color.FromArgb(28, 15, 18));
            AddThemeOption(grid, "Deep Blue", LauncherTheme.Blue, Color.FromArgb(0, 160, 255), Color.FromArgb(16, 22, 34));
            AddThemeOption(grid, "Cyber Purple", LauncherTheme.Purple, Color.FromArgb(185, 75, 255), Color.FromArgb(24, 16, 36));
            AddThemeOption(grid, "Golden Sunset", LauncherTheme.Gold, Color.FromArgb(255, 195, 0), Color.FromArgb(28, 24, 16));

            card.Controls.Add(grid);
            this.Controls.Add(card);
        }

        private void AddThemeOption(FlowLayoutPanel panel, string name, LauncherTheme theme, Color accentPreview, Color bgPreview)
        {
            Panel card = new Panel
            {
                Size = new Size(130, 92),
                Margin = new Padding(5),
                Cursor = Cursors.Hand,
                BackColor = MainUi.SurfaceHighlight
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle previewRect = new Rectangle(8, 8, 114, 44);
                using (SolidBrush bgB = new SolidBrush(bgPreview))
                using (GraphicsPath p = MainUi.GetRoundedPath(previewRect, 4))
                {
                    e.Graphics.FillPath(bgB, p);
                }

                using (SolidBrush dotB = new SolidBrush(accentPreview))
                {
                    e.Graphics.FillEllipse(dotB, 16, 22, 16, 16);
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
                    e.Graphics.DrawString(name, MainUi.Small, textB, new Rectangle(6, 60, 118, 24), new StringFormat
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
