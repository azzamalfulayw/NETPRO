using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    [Table("Transactions")]
    public class Transaction
    {
        public int Id { get; set; }
        public string AppUserId { get; set; } = string.Empty;
        public int StockId { get; set; }
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerShare { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public AppUser AppUser { get; set; } = null!;
        public Stock Stock { get; set; } = null!;
        public TransactionCategory Category { get; set; } = TransactionCategory.MarketOrder;
    }

    public enum TransactionType
    {
        Buy,
        Sell
    }

    public enum TransactionCategory
    {
        MarketOrder,
        LimitOrder,
        StopOrder
    }
}