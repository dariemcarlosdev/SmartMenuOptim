using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.Domain.Specifications.ReviewSpecifications
{
    /// <summary>
    /// Specification for filtering reviews with optional dish name and sentiment score criteria.
    /// </summary>
    public class FilteredReviewsSpecification : BaseSpecification<Review>
    {
        /// <summary>
        /// Initializes a specification to filter reviews by optional dish name and sentiment score.
        /// </summary>
        /// <param name="dishName">Optional dish name to filter by (case-insensitive).</param>
        /// <param name="targetSentiment">Optional sentiment score to filter by.</param>
        /// <param name="sentimentTolerance">Tolerance range for sentiment matching (default: 0.03).</param>
        public FilteredReviewsSpecification(
            string? dishName = null, 
            double? targetSentiment = null,
            double sentimentTolerance = 0.03)
            : base(r => 
                // Filter by dish name if provided
                (string.IsNullOrWhiteSpace(dishName) || 
                 (r.Dish != null && r.Dish.Name != null && r.Dish.Name.ToLower() == dishName.ToLower())) &&
                // Filter by sentiment if provided
                (!targetSentiment.HasValue || 
                 Math.Abs(r.SentimentScore - targetSentiment.Value) <= sentimentTolerance))
        {
            AddInclude(r => r.Customer);
            AddInclude(r => r.Dish);
            ApplyOrderByDescending(r => r.SentimentScore);
        }
    }
}
