using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate.Events;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Application.Handlers.SaleEventHandlers;

/// <summary>
/// Handles the <see cref="SaleRecordedEvent"/> to persist sale records and update real-time analytics.
/// </summary>
/// <remarks>
/// <para><strong>Handler Responsibility:</strong></para>
/// <para>Creates <see cref="SaleRecord"/> entities in the database for each sale event,
/// then updates real-time analytics when individual sales are recorded, enabling live
/// dashboard updates and performance tracking.</para>
/// 
/// <para><strong>Actions Performed:</strong></para>
/// <list type="bullet">
///     <item><description>Persist a new <see cref="SaleRecord"/> entity to the database</description></item>
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
    private readonly IUnityOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SaleRecordedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRecordedHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for persisting sale records.</param>
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
    public SaleRecordedHandler(
        IUnityOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<SaleRecordedHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(SaleRecordedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Processing SaleRecordedEvent. OrderId={OrderId}, DishId={DishId}, Quantity={Quantity}",
            notification.OrderId,
            notification.DishId,
            notification.QuantitySold);

        // 1. Persist the sale record to the database
        await PersistSaleRecordAsync(notification, cancellationToken);

        // 2. Log sale analytics
        LogSaleAnalytics(notification);

        // 3. Invalidate analytics cache
        await _cacheService.InvalidateAnalyticsCacheAsync(
            notification.RestaurantId,
            cancellationToken);

        _logger.LogDebug(
            "SaleRecordedEvent processing completed for OrderId={OrderId}, DishId={DishId}",
            notification.OrderId,
            notification.DishId);
    }

    /// <summary>
    /// Creates and persists a <see cref="SaleRecord"/> entity from the domain event data.
    /// </summary>
    private async Task PersistSaleRecordAsync(SaleRecordedEvent notification, CancellationToken cancellationToken)
    {
        var saleAmount = new Money(notification.TotalAmount, notification.CurrencyCode);

        var saleRecord = new SaleRecord(
            restaurantId: notification.RestaurantId,
            dishId: notification.DishId,
            saleAmount: saleAmount,
            quantitySold: notification.QuantitySold);

        await _unitOfWork.SaleRecords.AddAsync(saleRecord);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "SaleRecord persisted: DishId={DishId}, Quantity={Quantity}, Amount={Amount:C}, RestaurantId={RestaurantId}",
            notification.DishId,
            notification.QuantitySold,
            notification.TotalAmount,
            notification.RestaurantId);
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
                "Discount Applied: OrderId={OrderId}, DishId={DishId}, DiscountAmount={Discount:C}, NetAmount={Net:C}",
                notification.OrderId,
                notification.DishId,
                notification.DiscountAmount,
                notification.NetAmount);
        }
    }
}
