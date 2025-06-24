using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoTrader.Questrade.Authentication
{
    public class AuthManager
    {
        public string AccessToken { get; private set; }
        public string ApiServer { get; private set; }
        public string RefreshToken { get; private set; }
        public bool IsPractice { get; private set; }

        private const string ConfigFile = "Configs/appsettings.json";

        public AuthManager() { }

        public async Task InitializeAsync()
        {
            LoadConfig();

            bool refreshed = await RefreshTokenAsync();
            if (!refreshed)
            {
                Console.WriteLine("❌ Failed to refresh token.");
                throw new Exception("Authorization failed.");
            }
        }

        public void LoadConfig()
        {
            var json = File.ReadAllText(ConfigFile);
            var config = JsonSerializer.Deserialize<AppSettings>(json);
            AccessToken = config.AccessToken;
            RefreshToken = config.RefreshToken;
            ApiServer = config.ApiServer;
            IsPractice = config.IsPractice;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            string baseUrl = IsPractice 
                ? "https://practicelogin.questrade.com" 
                : "https://login.questrade.com";

            string url = $"{baseUrl}/oauth2/token?grant_type=refresh_token&refresh_token={RefreshToken}";

            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthResponse>(json);

                AccessToken = result.access_token;
                RefreshToken = result.refresh_token;
                ApiServer = result.api_server;

                SaveConfig();

                Console.WriteLine("🔁 Token refreshed and config updated.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Token refresh failed: {ex.Message}");
                return false;
            }
        }

        private void SaveConfig()
        {
            var config = new AppSettings
            {
                AccessToken = AccessToken,
                RefreshToken = RefreshToken,
                ApiServer = ApiServer,
                IsPractice = IsPractice
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }

        private class AuthResponse
        {
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public string api_server { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
        }

        private class AppSettings
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public string ApiServer { get; set; }
            public bool IsPractice { get; set; }
        }
    }
}
