// SmartMenuOptim.Domain/Services/ReviewSentimentResult.cs
namespace SmartMenuOptim.Domain.DTOs;

// Domain Value Objects/DTOs
/// These could be placed in a separate file if preferred under folder Domain/DTOs
public class ReviewSentimentResult
{
    public int ReviewId { get; set; }
    public double? SentimentScore { get; set; }
    public SentimentCategory SentimentCategory { get; set; }
    public string Message { get; set; } = string.Empty;
}
