
using System;

namespace AutoTrader.Strategy
{
    public class TradePlan
    {
        public string Ticker { get; set; }
        public DateTime Time { get; set; }
        public string Direction { get; set; } // "BUY" or "SELL"
        public decimal EntryPrice { get; set; }
        public decimal StopPrice { get; set; }
        public decimal TargetPrice { get; set; }
        public decimal PositionSize { get; set; }

        public decimal RiskPerShare => Math.Abs(EntryPrice - StopPrice);
        public decimal RewardPerShare => Math.Abs(TargetPrice - EntryPrice);
        public decimal RiskRewardRatio => RiskPerShare == 0 ? 0 : RewardPerShare / RiskPerShare;
    }

    public static class RiskManager
    {
        public static TradePlan PlanTrade(
            string ticker,
            DateTime time,
            string direction,
            decimal entryPrice,
            decimal atr,
            decimal capital = 100_000m,
            decimal riskPercent = 0.01m,
            decimal stopMultiplier = 1m,
            decimal targetMultiplier = 2m)
        {
            decimal stopSize = atr * stopMultiplier;
            decimal targetSize = atr * targetMultiplier;

            decimal stopPrice = direction == "BUY" ? entryPrice - stopSize : entryPrice + stopSize;
            decimal targetPrice = direction == "BUY" ? entryPrice + targetSize : entryPrice - targetSize;

            decimal riskPerShare = Math.Abs(entryPrice - stopPrice);
            decimal maxRisk = capital * riskPercent;
            decimal positionSize = riskPerShare > 0 ? maxRisk / riskPerShare : 0;

            return new TradePlan
            {
                Ticker = ticker,
                Time = time,
                Direction = direction,
                EntryPrice = entryPrice,
                StopPrice = stopPrice,
                TargetPrice = targetPrice,
                PositionSize = Math.Floor(positionSize)
            };
        }
    }
}
