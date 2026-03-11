using SmartMenuOptim.Application.Dtos;

namespace SmartMenuOptim.Application.Contracts;

/// <summary>
/// Defines a contract for services that generate AI-driven improvement strategies for underperforming dishes based on sales and sentiment analysis.
/// Interface in the Application Layer according to Hexagonal Architecture principles since it defines application-specific operations that utilize domain concepts.
/// Orchestrates AI capabilities to provide actionable insights for menu optimization.
/// When I say "orchestrates AI capabilities", I mean that this interface is designed to coordinate and manage the interaction with AI services to generate improvement strategies. It defines a high-level operation that leverages AI to analyze data about underperforming dishes and produce actionable recommendations.
/// This abstraction allows the application to utilize AI technologies without being tightly coupled to specific implementations, adhering to Hexagonal Architecture principles.
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
