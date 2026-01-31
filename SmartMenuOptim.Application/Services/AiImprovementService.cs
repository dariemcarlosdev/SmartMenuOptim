using System.Text;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Application.Interfaces;  // ✅ CORRECT! Application references Application
using SmartMenuOptim.Domain.Repositories;

namespace SmartMenuOptim.Application.Services;

/// <summary>
/// Provides services for generating improvement strategies for underperforming dishes 
/// using AI-driven analysis of sales data and customer sentiment. 
/// This is an application-level service not tied to infrastructure, as per Clean Architecture principles cuz it uses domain repositories and application interfaces.
/// </summary>
/// <remarks>
/// This service integrates with AI and data access components to analyze dish performance 
/// and suggest actionable improvements. 
/// 
/// TODO: Convert to CQRS Query Handler pattern in future iterations.
/// </remarks>
public class AiImprovementService : IAImprovementStrategyService
{
    private readonly IOpenIAGptService _openAIGpt;
    private readonly ILogger<AiImprovementService> _logger;
    private readonly IUnityOfWork _unityOfWork;

    public AiImprovementService(
        IOpenIAGptService openIAGptService,
        ILogger<AiImprovementService> logger,
        IUnityOfWork unityOfWork)
    {
        _openAIGpt = openIAGptService ?? throw new ArgumentNullException(nameof(openIAGptService));
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

            return await _openAIGpt.GenerateAsync(prompt.ToString(), systemPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in GetImprovementStrategyAsync.");
            return "An error occurred while generating improvement strategies. Please try again later.";
        }
    }
}
