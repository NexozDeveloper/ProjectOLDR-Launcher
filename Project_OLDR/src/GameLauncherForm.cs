using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public class GameLauncherForm : Form
    {
        private const string CurrentLauncherVersion = "2.0.0.1";

        private Panel topBar;
        private Panel heroCard;
        private Panel playersBadge;
        private Label statusLabel;
        private Label dbStatusLabel;
        private Label onlinePlayersLabel;
        private Label versionLabel;
        private Label gameTitle;
        private Label gameInfo;
        private Button playBtn;
        private Button updateBtn;
        private Button themeBtn;
        private Button friendsBtn;
        private Button logoutBtn;
        private ProgressBar progressBar;
        private Label progressLabel;

        private string gamePath = "";
        private string appDataPath = "";
        private string documentsPath = "";
        private string rarPath = "";
        private string updatedFlagPath = "";
        private string currentUsername;
        private System.Windows.Forms.Timer refreshTimer;
        private Action themeListener;
        private Action rainbowListener;

        public GameLauncherForm(string username)
        {
            currentUsername = username;
            BuildUI();
            InitializePaths();

            themeListener = () => ApplyThemeColors();
            MainUi.OnThemeChanged += themeListener;

            rainbowListener = () =>
            {
                playBtn.BackColor = MainUi.Accent;
                playBtn.Invalidate();
                this.Invalidate(new Rectangle(0, 0, this.Width, 42));
            };
            MainUi.OnRainbowTick += rainbowListener;

            this.Shown += async (s, e) =>
            {
                ApplyThemeColors();
                CheckIfJustUpdated();
                await InitializeDashboardAsync();
            };
            this.FormClosed += GameLauncherForm_FormClosed;
        }

        private void BuildUI()
        {
            this.Text = "Project OLDR";
            this.Size = new Size(1020, 640);
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

            this.Controls.Add(MainUi.CreateTitleBar(this, "Project OLDR - Game Client"));

            topBar = MainUi.CreateContainer(24, 56, 972, 70);

            Panel avatar = new Panel
            {
                Location = new Point(18, 15),
                Size = new Size(40, 40),
                BackColor = MainUi.SurfaceHighlight
            };
            avatar.Paint += (s, e) =>
            {
                avatar.BackColor = MainUi.SurfaceHighlight;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath p = MainUi.GetRoundedPath(new Rectangle(0, 0, 39, 39), 20))
                {
                    avatar.Region = new Region(p);
                }
                IconRenderer.DrawUser(e.Graphics, new Rectangle(11, 10, 18, 20), MainUi.Accent);
            };

            Label userLabel = new Label
            {
                Text = currentUsername,
                Font = MainUi.HeaderMedium,
                ForeColor = MainUi.TextPrimary,
                Location = new Point(68, 14),
                AutoSize = true
            };

            dbStatusLabel = new Label
            {
                Text = "● Checking Network...",
                Font = MainUi.Small,
                ForeColor = MainUi.Warning,
                Location = new Point(69, 39),
                AutoSize = true
            };

            playersBadge = MainUi.CreateContainer(370, 15, 240, 40);
            onlinePlayersLabel = new Label
            {
                Text = "Players in launcher: ...",
                Font = MainUi.Small,
                ForeColor = MainUi.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            playersBadge.Controls.Add(onlinePlayersLabel);

            themeBtn = MainUi.CreateStyledButton("Themes", 670, 16, 90, 38, MainUi.SurfaceHighlight, MainUi.BorderColor);
            themeBtn.Click += (s, e) =>
            {
                using (ThemeForm form = new ThemeForm(this))
                    form.ShowDialog(this);
            };

            friendsBtn = MainUi.CreateStyledButton("Friends", 768, 16, 90, 38, MainUi.SurfaceHighlight, MainUi.BorderColor);
            friendsBtn.Click += (s, e) =>
            {
                using (FriendsForm form = new FriendsForm(currentUsername))
                    form.ShowDialog(this);
            };

            logoutBtn = MainUi.CreateStyledButton("Sign Out", 866, 16, 88, 38, Color.FromArgb(40, 20, 25), MainUi.Danger);
            logoutBtn.Click += async (s, e) => await SignOutAsync();

            topBar.Controls.Add(avatar);
            topBar.Controls.Add(userLabel);
            topBar.Controls.Add(dbStatusLabel);
            topBar.Controls.Add(playersBadge);
            topBar.Controls.Add(themeBtn);
            topBar.Controls.Add(friendsBtn);
            topBar.Controls.Add(logoutBtn);

            heroCard = MainUi.CreateContainer(24, 142, 972, 472);

            gameTitle = new Label
            {
                Text = "SECRET NEIGHBOR",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = MainUi.TextPrimary,
                Location = new Point(36, 32),
                AutoSize = true
            };

            gameInfo = new Label
            {
                Text = "Direct Network Play • Build 2020",
                Font = MainUi.HeaderMedium,
                ForeColor = MainUi.TextSecondary,
                Location = new Point(38, 86),
                AutoSize = true
            };

            progressBar = new ProgressBar
            {
                Location = new Point(38, 205),
                Size = new Size(896, 16),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            progressLabel = new Label
            {
                Text = "",
                Font = MainUi.Small,
                ForeColor = MainUi.TextMuted,
                Location = new Point(38, 228),
                Size = new Size(896, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            playBtn = MainUi.CreateStyledButton("PLAY GAME", 38, 305, 250, 60, MainUi.Accent, MainUi.AccentHover);
            playBtn.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            playBtn.Enabled = false;
            playBtn.Click += (s, e) => LaunchGame();

            updateBtn = MainUi.CreateStyledButton("Check Updates", 306, 313, 175, 46, MainUi.SurfaceHighlight, MainUi.BorderColor);
            updateBtn.Font = MainUi.HeaderMedium;
            updateBtn.Click += async (s, e) => await CheckUpdatesManualAsync();

            statusLabel = new Label
            {
                Text = "Checking local files...",
                Font = MainUi.Regular,
                ForeColor = MainUi.TextMuted,
                Location = new Point(38, 410),
                Size = new Size(700, 25)
            };

            versionLabel = new Label
            {
                Text = $"v{CurrentLauncherVersion}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = MainUi.TextMuted,
                Location = new Point(818, 410),
                Size = new Size(116, 25),
                TextAlign = ContentAlignment.MiddleRight
            };

            heroCard.Controls.Add(gameTitle);
            heroCard.Controls.Add(gameInfo);
            heroCard.Controls.Add(progressBar);
            heroCard.Controls.Add(progressLabel);
            heroCard.Controls.Add(playBtn);
            heroCard.Controls.Add(updateBtn);
            heroCard.Controls.Add(statusLabel);
            heroCard.Controls.Add(versionLabel);

            this.Controls.Add(topBar);
            this.Controls.Add(heroCard);
        }

        private void CheckIfJustUpdated()
        {
            if (File.Exists(updatedFlagPath))
            {
                try
                {
                    string installedVersion = File.ReadAllText(updatedFlagPath).Trim();
                    File.Delete(updatedFlagPath);
                    MessageBox.Show(
                        $"You are updated to version {installedVersion}!",
                        "Project OLDR",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch
                {
                }
            }
        }

        private void ApplyThemeColors()
        {
            this.BackColor = MainUi.BgColor;
            topBar.BackColor = MainUi.SurfaceColor;
            heroCard.BackColor = MainUi.SurfaceColor;
            playersBadge.BackColor = MainUi.SurfaceHighlight;

            gameTitle.ForeColor = MainUi.TextPrimary;
            gameInfo.ForeColor = MainUi.TextSecondary;
            onlinePlayersLabel.ForeColor = MainUi.TextPrimary;

            themeBtn.BackColor = MainUi.SurfaceHighlight;
            friendsBtn.BackColor = MainUi.SurfaceHighlight;
            updateBtn.BackColor = MainUi.SurfaceHighlight;

            playBtn.BackColor = MainUi.Accent;

            this.Invalidate(true);
        }

        private void InitializePaths()
        {
            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectOLDR");
            documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            gamePath = Path.Combine(appDataPath, "Secret Neighbour.exe");
            rarPath = Path.Combine(documentsPath, "game.rar");
            updatedFlagPath = Path.Combine(appDataPath, "just_updated.dat");

            Directory.CreateDirectory(appDataPath);
        }

        private async Task InitializeDashboardAsync()
        {
            await SupabaseApi.UpdatePresenceAsync(currentUsername);
            await RefreshDashboardStatsAsync();
            CheckAndExtractGame();

            refreshTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            refreshTimer.Tick += async (s, e) =>
            {
                await SupabaseApi.UpdatePresenceAsync(currentUsername);
                await RefreshDashboardStatsAsync();
            };
            refreshTimer.Start();
        }

        private async Task RefreshDashboardStatsAsync()
        {
            int count = await SupabaseApi.GetOnlinePlayersCountAsync();
            onlinePlayersLabel.Text = $"Players in launcher: {count}";

            var res = await SupabaseApi.CheckDatabaseHealthAsync();
            if (res.Success)
            {
                dbStatusLabel.Text = "● " + res.Message;
                dbStatusLabel.ForeColor = MainUi.Accent;
            }
            else
            {
                dbStatusLabel.Text = "● " + res.Message;
                dbStatusLabel.ForeColor = MainUi.Danger;
            }
        }

        private async Task CheckUpdatesManualAsync()
        {
            updateBtn.Enabled = false;
            updateBtn.Text = "Checking...";

            var res = await SupabaseApi.CheckGitHubUpdateAsync(CurrentLauncherVersion);
            if (!res.Success)
            {
                MessageBox.Show(res.Message, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                updateBtn.Text = "Check Updates";
                updateBtn.Enabled = true;
                return;
            }

            try
            {
                JObject obj = JObject.Parse(res.Data);
                string tag = ((string)obj["tag"] ?? "").Trim();
                string downloadUrl = (string)obj["url"];

                string cleanTag = tag.TrimStart('v', 'V').Trim();
                string cleanCurrent = CurrentLauncherVersion.TrimStart('v', 'V').Trim();

                bool isNewer = false;
                if (Version.TryParse(cleanTag, out Version remoteVer) && Version.TryParse(cleanCurrent, out Version localVer))
                {
                    isNewer = remoteVer > localVer;
                }

                if (!isNewer || string.IsNullOrEmpty(downloadUrl))
                {
                    MessageBox.Show("You are fully updated.", "Project OLDR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    updateBtn.Text = "Check Updates";
                    updateBtn.Enabled = true;
                    return;
                }

                var choice = MessageBox.Show(
                    $"A new update ({tag}) is available!\n\nDo you want to install it now?",
                    "Update Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (choice == DialogResult.Yes)
                {
                    PerformInAppUpdate(downloadUrl, tag);
                    return;
                }
            }
            catch
            {
                MessageBox.Show("Could not verify remote release version.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            updateBtn.Text = "Check Updates";
            updateBtn.Enabled = true;
        }

        private void PerformInAppUpdate(string downloadUrl, string newTag)
        {
            string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectOLDRUpdater.exe");
            string currentExePath = Environment.ProcessPath;

            if (!File.Exists(updaterPath))
            {
                MessageBox.Show("ProjectOLDRUpdater.exe was not found in the application directory.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                updateBtn.Enabled = true;
                updateBtn.Text = "Check Updates";
                return;
            }

            try
            {
                File.WriteAllText(updatedFlagPath, newTag);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"\"{downloadUrl}\" \"{currentExePath}\"",
                    UseShellExecute = true
                };

                Process.Start(psi);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not launch updater: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                updateBtn.Enabled = true;
                updateBtn.Text = "Check Updates";
            }
        }

        private async Task SignOutAsync()
        {
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Dispose();
            }

            await SupabaseApi.RemovePresenceAsync(currentUsername);
            Environment.Exit(0);
        }

        private async void GameLauncherForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (themeListener != null)
                MainUi.OnThemeChanged -= themeListener;

            if (rainbowListener != null)
                MainUi.OnRainbowTick -= rainbowListener;

            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Dispose();
            }

            try
            {
                await SupabaseApi.RemovePresenceAsync(currentUsername);
            }
            catch
            {
            }

            Environment.Exit(0);
        }

        private void CheckAndExtractGame()
        {
            if (File.Exists(gamePath))
            {
                statusLabel.Text = "Binaries ready to launch.";
                statusLabel.ForeColor = MainUi.Accent;
                playBtn.Enabled = true;
                return;
            }

            if (!File.Exists(rarPath))
            {
                statusLabel.Text = "Place game.rar in your Documents folder.";
                statusLabel.ForeColor = MainUi.Danger;
                return;
            }

            StartExtraction();
        }

        private void StartExtraction()
        {
            try
            {
                statusLabel.Text = "Extracting client files...";
                statusLabel.ForeColor = MainUi.Warning;
                playBtn.Enabled = false;

                progressBar.Visible = true;
                progressLabel.Visible = true;
                progressBar.Value = 0;
                progressLabel.Text = "0%";

                ExtractArchive(rarPath, appDataPath);

                if (File.Exists(gamePath))
                {
                    statusLabel.Text = "Extraction finished.";
                    statusLabel.ForeColor = MainUi.Accent;
                    playBtn.Enabled = true;
                    progressLabel.Text = "100%";

                    try { File.Delete(rarPath); } catch { }

                    var t = new System.Windows.Forms.Timer { Interval = 1000 };
                    t.Tick += (s, e) =>
                    {
                        t.Stop();
                        LaunchGame();
                    };
                    t.Start();
                }
                else
                {
                    statusLabel.Text = "Executable missing after extraction.";
                    statusLabel.ForeColor = MainUi.Danger;
                }
            }
            catch
            {
                statusLabel.Text = "Extraction error.";
                statusLabel.ForeColor = MainUi.Danger;
            }
        }

        private void ExtractArchive(string rarFile, string destDir)
        {
            string winrar = LocateUnpackBinary();
            if (string.IsNullOrEmpty(winrar))
                throw new Exception("WinRAR or UnRAR was not found on this machine.");

            string arguments = $"x -y -pSN2020MADEBYNEXOZANDRYZE \"{rarFile}\" \"{destDir}\\\"";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = winrar,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process proc = Process.Start(psi);
            if (proc == null) throw new Exception("Failed to start unpacking process.");

            DateTime start = DateTime.Now;
            int lastProgress = 0;

            while (!proc.HasExited)
            {
                int sec = (int)(DateTime.Now - start).TotalSeconds;
                int progress = Math.Min(95, (int)(sec * 0.5));

                if (progress > lastProgress)
                {
                    lastProgress = progress;
                    progressBar.Value = progress;
                    progressLabel.Text = $"{progress}%";
                }

                if (Directory.Exists(destDir) && Directory.GetFiles(destDir, "*.exe", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    progressBar.Value = 95;
                    progressLabel.Text = "95%";
                }

                Application.DoEvents();
                System.Threading.Thread.Sleep(200);
            }

            proc.WaitForExit();

            if (proc.ExitCode != 0 && proc.ExitCode != 1)
            {
                string err = proc.StandardError.ReadToEnd();
                throw new Exception($"Unpack code {proc.ExitCode}: {err}");
            }

            progressBar.Value = 100;
            progressLabel.Text = "100%";
            Application.DoEvents();
        }

        private string LocateUnpackBinary()
        {
            string[] paths = new string[]
            {
                @"C:\Program Files\WinRAR\WinRAR.exe",
                @"C:\Program Files (x86)\WinRAR\WinRAR.exe",
                @"C:\Program Files\UnRAR\UnRAR.exe",
                @"C:\Program Files (x86)\UnRAR\UnRAR.exe",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UnRAR.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "UnRAR.exe")
            };

            foreach (string p in paths)
                if (File.Exists(p)) return p;

            return "";
        }

        private void LaunchGame()
        {
            if (!File.Exists(gamePath))
            {
                statusLabel.Text = "Executable missing.";
                statusLabel.ForeColor = MainUi.Danger;
                playBtn.Enabled = true;
                return;
            }

            try
            {
                statusLabel.Text = "Starting game...";
                statusLabel.ForeColor = MainUi.Warning;
                playBtn.Enabled = false;

                Process.Start(gamePath);

                statusLabel.Text = "Game running.";
                statusLabel.ForeColor = MainUi.Accent;
                playBtn.Enabled = true;
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Launch failed.";
                statusLabel.ForeColor = MainUi.Danger;
                playBtn.Enabled = true;
            }
        }
    }
}
