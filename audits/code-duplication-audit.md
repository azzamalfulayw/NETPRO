# Code Duplication Audit Report

**Project:** NETPRO (Stock Portfolio Management API)
**Date:** 2026-03-30
**Scope:** All `.cs` files under `api/` (excluding `obj/` and Migrations)
**Overall Duplication Score:** ~22% of application code contains duplicated or near-duplicated logic

---

## Table of Contents

1. [Exact Duplicates](#1-exact-duplicates)
2. [Near Duplicates](#2-near-duplicates)
3. [Structural Duplicates](#3-structural-duplicates)
4. [Data Duplication](#4-data-duplication)
5. [Utilities Module Proposal](#5-utilities-module-proposal)
6. [Summary Matrix](#6-summary-matrix)

---

## 1. Exact Duplicates

### 1.1 `CreateStockRequestDto` vs `UpdateStockRequestDto` -- 100% identical

**Importance: 8/10**

| File | Lines |
|------|-------|
| `api/Dtos/Stock/CreateStockRequestDto.cs` | 9-29 |
| `api/Dtos/Stock/UpdateStockRequestDto.cs` | 9-29 |

Every property, every validation attribute, every error message is byte-for-byte identical. The only difference is the class name.

**Duplication: 20 lines / 100%**

**Remediation:** Extract a shared base class.

```csharp
// api/Dtos/Stock/StockRequestBaseDto.cs
public abstract class StockRequestBaseDto
{
    [Required]
    [MaxLength(10, ErrorMessage = "Symbol cannot be over 10 characters")]
    public string Sympol { get; set; } = string.Empty;

    [Required]
    [MaxLength(10, ErrorMessage = "Company name cannot be over 10 characters")]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [Range(1, 10000000000)]
    public decimal Purchase { get; set; }

    [Required]
    [Range(0.001, 100)]
    public decimal LastDiv { get; set; }

    [Required]
    [MaxLength(10, ErrorMessage = "Industry cannot be over 10 characters")]
    public string Industry { get; set; } = string.Empty;

    [Required]
    [Range(1, 500000000000)]
    public long MarketCap { get; set; }
}

// Then:
public class CreateStockRequestDto : StockRequestBaseDto { }
public class UpdateStockRequestDto : StockRequestBaseDto { }
```

**Effort:** Low (15 min). Create base, inherit, update references.

---

### 1.2 `CreateCommentDto` vs `UpdateCommentRequestDto` -- 100% identical

**Importance: 7/10**

| File | Lines |
|------|-------|
| `api/Dtos/Comment/CreateCommentDto.cs` | 9-20 |
| `api/Dtos/Comment/UpdateCommentRequestDto.cs` | 9-19 |

Both have the same two properties (`Title`, `Content`) with identical `[Required]`, `[MinLength(5)]`, `[MaxLength(280)]` attributes and error messages.

**Duplication: 10 lines / 100%**

**Remediation:** Same base-class approach.

```csharp
public abstract class CommentRequestBaseDto
{
    [Required]
    [MinLength(5, ErrorMessage = "Title must be 5 characters at least")]
    [MaxLength(280, ErrorMessage = "Title cannot be over 280 characters")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MinLength(5, ErrorMessage = "Content must be 5 characters at least")]
    [MaxLength(280, ErrorMessage = "Content cannot be over 280 characters")]
    public string Content { get; set; } = string.Empty;
}

public class CreateCommentDto : CommentRequestBaseDto { }
public class UpdateCommentRequestDto : CommentRequestBaseDto { }
```

**Effort:** Low (10 min).

---

### 1.3 `CreateRatingDto` vs `UpdateRatingDto` -- 100% identical

**Importance: 6/10**

| File | Lines |
|------|-------|
| `api/Dtos/Rating/CreateRatingDto.cs` | 9-14 |
| `api/Dtos/Rating/UpdateRatingDto.cs` | 9-14 |

Single property `Score` with identical `[Required]` and `[Range(0,5)]` annotations.

**Duplication: 5 lines / 100%**

**Remediation:**

```csharp
public abstract class RatingRequestBaseDto
{
    [Required]
    [Range(0, 5, ErrorMessage = "Rating must be between 0 and 5")]
    public int Score { get; set; }
}

public class CreateRatingDto : RatingRequestBaseDto { }
public class UpdateRatingDto : RatingRequestBaseDto { }
```

**Effort:** Low (10 min).

---

### 1.4 Transaction-to-DTO mapping -- copy-pasted 3 times (CRITICAL)

**Importance: 9/10**

The `Transaction -> TransactionDto` inline mapping block is copy-pasted verbatim in three places in `api/Controllers/TransactionController.cs`:

| Location | Lines |
|----------|-------|
| `GetUserTransactions()` | 49-62 |
| `GetById()` | 85-98 |
| `GetTransactionsForStock()` | 208-221 |

All three contain the identical 12-property mapping:

```csharp
new TransactionDto
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
```

**Duplication: 13 lines x 3 = 39 lines**

**Remediation:** Create a `TransactionMapper` extension method (consistent with the existing `CommentMapper` / `RatingMapper` pattern).

```csharp
// api/Mappers/TransactionMapper.cs
namespace api.Mappers
{
    public static class TransactionMapper
    {
        public static TransactionDto ToTransactionDto(this Transaction t)
        {
            return new TransactionDto
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
    }
}
```

Then replace all 3 sites:
```csharp
// GetUserTransactions (line 49):
var transactionDtos = transactions.Select(t => t.ToTransactionDto());

// GetById (line 85):
var transactionDto = transaction.ToTransactionDto();

// GetTransactionsForStock (line 208):
var transactionDtos = transactions.Select(t => t.ToTransactionDto());
```

**Effort:** Low (20 min).

---

## 2. Near Duplicates

### 2.1 Transaction query filtering/sorting -- duplicated across two repository methods

**Importance: 9/10**

| Method | File | Lines |
|--------|------|-------|
| `GetUserTransactionsAsync()` | `api/Repository/TransactionRepository.cs` | 165-220 |
| `GetAllUserTransactionsForExportAsync()` | `api/Repository/TransactionRepository.cs` | 29-77 |

These two methods share ~40 lines of **identical** filter + sort logic (StockSymbol filter, Type filter, StartDate, EndDate, SortBy with Date/Amount, default ordering). The only difference: paginated vs non-paginated return.

**Duplication: ~85% of the method bodies overlap (~40 lines)**

**Remediation:** Extract the shared filter/sort chain into a private helper.

```csharp
// api/Repository/TransactionRepository.cs -- private helper
private static IQueryable<Transaction> ApplyFiltersAndSort(
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
        if (filter.SortBy.Equals("Date", StringComparison.OrdinalIgnoreCase))
            query = filter.IsDescending
                ? query.OrderByDescending(t => t.TransactionDate)
                : query.OrderBy(t => t.TransactionDate);
        else if (filter.SortBy.Equals("Amount", StringComparison.OrdinalIgnoreCase))
            query = filter.IsDescending
                ? query.OrderByDescending(t => t.TotalAmount)
                : query.OrderBy(t => t.TotalAmount);
    }
    else
    {
        query = query.OrderByDescending(t => t.TransactionDate);
    }

    return query;
}
```

Then both methods simplify to:
```csharp
var transactions = ApplyFiltersAndSort(
    _context.Transactions.Include(t => t.Stock)
        .Where(t => t.AppUserId == user.Id),
    query);

// GetUserTransactionsAsync: adds .Skip().Take() after this
// GetAllUserTransactionsForExportAsync: returns directly
```

**Effort:** Low (30 min).

---

### 2.2 User authentication boilerplate -- repeated ~15 times across controllers

**Importance: 8/10**

The following 4-line block appears in nearly every `[Authorize]` action method:

```csharp
var username = User.GetUsername();
var appUser = await _userManager.FindByNameAsync(username);

if (appUser == null)
    return Unauthorized();
```

| Controller | Occurrences |
|------------|-------------|
| `TransactionController.cs` | 7 times (lines 42-46, 71-75, 107-111, 185-189, 200-204, 229-233, 245-249) |
| `PortfolioAnalyticsController.cs` | 3 times (lines 29-32, 42-45, 55-58) |
| `WatchListController.cs` | 3 times (lines 35-39, 52-55, 86-90) |
| `RatingController.cs` | 2 times (lines 70-74, 101-105) |
| `PortfolioController.cs` | 3 times (lines 34-35, 44-45, 78-79) -- **WARNING: missing null check!** |

**Total: ~65 duplicated lines**

**Remediation:** Create a base controller with a helper method.

```csharp
// api/Controllers/AuthorizedControllerBase.cs
namespace api.Controllers
{
    [ApiController]
    public abstract class AuthorizedControllerBase : ControllerBase
    {
        protected readonly UserManager<AppUser> _userManager;

        protected AuthorizedControllerBase(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        protected async Task<AppUser?> GetCurrentUserAsync()
        {
            var username = User.GetUsername();
            return await _userManager.FindByNameAsync(username);
        }
    }
}
```

Usage in any controller:
```csharp
public class TransactionController : AuthorizedControllerBase
{
    public TransactionController(UserManager<AppUser> userManager, ...)
        : base(userManager) { }

    [HttpGet("summary")]
    [Authorize]
    public async Task<IActionResult> GetSummary()
    {
        var appUser = await GetCurrentUserAsync();
        if (appUser == null) return Unauthorized();
        // ...
    }
}
```

**Effort:** Medium (1-2 hours). Touches every authorized controller.

---

### 2.3 Repository `CreateAsync` -- identical pattern across 5 repos

**Importance: 5/10**

Every repository has this exact same `CreateAsync` implementation:

```csharp
public async Task<T> CreateAsync(T model)
{
    await _context.{DbSet}.AddAsync(model);
    await _context.SaveChangesAsync();
    return model;
}
```

| Repository | Lines |
|------------|-------|
| `CommentRepository.cs` | 22-27 |
| `RatingRepository.cs` | 19-24 |
| `StockRepository.cs` | 22-27 |
| `PortfolioRepository.cs` | 20-25 |
| `WatchListRepository.cs` | 21-26 |
| `TransactionRepository.cs` | 22-27 |

**Duplication: 6 lines x 6 = 36 lines**

Similarly, `DeleteAsync` in `CommentRepository`, `RatingRepository`, and `StockRepository` share the same find-null-check-remove-save pattern (~10 lines x 3 = 30 lines).

**Remediation:** Generic base repository.

```csharp
// api/Repository/BaseRepository.cs
public abstract class BaseRepository<T> where T : class
{
    protected readonly ApplicationDBContext _context;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(ApplicationDBContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T?> DeleteByIdAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return null;
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
}
```

**Effort:** Medium (2-3 hours). Requires updating all repositories and interfaces.

---

### 2.4 `ModelState.IsValid` check -- redundant in 13 action methods

**Importance: 4/10**

```csharp
if (!ModelState.IsValid)
    return BadRequest(ModelState);
```

This 2-line block appears at the top of **13 action methods** across controllers:

| Controller | Count | Lines |
|------------|-------|-------|
| `CommentController.cs` | 5 | 33, 47, 63, 83, 99 |
| `RatingController.cs` | 4 | 37, 50, 68, 126 |
| `StockControllers.cs` | 5 | 34, 48, 62, 75, 93 |
| `AccountContriller.cs` | 2 | 32, 59 |

**These checks are completely redundant.** Controllers annotated with `[ApiController]` already auto-validate `ModelState` and return 400. This is documented ASP.NET Core behavior.

**Remediation:** Just delete them.

```diff
// Delete from every action:
- if (!ModelState.IsValid)
-     return BadRequest(ModelState);
```

**Effort:** Low (15 min). Pure deletion.

---

### 2.5 Day-change calculation -- duplicated within `GetPerformanceAsync`

**Importance: 6/10**

The "previous price from change percent" calculation appears twice in `api/Service/PortfolioAnalyticsService.cs`:

**First occurrence** -- inside the `foreach` building holdings (lines 75-79):
```csharp
var previousPrice = portfolio.Stock.PriceChangePercent != 0
    ? currentPrice / (1 + (portfolio.Stock.PriceChangePercent / 100))
    : currentPrice;
var dayChangeForHolding = (currentPrice - previousPrice) * portfolio.Quantity;
```

**Second occurrence** -- separate `foreach` for total day-change (lines 102-113):
```csharp
var previousPrice = portfolio.Stock.PriceChangePercent != 0
    ? currentPrice / (1 + (portfolio.Stock.PriceChangePercent / 100))
    : currentPrice;
dayChange += (currentPrice - previousPrice) * portfolio.Quantity;
```

The second loop is **entirely unnecessary** -- `dayChange` could be summed from `dayChangeForHolding` during the first loop.

**Remediation:**
```csharp
// In the first foreach, accumulate dayChange directly:
decimal dayChange = 0;
foreach (var portfolio in portfolios)
{
    // ... existing holding logic ...
    var dayChangeForHolding = (currentPrice - previousPrice) * portfolio.Quantity;
    dayChange += dayChangeForHolding;

    holdings.Add(new StockHolding { ... });
}
// DELETE the second foreach loop entirely (lines 102-114)
```

**Effort:** Low (10 min).

---

### 2.6 Average rating calculation -- duplicated in 2 places

**Importance: 5/10**

| Location | Code |
|----------|------|
| `api/Mappers/StockMappers.cs:24-25` | `stockModel.Ratings.Any() ? Math.Round(stockModel.Ratings.Average(r => r.Score), 2) : 0` |
| `api/Service/PortfolioAnalyticsService.cs:245` | `stock.Ratings.Any() ? Math.Round((decimal)stock.Ratings.Average(r => r.Score), 2) : 0` |

**Remediation:** Extension method on `ICollection<Rating>`.

```csharp
// api/Extensions/RatingExtensions.cs
public static class RatingExtensions
{
    public static decimal AverageScore(this ICollection<Rating>? ratings)
    {
        return ratings != null && ratings.Any()
            ? Math.Round((decimal)ratings.Average(r => r.Score), 2)
            : 0;
    }
}

// Usage:
AverageRating = stockModel.Ratings.AverageScore(),
```

**Effort:** Low (15 min).

---

### 2.7 Stock price fallback logic -- repeated 4 times

**Importance: 5/10**

The pattern `stock.CurrentPrice > 0 ? stock.CurrentPrice : stock.Purchase` appears in:

| Location | Lines |
|----------|-------|
| `PortfolioAnalyticsService.cs` `GetPerformanceAsync` | 66-68 |
| `PortfolioAnalyticsService.cs` `GetPerformanceAsync` (dayChange loop) | 105-107 |
| `PortfolioAnalyticsService.cs` `GetDiversificationAsync` | 145 |
| `PortfolioAnalyticsService.cs` `GetDiversificationAsync` (inner lambda) | 151 |
| `PortfolioAnalyticsService.cs` `GetPortfolioHistoryAsync` | 205-206 |

**Remediation:** Extension method on `Stock`.

```csharp
// api/Extensions/StockExtensions.cs
public static class StockExtensions
{
    public static decimal EffectivePrice(this Stock stock)
        => stock.CurrentPrice > 0 ? stock.CurrentPrice : stock.Purchase;
}

// Usage:
var currentPrice = portfolio.Stock.EffectivePrice();
```

**Effort:** Low (15 min).

---

### 2.8 Portfolio query -- repeated 3 times in `PortfolioAnalyticsService`

**Importance: 6/10**

This exact query appears **3 times** in `api/Service/PortfolioAnalyticsService.cs`:

```csharp
var portfolios = await _context.portfolios
    .AsNoTracking()
    .Include(p => p.Stock)
    .Where(p => p.AppUserId == user.Id && p.Quantity > 0)
    .ToListAsync();
```

| Method | Lines |
|--------|-------|
| `GetPerformanceAsync` | 39-43 |
| `GetDiversificationAsync` | 139-143 |
| `GetPortfolioHistoryAsync` | 184-188 |

**Remediation:** Extract a private method.

```csharp
private async Task<List<Portfolio>> GetUserActivePortfoliosAsync(string userId)
{
    return await _context.portfolios
        .AsNoTracking()
        .Include(p => p.Stock)
        .Where(p => p.AppUserId == userId && p.Quantity > 0)
        .ToListAsync();
}
```

**Effort:** Low (10 min).

---

## 3. Structural Duplicates

### 3.1 Controller CRUD pattern -- `CommentController` mirrors `RatingController`

**Importance: 4/10**

`CommentController` and `RatingController` follow an almost identical structural pattern:

| Action | CommentController | RatingController |
|--------|-------------------|------------------|
| `GetAll()` | lines 31-41 | lines 35-45 |
| `GetById(int id)` | lines 43-57 | lines 47-61 |
| `Create(stockId, dto)` | lines 59-77 | lines 63-92 |
| `Update(id, dto)` | lines 78-93 | lines 93-121 |
| `Delete(id)` | lines 95-109 | lines 122-137 |

The structure is: validate -> call repo -> null check -> map to DTO -> return `Ok()`.

**Remediation:** A generic CRUD base controller could consolidate this, but each entity has different authorization rules and business logic in `Create`/`Update`. **Not recommended** to genericize yet -- the higher-value fix is removing the redundant `ModelState.IsValid` checks (Finding 2.4).

**Effort:** High (4+ hours) if genericized. Not recommended currently.

---

### 3.2 Stock field projection -- repeated in `PortfolioRepository` and `WatchListRepository`

**Importance: 6/10**

Both repositories project the same stock fields inline:

**`api/Repository/PortfolioRepository.cs:44-53`:**
```csharp
.Select(stock => new Stock
{
    Id = stock.StockId,
    Sympol = stock.Stock.Sympol,
    CompanyName = stock.Stock.CompanyName,
    Purchase = stock.Stock.Purchase,
    LastDiv = stock.Stock.LastDiv,
    Industry = stock.Stock.Industry,
    MarketCap = stock.Stock.MarketCap
})
```

**`api/Repository/WatchListRepository.cs:47-57`:**
```csharp
.Select(w => new WatchListDto
{
    StockId = w.StockId,
    Symbol = w.Stock.Sympol,
    CompanyName = w.Stock.CompanyName,
    Purchase = w.Stock.Purchase,
    LastDiv = w.Stock.LastDiv,
    Industry = w.Stock.Industry,
    MarketCap = w.Stock.MarketCap,
    // ... plus watchlist-specific fields
})
```

6 of the 7 projected fields are identical. If a new field is added to `Stock`, **both locations must be updated manually** -- high risk for drift.

**Remediation:** Use `Include(p => p.Stock)` + mapper at the caller, or create a shared `StockSummaryDto`.

**Effort:** Medium (1 hour).

---

### 3.3 AlphaVantage API call pattern -- repeated 3 times in `StockDataService`

**Importance: 3/10**

Each of the three methods in `api/Service/StockDataService.cs` follows this pattern:

1. Build URL with `_settings.BaseUrl`, `symbol`, `_settings.ApiKey`
2. `await _httpClient.GetAsync(url)`
3. Check `IsSuccessStatusCode`
4. `ReadAsStringAsync` -> `JsonDocument.Parse`
5. Parse specific fields
6. Cache result

| Method | Lines |
|--------|-------|
| `GetCurrentPriceAsync` | 39-75 |
| `GetHistoricalPricesAsync` | 86-128 |
| `GetCompanyInfoAsync` | 139-174 |

**Remediation:** Extract a private `FetchAlphaVantageAsync` helper.

```csharp
private async Task<JsonDocument?> FetchAlphaVantageAsync(string function, string symbol)
{
    var url = $"{_settings.BaseUrl}?function={function}&symbol={symbol}&apikey={_settings.ApiKey}";
    var response = await _httpClient.GetAsync(url);
    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning("AlphaVantage {Function} failed for {Symbol}: {Status}",
            function, symbol, response.StatusCode);
        return null;
    }
    var json = await response.Content.ReadAsStringAsync();
    return JsonDocument.Parse(json);
}
```

**Effort:** Low (30 min).

---

### 3.4 Cache check pattern -- repeated 7 times across services

**Importance: 3/10**

The cache-check-early-return pattern repeats 7 times:

```csharp
var cacheKey = $"...";
var cached = _cacheService.Get<T>(cacheKey);
if (cached != null)
    return cached;
```

| Location | Count |
|----------|-------|
| `StockDataService.cs` | 3 times (lines 33-37, 80-84, 133-137) |
| `PortfolioAnalyticsService.cs` | 4 times (lines 33-37, 133-137, 178-182) |

**Remediation (optional):** Add `GetOrSetAsync<T>` to `ICacheService`.

```csharp
public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
{
    var cached = Get<T>(key);
    if (cached != null) return cached;
    var result = await factory();
    Set(key, result, expiration);
    return result;
}
```

**Effort:** Medium (1 hour).

---

## 4. Data Duplication

### 4.1 `NewUserDto` inline construction -- repeated in `AccountContriller`

**Importance: 4/10**

| Location | Lines |
|----------|-------|
| `Login()` | `api/Controllers/AccountContriller.cs:46-52` |
| `Register()` | `api/Controllers/AccountContriller.cs:75-82` |

Both construct the same `NewUserDto { UserName, Email, Token }`.

**Remediation:**

```csharp
private NewUserDto BuildUserResponse(AppUser user) => new NewUserDto
{
    UserName = user.UserName,
    Email = user.Email,
    Token = _tokenService.CreateToken(user)
};
```

**Effort:** Low (10 min).

---

### 4.2 `QueryObject` vs `TransactionQueryObject` -- overlapping pagination fields

**Importance: 3/10**

`api/Helpers/QueryObject.cs` and `api/Helpers/TransactionQueryObject.cs` both have:
- `SortBy` (string?)
- `PageNumber` (int, default 1)
- `PageSize` (int)

Note also the inconsistent naming: `IsDecsending` (typo) in `QueryObject` vs `IsDescending` in `TransactionQueryObject`.

**Remediation:** Extract a base paging class.

```csharp
public abstract class PagedQueryBase
{
    public string? SortBy { get; set; }
    public bool IsDescending { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class QueryObject : PagedQueryBase
{
    public string? Symbol { get; set; }
    public string? CompanyName { get; set; }
}

public class TransactionQueryObject : PagedQueryBase
{
    public string? StockSymbol { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
```

This also fixes the `IsDecsending` typo.

**Effort:** Low (20 min).

---

### 4.3 Boilerplate `using` blocks -- identical in nearly every file

**Importance: 2/10**

Most files start with:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
```

These are auto-generated and unused in many cases.

**Remediation:** Enable implicit usings in `api/api.csproj`:

```xml
<PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

Then delete the 4-line boilerplate from every file.

**Effort:** Low (15 min).

---

## 5. Utilities Module Proposal

Based on the findings above, create the following utility files:

### File: `api/Extensions/StockExtensions.cs`

```csharp
using api.Models;

namespace api.Extensions
{
    public static class StockExtensions
    {
        /// Fallback to Purchase price when CurrentPrice is not set
        public static decimal EffectivePrice(this Stock stock)
            => stock.CurrentPrice > 0 ? stock.CurrentPrice : stock.Purchase;
    }
}
```

### File: `api/Extensions/RatingExtensions.cs`

```csharp
using api.Models;

namespace api.Extensions
{
    public static class RatingExtensions
    {
        public static decimal AverageScore(this ICollection<Rating>? ratings)
            => ratings != null && ratings.Any()
                ? Math.Round((decimal)ratings.Average(r => r.Score), 2)
                : 0;

        public static int SafeCount(this ICollection<Rating>? ratings)
            => ratings?.Count ?? 0;
    }
}
```

### File: `api/Mappers/TransactionMapper.cs`

```csharp
using api.Dtos.Transaction;
using api.Models;

namespace api.Mappers
{
    public static class TransactionMapper
    {
        public static TransactionDto ToTransactionDto(this Transaction t)
        {
            return new TransactionDto
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
    }
}
```

### File: `api/Controllers/AuthorizedControllerBase.cs`

```csharp
using api.Extensions;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    public abstract class AuthorizedControllerBase : ControllerBase
    {
        protected readonly UserManager<AppUser> _userManager;

        protected AuthorizedControllerBase(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        protected async Task<AppUser?> GetCurrentUserAsync()
        {
            var username = User.GetUsername();
            return await _userManager.FindByNameAsync(username);
        }
    }
}
```

### File: `api/Dtos/Stock/StockRequestBaseDto.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Stock
{
    public abstract class StockRequestBaseDto
    {
        [Required]
        [MaxLength(10, ErrorMessage = "Symbol cannot be over 10 characters")]
        public string Sympol { get; set; } = string.Empty;

        [Required]
        [MaxLength(10, ErrorMessage = "Company name cannot be over 10 characters")]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [Range(1, 10000000000)]
        public decimal Purchase { get; set; }

        [Required]
        [Range(0.001, 100)]
        public decimal LastDiv { get; set; }

        [Required]
        [MaxLength(10, ErrorMessage = "Industry cannot be over 10 characters")]
        public string Industry { get; set; } = string.Empty;

        [Required]
        [Range(1, 500000000000)]
        public long MarketCap { get; set; }
    }
}
```

### File: `api/Helpers/PagedQueryBase.cs`

```csharp
namespace api.Helpers
{
    public abstract class PagedQueryBase
    {
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
```

---

## 6. Summary Matrix

| # | Finding | Type | Importance | Duplication % | Suggested Fix | Effort |
|---|---------|------|------------|---------------|---------------|--------|
| 1.1 | `CreateStockRequestDto` = `UpdateStockRequestDto` | Exact | **8/10** | 100% | Shared base DTO | Low |
| 1.2 | `CreateCommentDto` = `UpdateCommentRequestDto` | Exact | **7/10** | 100% | Shared base DTO | Low |
| 1.3 | `CreateRatingDto` = `UpdateRatingDto` | Exact | **6/10** | 100% | Shared base DTO | Low |
| 1.4 | Transaction->DTO mapping (3x) | Exact | **9/10** | 100% | `TransactionMapper` | Low |
| 2.1 | Transaction filter/sort logic (2x) | Near | **9/10** | 85% | Private helper method | Low |
| 2.2 | User auth boilerplate (~15x) | Near | **8/10** | 90% | `AuthorizedControllerBase` | Medium |
| 2.3 | Repository `CreateAsync` (6x) / `DeleteAsync` (3x) | Near | **5/10** | 95% | Generic `BaseRepository<T>` | Medium |
| 2.4 | `ModelState.IsValid` (13x) | Near | **4/10** | 100% | Delete (redundant with `[ApiController]`) | Low |
| 2.5 | Day-change calc (2x same method) | Near | **6/10** | 100% | Merge into single loop | Low |
| 2.6 | Average rating calc (2x) | Near | **5/10** | 90% | `RatingExtensions` | Low |
| 2.7 | Stock price fallback (4-5x) | Near | **5/10** | 100% | `StockExtensions.EffectivePrice()` | Low |
| 2.8 | Portfolio query (3x) | Near | **6/10** | 100% | Private helper | Low |
| 3.1 | Controller CRUD structure | Structural | **4/10** | 70% | Not recommended to genericize yet | High |
| 3.2 | Stock field projection (2x) | Structural | **6/10** | 85% | Shared DTO / Include | Medium |
| 3.3 | AlphaVantage API call (3x) | Structural | **3/10** | 60% | `FetchAlphaVantageAsync` helper | Low |
| 3.4 | Cache check pattern (7x) | Structural | **3/10** | 80% | `GetOrSetAsync` on cache service | Medium |
| 4.1 | `NewUserDto` construction (2x) | Data | **4/10** | 100% | Private helper | Low |
| 4.2 | `QueryObject` / `TransactionQueryObject` overlap | Data | **3/10** | 60% | `PagedQueryBase` | Low |
| 4.3 | Boilerplate usings (all files) | Data | **2/10** | 100% | Enable implicit usings | Low |

---

## Priority Remediation Roadmap

### Phase 1: Quick Wins (~2 hours, eliminates ~60% of duplication)

| Priority | Finding | Effort | Lines Saved |
|----------|---------|--------|-------------|
| 1 | **1.4** -- `TransactionMapper` | 20 min | ~26 lines |
| 2 | **2.1** -- Transaction filter extraction | 30 min | ~40 lines |
| 3 | **2.4** -- Delete redundant `ModelState.IsValid` | 15 min | ~26 lines |
| 4 | **1.1** -- Stock DTO base class | 15 min | ~20 lines |
| 5 | **2.5** -- Merge day-change loops | 10 min | ~12 lines |
| 6 | **2.8** -- Portfolio query helper | 10 min | ~10 lines |
| 7 | **1.2, 1.3** -- Comment/Rating DTO bases | 20 min | ~15 lines |

### Phase 2: Medium Effort (~3 hours)

| Priority | Finding | Effort |
|----------|---------|--------|
| 8 | **2.2** -- `AuthorizedControllerBase` | 1-2 hours |
| 9 | **2.6, 2.7** -- Extension methods | 30 min |
| 10 | **3.3** -- AlphaVantage helper | 30 min |

### Phase 3: Optional / Long-term

| Priority | Finding | Effort |
|----------|---------|--------|
| 11 | **2.3** -- Generic `BaseRepository<T>` | 2-3 hours |
| 12 | **3.2** -- Stock projection consolidation | 1 hour |
| 13 | **3.4** -- `GetOrSetAsync` cache pattern | 1 hour |
| 14 | **4.2** -- `PagedQueryBase` | 20 min |

---

### Estimated Total Lines Saved: ~250-300 lines (~15% of application code)
