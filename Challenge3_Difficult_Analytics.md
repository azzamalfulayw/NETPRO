# Challenge 3: Real-time Stock Price Integration & Portfolio Analytics (DIFFICULT)

**Estimated Time:** 12-16 hours
**Difficulty:** Difficult

## Objective
Integrate with a real stock market API to fetch live stock data and create comprehensive portfolio performance analytics. This challenge combines external API integration, background processing, caching, and complex business calculations.

## Background
Currently, stock prices are static values in the database. In the real world, stock prices change constantly. This challenge makes the application production-ready by:
- Fetching real-time stock prices from external APIs
- Updating prices automatically in the background
- Calculating real portfolio performance metrics
- Providing actionable analytics to users

## Part 1: External Stock API Integration

### 1. Choose a Stock API
Select one of these free stock market APIs:
- **Alpha Vantage** (recommended for beginners) - https://www.alphavantage.co/
- **Finnhub** - https://finnhub.io/
- **Polygon.io** - https://polygon.io/

Register for a free API key.

### 2. Create Configuration
Add API settings to `appsettings.json`:
```json
"StockApi": {
  "Provider": "AlphaVantage",
  "ApiKey": "YOUR_API_KEY",
  "BaseUrl": "https://www.alphavantage.co/query",
  "RateLimitPerMinute": 5
}
```

### 3. Create Stock API Service
Create `IStockDataService.cs` interface:
```csharp
public interface IStockDataService
{
    Task<StockPriceData?> GetCurrentPriceAsync(string symbol);
    Task<List<HistoricalPrice>> GetHistoricalPricesAsync(string symbol, int days);
    Task<CompanyInfo?> GetCompanyInfoAsync(string symbol);
}

public class StockPriceData
{
    public string Symbol { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class HistoricalPrice
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public long Volume { get; set; }
}
```

### 4. Implement Service
Create `StockDataService.cs`:
- Use `HttpClient` to make API requests
- Parse JSON responses (use `System.Text.Json` or `Newtonsoft.Json`)
- Handle API errors gracefully (rate limits, invalid symbols, network errors)
- Implement retry logic for failed requests
- Add logging for debugging

### 5. Implement Caching
Create `ICacheService.cs` and implement with `IMemoryCache`:
- Cache stock prices for 5-15 minutes
- Cache historical data for 1 hour
- Implement cache invalidation

Example:
```csharp
public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan expiration);
    void Remove(string key);
}
```

### 6. Background Price Update Service
Create a background service to update stock prices periodically:

Create `StockPriceUpdateService.cs` inheriting from `BackgroundService`:
```csharp
public class StockPriceUpdateService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Update prices for all stocks in database
            // Sleep for configured interval (e.g., 15 minutes)
        }
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddHostedService<StockPriceUpdateService>();
```

### 7. Update Stock Model
Add properties to track live prices:
```csharp
public decimal CurrentPrice { get; set; }
public decimal PriceChangePercent { get; set; }
public DateTime LastPriceUpdate { get; set; }
```

## Part 2: Portfolio Analytics

### 1. Create Analytics Models
Create `PortfolioPerformance.cs`:
```csharp
public class PortfolioPerformance
{
    public decimal TotalValue { get; set; } // Current value
    public decimal TotalInvested { get; set; } // Amount invested
    public decimal TotalGainLoss { get; set; } // Profit/Loss amount
    public decimal TotalGainLossPercent { get; set; } // Profit/Loss %
    public decimal DayChange { get; set; }
    public decimal DayChangePercent { get; set; }
    public List<StockHolding> Holdings { get; set; }
}

public class StockHolding
{
    public string Symbol { get; set; }
    public string CompanyName { get; set; }
    public int Quantity { get; set; }
    public decimal AverageCostBasis { get; set; } // Average price paid
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; } // Quantity × CurrentPrice
    public decimal TotalInvested { get; set; } // Quantity × AverageCostBasis
    public decimal GainLoss { get; set; }
    public decimal GainLossPercent { get; set; }
}
```

### 2. Create Portfolio Analytics Service
Create `IPortfolioAnalyticsService.cs`:
```csharp
Task<PortfolioPerformance> GetPerformanceAsync(AppUser user);
Task<List<PortfolioValuePoint>> GetPortfolioHistoryAsync(AppUser user, int days);
Task<PortfolioDiversification> GetDiversificationAsync(AppUser user);
Task<StockPerformanceMetrics> GetStockPerformanceAsync(int stockId);
```

### 3. Implement Complex Calculations

**Average Cost Basis Calculation:**
```csharp
// From user's BUY transactions, calculate weighted average price
var totalShares = buyTransactions.Sum(t => t.Quantity);
var totalCost = buyTransactions.Sum(t => t.TotalAmount);
var avgCostBasis = totalCost / totalShares;
```

**Portfolio Value Over Time:**
- For each day in the range, calculate portfolio value using historical prices
- Requires historical price data from API

**Diversification by Industry:**
- Group holdings by stock industry
- Calculate percentage allocation

### 4. Create Analytics Controller
Create `PortfolioAnalyticsController.cs`:

```csharp
[HttpGet("performance")]
[Authorize]
// GET /api/portfolio/performance - Get current portfolio performance
public async Task<IActionResult> GetPerformance()

[HttpGet("history")]
[Authorize]
// GET /api/portfolio/history?days=30 - Portfolio value over time
public async Task<IActionResult> GetPortfolioHistory([FromQuery] int days = 30)

[HttpGet("diversification")]
[Authorize]
// GET /api/portfolio/diversification - Industry allocation
public async Task<IActionResult> GetDiversification()
```

Create `StockAnalyticsController.cs`:
```csharp
[HttpGet("{id:int}/performance")]
// GET /api/stock/{id}/performance - Stock performance metrics
public async Task<IActionResult> GetStockPerformance([FromRoute] int id)
```

### 5. Implement Performance Optimization
- Cache expensive calculations (portfolio performance) for 5 minutes
- Use `.AsNoTracking()` for read-only queries
- Eager load related data with `.Include()` to avoid N+1 queries
- Consider using database views for complex aggregations

### 6. Error Handling & Resilience
Implement robust error handling:
- API rate limit exceeded → return cached data
- API unavailable → graceful degradation
- Invalid stock symbols → clear error messages
- Network timeouts → retry with exponential backoff

Use Polly library for resilience:
```bash
dotnet add package Polly
dotnet add package Microsoft.Extensions.Http.Polly
```

### 7. Create Unit Tests
Create test project and write tests for:
- Average cost basis calculation
- Gain/loss percentage calculation
- Portfolio value calculation
- API response parsing
- Cache behavior

Example:
```bash
dotnet new xunit -n api.Tests
dotnet add api.Tests reference api
```

## Success Criteria
- [ ] Successfully fetch real-time stock prices from external API
- [ ] Background service updates prices automatically
- [ ] Caching reduces API calls and improves performance
- [ ] Portfolio performance metrics are accurate
- [ ] Historical portfolio value chart works
- [ ] Diversification analysis works
- [ ] Stock performance metrics display correctly
- [ ] Error handling prevents crashes when API fails
- [ ] Unit tests cover core calculation logic
- [ ] API rate limits are respected

## Testing Steps
1. Test stock price API integration in isolation
2. Verify caching works (check logs, subsequent calls should be faster)
3. Test background service starts and updates prices
4. Create test portfolio with multiple stocks
5. Verify performance calculations:
   - Total value = sum of (quantity × current price)
   - Gain/loss matches expected values
6. Test historical chart with different time periods
7. Test error scenarios (invalid API key, network failure)
8. Run unit tests

## Bonus Challenges (Optional)
- [ ] Add support for multiple API providers with fallback
- [ ] Implement real-time price updates using WebSockets
- [ ] Add alerting (notify user when stock hits target price)
- [ ] Create endpoint to compare portfolio performance to market indexes (S&P 500)
- [ ] Add dividend tracking
- [ ] Generate PDF reports of portfolio performance
- [ ] Implement advanced charts (candlestick, moving averages)

## Tips
- Start with Part 1, get API integration working first
- Test API calls manually before implementing background service
- Use Postman or Swagger to understand API responses
- Log extensively during development
- Handle decimal precision carefully (use `decimal`, not `double`)
- Consider timezone issues (market hours, UTC vs local time)
- Read API documentation thoroughly (rate limits, data format)
- Implement caching early to avoid hitting rate limits during testing

## Common Pitfalls
- Not handling API rate limits → account suspended
- Ignoring API errors → application crashes
- Incorrect decimal calculations → wrong portfolio values
- Not caching → slow performance and excessive API calls
- Forgetting to handle sold stocks in calculations
- Not testing with real data → incorrect assumptions
- Synchronous API calls blocking threads → use async/await
- Not validating stock symbols before API calls

## Performance Optimization Checklist
- [ ] Use async/await throughout
- [ ] Implement caching for all external API calls
- [ ] Use `.AsNoTracking()` for read-only queries
- [ ] Eager load related entities to avoid N+1 queries
- [ ] Add database indexes on frequently queried columns
- [ ] Consider pagination for large result sets
- [ ] Profile and optimize slow endpoints

## Resources
- **Alpha Vantage Documentation:** https://www.alphavantage.co/documentation/
- **HttpClient Best Practices:** https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
- **Background Services:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
- **Memory Caching:** https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory
- **Polly (Resilience):** https://github.com/App-vNext/Polly
- **xUnit Testing:** https://xunit.net/

## Deliverables
1. Working external API integration with caching
2. Background service updating prices
3. Portfolio analytics endpoints with accurate calculations
4. Stock performance metrics
5. Error handling and logging
6. Unit tests for core logic
7. Updated API documentation in Swagger

This challenge will give you real-world experience with:
- External API integration
- Background processing
- Caching strategies
- Complex business logic
- Performance optimization
- Testing
- Production-ready code

Good luck! This is the most challenging task, but also the most rewarding!
