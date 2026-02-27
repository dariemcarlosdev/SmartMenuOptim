using SmartMenuOptim.Domain.ValueObjects;

using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Domain.Events.OrderEvents;

/// <summary>
/// Domain event raised when a new order is successfully placed in the system.
/// This event triggers downstream processes such as loyalty point accrual, 
/// kitchen notifications, and analytics updates.
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong></para>
/// <para>This event is raised by the Order aggregate when a customer successfully places an order,
/// after all validation rules pass and the order transitions to a confirmed state.</para>
/// 
/// <para><strong>Typical Event Handlers:</strong></para>
/// <list type="bullet">
///     <item><description><strong>LoyaltyPointsHandler:</strong> Awards points to the customer based on order total</description></item>
///     <item><description><strong>KitchenNotificationHandler:</strong> Sends order to kitchen display system</description></item>
///     <item><description><strong>AnalyticsHandler:</strong> Updates real-time sales dashboards</description></item>
///     <item><description><strong>InventoryHandler:</strong> Decrements ingredient stock levels</description></item>
///     <item><description><strong>CustomerNotificationHandler:</strong> Sends order confirmation email/SMS</description></item>
/// </list>
/// 
/// <para><strong>Multi-Tenant Context:</strong></para>
/// <para>The RestaurantId property ensures event handlers can correctly scope their operations
/// to the appropriate tenant, maintaining data isolation in a multi-tenant environment.</para>
/// 
/// <para><strong>Idempotency:</strong></para>
/// <para>Event handlers should use the OrderId and EventId to ensure idempotent processing,
/// preventing duplicate loyalty points or notifications if the event is replayed.</para>
/// </remarks>
/// <example>
/// <code>
/// // Raising the event from Order aggregate
/// public void Place()
/// {
///     if (Status != OrderStatus.Draft)
///         throw new DomainException("Order already placed");
///     
///     Status = OrderStatus.Pending;
///     AddDomainEvent(new OrderPlacedEvent(
///         orderId: Id,
///         restaurantId: RestaurantId,
///         customerId: CustomerId,
///         totalAmount: TotalAmount,
///         itemCount: Items.Count
///     ));
/// }
/// </code>
/// </example>
public sealed class OrderPlacedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the unique identifier of the order that was placed.
    /// </summary>
    public int OrderId { get; init; }

    /// <summary>
    /// Gets the restaurant (tenant) identifier where the order was placed.
    /// </summary>
    public int RestaurantId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the customer who placed the order.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Gets the total monetary amount of the order.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Gets the currency code for the order total (e.g., "USD", "EUR").
    /// </summary>
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>
    /// Gets the number of items in the order.
    /// </summary>
    public int ItemCount { get; init; }

    /// <summary>
    /// Gets any special instructions provided with the order.
    /// </summary>
    public string? SpecialInstructions { get; init; }

    /// <summary>
    /// Gets the order type (e.g., "DineIn", "TakeOut", "Delivery").
    /// </summary>
    public string? OrderType { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderPlacedEvent"/> class.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="restaurantId">The restaurant (tenant) identifier.</param>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="totalAmount">The order total amount.</param>
    /// <param name="itemCount">The number of items in the order.</param>
    /// <param name="currencyCode">The currency code (default: USD).</param>
    /// <param name="specialInstructions">Optional special instructions.</param>
    /// <param name="orderType">Optional order type.</param>
    public OrderPlacedEvent(
        int orderId,
        int restaurantId,
        int customerId,
        decimal totalAmount,
        int itemCount,
        string currencyCode = "USD",
        string? specialInstructions = null,
        string? orderType = null)
    {
        OrderId = orderId;
        RestaurantId = restaurantId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        CurrencyCode = currencyCode;
        ItemCount = itemCount;
        SpecialInstructions = specialInstructions;
        OrderType = orderType;
    }

    /// <summary>
    /// Private parameterless constructor for serialization support.
    /// </summary>
    private OrderPlacedEvent() { }
}
