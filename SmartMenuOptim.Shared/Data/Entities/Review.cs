using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents a customer review for a dish.
    /// Each Review is associated with a single Dish (DishId is the foreign key).
    /// The relationship is many-to-one, meaning a Review belongs to one Dish, but a Dish can have many Reviews. When many-to-one, the foreign key is on the "many" side.
    /// The many-to-one relationship means that a Dish can have many reviews, but each Review is associated with one Dish.
    /// Navigation properties:
    /// - Dish: the dish this review is for.
    /// - Customer: the customer who wrote the review (optional, can be null for anonymous reviews).
    ///
    /// Design notes for CustomerName:
    /// - If all reviews should be linked to a Customer: remove CustomerName.
    /// - If you want to support anonymous reviews or custom names: keep CustomerName as optional.
    ///
    /// Use cases:
    /// 1. Linked Review: Review is associated with a Customer via CustomerId, CustomerName is not needed.
    /// 2. Anonymous Review: CustomerId is null, CustomerName can be used to display a nickname or left empty.
    /// 3. Custom Name: Even if linked to a Customer, CustomerName can be used to display a custom name for the review.
    /// </summary>
    public class Review
    {
        /// <summary>
        /// Primary key for the Review entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the customer who wrote the review. Optional, used for anonymous or custom-named reviews.
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// The review comment.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Sentiment score of the review (e.g., from sentiment analysis).
        /// </summary>
        public double SentimentScore { get; set; }

        /// <summary>
        /// Foreign key to the Dish entity.
        /// </summary>
        public int DishId { get; set; }

        /// <summary>
        /// Navigation property to the Dish this review is for.
        /// </summary>
        public Dish? Dish { get; set; }

        /// <summary>
        /// Optional foreign key to the Customer entity. Null means anonymous review.
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Navigation property to the Customer who wrote the review (optional).
        /// </summary>
        public Customer? Customer { get; set; }

        /// <summary>
        /// Date the review was created.
        /// </summary>
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Rating out of 5 stars for the dish (optional, 1-5).
        /// </summary>
        public int Rating { get; set; }
    }
}
