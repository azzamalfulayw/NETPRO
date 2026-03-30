# Naming & Readability Audit

**Project:** NETPRO Stock Trading API
**Date:** 2026-03-30
**Scope:** All `.cs` source files (excluding `obj/`, `bin/`, `Migrations/`)

---

## 1. NAMING CONVENTIONS

### 1.1 Typos in Identifiers (Severity: 9/10)

Misspelled identifiers propagated across the entire codebase. These affect API contracts, database columns, and DTOs — making the API confusing for consumers and impossible to fix without a migration later.

| Typo | Correct | Files Affected |
|------|---------|----------------|
| `Sympol` | `Symbol` | `Stock.cs:13`, `StockDto.cs:12`, `CreateStockRequestDto.cs:13`, `UpdateStockRequestDto` (inferred), `StockMappers.cs:17,38`, `StockRepository.cs:52,60,76,92`, `PortfolioController.cs:53,83`, `TransactionController.cs:53,89,213,260`, `TransactionRepository.cs:38,95,174`, `PortfolioAnalyticsService.cs:83,199,240`, `StockPriceUpdateService.cs:74,78,88,101` |
| `CtreatedOn` | `CreatedOn` | `Comment.cs:15` |
| `IsDecsending` | `IsDescending` | `QueryObject.cs:13` |
| `AccountContriller` | `AccountController` | `AccountContriller.cs:16,22` (class name + filename) |
| `CteateWatchListRequestDto` | `CreateWatchListRequestDto` | `CteateWatchListRequestDto.cs:8` (class name + filename) |
| `ToStockFromCreteDTO` | `ToStockFromCreateDto` | `StockMappers.cs:34` |
| `_userMAnager` | `_userManager` | `AccountContriller.cs:18,24,35,68,72` |
| `exisitingStock` | `existingStock` | `StockRepository.cs:86,87,92-100` |
| `Inustry` | `Industry` | `CreateStockRequestDto.cs:24` (validation message) |

**Remediation (example for `Sympol` → `Symbol` in the model — all references must follow):**

```csharp
// Stock.cs:13 — before
public string Sympol { get; set; } = string.Empty;

// Stock.cs:13 — after
public string Symbol { get; set; } = string.Empty;
```

> Fixing `Sympol` requires an EF Core migration to rename the database column. Plan this as a coordinated change.

---

### 1.2 Inconsistent File Naming (Severity: 7/10)

Controller file names don't follow a consistent pattern.

| File | Problem |
|------|---------|
| `AccountContriller.cs` | Misspelled ("Contriller" instead of "Controller") |
| `StockControllers.cs` | Pluralized — every other controller is singular |
| `CommentController.cs` | Correct reference pattern |

**Remediation:** Rename files to match the `{Entity}Controller.cs` pattern:
- `AccountContriller.cs` → `AccountController.cs`
- `StockControllers.cs` → `StockController.cs`

---

### 1.3 Variable Naming: Cryptic Lambda Parameters (Severity: 4/10)

Single-letter lambda parameters are used inconsistently. Some are acceptable in short expressions, but several are ambiguous.

| Location | Code | Issue |
|----------|------|-------|
| `StockRepository.cs:43` | `.Include(c => c.Comments).ThenInclude(a => a.AppUser).Include(r => r.Ratings)` | `c`, `a`, `r` in a chained expression — hard to scan |
| `ApplicationDBContext.cs:29` | `x => x.HasKey(p => new {p.AppUserId, p.StockId})` | Nested `x` and `p` — fine here, but inconsistent with `u` elsewhere |
| `PortfolioController.cs:53` | `userPortfolio.Any(e => e.Sympol.ToLower() == ...)` | `e` is meaningless — should be `stock` or `s` |

**Remediation (StockRepository.cs:43-44):**

```csharp
// before
var stocks = _context.Stocks.Include(c => c.Comments)
    .ThenInclude(a => a.AppUser).Include(r => r.Ratings).AsQueryable();

// after
var stocks = _context.Stocks
    .Include(s => s.Comments)
        .ThenInclude(c => c.AppUser)
    .Include(s => s.Ratings)
    .AsQueryable();
```

---

### 1.4 PascalCase Violation on Local Variable (Severity: 6/10)

| Location | Code | Issue |
|----------|------|-------|
| `AccountContriller.cs:62` | `var AppUser = new AppUser { ... }` | Local variable uses PascalCase — collides with the type name |

**Remediation:**

```csharp
// before
var AppUser = new AppUser { UserName = registerDto.Username, Email = registerDto.Email };

// after
var appUser = new AppUser { UserName = registerDto.Username, Email = registerDto.Email };
```

> Lines 68, 72, 78, 79, 80 also reference this variable — update all.

---

### 1.5 DbSet Casing Inconsistency (Severity: 7/10)

| Location | Property | Convention |
|----------|----------|------------|
| `ApplicationDBContext.cs:19` | `Stocks` | PascalCase (correct) |
| `ApplicationDBContext.cs:20` | `Comments` | PascalCase (correct) |
| **`ApplicationDBContext.cs:21`** | **`portfolios`** | **camelCase (wrong)** |
| `ApplicationDBContext.cs:22` | `Ratings` | PascalCase (correct) |
| `ApplicationDBContext.cs:23` | `WatchLists` | PascalCase (correct) |
| `ApplicationDBContext.cs:24` | `Transactions` | PascalCase (correct) |

**Remediation:**

```csharp
// ApplicationDBContext.cs:21 — before
public DbSet<Portfolio> portfolios { get; set; }

// after
public DbSet<Portfolio> Portfolios { get; set; }
```

> All references to `_context.portfolios` must also change (found in `TransactionController.cs:124,159,172` and `PortfolioAnalyticsService.cs:39,139,184`).

---

### 1.6 Inconsistent Naming Between Query Objects (Severity: 5/10)

| Property | `QueryObject.cs` | `TransactionQueryObject.cs` |
|----------|-------------------|-----------------------------|
| Descending flag | `IsDecsending` (misspelled) | `IsDescending` (correct) |
| Page size default | `20` | `10` |

Both serve the same purpose but use different defaults and different spelling.

**Remediation:** Align both to `IsDescending` and pick a single default page size.

---

## 2. NAMING CONSISTENCY

### 2.1 Folder Naming: Singular vs Plural (Severity: 3/10)

| Folder | Convention |
|--------|-----------|
| `Controllers/` | Plural |
| `Models/` | Plural |
| `Dtos/` | Plural |
| `Interfaces/` | Plural |
| `Repository/` | **Singular** |
| `Service/` | **Singular** |
| `Mappers/` | Plural |
| `Helpers/` | Plural |
| `Extensions/` | Plural |

**Remediation:** Rename `Repository/` → `Repositories/` and `Service/` → `Services/` for consistency. Update namespace declarations accordingly.

---

### 2.2 Abbreviation Inconsistency (Severity: 4/10)

| Term | Usages |
|------|--------|
| `Dto` | `StockDto`, `CommentDto`, `RatingDto` |
| `DTO` | `ToStockFromCreteDTO` (StockMappers.cs:34) |
| `Repo` | `_stockRepo`, `_commentRepo`, `_ratingRepo` |
| `DB` | `ApplicationDBContext` |

C# convention: `Dto` (two-letter+ abbreviations use PascalCase). `DB` should be `Db`.

**Remediation for the mapper method:**

```csharp
// StockMappers.cs:34 — before
public static Stock ToStockFromCreteDTO(this CreateStockRequestDto stockDto)

// after
public static Stock ToStockFromCreateDto(this CreateStockRequestDto stockDto)
```

---

### 2.3 Domain Terminology: "Purchase" is Ambiguous (Severity: 5/10)

`Stock.Purchase` (`Stock.cs:16`) represents the purchase price but reads like a verb/action. Every other financial property uses a noun (`LastDiv`, `MarketCap`, `CurrentPrice`).

**Remediation:**

```csharp
// before
public decimal Purchase { get; set; }

// after
public decimal PurchasePrice { get; set; }
```

> Requires migration + updating all DTOs and mappers.

---

## 3. CODE READABILITY

### 3.1 Route Attribute Bug — Not Just Style (Severity: 10/10)

`CommentController.cs:96` has the route constraint **outside** the braces:

```csharp
// BROKEN — matches "{id}:int" literally, constraint not applied
[Route ("{id}:int")]

// CORRECT
[Route("{id:int}")]
```

This is a **runtime bug** — the Delete endpoint will not match integer-only routes as intended.

---

### 3.2 Unused Imports / Wrong Namespace Imports (Severity: 4/10)

| File | Import | Issue |
|------|--------|-------|
| `PortfolioController.cs:9` | `Microsoft.AspNetCore.Components` | Blazor namespace — not needed in a Web API controller |
| `PortfolioController.cs:12` | `Microsoft.VisualBasic` | VB compatibility namespace — has no business here |
| `PortfolioController.cs:13` | `RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute` | Alias needed only because of the Blazor import conflict |
| `RatingController.cs:4` | `System.Runtime.InteropServices` | Completely unused |
| `RatingController.cs:12` | `Microsoft.AspNetCore.Components` | Same Blazor conflict |

**Remediation:** Remove the wrong imports and the alias workaround:

```csharp
// PortfolioController.cs — remove lines 9, 12, 13
// RatingController.cs — remove lines 4, 12, 15
```

---

### 3.3 Missing Validation Attribute (Severity: 7/10)

`LoginDto.cs:13` — `Password` lacks `[Required]`:

```csharp
// before
public string Password { get; set; }

// after
[Required]
public string Password { get; set; }
```

Without this, a `null` password passes model validation and hits the sign-in manager, which will throw.

---

### 3.4 Magic Numbers in Validation (Severity: 6/10)

`CreateStockRequestDto.cs` uses `MaxLength(10)` for **CompanyName** and **Industry** — too restrictive for real data ("Goldman Sachs" = 13 chars, "Information Technology" = 21 chars).

| Line | Property | Current | Suggested |
|------|----------|---------|-----------|
| `15` | CompanyName | `MaxLength(10)` | `MaxLength(280)` |
| `24` | Industry | `MaxLength(10)` | `MaxLength(100)` |

---

### 3.5 Duplicated Mapping Logic (Severity: 6/10)

`TransactionController.cs` manually maps `Transaction` → `TransactionDto` in **four** separate places (lines 49-62, 85-98, 208-221, 256-261) instead of using a mapper method.

**Remediation:** Create `TransactionMapper.cs`:

```csharp
public static class TransactionMapper
{
    public static TransactionDto ToTransactionDto(this Transaction t)
    {
        return new TransactionDto
        {
            Id = t.Id,
            StockId = t.StockId,
            Symbol = t.Stock.Sympol, // fix to Symbol after rename
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
```

---

### 3.6 Null Check After Awaited Create (Severity: 3/10)

`PortfolioController.cs:64` checks if `portfolioModel == null` **after** `CreateAsync` has already been awaited. The model is never null at this point — it was just constructed on line 56.

```csharp
// PortfolioController.cs:62-71 — dead code, remove the null check
await _portfolioRepo.CreateAsync(portfolioModel);
// portfolioModel is always non-null here
return Created();
```

---

### 3.7 `DateTime.Now` vs `DateTime.UtcNow` Inconsistency (Severity: 5/10)

| Location | Usage |
|----------|-------|
| `Comment.cs:15` | `DateTime.Now` |
| `WatchList.cs:14` | `DateTime.UtcNow` |
| `Transaction.cs:23` | `DateTime.UtcNow` |
| `TokenService.cs:36` | `DateTime.Now` |

**Remediation:** Standardize on `DateTime.UtcNow` everywhere for a server-side API.

---

## 4. FUNCTION SIGNATURES

### 4.1 `TransactionController` Constructor — 4 Parameters (Severity: 4/10)

`TransactionController.cs:29-30` takes `UserManager`, `ITransactionRepository`, `IStockRepository`, **and** `ApplicationDBContext`. The direct `DbContext` dependency bypasses the repository pattern used everywhere else.

**Remediation:** Move the portfolio quantity update logic (lines 124-176) into the `TransactionRepository` or a dedicated `IPortfolioService` so the controller doesn't need the raw context.

---

### 4.2 `StockControllers` Constructor Takes Unused `ApplicationDBContext` (Severity: 5/10)

`StockControllers.cs:20,23` injects `ApplicationDBContext _context` but never uses it — all data access goes through `_stockRepo`.

**Remediation:**

```csharp
// Remove _context field and constructor parameter
public StockControllers(IStockRepository stockRepo, IStockDataService stockDataService)
{
    _stockRepo = stockRepo;
    _stockDataService = stockDataService;
}
```

---

### 4.3 `TransactionRepository._context` Is `public` (Severity: 6/10)

`TransactionRepository.cs:16`:

```csharp
// before
public readonly ApplicationDBContext _context;

// after
private readonly ApplicationDBContext _context;
```

---

## 5. CONFIGURATION ISSUES

### 5.1 JWT Issuer/Audience Typo (Severity: 8/10)

`appsettings.json` has `"http://ocalhost:5247"` — missing the `l` in `localhost`. Auth tokens generated with this issuer will fail validation if the typo is ever corrected.

```json
// before
"Issuer": "http://ocalhost:5247",
"Audience": "http://ocalhost:5247",

// after
"Issuer": "http://localhost:5247",
"Audience": "http://localhost:5247",
```

---

## Summary Scorecard

| # | Finding | Severity |
|---|---------|----------|
| 1.1 | Typos in identifiers (`Sympol`, `CtreatedOn`, etc.) | 9/10 |
| 1.2 | Inconsistent file naming | 7/10 |
| 1.3 | Cryptic lambda parameters | 4/10 |
| 1.4 | PascalCase local variable | 6/10 |
| 1.5 | DbSet casing (`portfolios`) | 7/10 |
| 1.6 | Query object naming mismatch | 5/10 |
| 2.1 | Folder singular/plural mix | 3/10 |
| 2.2 | Abbreviation inconsistency (`DTO` vs `Dto`) | 4/10 |
| 2.3 | Ambiguous domain term (`Purchase`) | 5/10 |
| 3.1 | Route attribute bug (`"{id}:int"`) | 10/10 |
| 3.2 | Unused/wrong namespace imports | 4/10 |
| 3.3 | Missing `[Required]` on `Password` | 7/10 |
| 3.4 | Magic numbers in `MaxLength` | 6/10 |
| 3.5 | Duplicated Transaction mapping (4x) | 6/10 |
| 3.6 | Dead null check after create | 3/10 |
| 3.7 | `DateTime.Now` vs `UtcNow` mix | 5/10 |
| 4.1 | Controller depends on raw DbContext | 4/10 |
| 4.2 | Unused DbContext injection | 5/10 |
| 4.3 | `public` field on repository | 6/10 |
| 5.1 | JWT issuer/audience typo | 8/10 |

---

## Naming Convention Guide

Based on findings above, the following conventions should be enforced going forward.

### Identifiers

| Element | Convention | Example |
|---------|-----------|---------|
| Class / Record | PascalCase, noun, singular | `StockController`, `TransactionDto` |
| Interface | `I` + PascalCase noun | `IStockRepository` |
| Public property | PascalCase | `CompanyName`, `TransactionDate` |
| Private field | `_camelCase` | `_userManager`, `_stockRepo` |
| Local variable | camelCase | `appUser`, `existingStock` |
| Method | PascalCase verb phrase | `GetByIdAsync`, `CreateAsync` |
| Enum | PascalCase (no flags suffix unless `[Flags]`) | `TransactionType.Buy` |
| Constant | PascalCase (C# convention, not UPPER_CASE) | `DefaultPageSize` |
| Lambda parameter | Single letter only in trivial one-liners; descriptive name in chains | `s => s.Id` OK; `.Include(stock => stock.Comments).ThenInclude(comment => comment.AppUser)` in chains |

### Abbreviations

| Rule | Example |
|------|---------|
| Two-letter abbreviations: UPPER | `IO`, `DB` → but prefer `Db` for C# modern style |
| Three+ letter abbreviations: PascalCase | `Dto`, `Api`, `Jwt` |
| Pick one form and use it everywhere | `Dto` not `DTO` |

### Files & Folders

| Element | Convention | Example |
|---------|-----------|---------|
| Source file | Match the primary type name exactly | `StockController.cs` |
| Folder | Plural noun | `Controllers/`, `Repositories/`, `Services/` |
| DTO subfolder | Match entity | `Dtos/Stock/`, `Dtos/Transaction/` |

### Domain Terms

| Use | Not |
|-----|-----|
| `Symbol` | `Sympol` |
| `CreatedOn` | `CtreatedOn` |
| `IsDescending` | `IsDecsending` |
| `PurchasePrice` | `Purchase` (ambiguous) |
| `LastDividend` | `LastDiv` (if clarity preferred) |

### Date/Time

- Always use `DateTime.UtcNow` in server-side code.
- Store and compare dates in UTC.
- Convert to local time only at the presentation layer.

### Route Attributes

```csharp
// Correct — constraint inside braces
[HttpGet("{id:int}")]
[Route("{id:int}")]

// WRONG — constraint outside braces
[Route("{id}:int")]
```

### Access Modifiers

- Repository and service fields: always `private readonly`.
- Never expose `_context` as `public`.
