using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public class GameLauncherForm : Form
    {
        private sealed class FluentProgress : Panel
        {
            private int value;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int Value
            {
                get { return value; }
                set
                {
                    this.value = Math.Max(0, Math.Min(100, value));
                    Invalidate();
                }
            }

            public FluentProgress()
            {
                Height = 8;
                BackColor = Color.Transparent;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle track = new Rectangle(0, 0, Width - 1, Height - 1);
                    using (GraphicsPath trackPath = MainUi.GetRoundedPath(track, 4))
                    using (SolidBrush bg = new SolidBrush(MainUi.SurfaceHighlight))
                    {
                        e.Graphics.FillPath(bg, trackPath);
                    }
                    if (value <= 0) return;
                    int fillWidth = Math.Max(5, (Width - 1) * value / 100);
                    Rectangle fillRect = new Rectangle(0, 0, fillWidth, Height - 1);
                    using (GraphicsPath fillPath = MainUi.GetRoundedPath(fillRect, 4))
                    using (LinearGradientBrush brush = new LinearGradientBrush(fillRect, MainUi.Accent, MainUi.AccentHover, 0f))
                    {
                        e.Graphics.FillPath(brush, fillPath);
                    }
                };

                Action listener = () => Invalidate();
                MainUi.OnThemeChanged += listener;
                Disposed += (s, e) => MainUi.OnThemeChanged -= listener;
            }
        }

        private sealed class GameRun
        {
            public GameVersion Version;
            public Process Process;
            public CancellationTokenSource Cancel;
        }

        private sealed class QualityButton : Button
        {
            private bool hover;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool Selected { get; set; }

            public QualityButton(string text, int x, int y, int width, int height)
            {
                Text = text;
                Location = new Point(x, y);
                Size = new Size(width, height);
                Font = MainUi.BodyStrong;
                FlatStyle = FlatStyle.Flat;
                BackColor = Color.Transparent;
                ForeColor = MainUi.TextPrimary;
                Cursor = Cursors.Hand;
                FlatAppearance.BorderSize = 0;
                FlatAppearance.MouseDownBackColor = Color.Transparent;
                FlatAppearance.MouseOverBackColor = Color.Transparent;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

                Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                    Color fill = Selected ? MainUi.Success : hover ? MainUi.BorderColor : MainUi.SurfaceHighlight;
                    Color border = Selected ? MainUi.Success : MainUi.BorderColor;
                    Color textColor = Selected ? Color.FromArgb(10, 10, 10) : MainUi.TextPrimary;

                    using (GraphicsPath path = MainUi.GetRoundedPath(rect, 6))
                    using (SolidBrush brush = new SolidBrush(fill))
                    {
                        e.Graphics.FillPath(brush, path);
                        using (Pen pen = new Pen(border, 1f))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }

                    TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                };

                MouseEnter += (s, e) => { hover = true; Invalidate(); };
                MouseLeave += (s, e) => { hover = false; Invalidate(); };
            }
        }

        private sealed class VersionCard : Panel
        {
            private readonly GameLauncherForm owner;
            private readonly GameVersion version;
            private static readonly Dictionary<string, string> QualityPreferences = new Dictionary<string, string>();

            private readonly Panel cover;
            private readonly Panel statusPill;
            private readonly Label statusText;
            private readonly Label notesLabel;
            private readonly Label playersLabel;
            private readonly QualityButton qualityButton;
            private readonly ContextMenuStrip qualityMenu;
            private readonly Button actionButton;
            private readonly Button folderButton;
            private readonly Button uninstallButton;
            private readonly FluentProgress progress;
            private readonly Label progressLabel;
            private string quality = "OriginalQuality";

            public bool IsDownloading;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public CancellationTokenSource CancelSource { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Button ActionButton => actionButton;

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string VersionId => version.Id;

            public VersionCard(GameLauncherForm form, GameVersion gameVersion)
            {
                owner = form;
                version = gameVersion;
                Size = new Size(984, 160);
                BackColor = MainUi.SurfaceColor;
                Margin = new Padding(0, 0, 0, 14);

                Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                    using (GraphicsPath path = MainUi.GetRoundedPath(rect, 12))
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
                };

                cover = new Panel { Location = new Point(20, 20), Size = new Size(56, 56), BackColor = Color.Transparent };
                cover.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle rect = new Rectangle(0, 0, cover.Width - 1, cover.Height - 1);
                    using (GraphicsPath path = MainUi.GetRoundedPath(rect, 10))
                    using (LinearGradientBrush brush = new LinearGradientBrush(rect,
                        Color.FromArgb(48, 48, 48), Color.FromArgb(20, 20, 20), 120f))
                    {
                        e.Graphics.FillPath(brush, path);
                        using (Pen pen = new Pen(MainUi.BorderColor, 1f))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }
                    string initial = string.IsNullOrWhiteSpace(version.Name) ? "?" : version.Name.Substring(0, 1).ToUpperInvariant();
                    TextRenderer.DrawText(e.Graphics, initial, new Font("Segoe UI Variable Display", 18, FontStyle.Bold),
                        new Rectangle(0, 0, cover.Width, cover.Height), MainUi.TextPrimary,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                };
                Controls.Add(cover);

                Controls.Add(MainUi.CreateLabel(version.Name, 92, 18, 430, 28,
                    new Font("Segoe UI Variable Text", 15, FontStyle.Bold), MainUi.TextPrimary));
                Controls.Add(MainUi.CreatePill("BUILD " + version.DisplayTag, MainUi.TextPrimary, 92, 52, 124, 24));

                playersLabel = MainUi.CreateLabel("Number Of Player Is Playing: 0", 228, 54, 340, 22,
                    MainUi.Small, MainUi.TextMuted);
                Controls.Add(playersLabel);

                notesLabel = MainUi.CreateLabel(string.IsNullOrWhiteSpace(version.Notes) ? "No notes for this release." : version.Notes,
                    92, 82, 430, 32, MainUi.Regular, MainUi.TextMuted);
                Controls.Add(notesLabel);

                statusPill = MainUi.CreatePill("", MainUi.TextMuted, 790, 20, 124, 26);
                statusText = MainUi.CreateLabel("", 790, 21, 124, 24, MainUi.Small, MainUi.TextMuted);
                statusText.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(statusPill);
                Controls.Add(statusText);

                qualityMenu = new ContextMenuStrip
                {
                    BackColor = MainUi.SurfaceColor,
                    ForeColor = MainUi.TextPrimary,
                    ShowImageMargin = false,
                    ShowCheckMargin = true,
                    RenderMode = ToolStripRenderMode.Professional
                };
                foreach (string option in VersionManager.QualityNames)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(QualityLabel(option)) { Tag = option };
                    item.Click += (s, e) => ApplyQuality((string)item.Tag);
                    qualityMenu.Items.Add(item);
                }

                qualityButton = new QualityButton("QUALITY: ORIGINAL", 600, 16, 170, 32);
                qualityButton.Click += (s, e) => qualityMenu.Show(qualityButton, new Point(0, qualityButton.Height));
                Controls.Add(qualityButton);

                actionButton = MainUi.CreateButton("DOWNLOAD", FluentIcon.Download, 600, 56, 170, 44, ButtonKind.Primary);
                actionButton.Click += ActionButton_Click;
                Controls.Add(actionButton);

                folderButton = MainUi.CreateIconButton(FluentIcon.Folder, 790, 64, 32, "Open game files folder");
                folderButton.Click += (s, e) => owner.OpenVersionFolder(version, quality);
                Controls.Add(folderButton);

                uninstallButton = MainUi.CreateIconButton(FluentIcon.Delete, 830, 64, 32, "Uninstall this version");
                uninstallButton.Click += (s, e) => owner.UninstallVersion(version, this);
                Controls.Add(uninstallButton);

                string savedQuality;
                if (QualityPreferences.TryGetValue(version.Id, out savedQuality) && IsQuality(savedQuality)) quality = savedQuality;
                ApplyQuality(quality);

                progress = new FluentProgress { Location = new Point(92, 126), Size = new Size(708, 6), Visible = false };
                Controls.Add(progress);

                progressLabel = MainUi.CreateLabel("", 92, 138, 708, 16, MainUi.Small, MainUi.TextMuted);
                progressLabel.Visible = false;
                Controls.Add(progressLabel);

                RefreshState();
            }

            public void RefreshState()
            {
                if (IsDownloading) return;
                bool installed = VersionManager.IsInstalled(version);
                actionButton.Text = installed ? "PLAY" : "DOWNLOAD";
                actionButton.ForeColor = Color.FromArgb(10, 10, 10);
                statusText.Text = installed ? "Installed" : "Not installed";
                statusText.ForeColor = installed ? MainUi.Accent : MainUi.TextMuted;
                statusPill.Invalidate();
                uninstallButton.Visible = installed;
            }

            public void SetPlayerCount(int count)
            {
                if (IsDisposed) return;
                playersLabel.Text = "Number Of Player Is Playing: " + count;
            }

            private static string QualityLabel(string value)
            {
                if (value == "MaxQuality") return "MAX QUALITY";
                if (value == "UltraPerformance") return "ULTRA PERFORMANCE";
                return "ORIGINAL QUALITY";
            }

            private static bool IsQuality(string value)
            {
                return value == "MaxQuality" || value == "OriginalQuality" || value == "UltraPerformance";
            }

            private void ApplyQuality(string selected)
            {
                if (!IsQuality(selected)) selected = "OriginalQuality";
                quality = selected;
                QualityPreferences[version.Id] = selected;
                qualityButton.Text = "QUALITY: " +
                    (selected == "MaxQuality" ? "MAX" : selected == "UltraPerformance" ? "ULTRA" : "ORIGINAL");
                qualityButton.Selected = true;
                foreach (ToolStripMenuItem item in qualityMenu.Items)
                {
                    item.Checked = string.Equals((string)item.Tag, selected, StringComparison.OrdinalIgnoreCase);
                }
            }

            public void SetProgress(int percent, string text)
            {
                progress.Visible = true;
                progressLabel.Visible = true;
                progress.Value = percent;
                progressLabel.Text = text;
            }

            public void HideProgress()
            {
                progress.Visible = false;
                progressLabel.Visible = false;
            }

            private void ActionButton_Click(object sender, EventArgs e)
            {
                if (IsDownloading)
                {
                    owner.CancelDownload(this);
                    return;
                }
                if (VersionManager.IsInstalled(version)) owner.PlayVersion(version, quality);
                else owner.StartDownload(this, version);
            }
        }

        private readonly string currentUsername;
        private readonly string currentPassword;
        private ProfileAvatar profileAvatar;
        private Label profileNameLabel;
        private Label profileSubLabel;
        private readonly FlowLayoutPanel versionFlow;
        private readonly Label onlineLabel;
        private readonly Label healthLabel;
        private readonly Label libraryStatus;
        private readonly Button adminNavButton;
        private System.Windows.Forms.Timer presenceTimer;
        private System.Windows.Forms.Timer statsTimer;
        private System.Windows.Forms.Timer versionsTimer;
        private AdminSession adminSession;
        private readonly List<GameRun> gameRuns = new List<GameRun>();
        private readonly object gameRunsLock = new object();
        private bool closing;

        public GameLauncherForm(string username, string password)
        {
            currentUsername = username;
            currentPassword = password ?? "";
            MainUi.SetupForm(this, "Project OLDR - Library", new Size(1280, 780));

            Panel sidebar = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = MainUi.SurfaceColor };

            sidebar.Controls.Add(new IconBox(FluentIcon.Gamepad, 28, MainUi.Accent) { Location = new Point(24, 20) });
            sidebar.Controls.Add(MainUi.CreateLabel("PROJECT OLDR", 70, 18, 154, 24, MainUi.HeaderMedium, MainUi.TextPrimary));
            sidebar.Controls.Add(MainUi.CreateLabel("Launcher", 70, 44, 154, 18, MainUi.Small, MainUi.TextMuted));

            Panel userCard = MainUi.CreateContainer(16, 84, 208, 78);
            userCard.Cursor = Cursors.Hand;
            userCard.Click += (s, e) => OpenProfileSettings();
            sidebar.Controls.Add(userCard);

            profileAvatar = new ProfileAvatar(44) { Location = new Point(16, 17) };
            userCard.Controls.Add(profileAvatar);
            profileNameLabel = MainUi.CreateLabel(username, 70, 15, 122, 23, MainUi.BodyStrong, MainUi.TextPrimary);
            userCard.Controls.Add(profileNameLabel);
            profileSubLabel = MainUi.CreateLabel("@" + username, 70, 38, 122, 18, MainUi.Small, MainUi.TextMuted);
            userCard.Controls.Add(profileSubLabel);
            healthLabel = MainUi.CreateLabel("Connecting...", 70, 58, 122, 16, MainUi.Small, MainUi.Warning);
            userCard.Controls.Add(healthLabel);

            onlineLabel = MainUi.CreateLabel("Players online: ...", 24, 176, 200, 22, MainUi.Regular, MainUi.TextSecondary);
            sidebar.Controls.Add(onlineLabel);

            sidebar.Controls.Add(MainUi.CreateLabel("LIBRARY", 24, 218, 160, 18, MainUi.Small, MainUi.TextMuted));
            sidebar.Controls.Add(MainUi.CreateNavButton(FluentIcon.Library, "Versions", 16, 246, 208, 46, true));

            Button friendsNav = MainUi.CreateNavButton(FluentIcon.People, "Friends", 16, 300, 208, 46);
            friendsNav.Click += (s, e) =>
            {
                using (FriendsForm friends = new FriendsForm(currentUsername))
                {
                    friends.ShowDialog(this);
                }
            };
            sidebar.Controls.Add(friendsNav);

            Button themesNav = MainUi.CreateNavButton(FluentIcon.Palette, "Themes", 16, 354, 208, 46);
            themesNav.Click += (s, e) =>
            {
                using (ThemeForm themes = new ThemeForm(this))
                {
                    themes.ShowDialog(this);
                }
            };
            sidebar.Controls.Add(themesNav);

            Button settingsNav = MainUi.CreateNavButton(FluentIcon.Settings, "Account", 16, 408, 208, 46);
            settingsNav.Click += (s, e) => OpenProfileSettings();
            sidebar.Controls.Add(settingsNav);

            adminNavButton = MainUi.CreateNavButton(FluentIcon.Shield, "Admin", 16, 462, 208, 46);
            adminNavButton.Visible = false;
            adminNavButton.Click += (s, e) => OpenAdmin();
            sidebar.Controls.Add(adminNavButton);

            sidebar.Controls.Add(MainUi.CreateLabel("F2 = admin console", 24, 646, 200, 30, MainUi.Small, MainUi.TextMuted));

            Button signOutNav = MainUi.CreateNavButton(FluentIcon.SignOut, "Sign out", 16, 682, 208, 44);
            signOutNav.Click += (s, e) => Close();
            sidebar.Controls.Add(signOutNav);

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = MainUi.BgColor };
            Controls.Add(content);
            Controls.Add(sidebar);

            Panel header = new Panel { Dock = DockStyle.Top, Height = 132, BackColor = MainUi.BgColor };

            header.Controls.Add(MainUi.CreateLabel("YOUR LIBRARY", 28, 20, 300, 20, MainUi.Small, MainUi.Accent));
            header.Controls.Add(MainUi.CreateLabel("Game versions", 28, 42, 500, 42, MainUi.HeaderLarge, MainUi.TextPrimary));
            libraryStatus = MainUi.CreateLabel("Loading versions...", 28, 92, 600, 22, MainUi.Regular, MainUi.TextMuted);
            header.Controls.Add(libraryStatus);

            Button refreshButton = MainUi.CreateButton("Refresh", FluentIcon.Refresh, 838, 44, 134, 38, ButtonKind.Secondary);
            refreshButton.Click += async (s, e) => await LoadVersionsAsync();
            header.Controls.Add(refreshButton);

            versionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(28, 4, 28, 24)
            };
            content.Controls.Add(versionFlow);
            content.Controls.Add(header);

            Controls.Add(MainUi.CreateTitleBar(this, "Project OLDR - Game library"));
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2)
                {
                    e.SuppressKeyPress = true;
                    OpenAdmin();
                }
            };

            FormClosed += (s, e) => Cleanup();
            Shown += async (s, e) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await SupabaseApi.UpdatePresenceAsync(currentUsername);
            await RefreshProfileAsync();
            await RefreshStatsAsync();
            await LoadVersionsAsync();

            presenceTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            presenceTimer.Tick += async (s, e) => await SupabaseApi.UpdatePresenceAsync(currentUsername);
            presenceTimer.Start();

            statsTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            statsTimer.Tick += async (s, e) => await RefreshStatsAsync();
            statsTimer.Start();

            versionsTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            versionsTimer.Tick += async (s, e) =>
            {
                if (!AnyDownloading()) await LoadVersionsAsync();
            };
            versionsTimer.Start();
        }

        private async Task RefreshStatsAsync()
        {
            int count = await SupabaseApi.GetOnlinePlayersCountAsync();
            onlineLabel.Text = "Players online: " + count;

            ApiResult health = await SupabaseApi.CheckDatabaseHealthAsync();
            healthLabel.Text = health.Success ? "Online" : "Offline";
            healthLabel.ForeColor = health.Success ? MainUi.Accent : MainUi.Danger;

            await RefreshPlayerCountsAsync();
        }

        private async Task RefreshPlayerCountsAsync()
        {
            Dictionary<string, int> counts = await SupabaseApi.GetGamePlayerCountsAsync();
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ApplyPlayerCounts(counts)));
                return;
            }
            ApplyPlayerCounts(counts);
        }

        private void ApplyPlayerCounts(Dictionary<string, int> counts)
        {
            foreach (Control control in versionFlow.Controls)
            {
                VersionCard card = control as VersionCard;
                if (card == null) continue;
                int count;
                card.SetPlayerCount(counts.TryGetValue(card.VersionId, out count) ? count : 0);
            }
        }

        private async Task RefreshProfileAsync()
        {
            LauncherProfile loaded = await ProfileStore.GetProfileAsync(currentUsername);
            if (loaded == null) loaded = new LauncherProfile { Username = currentUsername };
            await ProfileStore.SyncLocalAsync(loaded, currentPassword);
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ApplyProfile(loaded)));
                return;
            }
            ApplyProfile(loaded);
        }

        private void ApplyProfile(LauncherProfile profile)
        {
            if (IsDisposed) return;
            profileAvatar.SetProfile(profile);
            profileNameLabel.Text = FitText(profile.DisplayLabel, profileNameLabel.Font, profileNameLabel.Width);

            string bio = profile.Bio == null ? "" : profile.Bio.Trim();
            profileSubLabel.Text = FitText(bio.Length == 0 ? "@" + currentUsername : bio,
                profileSubLabel.Font, profileSubLabel.Width);
        }

        private static string FitText(string text, Font font, int maxWidth)
        {
            string value = text ?? "";
            if (TextRenderer.MeasureText(value, font).Width <= maxWidth) return value;

            string trimmed = value;
            while (trimmed.Length > 1 && TextRenderer.MeasureText(trimmed + "...", font).Width > maxWidth)
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }
            return trimmed + "...";
        }

        private void OpenProfileSettings()
        {
            using (ProfileSettingsForm settings = new ProfileSettingsForm(currentUsername, currentPassword))
            {
                settings.ShowDialog(this);
                if (settings.ProfileChanged) _ = RefreshProfileAsync();
            }
        }

        private bool AnyDownloading()
        {
            foreach (Control control in versionFlow.Controls)
            {
                VersionCard card = control as VersionCard;
                if (card != null && card.IsDownloading) return true;
            }
            return false;
        }

        private async Task LoadVersionsAsync()
        {
            if (AnyDownloading()) return;
            libraryStatus.Text = "Loading versions...";
            libraryStatus.ForeColor = MainUi.Warning;

            VersionFetchResult result = await SupabaseApi.GetVersionsAsync();
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RenderVersions(result)));
                return;
            }
            RenderVersions(result);
        }

        private void RenderVersions(VersionFetchResult result)
        {
            if (AnyDownloading()) return;
            versionFlow.Controls.Clear();

            if (!result.Success)
            {
                libraryStatus.Text = result.SetupNeeded
                    ? "Run supabase_setup.sql in Supabase to enable versions."
                    : result.Message;
                libraryStatus.ForeColor = MainUi.Danger;
                return;
            }

            if (result.Versions.Count == 0)
            {
                libraryStatus.Text = "No versions published yet. Admins can add one with F2.";
                libraryStatus.ForeColor = MainUi.TextMuted;
                return;
            }

            foreach (GameVersion version in result.Versions)
            {
                versionFlow.Controls.Add(new VersionCard(this, version));
            }

            libraryStatus.Text = result.Versions.Count + " version(s) available";
            libraryStatus.ForeColor = MainUi.Accent;
        }

        private async void PlayVersion(GameVersion version, string quality)
        {
            try
            {
                Process process = VersionManager.Launch(version, quality);
                if (process == null) throw new Exception("Could not start the game.");

                GameRun run = new GameRun
                {
                    Version = version,
                    Process = process,
                    Cancel = new CancellationTokenSource()
                };
                lock (gameRunsLock) gameRuns.Add(run);

                await SupabaseApi.JoinGameSessionAsync(currentUsername, currentPassword, version.Id);
                _ = TrackGameRunAsync(run);

                MainUi.ShowToast(this, "Starting " + version.Name + " in " + QualityText(quality) + ".");
                await RefreshPlayerCountsAsync();
            }
            catch (Exception ex)
            {
                MainUi.ShowToast(this, ex.Message, false);
            }
        }

        private void OpenVersionFolder(GameVersion version, string quality)
        {
            try
            {
                string path = VersionManager.OpenVersionFolder(version, quality);
                MainUi.ShowToast(this, "Opened game files: " + path);
            }
            catch (Exception ex)
            {
                MainUi.ShowToast(this, ex.Message, false);
            }
        }

        private async Task TrackGameRunAsync(GameRun run)
        {
            try
            {
                while (!run.Cancel.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(8000, run.Cancel.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    bool exited;
                    try
                    {
                        exited = run.Process == null || run.Process.HasExited;
                    }
                    catch
                    {
                        exited = true;
                    }
                    if (exited) break;

                    await SupabaseApi.JoinGameSessionAsync(currentUsername, currentPassword, run.Version.Id);
                }
            }
            finally
            {
                try
                {
                    await SupabaseApi.LeaveGameSessionAsync(currentUsername, currentPassword, run.Version.Id);
                }
                catch
                {
                }

                lock (gameRunsLock) gameRuns.Remove(run);
                run.Cancel.Dispose();
                await RefreshPlayerCountsAsync();
            }
        }

        private static string QualityText(string quality)
        {
            if (quality == "MaxQuality") return "MAX QUALITY";
            if (quality == "UltraPerformance") return "ULTRA PERFORMANCE";
            return "ORIGINAL QUALITY";
        }

        private async void StartDownload(VersionCard card, GameVersion version)
        {
            card.IsDownloading = true;
            card.CancelSource = new CancellationTokenSource();
            card.ActionButton.Text = "CANCEL";
            card.ActionButton.ForeColor = Color.FromArgb(10, 10, 10);
            card.SetProgress(0, "Preparing download...");

            try
            {
                IProgress<int> progress = new Progress<int>(percent =>
                {
                    if (card.IsDisposed) return;
                    card.SetProgress(percent, percent >= 100 ? "Installing files..." : "Downloading... " + percent + "%");
                });

                await VersionManager.InstallAsync(version, progress, card.CancelSource.Token);
                MainUi.ShowToast(this, version.Name + " installed.");
            }
            catch (OperationCanceledException)
            {
                MainUi.ShowToast(this, "Download cancelled.", false);
            }
            catch (Exception ex)
            {
                MainUi.ShowToast(this, ex.Message, false);
            }
            finally
            {
                card.IsDownloading = false;
                card.CancelSource.Dispose();
                card.CancelSource = null;
                card.HideProgress();
                card.RefreshState();
            }
        }

        private void CancelDownload(VersionCard card)
        {
            if (card.CancelSource != null && !card.CancelSource.IsCancellationRequested)
            {
                card.CancelSource.Cancel();
                card.ActionButton.Text = "CANCELLING";
            }
        }

        private void UninstallVersion(GameVersion version, VersionCard card)
        {
            try
            {
                VersionManager.Uninstall(version);
                card.RefreshState();
                MainUi.ShowToast(this, version.Name + " removed from this device.");
            }
            catch (Exception ex)
            {
                MainUi.ShowToast(this, ex.Message, false);
            }
        }

        private void OpenAdmin()
        {
            using (AdminGateForm gate = new AdminGateForm())
            {
                if (gate.ShowDialog(this) != DialogResult.OK || gate.Session == null) return;
                adminSession = gate.Session;
                adminNavButton.Visible = true;
                MainUi.ShowToast(this, "Admin access granted.");
                using (AdminForm admin = new AdminForm(adminSession))
                {
                    admin.ShowDialog(this);
                }
                _ = LoadVersionsAsync();
            }
        }

        private void Cleanup()
        {
            if (closing) return;
            closing = true;

            if (presenceTimer != null) { presenceTimer.Stop(); presenceTimer.Dispose(); }
            if (statsTimer != null) { statsTimer.Stop(); statsTimer.Dispose(); }
            if (versionsTimer != null) { versionsTimer.Stop(); versionsTimer.Dispose(); }

            List<GameRun> runs;
            lock (gameRunsLock) runs = new List<GameRun>(gameRuns);
            foreach (GameRun run in runs)
            {
                run.Cancel.Cancel();
                string versionId = run.Version.Id;
                Task.Run(() => SupabaseApi.LeaveGameSessionAsync(currentUsername, currentPassword, versionId)).GetAwaiter().GetResult();
            }

            Task.Run(() => SupabaseApi.RemovePresenceAsync(currentUsername)).GetAwaiter().GetResult();
            Environment.Exit(0);
        }
    }
}
