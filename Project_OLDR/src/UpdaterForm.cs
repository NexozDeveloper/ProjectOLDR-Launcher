using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public class UpdaterForm : Form
    {
        private readonly UpdateInfo info;
        private bool installing;

        private Label titleLabel;
        private Label detailsLabel;
        private Label statusLabel;
        private ProgressBar downloadBar;
        private Button updateButton;
        private Button laterButton;

        public UpdaterForm(UpdateInfo info)
        {
            this.info = info;
            BuildUI();

            this.FormClosing += (s, e) =>
            {
                if (info.IsMandatory && !installing)
                    Environment.Exit(0);
            };
        }

        private void BuildUI()
        {
            MainUi.SetupForm(this, "Project OLDR - Update Manager", new Size(600, 400));
            this.Controls.Add(MainUi.CreateTitleBar(this, "Client Update Available"));

            Panel card = MainUi.CreateContainer(20, 68, 560, 272);

            titleLabel = new Label
            {
                Text = info.IsMandatory ? "MANDATORY UPDATE REQUIRED" : "NEW UPDATE AVAILABLE",
                Font = MainUi.HeaderMedium,
                ForeColor = info.IsMandatory ? MainUi.Danger : MainUi.Warning,
                Location = new Point(24, 20),
                AutoSize = true
            };

            string subtitle = info.IsMandatory
                ? $"Release {info.Tag} is required to continue playing. The grace period has ended."
                : $"Release {info.Tag} is ready to install ({info.RemainingDays} day(s) before it becomes mandatory).";

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
                Text = "Ready to download and replace the launcher.",
                Font = MainUi.Small,
                ForeColor = MainUi.TextMuted,
                Location = new Point(24, 126),
                Size = new Size(452, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            updateButton = MainUi.CreateStyledButton("Update Now", 24, 166, 160, 44, MainUi.Accent, MainUi.AccentHover);
            updateButton.Click += async (s, e) => await StartInstallationAsync();

            laterButton = MainUi.CreateStyledButton(info.IsMandatory ? "Exit Client" : "Later", 196, 166, 120, 44, MainUi.SurfaceHighlight, MainUi.BorderColor);
            laterButton.Click += (s, e) =>
            {
                if (info.IsMandatory)
                    Environment.Exit(0);
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
            installing = true;
            updateButton.Enabled = false;
            laterButton.Enabled = false;
            downloadBar.Value = 0;
            downloadBar.Visible = true;
            statusLabel.ForeColor = MainUi.Warning;

            Progress<int> progress = new Progress<int>(value =>
            {
                downloadBar.Value = Math.Max(0, Math.Min(100, value));
            });

            Progress<string> status = new Progress<string>(text =>
            {
                statusLabel.Text = text;
            });

            try
            {
                await Updater.ApplyAsync(info, progress, status);
            }
            catch (Exception ex)
            {
                installing = false;
                downloadBar.Visible = false;
                statusLabel.Text = "Update failed.";
                statusLabel.ForeColor = MainUi.Danger;
                updateButton.Enabled = true;
                laterButton.Enabled = true;
                MessageBox.Show(ex.Message, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
