using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Events.LoyaltyEvents;

namespace SmartMenuOptim.Application.Handlers.LoyaltyEventHandlers;

/// <summary>
/// Handles the <see cref="LoyaltyPointsEarnedEvent"/> to send notifications and check for tier upgrades.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Notifies customers when they earn loyalty points and checks if the new balance
/// qualifies them for a tier upgrade.</para>
/// 
/// <para><strong>Actions Performed:</strong></para>
/// <list type="bullet">
///     <item><description>Send points earned notification to customer</description></item>
///     <item><description>Log loyalty analytics</description></item>
///     <item><description>Check for milestone achievements</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class LoyaltyPointsEarnedHandler : ResilientEventHandlerBase<LoyaltyPointsEarnedEvent>, INotificationHandler<LoyaltyPointsEarnedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<LoyaltyPointsEarnedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoyaltyPointsEarnedHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service for sending points earned notifications.</param>
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
    public LoyaltyPointsEarnedHandler(
        INotificationService notificationService,
        ILogger<LoyaltyPointsEarnedHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(LoyaltyPointsEarnedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing LoyaltyPointsEarnedEvent. CustomerId={CustomerId}, PointsEarned={Points}, " +
            "NewBalance={Balance}, Source={Source}",
            notification.CustomerId,
            notification.PointsEarned,
            notification.NewTotalBalance,
            notification.EarningSource);

        // 1. Send notification to customer
        await SendPointsEarnedNotificationAsync(notification, cancellationToken);

        // 2. Log analytics
        LogLoyaltyAnalytics(notification);

        // 3. Check for milestone achievements
        await CheckMilestonesAsync(notification, cancellationToken);
    }

    private async Task SendPointsEarnedNotificationAsync(
        LoyaltyPointsEarnedEvent notification,
        CancellationToken cancellationToken)
    {
        await _notificationService.SendLoyaltyPointsEarnedAsync(
            notification.CustomerId,
            notification.PointsEarned,
            notification.NewTotalBalance,
            cancellationToken);

        _logger.LogDebug(
            "Points earned notification sent to CustomerId={CustomerId}",
            notification.CustomerId);
    }

    private void LogLoyaltyAnalytics(LoyaltyPointsEarnedEvent notification)
    {
        _logger.LogInformation(
            "Loyalty Analytics: RestaurantId={RestaurantId}, CustomerId={CustomerId}, " +
            "PointsEarned={Points}, Source={Source}, Multiplier={Multiplier}, " +
            "RelatedOrderId={OrderId}",
            notification.RestaurantId,
            notification.CustomerId,
            notification.PointsEarned,
            notification.EarningSource,
            notification.PointsMultiplier,
            notification.RelatedOrderId);
    }

    private async Task CheckMilestonesAsync(
        LoyaltyPointsEarnedEvent notification,
        CancellationToken cancellationToken)
    {
        // Check for milestone achievements
        var milestones = new[] { 100, 250, 500, 1000, 2500, 5000, 10000 };

        foreach (var milestone in milestones)
        {
            if (notification.PreviousBalance < milestone && 
                notification.NewTotalBalance >= milestone)
            {
                _logger.LogInformation(
                    "Milestone reached! CustomerId={CustomerId} reached {Milestone} points",
                    notification.CustomerId,
                    milestone);

                await _notificationService.SendToCustomerAsync(
                    notification.CustomerId,
                    "🎉 Milestone Reached!",
                    $"Congratulations! You've reached {milestone:N0} loyalty points!",
                    NotificationType.Loyalty,
                    cancellationToken);

                break; // Only notify for the highest milestone crossed
            }
        }

        await Task.CompletedTask;
    }
}
