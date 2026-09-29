using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProjectOLDR
{
    public class AdminForm : Form
    {
        private TextBox secretBox;
        private TextBox newUserBox;
        private TextBox newPassBox;
        private Button createBtn;
        private Button verifyBtn;
        private Label statusLabel;

        private readonly string adminPassword = ConfigLoader.Get("AdminPassword");

        public AdminForm()
        {
            secretBox = new TextBox();
            newUserBox = new TextBox();
            newPassBox = new TextBox();
            createBtn = new Button();
            verifyBtn = new Button();
            statusLabel = new Label();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "Admin Panel - Project OLDR";
            this.Size = new Size(400, 300);
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Icon = null;

            Label secretLabel = new Label();
            secretLabel.Text = "Admin Password:";
            secretLabel.ForeColor = Color.White;
            secretLabel.Font = new Font("Segoe UI", 10);
            secretLabel.Size = new Size(120, 25);
            secretLabel.Location = new Point(20, 20);

            secretBox.Location = new Point(150, 20);
            secretBox.Size = new Size(200, 25);
            secretBox.BackColor = Color.FromArgb(30, 30, 30);
            secretBox.ForeColor = Color.White;
            secretBox.BorderStyle = BorderStyle.FixedSingle;
            secretBox.PasswordChar = '*';

            verifyBtn.Text = "Verify";
            verifyBtn.ForeColor = Color.White;
            verifyBtn.BackColor = Color.FromArgb(0, 100, 200);
            verifyBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            verifyBtn.Size = new Size(80, 25);
            verifyBtn.Location = new Point(360, 20);
            verifyBtn.FlatStyle = FlatStyle.Flat;
            verifyBtn.FlatAppearance.BorderSize = 0;
            verifyBtn.Click += VerifyBtn_Click;

            Label userLabel = new Label();
            userLabel.Text = "New Username:";
            userLabel.ForeColor = Color.White;
            userLabel.Font = new Font("Segoe UI", 10);
            userLabel.Size = new Size(120, 25);
            userLabel.Location = new Point(20, 70);
            userLabel.Visible = false;

            newUserBox.Location = new Point(150, 70);
            newUserBox.Size = new Size(200, 25);
            newUserBox.BackColor = Color.FromArgb(30, 30, 30);
            newUserBox.ForeColor = Color.White;
            newUserBox.BorderStyle = BorderStyle.FixedSingle;
            newUserBox.Visible = false;

            Label passLabel = new Label();
            passLabel.Text = "Password:";
            passLabel.ForeColor = Color.White;
            passLabel.Font = new Font("Segoe UI", 10);
            passLabel.Size = new Size(120, 25);
            passLabel.Location = new Point(20, 110);
            passLabel.Visible = false;

            newPassBox.Location = new Point(150, 110);
            newPassBox.Size = new Size(200, 25);
            newPassBox.BackColor = Color.FromArgb(30, 30, 30);
            newPassBox.ForeColor = Color.White;
            newPassBox.BorderStyle = BorderStyle.FixedSingle;
            newPassBox.PasswordChar = '*';
            newPassBox.Visible = false;

            createBtn.Text = "Create Account";
            createBtn.ForeColor = Color.White;
            createBtn.BackColor = Color.FromArgb(0, 150, 0);
            createBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            createBtn.Size = new Size(150, 40);
            createBtn.Location = new Point(115, 160);
            createBtn.FlatStyle = FlatStyle.Flat;
            createBtn.FlatAppearance.BorderSize = 0;
            createBtn.Visible = false;
            createBtn.Click += CreateBtn_Click;

            statusLabel.Text = "Enter admin password to continue";
            statusLabel.ForeColor = Color.Gray;
            statusLabel.Font = new Font("Segoe UI", 10);
            statusLabel.Size = new Size(380, 25);
            statusLabel.Location = new Point(10, 220);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;

            this.Controls.Add(secretLabel);
            this.Controls.Add(secretBox);
            this.Controls.Add(verifyBtn);
            this.Controls.Add(userLabel);
            this.Controls.Add(newUserBox);
            this.Controls.Add(passLabel);
            this.Controls.Add(newPassBox);
            this.Controls.Add(createBtn);
            this.Controls.Add(statusLabel);
        }

        private void VerifyBtn_Click(object sender, EventArgs e)
        {
            if (secretBox.Text == adminPassword)
            {
                statusLabel.Text = "Access granted! Create new accounts below.";
                statusLabel.ForeColor = Color.LightGreen;

                foreach (Control c in this.Controls)
                {
                    if (c is Label label && (label.Text.Contains("New") || label.Text.Contains("Password:")))
                        c.Visible = true;
                    else if (c is TextBox)
                        c.Visible = true;
                    else if (c == createBtn)
                        c.Visible = true;
                }
            }
            else
            {
                statusLabel.Text = "Invalid admin password!";
                statusLabel.ForeColor = Color.Red;
            }
        }

        private async void CreateBtn_Click(object sender, EventArgs e)
        {
            string username = newUserBox.Text.Trim();
            string password = newPassBox.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                statusLabel.Text = "Please fill all fields!";
                statusLabel.ForeColor = Color.Orange;
                return;
            }

            ApiResult result = await SupabaseApi.RegisterAsync(username, password);
            if (result.Success)
            {
                statusLabel.Text = $"Account '{username}' created!";
                statusLabel.ForeColor = Color.LightGreen;
                MessageBox.Show(
                    $"Account '{username}' created!\n\nPassword: {password}",
                    "Account details",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                newUserBox.Clear();
                newPassBox.Clear();
            }
            else
            {
                statusLabel.Text = result.Message;
                statusLabel.ForeColor = Color.Red;
            }
        }
    }
}