using SmartMenuOptim.Application.Common;

namespace SmartMenuOptim.API.Services.Interfaces
{
    public interface IAImprovementStrategyService
    {
        Task<string> GetImprovementStrategyAsync(UnderperformingDishDTO underperformingDishes);
    }
}
