using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProjectOLDR.UI
{
    public static class MainUi
    {
        public static readonly Color BackgroundColor = Color.Black;
        public static readonly Color AccentColor = Color.FromArgb(0, 150, 0);
        public static readonly Color DarkColor = Color.FromArgb(30, 30, 30);
        public static readonly Color TextColor = Color.White;
        public static readonly Color GrayTextColor = Color.Gray;

        public static readonly Font TitleFont = new Font("Segoe UI", 18, FontStyle.Bold);
        public static readonly Font SubTitleFont = new Font("Segoe UI", 16, FontStyle.Bold);
        public static readonly Font ButtonFont = new Font("Segoe UI", 12, FontStyle.Bold);
        public static readonly Font LabelFont = new Font("Segoe UI", 10);
        public static readonly Font SmallFont = new Font("Segoe UI", 9);
        public static readonly Font BigButtonFont = new Font("Segoe UI", 16, FontStyle.Bold);

        public static Label CreateTitle(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = TextColor,
                Font = TitleFont,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
        }

        public static Label CreateSubTitle(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = GrayTextColor,
                Font = SubTitleFont,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
        }

        public static Label CreateLabel(string text, int x, int y, int width = 120, int height = 25)
        {
            return new Label
            {
                Text = text,
                ForeColor = TextColor,
                Font = LabelFont,
                Size = new Size(width, height),
                Location = new Point(x, y),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
        }

        public static TextBox CreateTextBox(int x, int y, int width = 200, int height = 25, bool password = false)
        {
            return new TextBox
            {
                Size = new Size(width, height),
                Location = new Point(x, y),
                BackColor = DarkColor,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = password ? '*' : '\0'
            };
        }

        public static Button CreateButton(string text, int x, int y, int width = 100, int height = 40, Color? color = null)
        {
            return new Button
            {
                Text = text,
                ForeColor = TextColor,
                BackColor = color ?? AccentColor,
                Font = ButtonFont,
                Size = new Size(width, height),
                Location = new Point(x, y),
                FlatStyle = FlatStyle.Flat
            };
        }

        public static Button CreateSmallButton(string text, int x, int y, int width = 80, int height = 25)
        {
            return new Button
            {
                Text = text,
                ForeColor = TextColor,
                BackColor = Color.FromArgb(60, 60, 60),
                Font = SmallFont,
                Size = new Size(width, height),
                Location = new Point(x, y),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        public static ProgressBar CreateProgressBar(int x, int y, int width = 400, int height = 30)
        {
            return new ProgressBar
            {
                Size = new Size(width, height),
                Location = new Point(x, y),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };
        }

        public static Label CreateStatusLabel(int x, int y, int width = 480, int height = 25)
        {
            return new Label
            {
                Text = "Status: Ready",
                ForeColor = GrayTextColor,
                Font = LabelFont,
                Size = new Size(width, height),
                Location = new Point(x, y),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
        }
    }
}