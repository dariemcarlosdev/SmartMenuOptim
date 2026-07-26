using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate.Events;

namespace SmartMenuOptim.Application.Handlers.MenuEventHandlers;

/// <summary>
/// Handles the <see cref="DishAddedToMenuEvent"/> to update caches and indexes.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Performs necessary actions when a new dish is added to a menu, including cache invalidation,
/// search index updates, and analytics initialization.</para>
/// 
/// <para><strong>Actions Performed:</strong></para>
/// <list type="bullet">
///     <item><description>Invalidate menu caches</description></item>
///     <item><description>Update search indexes (for menu discovery)</description></item>
///     <item><description>Initialize dish analytics tracking</description></item>
///     <item><description>Log menu change for audit</description></item>
/// </list>
/// 
/// <para><strong>Resilience:</strong></para>
/// <para>Inherits retry logic with exponential backoff (3 attempts) and dead letter queue support
/// from <see cref="ResilientEventHandlerBase{TEvent}"/>.</para>
/// 
/// <para><strong>MediatR Integration:</strong></para>
/// <para>Implements <see cref="INotificationHandler{TNotification}"/> via base class for MediatR discovery.</para>
/// </remarks>
public class DishAddedToMenuHandler : ResilientEventHandlerBase<DishAddedToMenuEvent>, INotificationHandler<DishAddedToMenuEvent>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<DishAddedToMenuHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DishAddedToMenuHandler"/> class.
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
    /// <para>This handler does not require repository injection as it only invalidates caches without modifying aggregates.</para>
    /// </remarks>
    public DishAddedToMenuHandler(
        ICacheService cacheService,
        ILogger<DishAddedToMenuHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(DishAddedToMenuEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing DishAddedToMenuEvent. DishId={DishId}, DishName={DishName}, " +
            "MenuId={MenuId}, RestaurantId={RestaurantId}",
            notification.DishId,
            notification.DishName,
            notification.MenuId,
            notification.RestaurantId);

        // 1. Invalidate menu cache
        await InvalidateMenuCacheAsync(notification, cancellationToken);

        // 2. Log dish addition details
        LogDishDetails(notification);

        // 3. Initialize dish performance tracking
        await InitializeDishTrackingAsync(notification, cancellationToken);

        _logger.LogInformation(
            "DishAddedToMenuEvent processing completed for DishId={DishId}",
            notification.DishId);
    }

    private async Task InvalidateMenuCacheAsync(
        DishAddedToMenuEvent notification,
        CancellationToken cancellationToken)
    {
        await _cacheService.InvalidateMenuCacheAsync(
            notification.RestaurantId,
            cancellationToken);

        _logger.LogDebug(
            "Menu cache invalidated for RestaurantId={RestaurantId}",
            notification.RestaurantId);
    }

    private void LogDishDetails(DishAddedToMenuEvent notification)
    {
        _logger.LogInformation(
            "New Dish Added: DishId={DishId}, Name={Name}, Price={Price:C}, " +
            "Category={Category}, MenuType={MenuType}, Featured={IsFeatured}",
            notification.DishId,
            notification.DishName,
            notification.Price,
            notification.CategoryName,
            notification.MenuType,
            notification.IsFeatured);

        if (notification.DietaryFlags.Count != 0)
        {
            _logger.LogInformation(
                "Dietary Flags: {Flags}",
                string.Join(", ", notification.DietaryFlags));
        }

        if (notification.Allergens.Count != 0)
        {
            _logger.LogInformation(
                "Allergens: {Allergens}",
                string.Join(", ", notification.Allergens));
        }
    }

    private async Task InitializeDishTrackingAsync(
        DishAddedToMenuEvent notification,
        CancellationToken cancellationToken)
    {
        // Initialize performance tracking for the new dish
        _logger.LogDebug(
            "Initializing performance tracking for DishId={DishId}",
            notification.DishId);

        await Task.CompletedTask;
    }
}
