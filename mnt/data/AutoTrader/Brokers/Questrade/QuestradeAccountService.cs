using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoTrader.Brokers.Questrade
{
    public class QuestradeAccountService
    {
        private readonly string _accessToken;
        private readonly string _apiServer;

        public QuestradeAccountService(string accessToken, string apiServer)
        {
            _accessToken = accessToken;
            _apiServer = apiServer;
        }

        public async Task<string> GetPrimaryAccountNumberAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            var url = $"{_apiServer}v1/accounts";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var account = doc.RootElement.GetProperty("accounts")[0];

            var number = account.GetProperty("number").GetString();
            var type = account.GetProperty("type").GetString();
            var status = account.GetProperty("status").GetString();

            Console.WriteLine($"✅ Account Type: {type}, Status: {status}");
            return number;
        }
    }
}
