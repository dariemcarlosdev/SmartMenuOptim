using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Customers.DTOs;
using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Application.Features.Orders.Mappings;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate.Events;
using SmartMenuOptim.Domain.Exceptions;
using SmartMenuOptim.Domain.Repositories;

namespace SmartMenuOptim.Application.Features.Orders.Services;

/// <summary>
/// Service implementation for Order aggregate operations.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This service orchestrates use cases by coordinating between
/// DTOs, domain entities, and repositories. It resides in the Application layer.</para>
/// 
/// <para><strong>Error Handling:</strong></para>
/// <para>Uses Result pattern to return operation outcomes. Domain exceptions are
/// caught and converted to failure results with appropriate error messages.</para>
/// </remarks>
public class OrderService : IOrderService
{
    private readonly IUnityOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for repository access.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
    public OrderService(
        IUnityOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<OrderDetailDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving order with ID {OrderId}", id);

            var order = await _unitOfWork.Orders
                .Query()
                .IgnoreQueryFilters()   // ← bypasses global filters on Order AND OrderItems
                .Include(o => o.Status)
                .Include(o => o.Customer)
                .Include(o => o.HandledBy)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

            if (order is null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", id);
                return Result<OrderDetailDTO>.Failure($"Order with ID {id} not found.");
            }

            return Result<OrderDetailDTO>.Success(order.ToDetailDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order with ID {OrderId}", id);
            return Result<OrderDetailDTO>.Failure("An error occurred while retrieving the order.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<OrderDTO>>> GetAllByRestaurantAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving orders for restaurant {RestaurantId}", restaurantId);

            var orders = await _unitOfWork.Orders
                .Query()
                .IgnoreQueryFilters()
                .Where(o => o.RestaurantId == restaurantId && !o.IsDeleted)
                .Include(o => o.Status)
                .Include(o => o.Customer)
                .Include(o => o.HandledBy)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            var dtos = orders.Select(o => o.ToDto()).ToList();

            _logger.LogDebug("Retrieved {Count} orders for restaurant {RestaurantId}", dtos.Count, restaurantId);
            return Result<IReadOnlyList<OrderDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for restaurant {RestaurantId}", restaurantId);
            return Result<IReadOnlyList<OrderDTO>>.Failure("An error occurred while retrieving orders.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<PaginatedResponse<OrderDTO>>> GetAllByRestaurantPaginatedAsync(
        int restaurantId,
        PaginatedRequest request,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Retrieving paginated orders for restaurant {RestaurantId} — page {Page}, size {PageSize}, sort {SortBy} {SortDir}, status filter {Status}",
                restaurantId, request.Page, request.PageSize, request.SortBy, request.SortDirection, status ?? "(none)");

            // Build base query with filters
            var query = _unitOfWork.Orders
                .Query()
                .IgnoreQueryFilters()
                .Where(o => o.RestaurantId == restaurantId && !o.IsDeleted)
                .Include(o => o.Status)
                .Include(o => o.Customer)
                .Include(o => o.HandledBy)
                .Include(o => o.OrderItems)
                .AsQueryable();

            // Apply optional status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status != null && o.Status.Name == status);
            }

            // Count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting via allowlist (never pass raw sortBy to OrderBy)
            query = ApplySorting(query, request.SortBy, request.IsDescending);

            // Apply pagination
            var orders = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = orders.Select(o => o.ToDto()).ToList();

            var paginatedResult = PaginatedResponse<OrderDTO>.Create(
                data: dtos,
                totalCount: totalCount,
                page: request.Page,
                pageSize: request.PageSize);

            _logger.LogDebug(
                "Retrieved {Count}/{TotalCount} orders for restaurant {RestaurantId} (page {Page}/{TotalPages})",
                dtos.Count, totalCount, restaurantId, request.Page, paginatedResult.TotalPages);

            return Result<PaginatedResponse<OrderDTO>>.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated orders for restaurant {RestaurantId}", restaurantId);
            return Result<PaginatedResponse<OrderDTO>>.Failure("An error occurred while retrieving orders.");
        }
    }

    /// <summary>
    /// Applies sorting to the order query using an allowlist of permitted fields.
    /// </summary>
    /// <param name="query">The base query.</param>
    /// <param name="sortBy">The field name to sort by.</param>
    /// <param name="descending">Whether to sort descending.</param>
    /// <returns>The sorted query.</returns>
    private static IQueryable<Order> ApplySorting(IQueryable<Order> query, string sortBy, bool descending)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "orderdate" => descending
                ? query.OrderByDescending(o => o.OrderDate)
                : query.OrderBy(o => o.OrderDate),
            "totalamount" => descending
                ? query.OrderByDescending(o => o.TotalAmount)
                : query.OrderBy(o => o.TotalAmount),
            "statusname" => descending
                ? query.OrderByDescending(o => o.Status != null ? o.Status.Name : string.Empty)
                : query.OrderBy(o => o.Status != null ? o.Status.Name : string.Empty),
            // Default: createdAt
            _ => descending
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt),
        };
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<OrderDTO>>> GetByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving orders for customer {CustomerId}", customerId);

            var orders = await _unitOfWork.Orders
                .Query()
                .IgnoreQueryFilters()
                .Where(o => o.CustomerId == customerId && !o.IsDeleted)
                .Include(o => o.Status)
                .Include(o => o.Customer)
                .Include(o => o.HandledBy)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            var dtos = orders.Select(o => o.ToDto()).ToList();

            _logger.LogDebug("Retrieved {Count} orders for customer {CustomerId}", dtos.Count, customerId);
            return Result<IReadOnlyList<OrderDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for customer {CustomerId}", customerId);
            return Result<IReadOnlyList<OrderDTO>>.Failure("An error occurred while retrieving customer orders.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<OrderDTO>>> GetByStatusAsync(int restaurantId, int statusId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving orders for restaurant {RestaurantId} with status {StatusId}", restaurantId, statusId);

            var orders = await _unitOfWork.Orders
                .Query()
                .IgnoreQueryFilters()
                .Where(o => o.RestaurantId == restaurantId && o.OrderStatusId == statusId && !o.IsDeleted)
                .Include(o => o.Status)
                .Include(o => o.Customer)
                .Include(o => o.HandledBy)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            var dtos = orders.Select(o => o.ToDto()).ToList();

            _logger.LogDebug("Retrieved {Count} orders for restaurant {RestaurantId} with status {StatusId}", dtos.Count, restaurantId, statusId);
            return Result<IReadOnlyList<OrderDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for restaurant {RestaurantId} with status {StatusId}", restaurantId, statusId);
            return Result<IReadOnlyList<OrderDTO>>.Failure("An error occurred while retrieving orders.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<OrderStatusDTO>>> GetStatusesAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving order statuses for restaurant {RestaurantId}", restaurantId);

            var statuses = await _unitOfWork.OrderStatuses
                .Query()
                .Where(s => s.RestaurantId == restaurantId && !s.IsDeleted)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync(cancellationToken);

            var dtos = statuses.Select(s => s.ToDto()).ToList();

            _logger.LogDebug("Retrieved {Count} order statuses for restaurant {RestaurantId}", dtos.Count, restaurantId);
            return Result<IReadOnlyList<OrderStatusDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order statuses for restaurant {RestaurantId}", restaurantId);
            return Result<IReadOnlyList<OrderStatusDTO>>.Failure("An error occurred while retrieving order statuses.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<CustomerLookupDTO>>> GetCustomerLookupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving customer lookups");

            var customers = await _unitOfWork.Customers
                .Query()
                .OrderBy(c => c.Name)
                .Select(c => new CustomerLookupDTO
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} customer lookups", customers.Count);
            return Result<IReadOnlyList<CustomerLookupDTO>>.Success(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer lookups");
            return Result<IReadOnlyList<CustomerLookupDTO>>.Failure("An error occurred while retrieving customers.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<OrderDTO>> CreateAsync(OrderCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new order for restaurant {RestaurantId}, customer {CustomerId}", dto.RestaurantId, dto.CustomerId);

            // Look up the Pending status for this restaurant
            var pendingStatus = await _unitOfWork.OrderStatuses
                .Query()
                .FirstOrDefaultAsync(s => s.RestaurantId == dto.RestaurantId && s.Name == "Pending" && !s.IsDeleted, cancellationToken);

            if (pendingStatus is null)
            {
                _logger.LogWarning("No 'Pending' order status found for restaurant {RestaurantId}", dto.RestaurantId);
                return Result<OrderDTO>.Failure("Order status 'Pending' not configured for this restaurant.");
            }

            // Create the Order aggregate
            var order = new Order(
                restaurantId: dto.RestaurantId,
                customerId: dto.CustomerId,
                orderStatusId: pendingStatus.Id,
                specialInstructions: dto.SpecialInstructions);

            // Add items — look up dish prices
            foreach (var item in dto.Items)
            {
                var dish = await _unitOfWork.Dishes
                    .Query()
                    .FirstOrDefaultAsync(d => d.Id == item.DishId && d.RestaurantId == dto.RestaurantId && !d.IsDeleted, cancellationToken);

                if (dish is null)
                {
                    _logger.LogWarning("Dish with ID {DishId} not found in restaurant {RestaurantId}", item.DishId, dto.RestaurantId);
                    return Result<OrderDTO>.Failure($"Dish with ID {item.DishId} not found in this restaurant.");
                }

                order.AddItem(
                    dishId: dish.Id,
                    unitPrice: dish.DishPrice,
                    quantity: item.Quantity,
                    specialInstructions: item.SpecialInstructions);
            }

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // Reload with navigation properties for the DTO
            var created = await _unitOfWork.Orders
                .Query()
                .Include(o => o.Status)
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);

            _logger.LogInformation("Order created successfully with ID {OrderId}", order.Id);

            return Result<OrderDTO>.Success(created!.ToDto());
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error creating order: {Message}", ex.Message);
            return Result<OrderDTO>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating order: {Message}", ex.Message);
            return Result<OrderDTO>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for restaurant {RestaurantId}", dto.RestaurantId);
            return Result<OrderDTO>.Failure("An error occurred while creating the order.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<OrderDTO>> UpdateStatusAsync(int id, int newStatusId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating status of order {OrderId} to status {StatusId}", id, newStatusId);

            // Look up the target status to determine if it's a terminal state
            var targetStatus = await _unitOfWork.OrderStatuses
                .Query()
                .FirstOrDefaultAsync(s => s.Id == newStatusId && !s.IsDeleted, cancellationToken);

            if (targetStatus is null)
            {
                _logger.LogWarning("Order status with ID {StatusId} not found", newStatusId);
                return Result<OrderDTO>.Failure($"Order status with ID {newStatusId} not found.");
            }

            var order = await _unitOfWork.Orders
                .Query()
                .IgnoreQueryFilters()
                .Include(o => o.Status)
                .Include(o => o.Customer)
                .Include(o => o.HandledBy)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Dish)
                        .ThenInclude(d => d.Category)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

            if (order is null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", id);
                return Result<OrderDTO>.Failure($"Order with ID {id} not found.");
            }

            // Use domain methods for terminal statuses to raise proper domain events
            if (targetStatus.Name == "Completed")
            {
                order.Complete(completedStatusId: newStatusId);
            }
            else if (targetStatus.Name == "Cancelled")
            {
                order.Cancel(
                    cancelledStatusId: newStatusId,
                    reason: "Status changed to Cancelled",
                    cancelledBy: CancellationSource.Staff);
            }
            else
            {
                order.UpdateStatus(newStatusId);
            }

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status updated to {StatusName} ({StatusId})", id, targetStatus.Name, newStatusId);

            return Result<OrderDTO>.Success(order.ToDto());
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error updating order status: {Message}", ex.Message);
            return Result<OrderDTO>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status of order {OrderId}", id);
            return Result<OrderDTO>.Failure("An error occurred while updating the order status.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> CancelAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling order {OrderId} with reason: {Reason}", id, reason);

            var order = await _unitOfWork.Orders
                .Query()
                .IgnoreQueryFilters()
                .Include(o => o.Status)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

            if (order is null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", id);
                return Result.Failure($"Order with ID {id} not found.");
            }

            // Look up the Cancelled status for this restaurant
            var cancelledStatus = await _unitOfWork.OrderStatuses
                .Query()
                .FirstOrDefaultAsync(s => s.RestaurantId == order.RestaurantId && s.Name == "Cancelled" && !s.IsDeleted, cancellationToken);

            if (cancelledStatus is null)
            {
                _logger.LogWarning("No 'Cancelled' order status found for restaurant {RestaurantId}", order.RestaurantId);
                return Result.Failure("Order status 'Cancelled' not configured for this restaurant.");
            }

            order.Cancel(
                cancelledStatusId: cancelledStatus.Id,
                reason: reason,
                cancelledBy: CancellationSource.Staff);

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} cancelled successfully", id);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error cancelling order: {Message}", ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error cancelling order: {Message}", ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return Result.Failure("An error occurred while cancelling the order.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Soft-deleting order {OrderId}", id);

            var order = await _unitOfWork.Orders.GetByIdAsync(id);

            if (order is null || order.IsDeleted)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", id);
                return Result.Failure($"Order with ID {id} not found.");
            }

            order.IsDeleted = true;
            order.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} soft-deleted successfully", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", id);
            return Result.Failure("An error occurred while deleting the order.");
        }
    }
}
