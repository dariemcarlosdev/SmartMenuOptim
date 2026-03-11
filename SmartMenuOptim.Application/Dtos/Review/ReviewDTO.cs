using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Dtos.Review;

/// <summary>
/// Data Transfer Object for Review entity.
/// </summary>
/// <remarks>
/// <para><strong>Multi-Tenant Considerations:</strong></para>
/// <list type="bullet">
///   <item><description>Maintains RestaurantId for tenant isolation</description></item>
///   <item><description>Each review belongs to a specific restaurant (tenant)</description></item>
///   <item><description>Contains only necessary data for client operations</description></item>
/// </list>
/// </remarks>
public class ReviewDTO
{
    /// <summary>Review identifier.</summary>
    public int Id { get; set; }

    /// <summary>Name of the customer who wrote the review.</summary>
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The review comment text.</summary>
    [Required(ErrorMessage = "A comment is required.")]
    [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
    public string Comment { get; set; } = string.Empty;

    /// <summary>Sentiment score of the review from analysis (0.0-1.0).</summary>
    public double SentimentScore { get; set; }

    /// <summary>Date the review was created (UTC).</summary>
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>Rating out of 5 stars (1-5).</summary>
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    /// <summary>Dish identifier being reviewed.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "A dish must be selected.")]
    public int DishId { get; set; }

    /// <summary>Restaurant identifier where the review was made.</summary>
    public int RestaurantId { get; set; }

    /// <summary>Name of the dish being reviewed (for UI display).</summary>
    [Required(ErrorMessage = "Dish is required.")]
    public string? DishName { get; set; }

    /// <summary>Optional customer identifier for registered users.</summary>
    public int? CustomerId { get; set; }
}
