using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Domain.Events.OrderEvents;

/// <summary>
/// Domain event raised when an order is cancelled, either by the customer, 
/// staff, or system (e.g., payment failure, timeout).
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong></para>
/// <para>This event is raised by the Order aggregate when an order is cancelled,
/// capturing the reason and context for the cancellation.</para>
/// 
/// <para><strong>Typical Event Handlers:</strong></para>
/// <list type="bullet">
///     <item><description><strong>LoyaltyPointsHandler:</strong> Reverses any loyalty points that were pre-awarded</description></item>
///     <item><description><strong>InventoryHandler:</strong> Restores reserved ingredient stock levels</description></item>
///     <item><description><strong>RefundHandler:</strong> Initiates payment refund if applicable</description></item>
///     <item><description><strong>CustomerNotificationHandler:</strong> Sends cancellation confirmation</description></item>
///     <item><description><strong>AnalyticsHandler:</strong> Updates cancellation metrics and patterns</description></item>
///     <item><description><strong>KitchenHandler:</strong> Removes order from kitchen display if in preparation</description></item>
/// </list>
/// 
/// <para><strong>Business Rules:</strong></para>
/// <list type="bullet">
///     <item><description>Orders in certain states (e.g., Completed, Delivered) cannot be cancelled</description></item>
///     <item><description>Cancellation reason is required for auditing purposes</description></item>
///     <item><description>Late cancellations may incur fees (handled by event handlers)</description></item>
/// </list>
/// 
/// <para><strong>Cancellation Sources:</strong></para>
/// <list type="bullet">
///     <item><description><strong>Customer:</strong> Customer requested cancellation</description></item>
///     <item><description><strong>Staff:</strong> Staff cancelled due to issue (out of stock, etc.)</description></item>
///     <item><description><strong>System:</strong> Automatic cancellation (payment timeout, etc.)</description></item>
/// </list>
/// </remarks>
public sealed class OrderCancelledEvent : DomainEventBase
{
    /// <summary>
    /// Gets the unique identifier of the cancelled order.
    /// </summary>
    public int OrderId { get; init; }

    /// <summary>
    /// Gets the restaurant (tenant) identifier.
    /// </summary>
    public int RestaurantId { get; init; }

    /// <summary>
    /// Gets the customer identifier who placed the original order.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Gets the reason for order cancellation.
    /// </summary>
    public string CancellationReason { get; init; } = string.Empty;

    /// <summary>
    /// Gets the source of the cancellation request.
    /// </summary>
    public CancellationSource CancelledBy { get; init; }

    /// <summary>
    /// Gets the staff member ID if cancelled by staff.
    /// </summary>
    public int? CancelledByStaffId { get; init; }

    /// <summary>
    /// Gets the order total amount for refund calculation purposes.
    /// </summary>
    public decimal OrderTotal { get; init; }

    /// <summary>
    /// Gets the previous status of the order before cancellation.
    /// </summary>
    public string PreviousStatus { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether a refund should be processed.
    /// </summary>
    public bool RequiresRefund { get; init; }

    /// <summary>
    /// Gets whether any loyalty points need to be reversed.
    /// </summary>
    public int LoyaltyPointsToReverse { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderCancelledEvent"/> class.
    /// </summary>
    public OrderCancelledEvent(
        int orderId,
        int restaurantId,
        int customerId,
        string cancellationReason,
        CancellationSource cancelledBy,
        decimal orderTotal,
        string previousStatus,
        bool requiresRefund = false,
        int loyaltyPointsToReverse = 0,
        int? cancelledByStaffId = null)
    {
        OrderId = orderId;
        RestaurantId = restaurantId;
        CustomerId = customerId;
        CancellationReason = cancellationReason;
        CancelledBy = cancelledBy;
        OrderTotal = orderTotal;
        PreviousStatus = previousStatus;
        RequiresRefund = requiresRefund;
        LoyaltyPointsToReverse = loyaltyPointsToReverse;
        CancelledByStaffId = cancelledByStaffId;
    }

    /// <summary>
    /// Private parameterless constructor for serialization support.
    /// </summary>
    private OrderCancelledEvent() { }
}

/// <summary>
/// Defines the source of an order cancellation.
/// </summary>
public enum CancellationSource
{
    /// <summary>Customer requested the cancellation.</summary>
    Customer = 0,

    /// <summary>Staff member cancelled the order.</summary>
    Staff = 1,

    /// <summary>System automatically cancelled (timeout, payment failure, etc.).</summary>
    System = 2
}
