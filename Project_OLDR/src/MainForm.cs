using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public class MainForm : Form
    {
        private TextBox usernameBox;
        private TextBox passwordBox;
        private Button loginBtn;
        private Button registerBtn;
        private Label statusLabel;
        private IconBox passwordToggle;
        private string serviceStatus = "Checking service";
        private Color serviceColor;

        public MainForm()
        {
            serviceColor = MainUi.Warning;
            BuildUI();
            LoadSavedCredentials();
            Shown += async (s, e) => await CheckDbStatusAsync();
        }

        private void BuildUI()
        {
            MainUi.SetupForm(this, "Project OLDR", new Size(1180, 720));

            Panel brand = new Panel
            {
                Dock = DockStyle.Left,
                Width = 440,
                BackColor = MainUi.BgColor
            };

            brand.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, brand.Width, brand.Height);
                using (LinearGradientBrush brush = new LinearGradientBrush(rect,
                    blend(MainUi.BgColor, Color.Black, 0.3f), blend(MainUi.BgColor, Color.White, 0.04f), 115f))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
                using (Pen divider = new Pen(MainUi.BorderColor, 1f))
                {
                    e.Graphics.DrawLine(divider, rect.Right - 1, 0, rect.Right - 1, rect.Bottom);
                }

                using (GraphicsPath logo = MainUi.GetRoundedPath(new Rectangle(48, 88, 32, 32), 8))
                using (SolidBrush logoBg = new SolidBrush(MainUi.SurfaceColor))
                {
                    e.Graphics.FillPath(logoBg, logo);
                    using (Pen logoBorder = new Pen(MainUi.BorderColor, 1f))
                    {
                        e.Graphics.DrawPath(logoBorder, logo);
                    }
                }
                FluentIcons.Draw(e.Graphics, FluentIcon.Gamepad, new Rectangle(56, 96, 14, 14), MainUi.Accent, 1.15f);

                TextRenderer.DrawText(e.Graphics, "PROJECT OLDR", new Font("Segoe UI Variable Display", 28, FontStyle.Bold),
                    new Rectangle(48, 138, 360, 44), MainUi.TextPrimary, TextFormatFlags.SingleLine);
                TextRenderer.DrawText(e.Graphics, "SECRET NEIGHBOR EDITION", new Font("Segoe UI Variable Text", 10, FontStyle.Bold),
                    new Rectangle(50, 186, 360, 20), MainUi.TextMuted, TextFormatFlags.SingleLine);

                using (Pen rule = new Pen(MainUi.BorderColor, 1f))
                {
                    e.Graphics.DrawLine(rule, 48, 224, 352, 224);
                }

                string[] lines = new string[]
                {
                    "One library for every game version",
                    "Fast direct downloads in AppData",
                    "Friends, presence and live updates"
                };
                FluentIcon[] icons = new FluentIcon[] { FluentIcon.Library, FluentIcon.Download, FluentIcon.People };
                for (int i = 0; i < lines.Length; i++)
                {
                    int y = 264 + i * 52;
                    FluentIcons.Draw(e.Graphics, icons[i], new Rectangle(48, y, 14, 14), MainUi.TextMuted, 1.15f);
                    TextRenderer.DrawText(e.Graphics, lines[i], MainUi.Caption, new Rectangle(76, y - 4, 316, 24), MainUi.TextSecondary, TextFormatFlags.VerticalCenter);
                }

                using (GraphicsPath hint = MainUi.GetRoundedPath(new Rectangle(48, 600, 186, 34), 17))
                using (SolidBrush hintBg = new SolidBrush(Color.FromArgb(28, 255, 255, 255)))
                {
                    e.Graphics.FillPath(hintBg, hint);
                }
                FluentIcons.Draw(e.Graphics, FluentIcon.Shield, new Rectangle(58, 608, 14, 14), MainUi.TextMuted, 1.15f);
                TextRenderer.DrawText(e.Graphics, "F2  ADMIN CONSOLE", MainUi.Small, new Rectangle(82, 604, 148, 26), MainUi.TextSecondary, TextFormatFlags.VerticalCenter);
            };

            Panel formArea = new Panel { Dock = DockStyle.Fill, BackColor = MainUi.BgColor };
            Controls.Add(formArea);
            Controls.Add(brand);

            Panel card = MainUi.CreateContainer(90, 78, 560, 520);
            formArea.Controls.Add(card);

            card.Controls.Add(MainUi.CreateLabel("WELCOME BACK", 40, 34, 260, 20, MainUi.Small, MainUi.Accent));
            card.Controls.Add(MainUi.CreateLabel("Sign in to your library", 40, 60, 480, 42, MainUi.HeaderLarge, MainUi.TextPrimary));
            card.Controls.Add(MainUi.CreateLabel("Your games, versions and friends in one place.", 40, 104, 480, 24, MainUi.Caption, MainUi.TextSecondary));

            Panel statusPill = new Panel { Location = new Point(40, 140), Size = new Size(178, 30), BackColor = Color.Transparent };
            statusPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = MainUi.GetRoundedPath(new Rectangle(0, 0, statusPill.Width - 1, statusPill.Height - 1), 15))
                {
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(30, serviceColor.R, serviceColor.G, serviceColor.B)))
                    {
                        e.Graphics.FillPath(bg, path);
                    }
                    using (Pen pen = new Pen(Color.FromArgb(120, serviceColor.R, serviceColor.G, serviceColor.B), 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
                e.Graphics.FillEllipse(new SolidBrush(serviceColor), 14, 12, 7, 7);
                TextRenderer.DrawText(e.Graphics, serviceStatus, MainUi.Small, new Rectangle(28, 0, 142, 30), serviceColor, TextFormatFlags.VerticalCenter);
            };
            card.Controls.Add(statusPill);

            card.Controls.Add(MainUi.CreateLabel("USERNAME", 40, 188, 240, 18, MainUi.Small, MainUi.TextMuted));
            card.Controls.Add(new IconBox(FluentIcon.Person, 24, MainUi.TextMuted) { Location = new Point(40, 216) });
            usernameBox = MainUi.CreateStyledTextBox(76, 206, 444, 44);
            card.Controls.Add(usernameBox);

            card.Controls.Add(MainUi.CreateLabel("PASSWORD", 40, 258, 240, 18, MainUi.Small, MainUi.TextMuted));
            card.Controls.Add(new IconBox(FluentIcon.Lock, 24, MainUi.TextMuted) { Location = new Point(40, 286) });
            passwordBox = MainUi.CreateStyledTextBox(76, 276, 444, 44, true);
            card.Controls.Add(passwordBox);

            passwordToggle = new IconBox(FluentIcon.View, 24, MainUi.TextMuted)
            {
                Location = new Point(482, 286),
                Cursor = Cursors.Hand
            };
            passwordToggle.Click += (s, e) =>
            {
                bool visible = passwordBox.PasswordChar == '\0';
                passwordBox.PasswordChar = visible ? '\u25CF' : '\0';
                passwordToggle.Icon = visible ? FluentIcon.View : FluentIcon.Hide;
                passwordToggle.IconColor = visible ? MainUi.TextMuted : MainUi.Accent;
            };
            card.Controls.Add(passwordToggle);

            loginBtn = MainUi.CreateButton("Sign in", FluentIcon.ArrowRight, 40, 336, 480, 48, ButtonKind.Primary);
            loginBtn.Click += LoginBtn_Click;
            card.Controls.Add(loginBtn);

            registerBtn = MainUi.CreateButton("Create an account", FluentIcon.Plus, 40, 394, 480, 44, ButtonKind.Secondary);
            registerBtn.Click += RegisterBtn_Click;
            card.Controls.Add(registerBtn);

            statusLabel = MainUi.CreateLabel("Ready to connect.", 40, 452, 480, 28, MainUi.Caption, MainUi.TextMuted);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(statusLabel);

            card.Controls.Add(MainUi.CreateLabel("Authorized staff can press F2 for the admin console.", 40, 486, 480, 20, MainUi.Small, MainUi.TextMuted));

            Controls.Add(MainUi.CreateTitleBar(this, "Project OLDR Launcher"));
            KeyDown += MainForm_KeyDown;
        }

        private static Color blend(Color from, Color to, float amount)
        {
            return MainUi.blendColor(from, to, amount);
        }

        private static string CredentialsPath
        {
            get
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nexoz");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return Path.Combine(folder, "credentials.dat");
            }
        }

        private void SaveCredentials(string username, string password)
        {
            try
            {
                byte[] raw = Encoding.UTF8.GetBytes(username + "||" + password);
                byte[] protectedBytes = ProtectedData.Protect(raw, null, DataProtectionScope.CurrentUser);
                File.WriteAllText(CredentialsPath, Convert.ToBase64String(protectedBytes));
            }
            catch
            {
            }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                string path = CredentialsPath;
                if (!File.Exists(path)) return;
                byte[] protectedBytes = Convert.FromBase64String(File.ReadAllText(path).Trim());
                byte[] raw = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                string[] parts = Encoding.UTF8.GetString(raw).Split(new string[] { "||" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    usernameBox.Text = parts[0];
                    passwordBox.Text = parts[1];
                }
            }
            catch
            {
            }
        }

        private async Task CheckDbStatusAsync()
        {
            ApiResult result = await SupabaseApi.CheckDatabaseHealthAsync();
            serviceStatus = result.Success ? "Service online" : "Service offline";
            serviceColor = result.Success ? MainUi.Accent : MainUi.Danger;
            Invalidate(true);
        }

        private async void RegisterBtn_Click(object sender, EventArgs e)
        {
            string username = usernameBox.Text.Trim();
            string password = passwordBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                SetStatus("Enter a username and a password.", MainUi.Danger);
                return;
            }

            SetStatus("Creating your account...", MainUi.Warning);
            ApiResult result = await SupabaseApi.RegisterAsync(username, password);
            if (!result.Success)
            {
                SetStatus(result.Message, MainUi.Danger);
                return;
            }

            SaveCredentials(username, password);
            SetStatus("Account created. You can sign in now.", MainUi.Accent);
        }

        private async void LoginBtn_Click(object sender, EventArgs e)
        {
            string username = usernameBox.Text.Trim();
            string password = passwordBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                SetStatus("Enter a username and a password.", MainUi.Danger);
                return;
            }

            SetStatus("Authenticating...", MainUi.Warning);
            loginBtn.Enabled = false;
            ApiResult result = await SupabaseApi.LoginAsync(username, password);
            loginBtn.Enabled = true;

            if (!result.Success)
            {
                SetStatus(result.Message, MainUi.Danger);
                return;
            }

            SaveCredentials(username, password);
            SetStatus("Access granted.", MainUi.Accent);
            Hide();
            using (GameLauncherForm launcher = new GameLauncherForm(username, password))
            {
                launcher.ShowDialog(this);
            }
            Show();
        }

        private void SetStatus(string text, Color color)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.F2) return;
            e.SuppressKeyPress = true;

            using (AdminGateForm gate = new AdminGateForm())
            {
                if (gate.ShowDialog(this) != DialogResult.OK || gate.Session == null) return;
                using (AdminForm admin = new AdminForm(gate.Session))
                {
                    admin.ShowDialog(this);
                }
            }
        }
    }
}
