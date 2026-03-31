using MediatR;
using api.Models;
using api.Data;
using api.Dtos.Stock;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace api.Features.Stock.Commands
{
    public class CreateStockCommand : IRequest<api.Models.Stock>
    {
        public api.Models.Stock StockModel { get; set; }
    }

    public class CreateStockHandler : IRequestHandler<CreateStockCommand, api.Models.Stock>
    {
        private readonly ApplicationDBContext _context;
        public CreateStockHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Stock> Handle(CreateStockCommand request, CancellationToken cancellationToken)
        {
            await _context.Stocks.AddAsync(request.StockModel, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return request.StockModel;
        }
    }

    public class UpdateStockCommand : IRequest<api.Models.Stock?>
    {
        public int Id { get; set; }
        public UpdateStockRequestDto UpdateDto { get; set; }
    }

    public class UpdateStockHandler : IRequestHandler<UpdateStockCommand, api.Models.Stock?>
    {
        private readonly ApplicationDBContext _context;
        public UpdateStockHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Stock?> Handle(UpdateStockCommand request, CancellationToken cancellationToken)
        {
            var existingStock = await _context.Stocks.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (existingStock == null) return null;

            existingStock.Symbol = request.UpdateDto.Symbol;
            existingStock.CompanyName = request.UpdateDto.CompanyName;
            existingStock.Purchase = request.UpdateDto.Purchase;
            existingStock.LastDiv = request.UpdateDto.LastDiv;
            existingStock.Industry = request.UpdateDto.Industry;
            existingStock.MarketCap = request.UpdateDto.MarketCap;

            await _context.SaveChangesAsync(cancellationToken);
            return existingStock;
        }
    }

    public class DeleteStockCommand : IRequest<api.Models.Stock?>
    {
        public int Id { get; set; }
    }

    public class DeleteStockHandler : IRequestHandler<DeleteStockCommand, api.Models.Stock?>
    {
        private readonly ApplicationDBContext _context;
        public DeleteStockHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Stock?> Handle(DeleteStockCommand request, CancellationToken cancellationToken)
        {
            var stockModel = await _context.Stocks.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (stockModel == null) return null;

            _context.Stocks.Remove(stockModel);
            await _context.SaveChangesAsync(cancellationToken);
            return stockModel;
        }
    }
}
