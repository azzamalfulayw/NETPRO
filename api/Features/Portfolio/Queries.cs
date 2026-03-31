using MediatR;
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace api.Features.Portfolio.Queries
{
    public class GetUserPortfolioQuery : IRequest<List<api.Models.Stock>>
    {
        public AppUser User { get; set; }
    }

    public class GetUserPortfolioHandler : IRequestHandler<GetUserPortfolioQuery, List<api.Models.Stock>>
    {
        private readonly ApplicationDBContext _context;
        public GetUserPortfolioHandler(ApplicationDBContext context) => _context = context;
        public async Task<List<api.Models.Stock>> Handle(GetUserPortfolioQuery request, CancellationToken cancellationToken)
        {
            return await _context.portfolios.Where(u => u.AppUserId == request.User.Id)
            .Select(stock => new api.Models.Stock
            {
                Id = stock.StockId,
                Symbol = stock.Stock.Symbol,
                CompanyName = stock.Stock.CompanyName,
                Purchase = stock.Stock.Purchase,
                LastDiv = stock.Stock.LastDiv,
                Industry = stock.Stock.Industry,
                MarketCap = stock.Stock.MarketCap
            }).ToListAsync(cancellationToken);
        }
    }
}
