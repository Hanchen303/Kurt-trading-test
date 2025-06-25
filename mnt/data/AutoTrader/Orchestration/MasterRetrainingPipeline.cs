using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTrader.ML;
using AutoTrader.Brokers.Interfaces;
using AutoTrader.Brokers.Models;
using AutoTrader.Brokers;
using AutoTrader.Config;
using AutoTrader.Strategy;
using AutoTrader.Labeling;
using TradePlan = AutoTrader.Strategy.TradePlan;

namespace AutoTrader.Orchestration
{
    public static class MasterRetrainingPipeline
    {
        public static async Task RunFullRetrainingAsync(IBrokerMarketService marketService, List<string> tickers, int daysBack = 90)
        {
            Console.WriteLine("🚀 Starting FULL retraining pipeline...");

            var downloader = new HistoricalDownloader(marketService, tickers, daysBack);
            await downloader.DownloadAsync();

            var configText = File.ReadAllText("Configs/training-features.json");
            var featureConfig = JsonSerializer.Deserialize<TrainingFeatureConfig>(configText);

            var allCyclePlans = new List<TradePlan>();
            var allBreakoutPlans = new List<TradePlan>();

            foreach (var ticker in tickers)
            {
                Console.WriteLine($"🔄 Labeling signals for {ticker}...");
                var rawPath = Path.Combine("TrainingData", "Raw", ticker);
                if (!Directory.Exists(rawPath)) continue;

                var candleFiles = Directory.GetFiles(rawPath, "*.json").OrderBy(f => f).ToList();

                var allCandles = new List<Candle>();

                foreach (var file in candleFiles)
                {
                    var json = await File.ReadAllTextAsync(file);
                    var candles = JsonSerializer.Deserialize<List<Candle>>(json);

                    if (candles != null)
                        allCandles.AddRange(candles);
                }

                var simulator = new HistoricalSignalLabeler();
                var (cyclePlans, breakoutPlans) = simulator.Simulate(allCandles, ticker);

                allCyclePlans.AddRange(cyclePlans);
                allBreakoutPlans.AddRange(breakoutPlans);
            }

            DualDataGenerator.GenerateTrainingAndTestData(allCyclePlans, allBreakoutPlans, featureConfig);

            MLPipeline_Cycle.RunTraining();
            MLPipeline_Breakout.RunTraining();

            Console.WriteLine("✅ Full retraining pipeline completed.");
        }
    }
}
