using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Comment;

namespace api.Dtos.Stock
{
    public class StockDto
    {
        public int Id { get; set; }
        public string Sympol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal Purchase { get; set; }
        public decimal LastDiv { get; set; }
        public string Industry { get; set; } = string.Empty;
        public long MarketCap { get; set; }
        public List<CommentDto> Comments {get; set;}
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal PriceChangePercent { get; set; }
        public DateTime? LastPriceUpdate { get; set; }
    }
}