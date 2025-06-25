using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTrader.Config;
using AutoTrader.Brokers;
using AutoTrader.Brokers.Models;
using AutoTrader.Brokers.Interfaces;
using AutoTrader.Orchestration;
using AutoTrader.Labeling;
using AutoTrader.ML;


namespace AutoTrader
{
    class Program
    {
        private static IBrokerAuthService authService;
        private static IBrokerMarketService marketService;
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
                // Load appsettings.json
            var json = File.ReadAllText("Configs/appsettings.json");
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            // Use BrokerFactory to initialize broker services
            var (market, auth) = BrokerFactory.CreateBroker(settings);

            authService = auth;
            marketService = market;

            await authService.AuthenticateAsync();
        }

        private static void InitializeTickers()
        {
            // You can easily move tickers to config file later if needed
            tickers = new List<string> { "AAPL", "MSFT", "NVDA" };
        }

        private static async Task RunDownloadOnlyMode()
        {
            //add anything to download
        }

        private static async Task RunTestMode()
        {
            //add anything to test
        }
        public static AppSettings LoadAppSettings()
        {
            var json = File.ReadAllText("Configs/appsettings.json");
            return JsonSerializer.Deserialize<AppSettings>(json);
        }

    }
}
