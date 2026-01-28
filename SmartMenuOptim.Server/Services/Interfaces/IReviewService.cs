using SmartMenuOptim.Application.Common;

internal interface IReviewService
{
    /// <summary>
    /// Retrieves a list of reviews, optionally filtered by dish name.
    /// </summary>
    /// <param name="dishName">The name of the dish to filter reviews by (optional).</param>
    /// <returns>A list of reviews for the specified dish or all reviews if dishName is null or empty.</returns>
    Task<List<ReviewDTO>> GetReviewsAsync(string? dishName = null, double? sentiment = null);
    /// <summary>
    /// Adds a new review for a product.
    /// </summary>
    /// <param name="review">The review to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddReviewAsync(ReviewDTO review);
}