/*
 * File: ReviewService.cs
 * Review Service implementation
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Implements review operations via HTTP client to backend API.
 */

using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using SmartMenuOptim.Application.Features.Reviews.DTOs;
using SmartMenuOptim.Server.Features.Reviews.Services;

namespace SmartMenuOptim.Server.Features.Reviews.Services;

/// <summary>
/// HTTP client-based implementation for review operations.
/// </summary>
public class ReviewClientService : IReviewClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReviewClientService> _logger;

    /// <summary>
    /// Initializes a new instance of ReviewClientService.
    /// </summary>
    public ReviewClientService(IHttpClientFactory httpClientFactory, ILogger<ReviewClientService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BackendAPI");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ReviewDTO>> GetReviewsAsync(string? dishName = null, double? sentiment = null)
    {
        var queryParams = new Dictionary<string, string?>();

        if (!string.IsNullOrWhiteSpace(dishName))
            queryParams["dishname"] = dishName;
        if (sentiment.HasValue)
            queryParams["sentiment"] = sentiment.Value.ToString(CultureInfo.InvariantCulture);

        string url = "api/reviews";
        if (queryParams.Count > 0)
            url = QueryHelpers.AddQueryString(url, queryParams);

        try
        {
            return await _httpClient.GetFromJsonAsync<List<ReviewDTO>>(url) ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve reviews from API");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task AddReviewAsync(ReviewDTO review)
    {
        if (review.DateCreated == default)
            review.DateCreated = DateTime.UtcNow;
        if (review.Rating < 1 || review.Rating > 5)
            review.Rating = 0;

        var response = await _httpClient.PostAsJsonAsync("api/reviews", review);
        response.EnsureSuccessStatusCode();
    }
}
