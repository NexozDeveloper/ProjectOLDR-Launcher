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
        private const string CurrentLauncherVersion = "2.0.0.0";

        private Label statusLabel;
        private Label dbStatusLabel;
        private Label onlinePlayersLabel;
        private Label versionLabel;
        private Button playBtn;
        private Button friendsBtn;
        private Button updateBtn;
        private Button logoutBtn;
        private ProgressBar progressBar;
        private Label progressLabel;

        private string gamePath = "";
        private string appDataPath = "";
        private string documentsPath = "";
        private string rarPath = "";
        private string currentUsername;
        private System.Windows.Forms.Timer refreshTimer;

        public GameLauncherForm(string username)
        {
            currentUsername = username;
            BuildUI();
            InitializePaths();

            this.Shown += async (s, e) => await InitializeDashboardAsync();
            this.FormClosed += GameLauncherForm_FormClosed;
        }

        private void BuildUI()
        {
            this.Text = "Project OLDR";
            this.Size = new Size(740, 480);
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

            Panel topBar = MainUi.CreateContainer(20, 56, 700, 64);

            Panel avatar = new Panel
            {
                Location = new Point(16, 12),
                Size = new Size(40, 40),
                BackColor = MainUi.SurfaceHighlight
            };
            avatar.Paint += (s, e) =>
            {
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
                Location = new Point(66, 12),
                AutoSize = true
            };

            dbStatusLabel = new Label
            {
                Text = "● Checking Network...",
                Font = MainUi.Small,
                ForeColor = MainUi.Warning,
                Location = new Point(67, 36),
                AutoSize = true
            };

            Panel playersBadge = MainUi.CreateContainer(240, 14, 205, 36);
            onlinePlayersLabel = new Label
            {
                Text = "Players in launcher: ...",
                Font = MainUi.Small,
                ForeColor = MainUi.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            playersBadge.Controls.Add(onlinePlayersLabel);

            friendsBtn = MainUi.CreateStyledButton("Friends", 460, 14, 110, 36, MainUi.SurfaceHighlight, MainUi.BorderColor);
            friendsBtn.Click += (s, e) =>
            {
                using (var f = new FriendsForm(currentUsername))
                    f.ShowDialog(this);
            };

            logoutBtn = MainUi.CreateStyledButton("Sign Out", 580, 14, 104, 36, Color.FromArgb(40, 20, 25), MainUi.Danger);
            logoutBtn.Click += async (s, e) => await SignOutAsync();

            topBar.Controls.Add(avatar);
            topBar.Controls.Add(userLabel);
            topBar.Controls.Add(dbStatusLabel);
            topBar.Controls.Add(playersBadge);
            topBar.Controls.Add(friendsBtn);
            topBar.Controls.Add(logoutBtn);

            Panel heroCard = MainUi.CreateContainer(20, 132, 700, 326);

            Label gameTitle = new Label
            {
                Text = "SECRET NEIGHBOR",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = MainUi.TextPrimary,
                Location = new Point(28, 24),
                AutoSize = true
            };

            Label gameInfo = new Label
            {
                Text = "Direct Network Play • Build 2020",
                Font = MainUi.Regular,
                ForeColor = MainUi.TextSecondary,
                Location = new Point(30, 64),
                AutoSize = true
            };

            progressBar = new ProgressBar
            {
                Location = new Point(30, 125),
                Size = new Size(640, 14),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            progressLabel = new Label
            {
                Text = "",
                Font = MainUi.Small,
                ForeColor = MainUi.TextMuted,
                Location = new Point(30, 145),
                Size = new Size(640, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            playBtn = MainUi.CreateStyledButton("PLAY GAME", 30, 195, 200, 52, MainUi.Accent, MainUi.AccentHover);
            playBtn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            playBtn.Enabled = false;
            playBtn.Click += (s, e) => LaunchGame();

            updateBtn = MainUi.CreateStyledButton("Check Updates", 244, 201, 160, 42, MainUi.SurfaceHighlight, MainUi.BorderColor);
            updateBtn.Click += async (s, e) => await CheckGitHubUpdateAsync();

            statusLabel = new Label
            {
                Text = "Checking local files...",
                Font = MainUi.Regular,
                ForeColor = MainUi.TextMuted,
                Location = new Point(30, 275),
                Size = new Size(480, 25)
            };

            versionLabel = new Label
            {
                Text = $"v{CurrentLauncherVersion}",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = MainUi.TextMuted,
                Location = new Point(570, 278),
                Size = new Size(100, 20),
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

        private void InitializePaths()
        {
            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectOLDR");
            documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            gamePath = Path.Combine(appDataPath, "Secret Neighbour.exe");
            rarPath = Path.Combine(documentsPath, "game.rar");

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

        private async Task SignOutAsync()
        {
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Dispose();
            }

            await SupabaseApi.RemovePresenceAsync(currentUsername);
            this.Close();
        }

        private async void GameLauncherForm_FormClosed(object sender, FormClosedEventArgs e)
        {
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
        }

        private async Task CheckGitHubUpdateAsync()
        {
            updateBtn.Enabled = false;
            statusLabel.Text = "Checking GitHub releases...";
            statusLabel.ForeColor = MainUi.TextMuted;

            var res = await SupabaseApi.CheckGitHubUpdateAsync(CurrentLauncherVersion);
            if (res.Success)
            {
                try
                {
                    JObject obj = JObject.Parse(res.Data);
                    bool hasUpdate = (bool)obj["has_update"];
                    string tag = (string)obj["tag"];
                    string downloadUrl = (string)obj["url"];

                    if (hasUpdate)
                    {
                        var confirm = MessageBox.Show(
                            $"A new launcher version is available: {tag} (Current: v{CurrentLauncherVersion})\n\nOpen download page?",
                            "Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (confirm == DialogResult.Yes && !string.IsNullOrEmpty(downloadUrl))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = downloadUrl,
                                UseShellExecute = true
                            });
                        }
                        statusLabel.Text = $"Update detected: {tag}";
                        statusLabel.ForeColor = MainUi.Warning;
                    }
                    else
                    {
                        MessageBox.Show("You are fully updated.", "Project OLDR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        statusLabel.Text = "You are fully updated.";
                        statusLabel.ForeColor = MainUi.Accent;
                    }
                }
                catch
                {
                    statusLabel.Text = "Failed to parse update manifest.";
                    statusLabel.ForeColor = MainUi.Danger;
                }
            }
            else
            {
                MessageBox.Show(res.Message, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                statusLabel.Text = "Could not check for updates.";
                statusLabel.ForeColor = MainUi.Danger;
            }

            updateBtn.Enabled = true;
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
                MessageBox.Show(
                    "Place game.rar inside your Documents folder:\n" + rarPath,
                    "Archive Missing",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            catch (Exception ex)
            {
                statusLabel.Text = "Extraction error.";
                statusLabel.ForeColor = MainUi.Danger;
                MessageBox.Show(ex.Message, "Extraction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(ex.Message, "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                playBtn.Enabled = true;
            }
        }
    }
}
