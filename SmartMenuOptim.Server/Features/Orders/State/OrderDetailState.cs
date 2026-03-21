/*
 * File: OrderDetailState.cs
 * State container for OrderDetail component
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Manages state for the OrderDetail page component.
 * Separates state management from UI rendering following Clean Architecture.
 * 
 * Design Patterns:
 * - State Container Pattern: Centralized state for order details
 * - Dependency Injection: Uses IOrderClientService for data operations
 *
 * Reference: RestaurantDetailState.cs (canonical pattern)
 */

using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Server.Features.Orders.Services;
using SmartMenuOptim.Server.State;

namespace SmartMenuOptim.Server.Features.Orders.State;

/// <summary>
/// State container for OrderDetail component.
/// Manages loading, error, and data state for order details.
/// </summary>
/// <remarks>
/// <para><strong>Registration:</strong></para>
/// <para>Register as Scoped service in Program.cs:</para>
/// <code>builder.Services.AddScoped&lt;OrderDetailState&gt;();</code>
/// </remarks>
public class OrderDetailState : ComponentStateBase<OrderDetailDTO>
{
    private readonly IOrderClientService _orderService;
    private readonly ILogger<OrderDetailState> _logger;
    private bool _updatingStatus;
    private bool _cancelling;
    private bool _showCancelModal;
    private string _cancelReason = string.Empty;
    private IReadOnlyList<OrderStatusDTO>? _statuses;

    /// <summary>
    /// Initializes a new instance of OrderDetailState.
    /// </summary>
    /// <param name="orderService">The order client service for data operations.</param>
    /// <param name="logger">The logger instance.</param>
    public OrderDetailState(
        IOrderClientService orderService,
        ILogger<OrderDetailState> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// The current order data (alias for Data property for clarity).
    /// </summary>
    public OrderDetailDTO? Order => Data;

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
    /// Available order statuses for the dropdown.
    /// </summary>
    public IReadOnlyList<OrderStatusDTO>? Statuses => _statuses;

    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads order data by ID.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadAsync(int id, CancellationToken cancellationToken = default)
    {
        SetLoading();

        try
        {
            var result = await _orderService.GetByIdAsync(id, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                SetData(result.Value);
                _logger.LogInformation("Loaded order {Id}", id);

                // Load available statuses for the status dropdown
                await LoadStatusesAsync(result.Value.RestaurantId, cancellationToken);
            }
            else
            {
                SetError(result.Error ?? "Order not found.");
                _logger.LogWarning("Failed to load order {Id}: {Error}", id, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading order {Id}", id);
            SetError("An unexpected error occurred while loading the order.");
        }
    }

    /// <summary>
    /// Loads available order statuses for the restaurant.
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

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS — STATUS UPDATE
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the order's status.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="newStatusId">The new status identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateStatusAsync(int orderId, int newStatusId, CancellationToken cancellationToken = default)
    {
        if (Order is null) return false;

        IsUpdatingStatus = true;

        try
        {
            var result = await _orderService.UpdateStatusAsync(orderId, newStatusId, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Order {Id} status updated to {StatusId}", orderId, newStatusId);
                // Reload full order to reflect all changes (status name, color, terminal flag)
                await LoadAsync(orderId, cancellationToken);
                return true;
            }
            else
            {
                SetError(result.Error ?? "Failed to update order status.");
                _logger.LogWarning("Failed to update status for order {Id}: {Error}", orderId, result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for order {Id}", orderId);
            SetError("An error occurred while updating order status.");
            return false;
        }
        finally
        {
            IsUpdatingStatus = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS — CANCEL
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Shows the cancel confirmation modal.
    /// </summary>
    public void ShowCancelConfirmation()
    {
        _showCancelModal = true;
        _cancelReason = string.Empty;
        NotifyStateChanged();
    }

    /// <summary>
    /// Hides the cancel confirmation modal.
    /// </summary>
    public void HideCancelConfirmation()
    {
        _showCancelModal = false;
        _cancelReason = string.Empty;
        NotifyStateChanged();
    }

    /// <summary>
    /// Cancels the order with the specified reason.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the cancellation was successful, false otherwise.</returns>
    public async Task<bool> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_cancelReason)) return false;

        IsCancelling = true;

        try
        {
            var result = await _orderService.CancelAsync(orderId, _cancelReason, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Order {Id} cancelled with reason: {Reason}", orderId, _cancelReason);
                HideCancelConfirmation();
                // Reload to reflect cancelled state
                await LoadAsync(orderId, cancellationToken);
                return true;
            }
            else
            {
                SetError(result.Error ?? "Failed to cancel order.");
                _logger.LogWarning("Failed to cancel order {Id}: {Error}", orderId, result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {Id}", orderId);
            SetError("An error occurred while cancelling the order.");
            return false;
        }
        finally
        {
            IsCancelling = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clears the current error message.
    /// </summary>
    public void ClearError()
    {
        SetError(null!);
    }
}
