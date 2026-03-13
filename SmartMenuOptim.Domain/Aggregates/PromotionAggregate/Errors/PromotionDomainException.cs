using SmartMenuOptim.Domain.Exceptions;
namespace SmartMenuOptim.Domain.Aggregates.PromotionAggregate.Errors;

/// <summary>
/// Exception thrown when a promotion-related business rule is violated.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Represents business rule violations specific to the Promotion aggregate root,
/// such as activating before the valid date range, modifying an active promotion,
/// or tenant consistency failures.</para>
///
/// <para><strong>Promotion Aggregate Invariants:</strong></para>
/// <list type="bullet">
///   <item><description>A promotion cannot be activated before its ValidFrom date</description></item>
///   <item><description>Promotion details cannot be modified while the promotion is active</description></item>
///   <item><description>Promotion must belong to a valid, non-deleted restaurant</description></item>
///   <item><description>Restaurant navigation property must match RestaurantId</description></item>
/// </list>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new PromotionDomainException("Cannot activate promotion before ValidFrom date.");
/// throw new PromotionDomainException("Cannot update promotion details while active. Deactivate first.");
/// throw new PromotionDomainException("Promotion is associated with a deleted restaurant.", promotionId);
/// </code>
/// </remarks>
public class PromotionDomainException : DomainException
{
    /// <summary>
    /// Gets the promotion identifier associated with this exception, if available.
    /// </summary>
    public int? PromotionId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionDomainException"/> class.
    /// </summary>
    /// <param name="message">A message describing the promotion business rule violation.</param>
    public PromotionDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionDomainException"/> class with a promotion identifier.
    /// </summary>
    /// <param name="message">A message describing the promotion business rule violation.</param>
    /// <param name="promotionId">The identifier of the promotion involved in the violation.</param>
    public PromotionDomainException(string message, int promotionId)
        : base(message)
    {
        PromotionId = promotionId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionDomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">A message describing the promotion business rule violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public PromotionDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
