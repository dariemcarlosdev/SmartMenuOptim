using SmartMenuOptim.Shared.Data.Entities;

internal class ReviewService : IReviewService
{
    private readonly HttpClient _httpClient;

    public ReviewService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("BackendAPI");
    }

    public async Task<List<Review>> GetReviewsAsync()
       => await _httpClient.GetFromJsonAsync<List<Review>>("api/reviews") ?? [];

    public async Task AddReviewAsync(Review review)
    {
        // Ensure DateCreated is set
        if (review.DateCreated == default)
        {
            review.DateCreated = DateTime.UtcNow;
        }
        // Ensure Rating is set (default to 0 if out of range)
        if (review.Rating < 1 || review.Rating > 5)
        {
            review.Rating = 0;
        }
        var response = await _httpClient.PostAsJsonAsync("api/reviews", review);
        response.EnsureSuccessStatusCode();
    }
}
