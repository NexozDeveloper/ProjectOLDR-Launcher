using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;

namespace ProjectOLDR
{
    public sealed class GameVersion
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("version_tag")]
        public string VersionTag { get; set; } = "";

        [JsonProperty("download_url")]
        public string DownloadUrl { get; set; } = "";

        [JsonProperty("file_name")]
        public string FileName { get; set; } = "";

        [JsonProperty("archive_password")]
        public string ArchivePassword { get; set; } = "";

        [JsonProperty("notes")]
        public string Notes { get; set; } = "";

        [JsonProperty("color_accent")]
        public string ColorAccent { get; set; } = "#FFFFFF";

        [JsonProperty("is_active")]
        public bool IsActive { get; set; } = true;

        [JsonProperty("sort_order")]
        public int SortOrder { get; set; } = 0;

        [JsonIgnore]
        public string DisplayTag => string.IsNullOrWhiteSpace(VersionTag) ? "latest" : VersionTag.Trim();

        [JsonIgnore]
        public string Slug => VersionManager.ToSlug(Name + "-" + DisplayTag);

        [JsonIgnore]
        public Color AccentColor => VersionManager.ParseColor(ColorAccent, Color.FromArgb(255, 255, 255));

        public GameVersion Clone()
        {
            return new GameVersion
            {
                Id = Id,
                Name = Name,
                VersionTag = VersionTag,
                DownloadUrl = DownloadUrl,
                FileName = FileName,
                ArchivePassword = ArchivePassword,
                Notes = Notes,
                ColorAccent = ColorAccent,
                IsActive = IsActive,
                SortOrder = SortOrder
            };
        }
    }

    public sealed class AdminSession
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class VersionFetchResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public bool SetupNeeded { get; set; }
        public List<GameVersion> Versions { get; set; } = new List<GameVersion>();

        public static VersionFetchResult Ok(List<GameVersion> versions)
        {
            return new VersionFetchResult { Success = true, Versions = versions };
        }

        public static VersionFetchResult Fail(string message, bool setupNeeded = false)
        {
            return new VersionFetchResult { Success = false, Message = message, SetupNeeded = setupNeeded };
        }
    }

    public sealed class LauncherProfile
    {
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Bio { get; set; } = "";
        public string AvatarData { get; set; } = "";

        [JsonIgnore]
        public string DisplayLabel => string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName.Trim();

        [JsonIgnore]
        public string Initial
        {
            get
            {
                string source = DisplayLabel;
                return string.IsNullOrWhiteSpace(source) ? "?" : source.Substring(0, 1).ToUpperInvariant();
            }
        }
    }

    public sealed class ProfileSaveResult
    {
        public bool Success { get; set; }
        public bool Synced { get; set; }
        public string Message { get; set; } = "";

        public static ProfileSaveResult Saved(bool synced, string message)
        {
            return new ProfileSaveResult { Success = true, Synced = synced, Message = message };
        }

        public static ProfileSaveResult Failed(string message)
        {
            return new ProfileSaveResult { Success = false, Synced = false, Message = message };
        }
    }

    public sealed class InstalledVersionInfo
    {
        public string VersionId { get; set; } = "";
        public string Name { get; set; } = "";
        public string VersionTag { get; set; } = "";
        public string Executable { get; set; } = "";
        public string InstalledAt { get; set; } = "";
    }
}
