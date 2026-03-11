using SmartMenuOptim.Domain.Enums;

namespace SmartMenuOptim.Domain.Services.Models;

/// <summary>
/// Domain service output model for sentiment analysis of a single review. It lives in Domain.Services.Models because it is a specific output model for a domain service operation, not a generic result pattern. It is used to represent the sentiment analysis result of an individual review, including the sentiment score, category, and any relevant messages about the analysis process.
/// </summary>
/// <remarks>
/// This is a specific DTO for sentiment analysis operations, not to be confused
/// with generic result patterns like <see cref="Common.DomainResult{T}"/>.
/// </remarks>
public class ReviewSentimentDto
{
    public int ReviewId { get; set; }
    public double? SentimentScore { get; set; }
    public SentimentCategory SentimentCategory { get; set; }
    public string Message { get; set; } = string.Empty;
}
