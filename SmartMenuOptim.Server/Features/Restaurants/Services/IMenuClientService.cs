using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;

namespace SmartMenuOptim.Server.Features.Restaurants.Services;

/// <summary>
/// Defines the contract for Menu operations from the Blazor Server client.
/// </summary>
/// <remarks>
/// <para><strong>Architecture Note:</strong></para>
/// <para>This interface adapts the Application layer's IMenuService for use
/// in the Blazor Server project, communicating via HTTP with the backend API.</para>
/// </remarks>
public interface IMenuClientService
{
    /// <summary>
    /// Retrieves a menu by its unique identifier.
    /// </summary>
    Task<Result<MenuDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all menus for a restaurant.
    /// </summary>
    Task<Result<IReadOnlyList<MenuDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new menu for a restaurant.
    /// </summary>
    Task<Result<MenuDTO>> CreateAsync(int restaurantId, MenuCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing menu.
    /// </summary>
    Task<Result<MenuDTO>> UpdateAsync(MenuUpdateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a menu.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a menu.
    /// </summary>
    Task<Result> ActivateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a menu.
    /// </summary>
    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
