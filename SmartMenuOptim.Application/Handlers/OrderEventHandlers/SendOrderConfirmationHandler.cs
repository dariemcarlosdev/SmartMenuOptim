using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate.Events;

namespace SmartMenuOptim.Application.Handlers.OrderEventHandlers;

/// <summary>
/// Handles the <see cref="OrderPlacedEvent"/> to send confirmation to the customer.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Sends order confirmation notification to the customer via their preferred channel
/// (email, SMS, push notification, or in-app notification).</para>
/// 
/// <para><strong>Confirmation Details:</strong></para>
/// <list type="bullet">
///     <item><description>Order number and confirmation</description></item>
///     <item><description>Order total and items summary</description></item>
///     <item><description>Estimated preparation time</description></item>
///     <item><description>Restaurant contact information</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class SendOrderConfirmationHandler : ResilientEventHandlerBase<OrderPlacedEvent>, INotificationHandler<OrderPlacedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendOrderConfirmationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendOrderConfirmationHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service for sending customer confirmations.</param>
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
    public SendOrderConfirmationHandler(
        INotificationService notificationService,
        ILogger<SendOrderConfirmationHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending order confirmation to CustomerId={CustomerId} for OrderId={OrderId}",
            notification.CustomerId,
            notification.OrderId);

        await _notificationService.SendOrderConfirmationAsync(
            notification.CustomerId,
            notification.OrderId,
            notification.TotalAmount,
            cancellationToken);

        _logger.LogDebug(
            "Order confirmation sent successfully to CustomerId={CustomerId}",
            notification.CustomerId);
    }
}
