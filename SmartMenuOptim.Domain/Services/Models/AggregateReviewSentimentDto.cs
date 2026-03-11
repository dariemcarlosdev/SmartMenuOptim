using SmartMenuOptim.Domain.Enums;

namespace SmartMenuOptim.Domain.Services.Models;

/// <summary>
/// Domain service output model for aggregate sentiment analysis across multiple reviews. It live in Domain.Services.Models because it is a specific output model for a domain service operation, not a generic result pattern. It is used to represent the overall sentiment of a collection of reviews, including metrics like average sentiment score and counts of positive/negative reviews.
/// </summary>
/// <remarks>
/// This is a specific DTO for sentiment analysis operations, not to be confused
/// with generic result patterns like <see cref="Common.DomainResult{T}"/>.
/// </remarks>
public class AggregateReviewSentimentDto
{
    public int TotalReviews { get; set; }
    public int ReviewsAnalyzed { get; set; }
    public double? AverageSentiment { get; set; }
    public SentimentCategory OverallCategory { get; set; }
    public int PositiveReviewsCount { get; set; }
    public int NegativeReviewsCount { get; set; }
}
