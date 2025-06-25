using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTrader.Brokers.Interfaces;
using AutoTrader.Config;

namespace AutoTrader.Brokers.Questrade
{
    public class QuestradeAuthService : IBrokerAuthService
    {
        public string AccessToken { get; private set; }
        public string ApiServer { get; private set; }
        public string RefreshToken { get; private set; }
        public bool IsPractice { get; private set; }

        private readonly QuestradeConfig _config;
        private readonly string _configPath;

        public QuestradeAuthService(QuestradeConfig config, string configPath = "Configs/appsettings.json")
        {
            _config = config;
            _configPath = configPath;
            RefreshToken = config.RefreshToken;
            IsPractice = config.IsPractice;
        }

        public async Task AuthenticateAsync()
        {
            bool success = await RefreshTokenAsync();
            if (!success)
                throw new Exception("Failed to authenticate with Questrade.");
        }

        public async Task<string> GetAccessTokenAsync() => AccessToken;

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

                _config.AccessToken = AccessToken;
                _config.RefreshToken = RefreshToken;
                _config.ApiServer = ApiServer;

                var fullJson = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(fullJson);
                settings.Questrade = _config;

                var outputJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, outputJson);

                Console.WriteLine("🔁 Token refreshed and written to config.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Token refresh failed: {ex.Message}");
                return false;
            }
        }

        private class AuthResponse
        {
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public string api_server { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
        }
    }
}
