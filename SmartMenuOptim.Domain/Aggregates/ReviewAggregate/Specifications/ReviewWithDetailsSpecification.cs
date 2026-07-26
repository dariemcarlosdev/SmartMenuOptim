using SmartMenuOptim.Domain.Specifications;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Aggregates.ReviewAggregate;

namespace SmartMenuOptim.Domain.Aggregates.ReviewAggregate.Specifications
{
    /// <summary>
    /// Specification for retrieving reviews with their related Customer and Dish information.
    /// </summary>
    public class ReviewWithDetailsSpecification : BaseSpecification<Review>
    {
        /// <summary>
        /// Initializes a specification to get all reviews with customer and dish details.
        /// </summary>
        public ReviewWithDetailsSpecification()
        {
            AddInclude(r => r.Customer);
            AddInclude(r => r.Dish);
        }

        /// <summary>
        /// Initializes a specification to get a specific review by ID with all related data.
        /// </summary>
        /// <param name="reviewId">The unique identifier of the review.</param>
        public ReviewWithDetailsSpecification(int reviewId) 
            : base(r => r.Id == reviewId)
        {
            AddInclude(r => r.Customer);
            AddInclude(r => r.Dish);
        }

        /// <summary>
        /// Initializes a specification to get reviews filtered by dish name.
        /// </summary>
        /// <param name="dishName">The name of the dish to filter by.</param>
        /// <param name="caseSensitive">Whether the search should be case sensitive.</param>
        public ReviewWithDetailsSpecification(string dishName, bool caseSensitive = false)
            : base(r => r.Dish != null && r.Dish.Name != null && 
                       (caseSensitive ? r.Dish.Name.Value == dishName : r.Dish.Name.NormalizedValue == dishName.ToLowerInvariant()))
        {
            AddInclude(r => r.Customer);
            AddInclude(r => r.Dish);
            ApplyOrderByDescending(r => r.SentimentScore);
        }

        /// <summary>
        /// Initializes a specification to get reviews filtered by sentiment score with tolerance.
        /// </summary>
        /// <param name="targetSentiment">The target sentiment score.</param>
        /// <param name="tolerance">The acceptable tolerance range (default: 0.03).</param>
        public ReviewWithDetailsSpecification(double targetSentiment, double tolerance = 0.03)
            : base(r => Math.Abs(r.SentimentScore - targetSentiment) <= tolerance)
        {
            AddInclude(r => r.Customer);
            AddInclude(r => r.Dish);
            ApplyOrderByDescending(r => r.SentimentScore);
        }
    }
}
