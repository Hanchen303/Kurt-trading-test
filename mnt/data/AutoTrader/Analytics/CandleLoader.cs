using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTrader.Questrade.Market; // for the Candle class

namespace AutoTrader.Analytics
{
    public static class CandleLoader
    {
        public static async Task<List<Candle>> LoadCandlesFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"❌ File not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Candle>>(json);
        }
    }
}