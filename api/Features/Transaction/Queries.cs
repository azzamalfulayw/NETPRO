using MediatR;
using api.Models;
using api.Data;
using api.Helpers;
using api.Dtos.Transaction;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace api.Features.Transaction.Queries
{
    public class GetUserTransactionsQuery : IRequest<List<api.Models.Transaction>>
    {
        public AppUser User { get; set; }
        public TransactionQueryObject Query { get; set; }
    }

    public class GetUserTransactionsHandler : IRequestHandler<GetUserTransactionsQuery, List<api.Models.Transaction>>
    {
        private readonly ApplicationDBContext _context;
        public GetUserTransactionsHandler(ApplicationDBContext context) => _context = context;

        public async Task<List<api.Models.Transaction>> Handle(GetUserTransactionsQuery request, CancellationToken cancellationToken)
        {
            var user = request.User;
            var query = request.Query;

            var transactions = _context.Transactions
                .Include(t => t.Stock)
                .Where(t => t.AppUserId == user.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.StockSymbol))
                transactions = transactions.Where(t => t.Stock.Symbol.Contains(query.StockSymbol));

            if (query.Type.HasValue)
                transactions = transactions.Where(t => t.Type == query.Type.Value);

            if (query.StartDate.HasValue)
                transactions = transactions.Where(t => t.TransactionDate >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                transactions = transactions.Where(t => t.TransactionDate <= query.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                if (query.SortBy.Equals("Date", StringComparison.OrdinalIgnoreCase))
                    transactions = query.IsDescending ? transactions.OrderByDescending(t => t.TransactionDate) : transactions.OrderBy(t => t.TransactionDate);
                else if (query.SortBy.Equals("Amount", StringComparison.OrdinalIgnoreCase))
                    transactions = query.IsDescending ? transactions.OrderByDescending(t => t.TotalAmount) : transactions.OrderBy(t => t.TotalAmount);
            }
            else
            {
                transactions = transactions.OrderByDescending(t => t.TransactionDate);
            }

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            return await transactions.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        }
    }

    public class GetTransactionByIdQuery : IRequest<api.Models.Transaction?>
    {
        public int Id { get; set; }
    }

    public class GetTransactionByIdHandler : IRequestHandler<GetTransactionByIdQuery, api.Models.Transaction?>
    {
        private readonly ApplicationDBContext _context;
        public GetTransactionByIdHandler(ApplicationDBContext context) => _context = context;
        public async Task<api.Models.Transaction?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Transactions.Include(t => t.Stock).Include(t => t.AppUser)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        }
    }

    public class GetRealizedGainLossQuery : IRequest<List<RealizedGainLossDto>>
    {
        public AppUser User { get; set; }
    }

    public class GetRealizedGainLossHandler : IRequestHandler<GetRealizedGainLossQuery, List<RealizedGainLossDto>>
    {
        private readonly ApplicationDBContext _context;
        public GetRealizedGainLossHandler(ApplicationDBContext context) => _context = context;
        public async Task<List<RealizedGainLossDto>> Handle(GetRealizedGainLossQuery request, CancellationToken cancellationToken)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Stock)
                .Where(t => t.AppUserId == request.User.Id)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync(cancellationToken);

            var result = new List<RealizedGainLossDto>();
            var groupedByStock = transactions.GroupBy(t => new { t.StockId, t.Stock.Symbol, t.Stock.CompanyName });

            foreach (var group in groupedByStock)
            {
                decimal totalCost = 0;
                int totalShares = 0;
                decimal realizedGainLoss = 0;

                foreach (var transaction in group)
                {
                    if (transaction.Type == TransactionType.Buy)
                    {
                        totalCost += transaction.TotalAmount;
                        totalShares += transaction.Quantity;
                    }
                    else if (transaction.Type == TransactionType.Sell && totalShares > 0)
                    {
                        var averageCostPerShare = totalCost / totalShares;
                        var costBasis = averageCostPerShare * transaction.Quantity;
                        var saleAmount = transaction.TotalAmount;

                        realizedGainLoss += saleAmount - costBasis;
                        totalCost -= costBasis;
                        totalShares -= transaction.Quantity;
                    }
                }

                result.Add(new RealizedGainLossDto
                {
                    StockId = group.Key.StockId,
                    Symbol = group.Key.Symbol,
                    CompanyName = group.Key.CompanyName,
                    RealizedGainLoss = realizedGainLoss
                });
            }

            return result;
        }
    }

    public class GetUserSummaryQuery : IRequest<TransactionSummaryDto>
    {
        public AppUser User { get; set; }
    }

    public class GetUserSummaryHandler : IRequestHandler<GetUserSummaryQuery, TransactionSummaryDto>
    {
        private readonly ApplicationDBContext _context;
        public GetUserSummaryHandler(ApplicationDBContext context) => _context = context;

        public async Task<TransactionSummaryDto> Handle(GetUserSummaryQuery request, CancellationToken cancellationToken)
        {
            var transactions = await _context.Transactions
                .Where(t => t.AppUserId == request.User.Id)
                .ToListAsync(cancellationToken);

            var totalInvested = transactions.Where(t => t.Type == TransactionType.Buy).Sum(t => t.TotalAmount);
            var totalFromSales = transactions.Where(t => t.Type == TransactionType.Sell).Sum(t => t.TotalAmount);

            return new TransactionSummaryDto
            {
                TotalInvested = totalInvested,
                TotalFromSales = totalFromSales,
                TotalBuyTransactions = transactions.Count(t => t.Type == TransactionType.Buy),
                TotalSellTransactions = transactions.Count(t => t.Type == TransactionType.Sell),
                NetInvestment = totalInvested - totalFromSales
            };
        }
    }

    public class GetTransactionsForStockQuery : IRequest<List<api.Models.Transaction>>
    {
        public AppUser User { get; set; }
        public int StockId { get; set; }
    }

    public class GetTransactionsForStockHandler : IRequestHandler<GetTransactionsForStockQuery, List<api.Models.Transaction>>
    {
        private readonly ApplicationDBContext _context;
        public GetTransactionsForStockHandler(ApplicationDBContext context) => _context = context;
        public async Task<List<api.Models.Transaction>> Handle(GetTransactionsForStockQuery request, CancellationToken cancellationToken)
        {
            return await _context.Transactions
                .Include(t => t.Stock)
                .Where(t => t.AppUserId == request.User.Id && t.StockId == request.StockId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync(cancellationToken);
        }
    }

    public class GetAllUserTransactionsForExportQuery : IRequest<List<api.Models.Transaction>>
    {
        public AppUser User { get; set; }
        public TransactionQueryObject Query { get; set; }
    }

    public class GetAllUserTransactionsForExportHandler : IRequestHandler<GetAllUserTransactionsForExportQuery, List<api.Models.Transaction>>
    {
        private readonly ApplicationDBContext _context;
        public GetAllUserTransactionsForExportHandler(ApplicationDBContext context) => _context = context;

        public async Task<List<api.Models.Transaction>> Handle(GetAllUserTransactionsForExportQuery request, CancellationToken cancellationToken)
        {
            var user = request.User;
            var query = request.Query;

            var transactions = _context.Transactions
                .Include(t => t.Stock)
                .Where(t => t.AppUserId == user.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.StockSymbol))
                transactions = transactions.Where(t => t.Stock.Symbol.Contains(query.StockSymbol));
            if (query.Type.HasValue)
                transactions = transactions.Where(t => t.Type == query.Type.Value);
            if (query.StartDate.HasValue)
                transactions = transactions.Where(t => t.TransactionDate >= query.StartDate.Value);
            if (query.EndDate.HasValue)
                transactions = transactions.Where(t => t.TransactionDate <= query.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                if (query.SortBy.Equals("Date", StringComparison.OrdinalIgnoreCase))
                    transactions = query.IsDescending ? transactions.OrderByDescending(t => t.TransactionDate) : transactions.OrderBy(t => t.TransactionDate);
                else if (query.SortBy.Equals("Amount", StringComparison.OrdinalIgnoreCase))
                    transactions = query.IsDescending ? transactions.OrderByDescending(t => t.TotalAmount) : transactions.OrderBy(t => t.TotalAmount);
            }
            else
            {
                transactions = transactions.OrderByDescending(t => t.TransactionDate);
            }

            return await transactions.ToListAsync(cancellationToken);
        }
    }
}
