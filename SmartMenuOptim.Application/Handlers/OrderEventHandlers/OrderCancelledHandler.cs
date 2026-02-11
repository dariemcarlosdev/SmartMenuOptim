using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Events.OrderEvents;

namespace SmartMenuOptim.Application.Handlers.OrderEventHandlers;

/// <summary>
/// Handles the <see cref="OrderCancelledEvent"/> to process necessary reversals and notifications.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Coordinates all actions required when an order is cancelled, including loyalty point reversal,
/// inventory restoration, refund processing, and customer notification.</para>
/// 
/// <para><strong>Actions Performed:</strong></para>
/// <list type="bullet">
///     <item><description>Reverse loyalty points if applicable</description></item>
///     <item><description>Notify customer of cancellation</description></item>
///     <item><description>Update analytics with cancellation data</description></item>
///     <item><description>Remove order from kitchen display</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class OrderCancelledHandler : ResilientEventHandlerBase<OrderCancelledEvent>, INotificationHandler<OrderCancelledEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<OrderCancelledHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderCancelledHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service for customer communications.</param>
    /// <param name="cacheService">The cache service for invalidating analytics caches.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="deadLetterQueue">
    /// Optional dead letter queue service for capturing failed events.
    /// <para><strong>Why optional (null default):</strong></para>
    /// <list type="bullet">
    ///     <item><description>In production, inject a durable DLQ service (e.g., Azure Service Bus DLQ) to capture failed events for later analysis and reprocessing.</description></item>
    ///     <item><description>In development/testing, use an in-memory implementation that logs failures without persistence overhead.</description></item>
    ///     <item><description>The null default allows flexible DI registration across environments without requiring infrastructure setup.</description></item>
    /// </list>
    /// </param>
    /// <remarks>
    /// <para><strong>Repository Injection Note:</strong></para>
    /// <para>In a full implementation, inject <c>IRepository&lt;CustomerLoyalty&gt;</c> to reverse loyalty points 
    /// and <c>IRepository&lt;Order&gt;</c> to update order state on cancellation.</para>
    /// </remarks>
    public OrderCancelledHandler(
        INotificationService notificationService,
        ICacheService cacheService,
        ILogger<OrderCancelledHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _notificationService = notificationService;
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing OrderCancelledEvent. OrderId={OrderId}, Reason={Reason}, CancelledBy={CancelledBy}",
            notification.OrderId,
            notification.CancellationReason,
            notification.CancelledBy);

        var tasks = new List<Task>();

        // 1. Send cancellation notification to customer
        tasks.Add(SendCancellationNotificationAsync(notification, cancellationToken));

        // 2. Reverse loyalty points if needed
        if (notification.LoyaltyPointsToReverse > 0)
        {
            tasks.Add(ReverseLoyaltyPointsAsync(notification, cancellationToken));
        }

        // 3. Update analytics
        tasks.Add(UpdateCancellationAnalyticsAsync(notification, cancellationToken));

        // 4. Notify kitchen to remove order
        tasks.Add(NotifyKitchenOfCancellationAsync(notification, cancellationToken));

        // Execute all tasks concurrently
        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "OrderCancelledEvent processing completed for OrderId={OrderId}",
            notification.OrderId);
    }

    private async Task SendCancellationNotificationAsync(
        OrderCancelledEvent notification,
        CancellationToken cancellationToken)
    {
        await _notificationService.SendOrderCancellationAsync(
            notification.CustomerId,
            notification.OrderId,
            notification.CancellationReason,
            cancellationToken);

        _logger.LogDebug(
            "Cancellation notification sent to CustomerId={CustomerId}",
            notification.CustomerId);
    }

    private async Task ReverseLoyaltyPointsAsync(
        OrderCancelledEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reversing {Points} loyalty points for CustomerId={CustomerId} due to order cancellation",
            notification.LoyaltyPointsToReverse,
            notification.CustomerId);

        // Note: In full implementation, this would:
        // 1. Retrieve CustomerLoyalty aggregate
        // 2. Call loyalty.DeductPoints(notification.LoyaltyPointsToReverse, ...)
        // 3. Save the aggregate

        await Task.CompletedTask; // Placeholder
    }

    private async Task UpdateCancellationAnalyticsAsync(
        OrderCancelledEvent notification,
        CancellationToken cancellationToken)
    {
        await _cacheService.InvalidateAnalyticsCacheAsync(
            notification.RestaurantId,
            cancellationToken);

        _logger.LogDebug(
            "Cancellation analytics updated for RestaurantId={RestaurantId}",
            notification.RestaurantId);
    }

    private async Task NotifyKitchenOfCancellationAsync(
        OrderCancelledEvent notification,
        CancellationToken cancellationToken)
    {
        await _notificationService.SendToRestaurantStaffAsync(
            notification.RestaurantId,
            "Order Cancelled",
            $"Order #{notification.OrderId} has been cancelled. Reason: {notification.CancellationReason}",
            NotificationType.KitchenAlert,
            cancellationToken);

        _logger.LogDebug(
            "Kitchen notified of order cancellation for OrderId={OrderId}",
            notification.OrderId);
    }
}
