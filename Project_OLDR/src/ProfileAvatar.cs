using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ProjectOLDR.UI;

namespace ProjectOLDR
{
    public sealed class ProfileAvatar : Panel
    {
        private Image avatarImage;
        private string initialText = "?";

        public ProfileAvatar(int size)
        {
            Size = new Size(size, size);
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Resize += (s, e) => UpdateRegion();
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(new Rectangle(0, 0, Width - 1, Height - 1));
                Region = new Region(path);
            }
        }

        public void SetProfile(LauncherProfile profile)
        {
            if (profile == null)
            {
                SetInitial("?");
                return;
            }
            SetInitial(profile.Initial);
            SetImageData(profile.AvatarData);
        }

        public void SetInitial(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) initialText = "?";
            else initialText = name.Trim().Substring(0, 1).ToUpperInvariant();
            Invalidate();
        }

        public void SetImageData(string data)
        {
            ReleaseImage();
            if (!string.IsNullOrWhiteSpace(data))
            {
                try
                {
                    string payload = data.Contains(",") ? data.Substring(data.IndexOf(',') + 1) : data;
                    byte[] bytes = Convert.FromBase64String(payload);
                    using (MemoryStream stream = new MemoryStream(bytes))
                    {
                        Image loaded = Image.FromStream(stream);
                        avatarImage = new Bitmap(loaded);
                    }
                }
                catch
                {
                    avatarImage = null;
                }
            }
            Invalidate();
        }

        private void ReleaseImage()
        {
            if (avatarImage != null)
            {
                avatarImage.Dispose();
                avatarImage = null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath circle = new GraphicsPath())
            {
                circle.AddEllipse(rect);
                using (SolidBrush bg = new SolidBrush(MainUi.SurfaceHighlight))
                {
                    e.Graphics.FillPath(bg, circle);
                }
                if (avatarImage != null)
                {
                    e.Graphics.DrawImage(avatarImage, rect);
                }
                else
                {
                    using (SolidBrush text = new SolidBrush(MainUi.TextSecondary))
                    using (Font font = new Font("Segoe UI Variable Display", Math.Max(11, Width / 3f), FontStyle.Bold))
                    {
                        e.Graphics.DrawString(initialText, font, text, rect, new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        });
                    }
                }
                using (Pen pen = new Pen(MainUi.BorderColor, 1f))
                {
                    e.Graphics.DrawPath(pen, circle);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ReleaseImage();
            base.Dispose(disposing);
        }
    }
}
