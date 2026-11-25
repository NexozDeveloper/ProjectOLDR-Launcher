using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public sealed class AdminGateForm : Form
    {
        private readonly TextBox usernameBox;
        private readonly TextBox passwordBox;
        private readonly Button unlockButton;
        private readonly Label statusLabel;

        public AdminSession Session { get; private set; }

        public AdminGateForm()
        {
            MainUi.SetupForm(this, "Admin Verification", new Size(520, 450));
            Controls.Add(MainUi.CreateTitleBar(this, "Admin verification"));

            Panel card = MainUi.CreateContainer(24, 68, 472, 330);
            Controls.Add(card);

            card.Controls.Add(new IconBox(FluentIcon.Shield, 28, MainUi.Accent) { Location = new Point(28, 24) });
            card.Controls.Add(MainUi.CreateLabel("Restricted access", 68, 20, 354, 30, MainUi.HeaderMedium, MainUi.TextPrimary));
            card.Controls.Add(MainUi.CreateLabel("Sign in with an authorized account to continue.", 68, 52, 374, 24, MainUi.Regular, MainUi.TextMuted));

            card.Controls.Add(MainUi.CreateLabel("USERNAME", 28, 98, 240, 18, MainUi.Small, MainUi.TextMuted));
            card.Controls.Add(new IconBox(FluentIcon.Person, 24, MainUi.TextMuted) { Location = new Point(28, 126) });
            usernameBox = MainUi.CreateStyledTextBox(64, 116, 380, 44);
            card.Controls.Add(usernameBox);

            card.Controls.Add(MainUi.CreateLabel("PASSWORD", 28, 172, 240, 18, MainUi.Small, MainUi.TextMuted));
            card.Controls.Add(new IconBox(FluentIcon.Lock, 24, MainUi.TextMuted) { Location = new Point(28, 200) });
            passwordBox = MainUi.CreateStyledTextBox(64, 190, 380, 44, true);
            card.Controls.Add(passwordBox);

            unlockButton = MainUi.CreateButton("Verify and continue", FluentIcon.Shield, 28, 250, 416, 48, ButtonKind.Primary);
            unlockButton.Click += UnlockButton_Click;
            card.Controls.Add(unlockButton);

            statusLabel = MainUi.CreateLabel("Authorized accounts: Nexoz, ryze", 28, 304, 416, 24, MainUi.Small, MainUi.TextMuted);
            card.Controls.Add(statusLabel);

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) UnlockAsync();
                if (e.KeyCode == Keys.Escape) DialogResult = DialogResult.Cancel;
            };
        }

        private void UnlockButton_Click(object sender, EventArgs e)
        {
            UnlockAsync();
        }

        private async void UnlockAsync()
        {
            if (!unlockButton.Enabled) return;

            string username = usernameBox.Text.Trim();
            string password = passwordBox.Text;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                statusLabel.Text = "Enter both fields.";
                statusLabel.ForeColor = MainUi.Warning;
                return;
            }

            unlockButton.Enabled = false;
            unlockButton.Text = "Verifying...";
            statusLabel.Text = "Checking credentials with Supabase...";
            statusLabel.ForeColor = MainUi.Warning;

            ApiResult result = await SupabaseApi.ValidateAdminAsync(username, password);
            bool allowed = IsAuthorized(username);

            if (!result.Success || !allowed)
            {
                statusLabel.Text = "Access denied.";
                statusLabel.ForeColor = MainUi.Danger;
                Timer closeTimer = new Timer { Interval = 850 };
                closeTimer.Tick += (s, e) =>
                {
                    closeTimer.Stop();
                    closeTimer.Dispose();
                    DialogResult = DialogResult.Cancel;
                    Close();
                };
                closeTimer.Start();
                return;
            }

            Session = new AdminSession { Username = username, Password = password };
            DialogResult = DialogResult.OK;
            Close();
        }

        private static bool IsAuthorized(string username)
        {
            string configured = ConfigLoader.Get("AdminUsernames");
            if (string.IsNullOrWhiteSpace(configured)) configured = "Nexoz;ryze";
            return configured.Split(';')
                .Any(name => string.Equals(name.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
