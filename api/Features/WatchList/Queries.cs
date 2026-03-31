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

namespace api.Features.WatchList.Queries
{
    public class GetUserWatchListQuery : IRequest<List<WatchListDto>>
    {
        public AppUser User { get; set; }
    }

    public class GetUserWatchListHandler : IRequestHandler<GetUserWatchListQuery, List<WatchListDto>>
    {
        private readonly ApplicationDBContext _context;
        public GetUserWatchListHandler(ApplicationDBContext context) => _context = context;
        public async Task<List<WatchListDto>> Handle(GetUserWatchListQuery request, CancellationToken cancellationToken)
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
                DaysOnWatchList = EF.Functions.DateDiffDay(w.AddedOn, DateTime.UtcNow)
            }).ToListAsync(cancellationToken);
        }
    }
}
