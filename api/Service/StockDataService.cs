using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Options;

namespace api.Service
{
    public class StockDataService : IStockDataService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly ILogger<StockDataService> _logger;
        private readonly StockApiSettings _settings;

        public StockDataService(
            HttpClient httpClient,
            ICacheService cacheService,
            ILogger<StockDataService> logger,
            IOptions<StockApiSettings> options)
        {
            _httpClient = httpClient;
            _cacheService = cacheService;
            _logger = logger;
            _settings = options.Value;
        }

        public async Task<StockPriceData?> GetCurrentPriceAsync(string symbol)
        {
            symbol = symbol.ToUpper().Trim();
            var cacheKey = $"stock-price-{symbol}";
            var cached = _cacheService.Get<StockPriceData>(cacheKey);

            if (cached != null)
                return cached;

            try
            {
                var url = $"{_settings.BaseUrl}?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_settings.ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch stock price for {Symbol}. Status: {StatusCode}", symbol, response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty("Global Quote", out var quote) || quote.ValueKind == JsonValueKind.Null || !quote.EnumerateObject().Any())
                {
                    _logger.LogWarning("No 'Global Quote' found for {Symbol}. Raw Response: {Json}", symbol, json.Length > 200 ? json.Substring(0, 200) + "..." : json);

                    if (document.RootElement.TryGetProperty("Note", out _) || document.RootElement.TryGetProperty("Information", out _))
                    {
                        _logger.LogWarning("Alpha Vantage API rate limit hit. Falling back to mock data for {Symbol}.", symbol);
                        var random = new Random();
                        var mockPrice = 150m + (decimal)(random.NextDouble() * 100);
                        var mockData = new StockPriceData
                        {
                            Symbol = symbol,
                            CurrentPrice = Math.Round(mockPrice, 2),
                            ChangeAmount = Math.Round((decimal)(random.NextDouble() * 5) - 2.5m, 2),
                            ChangePercent = Math.Round((decimal)(random.NextDouble() * 2) - 1m, 2),
                            LastUpdated = DateTime.UtcNow
                        };
                        _cacheService.Set(cacheKey, mockData, TimeSpan.FromMinutes(_settings.PriceCacheMinutes));
                        return mockData;
                    }

                    return null;
                }

                var data = new StockPriceData
                {
                    Symbol = quote.GetProperty("01. symbol").GetString() ?? symbol,
                    CurrentPrice = decimal.Parse(quote.GetProperty("05. price").GetString() ?? "0"),
                    ChangeAmount = decimal.Parse(quote.GetProperty("09. change").GetString() ?? "0"),
                    ChangePercent = ParsePercent(quote.GetProperty("10. change percent").GetString()),
                    LastUpdated = DateTime.UtcNow
                };

                _cacheService.Set(cacheKey, data, TimeSpan.FromMinutes(_settings.PriceCacheMinutes));
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current price for {Symbol}", symbol);
                return null;
            }
        }

        public async Task<List<HistoricalPrice>> GetHistoricalPricesAsync(string symbol, int days)
        {
            symbol = symbol.ToUpper().Trim();
            var cacheKey = $"stock-history-{symbol}-{days}";
            var cached = _cacheService.Get<List<HistoricalPrice>>(cacheKey);

            if (cached != null)
                return cached;

            try
            {
                var url = $"{_settings.BaseUrl}?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={_settings.ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch historical data for {Symbol}", symbol);
                    return new List<HistoricalPrice>();
                }

                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty("Time Series (Daily)", out var series))
                {
                    _logger.LogWarning("No 'Time Series (Daily)' found for {Symbol}. Raw Response: {Json}", symbol, json.Length > 200 ? json.Substring(0, 200) + "..." : json);
                    
                    if (document.RootElement.TryGetProperty("Note", out _) || document.RootElement.TryGetProperty("Information", out _))
                    {
                        _logger.LogWarning("Alpha Vantage API rate limit hit. Falling back to mock history for {Symbol}.", symbol);
                        var mockPrices = new List<HistoricalPrice>();
                        var random = new Random();
                        decimal currentBase = 150m;
                        for (int i = days; i >= 0; i--)
                        {
                            var open = currentBase + (decimal)(random.NextDouble() * 4 - 2);
                            mockPrices.Add(new HistoricalPrice
                            {
                                Date = DateTime.UtcNow.AddDays(-i),
                                Open = Math.Round(open, 2),
                                High = Math.Round(open + 2, 2),
                                Low = Math.Round(open - 2, 2),
                                Close = Math.Round(open + (decimal)(random.NextDouble() * 2 - 1), 2),
                                Volume = random.Next(1000000, 5000000)
                            });
                            currentBase = open;
                        }
                        _cacheService.Set(cacheKey, mockPrices, TimeSpan.FromMinutes(_settings.HistoricalCacheMinutes));
                        return mockPrices;
                    }
                    
                    return new List<HistoricalPrice>();
                }

                var prices = new List<HistoricalPrice>();

                foreach (var item in series.EnumerateObject().Take(days))
                {
                    var day = item.Value;

                    prices.Add(new HistoricalPrice
                    {
                        Date = DateTime.Parse(item.Name),
                        Open = decimal.Parse(day.GetProperty("1. open").GetString() ?? "0"),
                        High = decimal.Parse(day.GetProperty("2. high").GetString() ?? "0"),
                        Low = decimal.Parse(day.GetProperty("3. low").GetString() ?? "0"),
                        Close = decimal.Parse(day.GetProperty("4. close").GetString() ?? "0"),
                        Volume = long.Parse(day.GetProperty("5. volume").GetString() ?? "0")
                    });
                }

                var ordered = prices.OrderBy(p => p.Date).ToList();
                _cacheService.Set(cacheKey, ordered, TimeSpan.FromMinutes(_settings.HistoricalCacheMinutes));
                return ordered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical prices for {Symbol}", symbol);
                return new List<HistoricalPrice>();
            }
        }

        public async Task<CompanyInfo?> GetCompanyInfoAsync(string symbol)
        {
            symbol = symbol.ToUpper().Trim();
            var cacheKey = $"company-info-{symbol}";
            var cached = _cacheService.Get<CompanyInfo>(cacheKey);

            if (cached != null)
                return cached;

            try
            {
                var url = $"{_settings.BaseUrl}?function=OVERVIEW&symbol={symbol}&apikey={_settings.ApiKey}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch company info for {Symbol}", symbol);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);

                var root = document.RootElement;
                if (!root.TryGetProperty("Symbol", out _) || root.ValueKind == JsonValueKind.Null || !root.EnumerateObject().Any())
                {
                    _logger.LogWarning("No 'Overview' data found for {Symbol}. Raw Response: {Json}", symbol, json.Length > 200 ? json.Substring(0, 200) + "..." : json);
                    
                    if (root.TryGetProperty("Note", out _) || root.TryGetProperty("Information", out _))
                    {
                        _logger.LogWarning("Alpha Vantage API rate limit hit. Falling back to mock company info for {Symbol}.", symbol);
                        var mockInfo = new CompanyInfo
                        {
                            Symbol = symbol,
                            CompanyName = $"{symbol} Corp (Mock Data)",
                            Industry = "Technology",
                            MarketCap = 150000000000
                        };
                        _cacheService.Set(cacheKey, mockInfo, TimeSpan.FromHours(6));
                        return mockInfo;
                    }
                    
                    return null;
                }

                var info = new CompanyInfo
                {
                    Symbol = root.GetProperty("Symbol").GetString() ?? symbol,
                    CompanyName = root.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
                    Industry = root.TryGetProperty("Industry", out var industry) ? industry.GetString() ?? "" : "",
                    MarketCap = root.TryGetProperty("MarketCapitalization", out var marketCap)
                        ? long.Parse(marketCap.GetString() ?? "0")
                        : 0
                };

                _cacheService.Set(cacheKey, info, TimeSpan.FromHours(6));
                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company info for {Symbol}", symbol);
                return null;
            }
        }

        private decimal ParsePercent(string? percentText)
        {
            if (string.IsNullOrWhiteSpace(percentText))
                return 0;

            return decimal.Parse(percentText.Replace("%", ""));
        }
    }
}