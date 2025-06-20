using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.Server.Services.Interfaces
{
    public interface IAIService
    {
        Task<AiRecomendationResponse?> GetRecommendationsAsync(List<SaleRecord> sales, List<Review> reviews);
    }
}
