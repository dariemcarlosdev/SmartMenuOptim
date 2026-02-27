using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Domain.Events.OrderEvents;

/// <summary>
/// Domain event raised when an order is successfully completed and fulfilled.
/// This represents the final state of a successful order lifecycle.
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong></para>
/// <para>This event is raised by the Order aggregate when the order reaches its final 
/// successful state (delivered to customer or picked up for takeout orders).</para>
/// 
/// <para><strong>Typical Event Handlers:</strong></para>
/// <list type="bullet">
///     <item><description><strong>LoyaltyHandler:</strong> Finalizes loyalty points (converts pending to confirmed)</description></item>
///     <item><description><strong>ReviewRequestHandler:</strong> Triggers request for customer review after delay</description></item>
///     <item><description><strong>SalesRecordHandler:</strong> Creates finalized sale records for accounting</description></item>
///     <item><description><strong>AnalyticsHandler:</strong> Updates completion metrics and average fulfillment times</description></item>
///     <item><description><strong>CustomerProfileHandler:</strong> Updates customer's order history and preferences</description></item>
///     <item><description><strong>ReportingHandler:</strong> Updates daily/weekly sales reports</description></item>
/// </list>
/// 
/// <para><strong>Order Completion Types:</strong></para>
/// <list type="bullet">
///     <item><description><strong>DineIn:</strong> Customer finished dining and paid</description></item>
///     <item><description><strong>TakeOut:</strong> Customer picked up the order</description></item>
///     <item><description><strong>Delivery:</strong> Order delivered to customer's location</description></item>
/// </list>
/// 
/// <para><strong>Metrics Captured:</strong></para>
/// <para>This event includes fulfillment time calculation to track operational efficiency
/// and support analytics for restaurant performance optimization.</para>
/// </remarks>
public sealed class OrderCompletedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the unique identifier of the completed order.
    /// </summary>
    public int OrderId { get; init; }

    /// <summary>
    /// Gets the restaurant (tenant) identifier.
    /// </summary>
    public int RestaurantId { get; init; }

    /// <summary>
    /// Gets the customer identifier.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Gets the final order total amount.
    /// </summary>
    public decimal FinalTotal { get; init; }

    /// <summary>
    /// Gets the currency code for the order total.
    /// </summary>
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>
    /// Gets the number of items in the completed order.
    /// </summary>
    public int ItemCount { get; init; }

    /// <summary>
    /// Gets the timestamp when the order was originally placed.
    /// </summary>
    public DateTime OrderPlacedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the order was completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Gets the total fulfillment time in minutes.
    /// </summary>
    public double FulfillmentTimeMinutes => (CompletedAt - OrderPlacedAt).TotalMinutes;

    /// <summary>
    /// Gets the order type (DineIn, TakeOut, Delivery).
    /// </summary>
    public string OrderType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the staff member ID who completed the order.
    /// </summary>
    public int? CompletedByStaffId { get; init; }

    /// <summary>
    /// Gets the tip amount if applicable.
    /// </summary>
    public decimal TipAmount { get; init; }

    /// <summary>
    /// Gets the payment method used.
    /// </summary>
    public string? PaymentMethod { get; init; }

    /// <summary>
    /// Gets the loyalty points earned from this order.
    /// </summary>
    public int LoyaltyPointsEarned { get; init; }

    /// <summary>
    /// Gets any promotions that were applied to this order.
    /// </summary>
    public List<string> AppliedPromotions { get; init; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderCompletedEvent"/> class.
    /// </summary>
    public OrderCompletedEvent(
        int orderId,
        int restaurantId,
        int customerId,
        decimal finalTotal,
        int itemCount,
        DateTime orderPlacedAt,
        DateTime completedAt,
        string orderType,
        string currencyCode = "USD",
        int? completedByStaffId = null,
        decimal tipAmount = 0,
        string? paymentMethod = null,
        int loyaltyPointsEarned = 0,
        List<string>? appliedPromotions = null)
    {
        OrderId = orderId;
        RestaurantId = restaurantId;
        CustomerId = customerId;
        FinalTotal = finalTotal;
        CurrencyCode = currencyCode;
        ItemCount = itemCount;
        OrderPlacedAt = orderPlacedAt;
        CompletedAt = completedAt;
        OrderType = orderType;
        CompletedByStaffId = completedByStaffId;
        TipAmount = tipAmount;
        PaymentMethod = paymentMethod;
        LoyaltyPointsEarned = loyaltyPointsEarned;
        AppliedPromotions = appliedPromotions ?? new List<string>();
    }

    /// <summary>
    /// Private parameterless constructor for serialization support.
    /// </summary>
    private OrderCompletedEvent() { }
}
