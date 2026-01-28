using Azure.AI.OpenAI;
using Azure;
using System.Text;
using OpenAI.Chat;
using OpenAI;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Application.Interfaces;
using SmartMenuOptim.Application.Common;

namespace SmartMenuOptim.API.Services
{
    public class AiImprovementService : IAImprovementStrategyService
    {
        private readonly IOpenIAGptService _openAIGpt;
        private readonly ILogger<AiImprovementService> _logger;
        private readonly IUnityOfWork _unityOfWork;

        public AiImprovementService(IOpenIAGptService openIAGptService,
            ILogger<AiImprovementService> logger,
            IUnityOfWork unityOfWork
            )
        {
            _openAIGpt = openIAGptService ?? throw new ArgumentNullException(nameof(openIAGptService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unityOfWork = unityOfWork; // Assuming unityOfWork is injected or set later
        }

        /// <summary>
        /// Generates improvement strategies for a list of underperforming dishes based on their sales and customer sentiment.
        /// </summary>
        /// <param name="underperformingDishes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<string> GetImprovementStrategyAsync(UnderperformingDishDTO underperformingDishes)
        {
            try
            {
                // Validate input
                if (underperformingDishes == null)
                {
                    throw new ArgumentNullException(nameof(underperformingDishes), "The underperforming dishes cannot be null.");
                }

                // Get reviews.Comments for underperforming dish where comment contain underperformaceDies.DishName
                var reviews = await _unityOfWork.Reviews.GetAllAsync();                
                underperformingDishes.Comments = reviews
                    .Where(c => c.Comment.Contains(underperformingDishes.DishName, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Comment)
                    .ToList();


                var prompt = new StringBuilder();
                //prompt.AppendLine("The following dishes are underperforming based on sales and customer sentiment. Suggest improvement strategies for each dish:");

               prompt.AppendLine($"Suggest 3–5 actionable improvements for the dish- '{underperformingDishes.DishName}'," +
                   $" which has low sales ({underperformingDishes.TotalSales})," +
                   $" and low sentiment ({underperformingDishes.AverageSentiment:F2})," +
                   $" and negative comment '{underperformingDishes.Comments}'." +
                   $" Focus on promotions, recipe updates, or presentation ideas.");


                var systemChatMessage = "You are a culinary expert and menu optimization specialist. Your task is to analyze underperforming dishes based on sales, customer sentiment and negative comments, and provide actionable suggestions for improvement. Consider factors such as ingredients, presentation, pricing, and customer preferences.";

                return await _openAIGpt.GenerateAsync(prompt.ToString(), systemChatMessage);
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
