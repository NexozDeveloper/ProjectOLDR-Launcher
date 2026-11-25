using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public sealed class AdminForm : Form
    {
        private sealed class VersionRow : Panel
        {
            public VersionRow(GameVersion version, Action<GameVersion> edit, Action<GameVersion> remove)
            {
                Size = new Size(844, 104);
                BackColor = MainUi.SurfaceColor;
                Margin = new Padding(0, 0, 0, 12);
                Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                    using (System.Drawing.Drawing2D.GraphicsPath path = MainUi.GetRoundedPath(rect, 10))
                    {
                        Region = new Region(path);
                        using (SolidBrush bg = new SolidBrush(MainUi.SurfaceColor))
                        {
                            e.Graphics.FillPath(bg, path);
                        }
                        using (Pen pen = new Pen(MainUi.BorderColor, 1f))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }
                    using (SolidBrush stripe = new SolidBrush(MainUi.TextPrimary))
                    {
                        e.Graphics.FillRectangle(stripe, 0, 0, 2, Height);
                    }
                };

                Controls.Add(MainUi.CreateLabel(version.Name, 18, 12, 360, 28, MainUi.BodyStrong, MainUi.TextPrimary));
                Controls.Add(MainUi.CreatePill("BUILD " + version.DisplayTag, MainUi.TextPrimary, 18, 46, 116, 26));
                Controls.Add(MainUi.CreateLabel(version.DownloadUrl, 148, 48, 420, 22, MainUi.Small, MainUi.TextMuted));

                Button editButton = MainUi.CreateButton("Edit", FluentIcon.Edit, 626, 30, 92, 42, ButtonKind.Secondary);
                editButton.Click += (s, e) => edit(version);
                Controls.Add(editButton);

                Button deleteButton = MainUi.CreateButton("Delete", FluentIcon.Delete, 730, 30, 92, 42, ButtonKind.Danger);
                deleteButton.Click += (s, e) => remove(version);
                Controls.Add(deleteButton);
            }
        }

        private readonly AdminSession session;
        private readonly Panel versionsView;
        private readonly Panel accountsView;
        private readonly FlowLayoutPanel versionList;
        private readonly Label versionsStatus;
        private readonly Label accountsStatus;
        private readonly TextBox accountUserBox;
        private readonly TextBox accountPassBox;
        private Button versionsTabButton;
        private Button accountsTabButton;

        public AdminForm(AdminSession adminSession)
        {
            session = adminSession;
            MainUi.SetupForm(this, "Project OLDR - Admin", new Size(1120, 720));

            Panel sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 228,
                BackColor = MainUi.SurfaceColor
            };

            sidebar.Controls.Add(new IconBox(FluentIcon.Shield, 28, MainUi.Accent) { Location = new Point(24, 20) });
            sidebar.Controls.Add(MainUi.CreateLabel("ADMIN CONSOLE", 70, 16, 150, 24, MainUi.HeaderMedium, MainUi.TextPrimary));
            sidebar.Controls.Add(MainUi.CreateLabel("Manage versions and accounts", 20, 72, 196, 36, MainUi.Small, MainUi.TextMuted));

            versionsTabButton = MainUi.CreateNavButton(FluentIcon.Library, "Versions", 16, 142, 196, 46, true);
            versionsTabButton.Click += (s, e) => ShowView(true);
            sidebar.Controls.Add(versionsTabButton);

            accountsTabButton = MainUi.CreateNavButton(FluentIcon.Person, "Accounts", 16, 196, 196, 46);
            accountsTabButton.Click += (s, e) => ShowView(false);
            sidebar.Controls.Add(accountsTabButton);

            sidebar.Controls.Add(MainUi.CreateLabel("Changes are stored in Supabase.", 20, 596, 196, 30, MainUi.Small, MainUi.TextMuted));

            Button closeButton = MainUi.CreateButton("Close console", FluentIcon.Power, 16, 626, 196, 42, ButtonKind.Secondary);
            closeButton.Click += (s, e) => Close();
            sidebar.Controls.Add(closeButton);

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = MainUi.BgColor };
            Controls.Add(content);
            Controls.Add(sidebar);

            versionsView = new Panel { Dock = DockStyle.Fill, BackColor = MainUi.BgColor };
            accountsView = new Panel { Dock = DockStyle.Fill, BackColor = MainUi.BgColor, Visible = false };
            content.Controls.Add(accountsView);
            content.Controls.Add(versionsView);

            versionsView.Controls.Add(MainUi.CreateLabel("Version library", 24, 20, 400, 36, MainUi.HeaderLarge, MainUi.TextPrimary));
            versionsView.Controls.Add(MainUi.CreateLabel("Publish direct download links for every game version.", 24, 60, 600, 24, MainUi.Caption, MainUi.TextSecondary));

            Button addButton = MainUi.CreateButton("Add version", FluentIcon.Plus, 688, 24, 132, 42, ButtonKind.Primary);
            addButton.Click += AddButton_Click;
            versionsView.Controls.Add(addButton);

            Button refreshButton = MainUi.CreateIconButton(FluentIcon.Refresh, 832, 27, 36, "Refresh versions");
            refreshButton.Click += async (s, e) => await LoadVersionsAsync();
            versionsView.Controls.Add(refreshButton);

            versionList = new FlowLayoutPanel
            {
                Location = new Point(24, 100),
                Size = new Size(844, 540),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            versionsView.Controls.Add(versionList);

            versionsStatus = MainUi.CreateLabel("Loading versions...", 24, 644, 844, 26, MainUi.Small, MainUi.TextMuted);
            versionsView.Controls.Add(versionsStatus);

            accountsView.Controls.Add(MainUi.CreateLabel("Create account", 24, 20, 400, 36, MainUi.HeaderLarge, MainUi.TextPrimary));
            accountsView.Controls.Add(MainUi.CreateLabel("New users can sign in with the credentials you set here.", 24, 60, 700, 24, MainUi.Caption, MainUi.TextSecondary));

            Panel accountCard = MainUi.CreateContainer(24, 100, 640, 300);
            accountsView.Controls.Add(accountCard);

            accountCard.Controls.Add(MainUi.CreateLabel("USERNAME", 28, 30, 240, 18, MainUi.Small, MainUi.TextMuted));
            accountUserBox = MainUi.CreateStyledTextBox(28, 54, 584, 44);
            accountCard.Controls.Add(accountUserBox);

            accountCard.Controls.Add(MainUi.CreateLabel("PASSWORD", 28, 116, 240, 18, MainUi.Small, MainUi.TextMuted));
            accountPassBox = MainUi.CreateStyledTextBox(28, 140, 584, 44, true);
            accountCard.Controls.Add(accountPassBox);

            Button createButton = MainUi.CreateButton("Create account", FluentIcon.Plus, 28, 210, 584, 46, ButtonKind.Primary);
            createButton.Click += CreateAccount_Click;
            accountCard.Controls.Add(createButton);

            accountsStatus = MainUi.CreateLabel("Ready.", 24, 420, 700, 30, MainUi.Caption, MainUi.TextMuted);
            accountsView.Controls.Add(accountsStatus);

            Controls.Add(MainUi.CreateTitleBar(this, "Admin console - " + session.Username));
            Shown += async (s, e) => await LoadVersionsAsync();
        }

        private void ShowView(bool showVersions)
        {
            versionsView.Visible = showVersions;
            accountsView.Visible = !showVersions;
            versionsTabButton.BackColor = showVersions ? MainUi.SurfaceHighlight : Color.Transparent;
            accountsTabButton.BackColor = showVersions ? Color.Transparent : MainUi.SurfaceHighlight;
        }

        private async Task LoadVersionsAsync()
        {
            versionsStatus.Text = "Loading versions...";
            versionsStatus.ForeColor = MainUi.Warning;

            VersionFetchResult result = await SupabaseApi.GetVersionsAsync();
            versionList.Controls.Clear();

            if (!result.Success)
            {
                versionsStatus.Text = result.SetupNeeded
                    ? "Table missing: run supabase_setup.sql in your Supabase SQL editor."
                    : result.Message;
                versionsStatus.ForeColor = MainUi.Danger;
                return;
            }

            if (result.Versions.Count == 0)
            {
                versionsStatus.Text = "No versions yet. Use 'Add version' to publish the first one.";
                versionsStatus.ForeColor = MainUi.TextMuted;
                return;
            }

            foreach (GameVersion version in result.Versions)
            {
                versionList.Controls.Add(new VersionRow(version, EditVersion, DeleteVersion));
            }

            versionsStatus.Text = result.Versions.Count + " version(s) published.";
            versionsStatus.ForeColor = MainUi.Accent;
        }

        private async void AddButton_Click(object sender, EventArgs e)
        {
            using (VersionEditorForm editor = new VersionEditorForm(null))
            {
                if (editor.ShowDialog(this) != DialogResult.OK || editor.Result == null) return;
                await SaveVersionAsync(editor.Result);
            }
        }

        private async void EditVersion(GameVersion version)
        {
            using (VersionEditorForm editor = new VersionEditorForm(version.Clone()))
            {
                if (editor.ShowDialog(this) != DialogResult.OK || editor.Result == null) return;
                await SaveVersionAsync(editor.Result);
            }
        }

        private async Task SaveVersionAsync(GameVersion version)
        {
            versionsStatus.Text = "Saving version...";
            versionsStatus.ForeColor = MainUi.Warning;
            ApiResult result = await SupabaseApi.SaveVersionAsync(session, version);
            if (!result.Success)
            {
                versionsStatus.Text = result.Message;
                versionsStatus.ForeColor = MainUi.Danger;
                return;
            }
            MainUi.ShowToast(this, "Version saved successfully.");
            await LoadVersionsAsync();
        }

        private async void DeleteVersion(GameVersion version)
        {
            DialogResult confirm = MessageBox.Show(this, "Delete '" + version.Name + "' from the library?", "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ApiResult result = await SupabaseApi.DeleteVersionAsync(session, version.Id);
            if (!result.Success)
            {
                versionsStatus.Text = result.Message;
                versionsStatus.ForeColor = MainUi.Danger;
                return;
            }
            MainUi.ShowToast(this, "Version deleted.");
            await LoadVersionsAsync();
        }

        private async void CreateAccount_Click(object sender, EventArgs e)
        {
            string username = accountUserBox.Text.Trim();
            string password = accountPassBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                accountsStatus.Text = "Fill both fields.";
                accountsStatus.ForeColor = MainUi.Warning;
                return;
            }

            accountsStatus.Text = "Creating account...";
            accountsStatus.ForeColor = MainUi.Warning;
            ApiResult result = await SupabaseApi.RegisterAsync(username, password);
            accountsStatus.Text = result.Success ? "Account '" + username + "' created." : result.Message;
            accountsStatus.ForeColor = result.Success ? MainUi.Accent : MainUi.Danger;
            if (result.Success) accountUserBox.Clear();
            if (result.Success) accountPassBox.Clear();
        }
    }
}
