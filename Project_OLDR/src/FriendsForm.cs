using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public class FriendsForm : Form
    {
        private readonly string currentUsername;
        private FlowLayoutPanel friendsFlow;
        private FlowLayoutPanel requestsFlow;
        private TextBox addFriendBox;
        private Button addFriendBtn;
        private Label statusLabel;
        private Label friendCountLabel;
        private Label requestCountLabel;
        private System.Windows.Forms.Timer loopTimer;
        private bool refreshing;

        public FriendsForm(string username)
        {
            currentUsername = (username ?? "").Trim();
            BuildUI();

            Shown += async (s, e) =>
            {
                await RefreshAllAsync();
                loopTimer = new System.Windows.Forms.Timer { Interval = 6000 };
                loopTimer.Tick += async (ts, te) => await RefreshAllAsync();
                loopTimer.Start();
            };

            FormClosed += (s, e) =>
            {
                if (loopTimer == null) return;
                loopTimer.Stop();
                loopTimer.Dispose();
            };
        }

        private void BuildUI()
        {
            MainUi.SetupForm(this, "Project OLDR - Friends", new Size(520, 640));
            Controls.Add(MainUi.CreateTitleBar(this, "Friends"));

            Panel card = MainUi.CreateContainer(20, 68, 480, 520);

            card.Controls.Add(MainUi.CreateLabel("FRIENDS", 16, 16, 180, 26, MainUi.HeaderMedium, MainUi.TextPrimary));
            friendCountLabel = MainUi.CreateLabel("0", 360, 17, 104, 24, MainUi.Small, MainUi.TextMuted);
            friendCountLabel.TextAlign = ContentAlignment.MiddleRight;
            card.Controls.Add(friendCountLabel);

            friendsFlow = new FlowLayoutPanel
            {
                Location = new Point(16, 48),
                Size = new Size(448, 216),
                BackColor = Color.Transparent,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            card.Controls.Add(friendsFlow);

            Panel divider = new Panel { Location = new Point(16, 274), Size = new Size(448, 1), BackColor = MainUi.BorderColor };
            card.Controls.Add(divider);

            card.Controls.Add(MainUi.CreateLabel("PENDING REQUESTS", 16, 290, 240, 20, MainUi.Small, MainUi.TextMuted));
            requestCountLabel = MainUi.CreateLabel("0", 360, 290, 104, 20, MainUi.Small, MainUi.TextMuted);
            requestCountLabel.TextAlign = ContentAlignment.MiddleRight;
            card.Controls.Add(requestCountLabel);

            requestsFlow = new FlowLayoutPanel
            {
                Location = new Point(16, 316),
                Size = new Size(448, 126),
                BackColor = Color.Transparent,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            card.Controls.Add(requestsFlow);

            card.Controls.Add(MainUi.CreateLabel("ADD A FRIEND", 16, 452, 240, 18, MainUi.Small, MainUi.TextMuted));
            addFriendBox = MainUi.CreateStyledTextBox(16, 476, 314, 36);
            card.Controls.Add(addFriendBox);

            addFriendBtn = MainUi.CreateButton("Add", FluentIcon.Plus, 338, 476, 126, 36, ButtonKind.Primary);
            addFriendBtn.Click += AddFriendBtn_Click;
            card.Controls.Add(addFriendBtn);

            statusLabel = MainUi.CreateLabel("Synchronizing contacts...", 16, 522, 448, 34, MainUi.Caption, MainUi.TextMuted);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(statusLabel);

            Controls.Add(card);
        }

        private async Task RefreshAllAsync()
        {
            if (refreshing || IsDisposed) return;
            refreshing = true;

            try
            {
                await SupabaseApi.UpdatePresenceAsync(currentUsername);

                ApiResult result = await SupabaseApi.GetFriendsDataAsync(currentUsername);
                if (!result.Success)
                {
                    SetStatus("Could not load friends: " + result.Message, MainUi.Danger);
                    return;
                }

                List<string> friendNames = new List<string>();
                List<string> requestNames = new List<string>();

                JArray rows = JArray.Parse(result.Data ?? "[]");
                foreach (JObject row in rows.OfType<JObject>())
                {
                    string sender = (string)row["sender"] ?? "";
                    string receiver = (string)row["receiver"] ?? "";
                    string status = (string)row["status"] ?? "";

                    if (status == "accepted")
                    {
                        string name = sender.Equals(currentUsername, StringComparison.OrdinalIgnoreCase) ? receiver : sender;
                        if (name.Length > 0 && !friendNames.Contains(name)) friendNames.Add(name);
                    }
                    else if (status == "pending" && receiver.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
                    {
                        if (sender.Length > 0 && !requestNames.Contains(sender)) requestNames.Add(sender);
                    }
                }

                Dictionary<string, LauncherProfile> profiles =
                    await ProfileStore.GetProfilesAsync(friendNames.Concat(requestNames));
                HashSet<string> online = await SupabaseApi.GetOnlineUsernamesAsync();

                RenderFriends(friendNames, profiles, online);
                RenderRequests(requestNames, profiles);

                int onlineCount = friendNames.Count(name => online.Contains(name));
                friendCountLabel.Text = friendNames.Count + " total";
                requestCountLabel.Text = requestNames.Count + " pending";
                SetStatus(friendNames.Count + " friends - " + onlineCount + " online", MainUi.TextMuted);
            }
            catch (Exception ex)
            {
                SetStatus("Could not refresh friends: " + ex.Message, MainUi.Danger);
            }
            finally
            {
                refreshing = false;
            }
        }

        private void RenderFriends(List<string> names, Dictionary<string, LauncherProfile> profiles, HashSet<string> online)
        {
            ClearControls(friendsFlow.Controls);
            if (names.Count == 0)
            {
                friendsFlow.Controls.Add(CreateEmptyRow("No accepted friends yet."));
                return;
            }

            foreach (string name in names)
            {
                LauncherProfile profile = profiles.ContainsKey(name) ? profiles[name] : new LauncherProfile { Username = name };
                friendsFlow.Controls.Add(CreateFriendRow(name, profile, online.Contains(name)));
            }
        }

        private void RenderRequests(List<string> names, Dictionary<string, LauncherProfile> profiles)
        {
            ClearControls(requestsFlow.Controls);
            if (names.Count == 0)
            {
                requestsFlow.Controls.Add(CreateEmptyRow("No pending requests."));
                return;
            }

            foreach (string name in names)
            {
                LauncherProfile profile = profiles.ContainsKey(name) ? profiles[name] : new LauncherProfile { Username = name };
                requestsFlow.Controls.Add(CreateRequestRow(name, profile));
            }
        }

        private Panel CreateFriendRow(string username, LauncherProfile profile, bool online)
        {
            if (profile == null) profile = new LauncherProfile();
            profile.Username = username;

            Panel row = CreateRowPanel(46);
            ProfileAvatar avatar = new ProfileAvatar(32) { Location = new Point(8, 7) };
            avatar.SetProfile(profile);
            row.Controls.Add(avatar);

            row.Controls.Add(MainUi.CreateLabel(profile.DisplayLabel, 50, 5, 240, 22,
                MainUi.BodyStrong, MainUi.TextPrimary));

            string sub = profile.Bio == null ? "" : profile.Bio.Trim();
            if (sub.Length == 0) sub = "@" + username;
            if (sub.Length > 32) sub = sub.Substring(0, 32) + "...";
            row.Controls.Add(MainUi.CreateLabel(sub, 50, 25, 268, 16, MainUi.Small, MainUi.TextMuted));

            Label status = MainUi.CreateLabel(online ? "ONLINE" : "OFFLINE", 330, 14, 88, 18,
                MainUi.Small, online ? MainUi.Accent : MainUi.TextMuted);
            status.TextAlign = ContentAlignment.MiddleRight;
            row.Controls.Add(status);

            return row;
        }

        private Panel CreateRequestRow(string username, LauncherProfile profile)
        {
            if (profile == null) profile = new LauncherProfile();
            profile.Username = username;

            Panel row = CreateRowPanel(40);
            ProfileAvatar avatar = new ProfileAvatar(28) { Location = new Point(8, 6) };
            avatar.SetProfile(profile);
            row.Controls.Add(avatar);

            string label = profile.DisplayLabel;
            if (!label.Equals(username, StringComparison.OrdinalIgnoreCase)) label += "  @" + username;
            row.Controls.Add(MainUi.CreateLabel(label, 44, 8, 292, 24, MainUi.Regular, MainUi.TextPrimary));

            Button accept = MainUi.CreateIconButton(FluentIcon.Check, 344, 6, 28, "Accept request");
            accept.Click += async (s, e) => await AcceptRequestAsync(username);
            row.Controls.Add(accept);

            Button decline = MainUi.CreateIconButton(FluentIcon.Close, 386, 6, 28, "Decline request");
            decline.Click += async (s, e) => await DeclineRequestAsync(username);
            row.Controls.Add(decline);

            return row;
        }

        private Panel CreateEmptyRow(string text)
        {
            Panel row = CreateRowPanel(40);
            Label label = MainUi.CreateLabel(text, 14, 8, 404, 24, MainUi.Small, MainUi.TextMuted);
            label.TextAlign = ContentAlignment.MiddleLeft;
            row.Controls.Add(label);
            return row;
        }

        private static Panel CreateRowPanel(int height)
        {
            Panel row = new Panel
            {
                Size = new Size(432, height),
                Margin = new Padding(0, 0, 0, 6),
                BackColor = MainUi.SurfaceColor
            };

            row.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, row.Width - 1, row.Height - 1);
                using (GraphicsPath path = MainUi.GetRoundedPath(rect, 7))
                using (SolidBrush bg = new SolidBrush(MainUi.SurfaceColor))
                {
                    e.Graphics.FillPath(bg, path);
                    using (Pen pen = new Pen(MainUi.BorderColor, 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            return row;
        }

        private static void ClearControls(Control.ControlCollection controls)
        {
            while (controls.Count > 0)
            {
                Control control = controls[0];
                controls.Remove(control);
                control.Dispose();
            }
        }

        private async Task AcceptRequestAsync(string sender)
        {
            SetStatus("Accepting request...", MainUi.TextMuted);
            ApiResult result = await SupabaseApi.AcceptFriendAsync(currentUsername, sender);
            SetStatus(result.Success ? "Friend request accepted." : result.Message, result.Success ? MainUi.Accent : MainUi.Danger);
            if (result.Success) await RefreshAllAsync();
        }

        private async Task DeclineRequestAsync(string sender)
        {
            SetStatus("Declining request...", MainUi.TextMuted);
            ApiResult result = await SupabaseApi.DeclineFriendAsync(currentUsername, sender);
            SetStatus(result.Success ? "Friend request declined." : result.Message, result.Success ? MainUi.TextMuted : MainUi.Danger);
            if (result.Success) await RefreshAllAsync();
        }

        private async void AddFriendBtn_Click(object sender, EventArgs e)
        {
            string target = addFriendBox.Text.Trim();
            if (target.Length == 0)
            {
                SetStatus("Enter a player username.", MainUi.Warning);
                return;
            }

            if (target.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("You cannot add yourself as a friend.", MainUi.Warning);
                return;
            }

            addFriendBtn.Enabled = false;
            SetStatus("Sending request...", MainUi.TextMuted);

            ApiResult result = await SupabaseApi.AddFriendAsync(currentUsername, target);
            addFriendBtn.Enabled = true;

            if (result.Success)
            {
                addFriendBox.Clear();
                SetStatus("Request sent to " + target + ".", MainUi.Accent);
                await RefreshAllAsync();
            }
            else
            {
                SetStatus(result.Message, MainUi.Danger);
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
