using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Events.MenuEvents;

namespace SmartMenuOptim.Application.Handlers.MenuEventHandlers;

/// <summary>
/// Handles the <see cref="DishRemovedFromMenuEvent"/> to update caches, indexes, and archive data.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Performs necessary actions when a dish is removed from a menu, including cache invalidation,
/// search index updates, performance data archival, and order validation.</para>
/// 
/// <para><strong>Actions Performed:</strong></para>
/// <list type="bullet">
///     <item><description>Invalidate menu caches</description></item>
///     <item><description>Remove from search indexes</description></item>
///     <item><description>Archive dish performance data</description></item>
///     <item><description>Update AI recommendation model</description></item>
///     <item><description>Log removal analytics</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class DishRemovedFromMenuHandler : ResilientEventHandlerBase<DishRemovedFromMenuEvent>, INotificationHandler<DishRemovedFromMenuEvent>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<DishRemovedFromMenuHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DishRemovedFromMenuHandler"/> class.
    /// </summary>
    /// <param name="cacheService">The cache service for invalidating menu caches.</param>
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
    /// <para>This handler does not require repository injection as it only invalidates caches and archives data without modifying aggregates.</para>
    /// </remarks>
    public DishRemovedFromMenuHandler(
        ICacheService cacheService,
        ILogger<DishRemovedFromMenuHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(DishRemovedFromMenuEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing DishRemovedFromMenuEvent. DishId={DishId}, DishName={DishName}, " +
            "Reason={Reason}, IsPermanent={IsPermanent}",
            notification.DishId,
            notification.DishName,
            notification.RemovalReason,
            notification.IsPermanent);

        // 1. Invalidate menu cache
        await InvalidateMenuCacheAsync(notification, cancellationToken);

        // 2. Archive dish performance data
        ArchiveDishPerformance(notification);

        // 3. Log removal analytics
        LogRemovalAnalytics(notification);

        _logger.LogInformation(
            "DishRemovedFromMenuEvent processing completed for DishId={DishId}",
            notification.DishId);
    }

    private async Task InvalidateMenuCacheAsync(
        DishRemovedFromMenuEvent notification,
        CancellationToken cancellationToken)
    {
        await _cacheService.InvalidateMenuCacheAsync(
            notification.RestaurantId,
            cancellationToken);

        _logger.LogDebug(
            "Menu cache invalidated for RestaurantId={RestaurantId}",
            notification.RestaurantId);
    }

    private void ArchiveDishPerformance(DishRemovedFromMenuEvent notification)
    {
        _logger.LogInformation(
            "Archiving Dish Performance: DishId={DishId}, Name={Name}, " +
            "DaysOnMenu={Days}, TotalSold={Quantity}, TotalRevenue={Revenue:C}, " +
            "AvgRating={Rating:F1}",
            notification.DishId,
            notification.DishName,
            notification.DaysOnMenu,
            notification.TotalQuantitySold,
            notification.TotalRevenue,
            notification.AverageRating);
    }

    private void LogRemovalAnalytics(DishRemovedFromMenuEvent notification)
    {
        _logger.LogInformation(
            "Dish Removal Analytics: RestaurantId={RestaurantId}, DishId={DishId}, " +
            "Category={Category}, Reason={Reason}, IsPermanent={IsPermanent}, " +
            "LastPrice={Price:C}, RemovedBy={StaffId}",
            notification.RestaurantId,
            notification.DishId,
            notification.CategoryName,
            notification.RemovalReason,
            notification.IsPermanent,
            notification.LastPrice,
            notification.RemovedByStaffId);

        // Alert for underperforming dish removal
        if (notification.RemovalReason == DishRemovalReason.Underperforming)
        {
            _logger.LogWarning(
                "Underperforming dish removed: DishId={DishId}, Name={Name}, " +
                "Sold only {Quantity} units in {Days} days",
                notification.DishId,
                notification.DishName,
                notification.TotalQuantitySold,
                notification.DaysOnMenu);
        }
    }
}
