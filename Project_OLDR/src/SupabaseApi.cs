using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProjectOLDR
{
    public sealed class ApiResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public string Data { get; private set; }

        public static ApiResult Ok(string message = "", string data = "")
        {
            return new ApiResult { Success = true, Message = message, Data = data };
        }

        public static ApiResult Fail(string message)
        {
            return new ApiResult { Success = false, Message = message, Data = "" };
        }
    }

    public static class SupabaseApi
    {
        private static readonly HttpClient Client = CreateClient();
        private static readonly HttpClient StatusClient = new HttpClient();
        private static readonly string ClientId = LoadClientId();

        private static HttpClient CreateClient()
        {
            string url = ConfigLoader.Get("SupabaseUrl").TrimEnd('/') + "/";
            string key = ConfigLoader.Get("SupabaseKey");
            var client = new HttpClient { BaseAddress = new Uri(url) };
            client.DefaultRequestHeaders.Add("apikey", key);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public static async Task<ApiResult> RegisterAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length > 32)
                return ApiResult.Fail("Username must be between 1 and 32 characters.");

            if (string.IsNullOrEmpty(password) || password.Length < 6 || password.Length > 128)
                return ApiResult.Fail("Password must contain between 6 and 128 characters.");

            var payload = new
            {
                p_username = username.Trim(),
                p_password = password,
                p_client_id = ClientId
            };

            return await CallRpcAsync("register_launcher_user", payload);
        }

        public static async Task<ApiResult> LoginAsync(string username, string password)
        {
            var payload = new
            {
                p_username = username.Trim(),
                p_password = password
            };

            return await CallRpcAsync("authenticate_launcher_user", payload);
        }

        public static async Task<ApiResult> CheckDatabaseHealthAsync()
        {
            try
            {
                StatusClient.DefaultRequestHeaders.UserAgent.Clear();
                StatusClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                using (var response = await StatusClient.GetAsync("https://status.supabase.com/api/v2/status.json"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        JObject json = JObject.Parse(body);
                        string indicator = (string)json["status"]?["indicator"] ?? "none";
                        string description = (string)json["status"]?["description"] ?? "Operational";

                        if (indicator == "none")
                            return ApiResult.Ok("Supabase: Operational");

                        return ApiResult.Fail($"Supabase: {description}");
                    }
                }
            }
            catch
            {
            }

            try
            {
                using (var fallback = await Client.GetAsync("rest/v1/"))
                {
                    if (fallback.IsSuccessStatusCode)
                        return ApiResult.Ok("Supabase: Operational");
                }
            }
            catch
            {
            }

            return ApiResult.Fail("Supabase: Offline");
        }

        public static async Task UpdatePresenceAsync(string username)
        {
            try
            {
                var payload = new
                {
                    username = username.Trim(),
                    last_seen = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                string json = JsonConvert.SerializeObject(payload);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, "rest/v1/user_presence")
                    {
                        Content = content
                    };
                    req.Headers.Add("Prefer", "resolution=merge-duplicates");
                    await Client.SendAsync(req);
                }
            }
            catch
            {
            }
        }

        public static async Task RemovePresenceAsync(string username)
        {
            try
            {
                string url = $"rest/v1/user_presence?username=eq.{Uri.EscapeDataString(username.Trim())}";
                await Client.DeleteAsync(url);
            }
            catch
            {
            }
        }

        public static async Task<int> GetOnlinePlayersCountAsync()
        {
            try
            {
                string threshold = DateTime.UtcNow.AddSeconds(-8).ToString("yyyy-MM-ddTHH:mm:ssZ");
                string url = $"rest/v1/user_presence?last_seen=gte.{Uri.EscapeDataString(threshold)}";

                using (var response = await Client.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        JArray arr = JArray.Parse(body);
                        return Math.Max(1, arr.Count);
                    }
                }
            }
            catch
            {
            }
            return 1;
        }

        public static async Task<bool> IsUserOnlineAsync(string username)
        {
            try
            {
                string threshold = DateTime.UtcNow.AddSeconds(-8).ToString("yyyy-MM-ddTHH:mm:ssZ");
                string url = $"rest/v1/user_presence?username=eq.{Uri.EscapeDataString(username)}&last_seen=gte.{Uri.EscapeDataString(threshold)}";
                using (var response = await Client.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        JArray arr = JArray.Parse(body);
                        return arr.Count > 0;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        public static async Task<bool> UserExistsAsync(string username)
        {
            try
            {
                string url = $"rest/v1/user_presence?username=eq.{Uri.EscapeDataString(username)}";
                using (var response = await Client.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        JArray arr = JArray.Parse(body);
                        return arr.Count > 0;
                    }
                }
            }
            catch
            {
            }
            return true;
        }

        public static async Task<ApiResult> AddFriendAsync(string currentUsername, string targetUsername)
        {
            try
            {
                bool exists = await UserExistsAsync(targetUsername);
                if (!exists)
                    return ApiResult.Fail("Player not found in database.");

                string checkUrl = $"rest/v1/friends?or=(and(sender.eq.{Uri.EscapeDataString(currentUsername)},receiver.eq.{Uri.EscapeDataString(targetUsername)}),and(sender.eq.{Uri.EscapeDataString(targetUsername)},receiver.eq.{Uri.EscapeDataString(currentUsername)}))";
                using (var checkResp = await Client.GetAsync(checkUrl))
                {
                    if (checkResp.IsSuccessStatusCode)
                    {
                        string checkBody = await checkResp.Content.ReadAsStringAsync();
                        JArray existing = JArray.Parse(checkBody);
                        if (existing.Count > 0)
                        {
                            string status = (string)existing[0]["status"];
                            if (status == "accepted")
                                return ApiResult.Fail("Already in your friends list.");
                            return ApiResult.Fail("A request is already pending.");
                        }
                    }
                }

                var record = new
                {
                    sender = currentUsername.Trim(),
                    receiver = targetUsername.Trim(),
                    status = "pending"
                };

                string json = JsonConvert.SerializeObject(record);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (var response = await Client.PostAsync("rest/v1/friends", content))
                {
                    if (response.IsSuccessStatusCode)
                        return ApiResult.Ok("Friend request sent.");

                    string err = await response.Content.ReadAsStringAsync();
                    return ApiResult.Fail(ParseError(err, (int)response.StatusCode));
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public static async Task<ApiResult> AcceptFriendAsync(string currentUsername, string senderUsername)
        {
            try
            {
                string url = $"rest/v1/friends?sender=eq.{Uri.EscapeDataString(senderUsername)}&receiver=eq.{Uri.EscapeDataString(currentUsername)}";
                var update = new { status = "accepted" };
                string json = JsonConvert.SerializeObject(update);

                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    var req = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
                    using (var response = await Client.SendAsync(req))
                    {
                        if (response.IsSuccessStatusCode)
                            return ApiResult.Ok("Request accepted.");
                        return ApiResult.Fail("Failed to accept request.");
                    }
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public static async Task<ApiResult> DeclineFriendAsync(string currentUsername, string senderUsername)
        {
            try
            {
                string url = $"rest/v1/friends?sender=eq.{Uri.EscapeDataString(senderUsername)}&receiver=eq.{Uri.EscapeDataString(currentUsername)}";
                using (var response = await Client.DeleteAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                        return ApiResult.Ok("Request declined.");
                    return ApiResult.Fail("Failed to decline request.");
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public static async Task<ApiResult> GetFriendsDataAsync(string currentUsername)
        {
            try
            {
                string url = $"rest/v1/friends?or=(sender.eq.{Uri.EscapeDataString(currentUsername)},receiver.eq.{Uri.EscapeDataString(currentUsername)})";
                using (var response = await Client.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        return ApiResult.Ok("Success", body);
                    }
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
            return ApiResult.Fail("Failed to fetch friends data.");
        }

        public static async Task<ApiResult> CheckGitHubUpdateAsync(string currentVersionStr)
        {
            try
            {
                using (var ghClient = new HttpClient())
                {
                    ghClient.DefaultRequestHeaders.UserAgent.Clear();
                    ghClient.DefaultRequestHeaders.UserAgent.ParseAdd("ProjectOLDR-Launcher");

                    using (var resp = await ghClient.GetAsync("https://api.github.com/repos/NexozDeveloper/ProjectOLDR-Launcher/releases/latest"))
                    {
                        if (resp.IsSuccessStatusCode)
                        {
                            string body = await resp.Content.ReadAsStringAsync();
                            JObject rel = JObject.Parse(body);
                            string tag = (string)rel["tag_name"] ?? "";
                            string assetUrl = "";

                            JArray assets = (JArray)rel["assets"];
                            if (assets != null && assets.Count > 0)
                            {
                                foreach (var a in assets)
                                {
                                    string name = (string)a["name"];
                                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                    {
                                        assetUrl = (string)a["browser_download_url"];
                                        break;
                                    }
                                }
                                if (string.IsNullOrEmpty(assetUrl))
                                    assetUrl = (string)assets[0]["browser_download_url"];
                            }

                            if (string.IsNullOrEmpty(assetUrl))
                                assetUrl = (string)rel["html_url"];

                            Version latestVer = NormalizeVersion(tag);
                            Version curVer = NormalizeVersion(currentVersionStr);

                            bool hasUpdate = latestVer > curVer;

                            var outData = new
                            {
                                tag = tag,
                                url = assetUrl,
                                has_update = hasUpdate
                            };

                            return ApiResult.Ok("Success", JsonConvert.SerializeObject(outData));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("GitHub check error: " + ex.Message);
            }

            return ApiResult.Fail("No releases found.");
        }

        private static Version NormalizeVersion(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new Version(1, 0, 0, 0);

            string clean = raw.Trim().TrimStart('v', 'V');
            string[] parts = clean.Split('.');
            int major = parts.Length > 0 && int.TryParse(parts[0], out int v0) ? v0 : 1;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out int v1) ? v1 : 0;
            int build = parts.Length > 2 && int.TryParse(parts[2], out int v2) ? v2 : 0;
            int rev = parts.Length > 3 && int.TryParse(parts[3], out int v3) ? v3 : 0;

            return new Version(major, minor, build, rev);
        }

        private static async Task<ApiResult> CallRpcAsync(string functionName, object payload)
        {
            try
            {
                string json = JsonConvert.SerializeObject(payload);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage response = await Client.PostAsync("rest/v1/rpc/" + functionName, content))
                {
                    string body = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                        return ApiResult.Ok("Success", body);

                    return ApiResult.Fail(ParseError(body, (int)response.StatusCode));
                }
            }
            catch (HttpRequestException)
            {
                return ApiResult.Fail("Connection failed. Check network.");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Database error: " + ex.Message);
            }
        }

        private static string ParseError(string body, int statusCode)
        {
            try
            {
                JObject error = JObject.Parse(body);
                string message = (string)error["message"];
                string code = (string)error["code"];

                if (!string.IsNullOrWhiteSpace(message) && message.StartsWith("RATE_LIMIT", StringComparison.OrdinalIgnoreCase))
                    return "Rate limited. Please retry later.";

                if (!string.IsNullOrWhiteSpace(message))
                    return message;

                if (!string.IsNullOrWhiteSpace(code))
                    return "Supabase error (" + code + ").";
            }
            catch
            {
            }

            return "Error (HTTP " + statusCode + ").";
        }

        private static string LoadClientId()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProjectOLDR");
            string path = Path.Combine(folder, "client.id");

            try
            {
                Directory.CreateDirectory(folder);
                if (File.Exists(path))
                {
                    string existing = File.ReadAllText(path).Trim();
                    if (Guid.TryParse(existing, out _))
                        return existing;
                }

                string id = Guid.NewGuid().ToString();
                File.WriteAllText(path, id);
                return id;
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }
    }
}
