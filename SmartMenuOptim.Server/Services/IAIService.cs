using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.Server.Services
{
    public interface IAIService
    {
        Task<AiRecomendationResponse?> GetRecommendationsAsync(List<SaleRecord> sales, List<Review> reviews);
    }
}
