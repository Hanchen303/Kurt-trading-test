using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader.Analytics;
using AutoTrader.Brokers.Models;
using AutoTrader.Config;

namespace AutoTrader.Labeling
{
    public class LabelingBreakoutStrategy
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
            var highs = candles.Select(c => c.High).ToList();
            var times = candles.Select(c => c.Timestamp).ToList();

            var output = new List<Signal>();

            for (int i = 5; i < closes.Count; i++)
            {
                bool breakoutCandidate = closes[i] > highs.Skip(i - 5).Take(5).Max();
                if (breakoutCandidate)
                {
                    output.Add(new Signal { Time = times[i], Type = "BUY", Price = closes[i] });
                }
            }
            return output;
        }
    }
}
