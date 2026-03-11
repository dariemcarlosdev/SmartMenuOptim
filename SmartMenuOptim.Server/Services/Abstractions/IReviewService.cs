/*
 * File: IReviewService.cs
 * Review Service interface
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Defines the contract for review operations.
 */

using SmartMenuOptim.Application.Dtos.Review;

namespace SmartMenuOptim.Server.Services.Abstractions;

/// <summary>
/// Defines the contract for review operations.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Retrieves reviews, optionally filtered by dish name and sentiment.
    /// </summary>
    /// <param name="dishName">The name of the dish to filter by (optional).</param>
    /// <param name="sentiment">The sentiment score to filter by (optional).</param>
    /// <returns>A list of reviews matching the criteria.</returns>
    Task<List<ReviewDTO>> GetReviewsAsync(string? dishName = null, double? sentiment = null);

    /// <summary>
    /// Adds a new review.
    /// </summary>
    /// <param name="review">The review to add.</param>
    Task AddReviewAsync(ReviewDTO review);
}
