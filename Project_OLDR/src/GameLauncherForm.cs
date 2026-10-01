using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ProjectOLDR
{
    public class GameLauncherForm : Form
    {
        private Label statusLabel;
        private Button playBtn;
        private Button logoutBtn;
        private ProgressBar progressBar;
        private Label progressLabel;
        private string gamePath;
        private string appDataPath;
        private string documentsPath;
        private string rarPath;
        private string currentUsername;

        public GameLauncherForm(string username)
        {
            statusLabel = new Label();
            playBtn = new Button();
            logoutBtn = new Button();
            progressBar = new ProgressBar();
            progressLabel = new Label();
            gamePath = "";
            appDataPath = "";
            documentsPath = "";
            rarPath = "";
            currentUsername = username;

            BuildUI();
            CheckAndExtractGame();
        }

        private void BuildUI()
        {
            this.Text = "Project OLDR";
            this.Size = new Size(500, 420);
            this.BackColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = null;
            this.FormClosed += GameLauncherForm_FormClosed;

            Label title = new Label();
            title.Text = "ProjectOLDR Launcher\n Secret Neigbor";
            title.ForeColor = Color.White;
            title.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            title.Size = new Size(480, 80);
            title.Location = new Point(10, 10);
            title.TextAlign = ContentAlignment.MiddleCenter;

            Label usernameDisplay = new Label();
            usernameDisplay.Text = $"Logged as: {currentUsername}";
            usernameDisplay.ForeColor = Color.Gray;
            usernameDisplay.Font = new Font("Segoe UI", 9);
            usernameDisplay.Size = new Size(200, 20);
            usernameDisplay.Location = new Point(10, 85);
            usernameDisplay.TextAlign = ContentAlignment.MiddleLeft;

            logoutBtn.Text = "Logout";
            logoutBtn.ForeColor = Color.White;
            logoutBtn.BackColor = Color.FromArgb(60, 60, 60);
            logoutBtn.Font = new Font("Segoe UI", 9);
            logoutBtn.Size = new Size(80, 25);
            logoutBtn.Location = new Point(400, 82);
            logoutBtn.FlatStyle = FlatStyle.Flat;
            logoutBtn.FlatAppearance.BorderSize = 0;
            logoutBtn.Cursor = Cursors.Hand;
            logoutBtn.Click += LogoutBtn_Click;

            progressBar.Size = new Size(400, 30);
            progressBar.Location = new Point(40, 130);
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Visible = false;

            progressLabel.Text = "";
            progressLabel.ForeColor = Color.White;
            progressLabel.Font = new Font("Segoe UI", 10);
            progressLabel.Size = new Size(400, 25);
            progressLabel.Location = new Point(40, 165);
            progressLabel.TextAlign = ContentAlignment.MiddleCenter;
            progressLabel.Visible = false;

            playBtn.Text = "▶ PLAY";
            playBtn.ForeColor = Color.White;
            playBtn.BackColor = Color.FromArgb(0, 150, 0);
            playBtn.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            playBtn.Size = new Size(180, 60);
            playBtn.Location = new Point(150, 210);
            playBtn.FlatStyle = FlatStyle.Flat;
            playBtn.FlatAppearance.BorderSize = 0;
            playBtn.Enabled = false;
            playBtn.Click += PlayBtn_Click;

            statusLabel.Text = "Status: Initializing...";
            statusLabel.ForeColor = Color.Gray;
            statusLabel.Font = new Font("Segoe UI", 10);
            statusLabel.Size = new Size(480, 25);
            statusLabel.Location = new Point(10, 320);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;

            this.Controls.Add(title);
            this.Controls.Add(usernameDisplay);
            this.Controls.Add(logoutBtn);
            this.Controls.Add(progressBar);
            this.Controls.Add(progressLabel);
            this.Controls.Add(playBtn);
            this.Controls.Add(statusLabel);

            this.KeyDown += GameLauncherForm_KeyDown;
            this.KeyPreview = true;
        }

        private void GameLauncherForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                AdminForm admin = new AdminForm(); 
                admin.ShowDialog();
            }
        }

        private void GameLauncherForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void LogoutBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CheckAndExtractGame()
        {
            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectOLDR");
            documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            gamePath = Path.Combine(appDataPath, "Secret Neighbour.exe");
            rarPath = Path.Combine(documentsPath, "game.rar");

            Directory.CreateDirectory(appDataPath);

            if (File.Exists(gamePath))
            {
                statusLabel.Text = "Status: Game ready!";
                statusLabel.ForeColor = Color.LightGreen;
                playBtn.Enabled = true;
                return;
            }

            if (!File.Exists(rarPath))
            {
                statusLabel.Text = "Status: game.rar not found in Documents!";
                statusLabel.ForeColor = Color.Red;
                MessageBox.Show(
                    "game.rar not found!\n\nExpected location:\n" + rarPath + "\n\nPlease place the game.rar file in your Documents folder.",
                    "Project OLDR - Error",
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
                statusLabel.Text = "Status: Extracting game files...";
                statusLabel.ForeColor = Color.Yellow;
                playBtn.Enabled = false;

                progressBar.Visible = true;
                progressLabel.Visible = true;
                progressBar.Value = 0;
                progressLabel.Text = "Extracting... 0%";

                ExtractRarWithProgress(rarPath, appDataPath);

                if (File.Exists(gamePath))
                {
                    statusLabel.Text = "Status: Extraction complete!";
                    statusLabel.ForeColor = Color.LightGreen;
                    playBtn.Enabled = true;
                    progressLabel.Text = "Extraction complete! 100%";

                    try { File.Delete(rarPath); } catch { }

                    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                    timer.Interval = 1000;
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        LaunchGame();
                    };
                    timer.Start();
                }
                else
                {
                    statusLabel.Text = "Status: Extraction failed!";
                    statusLabel.ForeColor = Color.Red;
                    progressLabel.Text = "Extraction failed!";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Status: Extraction failed!";
                statusLabel.ForeColor = Color.Red;
                MessageBox.Show($"Error extracting game:\n{ex.Message}", "Project OLDR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExtractRarWithProgress(string rarPath, string outputDir)
        {
            string winrarPath = FindWinRAR();

            if (string.IsNullOrEmpty(winrarPath))
                throw new Exception("WinRAR or UnRAR not found! Please install WinRAR.");

            string args = $"x -y -pHideforpublicrepo \"{rarPath}\" \"{outputDir}\\\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = winrarPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process process = Process.Start(startInfo);
            if (process == null) throw new Exception("Failed to start WinRAR process.");

            DateTime startTime = DateTime.Now;
            int lastProgress = 0;

            while (!process.HasExited)
            {
                int elapsed = (int)(DateTime.Now - startTime).TotalSeconds;
                int progress = Math.Min(95, (int)(elapsed * 0.5));

                if (progress > lastProgress)
                {
                    lastProgress = progress;
                    progressBar.Value = progress;
                    progressLabel.Text = $"Extracting... {progress}%";
                }

                if (Directory.Exists(outputDir) && Directory.GetFiles(outputDir, "*.exe", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    progressBar.Value = 95;
                    progressLabel.Text = "Extracting... 95%";
                }

                Application.DoEvents();
                System.Threading.Thread.Sleep(200);
            }

            process.WaitForExit();

            if (process.ExitCode != 0 && process.ExitCode != 1)
            {
                string error = process.StandardError.ReadToEnd();
                if (string.IsNullOrEmpty(error)) error = "Unknown error occurred.";
                throw new Exception($"Extraction failed (code {process.ExitCode}): {error}");
            }

            progressBar.Value = 100;
            progressLabel.Text = "Extraction complete! 100%";
            Application.DoEvents();
        }

        private string FindWinRAR()
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

            foreach (string path in paths)
                if (File.Exists(path)) return path;

            return "";
        }

        private void LaunchGame()
        {
            if (!File.Exists(gamePath))
            {
                statusLabel.Text = "Status: Game not found!";
                statusLabel.ForeColor = Color.Red;
                playBtn.Enabled = true;
                return;
            }

            try
            {
                statusLabel.Text = "Status: Launching game...";
                statusLabel.ForeColor = Color.Yellow;
                playBtn.Enabled = false;

                Process.Start(gamePath);

                statusLabel.Text = "Status: Game launched!";
                statusLabel.ForeColor = Color.LightGreen;
                playBtn.Enabled = true;
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Status: Launch failed";
                statusLabel.ForeColor = Color.Red;
                MessageBox.Show($"Error launching game:\n{ex.Message}", "Project OLDR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                playBtn.Enabled = true;
            }
        }

        private void PlayBtn_Click(object sender, EventArgs e)
        {
            LaunchGame();
        }
    }
}
