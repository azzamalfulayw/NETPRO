using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface IPortfolioAnalyticsService
    {
        Task<PortfolioPerformance> GetPerformanceAsync(AppUser user);
        Task<List<PortfolioValuePoint>> GetPortfolioHistoryAsync(AppUser user, int days);
        Task<PortfolioDiversification> GetDiversificationAsync(AppUser user);
        Task<StockPerformanceMetrics?> GetStockPerformanceAsync(int stockId, AppUser? user = null);
    }
}