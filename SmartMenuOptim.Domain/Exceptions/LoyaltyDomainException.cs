namespace SmartMenuOptim.Domain.Exceptions;

/// <summary>
/// Exception thrown when a customer loyalty business rule is violated.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Represents business rule violations specific to the CustomerLoyalty aggregate, such as
/// insufficient points for redemption, invalid point values, or tier progression violations.</para>
///
/// <para><strong>CustomerLoyalty Aggregate Invariants:</strong></para>
/// <list type="bullet">
///   <item><description>Points balance cannot go negative after a redemption</description></item>
///   <item><description>Points added or redeemed must be positive values</description></item>
///   <item><description>Tier must always reflect the current point balance</description></item>
///   <item><description>Transaction history is append-only (immutable once created)</description></item>
/// </list>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new LoyaltyDomainException("Insufficient points for redemption. Available: 50, Requested: 100.");
/// throw new LoyaltyDomainException("Points must be a positive value.", customerLoyaltyId);
/// </code>
/// </remarks>
public class LoyaltyDomainException : DomainException
{
    /// <summary>
    /// Gets the customer loyalty identifier associated with this exception, if available.
    /// </summary>
    public int? CustomerLoyaltyId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoyaltyDomainException"/> class.
    /// </summary>
    /// <param name="message">A message describing the loyalty business rule violation.</param>
    public LoyaltyDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoyaltyDomainException"/> class with a customer loyalty identifier.
    /// </summary>
    /// <param name="message">A message describing the loyalty business rule violation.</param>
    /// <param name="customerLoyaltyId">The identifier of the customer loyalty membership involved in the violation.</param>
    public LoyaltyDomainException(string message, int customerLoyaltyId)
        : base(message)
    {
        CustomerLoyaltyId = customerLoyaltyId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoyaltyDomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">A message describing the loyalty business rule violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LoyaltyDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
