using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTrader.Analytics;
using AutoTrader.Brokers.Models;
using AutoTrader.Brokers.Interfaces;
using AutoTrader.Strategy;
using AutoTrader.Config;
using AutoTrader.ML;

namespace AutoTrader.Strategy
{
    public class StrategyCoordinator
    {
        private readonly IBrokerMarketService _marketService;
        private readonly TradingConfig _config;
        private readonly TrainingFeatureConfig _featureConfig;

        private readonly BreakoutStrategy _breakoutStrategy;
        private readonly CycleStrategy _cycleStrategy;

        public StrategyCoordinator(IBrokerMarketService marketService)
        {
            _marketService = marketService;
            _config = LoadTradingConfig();
            _featureConfig = LoadTrainingFeatureConfig();

            _breakoutStrategy = new BreakoutStrategy();
            _cycleStrategy = new CycleStrategy();
        }

        private TradingConfig LoadTradingConfig()
        {
            var json = File.ReadAllText(Path.Combine("Configs", "trading-config.json"));
            return JsonSerializer.Deserialize<TradingConfig>(json);
        }

        private TrainingFeatureConfig LoadTrainingFeatureConfig()
        {
            var json = File.ReadAllText(Path.Combine("Configs", "training-features.json"));
            return JsonSerializer.Deserialize<TrainingFeatureConfig>(json);
        }

        public async Task<TradePlan?> EvaluateAsync(List<Candle> candles, string ticker, decimal capital = 100_000m)
        {
            var closes = candles.Select(c => c.Close).ToList();
            var highs = candles.Select(c => c.High).ToList();
            var lows = candles.Select(c => c.Low).ToList();
            var atr = Indicators.CalculateATR(highs, lows, closes);
            var atrValue = atr.LastOrDefault() ?? 0;

            var breakoutSignals = _breakoutStrategy.GenerateSignals(candles, _featureConfig);
            var cycleSignals = _cycleStrategy.GenerateSignals(candles, _featureConfig);

            var breakoutSignal = breakoutSignals.LastOrDefault();
            var cycleSignal = cycleSignals.LastOrDefault();

            if (breakoutSignal != null)
            {
                Console.WriteLine($"> ML Breakout signal: {breakoutSignal.Type} at {breakoutSignal.Price}, ML Score: {breakoutSignal.MLScore:0.00}");
                return RiskManager.PlanTrade(
                    ticker,
                    breakoutSignal.Time,
                    breakoutSignal.Type,
                    breakoutSignal.Price,
                    atrValue,
                    capital
                );
            }

            if (cycleSignal != null)
            {
                Console.WriteLine($"> ML Cycle signal: {cycleSignal.Type} at {cycleSignal.Price}, ML Score: {cycleSignal.MLScore:0.00}");
                return RiskManager.PlanTrade(
                    ticker,
                    cycleSignal.Time,
                    cycleSignal.Type,
                    cycleSignal.Price,
                    atrValue,
                    capital
                );
            }

            Console.WriteLine($"> No valid ML signal detected for {ticker}.");
            return null;
        }
    }
}
