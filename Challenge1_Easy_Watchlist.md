# Challenge 1: Stock Watchlist Feature (EASY)

**Estimated Time:** 2-4 hours
**Difficulty:** Easy

## Objective
Add a "Watchlist" feature that allows users to track stocks they're interested in watching (separate from their actual portfolio holdings).

## Background
Currently, users can only add stocks to their portfolio. However, users often want to monitor stocks they're considering buying without actually purchasing them. This is where a watchlist comes in handy.

## Requirements

### 1. Database Model
Create a new `Watchlist` model in the `Models` folder:
- Should represent a many-to-many relationship between `AppUser` and `Stock`
- Similar to how `Portfolio` works
- Properties needed:
  - `AppUserId` (foreign key)
  - `StockId` (foreign key)
  - `AddedOn` (DateTime - when the stock was added to watchlist)
  - Navigation properties to `AppUser` and `Stock`

### 2. Update Existing Models
- Add a `List<Watchlist>` property to the `AppUser` model
- Add a `List<Watchlist>` property to the `Stock` model

### 3. Create Repository Interface
Create `IWatchlistRepository.cs` in the `Interfaces` folder with methods:
- `Task<List<Stock>> GetUserWatchlist(AppUser user)`
- `Task<Watchlist> CreateAsync(Watchlist watchlist)`
- `Task<Watchlist?> DeleteAsync(AppUser user, int stockId)`

### 4. Implement Repository
Create `WatchlistRepository.cs` in the `Repository` folder implementing the interface above.

### 5. Create DTOs
Create necessary DTOs in the `Dtos/Watchlist` folder:
- `WatchlistDto` - for returning watchlist items with stock information
- Consider what information users need to see

### 6. Create Controller
Create `WatchlistController.cs` in the `Controllers` folder with endpoints:

```csharp
[HttpGet]
[Authorize]
// GET /api/watchlist - Get current user's watchlist
public async Task<IActionResult> GetUserWatchlist()

[HttpPost]
[Authorize]
[Route("{stockId:int}")]
// POST /api/watchlist/{stockId} - Add stock to watchlist
public async Task<IActionResult> AddToWatchlist([FromRoute] int stockId)

[HttpDelete]
[Authorize]
[Route("{stockId:int}")]
// DELETE /api/watchlist/{stockId} - Remove stock from watchlist
public async Task<IActionResult> RemoveFromWatchlist([FromRoute] int stockId)
```

### 7. Register Repository
Don't forget to register the repository in `Program.cs`:
```csharp
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
```

### 8. Create Migration
After creating the model, run:
```bash
dotnet ef migrations add AddWatchlist
dotnet ef database update
```

## Success Criteria
- [ ] Users can add stocks to their watchlist
- [ ] Users can view all stocks in their watchlist
- [ ] Users can remove stocks from their watchlist
- [ ] Users can only access their own watchlist (authorization works)
- [ ] No duplicate stocks in a user's watchlist
- [ ] API returns appropriate status codes (200, 201, 204, 400, 401, 404)

## Testing Steps
1. Use Swagger UI to test the endpoints
2. Register/Login as a user
3. Add a stock to watchlist
4. Get watchlist and verify the stock appears
5. Try adding the same stock again (should handle gracefully)
6. Remove the stock from watchlist
7. Verify the watchlist is empty

## Bonus Challenges (Optional)
- [ ] Add a check to prevent adding the same stock twice
- [ ] Return additional information like how long the stock has been on the watchlist
- [ ] Add ability to add notes when adding to watchlist

## Tips
- Follow the existing pattern used in `PortfolioController` and `PortfolioRepository`
- Look at how `Portfolio` model is structured - your `Watchlist` model should be very similar
- Use `User.GetUsername()` extension method to get the current logged-in user
- Test thoroughly with Swagger before considering it complete

## Resources
- Review `PortfolioController.cs` for reference
- Review `Portfolio.cs` model for the relationship structure
- Check how authorization is handled in existing controllers

Good luck!
