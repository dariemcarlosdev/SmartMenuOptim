using SmartMenuOptim.Shared.Models;

namespace SmartMenuOptim.Server.Services
{
    public interface IAIService
    {
        InsightResponse GetMenuRecomendation();
        IEnumerable<Review> AnalizeReviews();
    }
}
