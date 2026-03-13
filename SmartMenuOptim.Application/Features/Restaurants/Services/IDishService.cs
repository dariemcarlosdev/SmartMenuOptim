using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;

namespace SmartMenuOptim.Application.Features.Restaurants.Services;

/// <summary>
/// Defines the contract for Dish aggregate operations.
/// </summary>
public interface IDishService
{
    /// <summary>
    /// Retrieves a dish by its unique identifier.
    /// </summary>
    Task<Result<DishDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all dishes for a restaurant.
    /// </summary>
    Task<Result<IReadOnlyList<DishDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all dishes assigned to a specific menu.
    /// </summary>
    Task<Result<IReadOnlyList<DishDTO>>> GetByMenuIdAsync(int menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new dish for a restaurant.
    /// </summary>
    Task<Result<DishDTO>> CreateAsync(DishCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing dish.
    /// </summary>
    Task<Result<DishDTO>> UpdateAsync(DishUpdateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a dish.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
