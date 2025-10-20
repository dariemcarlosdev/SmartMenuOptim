using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents a customer review for a dish in a specific restaurant.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is tenant-specific. Each Review is linked to a Restaurant, enabling the application to support multiple restaurants (tenants), each with their own unique set of reviews. This structure is a foundation for a multi-tenant architecture.
    /// </remarks>
    public class Review
    {
        // === Standalone Properties ===

        /// <summary>
        /// Primary key for the Review entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the customer who wrote the review. Optional, used for anonymous or custom-named reviews.
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// The review comment text.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Sentiment score of the review (e.g., from sentiment analysis).
        /// </summary>
        public double SentimentScore { get; set; }

        /// <summary>
        /// Date the review was created (UTC).
        /// </summary>
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Rating out of 5 stars for the dish (1-5).
        /// </summary>
        public int Rating { get; set; }

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the Dish entity. Each review is for a single dish.
        /// </summary>
        public int DishId { get; set; }

        /// <summary>
        /// Foreign key to the Restaurant entity. Each review is for a single restaurant.
        /// </summary>
        public int RestaurantId { get; set; }

        /// <summary>
        /// Optional foreign key to the Customer entity. Null means anonymous review.
        /// </summary>
        public int? CustomerId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the Dish this review is for.
        /// </summary>
        public Dish? Dish { get; set; }

        /// <summary>
        /// Navigation property to the Restaurant this review is associated with.
        /// </summary>
        public Restaurant? Restaurant { get; set; }

        /// <summary>
        /// Navigation property to the Customer who wrote the review (optional).
        /// </summary>
        public Customer? Customer { get; set; }
    }
}
