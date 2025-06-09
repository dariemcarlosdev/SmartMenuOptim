using SmartMenuOptim.Shared.Models;

namespace SmartMenuOptim.Server.Services
{
    public class AIService : IAIService
    {
        public InsightResponse GetMenuRecomendation() => new InsightResponse
        {
            ConfidenceScore = 0.95,
            Recomendation = "Promote Ribeye Steak on weekends."
        };

        public IEnumerable<Review> AnalizeReviews() => new List<Review>
        {
            new Review
            {
                Id = 1,
                CustomerName = "John Doe",
                Comment = "The food was amazing!",
                SentimentScore = 0.9
            },
            new Review
            {
                Id = 2,
                CustomerName = "Jane Smith",
                Comment = "Not satisfied with the service.",
                SentimentScore = -0.5
            }
        };

    }
}
