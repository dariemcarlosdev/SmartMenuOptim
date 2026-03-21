/*
 * File: OrderListState.cs
 * State container for OrderList component
 * Version: 1.0
 * .NET Target: .NET 9
 *
 * Purpose: Manages state for the OrderList page component.
 * Separates state management from UI rendering following Clean Architecture.
 *
 * Design Patterns:
 * - State Container Pattern: Centralized state for order list
 * - Dependency Injection: Uses IOrderClientService for data operations
 *
 * Reference: RestaurantListState.cs (canonical pattern)
 */

using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Server.Features.Orders.Services;
using SmartMenuOptim.Server.State;

namespace SmartMenuOptim.Server.Features.Orders.State;

/// <summary>
/// State container for OrderList component.
/// Manages loading, error, data, and delete state for the order list.
/// </summary>
/// <remarks>
/// <para><strong>Registration:</strong></para>
/// <para>Register as Scoped service in DI:</para>
/// <code>builder.Services.AddScoped&lt;OrderListState&gt;();</code>
/// </remarks>
public class OrderListState : ComponentStateBase<IReadOnlyList<OrderDTO>>
{
    private readonly IOrderClientService _orderService;
    private readonly ILogger<OrderListState> _logger;

    private bool _deleting;
    private OrderDTO? _orderToDelete;
    private bool _showDeleteModal;
    private IReadOnlyList<OrderStatusDTO>? _statuses;
    private int? _selectedStatusFilter;
    private int _currentRestaurantId;
    private bool _updatingStatus;
    private bool _cancelling;
    private bool _showCancelModal;
    private OrderDTO? _orderToCancel;
    private string _cancelReason = string.Empty;

    /// <summary>
    /// Initializes a new instance of OrderListState.
    /// </summary>
    /// <param name="orderService">The order client service for data operations.</param>
    /// <param name="logger">The logger instance.</param>
    public OrderListState(
        IOrderClientService orderService,
        ILogger<OrderListState> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// The current list of orders (alias for Data property).
    /// </summary>
    public IReadOnlyList<OrderDTO>? Orders => Data;

    /// <summary>
    /// The filtered list of orders based on selected status.
    /// </summary>
    public IReadOnlyList<OrderDTO>? FilteredOrders =>
        _selectedStatusFilter is null
            ? Data
            : Data?.Where(o => o.StatusName.Equals(
                _statuses?.FirstOrDefault(s => s.Id == _selectedStatusFilter)?.Name ?? "",
                StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// Available order statuses for the filter dropdown.
    /// </summary>
    public IReadOnlyList<OrderStatusDTO>? Statuses => _statuses;

    /// <summary>
    /// The currently selected status filter. Null means "all statuses".
    /// </summary>
    public int? SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            _selectedStatusFilter = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Indicates whether a delete operation is in progress.
    /// </summary>
    public bool IsDeleting
    {
        get => _deleting;
        private set
        {
            _deleting = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// The order currently targeted for deletion.
    /// </summary>
    public OrderDTO? OrderToDelete => _orderToDelete;

    /// <summary>
    /// Indicates whether the delete confirmation modal is visible.
    /// </summary>
    public bool ShowDeleteModal => _showDeleteModal;

    /// <summary>
    /// Indicates whether a status update operation is in progress.
    /// </summary>
    public bool IsUpdatingStatus
    {
        get => _updatingStatus;
        private set
        {
            _updatingStatus = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Indicates whether a cancel operation is in progress.
    /// </summary>
    public bool IsCancelling
    {
        get => _cancelling;
        private set
        {
            _cancelling = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Indicates whether the cancel confirmation modal is visible.
    /// </summary>
    public bool ShowCancelModal => _showCancelModal;

    /// <summary>
    /// The order currently targeted for cancellation.
    /// </summary>
    public OrderDTO? OrderToCancel => _orderToCancel;

    /// <summary>
    /// The cancellation reason entered by the user.
    /// </summary>
    public string CancelReason
    {
        get => _cancelReason;
        set
        {
            _cancelReason = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Loads all orders for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        _currentRestaurantId = restaurantId;
        SetLoading();

        try
        {
            var result = await _orderService.GetByRestaurantAsync(restaurantId, cancellationToken);

            if (result.IsSuccess)
            {
                SetData(result.Value ?? (IReadOnlyList<OrderDTO>)[]);
                _logger.LogInformation("Loaded {Count} orders for restaurant {RestaurantId}", Data?.Count ?? 0, restaurantId);

                // Load available statuses for the filter dropdown
                await LoadStatusesAsync(restaurantId, cancellationToken);
            }
            else
            {
                SetError(result.Error ?? "Failed to load orders.");
                _logger.LogWarning("Failed to load orders for restaurant {RestaurantId}: {Error}", restaurantId, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading orders for restaurant {RestaurantId}", restaurantId);
            SetError("An unexpected error occurred.");
        }
    }

    /// <summary>
    /// Loads available order statuses for the filter dropdown.
    /// </summary>
    private async Task LoadStatusesAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _orderService.GetStatusesAsync(restaurantId, cancellationToken);
            if (result.IsSuccess)
            {
                _statuses = result.Value;
                NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load order statuses for restaurant {RestaurantId}", restaurantId);
        }
    }

    /// <summary>
    /// Reloads the current order list (uses last restaurant ID).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(_currentRestaurantId, cancellationToken);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // STATUS UPDATE
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the status of an order inline from the list.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="newStatusId">The new status identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UpdateStatusAsync(int orderId, int newStatusId, CancellationToken cancellationToken = default)
    {
        IsUpdatingStatus = true;

        try
        {
            var result = await _orderService.UpdateStatusAsync(orderId, newStatusId, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Order {Id} status updated to {StatusId}", orderId, newStatusId);
                // Reload list to reflect status change
                await ReloadAsync(cancellationToken);
            }
            else
            {
                SetError(result.Error ?? "Failed to update order status.");
                _logger.LogWarning("Failed to update status for order {Id}: {Error}", orderId, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for order {Id}", orderId);
            SetError("An error occurred while updating order status.");
        }
        finally
        {
            IsUpdatingStatus = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CANCEL ORDER
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Shows the cancel confirmation modal for an order.
    /// </summary>
    /// <param name="order">The order to cancel.</param>
    public void ConfirmCancel(OrderDTO order)
    {
        _orderToCancel = order;
        _cancelReason = string.Empty;
        _showCancelModal = true;
        NotifyStateChanged();
    }

    /// <summary>
    /// Hides the cancel confirmation modal.
    /// </summary>
    public void DismissCancel()
    {
        _orderToCancel = null;
        _cancelReason = string.Empty;
        _showCancelModal = false;
        NotifyStateChanged();
    }

    /// <summary>
    /// Cancels the currently targeted order.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CancelOrderAsync(CancellationToken cancellationToken = default)
    {
        if (_orderToCancel is null || string.IsNullOrWhiteSpace(_cancelReason)) return;

        IsCancelling = true;

        try
        {
            var result = await _orderService.CancelAsync(_orderToCancel.Id, _cancelReason, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Order {Id} cancelled", _orderToCancel.Id);
                DismissCancel();
                await ReloadAsync(cancellationToken);
            }
            else
            {
                SetError(result.Error ?? "Failed to cancel order.");
                _logger.LogWarning("Failed to cancel order {Id}: {Error}", _orderToCancel.Id, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {Id}", _orderToCancel.Id);
            SetError("An error occurred while cancelling the order.");
        }
        finally
        {
            IsCancelling = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DELETE ORDER
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Shows the delete confirmation modal for an order.
    /// </summary>
    /// <param name="order">The order to delete.</param>
    public void ConfirmDelete(OrderDTO order)
    {
        _orderToDelete = order;
        _showDeleteModal = true;
        NotifyStateChanged();
    }

    /// <summary>
    /// Cancels the delete operation and hides the modal.
    /// </summary>
    public void CancelDelete()
    {
        _orderToDelete = null;
        _showDeleteModal = false;
        NotifyStateChanged();
    }

    /// <summary>
    /// Deletes the currently targeted order.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (_orderToDelete is null) return;

        IsDeleting = true;

        try
        {
            var result = await _orderService.DeleteAsync(_orderToDelete.Id, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Order {Id} deleted successfully", _orderToDelete.Id);

                if (Data is not null)
                {
                    var updated = Data.Where(o => o.Id != _orderToDelete.Id).ToList();
                    SetData(updated);
                }

                CancelDelete();
            }
            else
            {
                SetError(result.Error ?? "Failed to delete order. Please try again.");
                _logger.LogWarning("Failed to delete order {Id}: {Error}", _orderToDelete.Id, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {Id}", _orderToDelete.Id);
            SetError("An error occurred while deleting the order.");
        }
        finally
        {
            IsDeleting = false;
        }
    }
}
