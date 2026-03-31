using MediatR;
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace api.Features.WatchList.Commands
{
    public class CreateWatchListCommand : IRequest<api.Models.WatchList>
    {
        public api.Models.WatchList WatchList { get; set; }
    }

    public class CreateWatchListHandler : IRequestHandler<CreateWatchListCommand, api.Models.WatchList>
    {
        private readonly ApplicationDBContext _context;
        public CreateWatchListHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.WatchList> Handle(CreateWatchListCommand request, CancellationToken cancellationToken)
        {
            await _context.WatchLists.AddAsync(request.WatchList, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return request.WatchList;
        }
    }

    public class DeleteWatchListCommand : IRequest<api.Models.WatchList?>
    {
        public AppUser AppUser { get; set; }
        public int StockId { get; set; }
    }

    public class DeleteWatchListHandler : IRequestHandler<DeleteWatchListCommand, api.Models.WatchList?>
    {
        private readonly ApplicationDBContext _context;
        public DeleteWatchListHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.WatchList?> Handle(DeleteWatchListCommand request, CancellationToken cancellationToken)
        {
            var watchlistModel = await _context.WatchLists
                .FirstOrDefaultAsync(x => x.AppUserId == request.AppUser.Id && x.StockId == request.StockId, cancellationToken);

            if (watchlistModel == null) return null;

            _context.WatchLists.Remove(watchlistModel);
            await _context.SaveChangesAsync(cancellationToken);

            return watchlistModel;
        }
    }
}
