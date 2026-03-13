/*
 * File: IAIService.cs
 * AI Service interface for recommendations and insights
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Defines the contract for AI-powered operations in the Blazor Server.
 */

using SmartMenuOptim.Application.Features.AI.DTOs;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Application.Features.Reviews.DTOs;
using SmartMenuOptim.Application.Features.Sales.DTOs;

namespace SmartMenuOptim.Server.Features.AI.Services;

/// <summary>
/// Defines the contract for AI-powered recommendations and insights.
/// </summary>
public interface IAIClientService
{
    /// <summary>
    /// Gets AI-powered recommendations based on sales and review data.
    /// </summary>
    Task<List<AiRecommendationResponseDTO>?> GetRecommendationsAsync(
        List<SaleRecordDTO> sales, 
        List<ReviewDTO> reviews);

    /// <summary>
    /// Gets improvement strategy for underperforming dishes.
    /// </summary>
    Task<string> GetImprovementStrategyAsync(UnderperformingDishDTO underperformingDishes);

    /// <summary>
    /// Retrieves list of underperforming dishes based on analytics.
    /// </summary>
    Task<List<UnderperformingDishDTO>> GetUnderperformingDishesAsync();
}
