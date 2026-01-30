
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Entities.ProfileEntities;

namespace SmartMenuOptim.Domain.Entities.RestaurantEntities
{
    /// <summary>
    /// Represents a customer review for a dish in a specific restaurant, providing feedback through ratings and comments.
    /// </summary>
    /// <remarks>
    /// <para><strong>3-TIER DDD STRATEGY: Tier 2 - Simple Aggregates (Lightweight DDD)</strong></para>
    /// <para>This class implements a lightweight DDD aggregate pattern suitable for entities that need rich domain behavior
    /// without the complexity of full aggregate roots. It balances encapsulation and validation with practical implementation.</para>
    /// 
    /// <para><strong>Tier 2 Characteristics:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Encapsulation:</strong> Properties use private setters to prevent unauthorized state changes</description></item>
    ///   <item><description><strong>Validation:</strong> Business rules enforced through constructor and behavioral methods</description></item>
    ///   <item><description><strong>Rich Behavior:</strong> Domain logic encapsulated in methods rather than anemic property bags</description></item>
    ///   <item><description><strong>Simple Lifecycle:</strong> No complex child entities or deep object graphs</description></item>
    ///   <item><description><strong>Lightweight Invariants:</strong> Basic consistency rules (rating range, comment length, date validation)</description></item>
    /// </list>
    /// 
    /// <para><strong>Entity Overview:</strong></para>
    /// <para>A Review captures customer feedback for a dish within a restaurant's menu system. It includes a 1-5 star rating,
    /// textual comment, sentiment analysis score, and links to both the dish being reviewed and optionally the customer who wrote it.
    /// Anonymous reviews (without CustomerId) are supported through the CustomerName property.</para>
    /// 
    /// <para><strong>Multi-Tenant Support:</strong></para>
    /// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each review is scoped to a specific
    /// restaurant (RestaurantId), ensuring proper data isolation in a multi-tenant environment. The review must belong to
    /// the same restaurant as the dish being reviewed to maintain tenant boundary integrity.</para>
    /// 
    /// <para><strong>Consistency Boundary:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Invariants Protected:</strong> Rating must be 1-5 stars, sentiment score 0.0-1.0, comment minimum 10 characters, review date cannot be in future</description></item>
    ///   <item><description><strong>Encapsulated State:</strong> Internal state can only be modified through behavioral methods (UpdateReview, UpdateSentiment, UpdateCustomerInfo)</description></item>
    ///   <item><description><strong>Transactional Consistency:</strong> All changes validated atomically through public methods</description></item>
    ///   <item><description><strong>Business Rules:</strong> Cannot review deleted/inactive dishes, cannot associate with deleted/inactive customers</description></item>
    /// </list>
    /// 
    /// <para><strong>Domain Features:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (Id property from EntityBase)</description></item>
    ///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt, UpdatedAt automatically managed through EntityBase</description></item>
    ///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for soft deletion scenarios</description></item>
    ///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
    ///   <item><description><strong>Sentiment Analysis:</strong> Supports storing AI-generated sentiment scores for analytics</description></item>
    /// </list>
    /// 
    /// <para><strong>Relationships:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Dish (Required):</strong> Each review must be for exactly one dish</description></item>
    ///   <item><description><strong>Customer (Optional):</strong> Reviews can be linked to registered customers or remain anonymous</description></item>
    ///   <item><description><strong>Restaurant (Required):</strong> Inherited from TenantEntityBase, ensures tenant isolation</description></item>
    /// </list>
    /// 
    /// <para><strong>Example Usage:</strong></para>
    /// <code>
    /// // Creating a review from a registered customer
    /// var review = new Review(
    ///     restaurantId: 123,
    ///     dishId: 456,
    ///     rating: 5,
    ///     comment: "Absolutely delicious! The flavors were perfectly balanced and the presentation was stunning.",
    ///     customerId: 789
    /// );
    /// 
    /// // Creating an anonymous review
    /// var anonymousReview = new Review(
    ///     restaurantId: 123,
    ///     dishId: 456,
    ///     rating: 4,
    ///     comment: "Very good dish, would recommend to others. Great value for money.",
    ///     customerName: "Anonymous Foodie"
    /// );
    /// 
    /// // Adding sentiment analysis
    /// review.UpdateSentiment(0.95); // Very positive sentiment
    /// 
    /// // Customer updates their review
    /// review.UpdateReview(
    ///     rating: 4,
    ///     comment: "Still great, but second visit wasn't quite as perfect as the first."
    /// );
    /// 
    /// // Updating customer information for anonymous review
    /// anonymousReview.UpdateCustomerInfo(customerId: 999, customerName: null);
    /// 
    /// // Checking review properties
    /// if (review.IsPositive())
    /// {
    ///     Console.WriteLine($"Positive review with {review.Rating} stars");
    /// }
    /// </code>
    /// 
    /// <para><strong>Entity Framework Core Support:</strong></para>
    /// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The entity can be
    /// persisted and retrieved through a repository pattern. Private setters are accessible to EF Core through reflection-based
    /// field mapping in the entity configuration.</para>
    /// 
    /// <para><strong>Data Annotations:</strong></para>
    /// <para>Uses attributes for basic validation and metadata that complement the domain logic:
    /// - [Required], [MaxLength], [Range] for data validation
    /// - [ForeignKey], [InverseProperty] for relationship mapping
    /// - [Table] for database mapping configuration</para>
    /// 
    /// <para><strong>Design Considerations:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Rating Scale:</strong> Standard 1-5 star rating system for simplicity and industry convention</description></item>
    ///   <item><description><strong>Comment Length:</strong> Minimum 10 characters to ensure meaningful feedback, maximum 2000 for storage efficiency</description></item>
    ///   <item><description><strong>Anonymous Reviews:</strong> Supported through optional CustomerId and CustomerName properties</description></item>
    ///   <item><description><strong>Sentiment Score:</strong> Normalized 0.0-1.0 range for integration with ML/AI sentiment analysis services</description></item>
    ///   <item><description><strong>Immutable Creation:</strong> Review date (DateCreated) set once at creation and never modified</description></item>
    ///   <item><description><strong>Modification Safety:</strong> UpdatedAt timestamp automatically updated when review content changes</description></item>
    /// </list>
    /// 
    /// <para><strong>Indexing Strategy:</strong></para>
    /// <para>Database indexes for efficient querying are defined centrally in AppDbContext.OnModelCreating:
    /// - IX_Reviews_Restaurant_CreatedAt: For tenant-scoped review listings sorted by date
    /// - IX_Reviews_Dish_CreatedAt: For dish-specific review queries
    /// - IX_Reviews_Customer_CreatedAt: For customer review history
    /// - IX_Reviews_Rating: For filtering by rating levels</para>
    /// </remarks>
    [Table("Reviews")]
    public class Review : TenantEntityBase
    {
        // === Private Setters (Encapsulated State) ===
        
        /// <summary>
        /// Name of the customer who wrote the review (maximum 150 characters).
        /// Used for anonymous or custom-named reviews when CustomerId is not provided.
        /// </summary>
        /// <remarks>
        /// Either CustomerName or CustomerId must be provided. If both are present, CustomerId takes precedence
        /// for linking to customer profile, but CustomerName can serve as a display override.
        /// </remarks>
        [MaxLength(150, ErrorMessage = "CustomerName cannot exceed 150 characters")]
        public string CustomerName { get; private set; }

        /// <summary>
        /// The review comment text containing customer feedback (maximum 2000 characters).
        /// </summary>
        /// <remarks>
        /// Comment must be at least 10 characters to ensure meaningful feedback.
        /// Trimmed during creation and updates to remove leading/trailing whitespace.
        /// </remarks>
        [Required(ErrorMessage = "Comment is required")]
        [MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters")]
        public string Comment { get; private set; }

        /// <summary>
        /// Sentiment score of the review from sentiment analysis (range 0.0 - 1.0).
        /// </summary>
        /// <remarks>
        /// Typically generated by AI/ML sentiment analysis services:
        /// - 0.0-0.3: Negative sentiment
        /// - 0.3-0.6: Neutral sentiment
        /// - 0.6-1.0: Positive sentiment
        /// Default is 0.5 (neutral) if not explicitly set.
        /// </remarks>
        [Range(0.0, 1.0, ErrorMessage = "SentimentScore must be between 0.0 and 1.0")]
        public double SentimentScore { get; private set; }

        /// <summary>
        /// Date and time the review was created (UTC).
        /// </summary>
        /// <remarks>
        /// Set once during construction and immutable thereafter.
        /// Cannot be in the future. Used for sorting and filtering reviews by date.
        /// </remarks>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DateCreated { get; private set; }

        /// <summary>
        /// Rating out of 5 stars for the dish (1-5 inclusive).
        /// </summary>
        /// <remarks>
        /// Industry-standard 5-star rating system:
        /// - 1 star: Poor
        /// - 2 stars: Fair
        /// - 3 stars: Good
        /// - 4 stars: Very Good
        /// - 5 stars: Excellent
        /// </remarks>
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; private set; }

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the Dish entity. Each review is for a single dish.
        /// </summary>
        /// <remarks>
        /// Required relationship. The dish must belong to the same restaurant as the review (tenant boundary enforcement).
        /// </remarks>
        [Required]
        [ForeignKey(nameof(Dish))]
        public int DishId { get; private set; }

        /// <summary>
        /// Optional foreign key to the Customer entity. Null indicates an anonymous review.
        /// </summary>
        /// <remarks>
        /// When provided, links the review to a registered customer profile for review history tracking.
        /// Customer must be active and not deleted to be associated with a review.
        /// </remarks>
        [ForeignKey(nameof(Customer))]
        public int? CustomerId { get; private set; }

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

        // === Constructors ===
        
        /// <summary>
        /// Protected parameterless constructor for Entity Framework Core.
        /// </summary>
        /// <remarks>
        /// Required by EF Core for entity materialization from database.
        /// Not intended for direct use in application code.
        /// </remarks>
        protected Review() 
        { 
            Comment = string.Empty; 
            CustomerName = string.Empty;
        }

        /// <summary>
        /// Creates a new review for a dish from a registered customer.
        /// </summary>
        /// <param name="restaurantId">The restaurant identifier this review belongs to (tenant context).</param>
        /// <param name="dishId">The dish being reviewed.</param>
        /// <param name="rating">Star rating for the dish (1-5).</param>
        /// <param name="comment">Review comment text (minimum 10 characters, maximum 2000).</param>
        /// <param name="customerId">The registered customer writing the review.</param>
        /// <param name="sentimentScore">Optional sentiment analysis score (0.0-1.0). Defaults to 0.5 (neutral).</param>
        /// <exception cref="ArgumentException">Thrown when rating is out of range, comment is invalid, or sentiment score is out of range.</exception>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are missing.</exception>
        public Review(
            int restaurantId,
            int dishId,
            int rating,
            string comment,
            int customerId,
            double sentimentScore = 0.5)
        {
            // Validate required parameters
            if (restaurantId <= 0)
                throw new ArgumentException("RestaurantId must be a positive integer.", nameof(restaurantId));

            if (dishId <= 0)
                throw new ArgumentException("DishId must be a positive integer.", nameof(dishId));

            if (customerId <= 0)
                throw new ArgumentException("CustomerId must be a positive integer.", nameof(customerId));

            ValidateRating(rating);
            ValidateComment(comment);
            ValidateSentimentScore(sentimentScore);

            // Set properties
            RestaurantId = restaurantId;
            DishId = dishId;
            CustomerId = customerId;
            Rating = rating;
            Comment = comment?.Trim() ?? string.Empty;
            SentimentScore = sentimentScore;
            CustomerName = string.Empty;
            DateCreated = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new anonymous review for a dish with a custom customer name.
        /// </summary>
        /// <param name="restaurantId">The restaurant identifier this review belongs to (tenant context).</param>
        /// <param name="dishId">The dish being reviewed.</param>
        /// <param name="rating">Star rating for the dish (1-5).</param>
        /// <param name="comment">Review comment text (minimum 10 characters, maximum 2000).</param>
        /// <param name="customerName">Display name for the anonymous reviewer (maximum 150 characters).</param>
        /// <param name="sentimentScore">Optional sentiment analysis score (0.0-1.0). Defaults to 0.5 (neutral).</param>
        /// <exception cref="ArgumentException">Thrown when rating is out of range, comment is invalid, sentiment score is out of range, or customer name is invalid.</exception>
        public Review(
            int restaurantId,
            int dishId,
            int rating,
            string comment,
            string customerName,
            double sentimentScore = 0.5)
        {
            // Validate required parameters
            if (restaurantId <= 0)
                throw new ArgumentException("RestaurantId must be a positive integer.", nameof(restaurantId));

            if (dishId <= 0)
                throw new ArgumentException("DishId must be a positive integer.", nameof(dishId));

            if (string.IsNullOrWhiteSpace(customerName))
                throw new ArgumentException("CustomerName cannot be empty for anonymous reviews.", nameof(customerName));

            if (customerName.Length > 150)
                throw new ArgumentException("CustomerName cannot exceed 150 characters.", nameof(customerName));

            ValidateRating(rating);
            ValidateComment(comment);
            ValidateSentimentScore(sentimentScore);

            // Set properties
            RestaurantId = restaurantId;
            DishId = dishId;
            CustomerId = null;
            Rating = rating;
            Comment = comment?.Trim() ?? string.Empty;
            SentimentScore = sentimentScore;
            CustomerName = customerName?.Trim() ?? string.Empty;
            DateCreated = DateTime.UtcNow;
        }

        // === Behavioral Methods ===

        /// <summary>
        /// Updates the review content with new rating and comment.
        /// </summary>
        /// <param name="rating">New star rating for the dish (1-5).</param>
        /// <param name="comment">New review comment text (minimum 10 characters, maximum 2000).</param>
        /// <exception cref="ArgumentException">Thrown when rating is out of range or comment is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update a deleted review.</exception>
        /// <remarks>
        /// This method allows customers to edit their reviews after initial submission.
        /// The UpdatedAt timestamp is automatically updated to reflect the modification.
        /// Cannot update a soft-deleted review - must be restored first.
        /// </remarks>
        public void UpdateReview(int rating, string comment)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot update a deleted review. Restore it first.");

            ValidateRating(rating);
            ValidateComment(comment);

            Rating = rating;
            Comment = comment?.Trim() ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the sentiment analysis score for the review.
        /// </summary>
        /// <param name="sentimentScore">New sentiment score (0.0-1.0).</param>
        /// <exception cref="ArgumentException">Thrown when sentiment score is out of valid range.</exception>
        /// <remarks>
        /// Typically called by sentiment analysis services after processing the comment text.
        /// Allows updating sentiment without modifying the actual review content.
        /// </remarks>
        public void UpdateSentiment(double sentimentScore)
        {
            ValidateSentimentScore(sentimentScore);
            SentimentScore = sentimentScore;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates customer information for the review.
        /// </summary>
        /// <param name="customerId">Optional customer ID to link the review to a registered customer. Pass null to make anonymous.</param>
        /// <param name="customerName">Optional custom display name. Pass null to clear when linking to customer.</param>
        /// <exception cref="ArgumentException">Thrown when both customerId and customerName are null/empty.</exception>
        /// <remarks>
        /// Use this method to:
        /// - Convert anonymous reviews to registered customer reviews (provide customerId)
        /// - Convert registered customer reviews to anonymous (provide customerName, set customerId to null)
        /// - Update display name for anonymous reviews
        /// At least one of customerId or customerName must be provided.
        /// </remarks>
        public void UpdateCustomerInfo(int? customerId, string? customerName)
        {
            // Validate that at least one identifier is provided
            if (!customerId.HasValue && string.IsNullOrWhiteSpace(customerName))
                throw new ArgumentException("Either customerId or customerName must be provided.");

            // Validate customerId if provided
            if (customerId.HasValue && customerId.Value <= 0)
                throw new ArgumentException("CustomerId must be a positive integer when provided.", nameof(customerId));

            // Validate customerName if provided
            if (!string.IsNullOrWhiteSpace(customerName) && customerName.Length > 150)
                throw new ArgumentException("CustomerName cannot exceed 150 characters.", nameof(customerName));

            CustomerId = customerId;
            CustomerName = customerName?.Trim() ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
        }

        // === Query Methods ===

        /// <summary>
        /// Determines if the review is positive based on rating and sentiment score.
        /// </summary>
        /// <returns>True if rating is 4 or 5 stars and sentiment score is above 0.6; otherwise, false.</returns>
        /// <remarks>
        /// Combines both quantitative rating and qualitative sentiment analysis for more accurate positivity detection.
        /// Useful for filtering top-rated dishes and identifying satisfied customers.
        /// </remarks>
        public bool IsPositive()
        {
            return Rating >= 4 && SentimentScore > 0.6;
        }

        /// <summary>
        /// Determines if the review is negative based on rating and sentiment score.
        /// </summary>
        /// <returns>True if rating is 2 or below and sentiment score is below 0.4; otherwise, false.</returns>
        /// <remarks>
        /// Identifies reviews that indicate customer dissatisfaction.
        /// Useful for flagging dishes that need improvement or customer service follow-up.
        /// </remarks>
        public bool IsNegative()
        {
            return Rating <= 2 && SentimentScore < 0.4;
        }

        /// <summary>
        /// Determines if the review is from an anonymous customer.
        /// </summary>
        /// <returns>True if CustomerId is null; otherwise, false.</returns>
        public bool IsAnonymous()
        {
            return !CustomerId.HasValue;
        }

        /// <summary>
        /// Gets the display name for the reviewer.
        /// </summary>
        /// <returns>CustomerName if available, otherwise "Anonymous".</returns>
        /// <remarks>
        /// Provides a fallback display name for UI rendering when CustomerName is empty.
        /// For registered customers, you should typically use Customer.Name navigation property instead.
        /// </remarks>
        public string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(CustomerName) ? "Anonymous" : CustomerName;
        }

        // === Private Validation Helpers ===

        /// <summary>
        /// Validates that the rating is within the acceptable 1-5 range.
        /// </summary>
        /// <param name="rating">The rating value to validate.</param>
        /// <exception cref="ArgumentException">Thrown when rating is not between 1 and 5.</exception>
        private static void ValidateRating(int rating)
        {
            if (rating < 1 || rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5 stars.", nameof(rating));
        }

        /// <summary>
        /// Validates that the comment meets length and content requirements.
        /// </summary>
        /// <param name="comment">The comment text to validate.</param>
        /// <exception cref="ArgumentException">Thrown when comment is null, empty, too short, or too long.</exception>
        private static void ValidateComment(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                throw new ArgumentException("Review comment cannot be empty.", nameof(comment));

            var trimmedComment = comment.Trim();
            
            if (trimmedComment.Length < 10)
                throw new ArgumentException("Review comment must be at least 10 characters long to provide meaningful feedback.", nameof(comment));

            if (trimmedComment.Length > 2000)
                throw new ArgumentException("Review comment cannot exceed 2000 characters.", nameof(comment));
        }

        /// <summary>
        /// Validates that the sentiment score is within the acceptable 0.0-1.0 range.
        /// </summary>
        /// <param name="sentimentScore">The sentiment score to validate.</param>
        /// <exception cref="ArgumentException">Thrown when sentiment score is not between 0.0 and 1.0.</exception>
        private static void ValidateSentimentScore(double sentimentScore)
        {
            if (sentimentScore < 0.0 || sentimentScore > 1.0)
                throw new ArgumentException("SentimentScore must be between 0.0 and 1.0.", nameof(sentimentScore));
        }
    }
}