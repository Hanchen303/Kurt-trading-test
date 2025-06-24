
using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Analytics;
using AutoTrader.Questrade.Market;
using Microsoft.ML;
using Microsoft.ML.Data;
using AutoTrader.Config;

namespace AutoTrader.Strategy
{
    public class BreakoutStrategy
    {
        public class Signal
        {
            public DateTime Time { get; set; }
            public string Type { get; set; }  // "BUY" or "SELL"
            public decimal Price { get; set; }
            public float MLScore { get; set; }  // ML model probability output
        }

        private readonly MLContext mlContext;
        private readonly PredictionEngine<AutoTrader.ML.TradeSample, ModelPrediction> predictionEngine;

        public BreakoutStrategy()
        {
            mlContext = new MLContext();
            var modelPath = "MLModels/BreakoutModel.zip";
            var model = mlContext.Model.Load(modelPath, out var inputSchema);
            predictionEngine = mlContext.Model.CreatePredictionEngine<AutoTrader.ML.TradeSample, ModelPrediction>(model);
        }

        public List<Signal> GenerateSignals(List<Candle> candles, TrainingFeatureConfig config)
        {
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

            var output = new List<Signal>();

            for (int i = 5; i < closes.Count; i++)
            {
                // Rule-based filter first (keep your original logic)
                bool breakoutCandidate = closes[i] > highs.Skip(i - 5).Take(5).Max();
                if (!breakoutCandidate) continue;

                // Build ML feature vector
                var features = config.Features.Select(f => (float)(indicators[f][i] ?? 0m)).ToArray();
                var sample = new AutoTrader.ML.TradeSample { Features = features };

                var prediction = predictionEngine.Predict(sample);

                if (prediction.Probability > 0.6f)  // Threshold you can tune
                {
                    output.Add(new Signal { Time = candles[i].Start, Type = "BUY", Price = closes[i], MLScore = prediction.Probability });
                }
            }

            return output;
        }

        private class ModelPrediction
        {
            [ColumnName("PredictedLabel")]
            public bool PredictedLabel { get; set; }
            public float Probability { get; set; }
            public float Score { get; set; }
        }
    }
}
