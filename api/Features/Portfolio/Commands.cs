using MediatR;
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace api.Features.Portfolio.Commands
{
    public class CreatePortfolioCommand : IRequest<api.Models.Portfolio>
    {
        public api.Models.Portfolio Portfolio { get; set; }
    }

    public class CreatePortfolioHandler : IRequestHandler<CreatePortfolioCommand, api.Models.Portfolio>
    {
        private readonly ApplicationDBContext _context;
        public CreatePortfolioHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Portfolio> Handle(CreatePortfolioCommand request, CancellationToken cancellationToken)
        {
            await _context.portfolios.AddAsync(request.Portfolio, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return request.Portfolio;
        }
    }

    public class DeletePortfolioCommand : IRequest<api.Models.Portfolio?>
    {
        public AppUser AppUser { get; set; }
        public string Symbol { get; set; }
    }

    public class DeletePortfolioHandler : IRequestHandler<DeletePortfolioCommand, api.Models.Portfolio?>
    {
        private readonly ApplicationDBContext _context;
        public DeletePortfolioHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Portfolio?> Handle(DeletePortfolioCommand request, CancellationToken cancellationToken)
        {
            var portfolioModel = await _context.portfolios.FirstOrDefaultAsync(x => x.AppUserId == request.AppUser.Id && x.Stock.Symbol.ToLower() == request.Symbol.ToLower(), cancellationToken);
            if (portfolioModel == null) return null;

            _context.portfolios.Remove(portfolioModel);
            await _context.SaveChangesAsync(cancellationToken);
            return portfolioModel;
        }
    }
}
