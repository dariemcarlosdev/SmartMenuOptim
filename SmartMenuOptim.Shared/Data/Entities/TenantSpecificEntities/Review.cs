using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a customer review for a dish in a specific restaurant.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is tenant-specific. Each Review is linked to a Restaurant, enabling the application to support multiple restaurants (tenants), each with their own unique set of reviews. Indexes for review queries are defined centrally in `AppDbContext.OnModelCreating` (e.g., IX_Reviews_Restaurant_CreatedAt, IX_Reviews_Dish_CreatedAt).
    /// </remarks>
    [Table("Reviews")]
    public class Review : TenantEntityBase, IValidatableObject
    {
        // === Standalone Properties ===

        /// <summary>
        /// Name of the customer who wrote the review. Optional, used for anonymous or custom-named reviews.
        /// </summary>
        [MaxLength(150, ErrorMessage = "CustomerName cannot exceed 150 characters")]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// The review comment text.
        /// </summary>
        [MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters")]
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Sentiment score of the review (e.g., from sentiment analysis). Range 0.0 - 1.0
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "SentimentScore must be between 0.0 and 1.0")]
        public double SentimentScore { get; set; }

        /// <summary>
        /// Date the review was created (UTC).
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Rating out of 5 stars for the dish (1-5).
        /// </summary>
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the Dish entity. Each review is for a single dish.
        /// </summary>
        [Required]
        [ForeignKey(nameof(Dish))]
        public int DishId { get; set; }

        /// <summary>
        /// Optional foreign key to the Customer entity. Null means anonymous review.
        /// </summary>
        [ForeignKey(nameof(Customer))]
        public int? CustomerId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the Dish this review is for.
        /// </summary>
        [InverseProperty(nameof(Dish.Reviews))]
        public Dish? Dish { get; set; }

        /// <summary>
        /// Navigation property to the Customer who wrote the review (optional).
        /// </summary>
        [InverseProperty(nameof(Customer.Reviews))]
        public Customer? Customer { get; set; }

        // === Validation ===
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DishId <= 0)
                yield return new ValidationResult("DishId must be a positive integer.", new[] { nameof(DishId) });

            if (Rating < 1 || Rating > 5)
                yield return new ValidationResult("Rating must be between 1 and 5.", new[] { nameof(Rating) });

            if (SentimentScore < 0.0 || SentimentScore > 1.0)
                yield return new ValidationResult("SentimentScore must be between 0.0 and 1.0.", new[] { nameof(SentimentScore) });

            if (!string.IsNullOrEmpty(CustomerName) && CustomerName.Length > 150)
                yield return new ValidationResult("CustomerName cannot exceed 150 characters.", new[] { nameof(CustomerName) });

            if (!string.IsNullOrEmpty(Comment) && Comment.Length > 2000)
                yield return new ValidationResult("Comment cannot exceed 2000 characters.", new[] { nameof(Comment) });

            yield break;
        }
    }
}
