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
        var response = await _httpClient.PostAsJsonAsync("api/reviews", review);
        response.EnsureSuccessStatusCode();
    }
}
