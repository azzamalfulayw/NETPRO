using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Service
{
    public class PortfolioAnalyticsService : IPortfolioAnalyticsService
    {
        private readonly ApplicationDBContext _context;
        private readonly IStockDataService _stockDataService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<PortfolioAnalyticsService> _logger;

        public PortfolioAnalyticsService(
            ApplicationDBContext context,
            IStockDataService stockDataService,
            ICacheService cacheService,
            ILogger<PortfolioAnalyticsService> logger)
        {
            _context = context;
            _stockDataService = stockDataService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<PortfolioPerformance> GetPerformanceAsync(AppUser user)
        {
            var cacheKey = $"portfolio-performance-{user.Id}";
            var cached = _cacheService.Get<PortfolioPerformance>(cacheKey);

            if (cached != null)
                return cached;

            var portfolios = await _context.portfolios
                .AsNoTracking()
                .Include(p => p.Stock)
                .Where(p => p.AppUserId == user.Id && p.Quantity > 0)
                .ToListAsync();

            var buyTransactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Stock)
                .Where(t => t.AppUserId == user.Id && t.Type == TransactionType.Buy)
                .ToListAsync();

            var holdings = new List<StockHolding>();

            foreach (var portfolio in portfolios)
            {
                var stockBuys = buyTransactions
                    .Where(t => t.StockId == portfolio.StockId)
                    .ToList();

                var totalSharesBought = stockBuys.Sum(t => t.Quantity);
                var totalCost = stockBuys.Sum(t => t.TotalAmount);

                decimal averageCostBasis = totalSharesBought > 0
                    ? totalCost / totalSharesBought
                    : 0;

                var currentPrice = portfolio.Stock.CurrentPrice > 0
                    ? portfolio.Stock.CurrentPrice
                    : portfolio.Stock.Purchase;

                var currentValue = portfolio.Quantity * currentPrice;
                var invested = portfolio.Quantity * averageCostBasis;
                var gainLoss = currentValue - invested;
                var gainLossPercent = invested > 0 ? (gainLoss / invested) * 100 : 0;

                var previousPrice = portfolio.Stock.PriceChangePercent != 0
                    ? currentPrice / (1 + (portfolio.Stock.PriceChangePercent / 100))
                    : currentPrice;

                var dayChangeForHolding = (currentPrice - previousPrice) * portfolio.Quantity;

                holdings.Add(new StockHolding
                {
                    Symbol = portfolio.Stock.Sympol,
                    CompanyName = portfolio.Stock.CompanyName,
                    Quantity = portfolio.Quantity,
                    AverageCostBasis = Math.Round(averageCostBasis, 2),
                    CurrentPrice = currentPrice,
                    CurrentValue = Math.Round(currentValue, 2),
                    TotalInvested = Math.Round(invested, 2),
                    GainLoss = Math.Round(gainLoss, 2),
                    GainLossPercent = Math.Round(gainLossPercent, 2)
                });
            }

            var totalValue = holdings.Sum(h => h.CurrentValue);
            var totalInvestedPortfolio = holdings.Sum(h => h.TotalInvested);
            var totalGainLoss = totalValue - totalInvestedPortfolio;
            var totalGainLossPercent = totalInvestedPortfolio > 0
                ? (totalGainLoss / totalInvestedPortfolio) * 100
                : 0;

            decimal dayChange = 0;
            foreach (var portfolio in portfolios)
            {
                var currentPrice = portfolio.Stock.CurrentPrice > 0
                    ? portfolio.Stock.CurrentPrice
                    : portfolio.Stock.Purchase;

                var previousPrice = portfolio.Stock.PriceChangePercent != 0
                    ? currentPrice / (1 + (portfolio.Stock.PriceChangePercent / 100))
                    : currentPrice;

                dayChange += (currentPrice - previousPrice) * portfolio.Quantity;
            }

            var result = new PortfolioPerformance
            {
                TotalValue = Math.Round(totalValue, 2),
                TotalInvested = Math.Round(totalInvestedPortfolio, 2),
                TotalGainLoss = Math.Round(totalGainLoss, 2),
                TotalGainLossPercent = Math.Round(totalGainLossPercent, 2),
                DayChange = Math.Round(dayChange, 2),
                DayChangePercent = totalValue != 0 ? Math.Round((dayChange / totalValue) * 100, 2) : 0,
                Holdings = holdings
            };

            _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        public async Task<PortfolioDiversification> GetDiversificationAsync(AppUser user)
        {
            var cacheKey = $"portfolio-diversification-{user.Id}";
            var cached = _cacheService.Get<PortfolioDiversification>(cacheKey);

            if (cached != null)
                return cached;

            var portfolios = await _context.portfolios
                .AsNoTracking()
                .Include(p => p.Stock)
                .Where(p => p.AppUserId == user.Id && p.Quantity > 0)
                .ToListAsync();

            var totalPortfolioValue = portfolios.Sum(p => p.Quantity * (p.Stock.CurrentPrice > 0 ? p.Stock.CurrentPrice : p.Stock.Purchase));

            var industries = portfolios
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Stock.Industry) ? "Unknown" : p.Stock.Industry)
                .Select(g =>
                {
                    var value = g.Sum(x => x.Quantity * (x.Stock.CurrentPrice > 0 ? x.Stock.CurrentPrice : x.Stock.Purchase));

                    return new IndustryAllocation
                    {
                        Industry = g.Key,
                        Value = Math.Round(value, 2),
                        Percentage = totalPortfolioValue > 0
                            ? Math.Round((value / totalPortfolioValue) * 100, 2)
                            : 0
                    };
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            var result = new PortfolioDiversification
            {
                Industries = industries
            };

            _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        public async Task<List<PortfolioValuePoint>> GetPortfolioHistoryAsync(AppUser user, int days)
        {
            days = days < 1 ? 30 : days;

            var cacheKey = $"portfolio-history-{user.Id}-{days}";
            var cached = _cacheService.Get<List<PortfolioValuePoint>>(cacheKey);

            if (cached != null)
                return cached;

            var portfolios = await _context.portfolios
                .AsNoTracking()
                .Include(p => p.Stock)
                .Where(p => p.AppUserId == user.Id && p.Quantity > 0)
                .ToListAsync();

            var history = new List<PortfolioValuePoint>();

            for (int i = days - 1; i >= 0; i--)
            {
                var targetDate = DateTime.UtcNow.Date.AddDays(-i);
                decimal dayValue = 0;

                foreach (var portfolio in portfolios)
                {
                    var historicalPrices = await _stockDataService.GetHistoricalPricesAsync(portfolio.Stock.Sympol, days + 5);

                    var point = historicalPrices
                        .OrderByDescending(h => h.Date)
                        .FirstOrDefault(h => h.Date.Date <= targetDate);

                    var price = point?.Close ?? 
                    (portfolio.Stock.CurrentPrice > 0 ? portfolio.Stock.CurrentPrice : portfolio.Stock.Purchase);
                    dayValue += portfolio.Quantity * price;
                }

                history.Add(new PortfolioValuePoint
                {
                    Date = targetDate,
                    Value = Math.Round(dayValue, 2)
                });
            }

            _cacheService.Set(cacheKey, history, TimeSpan.FromMinutes(30));
            return history;
        }

        public async Task<StockPerformanceMetrics?> GetStockPerformanceAsync(int stockId, AppUser? user = null)
        {
            var stock = await _context.Stocks
                .AsNoTracking()
                .Include(s => s.Ratings)
                .Include(s => s.Transactions)
                .FirstOrDefaultAsync(s => s.Id == stockId);

            if (stock == null)
                return null;

            var transactions = stock.Transactions.AsQueryable();

            if (user != null)
                transactions = transactions.Where(t => t.AppUserId == user.Id);

            return new StockPerformanceMetrics
            {
                StockId = stock.Id,
                Symbol = stock.Sympol,
                CompanyName = stock.CompanyName,
                CurrentPrice = stock.CurrentPrice,
                PriceChangePercent = stock.PriceChangePercent,
                LastPriceUpdate = stock.LastPriceUpdate,
                AverageRating = stock.Ratings.Any()? Math.Round((decimal)stock.Ratings.Average(r => r.Score), 2): 0,
                RatingCount = stock.Ratings.Count,
                TotalTransactions = transactions.Count(),
                TotalBuyQuantity = transactions.Where(t => t.Type == TransactionType.Buy).Sum(t => t.Quantity),
                TotalSellQuantity = transactions.Where(t => t.Type == TransactionType.Sell).Sum(t => t.Quantity)
            };
        }
    }
}