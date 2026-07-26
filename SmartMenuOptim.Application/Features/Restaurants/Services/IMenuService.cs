/*
 * File: IMenuService.cs
 * Service interface for Menu aggregate operations
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Defines the contract for Menu management operations
 * following Clean Architecture and Interface Segregation Principle (ISP).
 */

using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;

namespace SmartMenuOptim.Application.Features.Restaurants.Services;

/// <summary>
/// Defines the contract for Menu aggregate operations.
/// </summary>
/// <remarks>
/// <para><strong>Interface Segregation Principle:</strong></para>
/// <para>This interface is focused solely on Menu operations, separate from
/// Restaurant and Category operations.</para>
/// </remarks>
public interface IMenuService
{
    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves a menu by its unique identifier.
    /// </summary>
    /// <param name="id">The menu identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the MenuDTO if found.</returns>
    Task<Result<MenuDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all menus for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of MenuDTOs.</returns>
    Task<Result<IReadOnlyList<MenuDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves only active menus for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of active MenuDTOs.</returns>
    Task<Result<IReadOnlyList<MenuDTO>>> GetActiveByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new menu for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="dto">The menu creation data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the created MenuDTO.</returns>
    Task<Result<MenuDTO>> CreateAsync(int restaurantId, MenuCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing menu.
    /// </summary>
    /// <param name="dto">The menu update data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the updated MenuDTO.</returns>
    Task<Result<MenuDTO>> UpdateAsync(MenuUpdateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a menu.
    /// </summary>
    /// <param name="id">The menu identifier to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a menu available/active.
    /// </summary>
    /// <param name="id">The menu identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> MakeAvailableAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a menu unavailable/inactive.
    /// </summary>
    /// <param name="id">The menu identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> MakeUnavailableAsync(int id, CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // DISH MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a dish to a menu.
    /// </summary>
    /// <param name="menuId">The menu identifier.</param>
    /// <param name="dishId">The dish identifier to add.</param>
    /// <param name="displayOrder">Optional display order for the dish.</param>
    /// <param name="specialPrice">Optional special price for this menu.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> AddDishToMenuAsync(
        int menuId,
        int dishId,
        int displayOrder = 0,
        decimal? specialPrice = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a dish from a menu.
    /// </summary>
    /// <param name="menuId">The menu identifier.</param>
    /// <param name="dishId">The dish identifier to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RemoveDishFromMenuAsync(int menuId, int dishId, CancellationToken cancellationToken = default);
}
