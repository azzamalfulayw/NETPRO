using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.WatchList;
using api.Models;

namespace api.Interfaces
{
    public interface IWatchListRepository
    {
        Task<List<WatchListDto>> GetUserWatchList(AppUser user);
        Task<WatchList> CreateAsync(WatchList watchlist);
        Task<WatchList?> DeleteAsync(AppUser user, int stockId);
    }
}