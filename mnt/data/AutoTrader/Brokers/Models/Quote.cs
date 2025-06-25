namespace AutoTrader.Brokers.Models
{
    public class Quote
    {
        public string Symbol { get; set; } 
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Last { get; set; }
        public long Volume { get; set; }
    }

}
