using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Dtos.Dish;

namespace SmartMenuOptim.Server.Features.Restaurants.Services;

/// <summary>
/// Defines the contract for Dish operations from the Blazor Server client.
/// </summary>
/// <remarks>
/// <para><strong>Architecture Note:</strong></para>
/// <para>This interface adapts the Application layer's IDishService for use
/// in the Blazor Server project, communicating via HTTP with the backend API.</para>
/// </remarks>
public interface IDishClientService
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
    /// Creates a new dish.
    /// </summary>
    Task<Result<DishDTO>> CreateAsync(int restaurantId, DishCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing dish.
    /// </summary>
    Task<Result<DishDTO>> UpdateAsync(DishUpdateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a dish.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a dish to a menu.
    /// </summary>
    Task<Result> AddToMenuAsync(int menuId, int dishId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a dish from a menu.
    /// </summary>
    Task<Result> RemoveFromMenuAsync(int menuId, int dishId, CancellationToken cancellationToken = default);
}
