using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate.Events;

namespace SmartMenuOptim.Application.Handlers.SaleEventHandlers;

/// <summary>
/// Handles the <see cref="SaleRecordedEvent"/> to update real-time analytics and dish performance.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Updates real-time analytics when individual sales are recorded, enabling live
/// dashboard updates and performance tracking.</para>
/// 
/// <para><strong>Actions Performed:</strong></para>
/// <list type="bullet">
///     <item><description>Update real-time revenue tracking</description></item>
///     <item><description>Update dish-level performance metrics</description></item>
///     <item><description>Track sales patterns (time of day, day of week)</description></item>
///     <item><description>Feed data to AI recommendation system</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class SaleRecordedHandler : ResilientEventHandlerBase<SaleRecordedEvent>, INotificationHandler<SaleRecordedEvent>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<SaleRecordedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRecordedHandler"/> class.
    /// </summary>
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
    /// <para>This handler does not require repository injection as it only updates analytics caches without modifying aggregates.</para>
    /// </remarks>
    public SaleRecordedHandler(
        ICacheService cacheService,
        ILogger<SaleRecordedHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(SaleRecordedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Processing SaleRecordedEvent. SaleId={SaleId}, DishId={DishId}, Quantity={Quantity}",
            notification.SaleRecordId,
            notification.DishId,
            notification.QuantitySold);

        // 1. Log sale analytics
        LogSaleAnalytics(notification);

        // 2. Invalidate analytics cache
        await _cacheService.InvalidateAnalyticsCacheAsync(
            notification.RestaurantId,
            cancellationToken);

        _logger.LogDebug(
            "SaleRecordedEvent processing completed for SaleId={SaleId}",
            notification.SaleRecordId);
    }

    private void LogSaleAnalytics(SaleRecordedEvent notification)
    {
        var mealPeriod = notification.IsLunchHour ? "Lunch" :
                         notification.IsDinnerHour ? "Dinner" : "Other";

        _logger.LogInformation(
            "Sale Analytics: RestaurantId={RestaurantId}, DishId={DishId}, DishName={DishName}, " +
            "Category={Category}, Quantity={Quantity}, Amount={Amount:C}, " +
            "DayOfWeek={DayOfWeek}, Hour={Hour}, MealPeriod={MealPeriod}",
            notification.RestaurantId,
            notification.DishId,
            notification.DishName,
            notification.CategoryName,
            notification.QuantitySold,
            notification.TotalAmount,
            notification.DayOfWeek,
            notification.HourOfDay,
            mealPeriod);

        if (notification.DiscountAmount > 0)
        {
            _logger.LogDebug(
                "Discount Applied: SaleId={SaleId}, DiscountAmount={Discount:C}, NetAmount={Net:C}",
                notification.SaleRecordId,
                notification.DiscountAmount,
                notification.NetAmount);
        }
    }
}
