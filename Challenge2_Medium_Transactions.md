# Challenge 2: Transaction History System (MEDIUM)

**Estimated Time:** 6-8 hours
**Difficulty:** Medium

## Objective
Implement a comprehensive transaction tracking system to record all buy/sell actions for portfolio stocks, enabling users to track their trading history and investment performance.

## Background
Currently, the portfolio only shows which stocks a user owns, but doesn't track:
- When stocks were bought/sold
- At what price
- How many shares
- Transaction history

This challenge adds a complete transaction tracking system that will serve as the foundation for portfolio analytics.

## Requirements

### 1. Create Transaction Model
Create `Transaction.cs` in the `Models` folder:

```csharp
public class Transaction
{
    public int Id { get; set; }
    public string AppUserId { get; set; }
    public int StockId { get; set; }
    public TransactionType Type { get; set; } // Enum: Buy, Sell
    public int Quantity { get; set; }
    public decimal PricePerShare { get; set; }
    public decimal TotalAmount { get; set; } // Quantity * PricePerShare
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public AppUser AppUser { get; set; }
    public Stock Stock { get; set; }
}

public enum TransactionType
{
    Buy,
    Sell
}
```

### 2. Update Existing Models
- Add `List<Transaction> Transactions` to `AppUser` model
- Add `List<Transaction> Transactions` to `Stock` model

### 3. Update Portfolio Model (Enhancement)
Add a `Quantity` property to the Portfolio model:
```csharp
public int Quantity { get; set; } = 0;
```
This tracks how many shares the user owns.

### 4. Create Repository Interface
Create `ITransactionRepository.cs`:
- `Task<List<Transaction>> GetUserTransactionsAsync(AppUser user, TransactionQueryObject query)`
- `Task<Transaction?> GetByIdAsync(int id)`
- `Task<Transaction> CreateAsync(Transaction transaction)`
- `Task<TransactionSummary> GetUserSummaryAsync(AppUser user)`

### 5. Create Query Object
Create `TransactionQueryObject.cs` in the `Helpers` folder for filtering:
```csharp
public class TransactionQueryObject
{
    public string? StockSymbol { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SortBy { get; set; } // "Date", "Amount"
    public bool IsDescending { get; set; } = true;
}
```

### 6. Create DTOs
Create DTOs in `Dtos/Transaction` folder:
- `TransactionDto` - for returning transaction data
- `CreateTransactionDto` - for creating new transactions
- `TransactionSummaryDto` - for summary statistics

Example summary:
```csharp
public class TransactionSummaryDto
{
    public decimal TotalInvested { get; set; }
    public decimal TotalFromSales { get; set; }
    public int TotalBuyTransactions { get; set; }
    public int TotalSellTransactions { get; set; }
    public decimal NetInvestment { get; set; } // Invested - Sales
}
```

### 7. Implement Repository
Create `TransactionRepository.cs` implementing:
- Complex queries with filtering (by date range, stock, type)
- Sorting functionality
- Summary calculations using LINQ aggregations

### 8. Create Controller
Create `TransactionController.cs` with endpoints:

```csharp
[HttpGet]
[Authorize]
// GET /api/transaction - Get user's transactions with optional filtering
public async Task<IActionResult> GetUserTransactions([FromQuery] TransactionQueryObject query)

[HttpGet("{id:int}")]
[Authorize]
// GET /api/transaction/{id} - Get specific transaction
public async Task<IActionResult> GetById([FromRoute] int id)

[HttpPost]
[Authorize]
// POST /api/transaction - Create new transaction
public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionDto transactionDto)

[HttpGet("summary")]
[Authorize]
// GET /api/transaction/summary - Get user's transaction summary
public async Task<IActionResult> GetSummary()
```

### 9. Update PortfolioController
Modify the portfolio controller to:
- When adding a stock, also create a BUY transaction
- Track quantity (how many shares owned)
- Validate that user can't sell more shares than they own

### 10. Business Logic & Validation
Implement validation in the service or repository:
- Quantity must be positive
- Price must be positive
- For SELL transactions, verify user owns enough shares
- Calculate TotalAmount automatically (Quantity × PricePerShare)
- Set TransactionDate to current time if not provided

### 11. Database Migration
```bash
dotnet ef migrations add AddTransactions
dotnet ef database update
```

## Success Criteria
- [ ] Users can record buy/sell transactions
- [ ] Users can view their transaction history
- [ ] Filtering works (by date range, stock symbol, transaction type)
- [ ] Sorting works (by date, amount)
- [ ] Summary statistics are calculated correctly
- [ ] Validation prevents selling more shares than owned
- [ ] Only authorized users can access their own transactions
- [ ] Portfolio now tracks quantity of shares owned

## Testing Steps
1. Create multiple BUY transactions for different stocks
2. Test filtering by:
   - Date range
   - Stock symbol
   - Transaction type
3. Create a SELL transaction
4. Try to sell more shares than owned (should fail)
5. Verify summary calculations are correct
6. Test sorting by date and amount

## Bonus Challenges (Optional)
- [ ] Add pagination to transaction history
- [ ] Add endpoint to get transactions for a specific stock
- [ ] Calculate realized gains/losses (profit/loss on sold stocks)
- [ ] Add transaction categories (market order, limit order, etc.)
- [ ] Export transactions to CSV

## Tips
- Use `[Column(TypeName = "decimal(18,2)")]` for price and amount fields
- Consider using AutoMapper for complex DTO mappings
- Use LINQ `.Where()`, `.OrderBy()`, `.Sum()` for filtering and calculations
- Test edge cases: selling exactly the amount owned, selling with 0 quantity, etc.
- Look at `QueryObject.cs` to see how filtering is implemented for stocks

## Common Pitfalls
- Forgetting to update portfolio quantity when recording transactions
- Not validating sell quantity against owned shares
- Incorrect decimal calculations (use decimal type, not double/float)
- Not handling timezone issues with DateTime
- Forgetting to check user authorization for transaction access

## Resources
- Review LINQ aggregation methods: `.Sum()`, `.Count()`, `.Average()`
- Look at `StockRepository.cs` for query filtering examples
- Review Entity Framework relationships and eager loading (`.Include()`)

Good luck!
