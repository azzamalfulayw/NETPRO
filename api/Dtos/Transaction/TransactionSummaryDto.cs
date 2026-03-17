using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Transaction
{
    public class TransactionSummaryDto
    {
        public decimal TotalInvested { get; set; }
        public decimal TotalFromSales { get; set; }
        public int TotalBuyTransactions { get; set; }
        public int TotalSellTransactions { get; set; }
        public decimal NetInvestment { get; set; }
    }
}