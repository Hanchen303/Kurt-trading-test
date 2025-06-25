using AutoTrader.Brokers.Models;

namespace AutoTrader.Brokers.Interfaces
{
    public interface IBrokerMarketService
    {
        Task<List<Candle>> GetHistoricalCandlesAsync(string symbol, DateTime startTime, DateTime endTime, string interval);
        Task<Quote> GetQuoteAsync(string symbol);
        Task<List<OrderBookEntry>> GetOrderBookAsync(string symbol);
    }
}
