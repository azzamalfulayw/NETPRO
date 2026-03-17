using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Dtos.Transaction
{
    public class CreateTransactionDto
    {
        public int StockId { get; set; }
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerShare { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? Notes { get; set; }
        public TransactionCategory Category { get; set; } = TransactionCategory.MarketOrder;
    }
}