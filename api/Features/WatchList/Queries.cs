using MediatR;
using api.Models;
using api.Data;
using api.Dtos.WatchList;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using System.Threading.Tasks;
using api.Interfaces;

namespace api.Features.WatchList.Queries
{
    public class GetUserWatchListQuery : IRequest<List<WatchListDto>>
    {
        public required AppUser User { get; set; }
    }

    public class GetUserWatchListHandler : IRequestHandler<GetUserWatchListQuery, List<WatchListDto>>
    {
        private readonly ApplicationDBContext _context;
        private readonly IRedisCacheService _redisCacheService;

        public GetUserWatchListHandler(ApplicationDBContext context, IRedisCacheService redisCacheService)
        {
            _context = context;
            _redisCacheService = redisCacheService;
        }

        public async Task<List<WatchListDto>> Handle(GetUserWatchListQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"watchlist:user:{request.User.Id}";
            return await _redisCacheService.GetOrAddAsync(cacheKey, async () =>
            {
                return await _context.WatchLists.Where(w => w.AppUserId == request.User.Id)
                .Select(w => new WatchListDto
                {
                    StockId = w.StockId,
                    Symbol = w.Stock.Symbol,
                    CompanyName = w.Stock.CompanyName,
                    Purchase = w.Stock.Purchase,
                    LastDiv = w.Stock.LastDiv,
                    Industry = w.Stock.Industry,
                    MarketCap = w.Stock.MarketCap,
                    AddedOn = w.AddedOn,
                    Notes = w.Notes,
                    DaysOnWatchList = EF.Functions.DateDiffDay(w.AddedOn, DateTime.UtcNow),
                    AverageRating = w.Stock.Ratings.Any() ? (decimal)w.Stock.Ratings.Average(r => r.Score) : 0,
                    RatingCount = w.Stock.Ratings.Count()
                }).ToListAsync(cancellationToken);
            }, TimeSpan.FromMinutes(15)) ?? new List<WatchListDto>();
        }
    }
}
