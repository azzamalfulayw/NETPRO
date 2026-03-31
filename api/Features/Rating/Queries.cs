using MediatR;
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace api.Features.Rating.Queries
{
    public class GetAllRatingsQuery : IRequest<List<api.Models.Rating>> { }

    public class GetAllRatingsHandler : IRequestHandler<GetAllRatingsQuery, List<api.Models.Rating>>
    {
        private readonly ApplicationDBContext _context;
        public GetAllRatingsHandler(ApplicationDBContext context) => _context = context;
        public async Task<List<api.Models.Rating>> Handle(GetAllRatingsQuery request, CancellationToken cancellationToken)
        {
            return await _context.Ratings.Include(a => a.AppUser).ToListAsync(cancellationToken);
        }
    }

    public class GetRatingByIdQuery : IRequest<api.Models.Rating?>
    {
        public int Id { get; set; }
    }

    public class GetRatingByIdHandler : IRequestHandler<GetRatingByIdQuery, api.Models.Rating?>
    {
        private readonly ApplicationDBContext _context;
        public GetRatingByIdHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Rating?> Handle(GetRatingByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Ratings.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        }
    }

    public class GetUserRatingForStockQuery : IRequest<api.Models.Rating?>
    {
        public string AppUserId { get; set; }
        public int StockId { get; set; }
    }

    public class GetUserRatingForStockHandler : IRequestHandler<GetUserRatingForStockQuery, api.Models.Rating?>
    {
        private readonly ApplicationDBContext _context;
        public GetUserRatingForStockHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Rating?> Handle(GetUserRatingForStockQuery request, CancellationToken cancellationToken)
        {
            return await _context.Ratings.FirstOrDefaultAsync(r => r.AppUserId == request.AppUserId && r.StockId == request.StockId, cancellationToken);
        }
    }
}
