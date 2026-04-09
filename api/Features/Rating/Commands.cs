using MediatR;
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using api.Interfaces;

namespace api.Features.Rating.Commands
{
    public class CreateRatingCommand : IRequest<api.Models.Rating>
    {
        public api.Models.Rating RatingModel { get; set; }
    }

    public class CreateRatingHandler : IRequestHandler<CreateRatingCommand, api.Models.Rating>
    {
        private readonly ApplicationDBContext _context;
        private readonly IRedisCacheService _redisCacheService;

        public CreateRatingHandler(ApplicationDBContext context, IRedisCacheService redisCacheService)
        {
            _context = context;
            _redisCacheService = redisCacheService;
        }

        public async Task<api.Models.Rating> Handle(CreateRatingCommand request, CancellationToken cancellationToken)
        {
            await _context.Ratings.AddAsync(request.RatingModel, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var stock = await _context.Stocks.FindAsync(request.RatingModel.StockId);
            if (stock != null)
            {
                await _redisCacheService.RemoveAsync($"stock:detail:{stock.Id}");
                await _redisCacheService.RemoveAsync($"stock:symbol:{stock.Symbol.ToUpper()}");
            }

            return request.RatingModel;
        }
    }

    public class UpdateRatingCommand : IRequest<api.Models.Rating?>
    {
        public int Id { get; set; }
        public api.Models.Rating RatingModel { get; set; }
    }

    public class UpdateRatingHandler : IRequestHandler<UpdateRatingCommand, api.Models.Rating?>
    {
        private readonly ApplicationDBContext _context;
        private readonly IRedisCacheService _redisCacheService;

        public UpdateRatingHandler(ApplicationDBContext context, IRedisCacheService redisCacheService)
        {
            _context = context;
            _redisCacheService = redisCacheService;
        }

        public async Task<api.Models.Rating?> Handle(UpdateRatingCommand request, CancellationToken cancellationToken)
        {
            var existingRating = await _context.Ratings.FindAsync(request.Id);
            if (existingRating == null) return null;

            existingRating.Score = request.RatingModel.Score;
            await _context.SaveChangesAsync(cancellationToken);

            var stock = await _context.Stocks.FindAsync(existingRating.StockId);
            if (stock != null)
            {
                await _redisCacheService.RemoveAsync($"stock:detail:{stock.Id}");
                await _redisCacheService.RemoveAsync($"stock:symbol:{stock.Symbol.ToUpper()}");
            }

            return existingRating;
        }
    }

    public class DeleteRatingCommand : IRequest<api.Models.Rating?>
    {
        public int Id { get; set; }
    }

    public class DeleteRatingHandler : IRequestHandler<DeleteRatingCommand, api.Models.Rating?>
    {
        private readonly ApplicationDBContext _context;
        private readonly IRedisCacheService _redisCacheService;

        public DeleteRatingHandler(ApplicationDBContext context, IRedisCacheService redisCacheService)
        {
            _context = context;
            _redisCacheService = redisCacheService;
        }

        public async Task<api.Models.Rating?> Handle(DeleteRatingCommand request, CancellationToken cancellationToken)
        {
            var ratingModel = await _context.Ratings.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (ratingModel == null) return null;

            _context.Ratings.Remove(ratingModel);
            await _context.SaveChangesAsync(cancellationToken);

            var stock = await _context.Stocks.FindAsync(ratingModel.StockId);
            if (stock != null)
            {
                await _redisCacheService.RemoveAsync($"stock:detail:{stock.Id}");
                await _redisCacheService.RemoveAsync($"stock:symbol:{stock.Symbol.ToUpper()}");
            }

            return ratingModel;
        }
    }
}
