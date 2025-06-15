using SmartMenuOptim.Shared.Models;

namespace SmartMenuOptim.Server.Services
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;

        public AIService( IHttpClientFactory httpClientFactory, ILogger<AIService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("BackendAPI");
            _logger = logger;
        }

        public async Task<AiRecomendationResponse?> GetRecommendationsAsync(List<SaleRecord> sales, List<Review> reviews)
        {
            var request = new AiRecomendationRequest
            {
                SaleRecords = sales,
                Reviews = reviews
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/ai/recommend", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AiRecomendationResponse>();
                }
                else
                {
                    _logger.LogError($"AI recommendation failed with status code {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting AI recommendations");
                return null;
            }
        }
    }
}
