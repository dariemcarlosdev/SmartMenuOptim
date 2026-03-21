/*
 * File: IOrderClientService.cs
 * Client-side service interface for Order operations
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Defines the contract for Order operations from the Blazor Server
 * perspective, communicating with the backend API via HTTP.
 * 
 * Design Patterns:
 * - Interface Segregation Principle: Focused interface for client operations
 * - Adapter Pattern: Adapts HTTP API calls to service interface
 * - Result Pattern: Returns operation results with success/failure semantics
 * 
 * Reference: IRestaurantClientService.cs (canonical pattern)
 */

using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Orders.DTOs;

namespace SmartMenuOptim.Server.Features.Orders.Services;

/// <summary>
/// Defines the contract for Order operations from the Blazor Server client.
/// </summary>
/// <remarks>
/// <para><strong>Architecture Note:</strong></para>
/// <para>This interface adapts the Application layer's IOrderService for use
/// in the Blazor Server project, communicating via HTTP with the backend API.</para>
/// </remarks>
public interface IOrderClientService
{
    /// <summary>
    /// Retrieves all orders for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of OrderDTOs.</returns>
    Task<Result<IReadOnlyList<OrderDTO>>> GetByRestaurantAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an order with full details including items.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the OrderDetailDTO if found.</returns>
    Task<Result<OrderDetailDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves available order statuses for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of OrderStatusDTOs.</returns>
    Task<Result<IReadOnlyList<OrderStatusDTO>>> GetStatusesAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Places a new order.
    /// </summary>
    /// <param name="dto">The order creation data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the created OrderDTO.</returns>
    Task<Result<OrderDTO>> CreateAsync(OrderCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an existing order.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="newStatusId">The new status identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the updated OrderDTO.</returns>
    Task<Result<OrderDTO>> UpdateStatusAsync(int id, int newStatusId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an order with a specified reason.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="reason">The cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CancelAsync(int id, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an order.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
