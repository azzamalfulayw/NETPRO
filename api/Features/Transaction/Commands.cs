using MediatR;
using api.Models;
using api.Data;
using api.Dtos.Transaction;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace api.Features.Transaction.Commands
{
    public class CreateTransactionCommand : IRequest<string>
    {
        public AppUser AppUser { get; set; }
        public CreateTransactionDto TransactionDto { get; set; }
    }

    public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, string>
    {
        private readonly ApplicationDBContext _context;
        public CreateTransactionHandler(ApplicationDBContext context) => _context = context;

        public async Task<string> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
        {
            var appUser = request.AppUser;
            var transactionDto = request.TransactionDto;

            if (transactionDto.Quantity <= 0) return "Error: Quantity must be greater than 0";
            if (transactionDto.PricePerShare <= 0) return "Error: Price per share must be greater than 0";

            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Id == transactionDto.StockId, cancellationToken);
            if (stock == null) return "Error: Stock not found";

            var portfolioItem = await _context.portfolios
                .FirstOrDefaultAsync(p => p.AppUserId == appUser.Id && p.StockId == transactionDto.StockId, cancellationToken);

            if (transactionDto.Type == TransactionType.Sell)
            {
                if (portfolioItem == null || portfolioItem.Quantity < transactionDto.Quantity)
                    return "Error: You do not own enough shares to sell";
            }

            var transaction = new api.Models.Transaction
            {
                AppUserId = appUser.Id,
                StockId = transactionDto.StockId,
                Type = transactionDto.Type,
                Quantity = transactionDto.Quantity,
                PricePerShare = transactionDto.PricePerShare,
                TotalAmount = transactionDto.Quantity * transactionDto.PricePerShare,
                TransactionDate = transactionDto.TransactionDate ?? DateTime.UtcNow,
                Category = transactionDto.Category,
                Notes = transactionDto.Notes
            };

            await _context.Transactions.AddAsync(transaction, cancellationToken);

            if (transactionDto.Type == TransactionType.Buy)
            {
                if (portfolioItem == null)
                {
                    portfolioItem = new Portfolio
                    {
                        AppUserId = appUser.Id,
                        StockId = transactionDto.StockId,
                        Quantity = transactionDto.Quantity
                    };
                    await _context.portfolios.AddAsync(portfolioItem, cancellationToken);
                }
                else
                {
                    portfolioItem.Quantity += transactionDto.Quantity;
                }
            }
            else if (transactionDto.Type == TransactionType.Sell)
            {
                portfolioItem.Quantity -= transactionDto.Quantity;
                if (portfolioItem.Quantity == 0)
                {
                    _context.portfolios.Remove(portfolioItem);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return "Success";
        }
    }
}
