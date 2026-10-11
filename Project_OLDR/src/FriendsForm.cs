using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public class FriendsForm : Form
    {
        private string currentUsername;
        private ListBox friendsListBox;
        private ListBox requestsListBox;
        private TextBox addFriendBox;
        private Button addFriendBtn;
        private Button acceptBtn;
        private Button declineBtn;
        private Label statusLabel;
        private System.Windows.Forms.Timer loopTimer;

        public FriendsForm(string username)
        {
            currentUsername = username;
            BuildUI();

            this.Shown += async (s, e) =>
            {
                await RefreshAllAsync();
                loopTimer = new System.Windows.Forms.Timer { Interval = 5000 };
                loopTimer.Tick += async (ts, te) => await RefreshAllAsync();
                loopTimer.Start();
            };
        }

        private void BuildUI()
        {
            this.Text = "Social Hub";
            this.Size = new Size(420, 560);
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

            this.Controls.Add(MainUi.CreateTitleBar(this, "Friends Hub"));

            Panel card = MainUi.CreateContainer(20, 54, 380, 486);

            Label friendsTitle = new Label
            {
                Text = "MY FRIENDS",
                Font = MainUi.HeaderMedium,
                ForeColor = MainUi.TextPrimary,
                Location = new Point(16, 14),
                AutoSize = true
            };

            friendsListBox = new ListBox
            {
                Location = new Point(16, 42),
                Size = new Size(348, 200),
                BackColor = MainUi.SurfaceHighlight,
                ForeColor = MainUi.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = MainUi.Regular,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 36
            };
            friendsListBox.DrawItem += FriendsListBox_DrawItem;

            Label reqTitle = new Label
            {
                Text = "PENDING REQUESTS",
                Font = MainUi.Small,
                ForeColor = MainUi.TextSecondary,
                Location = new Point(16, 254),
                AutoSize = true
            };

            requestsListBox = new ListBox
            {
                Location = new Point(16, 276),
                Size = new Size(180, 76),
                BackColor = MainUi.SurfaceHighlight,
                ForeColor = MainUi.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = MainUi.Regular
            };

            acceptBtn = MainUi.CreateStyledButton("Accept", 204, 276, 76, 76, MainUi.Accent, MainUi.AccentHover);
            acceptBtn.Click += async (s, e) => await AcceptSelectedRequestAsync();

            declineBtn = MainUi.CreateStyledButton("Decline", 286, 276, 76, 76, Color.FromArgb(40, 22, 28), MainUi.Danger);
            declineBtn.Click += async (s, e) => await DeclineSelectedRequestAsync();

            Label addTitle = new Label
            {
                Text = "SEND FRIEND INVITATION",
                Font = MainUi.Small,
                ForeColor = MainUi.TextSecondary,
                Location = new Point(16, 366),
                AutoSize = true
            };

            addFriendBox = MainUi.CreateStyledTextBox(16, 388, 244, 32);
            addFriendBtn = MainUi.CreateStyledButton("Add", 268, 387, 96, 34, Color.FromArgb(35, 42, 60), MainUi.BorderColor);
            addFriendBtn.Click += async (s, e) => await AddFriendClickAsync();

            statusLabel = new Label
            {
                Text = "Synchronizing contacts...",
                Font = MainUi.Small,
                ForeColor = MainUi.TextMuted,
                Location = new Point(16, 436),
                Size = new Size(348, 36),
                TextAlign = ContentAlignment.TopCenter
            };

            card.Controls.Add(friendsTitle);
            card.Controls.Add(friendsListBox);
            card.Controls.Add(reqTitle);
            card.Controls.Add(requestsListBox);
            card.Controls.Add(acceptBtn);
            card.Controls.Add(declineBtn);
            card.Controls.Add(addTitle);
            card.Controls.Add(addFriendBox);
            card.Controls.Add(addFriendBtn);
            card.Controls.Add(statusLabel);

            this.Controls.Add(card);

            this.FormClosed += (s, e) =>
            {
                if (loopTimer != null)
                {
                    loopTimer.Stop();
                    loopTimer.Dispose();
                }
            };
        }

        private void FriendsListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bg = sel ? MainUi.BorderColor : MainUi.SurfaceHighlight;
            using (SolidBrush b = new SolidBrush(bg))
                e.Graphics.FillRectangle(b, e.Bounds);

            string item = friendsListBox.Items[e.Index].ToString();
            bool isOnline = item.StartsWith("[ON]");
            string cleanName = item.Substring(4).Trim();

            Color dotColor = isOnline ? MainUi.Accent : MainUi.TextMuted;
            using (SolidBrush dot = new SolidBrush(dotColor))
                e.Graphics.FillEllipse(dot, e.Bounds.X + 12, e.Bounds.Y + 13, 10, 10);

            using (SolidBrush t = new SolidBrush(MainUi.TextPrimary))
                e.Graphics.DrawString(cleanName, MainUi.HeaderMedium, t, e.Bounds.X + 32, e.Bounds.Y + 7);

            using (SolidBrush st = new SolidBrush(isOnline ? MainUi.Accent : MainUi.TextMuted))
            {
                string statusText = isOnline ? "ONLINE" : "OFFLINE";
                e.Graphics.DrawString(statusText, MainUi.Small, st, e.Bounds.Right - 70, e.Bounds.Y + 9);
            }
        }

        private async Task RefreshAllAsync()
        {
            await SupabaseApi.UpdatePresenceAsync(currentUsername);

            var res = await SupabaseApi.GetFriendsDataAsync(currentUsername);
            if (res.Success && !string.IsNullOrEmpty(res.Data))
            {
                try
                {
                    JArray arr = JArray.Parse(res.Data);

                    friendsListBox.Items.Clear();
                    requestsListBox.Items.Clear();

                    int onlineCount = 0;
                    foreach (var row in arr)
                    {
                        string s = (string)row["sender"];
                        string r = (string)row["receiver"];
                        string status = (string)row["status"];

                        if (status == "accepted")
                        {
                            string friendName = s.Equals(currentUsername, StringComparison.OrdinalIgnoreCase) ? r : s;
                            bool online = await SupabaseApi.IsUserOnlineAsync(friendName);
                            if (online) onlineCount++;
                            string prefix = online ? "[ON]" : "[OF]";
                            friendsListBox.Items.Add($"{prefix} {friendName}");
                        }
                        else if (status == "pending" && r.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
                        {
                            requestsListBox.Items.Add(s);
                        }
                    }

                    statusLabel.Text = $"{friendsListBox.Items.Count} friends • {onlineCount} online";
                    statusLabel.ForeColor = MainUi.TextMuted;
                }
                catch
                {
                }
            }
        }

        private async Task AcceptSelectedRequestAsync()
        {
            if (requestsListBox.SelectedIndex < 0)
            {
                statusLabel.Text = "Select a pending request first.";
                statusLabel.ForeColor = MainUi.Warning;
                return;
            }

            string target = requestsListBox.SelectedItem.ToString();
            acceptBtn.Enabled = false;

            var res = await SupabaseApi.AcceptFriendAsync(currentUsername, target);
            if (res.Success)
            {
                statusLabel.Text = $"Accepted friend request from {target}!";
                statusLabel.ForeColor = MainUi.Accent;
                await RefreshAllAsync();
            }
            else
            {
                statusLabel.Text = res.Message;
                statusLabel.ForeColor = MainUi.Danger;
            }

            acceptBtn.Enabled = true;
        }

        private async Task DeclineSelectedRequestAsync()
        {
            if (requestsListBox.SelectedIndex < 0)
            {
                statusLabel.Text = "Select a pending request first.";
                statusLabel.ForeColor = MainUi.Warning;
                return;
            }

            string target = requestsListBox.SelectedItem.ToString();
            declineBtn.Enabled = false;

            var res = await SupabaseApi.DeclineFriendAsync(currentUsername, target);
            if (res.Success)
            {
                statusLabel.Text = $"Declined friend request from {target}.";
                statusLabel.ForeColor = MainUi.TextMuted;
                await RefreshAllAsync();
            }
            else
            {
                statusLabel.Text = res.Message;
                statusLabel.ForeColor = MainUi.Danger;
            }

            declineBtn.Enabled = true;
        }

        private async Task AddFriendClickAsync()
        {
            string target = addFriendBox.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                statusLabel.Text = "Please enter a valid player username.";
                statusLabel.ForeColor = MainUi.Warning;
                return;
            }

            if (target.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                statusLabel.Text = "You cannot add yourself as a friend.";
                statusLabel.ForeColor = MainUi.Warning;
                return;
            }

            addFriendBtn.Enabled = false;
            statusLabel.Text = "Sending request...";
            statusLabel.ForeColor = MainUi.TextMuted;

            var res = await SupabaseApi.AddFriendAsync(currentUsername, target);
            if (res.Success)
            {
                addFriendBox.Clear();
                statusLabel.Text = $"Request successfully sent to {target}!";
                statusLabel.ForeColor = MainUi.Accent;
                await RefreshAllAsync();
            }
            else
            {
                statusLabel.Text = res.Message;
                statusLabel.ForeColor = MainUi.Danger;
            }

            addFriendBtn.Enabled = true;
        }
    }
}
