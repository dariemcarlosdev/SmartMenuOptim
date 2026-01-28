using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Server.Services.Interfaces;

namespace SmartMenuOptim.Server.Services
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;

        // HttpClientFactory is preferred to avoid socket exhaustion and manage the lifecycle of HttpClient instances
        public AIService( IHttpClientFactory httpClientFactory, ILogger<AIService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("BackendAPI");
            _logger = logger;
        }

        public async Task<string> GetImprovementStrategyAsync(UnderperformingDishDTO underperformingDishes)
        {
            if( underperformingDishes == null)
            {
                _logger.LogError("The underperforming dishes cannot be null.");
                throw new ArgumentNullException(nameof(underperformingDishes), "The underperforming dishes cannot be null.");
            }

            var request = new AiImprovementRequest
            {
                DishName = underperformingDishes.DishName,
                TotalSales = underperformingDishes.TotalSales,
                AverageSentiment = underperformingDishes.AverageSentiment,
                Comments = underperformingDishes.Comments
            };

            var uri = $"api/ai/underperforming/improve-strategy?name={Uri.EscapeDataString(request.DishName)}&sales={request.TotalSales}&sentiment={request.AverageSentiment}";

            try
            {
   
                var response = await _httpClient.PostAsJsonAsync(uri,request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    _logger.LogError($"AI improvement strategy failed with status code {response.StatusCode}");
                    return string.Empty;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error occurred while getting AI improvement strategy");
                return string.Empty;
            }

        }

        public async Task<List<AiRecomendationResponseDTO>?> GetRecommendationsAsync(List<SaleRecordDTO> sales, List<ReviewDTO> reviews)
        {
            var request = new AiRecomendationRequestDTO
            {
                SaleRecords = sales,
                Reviews = reviews
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/ai/recommend", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<AiRecomendationResponseDTO>>();
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

        public async Task<List<UnderperformingDishDTO>> GetUnderperformingDishesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<UnderperformingDishDTO>>("api/ai/underperforming")
                    ?? [];
        }
    }
}
