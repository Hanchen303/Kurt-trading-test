namespace AutoTrader.Config
{
    public class TradingConfig
    {
        public RiskParameters Risk { get; set; }
    }

    public class RiskParameters
    {
        public decimal ATRStopMultiplier { get; set; }
        public decimal ATRTargetMultiplier { get; set; }
        public decimal CapitalPerTrade { get; set; }
    }
}