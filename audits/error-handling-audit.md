# Error Handling Audit Report

**Project:** NETPRO Stock Portfolio Management API
**Date:** 2026-03-30
**Scope:** All controllers, services, repositories, middleware, DTOs, and configuration

---

## 1. ERROR HANDLING CONSISTENCY

### 1.1 No Centralized Error Handler / Global Exception Middleware

**Severity: 9/10**

`Program.cs:110-125` contains no global exception-handling middleware. Every controller must catch its own exceptions. If any controller action throws an unhandled exception, ASP.NET returns a default developer exception page (dev) or a generic 500 (prod) with no structured JSON body.

**Current pipeline:**
```csharp
// Program.cs:118-123
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

**Remediation** -- add a global exception middleware:

```csharp
// Middleware/ExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;

namespace api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                status = 500,
                message = "An unexpected error occurred.",
                detail = _env.IsDevelopment() ? ex.ToString() : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
```

Register in `Program.cs` **before** `UseAuthentication()`:
```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
```

---

### 1.2 Errors Are Not Handled Uniformly Across Controllers

**Severity: 7/10**

Error responses use inconsistent shapes. Some actions return `BadRequest(ModelState)` (serialized dictionary), others return `BadRequest("string message")`, others return `StatusCode(500, exception)`. There is no consistent error envelope.

| Controller | Style |
|---|---|
| `AccountContriller.cs:97` | `StatusCode(500, e)` -- leaks full `Exception` object |
| `PortfolioController.cs:66` | `StatusCode(500, "Could not create")` -- bare string |
| `StockControllers.cs:100` | `NotFound()` -- empty body |
| `CommentController.cs:89` | `NotFound("Comment not found")` -- string body |

**Remediation** -- standardize on a `ProblemDetails` response. With a global exception middleware in place, controller errors can adopt a consistent pattern:

```csharp
// Uniform not-found response in any controller:
return NotFound(new { status = 404, message = "Comment not found" });

// Uniform validation response:
return BadRequest(new { status = 400, message = "Stock not found!", errors = ModelState });
```

Or adopt ASP.NET's built-in `ProblemDetails`:
```csharp
// Program.cs -- add after AddControllers()
builder.Services.AddProblemDetails();
```

---

### 1.3 No Custom Exception Classes

**Severity: 5/10**

The project uses zero custom exceptions. Every failure is communicated via ad-hoc status codes and string messages. This makes it impossible to catch specific business-rule violations at a middleware level.

**Remediation** -- introduce typed exceptions that the global middleware can map:

```csharp
// Exceptions/ApiException.cs
namespace api.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiException(string message, int statusCode = 500) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public class ValidationException : ApiException
{
    public ValidationException(string message) : base(message, 400) { }
}
```

Then in the global middleware:
```csharp
catch (ApiException ex)
{
    context.Response.StatusCode = ex.StatusCode;
    // structured response...
}
catch (Exception ex)
{
    context.Response.StatusCode = 500;
    // structured response...
}
```

---

## 2. ERROR CATEGORIES

### 2.1 Validation Errors (400) -- Missing `[Required]` on LoginDto.Password

**Severity: 8/10**

`Dtos/Account/LoginDto.cs:13` -- `Password` has no `[Required]` attribute. A request with `{ "Username": "x" }` and no password passes `ModelState.IsValid`, then hits `SignInManager.CheckPasswordSignInAsync` with a `null` password, which may throw a `NullReferenceException` or return a misleading "incorrect password" message.

```csharp
// LoginDto.cs:13 -- CURRENT
public string Password {get; set;}
```

**Remediation:**
```csharp
[Required]
public string Password {get; set;}
```

---

### 2.2 Authentication Errors (401) -- Inconsistent Handling

**Severity: 6/10**

`AccountContriller.cs:38` returns `Unauthorized("Invalid username")` which tells an attacker whether the username exists. `AccountContriller.cs:43` returns `Unauthorized("Username or password incorrect")`. These should be identical to prevent user enumeration.

**Remediation:**
```csharp
// AccountContriller.cs -- both cases should return the same message:
if (user == null)
    return Unauthorized("Invalid username or password");

if (!result.Succeeded)
    return Unauthorized("Invalid username or password");
```

---

### 2.3 Authorization Errors (403) -- Inconsistent Use of Forbid vs Unauthorized

**Severity: 6/10**

- `TransactionController.cs:83` returns `Unauthorized()` when a user tries to access another user's transaction. This should be `Forbid()` (the user IS authenticated, just not authorized for THIS resource).
- `RatingController.cs:113` correctly uses `Forbid()` for the same pattern.

**Remediation** at `TransactionController.cs:82-83`:
```csharp
if (transaction.AppUserId != appUser.Id)
    return Forbid();  // was: return Unauthorized();
```

---

### 2.4 Not Found Errors (404) -- Empty Bodies

**Severity: 4/10**

Several endpoints return `NotFound()` with no body:
- `StockControllers.cs:53` -- `return NotFound();`
- `StockControllers.cs:100` -- `return NotFound();`
- `RatingController.cs:57` -- `return NotFound();`
- `StockAnalyticsController.cs:23` -- `return NotFound();`

API consumers receive a 404 with zero context on what was not found.

**Remediation** -- always include a message:
```csharp
return NotFound(new { message = "Stock not found" });
```

---

### 2.5 Server Errors (500) -- Full Exception Leaked to Client

**Severity: 10/10**

`AccountContriller.cs:97` returns the full `Exception` object to the client:
```csharp
return StatusCode(500, e);  // e is Exception
```

This exposes stack traces, internal paths, and potentially connection strings in production.

**Remediation:**
```csharp
catch (Exception e)
{
    _logger.LogError(e, "Registration failed for {Username}", registerDto.Username);
    return StatusCode(500, new { message = "An error occurred during registration." });
}
```

---

### 2.6 Rate Limit Errors (429) -- Completely Missing

**Severity: 7/10**

No rate limiting exists anywhere in the pipeline. The `StockApiSettings.RateLimitPerMinute = 5` config value (`appsettings.json:21`) is defined but **never read or enforced**. Both the API itself and the outbound AlphaVantage calls are unthrottled.

**Remediation** -- add ASP.NET rate limiting middleware:
```csharp
// Program.cs
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// After UseAuthorization():
app.UseRateLimiter();
```

---

## 3. ASYNC ERROR HANDLING

### 3.1 `NullReferenceException` from `GetUsername()` Extension -- No Null Guard

**Severity: 9/10**

`Extensions/ClaimExtensions.cs:13` calls `.Value` on the result of `SingleOrDefault()` which returns `null` if the claim is missing (e.g., expired token, malformed token, anonymous request to an endpoint that forgot `[Authorize]`). This causes an unhandled `NullReferenceException`.

```csharp
// ClaimExtensions.cs:13 -- CURRENT
return user.Claims.SingleOrDefault(x => x.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")).Value;
```

This is called in **9 controller actions** without any null check on the result:
- `CommentController.cs:70`
- `PortfolioController.cs:34,44,77`
- `RatingController.cs:70,101`
- `WatchListController.cs:35,52,86`
- `TransactionController.cs:42,107,184,200,230,244`
- `PortfolioAnalyticsController.cs:29,42,55`

**Remediation:**
```csharp
public static string GetUsername(this ClaimsPrincipal user)
{
    var claim = user.Claims.SingleOrDefault(x =>
        x.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"));
    return claim?.Value ?? throw new UnauthorizedAccessException("Username claim not found in token.");
}
```

---

### 3.2 `FindByNameAsync` Returns Null -- Not Checked in PortfolioController

**Severity: 8/10**

`PortfolioController.cs:35` and `PortfolioController.cs:45` call `_userManager.FindByNameAsync(username)` but never check for null before using `appUser`. If the user was deleted between token issuance and request, this causes a `NullReferenceException`.

```csharp
// PortfolioController.cs:34-36
var username = User.GetUsername();
var appUser = await _userManager.FindByNameAsync(username);
var userPortfolio = await _portfolioRepo.GetUserPortfolio(appUser);  // appUser could be null
```

Same issue at:
- `PortfolioController.cs:44-46` (AddPortfolio)
- `PortfolioController.cs:78-79` (DeletePortfolio)
- `CommentController.cs:70-71` (Create)

**Remediation** -- add null check after `FindByNameAsync` in every occurrence:
```csharp
var appUser = await _userManager.FindByNameAsync(username);
if (appUser == null)
    return Unauthorized();
```

---

### 3.3 PortfolioAnalyticsService -- No Try-Catch on Any Method

**Severity: 7/10**

`Service/PortfolioAnalyticsService.cs` -- all four public methods (`GetPerformanceAsync`, `GetDiversificationAsync`, `GetPortfolioHistoryAsync`, `GetStockPerformanceAsync`) have zero try-catch blocks. Any DB failure, division by zero, or external API failure propagates as an unhandled 500 to the client.

Compare with `StockDataService.cs` which wraps every method in try-catch.

**Remediation** -- either wrap each method or rely on the global exception middleware (Finding 1.1). If choosing per-method:
```csharp
public async Task<PortfolioPerformance> GetPerformanceAsync(AppUser user)
{
    try
    {
        // existing code...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to compute performance for user {UserId}", user.Id);
        throw; // let global middleware handle the response
    }
}
```

---

### 3.4 No Unhandled Promise Rejection Equivalent (Unobserved Task Exceptions)

**Severity: 5/10**

`StockPriceUpdateService.cs:28` calls `UpdateAllStockPricesAsync(stoppingToken)` **outside** the try-catch loop on first run. If the very first invocation throws, the background service crashes silently.

```csharp
// StockPriceUpdateService.cs:28-29 -- OUTSIDE the while loop's try-catch
await UpdateAllStockPricesAsync(stoppingToken);

while (!stoppingToken.IsCancellationRequested)
{
    try { ... }
```

**Remediation:**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("StockPriceUpdateService started.");

    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            await UpdateAllStockPricesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in StockPriceUpdateService loop.");
        }
    }

    _logger.LogInformation("StockPriceUpdateService stopped.");
}
```

---

### 3.5 Transaction Create -- Non-Atomic Operation

**Severity: 8/10**

`TransactionController.cs:146-176` creates a transaction via the repository (which calls `SaveChangesAsync` internally at `TransactionRepository.cs:25`), then separately updates the portfolio and calls `SaveChangesAsync` again at line 176. These are **two separate DB commits**. If the second `SaveChangesAsync` fails, the transaction is recorded but the portfolio quantity is wrong.

```csharp
await _transactionRepo.CreateAsync(transaction);   // Commits TX record
// ... portfolio logic ...
await _context.SaveChangesAsync();                  // Commits portfolio update
```

**Remediation** -- wrap in an explicit DB transaction:
```csharp
using var dbTransaction = await _context.Database.BeginTransactionAsync();
try
{
    await _context.Transactions.AddAsync(transaction);
    // ... portfolio update logic (without intermediate SaveChanges) ...
    await _context.SaveChangesAsync();
    await dbTransaction.CommitAsync();
    return Ok("Transaction created successfully");
}
catch (Exception ex)
{
    await dbTransaction.RollbackAsync();
    return StatusCode(500, new { message = "Transaction failed." });
}
```

Note: This also requires removing the `SaveChangesAsync` from inside `TransactionRepository.CreateAsync` or bypassing it for this flow.

---

## 4. ERROR RECOVERY

### 4.1 No Retry Mechanism on External API Calls

**Severity: 6/10**

`StockDataService.cs` makes HTTP calls to AlphaVantage with no retry policy. If the API returns a transient 503 or times out, the request simply fails and returns `null`.

**Remediation** -- add Polly retry via `HttpClientFactory`:
```csharp
// Program.cs -- replace the HttpClient registration
builder.Services.AddHttpClient<IStockDataService, StockDataService>()
    .AddStandardResilienceHandler();  // .NET 8+ Microsoft.Extensions.Http.Resilience
```

Or with Polly directly:
```csharp
using Microsoft.Extensions.Http.Resilience;

builder.Services.AddHttpClient<IStockDataService, StockDataService>()
    .AddResilienceHandler("stock-api", builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential
        });
        builder.AddTimeout(TimeSpan.FromSeconds(10));
    });
```

---

### 4.2 No Circuit Breaker

**Severity: 5/10**

If AlphaVantage goes down, the `StockPriceUpdateService` will keep firing requests every 15 seconds per stock (line `StockPriceUpdateService.cs:93`), potentially exhausting connections. No circuit breaker stops the cascade.

**Remediation** -- add a circuit breaker to the resilience handler above:
```csharp
builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
{
    SamplingDuration = TimeSpan.FromSeconds(30),
    FailureRatio = 0.5,
    MinimumThroughput = 3,
    BreakDuration = TimeSpan.FromMinutes(1)
});
```

---

### 4.3 No Graceful Degradation Strategy

**Severity: 4/10**

When the external stock API fails, `StockDataService` returns `null` or empty list. The consumers (`StockControllers.cs:109-113`, `PortfolioAnalyticsService.cs:199`) silently fall back to DB-stored prices. This is partially graceful but undocumented -- consumers don't know the data is stale.

**Remediation** -- return a flag indicating data freshness:
```csharp
// In StockPriceData or a wrapper:
public bool IsFromCache { get; set; }
public DateTime? CacheExpiry { get; set; }
```

---

## 5. ERROR INFORMATION

### 5.1 Stack Trace Exposed in Production

**Severity: 10/10**

`AccountContriller.cs:97` -- `return StatusCode(500, e)` serializes the full exception including stack trace, inner exceptions, and target site info to the client. No dev/prod distinction.

Additionally, `AccountContriller.cs:86` returns `StatusCode(500, roleResult.Errors)` and line 92 returns `StatusCode(500, createdUser.Errors)`. While these are Identity error objects (not stack traces), they still expose internal error codes.

See remediation at Finding 2.5.

---

### 5.2 No Structured Logging for Controller-Level Errors

**Severity: 6/10**

Only `AccountContriller.cs:57` (Register) has a try-catch in a controller. All other controllers have zero logging. If a request fails at the controller level, the only trace is the default ASP.NET request log -- no structured error context (userId, stockId, etc.).

The services (`StockDataService`, `StockPriceUpdateService`) log properly. The controllers and repositories do not.

**Remediation** -- with a global exception middleware (Finding 1.1), this is solved centrally. The middleware should log request path, method, user identity, and correlation ID.

---

### 5.3 JWT Configuration Errors Fail Silently

**Severity: 7/10**

`Program.cs:90` uses the null-forgiving operator on `JWT:SigningKey`:
```csharp
System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"]!)
```

If the config value is missing, this throws a `NullReferenceException` at startup with no meaningful message. Same applies to `TokenService.cs:21`.

**Remediation:**
```csharp
var signingKey = builder.Configuration["JWT:SigningKey"]
    ?? throw new InvalidOperationException("JWT:SigningKey is not configured.");
```

---

### 5.4 appsettings.json Exposes Secrets in Source Control

**Severity: 8/10**

`appsettings.json:15-16` contains the JWT signing key in plaintext and the full SQL Server connection string with integrated auth. These are checked into git.

```json
"SigningKey": "fdvdkvpodkf8f5dv5fd88d5v5df5vd88s9s898fd656v23c2v6x2cTkofk8f6d5f65c68fdfd"
```

**Remediation:**
```bash
# Use user secrets for development:
dotnet user-secrets set "JWT:SigningKey" "your-key-here"

# For production, use environment variables or Azure Key Vault
```

Remove the key from `appsettings.json` and set a placeholder:
```json
"JWT": {
    "SigningKey": ""
}
```

---

### 5.5 JWT Issuer/Audience Has Typo

**Severity: 6/10**

`appsettings.json:13-14`:
```json
"Issuer": "http://ocalhost:5247",
"Audience": "http://ocalhost:5247",
```

`ocalhost` -- missing the `l` in `localhost`. Token validation will only pass if both token creation and validation use this same typo (which they do here), but this will break if either side is corrected independently.

**Remediation:**
```json
"Issuer": "http://localhost:5247",
"Audience": "http://localhost:5247"
```

---

## 6. ADDITIONAL FINDINGS

### 6.1 Route Bug -- CommentController Delete Route

**Severity: 8/10**

`CommentController.cs:96` has the route `{id}:int` instead of `{id:int}`. The colon is outside the braces, so `id` is an unconstrained string parameter and the literal `:int` is part of the route. A request to `DELETE /api/comment/5` won't match -- you'd need `DELETE /api/comment/5:int`.

```csharp
[Route ("{id}:int")]  // BUG
```

**Remediation:**
```csharp
[Route("{id:int}")]
```

---

### 6.2 Unreachable Null Check in PortfolioController.AddPortfolio

**Severity: 3/10**

`PortfolioController.cs:63-67` -- the null check on `portfolioModel` happens after `CreateAsync` has already been called. If `CreateAsync` fails it will throw, not return null. The check is dead code.

```csharp
await _portfolioRepo.CreateAsync(portfolioModel);  // throws on failure
if(portfolioModel == null)  // always false
```

**Remediation** -- remove the dead check and add a try-catch if needed, or rely on global middleware.

---

### 6.3 CSV Export -- No Field Escaping (Injection Risk)

**Severity: 7/10**

`TransactionController.cs:258-261` only replaces commas in notes. If `CompanyName` or `Notes` contain quotes, newlines, or CSV formula characters (`=`, `+`, `-`, `@`), the output is corrupted or vulnerable to CSV injection.

```csharp
var notes = t.Notes?.Replace(",", " ") ?? "";
builder.AppendLine($"{t.Id},...,{t.Stock.CompanyName},...,{notes}");
```

**Remediation:**
```csharp
static string CsvEscape(string? value)
{
    if (string.IsNullOrEmpty(value)) return "";
    if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        return $"\"{value.Replace("\"", "\"\"")}\"";
    // Prevent CSV formula injection
    if (value.StartsWith('=') || value.StartsWith('+') || value.StartsWith('-') || value.StartsWith('@'))
        return $"'{value}";
    return value;
}
```

---

### 6.4 `decimal.Parse` Without CultureInfo in StockDataService

**Severity: 5/10**

`StockDataService.cs:62-64` and `StockDataService.cs:111-116` use `decimal.Parse()` without specifying `CultureInfo.InvariantCulture`. On servers with non-US locale settings (e.g., Germany where `,` is the decimal separator), parsing `"154.23"` would throw a `FormatException`.

```csharp
CurrentPrice = decimal.Parse(quote.GetProperty("05. price").GetString() ?? "0"),
```

**Remediation:**
```csharp
using System.Globalization;
// ...
CurrentPrice = decimal.Parse(
    quote.GetProperty("05. price").GetString() ?? "0",
    CultureInfo.InvariantCulture),
```

---

### 6.5 `TokenService.CreateToken` Uses `DateTime.Now` Instead of `DateTime.UtcNow`

**Severity: 5/10**

`Service/TokenService.cs:36`:
```csharp
Expires = DateTime.Now.AddDays(7),
```

Using local time for token expiration can cause tokens to expire early or late depending on server timezone and DST transitions.

**Remediation:**
```csharp
Expires = DateTime.UtcNow.AddDays(7),
```

---

## Summary Table

| # | Finding | Severity | Category |
|---|---------|----------|----------|
| 1.1 | No global exception middleware | 9/10 | Consistency |
| 1.2 | Inconsistent error response shapes | 7/10 | Consistency |
| 1.3 | No custom exception classes | 5/10 | Consistency |
| 2.1 | `LoginDto.Password` missing `[Required]` | 8/10 | Validation |
| 2.2 | User enumeration via distinct login error messages | 6/10 | Authentication |
| 2.3 | `Unauthorized()` used where `Forbid()` is correct | 6/10 | Authorization |
| 2.4 | Empty-body `NotFound()` responses | 4/10 | Not Found |
| 2.5 | Full `Exception` object returned to client | 10/10 | Server Errors |
| 2.6 | Rate limiting configured but never enforced | 7/10 | Rate Limiting |
| 3.1 | `GetUsername()` throws `NullReferenceException` on missing claim | 9/10 | Async/Null Safety |
| 3.2 | `FindByNameAsync` null result not checked in PortfolioController | 8/10 | Async/Null Safety |
| 3.3 | PortfolioAnalyticsService has zero try-catch | 7/10 | Async Error Handling |
| 3.4 | Background service first-run outside try-catch | 5/10 | Async Error Handling |
| 3.5 | Non-atomic transaction + portfolio update | 8/10 | Async Error Handling |
| 4.1 | No retry on external HTTP calls | 6/10 | Error Recovery |
| 4.2 | No circuit breaker for AlphaVantage API | 5/10 | Error Recovery |
| 4.3 | No stale-data indicator on fallback prices | 4/10 | Error Recovery |
| 5.1 | Stack trace exposed in production (duplicate of 2.5) | 10/10 | Error Information |
| 5.2 | No structured logging in controllers | 6/10 | Error Information |
| 5.3 | JWT config missing-key fails with `NullReferenceException` | 7/10 | Error Information |
| 5.4 | JWT signing key committed to source control | 8/10 | Error Information |
| 5.5 | JWT Issuer/Audience typo (`ocalhost`) | 6/10 | Error Information |
| 6.1 | CommentController Delete route bug `{id}:int` | 8/10 | Routing |
| 6.2 | Dead null check after `CreateAsync` | 3/10 | Dead Code |
| 6.3 | CSV export vulnerable to injection | 7/10 | Security |
| 6.4 | `decimal.Parse` without `CultureInfo.InvariantCulture` | 5/10 | Localization |
| 6.5 | `DateTime.Now` in token expiration instead of `UtcNow` | 5/10 | Time Handling |

---

## Priority Remediation Order

1. **Immediate (Severity 8-10):** Findings 2.5/5.1, 1.1, 3.1, 2.1, 3.2, 3.5, 5.4, 6.1
2. **Short-term (Severity 6-7):** Findings 1.2, 2.2, 2.3, 2.6, 3.3, 4.1, 5.2, 5.3, 5.5, 6.3
3. **Nice-to-have (Severity 3-5):** Findings 1.3, 2.4, 3.4, 4.2, 4.3, 6.2, 6.4, 6.5
