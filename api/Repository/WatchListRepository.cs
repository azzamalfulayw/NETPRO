using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.WatchList;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class WatchListRepository : IWatchListRepository
    {
        private readonly ApplicationDBContext _context;

        public WatchListRepository(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task<WatchList> CreateAsync(WatchList watchlist)
        {
            await _context.WatchLists.AddAsync(watchlist);
            await _context.SaveChangesAsync();
            return watchlist;
        }

        public async Task<WatchList?> DeleteAsync(AppUser user, int stockId)
        {
            var watchlistModel = await _context.WatchLists
                .FirstOrDefaultAsync(x => x.AppUserId == user.Id && x.StockId == stockId);

            if (watchlistModel == null)
            {
                return null;
            }

            _context.WatchLists.Remove(watchlistModel);
            await _context.SaveChangesAsync();

            return watchlistModel;
        }

        public async Task<List<WatchListDto>> GetUserWatchList(AppUser user)
        {
            return await _context.WatchLists.Where(w => w.AppUserId == user.Id)
            .Select(w => new WatchListDto
            {
                StockId = w.StockId,
                Symbol = w.Stock.Sympol,
                CompanyName = w.Stock.CompanyName,
                Purchase = w.Stock.Purchase,
                LastDiv = w.Stock.LastDiv,
                Industry = w.Stock.Industry,
                MarketCap = w.Stock.MarketCap,
                AddedOn = w.AddedOn,
                 Notes = w.Notes,
                DaysOnWatchList = EF.Functions.DateDiffDay(w.AddedOn, DateTime.UtcNow)
            }).ToListAsync();
        }
    }
}