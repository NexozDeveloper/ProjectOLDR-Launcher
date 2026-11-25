using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public class ProfileSettingsForm : Form
    {
        private readonly string username;
        private readonly string password;
        private LauncherProfile profile;
        private ProfileAvatar avatarPreview;
        private TextBox displayNameBox;
        private TextBox bioBox;
        private Label statusLabel;
        private string pendingAvatarData;
        private bool avatarChanged;

        public bool ProfileChanged { get; private set; }

        public ProfileSettingsForm(string username, string password)
        {
            this.username = (username ?? "").Trim();
            this.password = password ?? "";
            BuildUI();

            Shown += async (s, e) => await LoadProfileAsync();
        }

        private void BuildUI()
        {
            MainUi.SetupForm(this, "Project OLDR - Account Settings", new Size(620, 560));
            Controls.Add(MainUi.CreateTitleBar(this, "Account Settings"));

            Panel card = MainUi.CreateContainer(20, 68, 580, 440);

            avatarPreview = new ProfileAvatar(84) { Location = new Point(28, 26) };
            card.Controls.Add(avatarPreview);

            Button changePhoto = MainUi.CreateButton("Change photo", null, 132, 28, 152, 34, ButtonKind.Secondary);
            changePhoto.Click += ChangePhoto_Click;
            card.Controls.Add(changePhoto);

            Button removePhoto = MainUi.CreateButton("Remove photo", null, 132, 70, 152, 32, ButtonKind.Subtle);
            removePhoto.Click += (s, e) =>
            {
                pendingAvatarData = "";
                avatarChanged = true;
                avatarPreview.SetInitial(profile == null ? username : profile.Initial);
                SetStatus("Photo removed. Save to apply.", MainUi.TextMuted);
            };
            card.Controls.Add(removePhoto);

            card.Controls.Add(MainUi.CreateLabel("PNG or JPG. Your photo is cropped to a square and shown to friends.",
                300, 30, 252, 46, MainUi.Small, MainUi.TextMuted));

            card.Controls.Add(MainUi.CreateLabel("DISPLAY NAME", 28, 128, 240, 18, MainUi.Small, MainUi.TextMuted));
            displayNameBox = MainUi.CreateStyledTextBox(28, 148, 524, 40);
            displayNameBox.MaxLength = 32;
            card.Controls.Add(displayNameBox);

            card.Controls.Add(MainUi.CreateLabel("BIO", 28, 204, 240, 18, MainUi.Small, MainUi.TextMuted));
            bioBox = MainUi.CreateStyledTextBox(28, 224, 524, 84);
            bioBox.Multiline = true;
            bioBox.AcceptsReturn = true;
            bioBox.WordWrap = true;
            bioBox.ScrollBars = ScrollBars.Vertical;
            bioBox.MaxLength = 160;
            card.Controls.Add(bioBox);

            statusLabel = MainUi.CreateLabel("Loading profile...", 28, 320, 524, 42, MainUi.Caption, MainUi.TextMuted);
            card.Controls.Add(statusLabel);

            Button saveButton = MainUi.CreateButton("Save changes", FluentIcon.Check, 28, 376, 250, 42, ButtonKind.Primary);
            saveButton.Click += async (s, e) => await Save_ClickAsync();
            card.Controls.Add(saveButton);

            Button cancelButton = MainUi.CreateButton("Cancel", null, 294, 376, 258, 42, ButtonKind.Secondary);
            cancelButton.Click += (s, e) => Close();
            card.Controls.Add(cancelButton);

            card.Controls.Add(MainUi.CreateLabel("Your display name, bio and photo are visible to accepted friends.",
                28, 430, 524, 20, MainUi.Small, MainUi.TextMuted));

            Controls.Add(card);
        }

        private async Task LoadProfileAsync()
        {
            profile = await ProfileStore.GetProfileAsync(username);
            if (profile == null) profile = new LauncherProfile { Username = username };
            if (string.IsNullOrWhiteSpace(profile.Username)) profile.Username = username;

            pendingAvatarData = profile.AvatarData;
            displayNameBox.Text = profile.DisplayName ?? "";
            bioBox.Text = profile.Bio ?? "";
            avatarPreview.SetProfile(profile);
            SetStatus("Edit your public card.", MainUi.TextMuted);
        }

        private void ChangePhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose a profile photo";
                dialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*";
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    FileInfo info = new FileInfo(dialog.FileName);
                    if (info.Length > 8L * 1024L * 1024L)
                    {
                        SetStatus("Choose an image smaller than 8 MB.", MainUi.Danger);
                        return;
                    }

                    pendingAvatarData = CreateAvatarData(dialog.FileName);
                    avatarChanged = true;
                    avatarPreview.SetImageData(pendingAvatarData);
                    SetStatus("Photo ready. Save to apply.", MainUi.TextMuted);
                }
                catch (Exception ex)
                {
                    SetStatus("Could not load that image: " + ex.Message, MainUi.Danger);
                }
            }
        }

        private static string CreateAvatarData(string path)
        {
            using (Image source = Image.FromFile(path))
            {
                if (source.Width > 8000 || source.Height > 8000)
                    throw new InvalidOperationException("Image dimensions are too large.");

                int side = Math.Min(source.Width, source.Height);
                Rectangle crop = new Rectangle((source.Width - side) / 2, (source.Height - side) / 2, side, side);

                using (Bitmap square = new Bitmap(192, 192, PixelFormat.Format32bppArgb))
                using (Graphics g = Graphics.FromImage(square))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.DrawImage(source, new Rectangle(0, 0, 192, 192), crop, GraphicsUnit.Pixel);

                    using (MemoryStream stream = new MemoryStream())
                    {
                        square.Save(stream, ImageFormat.Png);
                        byte[] bytes = stream.ToArray();
                        if (bytes.Length > 512L * 1024L)
                            throw new InvalidOperationException("Image is too large after processing.");
                        return "data:image/png;base64," + Convert.ToBase64String(bytes);
                    }
                }
            }
        }

        private async Task Save_ClickAsync()
        {
            string displayName = displayNameBox.Text.Trim();
            string bio = bioBox.Text.Trim();
            if (displayName.Length == 0) displayName = username;
            if (displayName.Length > 32) displayName = displayName.Substring(0, 32);
            if (bio.Length > 160) bio = bio.Substring(0, 160);

            LauncherProfile updated = new LauncherProfile
            {
                Username = username,
                DisplayName = displayName,
                Bio = bio,
                AvatarData = avatarChanged ? pendingAvatarData : (profile == null ? "" : profile.AvatarData)
            };

            SetStatus("Saving profile...", MainUi.TextMuted);
            ProfileSaveResult result = await ProfileStore.SaveAsync(updated, password);
            if (!result.Success)
            {
                SetStatus(result.Message, MainUi.Danger);
                return;
            }

            ProfileChanged = true;
            profile = updated;
            SetStatus(result.Message, result.Synced ? MainUi.Accent : MainUi.Warning);
            if (result.Synced)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void SetStatus(string text, Color color)
        {
            if (statusLabel == null || statusLabel.IsDisposed) return;
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }
    }
}
