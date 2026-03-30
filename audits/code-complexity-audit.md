# Code Complexity Audit — NETPRO

**Date:** 2026-03-30
**Scope:** All non-generated `.cs` files under `api/` (excluding `obj/`, `bin/`, `Migrations/`)

---

## 1. CYCLOMATIC COMPLEXITY

### Finding 1.1 — `TransactionController.CreateTransaction` (Cyclomatic Complexity ~12)

| | |
|---|---|
| **File** | `api/Controllers/TransactionController.cs:105-179` |
| **Importance** | 8/10 |

**Decision points:** null-check `appUser`, `Quantity <= 0`, `PricePerShare <= 0`, null-check `stock`, null-check `portfolioItem`, `Type == Sell`, quantity check inside Sell, `Type == Buy`, null-check `portfolioItem` inside Buy, `Type == Sell` (else-if), `Quantity == 0` inside Sell.

This method handles validation, transaction creation, AND portfolio mutation in a single 74-line action. It mixes HTTP-layer concerns with domain logic.

**Remediation:** Extract the portfolio-update logic into a service method.

```csharp
// New: ITransactionService (or add to existing ITransactionRepository)
Task<Transaction> ExecuteTransactionAsync(AppUser user, CreateTransactionDto dto);

// The service encapsulates:
//   1. Stock existence check
//   2. Sell-quantity validation
//   3. Transaction creation
//   4. Portfolio quantity update (buy/sell/remove)
//   5. SaveChanges
```

The controller shrinks to:

```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionDto dto)
{
    var appUser = await GetAuthenticatedUser();
    if (appUser == null) return Unauthorized();

    var result = await _transactionService.ExecuteTransactionAsync(appUser, dto);
    return Ok("Transaction created successfully");
}
```

---

### Finding 1.2 — `TransactionRepository.GetUserTransactionsAsync` (Cyclomatic Complexity ~11)

| | |
|---|---|
| **File** | `api/Repository/TransactionRepository.cs:165-220` |
| **Importance** | 5/10 |

Five `if` guards for query filters + nested ternaries for sort direction. Not dangerous, but the identical filtering logic is duplicated in `GetAllUserTransactionsForExportAsync` (lines 29-77).

**Remediation:** Extract shared filter-and-sort into a private method.

```csharp
private IQueryable<Transaction> ApplyFiltersAndSort(
    IQueryable<Transaction> query, TransactionQueryObject filter)
{
    if (!string.IsNullOrWhiteSpace(filter.StockSymbol))
        query = query.Where(t => t.Stock.Sympol.Contains(filter.StockSymbol));

    if (filter.Type.HasValue)
        query = query.Where(t => t.Type == filter.Type.Value);

    if (filter.StartDate.HasValue)
        query = query.Where(t => t.TransactionDate >= filter.StartDate.Value);

    if (filter.EndDate.HasValue)
        query = query.Where(t => t.TransactionDate <= filter.EndDate.Value);

    if (!string.IsNullOrWhiteSpace(filter.SortBy))
    {
        query = filter.SortBy.ToLower() switch
        {
            "date" => filter.IsDescending
                ? query.OrderByDescending(t => t.TransactionDate)
                : query.OrderBy(t => t.TransactionDate),
            "amount" => filter.IsDescending
                ? query.OrderByDescending(t => t.TotalAmount)
                : query.OrderBy(t => t.TotalAmount),
            _ => query.OrderByDescending(t => t.TransactionDate)
        };
    }
    else
    {
        query = query.OrderByDescending(t => t.TransactionDate);
    }

    return query;
}
```

Both `GetUserTransactionsAsync` and `GetAllUserTransactionsForExportAsync` call this, eliminating ~50 duplicated lines.

---

### Finding 1.3 — `StockRepository.GetAllAsync` (Cyclomatic Complexity ~7)

| | |
|---|---|
| **File** | `api/Repository/StockRepository.cs:41-67` |
| **Importance** | 3/10 |

Acceptable. Three filter branches + pagination. Follows the same pattern as `TransactionRepository` but is simpler.

---

### Finding 1.4 — `StockPriceUpdateService.UpdateAllStockPricesAsync` (Cyclomatic Complexity ~7)

| | |
|---|---|
| **File** | `api/Service/StockPriceUpdateService.cs:50-107` |
| **Importance** | 4/10 |

`foreach` with cancellation check, null-check on `liveData`, two catch blocks. Acceptable for a background service, but the 15-second delay between calls means updating 100 stocks takes 25 minutes — may exceed the 15-minute interval.

**Remediation:** Guard against overlapping runs.

```csharp
private int _running;

private async Task UpdateAllStockPricesAsync(CancellationToken ct)
{
    if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
    {
        _logger.LogWarning("Previous update still running, skipping.");
        return;
    }
    try { /* existing logic */ }
    finally { Interlocked.Exchange(ref _running, 0); }
}
```

---

## 2. COGNITIVE COMPLEXITY

### Finding 2.1 — `PortfolioAnalyticsService.GetPerformanceAsync` (Cognitive Complexity: HIGH)

| | |
|---|---|
| **File** | `api/Service/PortfolioAnalyticsService.cs:31-129` |
| **Importance** | 9/10 |

This 98-line method:
- Queries two separate tables (portfolios, buyTransactions)
- Iterates portfolios, then filters transactions per stock inside the loop (O(n*m))
- Computes average cost basis, current value, gain/loss, day change
- Then **re-iterates** the same `portfolios` list (lines 103-114) to compute `dayChange` — duplicating the price-resolution logic from lines 66-79

Mixed levels of abstraction: DB queries → financial math → DTO assembly all in one method.

**Remediation:** Split into focused helpers and eliminate the duplicate loop.

```csharp
// Accumulate dayChange inside the first foreach loop (remove lines 102-114):
decimal dayChange = 0;

foreach (var portfolio in portfolios)
{
    // ... existing holding calculation ...

    dayChange += dayChangeForHolding; // already computed on line 79

    holdings.Add(new StockHolding { /* ... */ });
}
```

This alone removes 12 lines and the duplicate price-resolution logic.

---

### Finding 2.2 — `PortfolioAnalyticsService.GetPortfolioHistoryAsync` — N+1 API call problem

| | |
|---|---|
| **File** | `api/Service/PortfolioAnalyticsService.cs:174-219` |
| **Importance** | 10/10 |

```
for (days)           // outer loop: e.g. 30 iterations
    foreach (portfolio)  // inner loop: e.g. 10 stocks
        await GetHistoricalPricesAsync(...)  // external API call
```

For 30 days × 10 stocks = **300 external API calls** per request. The cache mitigates repeat calls for the same symbol, but the first invocation still fires `days * stocks` calls.

**Remediation:** Fetch historical prices per-stock once, then index by date.

```csharp
// Pre-fetch all stock histories ONCE
var priceCache = new Dictionary<string, List<HistoricalPrice>>();
foreach (var portfolio in portfolios)
{
    if (!priceCache.ContainsKey(portfolio.Stock.Sympol))
        priceCache[portfolio.Stock.Sympol] =
            await _stockDataService.GetHistoricalPricesAsync(portfolio.Stock.Sympol, days + 5);
}

// Then build the history using the local dictionary
for (int i = days - 1; i >= 0; i--)
{
    var targetDate = DateTime.UtcNow.Date.AddDays(-i);
    decimal dayValue = 0;

    foreach (var portfolio in portfolios)
    {
        var prices = priceCache[portfolio.Stock.Sympol];
        var point = prices.OrderByDescending(h => h.Date).FirstOrDefault(h => h.Date.Date <= targetDate);
        var price = point?.Close
            ?? (portfolio.Stock.CurrentPrice > 0 ? portfolio.Stock.CurrentPrice : portfolio.Stock.Purchase);
        dayValue += portfolio.Quantity * price;
    }

    history.Add(new PortfolioValuePoint { Date = targetDate, Value = Math.Round(dayValue, 2) });
}
```

Reduces API calls from `days * stocks` to just `stocks`.

---

### Finding 2.3 — `TransactionRepository.GetRealizedGainLossAsync` — Nested loop with financial logic

| | |
|---|---|
| **File** | `api/Repository/TransactionRepository.cs:85-133` |
| **Importance** | 6/10 |

Two nested loops (group-by stock → iterate transactions). The FIFO gain/loss calculation is inherently sequential so the nesting is justified. Cognitive load is moderate.

**Remediation:** Consider extracting the inner loop into a named method for testability:

```csharp
private static decimal CalculateRealizedGainLoss(IEnumerable<Transaction> transactions)
{
    decimal totalCost = 0, realizedGainLoss = 0;
    int totalShares = 0;

    foreach (var t in transactions)
    {
        if (t.Type == TransactionType.Buy)
        {
            totalCost += t.TotalAmount;
            totalShares += t.Quantity;
        }
        else if (t.Type == TransactionType.Sell && totalShares > 0)
        {
            var avgCost = totalCost / totalShares;
            var costBasis = avgCost * t.Quantity;
            realizedGainLoss += t.TotalAmount - costBasis;
            totalCost -= costBasis;
            totalShares -= t.Quantity;
        }
    }

    return realizedGainLoss;
}
```

---

### Finding 2.4 — Repeated user-lookup boilerplate across controllers

| | |
|---|---|
| **File** | Every `[Authorize]` controller action |
| **Importance** | 7/10 |

The same 4 lines appear in **17 controller actions**:

```csharp
var username = User.GetUsername();
var appUser = await _userManager.FindByNameAsync(username);
if (appUser == null)
    return Unauthorized();
```

**Remediation:** Extract into a base controller or helper.

```csharp
public class AuthControllerBase : ControllerBase
{
    protected readonly UserManager<AppUser> _userManager;

    public AuthControllerBase(UserManager<AppUser> userManager)
        => _userManager = userManager;

    protected async Task<AppUser?> GetAuthenticatedUser()
    {
        var username = User.GetUsername();
        return await _userManager.FindByNameAsync(username);
    }
}
```

---

## 3. LINES OF CODE METRICS

### Functions over 50 lines

| Method | File | Lines | Importance |
|--------|------|-------|------------|
| `GetPerformanceAsync` | `PortfolioAnalyticsService.cs:31-129` | 98 | 9/10 |
| `CreateTransaction` | `TransactionController.cs:105-179` | 74 | 8/10 |
| `GetUserTransactionsAsync` | `TransactionRepository.cs:165-220` | 55 | 5/10 |
| `GetAllUserTransactionsForExportAsync` | `TransactionRepository.cs:29-77` | 48 | 5/10 |
| `GetPortfolioHistoryAsync` | `PortfolioAnalyticsService.cs:174-219` | 45 | 10/10 (due to API call issue) |
| `ExportTransactionsToCsv` | `TransactionController.cs:243-267` | 24 | 2/10 (fine) |

### Files over 300 lines

| File | Lines | Importance |
|------|-------|------------|
| `TransactionController.cs` | 270 | 4/10 |

No files exceed 300 lines. The codebase is well-distributed.

### Classes over 500 lines

None. Largest class is `TransactionRepository` at ~230 lines.

---

## 4. COUPLING METRICS

### Finding 4.1 — `TransactionController` has high efferent coupling (4 dependencies)

| | |
|---|---|
| **File** | `api/Controllers/TransactionController.cs:24-28` |
| **Importance** | 7/10 |

Depends on: `UserManager<AppUser>`, `ITransactionRepository`, `IStockRepository`, `ApplicationDBContext`.

The controller **directly accesses `ApplicationDBContext`** (lines 124-125, 159, 168, 172, 176) to manipulate `portfolios`, bypassing the `IPortfolioRepository` abstraction. This defeats the repository pattern.

**Instability Index:** High — changes to DbContext schema directly impact this controller.

**Remediation:** Move portfolio mutations into `IPortfolioRepository` or a new `ITransactionService`.

```csharp
// Add to IPortfolioRepository:
Task<Portfolio?> GetAsync(string userId, int stockId);
Task UpdateQuantityAsync(string userId, int stockId, int quantityDelta);
Task RemoveIfEmptyAsync(string userId, int stockId);
```

Then the controller no longer needs `ApplicationDBContext` at all.

---

### Finding 4.2 — `PortfolioAnalyticsService` has high efferent coupling (4 dependencies)

| | |
|---|---|
| **File** | `api/Service/PortfolioAnalyticsService.cs:14-17` |
| **Importance** | 5/10 |

Depends on: `ApplicationDBContext`, `IStockDataService`, `ICacheService`, `ILogger`. Direct DbContext access is acceptable in a service layer, but it queries both `portfolios` and `Transactions` tables directly rather than going through repositories.

**Remediation (optional):** If repositories already provide these queries, delegate to them. If not, this coupling is acceptable for a service.

---

### Finding 4.3 — `IStockRepository` is the most depended-upon interface (afferent coupling = 4)

| | |
|---|---|
| **Importance** | 3/10 (informational) |

Consumed by: `StockControllers`, `CommentController`, `RatingController`, `WatchListController`, `PortfolioController`, `TransactionController`.

This is expected — Stock is the central domain entity. No action needed.

---

### Finding 4.4 — Duplicated `TransactionDto` mapping in 3 controller actions

| | |
|---|---|
| **File** | `TransactionController.cs` lines 49-62, 85-98, 208-221 |
| **Importance** | 6/10 |

The same 12-field manual mapping from `Transaction` → `TransactionDto` appears three times.

**Remediation:** Add a `ToTransactionDto()` extension method in `Mappers/`:

```csharp
public static class TransactionMapper
{
    public static TransactionDto ToTransactionDto(this Transaction t) => new()
    {
        Id = t.Id,
        StockId = t.StockId,
        Symbol = t.Stock.Sympol,
        CompanyName = t.Stock.CompanyName,
        Type = t.Type.ToString(),
        Quantity = t.Quantity,
        PricePerShare = t.PricePerShare,
        TotalAmount = t.TotalAmount,
        TransactionDate = t.TransactionDate,
        Category = t.Category.ToString(),
        Notes = t.Notes
    };
}
```

---

## 5. COHESION ANALYSIS

### Finding 5.1 — `TransactionController` violates Single Responsibility

| | |
|---|---|
| **File** | `api/Controllers/TransactionController.cs` |
| **Importance** | 7/10 |

This controller handles:
1. CRUD for transactions (GET, POST)
2. Portfolio mutation logic (Buy → create/update portfolio, Sell → decrement/remove)
3. Summary/analytics (GET summary, realized gains)
4. Data export (CSV generation)

Four distinct responsibilities in one class.

**Remediation:** Split into:
- `TransactionController` — CRUD only (GET list, GET by id, POST create)
- `TransactionAnalyticsController` — summary, realized-gains, export

---

### Finding 5.2 — `PortfolioAnalyticsService` has good cohesion

| | |
|---|---|
| **File** | `api/Service/PortfolioAnalyticsService.cs` |
| **Importance** | 2/10 (positive finding) |

All four methods (`GetPerformanceAsync`, `GetDiversificationAsync`, `GetPortfolioHistoryAsync`, `GetStockPerformanceAsync`) are analytics over portfolio data. Focused and cohesive.

---

### Finding 5.3 — `StockDataService` has good cohesion

| | |
|---|---|
| **File** | `api/Service/StockDataService.cs` |
| **Importance** | 2/10 (positive finding) |

Three methods, all fetching external stock data. Well-focused.

---

### Finding 5.4 — `AccountContriller` (typo in filename) mixes Login and Registration

| | |
|---|---|
| **File** | `api/Controllers/AccountContriller.cs` |
| **Importance** | 3/10 |

Login + Register in the same controller is a common and acceptable pattern. The class name has a typo (`Contriller` → `Controller`).

---

## 6. ADDITIONAL FINDINGS

### Finding 6.1 — CSV export is vulnerable to injection

| | |
|---|---|
| **File** | `api/Controllers/TransactionController.cs:258-261` |
| **Importance** | 7/10 |

`CompanyName` values containing commas or quotes will break CSV parsing. Only `Notes` is sanitized (comma → space), but `CompanyName` and `Symbol` are not.

**Remediation:** Use proper CSV escaping:

```csharp
private static string CsvEscape(string? value)
{
    if (string.IsNullOrEmpty(value)) return "";
    if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        return $"\"{value.Replace("\"", "\"\"")}\"";
    return value;
}
```

Then use `CsvEscape(t.Stock.CompanyName)` etc. in the `AppendLine` call.

---

### Finding 6.2 — `Register` exposes raw exception to client

| | |
|---|---|
| **File** | `api/Controllers/AccountContriller.cs:97` |
| **Importance** | 8/10 |

```csharp
return StatusCode(500, e);  // leaks stack trace, internal paths, etc.
```

**Remediation:**

```csharp
catch (Exception e)
{
    _logger.LogError(e, "Registration failed");
    return StatusCode(500, "An error occurred during registration");
}
```

(Requires injecting `ILogger<AccountContriller>` into the controller.)

---

### Finding 6.3 — `CommentController.Delete` has broken route constraint

| | |
|---|---|
| **File** | `api/Controllers/CommentController.cs:96` |
| **Importance** | 9/10 |

```csharp
[Route ("{id}:int")]  // WRONG — colon is outside the brace
```

Should be:

```csharp
[Route("{id:int}")]
```

As written, the route is `api/comment/{id}:int` literally, meaning DELETE requests will fail with 404.

---

### Finding 6.4 — `PortfolioController.AddPortfolio` null-check after `CreateAsync` is dead code

| | |
|---|---|
| **File** | `api/Controllers/PortfolioController.cs:64-71` |
| **Importance** | 3/10 |

```csharp
await _portfolioRepo.CreateAsync(portfolioModel);

if(portfolioModel == null)  // portfolioModel was just constructed on line 56 — never null
```

**Remediation:** Remove the dead branch:

```csharp
await _portfolioRepo.CreateAsync(portfolioModel);
return Created();
```

---

### Finding 6.5 — `CommentController.Create` does not null-check `appUser`

| | |
|---|---|
| **File** | `api/Controllers/CommentController.cs:70-74` |
| **Importance** | 6/10 |

```csharp
var appUser = await _userManager.FindByNameAsync(username);
// No null check — will throw NullReferenceException if user not found
commentModel.AppUserId = appUser.Id;
```

Every other controller checks for null. This one doesn't, and the action also lacks `[Authorize]`.

**Remediation:**

```csharp
[HttpPost("{stockId:int}")]
[Authorize]                          // <-- add this
public async Task<IActionResult> Create(...)
{
    // ...
    var appUser = await _userManager.FindByNameAsync(username);
    if (appUser == null) return Unauthorized();   // <-- add this
```

---

## Summary Table

| # | Finding | Location | Importance | Category |
|---|---------|----------|------------|----------|
| 1.1 | `CreateTransaction` complexity ~12 | `TransactionController.cs:105` | 8/10 | Cyclomatic |
| 1.2 | Duplicated filter logic in TransactionRepo | `TransactionRepository.cs:29,165` | 5/10 | Cyclomatic |
| 1.3 | `GetAllAsync` complexity ~7 | `StockRepository.cs:41` | 3/10 | Cyclomatic |
| 1.4 | Background service overlap risk | `StockPriceUpdateService.cs:50` | 4/10 | Cyclomatic |
| 2.1 | `GetPerformanceAsync` duplicate loop + mixed abstraction | `PortfolioAnalyticsService.cs:31` | 9/10 | Cognitive |
| 2.2 | N+1 external API calls in `GetPortfolioHistoryAsync` | `PortfolioAnalyticsService.cs:174` | **10/10** | Cognitive |
| 2.3 | Nested loop in `GetRealizedGainLossAsync` | `TransactionRepository.cs:85` | 6/10 | Cognitive |
| 2.4 | Repeated user-lookup boilerplate (17 occurrences) | All `[Authorize]` controllers | 7/10 | Cognitive |
| 3.x | No files >300 lines, no classes >500 lines | — | 1/10 | LOC (pass) |
| 4.1 | `TransactionController` bypasses repository pattern | `TransactionController.cs:124` | 7/10 | Coupling |
| 4.2 | `PortfolioAnalyticsService` direct DbContext access | `PortfolioAnalyticsService.cs` | 5/10 | Coupling |
| 4.3 | `IStockRepository` high afferent coupling (expected) | — | 3/10 | Coupling |
| 4.4 | `TransactionDto` mapping duplicated 3x | `TransactionController.cs` | 6/10 | Coupling |
| 5.1 | `TransactionController` has 4 responsibilities | `TransactionController.cs` | 7/10 | Cohesion |
| 5.2 | `PortfolioAnalyticsService` — good cohesion | — | 2/10 | Cohesion (pass) |
| 5.3 | `StockDataService` — good cohesion | — | 2/10 | Cohesion (pass) |
| 6.1 | CSV export lacks proper escaping | `TransactionController.cs:258` | 7/10 | Bug |
| 6.2 | Raw exception exposed to client | `AccountContriller.cs:97` | 8/10 | Security |
| 6.3 | Broken route constraint on DELETE | `CommentController.cs:96` | 9/10 | Bug |
| 6.4 | Dead null-check after CreateAsync | `PortfolioController.cs:64` | 3/10 | Dead code |
| 6.5 | Missing null-check + missing `[Authorize]` | `CommentController.cs:59` | 6/10 | Bug |

---

## Priority Remediation Order

1. **Finding 6.3** — Broken DELETE route (immediate bug, 1-line fix)
2. **Finding 2.2** — N+1 API calls in portfolio history (performance catastrophe)
3. **Finding 6.2** — Exception leak in Register (security)
4. **Finding 2.1** — Duplicate loop in GetPerformanceAsync (12-line removal)
5. **Finding 1.1 / 4.1 / 5.1** — Extract transaction service + split controller
6. **Finding 6.5** — Missing auth on comment creation
7. **Finding 2.4** — Base controller for auth boilerplate
8. **Finding 4.4 / 1.2** — Extract shared mappers and filter logic
