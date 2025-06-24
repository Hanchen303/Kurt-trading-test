using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Analytics;
using AutoTrader.Questrade.Market;
using AutoTrader.Config;

namespace AutoTrader.Labeling
{
    public class LabelingCycleStrategy
    {
        public class Signal
        {
            public DateTime Time { get; set; }
            public string Type { get; set; }
            public decimal Price { get; set; }
        }

        public List<Signal> GenerateSignals(List<Candle> candles, TrainingFeatureConfig config)
        {
            var closes = candles.Select(c => c.Close).ToList();
            var lows = candles.Select(c => c.Low).ToList();
            var times = candles.Select(c => c.Start).ToList();

            var output = new List<Signal>();

            for (int i = 5; i < closes.Count; i++)
            {
                bool cycleCandidate = lows[i] < lows.Skip(i - 5).Take(5).Min();
                if (cycleCandidate)
                {
                    output.Add(new Signal { Time = times[i], Type = "BUY", Price = closes[i] });
                }
            }
            return output;
        }
    }
}
