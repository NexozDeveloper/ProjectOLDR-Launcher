using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private Label dbHealthLabel;

        public MainForm()
        {
            usernameBox = new TextBox();
            passwordBox = new TextBox();
            loginBtn = new Button();
            registerBtn = new Button();
            statusLabel = new Label();
            dbHealthLabel = new Label();

            BuildUI();

            this.Shown += async (s, e) => await CheckDbStatusAsync();
        }

        private void BuildUI()
        {
            this.Text = "Project OLDR";
            this.Size = new Size(420, 520);
            this.BackColor = MainUi.BgColor;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;

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

            this.Controls.Add(MainUi.CreateTitleBar(this, "Project OLDR Launcher"));

            Panel card = MainUi.CreateContainer(35, 55, 350, 430);

            Label title = new Label
            {
                Text = "Welcome Back",
                Font = MainUi.HeaderLarge,
                ForeColor = MainUi.TextPrimary,
                Location = new Point(20, 20),
                Size = new Size(310, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label subtitle = new Label
            {
                Text = "Sign in to Secret Neighbor Edition",
                Font = MainUi.Small,
                ForeColor = MainUi.TextMuted,
                Location = new Point(20, 50),
                Size = new Size(310, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            dbHealthLabel = new Label
            {
                Text = "● Checking Supabase...",
                Font = MainUi.Small,
                ForeColor = MainUi.Warning,
                Location = new Point(20, 75),
                Size = new Size(310, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label userTag = new Label
            {
                Text = "USERNAME",
                Font = MainUi.Small,
                ForeColor = MainUi.TextSecondary,
                Location = new Point(25, 110),
                Size = new Size(300, 16)
            };

            usernameBox = MainUi.CreateStyledTextBox(25, 130, 300, 30);

            Label passTag = new Label
            {
                Text = "PASSWORD",
                Font = MainUi.Small,
                ForeColor = MainUi.TextSecondary,
                Location = new Point(25, 175),
                Size = new Size(300, 16)
            };

            passwordBox = MainUi.CreateStyledTextBox(25, 195, 300, 30, true);

            loginBtn = MainUi.CreateStyledButton("Sign In", 25, 250, 300, 42, MainUi.Accent, MainUi.AccentHover);
            loginBtn.Click += LoginBtn_Click;

            registerBtn = MainUi.CreateStyledButton("Create Account", 25, 300, 300, 38, MainUi.SurfaceHighlight, MainUi.BorderColor);
            registerBtn.Click += RegisterBtn_Click;

            statusLabel = new Label
            {
                Text = "Ready to connect",
                Font = MainUi.Small,
                ForeColor = MainUi.TextMuted,
                Location = new Point(25, 355),
                Size = new Size(300, 45),
                TextAlign = ContentAlignment.TopCenter
            };

            card.Controls.Add(title);
            card.Controls.Add(subtitle);
            card.Controls.Add(dbHealthLabel);
            card.Controls.Add(userTag);
            card.Controls.Add(usernameBox);
            card.Controls.Add(passTag);
            card.Controls.Add(passwordBox);
            card.Controls.Add(loginBtn);
            card.Controls.Add(registerBtn);
            card.Controls.Add(statusLabel);

            this.Controls.Add(card);

            this.KeyDown += MainForm_KeyDown;
        }

        private async Task CheckDbStatusAsync()
        {
            var result = await SupabaseApi.CheckDatabaseHealthAsync();
            if (result.Success)
            {
                dbHealthLabel.Text = "● " + result.Message;
                dbHealthLabel.ForeColor = MainUi.Accent;
            }
            else
            {
                dbHealthLabel.Text = "● " + result.Message;
                dbHealthLabel.ForeColor = MainUi.Danger;
            }
        }

        private async void RegisterBtn_Click(object sender, EventArgs e)
        {
            string username = usernameBox.Text.Trim();
            string password = passwordBox.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                statusLabel.Text = "Please complete all fields.";
                statusLabel.ForeColor = MainUi.Danger;
                return;
            }

            statusLabel.Text = "Creating user account...";
            statusLabel.ForeColor = MainUi.Warning;

            ApiResult result = await SupabaseApi.RegisterAsync(username, password);
            if (result.Success)
            {
                statusLabel.Text = "Account registered! You can now sign in.";
                statusLabel.ForeColor = MainUi.Accent;
            }
            else
            {
                statusLabel.Text = result.Message;
                statusLabel.ForeColor = MainUi.Danger;
            }
        }

        private async void LoginBtn_Click(object sender, EventArgs e)
        {
            string username = usernameBox.Text.Trim();
            string password = passwordBox.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                statusLabel.Text = "Please complete all fields.";
                statusLabel.ForeColor = MainUi.Danger;
                return;
            }

            statusLabel.Text = "Authenticating profile...";
            statusLabel.ForeColor = MainUi.Warning;

            ApiResult result = await SupabaseApi.LoginAsync(username, password);
            if (result.Success)
            {
                statusLabel.Text = "Access granted.";
                statusLabel.ForeColor = MainUi.Accent;

                this.Hide();
                GameLauncherForm launcher = new GameLauncherForm(username);
                launcher.ShowDialog();
                this.Close();
            }
            else
            {
                statusLabel.Text = result.Message;
                statusLabel.ForeColor = MainUi.Danger;
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                AdminForm admin = new AdminForm();
                admin.ShowDialog();
            }
        }
    }
}
