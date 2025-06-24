using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTrader.Config;
using AutoTrader.Questrade.Market;
using AutoTrader.Questrade.Authentication;
using AutoTrader.Orchestration;
using AutoTrader.Labeling;
using AutoTrader.ML;


namespace AutoTrader
{
    class Program
    {
        private static AuthManager authManager;
        private static MarketService marketService;
        private static List<string> tickers;

        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 AutoTrader ML Production System");

            await InitializeAuthAsync();
            InitializeTickers();

            if (args.Length == 0)
            {
                Console.WriteLine("❓ Please specify mode: retrain | download | trading | test");
                return;
            }

            var mode = args[0].ToLower();

            switch (mode)
            {
                case "retrain":
                    await MasterRetrainingPipeline.RunFullRetrainingAsync(marketService, tickers, daysBack: 90);
                    break;

                case "download":
                    await RunDownloadOnlyMode();
                    break;

                case "trading":
                    Console.WriteLine("🚀 Live Trading Mode (not yet implemented)");
                    break;

                case "test":
                    await RunTestMode();
                    break;
                
                case "modeltest":
                    ModelTester.RunFullModelTest();
                    break; 

                default:
                    Console.WriteLine($"❌ Unknown mode: {mode}");
                    break;
            }
        }

        private static async Task InitializeAuthAsync()
        {
            authManager = new AuthManager();
            await authManager.InitializeAsync();
            marketService = new MarketService(authManager.AccessToken, authManager.ApiServer);
        }

        private static void InitializeTickers()
        {
            // You can easily move tickers to config file later if needed
            tickers = new List<string> { "AAPL", "MSFT", "NVDA" };
        }

        private static async Task RunDownloadOnlyMode()
        {
            Console.WriteLine("🚀 Running Downloader Only Mode...");
            var downloader = new HistoricalDownloader(marketService, tickers, 90);
            await downloader.DownloadAsync();
            Console.WriteLine("✅ Downloader finished.");
        }

        private static async Task RunTestMode()
        {
            Console.WriteLine("🔧 Running authentication test...");

            authManager = new AuthManager();
            authManager.LoadConfig();

            Console.WriteLine($"Loaded AccessToken: {authManager.AccessToken.Substring(0, 10)}...");
            Console.WriteLine($"Loaded RefreshToken: {authManager.RefreshToken.Substring(0, 10)}...");

            Console.WriteLine("🔄 Attempting token refresh...");
            bool refreshed = await authManager.RefreshTokenAsync();

            if (refreshed)
            {
                Console.WriteLine("✅ Token refresh succeeded!");
                Console.WriteLine($"New AccessToken: {authManager.AccessToken.Substring(0, 10)}...");
                Console.WriteLine($"New ApiServer: {authManager.ApiServer}");
            }
            else
            {
                Console.WriteLine("❌ Token refresh failed. 401 Unauthorized likely.");
            }
        }

    }
}
