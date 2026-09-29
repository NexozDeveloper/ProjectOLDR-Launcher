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

        public static ApiResult Ok(string message = "")
        {
            return new ApiResult { Success = true, Message = message };
        }

        public static ApiResult Fail(string message)
        {
            return new ApiResult { Success = false, Message = message };
        }
    }

    public static class SupabaseApi
    {
        private static readonly HttpClient Client = CreateClient();
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
                return ApiResult.Fail("Invalid username (1 to 32 characters).");

            if (string.IsNullOrEmpty(password) || password.Length < 6 || password.Length > 128)
                return ApiResult.Fail("The password must contain between 6 and 128 characters.");

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
                        return ApiResult.Ok();

                    return ApiResult.Fail(ParseError(body, (int)response.StatusCode));
                }
            }
            catch (HttpRequestException)
            {
                return ApiResult.Fail("Unable to contact the DB. Checks the Internet and project URL.");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("DB error : " + ex.Message);
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
                    return "You have been rate limited - retry in 5 days.";

                if (!string.IsNullOrWhiteSpace(message))
                    return message;

                if (!string.IsNullOrWhiteSpace(code))
                    return "Erreur Supabase (" + code + ").";
            }
            catch
            {
            }

            return "Supabase Error (HTTP " + statusCode + ").";
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
