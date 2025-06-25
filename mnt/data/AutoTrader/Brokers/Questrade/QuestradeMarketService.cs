using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTrader.Brokers.Interfaces;
using AutoTrader.Brokers.Models;

namespace AutoTrader.Brokers.Questrade
{
    public class QuestradeMarketService : IBrokerMarketService
    {
        private readonly string _accessToken;
        private readonly string _apiServer;

        public QuestradeMarketService(string accessToken, string apiServer)
        {
            _accessToken = accessToken;
            _apiServer = apiServer;
        }

        public async Task<Quote> GetQuoteAsync(string symbol)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var symbolSearchUrl = $"{_apiServer}v1/symbols/search?prefix={symbol}";
            var symbolResp = await client.GetAsync(symbolSearchUrl);
            symbolResp.EnsureSuccessStatusCode();
            var symbolJson = await symbolResp.Content.ReadAsStringAsync();
            using var symbolDoc = JsonDocument.Parse(symbolJson);

            var symbolId = symbolDoc.RootElement.GetProperty("symbols")[0].GetProperty("symbolId").GetInt32();

            var quoteUrl = $"{_apiServer}v1/markets/quotes/{symbolId}";
            var quoteResp = await client.GetAsync(quoteUrl);
            quoteResp.EnsureSuccessStatusCode();
            var quoteJson = await quoteResp.Content.ReadAsStringAsync();
            using var quoteDoc = JsonDocument.Parse(quoteJson);

            var quote = quoteDoc.RootElement.GetProperty("quotes")[0];

            return new Quote
            {
                Symbol = quote.GetProperty("symbol").GetString(),
                Last = quote.GetProperty("lastTradePrice").GetDecimal(),
                Bid = quote.GetProperty("bidPrice").GetDecimal(),
                Ask = quote.GetProperty("askPrice").GetDecimal(),
                Volume = quote.GetProperty("volume").GetInt32()
            };
        }

        public async Task<List<Candle>> GetHistoricalCandlesAsync(string symbol, DateTime startTime, DateTime endTime, string interval)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var symbolResp = await client.GetAsync($"{_apiServer}v1/symbols/search?prefix={symbol}");
            symbolResp.EnsureSuccessStatusCode();
            var symbolJson = await symbolResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(symbolJson);
            var symbolId = doc.RootElement.GetProperty("symbols")[0].GetProperty("symbolId").GetInt32();

            var url = $"{_apiServer}v1/markets/candles/{symbolId}?startTime={startTime:O}&endTime={endTime:O}&interval={interval}";
            var candleResp = await client.GetAsync(url);
            candleResp.EnsureSuccessStatusCode();
            var candleJson = await candleResp.Content.ReadAsStringAsync();
            using var candleDoc = JsonDocument.Parse(candleJson);

            var candles = new List<Candle>();
            foreach (var c in candleDoc.RootElement.GetProperty("candles").EnumerateArray())
            {
                candles.Add(new Candle
                {
                    Timestamp = c.GetProperty("start").GetDateTime(),
                    Open = c.GetProperty("open").GetDecimal(),
                    High = c.GetProperty("high").GetDecimal(),
                    Low = c.GetProperty("low").GetDecimal(),
                    Close = c.GetProperty("close").GetDecimal(),
                    Volume = c.GetProperty("volume").GetInt32()
                });
            }

            return candles;
        }

        public async Task<List<OrderBookEntry>> GetOrderBookAsync(string symbol)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var symbolResp = await client.GetAsync($"{_apiServer}v1/symbols/search?prefix={symbol}");
            symbolResp.EnsureSuccessStatusCode();
            var symbolJson = await symbolResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(symbolJson);
            var symbolId = doc.RootElement.GetProperty("symbols")[0].GetProperty("symbolId").GetInt32();

            var bookResp = await client.GetAsync($"{_apiServer}v1/markets/quotes/level2/{symbolId}");
            bookResp.EnsureSuccessStatusCode();
            var bookJson = await bookResp.Content.ReadAsStringAsync();
            using var bookDoc = JsonDocument.Parse(bookJson);

            var entries = new List<OrderBookEntry>();
            foreach (var side in new[] { "asks", "bids" })
            {
                if (bookDoc.RootElement.TryGetProperty(side, out var bookSide))
                {
                    foreach (var entry in bookSide.EnumerateArray())
                    {
                        entries.Add(new OrderBookEntry
                        {
                            Price = entry.GetProperty("price").GetDecimal(),
                            Size = entry.GetProperty("volume").GetInt64()
                        });
                    }
                }
            }

            return entries;
        }
    }
}
