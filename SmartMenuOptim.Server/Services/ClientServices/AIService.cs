/*
 * File: AIService.cs
 * AI Service implementation for recommendations and insights
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Implements AI-powered operations via HTTP client to backend API.
 */

using SmartMenuOptim.Application.Dtos.AI;
using SmartMenuOptim.Application.Dtos.Dish;
using SmartMenuOptim.Application.Dtos.Review;
using SmartMenuOptim.Application.Dtos.Sales;
using SmartMenuOptim.Server.Services.Abstractions;
using SmartMenuOptim.Server.Services.Models;

namespace SmartMenuOptim.Server.Services.ClientServices;

/// <summary>
/// HTTP client-based implementation for AI operations.
/// </summary>
public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIService> _logger;

    /// <summary>
    /// Initializes a new instance of AIService.
    /// </summary>
    public AIService(IHttpClientFactory httpClientFactory, ILogger<AIService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BackendAPI");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetImprovementStrategyAsync(UnderperformingDishDTO underperformingDishes)
    {
        if (underperformingDishes == null)
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
            var response = await _httpClient.PostAsJsonAsync(uri, request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                _logger.LogError("AI improvement strategy failed with status code {StatusCode}", response.StatusCode);
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting AI improvement strategy");
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public async Task<List<AiRecommendationResponseDTO>?> GetRecommendationsAsync(
        List<SaleRecordDTO> sales, 
        List<ReviewDTO> reviews)
    {
        var request = new AiRecommendationRequestDTO
        {
            SaleRecords = sales,
            Reviews = reviews
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/ai/recommend", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<AiRecommendationResponseDTO>>();
            }
            else
            {
                _logger.LogError("AI recommendation failed with status code {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting AI recommendations");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<UnderperformingDishDTO>> GetUnderperformingDishesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<UnderperformingDishDTO>>("api/ai/underperforming")
                ?? [];
    }
}
