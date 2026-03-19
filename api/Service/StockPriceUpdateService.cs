using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Service
{
    public class StockPriceUpdateService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StockPriceUpdateService> _logger;

        public StockPriceUpdateService(
            IServiceScopeFactory scopeFactory,
            ILogger<StockPriceUpdateService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StockPriceUpdateService started.");

            await UpdateAllStockPricesAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                    await UpdateAllStockPricesAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in StockPriceUpdateService loop.");
                }
            }

            _logger.LogInformation("StockPriceUpdateService stopped.");
        }

        private async Task UpdateAllStockPricesAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var stockDataService = scope.ServiceProvider.GetRequiredService<IStockDataService>();

            var stocks = await context.Stocks.ToListAsync(stoppingToken);

            if (!stocks.Any())
            {
                _logger.LogInformation("No stocks found to update.");
                return;
            }

            _logger.LogInformation("Updating live prices for {Count} stocks...", stocks.Count);

            foreach (var stock in stocks)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    var liveData = await stockDataService.GetCurrentPriceAsync(stock.Sympol);

                    if (liveData == null)
                    {
                        _logger.LogWarning("No live data returned for stock {Symbol}", stock.Sympol);
                        continue;
                    }

                    stock.CurrentPrice = liveData.CurrentPrice;
                    stock.PriceChangePercent = liveData.ChangePercent;
                    stock.LastPriceUpdate = liveData.LastUpdated;

                    _logger.LogInformation(
                        "Updated {Symbol}: Price={Price}, Change%={ChangePercent}",
                        stock.Sympol,
                        stock.CurrentPrice,
                        stock.PriceChangePercent
                    );

                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update stock {Symbol}", stock.Sympol);
                }
            }

            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Finished updating stock prices.");
        }
    }
}