using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Events.LoyaltyEvents;

namespace SmartMenuOptim.Application.Handlers.LoyaltyEventHandlers;

/// <summary>
/// Handles the <see cref="LoyaltyTierChangedEvent"/> to activate benefits and send notifications.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Manages all actions when a customer's loyalty tier changes, including activating/deactivating
/// tier benefits, sending notifications, and updating marketing segments.</para>
/// 
/// <para><strong>Actions Performed:</strong></para>
/// <list type="bullet">
///     <item><description>Send tier change notification (congratulations or information)</description></item>
///     <item><description>Activate or deactivate tier-specific benefits</description></item>
///     <item><description>Update marketing segments</description></item>
///     <item><description>Log tier change for analytics</description></item>
/// </list>
/// 
/// <para><strong>Tier Benefits:</strong></para>
/// <list type="bullet">
///     <item><description><strong>Bronze:</strong> Base benefits, newsletter</description></item>
///     <item><description><strong>Silver:</strong> 10% discount, birthday reward</description></item>
///     <item><description><strong>Gold:</strong> 15% discount, birthday reward, priority seating</description></item>
///     <item><description><strong>Platinum:</strong> 20% discount, all benefits, VIP access, free delivery</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class LoyaltyTierChangedHandler : ResilientEventHandlerBase<LoyaltyTierChangedEvent>, INotificationHandler<LoyaltyTierChangedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<LoyaltyTierChangedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoyaltyTierChangedHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service for sending tier change notifications.</param>
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
    /// <para>In a full implementation, inject <c>IRepository&lt;CustomerLoyalty&gt;</c> to activate or deactivate 
    /// tier-specific benefits when the customer's tier changes.</para>
    /// </remarks>
    public LoyaltyTierChangedHandler(
        INotificationService notificationService,
        ILogger<LoyaltyTierChangedHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(LoyaltyTierChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing LoyaltyTierChangedEvent. CustomerId={CustomerId}, " +
            "TierChange={PreviousTier}->{NewTier}, IsUpgrade={IsUpgrade}",
            notification.CustomerId,
            notification.PreviousTier,
            notification.NewTier,
            notification.IsUpgrade);

        // 1. Send tier change notification
        await SendTierChangeNotificationAsync(notification, cancellationToken);

        // 2. Log tier change analytics
        LogTierChangeAnalytics(notification);

        // 3. Update customer profile/segments (for marketing)
        await UpdateCustomerSegmentAsync(notification, cancellationToken);
    }

    private async Task SendTierChangeNotificationAsync(
        LoyaltyTierChangedEvent notification,
        CancellationToken cancellationToken)
    {
        await _notificationService.SendLoyaltyTierChangedAsync(
            notification.CustomerId,
            notification.PreviousTier,
            notification.NewTier,
            notification.IsUpgrade,
            cancellationToken);

        _logger.LogDebug(
            "Tier change notification sent to CustomerId={CustomerId}",
            notification.CustomerId);
    }

    private void LogTierChangeAnalytics(LoyaltyTierChangedEvent notification)
    {
        _logger.LogInformation(
            "Tier Change Analytics: RestaurantId={RestaurantId}, CustomerId={CustomerId}, " +
            "TierChange={Previous}->{New}, Reason={Reason}, " +
            "DiscountChange={PrevDiscount}%->{NewDiscount}%, " +
            "PointBalance={Balance}",
            notification.RestaurantId,
            notification.CustomerId,
            notification.PreviousTier,
            notification.NewTier,
            notification.ChangeReason,
            notification.PreviousTierDiscountPercent,
            notification.NewTierDiscountPercent,
            notification.CurrentPointBalance);

        if (notification.BenefitsChanged.Count != 0)
        {
            var benefitsAction = notification.IsUpgrade ? "Gained" : "Lost";
            _logger.LogInformation(
                "Benefits {Action}: {Benefits}",
                benefitsAction,
                string.Join(", ", notification.BenefitsChanged));
        }
    }

    private async Task UpdateCustomerSegmentAsync(
        LoyaltyTierChangedEvent notification,
        CancellationToken cancellationToken)
    {
        // Update customer segments for targeted marketing
        // In a full implementation, this would update a marketing platform or CRM
        
        _logger.LogDebug(
            "Updating marketing segment for CustomerId={CustomerId} to tier {Tier}",
            notification.CustomerId,
            notification.NewTier);

        await Task.CompletedTask;
    }
}
