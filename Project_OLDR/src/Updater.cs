using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ProjectOLDR
{
    public sealed class UpdateInfo
    {
        public bool HasUpdate { get; set; }
        public bool IsMandatory { get; set; }
        public int RemainingDays { get; set; }
        public string Tag { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Error { get; set; } = "";
    }

    public static class Updater
    {
        private const string ReleaseApiUrl = "https://api.github.com/repos/NexozDeveloper/ProjectOLDR-Launcher/releases/latest";
        private const int GraceDays = 3;
        private const long MinimumExeSize = 102400;
        private const int MaxReplaceAttempts = 40;

        public static string DataFolder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectOLDR");
            }
        }

        public static string InstalledVersionPath
        {
            get { return Path.Combine(DataFolder, "installed_version.dat"); }
        }

        public static string JustUpdatedFlagPath
        {
            get { return Path.Combine(DataFolder, "just_updated.dat"); }
        }

        private static string DeadlinePath
        {
            get { return Path.Combine(DataFolder, "update_deadline.dat"); }
        }

        public static Version ParseVersion(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new Version(0, 0, 0, 0);

            string clean = raw.Trim().TrimStart('v', 'V');
            int cut = clean.IndexOfAny(new[] { '-', '+', ' ' });
            if (cut >= 0)
                clean = clean.Substring(0, cut);

            string[] parts = clean.Split('.');
            int[] numbers = new int[4];
            for (int i = 0; i < 4; i++)
            {
                if (i < parts.Length && int.TryParse(parts[i], out int value) && value >= 0)
                    numbers[i] = value;
            }

            return new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        public static Version GetCurrentVersion()
        {
            Version assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);
            Version installedVersion = new Version(0, 0, 0, 0);

            try
            {
                if (File.Exists(InstalledVersionPath))
                    installedVersion = ParseVersion(File.ReadAllText(InstalledVersionPath));
            }
            catch
            {
            }

            return installedVersion > assemblyVersion ? installedVersion : assemblyVersion;
        }

        public static string GetCurrentVersionString()
        {
            Version v = GetCurrentVersion();
            return v.Revision == 0 ? v.ToString(3) : v.ToString();
        }

        public static async Task<UpdateInfo> CheckAsync()
        {
            UpdateInfo info = new UpdateInfo();

            try
            {
                using (HttpClient client = CreateClient(TimeSpan.FromSeconds(20)))
                using (HttpResponseMessage response = await client.GetAsync(ReleaseApiUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        info.Error = "No release found (HTTP " + (int)response.StatusCode + ").";
                        return info;
                    }

                    string body = await response.Content.ReadAsStringAsync();
                    JObject release = JObject.Parse(body);

                    string tag = ((string)release["tag_name"] ?? "").Trim();
                    string url = PickAsset(release);

                    if (string.IsNullOrEmpty(tag))
                    {
                        info.Error = "The latest release has no version tag.";
                        return info;
                    }

                    if (string.IsNullOrEmpty(url))
                    {
                        info.Error = "The latest release has no .exe file attached.";
                        return info;
                    }

                    info.Tag = tag;
                    info.DownloadUrl = url;
                    info.HasUpdate = ParseVersion(tag) > GetCurrentVersion();
                }
            }
            catch (TaskCanceledException)
            {
                info.Error = "Update check timed out.";
                return info;
            }
            catch (Exception ex)
            {
                info.Error = "Update check failed: " + ex.Message;
                return info;
            }

            if (info.HasUpdate)
                ApplyDeadline(info);
            else
                ClearDeadline();

            return info;
        }

        public static async Task ApplyAsync(UpdateInfo info, IProgress<int> progress, IProgress<string> status)
        {
            string currentExe = GetCurrentExePath();
            string newExe = Path.Combine(Path.GetTempPath(), "ProjectOLDR_new.exe");
            string script = Path.Combine(Path.GetTempPath(), "oldr_updater.bat");

            status.Report("Downloading " + info.Tag + "...");
            await DownloadAsync(info.DownloadUrl, newExe, progress);

            ValidateExecutable(newExe);

            status.Report("Applying update...");
            progress.Report(100);

            Directory.CreateDirectory(DataFolder);
            File.WriteAllText(script, BuildScript(currentExe, newExe, info.Tag), new UTF8Encoding(false));

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = script,
                WorkingDirectory = Path.GetTempPath(),
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);

            Environment.Exit(0);
        }

        private static HttpClient CreateClient(TimeSpan timeout)
        {
            HttpClient client = new HttpClient { Timeout = timeout };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ProjectOLDR-Launcher");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private static string PickAsset(JObject release)
        {
            JArray assets = release["assets"] as JArray;
            if (assets == null)
                return "";

            string fallback = "";
            foreach (JToken asset in assets)
            {
                string name = (string)asset["name"] ?? "";
                string url = (string)asset["browser_download_url"] ?? "";

                if (url.Length == 0 || !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (name.IndexOf("ProjectOLDR", StringComparison.OrdinalIgnoreCase) >= 0)
                    return url;

                if (fallback.Length == 0)
                    fallback = url;
            }

            return fallback;
        }

        private static string GetCurrentExePath()
        {
            string path = Environment.ProcessPath;
            if (string.IsNullOrEmpty(path))
                path = Process.GetCurrentProcess().MainModule.FileName;

            string name = Path.GetFileName(path);
            if (name.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The launcher must run as a standalone executable to update itself.");

            return path;
        }

        private static async Task DownloadAsync(string url, string destination, IProgress<int> progress)
        {
            if (File.Exists(destination))
                File.Delete(destination);

            using (HttpClient client = CreateClient(TimeSpan.FromMinutes(10)))
            using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                long? total = response.Content.Headers.ContentLength;
                long totalRead = 0;
                int lastPercent = -1;

                using (Stream input = await response.Content.ReadAsStreamAsync())
                using (FileStream output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    byte[] buffer = new byte[81920];
                    int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (total.HasValue && total.Value > 0)
                        {
                            int percent = (int)Math.Min(99, totalRead * 100 / total.Value);
                            if (percent != lastPercent)
                            {
                                lastPercent = percent;
                                progress.Report(percent);
                            }
                        }
                    }
                }

                if (total.HasValue && totalRead != total.Value)
                    throw new IOException("Download was incomplete. Please try again.");
            }
        }

        private static void ValidateExecutable(string path)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists || file.Length < MinimumExeSize)
                throw new InvalidDataException("The downloaded file is too small to be a valid program.");

            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z')
                    throw new InvalidDataException("The downloaded file is not a valid Windows program.");
            }
        }

        private static string EscapePercent(string value)
        {
            return value.Replace("%", "%%");
        }

        private static string BuildScript(string currentExe, string newExe, string tag)
        {
            string safeTag = Regex.Replace(tag, "[^0-9A-Za-z._-]", "");
            string cur = EscapePercent(currentExe);
            string fresh = EscapePercent(newExe);
            string versionFile = EscapePercent(InstalledVersionPath);
            string flagFile = EscapePercent(JustUpdatedFlagPath);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("chcp 65001 > nul");
            sb.AppendLine("set tries=0");
            sb.AppendLine(":retry");
            sb.AppendLine("ping 127.0.0.1 -n 2 > nul");
            sb.AppendLine("move /y \"" + fresh + "\" \"" + cur + "\" > nul 2>&1");
            sb.AppendLine("if not exist \"" + fresh + "\" goto done");
            sb.AppendLine("set /a tries+=1");
            sb.AppendLine("if %tries% geq " + MaxReplaceAttempts + " goto fail");
            sb.AppendLine("goto retry");
            sb.AppendLine(":done");
            sb.AppendLine("echo " + safeTag + "> \"" + versionFile + "\"");
            sb.AppendLine("echo " + safeTag + "> \"" + flagFile + "\"");
            sb.AppendLine("start \"\" \"" + cur + "\"");
            sb.AppendLine("del \"%~f0\"");
            sb.AppendLine("exit");
            sb.AppendLine(":fail");
            sb.AppendLine("del \"" + fresh + "\" > nul 2>&1");
            sb.AppendLine("start \"\" \"" + cur + "\"");
            sb.AppendLine("del \"%~f0\"");
            sb.AppendLine("exit");
            return sb.ToString();
        }

        private static void ApplyDeadline(UpdateInfo info)
        {
            DateTime deadline = DateTime.UtcNow.AddDays(GraceDays);

            try
            {
                Directory.CreateDirectory(DataFolder);
                bool reuse = false;

                if (File.Exists(DeadlinePath))
                {
                    string[] parts = File.ReadAllText(DeadlinePath).Trim().Split('|');
                    if (parts.Length == 2 && parts[0] == info.Tag && long.TryParse(parts[1], out long ticks))
                    {
                        deadline = new DateTime(ticks, DateTimeKind.Utc);
                        reuse = true;
                    }
                }

                if (!reuse)
                    File.WriteAllText(DeadlinePath, info.Tag + "|" + deadline.Ticks);
            }
            catch
            {
            }

            TimeSpan left = deadline - DateTime.UtcNow;
            info.IsMandatory = left <= TimeSpan.Zero;
            info.RemainingDays = info.IsMandatory ? 0 : (int)Math.Ceiling(left.TotalDays);
        }

        private static void ClearDeadline()
        {
            try
            {
                if (File.Exists(DeadlinePath))
                    File.Delete(DeadlinePath);
            }
            catch
            {
            }
        }
    }
}
