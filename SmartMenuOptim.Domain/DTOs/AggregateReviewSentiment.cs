// SmartMenuOptim.Domain/Services/AggregateReviewSentiment.cs
namespace SmartMenuOptim.Domain.DTOs;

public class AggregateReviewSentiment
{
    public int TotalReviews { get; set; }
    public int ReviewsAnalyzed { get; set; }
    public double? AverageSentiment { get; set; }
    public SentimentCategory OverallCategory { get; set; }
    public int PositiveReviewsCount { get; set; }
    public int NegativeReviewsCount { get; set; }
}
