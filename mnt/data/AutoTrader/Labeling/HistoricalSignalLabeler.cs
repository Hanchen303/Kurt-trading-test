using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoTrader.Analytics;
using AutoTrader.Brokers.Interfaces;
using AutoTrader.Brokers.Models;
using AutoTrader.Strategy;
using AutoTrader.Config;


namespace AutoTrader.Labeling
{
    public class HistoricalSignalLabeler
    {
        private readonly TrainingFeatureConfig _featureConfig;

        public HistoricalSignalLabeler()
        {
            var configText = File.ReadAllText("Configs/training-features.json");
            _featureConfig = JsonSerializer.Deserialize<TrainingFeatureConfig>(configText);
        }

        public (List<TradePlan> cyclePlans, List<TradePlan> breakoutPlans) Simulate(List<Candle> candles, string ticker, decimal capital = 100000m)
        {
            var closes = candles.Select(c => c.Close).ToList();
            var highs = candles.Select(c => c.High).ToList();
            var lows = candles.Select(c => c.Low).ToList();
            var atr = Indicators.CalculateATR(highs, lows, closes);
            var atrValue = atr.LastOrDefault() ?? 0m;

            var cycleStrategy = new LabelingCycleStrategy();
            var breakoutStrategy = new LabelingBreakoutStrategy();

            var cycleSignals = cycleStrategy.GenerateSignals(candles, _featureConfig);
            var breakoutSignals = breakoutStrategy.GenerateSignals(candles, _featureConfig);

            var cyclePlans = cycleSignals.Select(sig =>
                RiskManager.PlanTrade(ticker, sig.Time, sig.Type, sig.Price, atrValue, capital)).ToList();

            var breakoutPlans = breakoutSignals.Select(sig =>
                RiskManager.PlanTrade(ticker, sig.Time, sig.Type, sig.Price, atrValue, capital)).ToList();

            return (cyclePlans, breakoutPlans);
        }

        internal (IEnumerable<TradePlan> cyclePlans, IEnumerable<TradePlan> breakoutPlans) Simulate(List<Candle> allCandles, string ticker)
        {
            throw new NotImplementedException();
        }
    }
}
