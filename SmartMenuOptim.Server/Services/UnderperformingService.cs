using SmartMenuOptim.Server.Services.Interfaces;
using SmartMenuOptim.Shared.Data.Dtos;

namespace SmartMenuOptim.Server.Services
{
    public class UnderperformingService : IUnderperformingService
    {
        private readonly ILogger<UnderperformingService> _logger;
        private readonly HttpClient httpClient;

        public UnderperformingService(IHttpClientFactory httpClientFactory, ILogger<UnderperformingService> logger)
        {
            httpClient = httpClientFactory.CreateClient("BackendAPI");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<List<UnderperformingDishDTO>> GetUnderperformingDishesAsync()
        {
            return await httpClient.GetFromJsonAsync<List<UnderperformingDishDTO>>("api/ai/underperforming-dishes")
                   ?? [];
        }

        // No implemented yet in API
        public async Task<string?> SuggestActionForDishAsync(string dishName)
        {
            return await httpClient.GetFromJsonAsync<string?>($"api/ai/suggest-action/{Uri.EscapeDataString(dishName)}")
                   ?? null;
        }
    }
}
