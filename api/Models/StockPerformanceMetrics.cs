using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class StockPerformanceMetrics
    {
        public int StockId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        public decimal CurrentPrice { get; set; }
        public decimal PriceChangePercent { get; set; }
        public DateTime? LastPriceUpdate { get; set; }

        public decimal AverageRating { get; set; }
        public int RatingCount { get; set; }

        public int TotalTransactions { get; set; }
        public int TotalBuyQuantity { get; set; }
        public int TotalSellQuantity { get; set; }
    }
}