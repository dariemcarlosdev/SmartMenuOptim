/*
 * File: IAIService.cs
 * AI Service interface for recommendations and insights
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Defines the contract for AI-powered operations in the Blazor Server.
 */

using SmartMenuOptim.Application.Dtos.AI;
using SmartMenuOptim.Application.Dtos.Dish;
using SmartMenuOptim.Application.Dtos.Review;
using SmartMenuOptim.Application.Dtos.Sales;

namespace SmartMenuOptim.Server.Services.Abstractions;

/// <summary>
/// Defines the contract for AI-powered recommendations and insights.
/// </summary>
public interface IAIService
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
