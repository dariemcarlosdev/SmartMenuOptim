using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Customers.DTOs;
using SmartMenuOptim.Application.Features.Orders.DTOs;

namespace SmartMenuOptim.Application.Features.Orders.Services;

/// <summary>
/// Defines the contract for Order aggregate operations.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This interface resides in the Application layer and defines use cases
/// for Order management. Implementations orchestrate domain logic and
/// repository operations.</para>
/// 
/// <para><strong>Result Pattern:</strong></para>
/// <para>All methods return Result objects to encapsulate success/failure
/// semantics, avoiding exceptions for expected business failures.</para>
/// </remarks>
public interface IOrderService
{
    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves an order with full details including items.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the OrderDetailDTO if found, or error if not found.</returns>
    Task<Result<OrderDetailDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all orders for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of OrderDTOs.</returns>
    Task<Result<IReadOnlyList<OrderDTO>>> GetAllByRestaurantAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated orders for a restaurant with sorting and optional status filtering.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="request">Pagination, sorting, and filtering parameters.</param>
    /// <param name="status">Optional status name filter (exact match).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a paginated response of OrderDTOs.</returns>
    Task<Result<PaginatedResponse<OrderDTO>>> GetAllByRestaurantPaginatedAsync(
        int restaurantId,
        PaginatedRequest request,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all orders placed by a customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of OrderDTOs.</returns>
    Task<Result<IReadOnlyList<OrderDTO>>> GetByCustomerAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves orders for a restaurant filtered by status.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="statusId">The order status identifier to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of OrderDTOs.</returns>
    Task<Result<IReadOnlyList<OrderDTO>>> GetByStatusAsync(int restaurantId, int statusId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves available order statuses for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of OrderStatusDTOs.</returns>
    Task<Result<IReadOnlyList<OrderStatusDTO>>> GetStatusesAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a lightweight list of all customers for dropdown/lookup scenarios.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of CustomerLookupDTOs.</returns>
    Task<Result<IReadOnlyList<CustomerLookupDTO>>> GetCustomerLookupsAsync(CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="dto">The order creation data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the created OrderDTO, or error if creation failed.</returns>
    Task<Result<OrderDTO>> CreateAsync(OrderCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an existing order.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="newStatusId">The new order status identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the updated OrderDTO, or error if update failed.</returns>
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
