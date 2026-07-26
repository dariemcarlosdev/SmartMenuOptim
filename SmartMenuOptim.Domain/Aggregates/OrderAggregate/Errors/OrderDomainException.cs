using SmartMenuOptim.Domain.Exceptions;
namespace SmartMenuOptim.Domain.Aggregates.OrderAggregate.Errors;

/// <summary>
/// Exception thrown when an order-related business rule is violated.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Represents business rule violations specific to the Order aggregate, such as invalid
/// status transitions, empty order placement, item modification after preparation, or
/// insufficient order items.</para>
///
/// <para><strong>Order Aggregate Invariants:</strong></para>
/// <list type="bullet">
///   <item><description>An order must contain at least one item before being placed</description></item>
///   <item><description>Status transitions must follow the valid lifecycle (Pending → Confirmed → Preparing → Ready → Completed)</description></item>
///   <item><description>Items cannot be modified after certain status transitions (e.g., after Preparing)</description></item>
///   <item><description>Completed and Cancelled orders are in terminal states and cannot be modified</description></item>
///   <item><description>Total amount must always equal the sum of item subtotals</description></item>
/// </list>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new OrderDomainException("Cannot place an order without items.");
/// throw new OrderDomainException($"Order item '{orderItemId}' not found in this order.");
/// throw new OrderDomainException("Cannot modify a completed order.", orderId);
/// </code>
/// </remarks>
public class OrderDomainException : DomainException
{
    /// <summary>
    /// Gets the order identifier associated with this exception, if available.
    /// </summary>
    public int? OrderId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderDomainException"/> class.
    /// </summary>
    /// <param name="message">A message describing the order business rule violation.</param>
    public OrderDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderDomainException"/> class with an order identifier.
    /// </summary>
    /// <param name="message">A message describing the order business rule violation.</param>
    /// <param name="orderId">The identifier of the order involved in the violation.</param>
    public OrderDomainException(string message, int orderId)
        : base(message)
    {
        OrderId = orderId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderDomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">A message describing the order business rule violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public OrderDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
