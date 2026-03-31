using MediatR;
using api.Models;
using api.Data;
using api.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace api.Features.Stock.Queries
{
    public class GetAllStocksQuery : IRequest<List<api.Models.Stock>>
    {
        public QueryObject Query { get; set; }
    }

    public class GetAllStocksHandler : IRequestHandler<GetAllStocksQuery, List<api.Models.Stock>>
    {
        private readonly ApplicationDBContext _context;
        public GetAllStocksHandler(ApplicationDBContext context) => _context = context;

        public async Task<List<api.Models.Stock>> Handle(GetAllStocksQuery request, CancellationToken cancellationToken)
        {
            var stocks = _context.Stocks.Include(c => c.Comments)
                .ThenInclude(a => a.AppUser).Include(r => r.Ratings).AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Query.CompanyName))
                stocks = stocks.Where(s => s.CompanyName.Contains(request.Query.CompanyName));
            if (!string.IsNullOrWhiteSpace(request.Query.Symbol))
                stocks = stocks.Where(s => s.Symbol.Contains(request.Query.Symbol));

            if (!string.IsNullOrWhiteSpace(request.Query.SortBy) && request.Query.SortBy.Equals("Symbol", System.StringComparison.OrdinalIgnoreCase))
                stocks = request.Query.IsDecsending ? stocks.OrderByDescending(s => s.Symbol) : stocks.OrderBy(s => s.Symbol);

            var skipNumber = (request.Query.PageNumber - 1) * request.Query.PageSize;
            return await stocks.Skip(skipNumber).Take(request.Query.PageSize).ToListAsync(cancellationToken);
        }
    }

    public class GetStockByIdQuery : IRequest<api.Models.Stock?>
    {
        public int Id { get; set; }
    }

    public class GetStockByIdHandler : IRequestHandler<GetStockByIdQuery, api.Models.Stock?>
    {
        private readonly ApplicationDBContext _context;
        public GetStockByIdHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Stock?> Handle(GetStockByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Stocks.Include(c => c.Comments).FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
        }
    }

    public class GetStockBySymbolQuery : IRequest<api.Models.Stock?>
    {
        public string Symbol { get; set; }
    }

    public class GetStockBySymbolHandler : IRequestHandler<GetStockBySymbolQuery, api.Models.Stock?>
    {
        private readonly ApplicationDBContext _context;
        public GetStockBySymbolHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Stock?> Handle(GetStockBySymbolQuery request, CancellationToken cancellationToken)
        {
            return await _context.Stocks.FirstOrDefaultAsync(s => s.Symbol == request.Symbol, cancellationToken);
        }
    }

    public class CheckStockExistsQuery : IRequest<bool>
    {
        public int Id { get; set; }
    }

    public class CheckStockExistsHandler : IRequestHandler<CheckStockExistsQuery, bool>
    {
        private readonly ApplicationDBContext _context;
        public CheckStockExistsHandler(ApplicationDBContext context) => _context = context;
        public async Task<bool> Handle(CheckStockExistsQuery request, CancellationToken cancellationToken)
        {
            return await _context.Stocks.AnyAsync(s => s.Id == request.Id, cancellationToken);
        }
    }
}
