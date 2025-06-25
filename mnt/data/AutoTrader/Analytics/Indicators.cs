using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTrader.Analytics
{
    public static class Indicators
    {
        public static List<decimal?> CalculateSMA(List<decimal> values, int period)
        {
            var result = new List<decimal?>();
            for (int i = 0; i < values.Count; i++)
            {
                if (i + 1 < period)
                    result.Add(null);
                else
                    result.Add(Math.Round(values.Skip(i + 1 - period).Take(period).Average(), 4));
            }
            return result;
        }

        public static List<decimal?> CalculateVWMA(List<decimal> prices, List<long> volumes, int period)
        {
            var result = new List<decimal?>();
            for (int i = 0; i < prices.Count; i++)
            {
                if (i + 1 < period)
                {
                    result.Add(null);
                    continue;
                }

                var priceSegment = prices.Skip(i + 1 - period).Take(period).ToList();
                var volSegment = volumes.Skip(i + 1 - period).Take(period).ToList();

                decimal weightedSum = 0, totalVolume = 0;
                for (int j = 0; j < period; j++)
                {
                    weightedSum += priceSegment[j] * volSegment[j];
                    totalVolume += volSegment[j];
                }

                result.Add(totalVolume == 0 ? 0 : Math.Round(weightedSum / totalVolume, 4));
            }
            return result;
        }

        public static List<decimal?> CalculateRollingAccDist(List<decimal> closes, List<decimal> highs, List<decimal> lows, List<long> volumes, int window = 60)
        {
            var result = new List<decimal?>();
            for (int i = 0; i < closes.Count; i++)
            {
                if (i + 1 < window)
                {
                    result.Add(null);
                    continue;
                }

                decimal rollingAD = 0;
                for (int j = i + 1 - window; j <= i; j++)
                {
                    var range = highs[j] - lows[j];
                    if (range == 0) continue;
                    var clv = ((closes[j] - lows[j]) - (highs[j] - closes[j])) / range;
                    rollingAD += clv * volumes[j];
                }
                result.Add(Math.Round(rollingAD, 2));
            }
            return result;
        }

        public static List<long?> CalculateRollingOBV(List<decimal> closes, List<long> volumes, List<DateTime> times, int window = 60)
        {
            var result = new List<long?>();
            var dayStarts = new HashSet<int>();

            for (int i = 1; i < times.Count; i++)
            {
                if (times[i].Date != times[i - 1].Date)
                    dayStarts.Add(i);
            }

            for (int i = 0; i < closes.Count; i++)
            {
                if (i + 1 < window)
                {
                    result.Add(null);
                    continue;
                }

                long rollingObv = 0;
                for (int j = i + 1 - window; j <= i; j++)
                {
                    if (j == 0 || dayStarts.Contains(j)) continue;
                    if (closes[j] > closes[j - 1]) rollingObv += (long)volumes[j];
                    else if (closes[j] < closes[j - 1]) rollingObv -= (long)volumes[j];
                }

                result.Add(rollingObv);
            }

            return result;
        }

        public static List<decimal?> CalculateEMA(List<decimal> values, int period)
        {
            var result = new List<decimal?>();
            decimal multiplier = 2m / (period + 1);
            decimal? ema = null;

            for (int i = 0; i < values.Count; i++)
            {
                if (i + 1 < period)
                {
                    result.Add(null);
                }
                else if (i + 1 == period)
                {
                    ema = values.Take(period).Average();
                    result.Add(Math.Round(ema.Value, 4));
                }
                else
                {
                    ema = ((values[i] - ema.Value) * multiplier) + ema.Value;
                    result.Add(Math.Round(ema.Value, 4));
                }
            }

            return result;
        }

        public static (List<decimal?> Upper, List<decimal?> Lower) CalculateBollingerBands(List<decimal> closes, int period = 20, decimal multiplier = 2)
        {
            var sma = CalculateSMA(closes, period);
            var stdDev = CalculateStdDev(closes, period);

            var upper = new List<decimal?>();
            var lower = new List<decimal?>();

            for (int i = 0; i < closes.Count; i++)
            {
                if (sma[i] == null || stdDev[i] == null)
                {
                    upper.Add(null);
                    lower.Add(null);
                }
                else
                {
                    upper.Add(Math.Round(sma[i].Value + multiplier * stdDev[i].Value, 4));
                    lower.Add(Math.Round(sma[i].Value - multiplier * stdDev[i].Value, 4));
                }
            }

            return (upper, lower);
        }

        public static List<decimal?> CalculateRSI(List<decimal> closes, int period = 14)
        {
            var rsi = new List<decimal?> { null };
            decimal gain = 0, loss = 0;

            for (int i = 1; i < closes.Count; i++)
            {
                decimal change = closes[i] - closes[i - 1];
                if (i < period)
                {
                    if (change > 0) gain += change;
                    else loss -= change;
                    rsi.Add(null);
                }
                else if (i == period)
                {
                    if (change > 0) gain += change;
                    else loss -= change;

                    var avgGain = gain / period;
                    var avgLoss = loss / period;
                    var rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                    rsi.Add(Math.Round(100 - (100 / (1 + rs)), 2));
                }
                else
                {
                    var currentGain = change > 0 ? change : 0;
                    var currentLoss = change < 0 ? -change : 0;
                    gain = ((gain * (period - 1)) + currentGain) / period;
                    loss = ((loss * (period - 1)) + currentLoss) / period;
                    var rs = loss == 0 ? 100 : gain / loss;
                    rsi.Add(Math.Round(100 - (100 / (1 + rs)), 2));
                }
            }

            return rsi;
        }

        public static (List<decimal?> MACDLine, List<decimal?> SignalLine, List<decimal?> Histogram) CalculateMACD(List<decimal> closes, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
        {
            var fastEma = CalculateEMA(closes, fastPeriod);
            var slowEma = CalculateEMA(closes, slowPeriod);

            var macdLine = closes.Select((_, i) =>
                fastEma[i] != null && slowEma[i] != null ? (decimal?)(Math.Round(fastEma[i].Value - slowEma[i].Value, 4)) : null).ToList();

            var signalLine = CalculateEMA(macdLine.Select(x => x ?? 0).ToList(), signalPeriod);

            var histogram = macdLine.Select((m, i) =>
                m != null && signalLine[i] != null ? (decimal?)(Math.Round(m.Value - signalLine[i].Value, 4)) : null).ToList();

            return (macdLine, signalLine, histogram);
        }

        public static List<decimal?> CalculateATR(List<decimal> highs, List<decimal> lows, List<decimal> closes, int period = 14)
        {
            var tr = new List<decimal>();
            for (int i = 1; i < closes.Count; i++)
            {
                var high = highs[i];
                var low = lows[i];
                var prevClose = closes[i - 1];
                var range = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
                tr.Add(range);
            }

            var atr = new List<decimal?> { null };
            atr.AddRange(CalculateSMA(tr, period));
            return atr;
        }

        public static (List<decimal?> Cycle, List<decimal?> Signal) CalculateCyberCycleWithSignal(List<decimal> closes, int alphaLength = 5, int signalPeriod = 3)
        {
            var cycle = new decimal[closes.Count];
            var smooth = new decimal[closes.Count];
            var result = new List<decimal?>();

            decimal alpha = 2m / (alphaLength + 1);

            for (int i = 0; i < closes.Count; i++)
            {
                if (i < 2)
                {
                    result.Add(null);
                    continue;
                }

                smooth[i] = (closes[i] + 2 * closes[i - 1] + closes[i - 2]) / 4;

                if (i < 3)
                {
                    result.Add(null);
                    continue;
                }

                cycle[i] = (1 - 0.5m * alpha) * (1 - 0.5m * alpha) * (smooth[i] - 2 * smooth[i - 1] + smooth[i - 2])
                         + 2 * (1 - alpha) * cycle[i - 1]
                         - (1 - alpha) * (1 - alpha) * cycle[i - 2];

                result.Add(Math.Round(cycle[i], 4));
            }

            var signal = CalculateEMA(result.Select(x => x ?? 0).ToList(), signalPeriod);
            return (result, signal);
        }

        public static List<decimal?> CalculateStdDev(List<decimal> values, int period)
        {
            var result = new List<decimal?>();
            for (int i = 0; i < values.Count; i++)
            {
                if (i + 1 < period)
                    result.Add(null);
                else
                {
                    var window = values.Skip(i + 1 - period).Take(period);
                    var avg = window.Average();
                    var variance = window.Select(x => (x - avg) * (x - avg)).Average();
                    result.Add(Math.Round((decimal)Math.Sqrt((double)variance), 4));
                }
            }
            return result;
        }
    }
}
