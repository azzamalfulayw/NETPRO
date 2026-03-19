using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class StockApiSettings
    {
        public string Provider { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public int RateLimitPerMinute { get; set; }
        public int PriceCacheMinutes { get; set; } = 10;
        public int HistoricalCacheMinutes { get; set; } = 60;
        public int UpdateIntervalMinutes { get; set; } = 15;
    }
}