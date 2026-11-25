using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProjectOLDR
{
    public static class VersionManager
    {
        public static string NexozFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nexoz");

        public static string RootFolder => Path.Combine(NexozFolder, "SN ProjectOLDR");

        public static string ToSlug(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "version";
            string slug = Regex.Replace(raw.Trim(), @"[^a-zA-Z0-9]+", "-").Trim('-');
            if (slug.Length > 64) slug = slug.Substring(0, 64).Trim('-');
            return string.IsNullOrEmpty(slug) ? "version" : slug;
        }

        public static Color ParseColor(string value, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            try
            {
                string text = value.Trim();
                if (!text.StartsWith("#", StringComparison.Ordinal)) text = "#" + text;
                return ColorTranslator.FromHtml(text);
            }
            catch
            {
                return fallback;
            }
        }

        public static string VersionFolder(GameVersion version)
        {
            return Path.Combine(RootFolder, version.Slug);
        }

        public static InstalledVersionInfo ReadInstalledInfo(GameVersion version)
        {
            try
            {
                string path = Path.Combine(VersionFolder(version), "installed.json");
                if (!File.Exists(path)) return null;
                return JsonConvert.DeserializeObject<InstalledVersionInfo>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public static readonly string[] QualityNames = new string[]
        {
            "MaxQuality",
            "OriginalQuality",
            "UltraPerformance"
        };

        public static string NormalizeQuality(string raw)
        {
            string value = Regex.Replace((raw ?? "").ToLowerInvariant(), "[^a-z0-9]", "");
            if (value.Contains("ultra") || value.Contains("performance")) return "UltraPerformance";
            if (value.Contains("max")) return "MaxQuality";
            if (value.Contains("origin")) return "OriginalQuality";
            return "";
        }

        private static string[] QualityExecutableNames(string quality)
        {
            string normalized = NormalizeQuality(quality);
            if (normalized == "MaxQuality") return new string[] { "MaxQuality.exe" };
            if (normalized == "UltraPerformance") return new string[] { "Ultraperformance.exe", "UltraPerformance.exe" };
            if (normalized == "OriginalQuality") return new string[] { "OriginQuality.exe", "OriginalQuality.exe", "Secret Neighbour.exe" };
            return new string[0];
        }

        public static string FindQualityExecutable(GameVersion version, string quality)
        {
            string root = VersionFolder(version);
            if (!Directory.Exists(root)) return "";

            foreach (string name in QualityExecutableNames(quality))
            {
                string match = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                    .FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)
                        && !IsIgnoredExecutable(f));
                if (!string.IsNullOrEmpty(match)) return match;
            }
            return "";
        }

        public static string QualityFolder(GameVersion version, string quality)
        {
            string executable = FindQualityExecutable(version, quality);
            return string.IsNullOrEmpty(executable) ? VersionFolder(version) : Path.GetDirectoryName(executable);
        }

        public static string FindExecutable(GameVersion version)
        {
            return FindExecutableInFolder(VersionFolder(version), version, true);
        }

        public static string FindExecutable(GameVersion version, string quality)
        {
            string executable = FindQualityExecutable(version, quality);
            return string.IsNullOrEmpty(executable) ? FindExecutable(version) : executable;
        }

        private static string FindExecutableInFolder(string folder, GameVersion version, bool useInstalledInfo)
        {
            if (!Directory.Exists(folder)) return "";

            if (useInstalledInfo)
            {
                InstalledVersionInfo info = ReadInstalledInfo(version);
                if (info != null && !string.IsNullOrWhiteSpace(info.Executable))
                {
                    string relative = info.Executable.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    string exact = Path.IsPathRooted(relative) ? relative : Path.Combine(folder, relative);
                    if (File.Exists(exact)) return exact;
                }
            }

            if (!string.IsNullOrWhiteSpace(version.FileName))
            {
                string named = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                    .FirstOrDefault(f => string.Equals(Path.GetFileName(f), Path.GetFileName(version.FileName), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(named)) return named;
            }

            List<string> candidates = Directory.GetFiles(folder, "*.exe", SearchOption.AllDirectories)
                .Where(f => !IsIgnoredExecutable(f))
                .OrderBy(f => f.Count(c => c == Path.DirectorySeparatorChar))
                .ThenBy(f => Path.GetFileName(f).Length)
                .ToList();

            return candidates.Count > 0 ? candidates[0] : "";
        }

        public static bool IsInstalled(GameVersion version)
        {
            string exe = FindExecutable(version);
            return !string.IsNullOrEmpty(exe) && File.Exists(exe);
        }

        private static string NormalizeDownloadUrl(string raw)
        {
            string url = (raw ?? "").Trim();
            if (url.Length == 0) throw new Exception("This version has no download URL.");

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new Exception("The configured download URL is invalid.");
            }

            if (uri.Host.Equals("dropbox.com", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Equals("www.dropbox.com", StringComparison.OrdinalIgnoreCase))
            {
                if (uri.Query.IndexOf("dl=", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    url += string.IsNullOrEmpty(uri.Query) ? "?dl=1" : "&dl=1";
                }
            }

            return url;
        }

        public static async Task InstallAsync(GameVersion version, IProgress<int> progress, CancellationToken token)
        {
            string folder = VersionFolder(version);
            Directory.CreateDirectory(RootFolder);

            string downloadUrl = NormalizeDownloadUrl(version.DownloadUrl);
            string tempDirectory = Path.Combine(Path.GetTempPath(), "ProjectOLDRDownloads");
            Directory.CreateDirectory(tempDirectory);
            string extension = GetExtension(downloadUrl);
            string tempFile = Path.Combine(tempDirectory, version.Slug + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + extension);

            try
            {
                progress?.Report(1);
                using (HttpClient client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("ProjectOLDR-Launcher/1.0");
                    using (HttpResponseMessage response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, token))
                    {
                        response.EnsureSuccessStatusCode();
                        long? total = response.Content.Headers.ContentLength;
                        using (Stream source = await response.Content.ReadAsStreamAsync(token))
                        using (FileStream destination = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                        {
                            byte[] buffer = new byte[81920];
                            long downloaded = 0;
                            int read;
                            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                            {
                                token.ThrowIfCancellationRequested();
                                await destination.WriteAsync(buffer, 0, read, token);
                                downloaded += read;
                                if (total.HasValue && total.Value > 0)
                                {
                                    progress?.Report((int)Math.Min(96, downloaded * 100 / total.Value));
                                }
                            }
                        }
                    }
                }

                token.ThrowIfCancellationRequested();
                progress?.Report(97);

                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                Directory.CreateDirectory(folder);

                string ext = extension.ToLowerInvariant();
                if (ext == ".zip")
                {
                    ZipFile.ExtractToDirectory(tempFile, folder, true);
                }
                else if (ext == ".rar" || ext == ".7z")
                {
                    await ExtractWithArchiverAsync(tempFile, folder, version.ArchivePassword, token);
                }
                else
                {
                    string targetName = string.IsNullOrWhiteSpace(version.FileName) ? Path.GetFileName(tempFile) : Path.GetFileName(version.FileName);
                    string target = Path.Combine(folder, targetName);
                    string parent = Path.GetDirectoryName(target);
                    if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
                    File.Copy(tempFile, target, true);
                }

                token.ThrowIfCancellationRequested();
                string executable = FindExecutable(version);
                if (string.IsNullOrEmpty(executable))
                {
                    throw new Exception("No executable was found in the downloaded files.");
                }

                InstalledVersionInfo info = new InstalledVersionInfo
                {
                    VersionId = version.Id,
                    Name = version.Name,
                    VersionTag = version.DisplayTag,
                    Executable = GetRelativePath(folder, executable),
                    InstalledAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                File.WriteAllText(Path.Combine(folder, "installed.json"), JsonConvert.SerializeObject(info, Formatting.Indented));
                progress?.Report(100);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
                catch
                {
                }
            }
        }

        public static void Launch(GameVersion version)
        {
            Launch(version, "");
        }

        public static Process Launch(GameVersion version, string quality)
        {
            string executable = FindExecutable(version, quality);
            if (string.IsNullOrEmpty(executable)) throw new Exception("Executable missing.");

            return Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = Path.GetDirectoryName(executable),
                UseShellExecute = true
            });
        }

        public static string OpenVersionFolder(GameVersion version, string quality)
        {
            string path = QualityFolder(version, quality);
            if (!Directory.Exists(path))
            {
                path = VersionFolder(version);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "\"" + path + "\"",
                UseShellExecute = true
            });
            return path;
        }

        public static void Uninstall(GameVersion version)
        {
            string folder = VersionFolder(version);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        private static string GetExtension(string url)
        {
            try
            {
                string path = new Uri(url).AbsolutePath;
                string ext = Path.GetExtension(path);
                if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 8) return ext;
            }
            catch
            {
            }
            return ".bin";
        }

        private static string GetRelativePath(string folder, string file)
        {
            string fullFolder = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullFile = Path.GetFullPath(file);
            if (fullFile.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase))
                return fullFile.Substring(fullFolder.Length);
            return Path.GetFileName(fullFile);
        }

        private static bool IsIgnoredExecutable(string path)
        {
            string name = Path.GetFileName(path);
            return name.IndexOf("unins", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("update", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("crash", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("dxsetup", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("vcredist", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static async Task ExtractWithArchiverAsync(string archive, string folder, string password, CancellationToken token)
        {
            string binary = LocateArchiver(out bool isSevenZip);
            if (string.IsNullOrEmpty(binary))
            {
                throw new Exception("Install WinRAR, UnRAR or 7-Zip to extract this version."); // 7-Zip Better buddy
            }

            string passwordArgument = string.IsNullOrWhiteSpace(password) ? "" : "-p\"" + password + "\"";
            string arguments;
            if (isSevenZip)
            {
                arguments = "x -y " + passwordArgument + " -o\"" + folder + "\" \"" + archive + "\"";
            }
            else
            {
                arguments = "x -y " + passwordArgument + " \"" + archive + "\" \"" + folder + Path.DirectorySeparatorChar + "\"";
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = binary,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(psi))
            {
                if (process == null) throw new Exception("Could not start the archive extractor.");
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(token);
                if (process.ExitCode != 0 && process.ExitCode != 1)
                {
                    throw new Exception("Extractor failed with code " + process.ExitCode + ". " + error.Trim());
                }
            }
        }

        private static string LocateArchiver(out bool isSevenZip)
        {
            string[] sevenZip = new string[]
            {
                @"C:\Program Files\7-Zip\7z.exe",
                @"C:\Program Files (x86)\7-Zip\7z.exe"
            };
            string[] rar = new string[]
            {
                @"C:\Program Files\WinRAR\WinRAR.exe",
                @"C:\Program Files (x86)\WinRAR\WinRAR.exe",
                @"C:\Program Files\UnRAR\UnRAR.exe",
                @"C:\Program Files (x86)\UnRAR\UnRAR.exe",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UnRAR.exe")
            };

            foreach (string path in sevenZip)
            {
                if (File.Exists(path))
                {
                    isSevenZip = true;
                    return path;
                }
            }

            foreach (string path in rar)
            {
                if (File.Exists(path))
                {
                    isSevenZip = false;
                    return path;
                }
            }

            isSevenZip = false;
            return "";
        }
    }
}
