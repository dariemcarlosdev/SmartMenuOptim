using SmartMenuOptim.Shared.Data.Dtos;
using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.Server.Services.Interfaces
{
    public interface IAIService
    {
        Task<AiRecomendationResponse?> GetRecommendationsAsync(List<SaleRecord> sales, List<Review> reviews);
        Task<string> GetImprovementStrategyAsync(UnderperformingDishDTO underperformingDishes);
        Task<List<UnderperformingDishDTO>> GetUnderperformingDishesAsync();
    }
}
