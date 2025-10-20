using SmartMenuOptim.Shared.Data.Dtos;
using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;

internal class ReviewService : IReviewService
{
    private readonly HttpClient _httpClient;

    public ReviewService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("BackendAPI");
    }

    /// <summary>
    /// Retrieves a list of reviews, optionally filtered by dish name and sentiment.
    /// </summary>
    public async Task<List<ReviewDTO>> GetReviewsAsync(string? dishName = null, double? sentiment = null)
    {
        // Using a Dictionary to build query parameters makes it easy to add, remove, or modify parameters
        // and ensures that only non-null values are included in the final query string.
        var queryParams = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(dishName))
            queryParams["dishname"] = dishName;
        if (sentiment.HasValue)
            queryParams["sentiment"] = sentiment.Value.ToString(CultureInfo.InvariantCulture);

        string url = "api/reviews";
        // QueryHelpers.AddQueryString safely appends the query parameters to the base URL,
        // handling encoding and formatting, and preventing manual string concatenation errors.
        if (queryParams.Count > 0)
            url = QueryHelpers.AddQueryString(url, queryParams);

        return await _httpClient.GetFromJsonAsync<List<ReviewDTO>>(url) ?? [];
    }

    /// <summary>
    /// Adds a new review for a product.
    /// </summary>
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
