using SmartMenuOptim.Application.Dtos;

namespace SmartMenuOptim.Application.Interfaces;

/// <summary>
/// Defines a contract for services that generate AI-driven improvement strategies
/// for underperforming dishes based on sales and sentiment analysis.
/// </summary>
public interface IAImprovementStrategyService
{
    /// <summary>
    /// Generates actionable improvement strategies for underperforming dishes.
    /// </summary>
    /// <param name="underperformingDishes">DTO containing dish performance data.</param>
    /// <returns>AI-generated improvement strategy as a string.</returns>
    Task<string> GetImprovementStrategyAsync(UnderperformingDishDTO underperformingDishes);
}
