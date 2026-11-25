using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public sealed class VersionEditorForm : Form
    {
        private readonly TextBox nameBox;
        private readonly TextBox tagBox;
        private readonly TextBox urlBox;
        private readonly TextBox fileBox;
        private readonly TextBox passwordBox;
        private readonly TextBox notesBox;
        private readonly ComboBox colorBox;
        private readonly NumericUpDown orderInput;
        private readonly CheckBox activeCheck;
        private readonly Label statusLabel;
        private readonly string editingId;
        private readonly Dictionary<string, string> colors = new Dictionary<string, string>
        {
            { "White", "#FFFFFF" },
            { "Light gray", "#D6D6D6" },
            { "Gray", "#A3A3A3" },
            { "Dim gray", "#7A7A7A" }
        };

        public GameVersion Result { get; private set; }

        public VersionEditorForm(GameVersion existing)
        {
            editingId = existing == null ? "" : existing.Id;
            MainUi.SetupForm(this, "Version Editor", new Size(680, 790));
            Controls.Add(MainUi.CreateTitleBar(this, existing == null ? "Add a version" : "Edit version"));

            Panel card = MainUi.CreateContainer(24, 68, 632, 670);
            Controls.Add(card);

            card.Controls.Add(new IconBox(FluentIcon.Library, 28, MainUi.Accent) { Location = new Point(28, 22) });
            card.Controls.Add(MainUi.CreateLabel(existing == null ? "New version" : "Edit version", 68, 20, 368, 30, MainUi.HeaderMedium, MainUi.TextPrimary));
            card.Controls.Add(MainUi.CreateLabel("Define how this build appears and where it downloads from.", 28, 58, 560, 22, MainUi.Regular, MainUi.TextMuted));

            card.Controls.Add(MainUi.CreateLabel("DISPLAY NAME", 28, 92, 260, 18, MainUi.Small, MainUi.TextMuted));
            nameBox = MainUi.CreateStyledTextBox(28, 114, 576, 42);
            card.Controls.Add(nameBox);

            card.Controls.Add(MainUi.CreateLabel("VERSION TAG", 28, 168, 240, 18, MainUi.Small, MainUi.TextMuted));
            tagBox = MainUi.CreateStyledTextBox(28, 190, 280, 42);
            card.Controls.Add(tagBox);

            card.Controls.Add(MainUi.CreateLabel("SORT ORDER", 324, 168, 240, 18, MainUi.Small, MainUi.TextMuted));
            orderInput = new NumericUpDown
            {
                Location = new Point(324, 190),
                Size = new Size(280, 42),
                BackColor = MainUi.SurfaceHighlight,
                ForeColor = MainUi.TextPrimary,
                Font = MainUi.Caption,
                BorderStyle = BorderStyle.FixedSingle,
                Minimum = -99,
                Maximum = 999
            };
            card.Controls.Add(orderInput);

            card.Controls.Add(MainUi.CreateLabel("DIRECT DOWNLOAD URL", 28, 246, 360, 18, MainUi.Small, MainUi.TextMuted));
            urlBox = MainUi.CreateStyledTextBox(28, 268, 576, 42);
            card.Controls.Add(urlBox);

            card.Controls.Add(MainUi.CreateLabel("EXECUTABLE FILE NAME (OPTIONAL)", 28, 324, 280, 18, MainUi.Small, MainUi.TextMuted));
            fileBox = MainUi.CreateStyledTextBox(28, 346, 280, 42);
            card.Controls.Add(fileBox);

            card.Controls.Add(MainUi.CreateLabel("ARCHIVE PASSWORD (OPTIONAL)", 324, 324, 280, 18, MainUi.Small, MainUi.TextMuted));
            passwordBox = MainUi.CreateStyledTextBox(324, 346, 280, 42, true);
            card.Controls.Add(passwordBox);

            card.Controls.Add(MainUi.CreateLabel("NOTES", 28, 402, 240, 18, MainUi.Small, MainUi.TextMuted));
            notesBox = MainUi.CreateStyledTextBox(28, 424, 576, 76);
            notesBox.Multiline = true;
            card.Controls.Add(notesBox);

            card.Controls.Add(MainUi.CreateLabel("CARD TONE", 28, 516, 240, 18, MainUi.Small, MainUi.TextMuted));
            colorBox = new ComboBox
            {
                Location = new Point(28, 538),
                Size = new Size(280, 42),
                BackColor = MainUi.SurfaceHighlight,
                ForeColor = MainUi.TextPrimary,
                Font = MainUi.Caption,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (string key in colors.Keys) colorBox.Items.Add(key);
            card.Controls.Add(colorBox);

            activeCheck = new CheckBox
            {
                Location = new Point(324, 538),
                Size = new Size(280, 42),
                Text = "Visible to all players",
                ForeColor = MainUi.TextPrimary,
                BackColor = Color.Transparent,
                Font = MainUi.Caption,
                Checked = true
            };
            card.Controls.Add(activeCheck);

            statusLabel = MainUi.CreateLabel("The link must point directly to an .exe, .zip, .rar or .7z file.", 28, 590, 576, 24, MainUi.Small, MainUi.TextMuted);
            card.Controls.Add(statusLabel);

            Button cancelButton = MainUi.CreateButton("Cancel", null, 448, 626, 156, 42, ButtonKind.Secondary);
            cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            card.Controls.Add(cancelButton);

            Button saveButton = MainUi.CreateButton("Save version", FluentIcon.Check, 28, 626, 404, 42, ButtonKind.Primary);
            saveButton.Click += SaveButton_Click;
            card.Controls.Add(saveButton);

            if (existing != null)
            {
                nameBox.Text = existing.Name;
                tagBox.Text = existing.VersionTag;
                urlBox.Text = existing.DownloadUrl;
                fileBox.Text = existing.FileName;
                passwordBox.Text = existing.ArchivePassword;
                notesBox.Text = existing.Notes;
                orderInput.Value = Math.Max(orderInput.Minimum, Math.Min(orderInput.Maximum, existing.SortOrder));
                activeCheck.Checked = existing.IsActive;
                string matched = "";
                foreach (KeyValuePair<string, string> pair in colors)
                {
                    if (string.Equals(pair.Value, existing.ColorAccent, StringComparison.OrdinalIgnoreCase)) matched = pair.Key;
                }
                colorBox.SelectedItem = string.IsNullOrEmpty(matched) ? "White" : matched;
            }
            else
            {
                colorBox.SelectedIndex = 0;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            string name = nameBox.Text.Trim();
            string url = urlBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                statusLabel.Text = "A display name is required.";
                statusLabel.ForeColor = MainUi.Danger;
                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri parsed) || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            {
                statusLabel.Text = "Enter a valid http or https download URL.";
                statusLabel.ForeColor = MainUi.Danger;
                return;
            }

            string selected = colorBox.SelectedItem == null ? "" : colorBox.SelectedItem.ToString();
            string color = colors.ContainsKey(selected) ? colors[selected] : "#FFFFFF";

            Result = new GameVersion
            {
                Id = editingId,
                Name = name,
                VersionTag = tagBox.Text.Trim(),
                DownloadUrl = url,
                FileName = fileBox.Text.Trim(),
                ArchivePassword = passwordBox.Text,
                Notes = notesBox.Text.Trim(),
                ColorAccent = color,
                IsActive = activeCheck.Checked,
                SortOrder = (int)orderInput.Value
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
