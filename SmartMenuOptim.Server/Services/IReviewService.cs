using SmartMenuOptim.Shared.Data.Entities;

internal interface IReviewService
{
    /// <summary>
    /// Retrieves a list of reviews for a specific product.
    /// </summary>
    /// <param name="productId">The ID of the product to retrieve reviews for.</param>
    /// <returns>A list of reviews for the specified product.</returns>
    Task<List<Review>> GetReviewsAsync();
    /// <summary>
    /// Adds a new review for a product.
    /// </summary>
    /// <param name="review">The review to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddReviewAsync(Review review);
}