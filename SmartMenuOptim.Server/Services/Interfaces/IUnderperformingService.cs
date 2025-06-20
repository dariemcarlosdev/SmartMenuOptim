using SmartMenuOptim.Shared.Data.Dtos;

namespace SmartMenuOptim.Server.Services.Interfaces
{
    public interface IUnderperformingService
    {
        /// <summary>
        /// Retrieves a list of underperforming dishes based on sales and sentiment analysis.
        /// </summary>
        /// <returns>A list of underperforming dishes with their details.</returns>
        Task<List<UnderperformingDishDTO>> GetUnderperformingDishesAsync();
        /// <summary>
        /// Suggests actions for underperforming dishes based on sales and sentiment analysis.
        /// </summary>
        /// <param name="dishName">The name of the dish to analyze.</param>
        /// <returns>A suggested action for the specified dish.</returns>
        Task<string?> SuggestActionForDishAsync(string dishName);
    }
}
