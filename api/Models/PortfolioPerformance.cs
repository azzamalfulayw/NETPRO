using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class PortfolioPerformance
    {
        public decimal TotalValue { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal TotalGainLoss { get; set; }
        public decimal TotalGainLossPercent { get; set; }
        public decimal DayChange { get; set; }
        public decimal DayChangePercent { get; set; }
        public List<StockHolding> Holdings { get; set; } = new();
    }

    public class StockHolding
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal AverageCostBasis { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal GainLoss { get; set; }
        public decimal GainLossPercent { get; set; }
    }
}