
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoTrader.Questrade.Market
{
    public class MarketService
    {
        private readonly string _accessToken;
        private readonly string _apiServer;

        public MarketService(string accessToken, string apiServer)
        {
            _accessToken = accessToken;
            _apiServer = apiServer;
        }

        public async Task<QuoteData> GetQuoteBySymbolAsync(string ticker)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var symbolSearchUrl = $"{_apiServer}v1/symbols/search?prefix={ticker}";
            var symbolResp = await client.GetAsync(symbolSearchUrl);
            symbolResp.EnsureSuccessStatusCode();
            var symbolJson = await symbolResp.Content.ReadAsStringAsync();
            using var symbolDoc = JsonDocument.Parse(symbolJson);

            if (!symbolDoc.RootElement.TryGetProperty("symbols", out var symbols) || symbols.GetArrayLength() == 0)
            {
                throw new Exception($"❌ Could not find symbolId for ticker: {ticker}. Raw response: {symbolJson}");
            }

            var symbolId = symbols[0].GetProperty("symbolId").GetInt32();

            var quoteUrl = $"{_apiServer}v1/markets/quotes/{symbolId}";
            var quoteResp = await client.GetAsync(quoteUrl);
            quoteResp.EnsureSuccessStatusCode();
            var quoteJson = await quoteResp.Content.ReadAsStringAsync();
            using var quoteDoc = JsonDocument.Parse(quoteJson);

            if (!quoteDoc.RootElement.TryGetProperty("quotes", out var quotes) || quotes.GetArrayLength() == 0)
            {
                throw new Exception($"❌ No quote returned for symbolId {symbolId}. Raw response: {quoteJson}");
            }

            var quote = quotes[0];

            return new QuoteData
            {
                Symbol = quote.TryGetProperty("symbol", out var s) ? s.GetString() : "Unknown",
                LastTradePrice = quote.TryGetProperty("lastTradePrice", out var ltp) ? ltp.GetDecimal() : 0,
                BidPrice = quote.TryGetProperty("bidPrice", out var bp) ? bp.GetDecimal() : 0,
                BidSize = quote.TryGetProperty("bidSize", out var bs) ? bs.GetInt32() : 0,
                AskPrice = quote.TryGetProperty("askPrice", out var ap) ? ap.GetDecimal() : 0,
                AskSize = quote.TryGetProperty("askSize", out var asp) ? asp.GetInt32() : 0,
                Volume = quote.TryGetProperty("volume", out var v) ? v.GetInt32() : 0,
                IsDelayed = quote.TryGetProperty("isDelayed", out var delay) && delay.GetBoolean()
            };
        }

        public async Task<List<QuoteData>> GetQuotesByTickersAsync(List<string> tickers)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var symbolIds = new List<int>();

            foreach (var ticker in tickers)
            {
                var symbolResp = await client.GetAsync($"{_apiServer}v1/symbols/search?prefix={ticker}");
                var symbolJson = await symbolResp.Content.ReadAsStringAsync();

                try
                {
                    symbolResp.EnsureSuccessStatusCode();
                    using var doc = JsonDocument.Parse(symbolJson);

                    if (!doc.RootElement.TryGetProperty("symbols", out var symbols) || symbols.GetArrayLength() == 0)
                    {
                        Console.WriteLine($"⚠️ Ticker not found: {ticker}");
                        continue;
                    }

                    var symbolId = symbols[0].GetProperty("symbolId").GetInt32();
                    symbolIds.Add(symbolId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to resolve ticker '{ticker}': {ex.Message}");
                    Console.WriteLine($"Raw response: {symbolJson}");
                }
            }

            if (symbolIds.Count == 0)
            {
                Console.WriteLine("⚠️ No valid symbol IDs found. Skipping quote request.");
                return new List<QuoteData>();
            }

            var quoteUrl = $"{_apiServer}v1/markets/quotes?ids={string.Join(",", symbolIds)}";
            var quoteResp = await client.GetAsync(quoteUrl);
            quoteResp.EnsureSuccessStatusCode();
            var quoteJson = await quoteResp.Content.ReadAsStringAsync();
            using var quoteDoc = JsonDocument.Parse(quoteJson);

            if (!quoteDoc.RootElement.TryGetProperty("quotes", out var quotesJson))
            {
                Console.WriteLine($"❌ 'quotes' field missing. Raw response: {quoteJson}");
                return new List<QuoteData>();
            }

            var quotes = new List<QuoteData>();
            foreach (var q in quotesJson.EnumerateArray())
            {
                quotes.Add(new QuoteData
                {
                    Symbol = q.TryGetProperty("symbol", out var s) ? s.GetString() : "Unknown",
                    LastTradePrice = q.TryGetProperty("lastTradePrice", out var ltp) ? ltp.GetDecimal() : 0,
                    BidPrice = q.TryGetProperty("bidPrice", out var bp) ? bp.GetDecimal() : 0,
                    BidSize = q.TryGetProperty("bidSize", out var bs) ? bs.GetInt32() : 0,
                    AskPrice = q.TryGetProperty("askPrice", out var ap) ? ap.GetDecimal() : 0,
                    AskSize = q.TryGetProperty("askSize", out var asp) ? asp.GetInt32() : 0,
                    Volume = q.TryGetProperty("volume", out var v) ? v.GetInt32() : 0,
                    IsDelayed = q.TryGetProperty("isDelayed", out var delay) && delay.GetBoolean()
                });
            }

            return quotes;
        }

        public async Task<List<Candle>> GetHistoricalCandlesAsync(string ticker, DateTime start, DateTime end, string interval = "OneMinute")
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var symbolResp = await client.GetAsync($"{_apiServer}v1/symbols/search?prefix={ticker}");
            symbolResp.EnsureSuccessStatusCode();
            var symbolJson = await symbolResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(symbolJson);
            var symbolId = doc.RootElement.GetProperty("symbols")[0].GetProperty("symbolId").GetInt32();

            string url = $"{_apiServer}v1/markets/candles/{symbolId}?startTime={start:O}&endTime={end:O}&interval={interval}";
            var candleResp = await client.GetAsync(url);
            candleResp.EnsureSuccessStatusCode();
            var candleJson = await candleResp.Content.ReadAsStringAsync();
            using var candleDoc = JsonDocument.Parse(candleJson);

            var candles = new List<Candle>();
            foreach (var c in candleDoc.RootElement.GetProperty("candles").EnumerateArray())
            {
                candles.Add(new Candle
                {
                    Start = c.GetProperty("start").GetDateTime(),
                    Open = c.GetProperty("open").GetDecimal(),
                    High = c.GetProperty("high").GetDecimal(),
                    Low = c.GetProperty("low").GetDecimal(),
                    Close = c.GetProperty("close").GetDecimal(),
                    Volume = c.GetProperty("volume").GetInt32()
                });
            }

            return candles;
        }
        public async Task SaveHistoricalCandlesToFileAsync(string ticker, DateTime start, DateTime end, string interval = "OneMinute")
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Step 1: Get symbolId
            var symbolResp = await client.GetAsync($"{_apiServer}v1/symbols/search?prefix={ticker}");
            symbolResp.EnsureSuccessStatusCode();
            var symbolJson = await symbolResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(symbolJson);
            var symbolId = doc.RootElement.GetProperty("symbols")[0].GetProperty("symbolId").GetInt32();

            // Step 2: Get candle data
            string url = $"{_apiServer}v1/markets/candles/{symbolId}?startTime={start:O}&endTime={end:O}&interval={interval}";
            var candleResp = await client.GetAsync(url);
            candleResp.EnsureSuccessStatusCode();
            var candleJson = await candleResp.Content.ReadAsStringAsync();
            using var candleDoc = JsonDocument.Parse(candleJson);

            var candles = new List<Candle>();
            foreach (var c in candleDoc.RootElement.GetProperty("candles").EnumerateArray())
            {
                candles.Add(new Candle
                {
                    Start = c.GetProperty("start").GetDateTime(),
                    Open = c.GetProperty("open").GetDecimal(),
                    High = c.GetProperty("high").GetDecimal(),
                    Low = c.GetProperty("low").GetDecimal(),
                    Close = c.GetProperty("close").GetDecimal(),
                    Volume = c.GetProperty("volume").GetInt32()
                });
            }

            // Step 3: Save to file
            string fileName = $"candles-{ticker.ToUpper()}.json";
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonOutput = JsonSerializer.Serialize(candles, options);
            await File.WriteAllTextAsync(fileName, jsonOutput);

            Console.WriteLine($"✅ Saved {candles.Count} candles to {fileName}");
        }
    }

    public class QuoteData
    {
        public string Symbol { get; set; }
        public decimal LastTradePrice { get; set; }
        public decimal BidPrice { get; set; }
        public int BidSize { get; set; }
        public decimal AskPrice { get; set; }
        public int AskSize { get; set; }
        public int Volume { get; set; }
        public bool IsDelayed { get; set; }
    }

    public class Candle
    {
        public DateTime Start { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public int Volume { get; set; }
    }
}
