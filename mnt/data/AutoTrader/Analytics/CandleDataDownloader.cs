
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTrader.Questrade.Authentication;
using AutoTrader.Questrade.Market;

namespace AutoTrader.Analytics
{
    public class CandleDataDownloader
    {
        private readonly MarketService _marketService;
        private static readonly TimeSpan MarketOpen = new TimeSpan(13, 30, 0);  // 9:30 AM EST in UTC
        private static readonly TimeSpan MarketClose = new TimeSpan(20, 0, 0);  // 4:00 PM EST in UTC

        public CandleDataDownloader(MarketService marketService)
        {
            _marketService = marketService;
        }

        public async Task DownloadRecentTradingDaysAsync(List<string> tickers, int daysBack = 28)
        {
            var now = DateTime.UtcNow.Date;
            var startDate = now.AddDays(-daysBack);

            for (DateTime date = startDate; date <= now; date = date.AddDays(1))
            {
                if (!IsWeekday(date))
                    continue;

                var startTime = date.Add(MarketOpen);
                var endTime = date.Add(MarketClose);

                foreach (var ticker in tickers)
                {
                    try
                    {
                        var candles = await _marketService.GetHistoricalCandlesAsync(ticker, startTime, endTime, "OneMinute");
                        if (candles.Count == 0) continue;

                        var dir = Path.Combine("TrainingData", ticker);
                        Directory.CreateDirectory(dir);

                        var filePath = Path.Combine(dir, $"{date:yyyy-MM-dd}.json");
                        var json = JsonSerializer.Serialize(candles, new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(filePath, json);

                        Console.WriteLine($"✅ Saved {candles.Count} candles for {ticker} on {date:yyyy-MM-dd}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to download {ticker} for {date:yyyy-MM-dd}: {ex.Message}");
                    }
                }
            }
        }

        private bool IsWeekday(DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
        }
    }
}
