using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Transaction;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface ITransactionRepository
    {
        Task<List<Transaction>> GetUserTransactionsAsync(AppUser user, TransactionQueryObject query);
        Task<Transaction?> GetByIdAsync(int id);
        Task<Transaction> CreateAsync(Transaction transaction);
        Task<TransactionSummaryDto> GetUserSummaryAsync(AppUser user);
        Task<List<Transaction>> GetUserTransactionsForStockAsync(AppUser user, int stockId);
        Task<List<RealizedGainLossDto>> GetRealizedGainLossAsync(AppUser user);
        Task<List<Transaction>> GetAllUserTransactionsForExportAsync(AppUser user, TransactionQueryObject query);
    }
}