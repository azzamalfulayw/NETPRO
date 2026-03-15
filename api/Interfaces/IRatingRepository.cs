using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface IRatingRepository
    {
        Task<List<Rating>> GetAllAsync();
        Task<Rating?> GetByIdAsync(int id);
        Task<Rating?> CreateAsync(Rating ratingModel);
        Task<Rating?> UpdateAsync(int id, Rating ratingModel);
        Task<Rating?> DeleteAsync(int id); 
        Task<Rating?> GetUserRatingForStockAsync(string appUserId, int stockId);
    }
}