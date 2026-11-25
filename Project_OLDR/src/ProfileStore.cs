using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProjectOLDR
{
    public static class ProfileStore
    {
        private static string Folder
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Nexoz",
                    "Profiles");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return folder;
            }
        }

        private static string FileNameFor(string username)
        {
            string clean = (username ?? "").Trim();
            foreach (char c in Path.GetInvalidFileNameChars()) clean = clean.Replace(c, '_');
            if (clean.Length == 0) clean = "unknown";
            return clean + ".json";
        }

        private static string ReadValue(JObject row, string snakeName, string localName)
        {
            JToken token = row[snakeName] ?? row[localName];
            return token == null ? "" : token.ToString();
        }

        public static LauncherProfile FromJObject(JObject row)
        {
            if (row == null) return new LauncherProfile();
            return new LauncherProfile
            {
                Username = ReadValue(row, "username", "Username"),
                DisplayName = ReadValue(row, "display_name", "DisplayName"),
                Bio = ReadValue(row, "bio", "Bio"),
                AvatarData = ReadValue(row, "avatar_data", "AvatarData")
            };
        }

        public static LauncherProfile LoadLocal(string username)
        {
            try
            {
                string path = Path.Combine(Folder, FileNameFor(username));
                if (!File.Exists(path)) return new LauncherProfile { Username = username ?? "" };
                LauncherProfile profile = JsonConvert.DeserializeObject<LauncherProfile>(File.ReadAllText(path));
                if (profile == null) profile = new LauncherProfile();
                profile.Username = (username ?? "").Trim();
                return profile;
            }
            catch
            {
                return new LauncherProfile { Username = username ?? "" };
            }
        }

        public static void SaveLocal(LauncherProfile profile)
        {
            if (profile == null) return;
            try
            {
                string path = Path.Combine(Folder, FileNameFor(profile.Username));
                File.WriteAllText(path, JsonConvert.SerializeObject(profile, Formatting.Indented));
            }
            catch
            {
            }
        }

        public static async Task<LauncherProfile> GetProfileAsync(string username)
        {
            string clean = (username ?? "").Trim();
            LauncherProfile fallback = LoadLocal(clean);

            ApiResult result = await SupabaseApi.GetProfileAsync(clean);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Data)) return fallback;

            try
            {
                JArray rows = JArray.Parse(result.Data);
                if (rows.Count == 0) return fallback;
                LauncherProfile profile = FromJObject(rows[0] as JObject);
                if (profile == null) return fallback;
                profile.Username = clean;
                SaveLocal(profile);
                return profile;
            }
            catch
            {
                return fallback;
            }
        }

        public static async Task SyncLocalAsync(LauncherProfile profile, string password)
        {
            if (profile == null) return;
            bool hasData = !string.IsNullOrWhiteSpace(profile.DisplayName) ||
                           !string.IsNullOrWhiteSpace(profile.Bio) ||
                           !string.IsNullOrWhiteSpace(profile.AvatarData);
            if (!hasData) return;

            ApiResult remote = await SupabaseApi.GetProfileAsync(profile.Username);
            if (remote.Success && !string.IsNullOrWhiteSpace(remote.Data))
            {
                try
                {
                    JArray rows = JArray.Parse(remote.Data);
                    if (rows.Count > 0) return;
                }
                catch
                {
                }
            }

            await SaveAsync(profile, password);
        }

        public static async Task<Dictionary<string, LauncherProfile>> GetProfilesAsync(IEnumerable<string> usernames)
        {
            Dictionary<string, LauncherProfile> profiles = new Dictionary<string, LauncherProfile>(StringComparer.OrdinalIgnoreCase);
            List<string> names = new List<string>();

            foreach (string username in usernames)
            {
                string clean = (username ?? "").Trim();
                if (clean.Length == 0 || names.Contains(clean)) continue;
                names.Add(clean);
                profiles[clean] = LoadLocal(clean);
            }

            if (names.Count == 0) return profiles;

            ApiResult result = await SupabaseApi.GetProfilesAsync(names);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Data)) return profiles;

            try
            {
                JArray rows = JArray.Parse(result.Data);
                foreach (JObject row in rows.OfType<JObject>())
                {
                    LauncherProfile profile = FromJObject(row);
                    if (profile == null || string.IsNullOrWhiteSpace(profile.Username)) continue;
                    profiles[profile.Username.Trim()] = profile;
                    SaveLocal(profile);
                }
            }
            catch
            {
            }

            return profiles;
        }

        public static async Task<ProfileSaveResult> SaveAsync(LauncherProfile profile, string password)
        {
            if (profile == null) return ProfileSaveResult.Failed("No profile data to save.");
            profile.Username = (profile.Username ?? "").Trim();
            if (profile.Username.Length == 0) return ProfileSaveResult.Failed("Username required.");

            try
            {
                SaveLocal(profile);
            }
            catch (Exception ex)
            {
                return ProfileSaveResult.Failed("Could not save profile locally: " + ex.Message);
            }

            ApiResult result = await SupabaseApi.SaveProfileAsync(
                profile.Username,
                password,
                profile.DisplayName ?? "",
                profile.Bio ?? "",
                profile.AvatarData ?? "");

            if (result.Success)
            {
                return ProfileSaveResult.Saved(true, "Profile saved. Your friends will see the updated card.");
            }

            return ProfileSaveResult.Saved(false,
                "Saved on this device only. Account sync is not available yet: " + result.Message);
        }
    }
}
