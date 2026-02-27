using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Domain.Events.SaleEvents;

/// <summary>
/// Domain event raised when a new sale transaction is recorded in the system.
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong></para>
/// <para>This event is raised when a sale is finalized, typically when an order is completed
/// and payment is confirmed. It captures the transactional details for analytics and reporting.</para>
/// 
/// <para><strong>Typical Event Handlers:</strong></para>
/// <list type="bullet">
///     <item><description><strong>AnalyticsHandler:</strong> Updates real-time sales dashboards and KPIs</description></item>
///     <item><description><strong>InventoryHandler:</strong> Decrements inventory based on items sold</description></item>
///     <item><description><strong>RevenueHandler:</strong> Updates daily/weekly/monthly revenue tracking</description></item>
///     <item><description><strong>PerformanceHandler:</strong> Updates dish performance metrics</description></item>
///     <item><description><strong>TaxHandler:</strong> Logs sale for tax calculation and reporting</description></item>
///     <item><description><strong>ForecastingHandler:</strong> Feeds data to demand forecasting models</description></item>
///     <item><description><strong>AIOptimizationHandler:</strong> Provides data for menu optimization AI</description></item>
/// </list>
/// 
/// <para><strong>Data Analytics Value:</strong></para>
/// <para>This event is fundamental to the SmartMenuOptimizer's AI-powered analytics features,
/// providing the raw data needed for:</para>
/// <list type="bullet">
///     <item><description>Dish performance analysis</description></item>
///     <item><description>Sales trend identification</description></item>
///     <item><description>Revenue optimization recommendations</description></item>
///     <item><description>Underperforming dish detection</description></item>
///     <item><description>Peak hours analysis</description></item>
///     <item><description>Customer preference learning</description></item>
/// </list>
/// </remarks>
public sealed class SaleRecordedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the unique identifier of the sale record.
    /// </summary>
    public int SaleRecordId { get; init; }

    /// <summary>
    /// Gets the restaurant (tenant) identifier.
    /// </summary>
    public int RestaurantId { get; init; }

    /// <summary>
    /// Gets the order identifier associated with this sale.
    /// </summary>
    public int OrderId { get; init; }

    /// <summary>
    /// Gets the dish identifier that was sold.
    /// </summary>
    public int DishId { get; init; }

    /// <summary>
    /// Gets the name of the dish that was sold.
    /// </summary>
    public string DishName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the category of the dish.
    /// </summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the quantity sold.
    /// </summary>
    public int QuantitySold { get; init; }

    /// <summary>
    /// Gets the unit price at the time of sale.
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Gets the total sale amount (quantity × unit price).
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Gets the currency code.
    /// </summary>
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>
    /// Gets the date and time of the sale.
    /// </summary>
    public DateTime SaleDateTime { get; init; }

    /// <summary>
    /// Gets the day of week of the sale.
    /// </summary>
    public DayOfWeek DayOfWeek => SaleDateTime.DayOfWeek;

    /// <summary>
    /// Gets the hour of day of the sale (0-23).
    /// </summary>
    public int HourOfDay => SaleDateTime.Hour;

    /// <summary>
    /// Gets whether this sale occurred during lunch hours (11 AM - 2 PM).
    /// </summary>
    public bool IsLunchHour => HourOfDay >= 11 && HourOfDay <= 14;

    /// <summary>
    /// Gets whether this sale occurred during dinner hours (5 PM - 9 PM).
    /// </summary>
    public bool IsDinnerHour => HourOfDay >= 17 && HourOfDay <= 21;

    /// <summary>
    /// Gets any discount amount applied to this sale.
    /// </summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>
    /// Gets the net amount after discount.
    /// </summary>
    public decimal NetAmount => TotalAmount - DiscountAmount;

    /// <summary>
    /// Gets the customer identifier (if available).
    /// </summary>
    public int? CustomerId { get; init; }

    /// <summary>
    /// Gets the staff member who processed the sale.
    /// </summary>
    public int? ProcessedByStaffId { get; init; }

    /// <summary>
    /// Gets the order type (DineIn, TakeOut, Delivery).
    /// </summary>
    public string? OrderType { get; init; }

    /// <summary>
    /// Gets any promotions applied to this sale.
    /// </summary>
    public List<string> AppliedPromotions { get; init; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRecordedEvent"/> class.
    /// </summary>
    public SaleRecordedEvent(
        int saleRecordId,
        int restaurantId,
        int orderId,
        int dishId,
        string dishName,
        string categoryName,
        int quantitySold,
        decimal unitPrice,
        decimal totalAmount,
        DateTime saleDateTime,
        string currencyCode = "USD",
        decimal discountAmount = 0,
        int? customerId = null,
        int? processedByStaffId = null,
        string? orderType = null,
        List<string>? appliedPromotions = null)
    {
        SaleRecordId = saleRecordId;
        RestaurantId = restaurantId;
        OrderId = orderId;
        DishId = dishId;
        DishName = dishName;
        CategoryName = categoryName;
        QuantitySold = quantitySold;
        UnitPrice = unitPrice;
        TotalAmount = totalAmount;
        SaleDateTime = saleDateTime;
        CurrencyCode = currencyCode;
        DiscountAmount = discountAmount;
        CustomerId = customerId;
        ProcessedByStaffId = processedByStaffId;
        OrderType = orderType;
        AppliedPromotions = appliedPromotions ?? new List<string>();
    }

    /// <summary>
    /// Private parameterless constructor for serialization support.
    /// </summary>
    private SaleRecordedEvent() { }
}
