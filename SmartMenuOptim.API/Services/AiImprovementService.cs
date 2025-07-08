using Azure.AI.OpenAI;
using Azure;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Shared.Data.Dtos;
using System.Text;
using OpenAI.Chat;
using OpenAI;
using Microsoft.Extensions.Logging;

namespace SmartMenuOptim.API.Services
{
    public class AiImprovementService : IAiImprovementStrategyService
    {
        private readonly IOpenIAGptService _gpt;
        private readonly ILogger<AiImprovementService> _logger;

        public AiImprovementService(IOpenIAGptService openIAGptService, ILogger<AiImprovementService> logger)
        {
            this._gpt = openIAGptService ?? throw new ArgumentNullException(nameof(openIAGptService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates improvement strategies for a list of underperforming dishes based on their sales and customer sentiment.
        /// </summary>
        /// <param name="underperformingDishes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<string> GetImprovementStrategyAsync(List<UnderperformingDishDTO> underperformingDishes)
        {
            try
            {
                // Validate input
                if (underperformingDishes == null || underperformingDishes.Count == 0)
                {
                    throw new ArgumentException("The list of underperforming dishes cannot be null or empty.");
                }

                var prompt = new StringBuilder();
                prompt.AppendLine("The following dishes are underperforming based on sales and customer sentiment. Suggest improvement strategies for each dish:");
                foreach (var dish in underperformingDishes)
                {
                    prompt.AppendLine($"- {dish.DishName}: {dish.TotalSales} sales, {dish.AverageSentiment:F2} sentiment, {dish.Comments.ToList()} negative comments");
                }

                var systemChatMessage = "You are a culinary expert and menu optimization specialist. Your task is to analyze underperforming dishes based on sales, customer sentiment and negative comments, and provide actionable suggestions for improvement. Consider factors such as ingredients, presentation, pricing, and customer preferences.";

                return await _gpt.GenerateAsync(prompt.ToString(), systemChatMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in GetImprovementStrategyAsync.");
                // Optionally, return a user-friendly message or rethrow
                return "An error occurred while generating improvement strategies. Please try again later.";
            }
        }
    }
}
