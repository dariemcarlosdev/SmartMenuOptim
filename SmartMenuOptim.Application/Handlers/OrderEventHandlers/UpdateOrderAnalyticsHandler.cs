using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate.Events;

namespace SmartMenuOptim.Application.Handlers.OrderEventHandlers;

/// <summary>
/// Handles the <see cref="OrderPlacedEvent"/> to update real-time analytics.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Updates real-time analytics dashboards and metrics when orders are placed.
/// This enables live monitoring of restaurant performance.</para>
/// 
/// <para><strong>Metrics Updated:</strong></para>
/// <list type="bullet">
///     <item><description>Real-time order count</description></item>
///     <item><description>Revenue tracking</description></item>
///     <item><description>Average order value</description></item>
///     <item><description>Orders by type (dine-in, takeout, delivery)</description></item>
///     <item><description>Peak hours analysis</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class UpdateOrderAnalyticsHandler : ResilientEventHandlerBase<OrderPlacedEvent>, INotificationHandler<OrderPlacedEvent>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<UpdateOrderAnalyticsHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateOrderAnalyticsHandler"/> class.
    /// </summary>
    /// <param name="cacheService">The cache service for invalidating and updating analytics caches.</param>
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
    /// <para>This handler does not require repository injection as it only updates analytics caches without modifying aggregates.</para>
    /// </remarks>
    public UpdateOrderAnalyticsHandler(
        ICacheService cacheService,
        ILogger<UpdateOrderAnalyticsHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Updating analytics for OrderPlacedEvent. OrderId={OrderId}, RestaurantId={RestaurantId}",
            notification.OrderId,
            notification.RestaurantId);

        // Invalidate analytics cache to force refresh
        await _cacheService.InvalidateAnalyticsCacheAsync(
            notification.RestaurantId,
            cancellationToken);

        // Log analytics data point
        _logger.LogInformation(
            "Analytics updated: RestaurantId={RestaurantId}, OrderAmount={Amount}, ItemCount={Items}, Hour={Hour}",
            notification.RestaurantId,
            notification.TotalAmount,
            notification.ItemCount,
            notification.OccurredOn.Hour);
    }
}
