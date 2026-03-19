using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface IStockDataService
    {
        Task<StockPriceData?> GetCurrentPriceAsync(string symbol);
        Task<List<HistoricalPrice>> GetHistoricalPricesAsync(string symbol, int days);
        Task<CompanyInfo?> GetCompanyInfoAsync(string symbol);
    }
}