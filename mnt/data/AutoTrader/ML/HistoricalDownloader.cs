using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using AutoTrader.Brokers.Interfaces;

namespace AutoTrader.ML
{
    public class HistoricalDownloader
    {
        private readonly IBrokerMarketService _marketService;
        private readonly int _daysBack;
        private readonly List<string> _tickers;
        private readonly int _maxRetries = 3;
        private readonly int _retryDelayMs = 2000;

        public HistoricalDownloader(IBrokerMarketService marketService, List<string> tickers, int daysBack = 90)
        {
            _marketService = marketService;
            _tickers = tickers;
            _daysBack = daysBack;
        }

        public async Task DownloadAsync()
        {
            Console.WriteLine("🚀 Starting historical data download...");

            var now = DateTime.UtcNow.Date.AddDays(-1);
            var startDate = now.AddDays(-_daysBack + 1);

            int totalAttempts = 0;
            int totalSuccess = 0;
            int totalFailures = 0;

            for (DateTime date = startDate; date <= now; date = date.AddDays(1))
            {
                if (!IsWeekday(date)) continue;

                var startTime = date.AddHours(13.5);  // 9:00 AM ET (UTC)
                var endTime = date.AddHours(20);    // 4:00 PM ET (UTC)

                foreach (var ticker in _tickers)
                {
                    totalAttempts++;

                    bool success = false;
                    for (int attempt = 1; attempt <= _maxRetries; attempt++)
                    {
                        try
                        {
                            var candles = await _marketService.GetHistoricalCandlesAsync(ticker, startTime, endTime, "OneMinute");
                            if (candles.Count == 0)
                            {
                                Console.WriteLine($"⚠ No candles found for {ticker} on {date:yyyy-MM-dd}.");
                                break;
                            }

                            var dir = Path.Combine("TrainingData", "Raw", ticker);
                            Directory.CreateDirectory(dir);

                            var filePath = Path.Combine(dir, $"{date:yyyy-MM-dd}.json");
                            var json = JsonSerializer.Serialize(candles);
                            await File.WriteAllTextAsync(filePath, json);

                            Console.WriteLine($"✅ Saved {candles.Count} candles for {ticker} on {date:yyyy-MM-dd}");
                            totalSuccess++;
                            success = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠ Attempt {attempt} failed for {ticker} on {date:yyyy-MM-dd}: {ex.Message}");

                            if (attempt < _maxRetries)
                            {
                                await Task.Delay(_retryDelayMs);
                            }
                        }
                    }

                    if (!success)
                    {
                        Console.WriteLine($"❌ Final failure for {ticker} on {date:yyyy-MM-dd} after {_maxRetries} retries.");
                        totalFailures++;
                    }
                }
            }

            Console.WriteLine("✅ Historical download completed.");
            Console.WriteLine($"Summary: {totalSuccess} successful, {totalFailures} failed, {totalAttempts} total requests.");
        }

        private bool IsWeekday(DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
        }
    }
}
