using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoTrader.Analytics;
using AutoTrader.Questrade.Market;
using AutoTrader.Config;

namespace AutoTrader.ML
{
    public static class DualDataGenerator
    {
        public static void GenerateTrainingAndTestData(
            List<AutoTrader.Strategy.TradePlan> cyclePlans, 
            List<AutoTrader.Strategy.TradePlan> breakoutPlans, 
            TrainingFeatureConfig config)
        {
            GeneratePartitionedData("Cycle", cyclePlans, config);
            GeneratePartitionedData("Breakout", breakoutPlans, config);
        }

        private static void GeneratePartitionedData(
            string strategyName, 
            List<AutoTrader.Strategy.TradePlan> plans, 
            TrainingFeatureConfig config)
        {
            var sorted = plans.OrderBy(p => p.Time).ToList();

            int splitIndex = (int)(sorted.Count * 0.95);
            var trainPlans = sorted.Take(splitIndex).ToList();
            var testPlans = sorted.Skip(splitIndex).ToList();

            GenerateDataForSubset(strategyName, "Train", trainPlans, config);
            GenerateDataForSubset(strategyName, "Test", testPlans, config);
        }

        private static void GenerateDataForSubset(
            string strategyName, 
            string subset, 
            List<AutoTrader.Strategy.TradePlan> plans, 
            TrainingFeatureConfig config)
        {
            var rows = new List<(float[] Features, bool Label)>();

            foreach (var plan in plans)
            {
                string candleFilePath = Path.Combine("TrainingData", "Raw", plan.Ticker, $"{plan.Time:yyyy-MM-dd}.json");
                if (!File.Exists(candleFilePath)) continue;

                var json = File.ReadAllText(candleFilePath);
                var candles = JsonSerializer.Deserialize<List<Candle>>(json);

                var closes = candles.Select(c => c.Close).ToList();
                var highs = candles.Select(c => c.High).ToList();
                var lows = candles.Select(c => c.Low).ToList();
                var volumes = candles.Select(c => c.Volume).ToList();
                var times = candles.Select(c => c.Start).ToList();

                var indicators = new Dictionary<string, List<decimal?>>();

                foreach (var feature in config.Features)
                {
                    indicators[feature] = feature switch
                    {
                        "Rsi" => Indicators.CalculateRSI(closes).Select(x => (decimal?)x).ToList(),
                        "Cycle" => Indicators.CalculateCyberCycleWithSignal(closes).Cycle,
                        "Band" => Indicators.CalculateBollingerBands(closes).Lower,
                        "Atr" => Indicators.CalculateATR(highs, lows, closes).Select(x => (decimal?)x).ToList(),
                        "Obv" => Indicators.CalculateRollingOBV(closes, volumes, times).Select(x => (decimal?)x).ToList(),
                        "AccDist" => Indicators.CalculateRollingAccDist(closes, highs, lows, volumes).Select(x => (decimal?)x).ToList(),
                        _ => throw new Exception($"Unknown feature: {feature}")
                    };
                }

                int entryIndex = times.FindIndex(t => Math.Abs((t - plan.Time).TotalSeconds) < 1);
                if (entryIndex == -1) continue;

                float[] featureVector = config.Features
                    .Select(f => indicators.ContainsKey(f) && indicators[f].Count > entryIndex && indicators[f][entryIndex].HasValue
                        ? (float)indicators[f][entryIndex].Value
                        : 0f)
                    .ToArray();

                bool label = EvaluateOutcome(candles, entryIndex, plan.TargetPrice, plan.StopPrice);
                rows.Add((featureVector, label));
            }

            string dir = Path.Combine("TrainingData", subset, strategyName);
            Directory.CreateDirectory(dir);
            string filePath = Path.Combine(dir, $"data-{strategyName.ToLower()}.csv");
            WriteSamplesToCsv(rows, config, filePath);

            Console.WriteLine($"✅ Generated {rows.Count} samples for {strategyName} [{subset}] → {filePath}");
        }

        private static void WriteSamplesToCsv(List<(float[] Features, bool Label)> rows, TrainingFeatureConfig config, string filePath)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine(string.Join(",", config.Features.Select(f => f.ToLower())) + ",label");
            foreach (var row in rows)
            {
                writer.WriteLine($"{string.Join(",", row.Features.Select(f => f.ToString(CultureInfo.InvariantCulture)))},{row.Label.ToString().ToLower()}");
            }
        }

        private static bool EvaluateOutcome(List<Candle> candles, int entryIndex, decimal targetPrice, decimal stopLossPrice, int maxLookahead = 60)
        {
            for (int i = entryIndex + 1; i < candles.Count && i <= entryIndex + maxLookahead; i++)
            {
                var candle = candles[i];
                if (candle.Low <= stopLossPrice) return false;
                if (candle.High >= targetPrice) return true;
            }
            return false;
        }
    }
}
