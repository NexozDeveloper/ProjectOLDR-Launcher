using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProjectOLDR
{
    public static class Logger
    {
        private static readonly string supabaseUrl = ConfigLoader.Get("SupabaseUrl");
        private static readonly HttpClient client = CreateClient();

        private static HttpClient CreateClient()
        {
            string key = ConfigLoader.Get("SupabaseKey");
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", key);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
            return httpClient;
        }

        public static async Task Log(string level, string message, string details = "")
        {
            try
            {
                var log = new
                {
                    level = level,
                    message = message,
                    details = details
                };

                string json = JsonConvert.SerializeObject(log);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string url = $"{supabaseUrl.TrimEnd('/')}/rest/v1/logs";
                await client.PostAsync(url, content);
            }
            catch
            {
            }
        }

        public static async Task Info(string message, string details = "")
        {
            await Log("INFO", message, details);
        }

        public static async Task Warn(string message, string details = "")
        {
            await Log("WARN", message, details);
        }

        public static async Task Error(string message, string details = "")
        {
            await Log("ERROR", message, details);
        }
    }
}
