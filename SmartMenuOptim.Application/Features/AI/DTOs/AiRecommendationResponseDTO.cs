namespace SmartMenuOptim.Application.Features.AI.DTOs;

/// <summary>
/// DTO for AI recommendation response data.
/// </summary>
/// <remarks>
/// Contains AI-generated menu optimization recommendations based on sales and review data.
/// OpenAI GPT or similar models analyze patterns in customer reviews and sales data
/// to provide data-driven recommendations for menu improvements.
/// </remarks>
public class AiRecommendationResponseDTO
{
    /// <summary>
    /// The recommended dish name (with corrected spelling).
    /// </summary>
    public string RecommendedDish { get; set; } = string.Empty;

    /// <summary>
    /// Strategy note provides additional context or explanation for the recommendations.
    /// Can guide restaurants on implementing recommendations effectively.
    /// </summary>
    public string StrategyNote { get; set; } = string.Empty;

    public int QuantitySold { get; set; }
    public double AverageRating { get; set; }
    public double AverageSentimentScore { get; set; }
}
