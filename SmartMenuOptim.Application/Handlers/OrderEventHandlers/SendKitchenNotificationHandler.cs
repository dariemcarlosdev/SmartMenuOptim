using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Events.OrderEvents;

namespace SmartMenuOptim.Application.Handlers.OrderEventHandlers;

/// <summary>
/// Handles the <see cref="OrderPlacedEvent"/> to send notifications to kitchen staff.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>This handler sends real-time notifications to the kitchen display system
/// when a new order is placed, ensuring timely order preparation.</para>
/// 
/// <para><strong>Notification Flow:</strong></para>
/// <list type="bullet">
///     <item><description>Order details sent to kitchen display</description></item>
///     <item><description>Special instructions highlighted</description></item>
///     <item><description>Priority ordering based on order time</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class SendKitchenNotificationHandler : ResilientEventHandlerBase<OrderPlacedEvent>, INotificationHandler<OrderPlacedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendKitchenNotificationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendKitchenNotificationHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service for sending kitchen display updates.</param>
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
    /// <para>This handler does not require repository injection as it only sends notifications without modifying aggregates.</para>
    /// </remarks>
    public SendKitchenNotificationHandler(
        INotificationService notificationService,
        ILogger<SendKitchenNotificationHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending kitchen notification for OrderId={OrderId}, RestaurantId={RestaurantId}, Items={ItemCount}",
            notification.OrderId,
            notification.RestaurantId,
            notification.ItemCount);

        await _notificationService.SendKitchenOrderAsync(
            notification.RestaurantId,
            notification.OrderId,
            notification.ItemCount,
            notification.SpecialInstructions,
            cancellationToken);

        _logger.LogDebug(
            "Kitchen notification sent successfully for OrderId={OrderId}",
            notification.OrderId);
    }
}
