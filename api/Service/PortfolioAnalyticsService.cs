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
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<PortfolioAnalyticsService> _logger;

        public PortfolioAnalyticsService(
            ApplicationDBContext context,
            IStockDataService stockDataService,
            IRedisCacheService redisCacheService,
            ILogger<PortfolioAnalyticsService> logger)
        {
            _context = context;
            _stockDataService = stockDataService;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        public async Task<PortfolioPerformance> GetPerformanceAsync(AppUser user)
        {
            var cacheKey = $"portfolio:analytics:{user.Id}";
            return await _redisCacheService.GetOrAddAsync(cacheKey, async () =>
            {
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
                        Symbol = portfolio.Stock.Symbol,
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

                return new PortfolioPerformance
                {
                    TotalValue = Math.Round(totalValue, 2),
                    TotalInvested = Math.Round(totalInvestedPortfolio, 2),
                    TotalGainLoss = Math.Round(totalGainLoss, 2),
                    TotalGainLossPercent = Math.Round(totalGainLossPercent, 2),
                    DayChange = Math.Round(dayChange, 2),
                    DayChangePercent = totalValue != 0 ? Math.Round((dayChange / totalValue) * 100, 2) : 0,
                    Holdings = holdings
                };
            }, TimeSpan.FromMinutes(10)) ?? new PortfolioPerformance();
        }

        public async Task<PortfolioDiversification> GetDiversificationAsync(AppUser user)
        {
            var cacheKey = $"portfolio:diversification:{user.Id}";
            return await _redisCacheService.GetOrAddAsync(cacheKey, async () =>
            {
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

                return new PortfolioDiversification
                {
                    Industries = industries
                };
            }, TimeSpan.FromMinutes(10)) ?? new PortfolioDiversification();
        }

        public async Task<List<PortfolioValuePoint>> GetPortfolioHistoryAsync(AppUser user, int days)
        {
            days = days < 1 ? 30 : days;

            var cacheKey = $"portfolio:history:{user.Id}:{days}";
            return await _redisCacheService.GetOrAddAsync(cacheKey, async () =>
            {
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
                        var historicalPrices = await _stockDataService.GetHistoricalPricesAsync(portfolio.Stock.Symbol, days + 5);

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

                return history;
            }, TimeSpan.FromMinutes(30)) ?? new List<PortfolioValuePoint>();
        }

        public async Task<StockPerformanceMetrics?> GetStockPerformanceAsync(int stockId, AppUser? user = null)
        {
            var cacheKey = $"portfolio:stock-performance:{stockId}:{(user?.Id ?? "anonymous")}";
            return await _redisCacheService.GetOrAddAsync(cacheKey, async () =>
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
                    Symbol = stock.Symbol,
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
            }, TimeSpan.FromMinutes(10));
        }
    }
}