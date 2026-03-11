/*
 * File: IRestaurantClientService.cs
 * Client-side service interface for Restaurant operations
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Defines the contract for Restaurant operations from the Blazor Server
 * perspective, communicating with the backend API via HTTP.
 * 
 * Design Patterns:
 * - Interface Segregation Principle: Focused interface for client operations
 * - Adapter Pattern: Adapts HTTP API calls to service interface
 * - Result Pattern: Returns operation results with success/failure semantics
 */

using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;

namespace SmartMenuOptim.Server.Features.Restaurants.Services;

/// <summary>
/// Defines the contract for Restaurant operations from the Blazor Server client.
/// </summary>
/// <remarks>
/// <para><strong>Architecture Note:</strong></para>
/// <para>This interface adapts the Application layer's IRestaurantService for use
/// in the Blazor Server project, communicating via HTTP with the backend API.</para>
/// </remarks>
public interface IRestaurantClientService
{
    /// <summary>
    /// Retrieves a restaurant by its unique identifier.
    /// </summary>
    /// <param name="id">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the RestaurantDTO if found.</returns>
    Task<Result<RestaurantDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all restaurants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of RestaurantDTOs.</returns>
    Task<Result<IReadOnlyList<RestaurantDTO>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles whether a restaurant is accepting orders.
    /// </summary>
    /// <param name="id">The restaurant identifier.</param>
    /// <param name="isAccepting">Whether the restaurant should accept orders.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ToggleAcceptingOrdersAsync(int id, bool isAccepting, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new restaurant.
    /// </summary>
    /// <param name="dto">The restaurant creation data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the created RestaurantDTO.</returns>
    Task<Result<RestaurantDTO>> CreateAsync(RestaurantCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing restaurant.
    /// </summary>
    /// <param name="dto">The restaurant update data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the updated RestaurantDTO.</returns>
    Task<Result<RestaurantDTO>> UpdateAsync(RestaurantUpdateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a restaurant.
    /// </summary>
    /// <param name="id">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
