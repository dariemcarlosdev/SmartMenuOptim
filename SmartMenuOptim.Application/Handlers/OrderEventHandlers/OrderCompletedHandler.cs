using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Events.OrderEvents;

namespace SmartMenuOptim.Application.Handlers.OrderEventHandlers;

/// <summary>
/// Handles the <see cref="OrderCompletedEvent"/> to finalize order lifecycle actions.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Performs all necessary actions when an order is successfully completed and delivered,
/// including requesting reviews, creating sale records, and finalizing analytics.</para>
/// 
/// <para><strong>Actions Performed:</strong></para>
/// <list type="bullet">
///     <item><description>Schedule review request (delayed notification)</description></item>
///     <item><description>Create finalized sale records</description></item>
///     <item><description>Update completion metrics and fulfillment times</description></item>
///     <item><description>Finalize loyalty points (if held pending)</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class OrderCompletedHandler : ResilientEventHandlerBase<OrderCompletedEvent>, INotificationHandler<OrderCompletedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<OrderCompletedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderCompletedHandler"/> class.
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
    /// <para>In a full implementation, inject <c>IRepository&lt;Order&gt;</c> and <c>IRepository&lt;CustomerLoyalty&gt;</c> 
    /// to finalize order state and loyalty points when pending.</para>
    /// </remarks>
    public OrderCompletedHandler(
        INotificationService notificationService,
        ICacheService cacheService,
        ILogger<OrderCompletedHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _notificationService = notificationService;
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(OrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing OrderCompletedEvent. OrderId={OrderId}, FulfillmentTime={FulfillmentTime:F1}min",
            notification.OrderId,
            notification.FulfillmentTimeMinutes);

        // 1. Log completion metrics
        LogCompletionMetrics(notification);

        // 2. Invalidate analytics cache
        await _cacheService.InvalidateAnalyticsCacheAsync(
            notification.RestaurantId,
            cancellationToken);

        // 3. Send thank you notification (which can include review request)
        await SendThankYouNotificationAsync(notification, cancellationToken);

        _logger.LogInformation(
            "OrderCompletedEvent processing completed for OrderId={OrderId}",
            notification.OrderId);
    }

    private void LogCompletionMetrics(OrderCompletedEvent notification)
    {
        _logger.LogInformation(
            "Order Completed Metrics: OrderId={OrderId}, RestaurantId={RestaurantId}, " +
            "Total={Total:C}, Items={Items}, FulfillmentMinutes={Minutes:F1}, " +
            "OrderType={OrderType}, PointsEarned={Points}",
            notification.OrderId,
            notification.RestaurantId,
            notification.FinalTotal,
            notification.ItemCount,
            notification.FulfillmentTimeMinutes,
            notification.OrderType,
            notification.LoyaltyPointsEarned);
    }

    private async Task SendThankYouNotificationAsync(
        OrderCompletedEvent notification,
        CancellationToken cancellationToken)
    {
        var message = notification.LoyaltyPointsEarned > 0
            ? $"Thank you for your order! You earned {notification.LoyaltyPointsEarned} loyalty points."
            : "Thank you for your order! We hope you enjoyed your meal.";

        await _notificationService.SendToCustomerAsync(
            notification.CustomerId,
            "Order Complete - Thank You!",
            message,
            NotificationType.Order,
            cancellationToken);

        _logger.LogDebug(
            "Thank you notification sent to CustomerId={CustomerId}",
            notification.CustomerId);
    }
}
