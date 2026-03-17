using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Transaction;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class TransactionRepository : ITransactionRepository
    {
        public readonly ApplicationDBContext _context;
        public TransactionRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<Transaction> CreateAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<List<Transaction>> GetAllUserTransactionsForExportAsync(AppUser user, TransactionQueryObject query)
        {
            var transactions = _context.Transactions
                .Include(t => t.Stock)
                .Where(t => t.AppUserId == user.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.StockSymbol))
            {
                transactions = transactions.Where(t => t.Stock.Sympol.Contains(query.StockSymbol));
            }

            if (query.Type.HasValue)
            {
                transactions = transactions.Where(t => t.Type == query.Type.Value);
            }

            if (query.StartDate.HasValue)
            {
                transactions = transactions.Where(t => t.TransactionDate >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                transactions = transactions.Where(t => t.TransactionDate <= query.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                if (query.SortBy.Equals("Date", StringComparison.OrdinalIgnoreCase))
                {
                    transactions = query.IsDescending
                        ? transactions.OrderByDescending(t => t.TransactionDate)
                        : transactions.OrderBy(t => t.TransactionDate);
                }
                else if (query.SortBy.Equals("Amount", StringComparison.OrdinalIgnoreCase))
                {
                    transactions = query.IsDescending
                        ? transactions.OrderByDescending(t => t.TotalAmount)
                        : transactions.OrderBy(t => t.TotalAmount);
                }
            }
            else
            {
                transactions = transactions.OrderByDescending(t => t.TransactionDate);
            }

            return await transactions.ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(int id)
        {
            return await _context.Transactions.Include(t => t.Stock).Include(t => t.AppUser)
            .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<RealizedGainLossDto>> GetRealizedGainLossAsync(AppUser user)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Stock)
                .Where(t => t.AppUserId == user.Id)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync();

            var result = new List<RealizedGainLossDto>();

            var groupedByStock = transactions.GroupBy(t => new { t.StockId, t.Stock.Sympol, t.Stock.CompanyName });

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
                    Symbol = group.Key.Sympol,
                    CompanyName = group.Key.CompanyName,
                    RealizedGainLoss = realizedGainLoss
                });
            }

            return result;
        }

        public async Task<TransactionSummaryDto> GetUserSummaryAsync(AppUser user)
        {
            var transactions = await _context.Transactions
                .Where(t => t.AppUserId == user.Id)
                .ToListAsync();

            var totalInvested = transactions
                .Where(t => t.Type == TransactionType.Buy)
                .Sum(t => t.TotalAmount);

            var totalFromSales = transactions
                .Where(t => t.Type == TransactionType.Sell)
                .Sum(t => t.TotalAmount);

            var totalBuyTransactions = transactions
                .Count(t => t.Type == TransactionType.Buy);

            var totalSellTransactions = transactions
                .Count(t => t.Type == TransactionType.Sell);

            return new TransactionSummaryDto
            {
                TotalInvested = totalInvested,
                TotalFromSales = totalFromSales,
                TotalBuyTransactions = totalBuyTransactions,
                TotalSellTransactions = totalSellTransactions,
                NetInvestment = totalInvested - totalFromSales
            };
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(AppUser user, TransactionQueryObject query)
        {
            var transactions = _context.Transactions
                .Include(t => t.Stock)
                .Where(t => t.AppUserId == user.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.StockSymbol))
            {
                transactions = transactions.Where(t => t.Stock.Sympol.Contains(query.StockSymbol));
            }

            if (query.Type.HasValue)
            {
                transactions = transactions.Where(t => t.Type == query.Type.Value);
            }

            if (query.StartDate.HasValue)
            {
                transactions = transactions.Where(t => t.TransactionDate >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                transactions = transactions.Where(t => t.TransactionDate <= query.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                if (query.SortBy.Equals("Date", StringComparison.OrdinalIgnoreCase))
                {
                    transactions = query.IsDescending
                        ? transactions.OrderByDescending(t => t.TransactionDate)
                        : transactions.OrderBy(t => t.TransactionDate);
                }
                else if (query.SortBy.Equals("Amount", StringComparison.OrdinalIgnoreCase))
                {
                    transactions = query.IsDescending
                        ? transactions.OrderByDescending(t => t.TotalAmount)
                        : transactions.OrderBy(t => t.TotalAmount);
                }
            }
            else
            {
                transactions = transactions.OrderByDescending(t => t.TransactionDate);
            }

            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            transactions = transactions
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return await transactions.ToListAsync();
        }

        public async Task<List<Transaction>> GetUserTransactionsForStockAsync(AppUser user, int stockId)
        {
            return await _context.Transactions
                .Include(t => t.Stock)
                .Where(t => t.AppUserId == user.Id && t.StockId == stockId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }
    }
}