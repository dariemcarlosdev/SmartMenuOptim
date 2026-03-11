/*
 * File: IRestaurantService.cs
 * Service interface for Restaurant aggregate operations
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Defines the contract for Restaurant management operations
 * following Clean Architecture and Interface Segregation Principle (ISP).
 * 
 * Design Patterns:
 * - Interface Segregation Principle: Focused interface for Restaurant operations
 * - Repository Pattern: Abstracts data access through IUnityOfWork
 * - Result Pattern: Returns operation results with success/failure semantics
 */

using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;

namespace SmartMenuOptim.Application.Features.Restaurants.Services;

/// <summary>
/// Defines the contract for Restaurant aggregate operations.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This interface resides in the Application layer and defines use cases
/// for Restaurant management. Implementations orchestrate domain logic and
/// repository operations.</para>
/// 
/// <para><strong>Interface Segregation Principle:</strong></para>
/// <para>This interface is focused solely on Restaurant aggregate operations.
/// Menu and Category operations are handled by separate service interfaces.</para>
/// 
/// <para><strong>Result Pattern:</strong></para>
/// <para>All methods return Result objects to encapsulate success/failure
/// semantics, avoiding exceptions for expected business failures.</para>
/// </remarks>
public interface IRestaurantService
{
    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves a restaurant by its unique identifier.
    /// </summary>
    /// <param name="id">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the RestaurantDTO if found, or error if not found.</returns>
    Task<Result<RestaurantDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a restaurant with full details including menus, dishes, and categories.
    /// </summary>
    /// <param name="id">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the RestaurantDetailDTO if found, or error if not found.</returns>
    Task<Result<RestaurantDetailDTO>> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all restaurants (non-deleted).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of RestaurantDTOs.</returns>
    Task<Result<IReadOnlyList<RestaurantDTO>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all restaurants owned by a specific user.
    /// </summary>
    /// <param name="ownerId">The owner's user identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of RestaurantDTOs owned by the user.</returns>
    Task<Result<IReadOnlyList<RestaurantDTO>>> GetByOwnerAsync(int ownerId, CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new restaurant.
    /// </summary>
    /// <param name="dto">The restaurant creation data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the created RestaurantDTO, or error if creation failed.</returns>
    Task<Result<RestaurantDTO>> CreateAsync(RestaurantCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing restaurant.
    /// </summary>
    /// <param name="dto">The restaurant update data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the updated RestaurantDTO, or error if update failed.</returns>
    Task<Result<RestaurantDTO>> UpdateAsync(RestaurantUpdateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a restaurant.
    /// </summary>
    /// <param name="id">The restaurant identifier to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles whether a restaurant is accepting orders.
    /// </summary>
    /// <param name="id">The restaurant identifier.</param>
    /// <param name="isAccepting">Whether the restaurant should accept orders.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ToggleAcceptingOrdersAsync(int id, bool isAccepting, CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // BUSINESS HOURS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets the business hours for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="businessHours">The list of business hours to set.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SetBusinessHoursAsync(
        int restaurantId,
        List<BusinessHoursDTO> businessHours,
        CancellationToken cancellationToken = default);
}
