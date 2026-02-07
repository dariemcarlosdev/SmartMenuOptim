using System.Text;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Application.Interfaces;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.Services.Abstraction;

namespace SmartMenuOptim.Application.Services;

/// <summary>
/// Application Service for generating AI-driven improvement strategies for underperforming dishes.
/// </summary>
/// <remarks>
/// This is an Application Layer service that:
/// - Orchestrates use cases and business workflows
/// - Uses domain abstractions (IAiTextGenerator) rather than infrastructure implementations
/// - Coordinates between AI services and domain repositories
/// - Transforms domain data into actionable insights
/// 
/// Clean Architecture compliance:
/// - Depends on domain abstractions (IAiTextGenerator), not infrastructure
/// - Uses application DTOs for data transfer
/// - Contains no business rules (delegates to domain services)
/// 
/// TODO: Convert to CQRS Query Handler pattern in future iterations.
/// </remarks>
public class AiImprovementService : IAImprovementStrategyService
{
    private readonly IAiTextGenerator _aiTextGenerator;
    private readonly ILogger<AiImprovementService> _logger;
    private readonly IUnityOfWork _unityOfWork;

    public AiImprovementService(
        IAiTextGenerator aiTextGenerator,
        ILogger<AiImprovementService> logger,
        IUnityOfWork unityOfWork)
    {
        _aiTextGenerator = aiTextGenerator ?? throw new ArgumentNullException(nameof(aiTextGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unityOfWork = unityOfWork ?? throw new ArgumentNullException(nameof(unityOfWork));
    }

    /// <inheritdoc />
    public async Task<string> GetImprovementStrategyAsync(UnderperformingDishDTO underperformingDishes)
    {
        try
        {
            // Validate input
            if (underperformingDishes == null)
            {
                throw new ArgumentNullException(nameof(underperformingDishes), 
                    "The underperforming dishes cannot be null.");
            }

            // Get reviews for underperforming dish
            var reviews = await _unityOfWork.Reviews.GetAllAsync();
            underperformingDishes.Comments = reviews
                .Where(c => c.Comment.Contains(underperformingDishes.DishName, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Comment)
                .ToList();

            // Build AI prompt
            var prompt = new StringBuilder();
            prompt.AppendLine($"Suggest 3–5 actionable improvements for the dish '{underperformingDishes.DishName}', " +
                $"which has low sales ({underperformingDishes.TotalSales}), " +
                $"low sentiment ({underperformingDishes.AverageSentiment:F2}), " +
                $"and negative comments: '{string.Join("; ", underperformingDishes.Comments)}'. " +
                $"Focus on promotions, recipe updates, or presentation ideas.");

            var systemPrompt = "You are a culinary expert and menu optimization specialist. " +
                "Your task is to analyze underperforming dishes based on sales, customer sentiment, " +
                "and negative comments, then provide actionable suggestions for improvement. " +
                "Consider factors such as ingredients, presentation, pricing, and customer preferences.";

            return await _aiTextGenerator.GenerateAsync(prompt.ToString(), systemPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in GetImprovementStrategyAsync.");
            return "An error occurred while generating improvement strategies. Please try again later.";
        }
    }
}
