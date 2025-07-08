using SmartMenuOptim.Shared.Data.Dtos;

namespace SmartMenuOptim.API.Services.Interfaces
{
    public interface IAiImprovementStrategyService
    {
        Task<string> GetImprovementStrategyAsync(List<UnderperformingDishDTO> underperformingDishes);
    }
}
