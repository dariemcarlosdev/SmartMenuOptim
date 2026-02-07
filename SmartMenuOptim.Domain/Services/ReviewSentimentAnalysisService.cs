// SmartMenuOptim.Domain/Services/ReviewSentimentAnalysisService.cs
using SmartMenuOptim.Domain.DTOs;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Services.Abstraction;

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Domain service for analyzing customer review sentiment, consistent with Hexagonal Architecture principles which separate core domain logic from external services.
/// Provides services for analyzing the sentiment of customer reviews, including single and aggregate sentiment
/// analysis, as well as identification of anomalous reviews based on sentiment and rating.
/// </summary>
/// <remarks>This service uses an injected sentiment analyzer to evaluate review comments and applies business
/// rules to categorize sentiment as positive, neutral, or negative. It can process individual reviews or collections of
/// reviews, and can flag reviews where the sentiment and rating are inconsistent (for example, a high rating with
/// negative sentiment). The service is intended for use in scenarios where understanding customer feedback trends and
/// identifying outlier reviews is important.</remarks>
public class ReviewSentimentAnalysisService
{
    // Injected a sentiment analyzer service. Any implementation of ISentimentAnalyzer can be used here, like SentimentService.
    private readonly ISentimentAnalyzer _sentimentAnalyzer;

    // Constants for business rules
    private const double PositiveSentimentThreshold = 0.7;
    private const double NegativeSentimentThreshold = 0.3;

    public ReviewSentimentAnalysisService(ISentimentAnalyzer sentimentAnalyzer)
    {
        _sentimentAnalyzer = sentimentAnalyzer ?? throw new ArgumentNullException(nameof(sentimentAnalyzer));
    }

    /// <summary>
    /// Analyzes the sentiment of the specified review comment asynchronously and returns the sentiment result.
    /// </summary>
    /// <param name="review">The review to analyze. The review's comment is used to determine sentiment. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see
    /// cref="ReviewSentimentResult"/> with the sentiment analysis outcome. If the review comment is empty or
    /// whitespace, the result will indicate an unknown sentiment.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="review"/> is null.</exception>
    public async Task<ReviewSentimentResult> AnalyzeReviewSentimentAsync(Review review)
    {
        if (review == null)
            throw new ArgumentNullException(nameof(review));

        if (string.IsNullOrWhiteSpace(review.Comment))
        {
            return new ReviewSentimentResult
            {
                ReviewId = review.Id,
                SentimentScore = null,
                SentimentCategory = SentimentCategory.Unknown,
                Message = "Review has no comment to analyze"
            };
        }

        var score = await _sentimentAnalyzer.AnalyzePositiveSentimentAsync(new[] { review.Comment });

        return new ReviewSentimentResult
        {
            ReviewId = review.Id,
            SentimentScore = score,
            SentimentCategory = CategorizeSentiment(score),
            Message = GetSentimentMessage(score)
        };
    }

    /// <summary>
    /// Analyzes the sentiment of multiple reviews and returns aggregate sentiment results.
    /// </summary>
    /// <param name="reviews">A collection of reviews to analyze. The collection cannot be null or empty. Only reviews with non-empty comments
    /// are included in the sentiment analysis.</param>
    /// <returns>An AggregateReviewSentiment object containing the total number of reviews, the number of reviews analyzed, the
    /// average sentiment score, the overall sentiment category, and counts of positive and negative reviews. If no
    /// reviews with comments are provided, the average sentiment is null and the overall category is set to Unknown.</returns>
    /// <exception cref="ArgumentException">Thrown if the reviews collection is null or empty.</exception>
    public async Task<AggregateReviewSentiment> AnalyzeMultipleReviewsAsync(
        IEnumerable<Review> reviews)
    {
        if (reviews == null || !reviews.Any())
            throw new ArgumentException("Reviews collection cannot be null or empty", nameof(reviews));

        var reviewList = reviews.ToList();
        var commentsToAnalyze = reviewList
            .Where(r => !string.IsNullOrWhiteSpace(r.Comment))
            .Select(r => r.Comment)
            .ToList();

        if (!commentsToAnalyze.Any())
        {
            return new AggregateReviewSentiment
            {
                TotalReviews = reviewList.Count,
                ReviewsAnalyzed = 0,
                AverageSentiment = null,
                OverallCategory = SentimentCategory.Unknown
            };
        }

        // Analyze average sentiment across all comments. Bulk analysis for efficiency.
        var averageSentiment = await _sentimentAnalyzer.AnalyzeAverageSentimentAsync(
            string.Join(" ", commentsToAnalyze)
        );

        return new AggregateReviewSentiment
        {
            TotalReviews = reviewList.Count,
            ReviewsAnalyzed = commentsToAnalyze.Count,
            AverageSentiment = averageSentiment,
            OverallCategory = CategorizeSentiment(averageSentiment),
            PositiveReviewsCount = reviewList.Count(r => r.Rating >= 4),
            NegativeReviewsCount = reviewList.Count(r => r.Rating <= 2)
        };
    }

    /// <summary>
    /// Identifies reviews requiring immediate attention based on sentiment.
    /// Business rule: Negative sentiment with high rating or vice versa.
    /// </summary>
    public async Task<IEnumerable<Review>> IdentifyAnomalousReviewsAsync(
        IEnumerable<Review> reviews)
    {
        var anomalousReviews = new List<Review>();

        foreach (var review in reviews)
        {
            if (string.IsNullOrWhiteSpace(review.Comment))
                continue;

            var result = await AnalyzeReviewSentimentAsync(review);

            // Business rule: High rating but negative sentiment
            if (review.Rating >= 4 && result.SentimentCategory == SentimentCategory.Negative)
            {
                anomalousReviews.Add(review);
            }
            // Business rule: Low rating but positive sentiment
            else if (review.Rating <= 2 && result.SentimentCategory == SentimentCategory.Positive)
            {
                anomalousReviews.Add(review);
            }
        }

        return anomalousReviews;
    }

    #region Private Business Logic Methods

    /// <summary>
    /// Determines the sentiment category based on the specified sentiment score.
    /// </summary>
    /// <param name="score">The sentiment score to evaluate. A higher value typically indicates more positive sentiment. Can be null to
    /// indicate an unknown or unavailable score.</param>
    /// <returns>A value of the SentimentCategory enumeration representing the sentiment classification: Positive, Negative,
    /// Neutral, or Unknown if the score is null.</returns>
    private SentimentCategory CategorizeSentiment(double? score)
    {
        if (!score.HasValue)
            return SentimentCategory.Unknown;

        return score.Value switch
        {
            >= PositiveSentimentThreshold => SentimentCategory.Positive,
            <= NegativeSentimentThreshold => SentimentCategory.Negative,
            _ => SentimentCategory.Neutral
        };
    }

    /// <summary>
    /// Returns a descriptive message corresponding to the specified sentiment score.
    /// </summary>
    /// <remarks>The thresholds for sentiment categories are determined by the values of
    /// PositiveSentimentThreshold and NegativeSentimentThreshold. Adjust these thresholds to customize the
    /// classification boundaries.</remarks>
    /// <param name="score">The sentiment score to evaluate. A value between 0.0 and 1.0, where higher values indicate more positive
    /// sentiment. If null, sentiment analysis is considered unavailable.</param>
    /// <returns>A string message describing the sentiment category for the given score. Returns "Unable to analyze sentiment" if
    /// <paramref name="score"/> is null.</returns>
    private string GetSentimentMessage(double? score)
    {
        if (!score.HasValue)
            return "Unable to analyze sentiment";

        return score.Value switch
        {
            >= 0.9 => "Highly positive review",
            >= PositiveSentimentThreshold => "Positive review",
            >= 0.4 => "Neutral to positive review",
            >= NegativeSentimentThreshold => "Neutral to negative review",
            >= 0.1 => "Negative review",
            _ => "Highly negative review"
        };
    }

    #endregion
}
