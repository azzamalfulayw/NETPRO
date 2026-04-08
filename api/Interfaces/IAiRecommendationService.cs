using System.Threading.Tasks;
using api.Dtos.AiRecommendation;

namespace api.Interfaces
{
    public interface IAiRecommendationService
    {
        Task<AiRecommendationResponseDto?> GetRecommendationAsync(string symbol);
    }
}
