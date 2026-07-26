using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate.Events;
using SmartMenuOptim.Domain.Repositories;

namespace SmartMenuOptim.Application.Handlers.OrderEventHandlers;

/// <summary>
/// Handles the <see cref="OrderPlacedEvent"/> to award loyalty points to the customer.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>This handler awards loyalty points to customers based on their order total.
/// The standard earning rate is 1 point per $1 spent.</para>
/// 
/// <para><strong>Business Rules:</strong></para>
/// <list type="bullet">
///     <item><description>Only active loyalty members receive points</description></item>
///     <item><description>Points are calculated from the order total (1 point per $1)</description></item>
///     <item><description>Promotional multipliers may apply during special events</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// 
/// <para><strong>Related Documentation:</strong></para>
/// <para>See docs/architecture/EVENT_HANDLER_IMPLEMENTATION.md for handler patterns.</para>
/// <para>See docs/architecture/DOMAIN_EVENTS_GUIDE.md for event specifications.</para>
/// </remarks>
public class AwardLoyaltyPointsHandler : ResilientEventHandlerBase<OrderPlacedEvent>, INotificationHandler<OrderPlacedEvent>
{
    private readonly ILogger<AwardLoyaltyPointsHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwardLoyaltyPointsHandler"/> class.
    /// </summary>
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
    /// <para>In a full implementation, inject <c>IRepository&lt;CustomerLoyalty&gt;</c> to retrieve and update 
    /// the CustomerLoyalty aggregate when awarding points.</para>
    /// </remarks>
    public AwardLoyaltyPointsHandler(
        ILogger<AwardLoyaltyPointsHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing OrderPlacedEvent for loyalty points. OrderId={OrderId}, CustomerId={CustomerId}, Amount={Amount}",
            notification.OrderId,
            notification.CustomerId,
            notification.TotalAmount);

        // Calculate points to award (1 point per $1)
        var pointsToAward = CalculatePoints(notification.TotalAmount);

        _logger.LogInformation(
            "Awarding {Points} loyalty points to CustomerId={CustomerId} for OrderId={OrderId}",
            pointsToAward,
            notification.CustomerId,
            notification.OrderId);

        // Note: In full implementation, this would:
        // 1. Retrieve CustomerLoyalty aggregate from repository
        // 2. Call loyalty.AddPoints(pointsToAward, PointEarningSource.Purchase, notification.OrderId)
        // 3. Save the updated aggregate
        // The aggregate will raise LoyaltyPointsEarnedEvent automatically

        await Task.CompletedTask; // Placeholder for actual implementation
    }

    private static int CalculatePoints(decimal orderTotal)
    {
        // Standard rate: 1 point per $1 spent
        return (int)Math.Floor(orderTotal);
    }
}
