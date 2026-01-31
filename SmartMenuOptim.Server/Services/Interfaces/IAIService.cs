using SmartMenuOptim.Application.Dtos;

namespace SmartMenuOptim.Server.Services.Interfaces
{
    public interface IAIService
    {
        Task<List<AiRecomendationResponseDTO>?> GetRecommendationsAsync(List<SaleRecordDTO> sales, List<ReviewDTO> reviews);
        Task<string> GetImprovementStrategyAsync(UnderperformingDishDTO underperformingDishes);
        Task<List<UnderperformingDishDTO>> GetUnderperformingDishesAsync();
    }
}
