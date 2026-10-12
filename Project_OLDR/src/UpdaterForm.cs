using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public class UpdaterForm : Form
    {
        private string targetVersion;
        private string downloadUrl;
        private bool isMandatory;
        private int remainingDays;

        private Label titleLabel;
        private Label detailsLabel;
        private Label statusLabel;
        private ProgressBar downloadBar;
        private Button updateButton;
        private Button laterButton;

        public UpdaterForm(string targetVersion, string downloadUrl, bool isMandatory, int remainingDays)
        {
            this.targetVersion = targetVersion;
            this.downloadUrl = downloadUrl;
            this.isMandatory = isMandatory;
            this.remainingDays = remainingDays;

            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "Project OLDR - Update Manager";
            this.Size = new Size(540, 310);
            this.BackColor = MainUi.BgColor;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;

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

            this.Controls.Add(MainUi.CreateTitleBar(this, "Client Update Available"));

            Panel card = MainUi.CreateContainer(20, 54, 500, 236);

            titleLabel = new Label
            {
                Text = isMandatory ? "MANDATORY UPDATE REQUIRED" : "NEW UPDATE AVAILABLE",
                Font = MainUi.HeaderMedium,
                ForeColor = isMandatory ? MainUi.Danger : MainUi.Warning,
                Location = new Point(24, 20),
                AutoSize = true
            };

            string subtitle = isMandatory
                ? $"Release {targetVersion} is required to continue playing. The grace period has ended."
                : $"Release {targetVersion} is ready to install ({remainingDays} day(s) before mandatory update).";

            detailsLabel = new Label
            {
                Text = subtitle,
                Font = MainUi.Regular,
                ForeColor = MainUi.TextSecondary,
                Location = new Point(24, 48),
                Size = new Size(452, 40)
            };

            downloadBar = new ProgressBar
            {
                Location = new Point(24, 100),
                Size = new Size(452, 16),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            statusLabel = new Label
            {
                Text = "Ready to download and replace executable files.",
                Font = MainUi.Small,
                ForeColor = MainUi.TextMuted,
                Location = new Point(24, 126),
                Size = new Size(452, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            updateButton = MainUi.CreateStyledButton("Update Now", 24, 166, 160, 44, MainUi.Accent, MainUi.AccentHover);
            updateButton.Click += async (s, e) => await StartInstallationAsync();

            laterButton = MainUi.CreateStyledButton(isMandatory ? "Exit Client" : "Later", 196, 166, 120, 44, MainUi.SurfaceHighlight, MainUi.BorderColor);
            laterButton.Click += (s, e) =>
            {
                if (isMandatory)
                    Application.Exit();
                else
                    this.Close();
            };

            card.Controls.Add(titleLabel);
            card.Controls.Add(detailsLabel);
            card.Controls.Add(downloadBar);
            card.Controls.Add(statusLabel);
            card.Controls.Add(updateButton);
            card.Controls.Add(laterButton);

            this.Controls.Add(card);
        }

        private async Task StartInstallationAsync()
        {
            updateButton.Enabled = false;
            laterButton.Enabled = false;
            downloadBar.Visible = true;
            downloadBar.Value = 20;

            statusLabel.Text = "Downloading updated client binary...";
            statusLabel.ForeColor = MainUi.Warning;

            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            string tempDownloadedFile = Path.Combine(Path.GetTempPath(), "ProjectOLDR_new.exe");
            string updaterScript = Path.Combine(Path.GetTempPath(), "oldr_updater.bat");
            string deadlineFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectOLDR", "update_deadline.dat");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "ProjectOLDR-AutoUpdater");
                    byte[] data = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(tempDownloadedFile, data);
                }

                downloadBar.Value = 80;
                statusLabel.Text = "Deploying replacement script...";

                string batchContent = $@"@echo off
timeout /t 2 /nobreak > nul
:retry
del ""{currentExePath}"" > nul 2>&1
if exist ""{currentExePath}"" (
    timeout /t 1 /nobreak > nul
    goto retry
)
move /y ""{tempDownloadedFile}"" ""{currentExePath}""
start """" ""{currentExePath}""
del ""%~f0""
exit
";
                await File.WriteAllTextAsync(updaterScript, batchContent);

                if (File.Exists(deadlineFilePath)) File.Delete(deadlineFilePath);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = updaterScript,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);

                Application.Exit();
            }
            catch (Exception ex)
            {
                downloadBar.Visible = false;
                statusLabel.Text = "Download failed.";
                statusLabel.ForeColor = MainUi.Danger;
                updateButton.Enabled = true;
                laterButton.Enabled = true;
                MessageBox.Show(ex.Message, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
