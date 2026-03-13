using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate.Events;

namespace SmartMenuOptim.Application.Handlers.SaleEventHandlers;

/// <summary>
/// Handles the <see cref="DailySalesSummarizedEvent"/> to generate reports and insights.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Processes daily sales summaries to generate reports, update dashboards,
/// and trigger AI-powered insights for restaurant optimization.</para>
/// 
/// <para><strong>Actions Performed:</strong></para>
/// <list type="bullet">
///     <item><description>Generate daily sales report</description></item>
///     <item><description>Compare against targets and historical data</description></item>
///     <item><description>Identify underperforming dishes</description></item>
///     <item><description>Send management alerts for significant events</description></item>
///     <item><description>Trigger AI insights generation</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class DailySalesSummarizedHandler : ResilientEventHandlerBase<DailySalesSummarizedEvent>, INotificationHandler<DailySalesSummarizedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DailySalesSummarizedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DailySalesSummarizedHandler"/> class.
    /// </summary>
    /// <param name="notificationService">The notification service for sending management alerts.</param>
    /// <param name="cacheService">The cache service for updating analytics caches.</param>
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
    /// <para>This handler does not require repository injection as it only generates reports and sends notifications without modifying aggregates.</para>
    /// </remarks>
    public DailySalesSummarizedHandler(
        INotificationService notificationService,
        ICacheService cacheService,
        ILogger<DailySalesSummarizedHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _notificationService = notificationService;
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(DailySalesSummarizedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing DailySalesSummarizedEvent. RestaurantId={RestaurantId}, Date={Date}, " +
            "TotalRevenue={Revenue:C}, TotalOrders={Orders}",
            notification.RestaurantId,
            notification.SummaryDate,
            notification.TotalRevenue,
            notification.TotalOrders);

        // 1. Log comprehensive daily summary
        LogDailySummary(notification);

        // 2. Check for alerts
        await CheckAndSendAlertsAsync(notification, cancellationToken);

        // 3. Invalidate analytics cache for fresh data
        await _cacheService.InvalidateAnalyticsCacheAsync(
            notification.RestaurantId,
            cancellationToken);

        _logger.LogInformation(
            "DailySalesSummarizedEvent processing completed for RestaurantId={RestaurantId}, Date={Date}",
            notification.RestaurantId,
            notification.SummaryDate);
    }

    private void LogDailySummary(DailySalesSummarizedEvent notification)
    {
        _logger.LogInformation(
            "Daily Sales Summary: RestaurantId={RestaurantId}, Restaurant={Name}, Date={Date}, " +
            "DayOfWeek={DayOfWeek}",
            notification.RestaurantId,
            notification.RestaurantName,
            notification.SummaryDate,
            notification.DayOfWeek);

        _logger.LogInformation(
            "Revenue Metrics: Total={Revenue:C}, Orders={Orders}, Items={Items}, " +
            "AvgOrderValue={AOV:C}, Discounts={Discounts:C}, Tips={Tips:C}",
            notification.TotalRevenue,
            notification.TotalOrders,
            notification.TotalItemsSold,
            notification.AverageOrderValue,
            notification.TotalDiscounts,
            notification.TotalTips);

        _logger.LogInformation(
            "Customer Metrics: UniqueCustomers={Customers}, NewCustomers={New}, " +
            "LoyaltyPointsAwarded={Points}",
            notification.UniqueCustomers,
            notification.NewCustomersAcquired,
            notification.TotalLoyaltyPointsAwarded);

        _logger.LogInformation(
            "Top Performer: Dish={Dish}, Quantity={Quantity}, Revenue={Revenue:C}",
            notification.TopSellingDish,
            notification.TopSellingDishQuantity,
            notification.TopSellingDishRevenue);

        _logger.LogInformation(
            "Peak Hour: Hour={Hour}, Orders={Orders}",
            notification.PeakHour,
            notification.PeakHourOrders);

        if (notification.PercentChangeFromPreviousDay.HasValue)
        {
            var direction = notification.PercentChangeFromPreviousDay > 0 ? "up" : "down";
            _logger.LogInformation(
                "Day-over-Day: Revenue is {Direction} {Percent:F1}% from yesterday ({PrevRevenue:C})",
                direction,
                Math.Abs(notification.PercentChangeFromPreviousDay.Value),
                notification.PreviousDayRevenue);
        }

        if (notification.PercentChangeFromLastWeek.HasValue)
        {
            var direction = notification.PercentChangeFromLastWeek > 0 ? "up" : "down";
            _logger.LogInformation(
                "Week-over-Week: Revenue is {Direction} {Percent:F1}% from same day last week ({PrevRevenue:C})",
                direction,
                Math.Abs(notification.PercentChangeFromLastWeek.Value),
                notification.SameDayLastWeekRevenue);
        }

        if (notification.TargetAchievementPercent.HasValue)
        {
            _logger.LogInformation(
                "Target Achievement: {Percent:F1}% of daily target ({Target:C})",
                notification.TargetAchievementPercent.Value,
                notification.DailyTarget);
        }

        if (notification.CancelledOrders > 0)
        {
            _logger.LogWarning(
                "Cancellations: {Count} orders cancelled, revenue lost: {Lost:C}",
                notification.CancelledOrders,
                notification.CancellationRevenueLost);
        }

        if (notification.UnderperformingDishes.Count != 0)
        {
            _logger.LogWarning(
                "Underperforming Dishes: {Dishes}",
                string.Join(", ", notification.UnderperformingDishes));
        }
    }

    private async Task CheckAndSendAlertsAsync(
        DailySalesSummarizedEvent notification,
        CancellationToken cancellationToken)
    {
        // Check for significant revenue drops
        if (notification.PercentChangeFromLastWeek.HasValue &&
            notification.PercentChangeFromLastWeek.Value < -20)
        {
            await SendAlertAsync(
                notification.RestaurantId,
                "⚠️ Revenue Alert",
                $"Revenue is down {Math.Abs(notification.PercentChangeFromLastWeek.Value):F1}% " +
                $"compared to same day last week. Consider reviewing promotions or operations.",
                cancellationToken);
        }

        // Check for target miss
        if (notification.TargetAchievementPercent.HasValue &&
            notification.TargetAchievementPercent.Value < 80)
        {
            await SendAlertAsync(
                notification.RestaurantId,
                "📊 Target Alert",
                $"Daily target only {notification.TargetAchievementPercent.Value:F0}% achieved. " +
                $"Target: {notification.DailyTarget:C}, Actual: {notification.TotalRevenue:C}",
                cancellationToken);
        }

        // Check for high cancellation rate
        if (notification.TotalOrders > 0)
        {
            var cancellationRate = (double)notification.CancelledOrders / notification.TotalOrders * 100;
            if (cancellationRate > 10)
            {
                await SendAlertAsync(
                    notification.RestaurantId,
                    "🚫 Cancellation Alert",
                    $"High cancellation rate: {cancellationRate:F1}% ({notification.CancelledOrders} orders cancelled)",
                    cancellationToken);
            }
        }

        // Alert for underperforming dishes
        if (notification.UnderperformingDishes.Count >= 3)
        {
            await SendAlertAsync(
                notification.RestaurantId,
                "📉 Menu Performance Alert",
                $"{notification.UnderperformingDishes.Count} dishes underperforming. " +
                $"Consider menu optimization: {string.Join(", ", notification.UnderperformingDishes.Take(3))}",
                cancellationToken);
        }
    }

    private async Task SendAlertAsync(
        int restaurantId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        await _notificationService.SendToRestaurantStaffAsync(
            restaurantId,
            title,
            message,
            NotificationType.SystemAlert,
            cancellationToken);

        _logger.LogInformation(
            "Alert sent to RestaurantId={RestaurantId}: {Title}",
            restaurantId,
            title);
    }
}
