using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ProjectOLDR
{
    public class MainForm : Form
    {
        private TextBox usernameBox;
        private TextBox passwordBox;
        private Button loginBtn;
        private Button registerBtn;
        private Label statusLabel;

        public MainForm()
        {

            usernameBox = new TextBox();
            passwordBox = new TextBox();
            loginBtn = new Button();
            registerBtn = new Button();
            statusLabel = new Label();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "Project OLDR - Login";
            this.Size = new Size(400, 400);
            this.BackColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = null;

            Label title = new Label();
            title.Text = "ProjectOLDR Launcher\nLogin Page";
            title.ForeColor = Color.White;
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.Size = new Size(380, 80);
            title.Location = new Point(10, 10);
            title.TextAlign = ContentAlignment.MiddleCenter;

            Label userLabel = new Label();
            userLabel.Text = "Username:";
            userLabel.ForeColor = Color.White;
            userLabel.Font = new Font("Segoe UI", 10);
            userLabel.Size = new Size(100, 25);
            userLabel.Location = new Point(50, 110);

            usernameBox.Location = new Point(150, 110);
            usernameBox.Size = new Size(200, 25);
            usernameBox.BackColor = Color.FromArgb(30, 30, 30);
            usernameBox.ForeColor = Color.White;
            usernameBox.BorderStyle = BorderStyle.FixedSingle;

            Label passLabel = new Label();
            passLabel.Text = "Password:";
            passLabel.ForeColor = Color.White;
            passLabel.Font = new Font("Segoe UI", 10);
            passLabel.Size = new Size(100, 25);
            passLabel.Location = new Point(50, 150);

            passwordBox.Location = new Point(150, 150);
            passwordBox.Size = new Size(200, 25);
            passwordBox.BackColor = Color.FromArgb(30, 30, 30);
            passwordBox.ForeColor = Color.White;
            passwordBox.BorderStyle = BorderStyle.FixedSingle;
            passwordBox.PasswordChar = '*';

            loginBtn.Text = "LOG IN";
            loginBtn.ForeColor = Color.White;
            loginBtn.BackColor = Color.FromArgb(0, 150, 0);
            loginBtn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            loginBtn.Size = new Size(100, 40);
            loginBtn.Location = new Point(90, 200);
            loginBtn.FlatStyle = FlatStyle.Flat;
            loginBtn.FlatAppearance.BorderSize = 0;
            loginBtn.Click += LoginBtn_Click;

            registerBtn.Text = "SIGN UP";
            registerBtn.ForeColor = Color.White;
            registerBtn.BackColor = Color.FromArgb(60, 60, 60);
            registerBtn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            registerBtn.Size = new Size(100, 40);
            registerBtn.Location = new Point(210, 200);
            registerBtn.FlatStyle = FlatStyle.Flat;
            registerBtn.FlatAppearance.BorderSize = 0;
            registerBtn.Click += RegisterBtn_Click;

            statusLabel.Text = "Enter your credentials";
            statusLabel.ForeColor = Color.Gray;
            statusLabel.Font = new Font("Segoe UI", 10);
            statusLabel.Size = new Size(380, 25);
            statusLabel.Location = new Point(10, 270);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;

            this.Controls.Add(title);
            this.Controls.Add(userLabel);
            this.Controls.Add(usernameBox);
            this.Controls.Add(passLabel);
            this.Controls.Add(passwordBox);
            this.Controls.Add(loginBtn);
            this.Controls.Add(registerBtn);
            this.Controls.Add(statusLabel);

            this.KeyDown += MainForm_KeyDown;
            this.KeyPreview = true;
        }

        private async void RegisterBtn_Click(object sender, EventArgs e)
        {
            string username = usernameBox.Text.Trim();
            string password = passwordBox.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                statusLabel.Text = "Please fill all fields!";
                statusLabel.ForeColor = Color.Orange;
                return;
            }

            statusLabel.Text = "Creating account...";
            statusLabel.ForeColor = Color.Yellow;

            ApiResult result = await SupabaseApi.RegisterAsync(username, password);
            if (result.Success)
            {
                statusLabel.Text = "Account created! You can log in.";
                statusLabel.ForeColor = Color.LightGreen;
                MessageBox.Show(
                    "Account created successfully!\n\nUsername: " + username + "\nPassword: " + password,
                    "Account details",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                statusLabel.Text = result.Message;
                statusLabel.ForeColor = Color.Red;
            }
        }

        private async void LoginBtn_Click(object sender, EventArgs e)
        {
            string username = usernameBox.Text.Trim();
            string password = passwordBox.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                statusLabel.Text = "Please fill all fields!";
                statusLabel.ForeColor = Color.Orange;
                return;
            }

            statusLabel.Text = "Connecting...";
            statusLabel.ForeColor = Color.Yellow;

            ApiResult result = await SupabaseApi.LoginAsync(username, password);
            if (result.Success)
            {
                statusLabel.Text = "Login successful!";
                statusLabel.ForeColor = Color.LightGreen;
                this.Hide();
                GameLauncherForm launcher = new GameLauncherForm(username);
                launcher.ShowDialog();
                this.Close();
            }
            else
            {
                statusLabel.Text = result.Message;
                statusLabel.ForeColor = Color.Red;
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
