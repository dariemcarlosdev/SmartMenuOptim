using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.Services;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Provides application-level operations for analyzing and aggregating review sentiment data.
/// </summary>
/// <remarks>This service coordinates review sentiment analysis and aggregation by interacting with the underlying
/// sentiment analysis and review repository services. It is intended to be used as an entry point for business logic
/// related to review sentiment within the application. All methods are asynchronous and log relevant information and
/// errors for monitoring and diagnostics.</remarks>
public class ReviewApplicationService
{
    // Dependencies

    private readonly ReviewSentimentAnalysisService _sentimentAnalysisService; // Domain Service
    private readonly IRepository<Review> _reviewRepository; // Repository for accessing Review entities  
    private readonly ILogger<ReviewApplicationService> _logger; // Logger for logging information and errors

    public ReviewApplicationService(
        ReviewSentimentAnalysisService sentimentService,
        IRepository<Review> reviewRepository,
        ILogger<ReviewApplicationService> logger)
    {
        _sentimentAnalysisService = sentimentService;
        _reviewRepository = reviewRepository;
        _logger = logger;
    }

        /// <summary>
        /// Analyzes the sentiment of a review identified by the specified review ID asynchronously.
        /// </summary>
        /// <remarks>Throws a NotFoundException if the review with the specified ID does not exist. This
        /// method logs informational and error messages during the analysis process.</remarks>
        /// <param name="reviewId">The unique identifier of the review to analyze. Must correspond to an existing review.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a ReviewSentimentResult with the
        /// sentiment analysis of the specified review.</returns>
        public async Task<ReviewSentimentResult> AnalyzeReviewAsync(int reviewId)
        {
            try
            {
                _logger.LogInformation("Starting sentiment analysis for review {ReviewId}", reviewId);

                var review = await _reviewRepository.GetByIdAsync(reviewId);
                if (review == null)
                {
                    _logger.LogWarning("Review {ReviewId} not found", reviewId);
                    throw new NotFoundException($"Review {reviewId} not found");
                }

                var result = await _sentimentAnalysisService.AnalyzeReviewSentimentAsync(review);

                _logger.LogInformation("Successfully analyzed sentiment for review {ReviewId}. Sentiment: {Sentiment}",
                    reviewId, result.SentimentScore);

                return result;
            }
            catch (NotFoundException ex)
            {
                _logger.LogError(ex, "Review {ReviewId} not found during sentiment analysis", reviewId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing sentiment for review {ReviewId}", reviewId);
                throw;
            }
        }

    /// <summary>
    /// Analyzes and returns the aggregate sentiment of all non-deleted reviews for the specified dish.
    /// </summary>
    /// <remarks>This method retrieves all non-deleted reviews for the given dish and performs sentiment
    /// analysis across them. The result reflects the combined sentiment of all relevant reviews. If no reviews are
    /// found, the returned sentiment may indicate a neutral or undefined state depending on the implementation of
    /// AggregateReviewSentiment.</remarks>
    /// <param name="dishId">The unique identifier of the dish for which to analyze review sentiment.</param>
    /// <returns>An AggregateReviewSentiment representing the overall sentiment derived from all reviews associated with the
    /// specified dish.</returns>
    public async Task<AggregateReviewSentiment> GetDishSentimentAsync(int dishId)
    {
        try
        {
            _logger.LogInformation("Starting aggregate sentiment analysis for dish {DishId}", dishId);

            var reviews = await _reviewRepository.Query()
                .Where(r => r.DishId == dishId && !r.IsDeleted)
                .ToListAsync();

            var result = await _sentimentAnalysisService.AnalyzeMultipleReviewsAsync(reviews);

            _logger.LogInformation("Successfully analyzed aggregate sentiment for dish {DishId}. Total reviews: {ReviewCount}",
                dishId, reviews.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing aggregate sentiment for dish {DishId}", dishId);
            throw;
        }
    }
}