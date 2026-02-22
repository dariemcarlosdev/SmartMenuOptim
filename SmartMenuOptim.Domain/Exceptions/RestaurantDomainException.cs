namespace SmartMenuOptim.Domain.Exceptions;

/// <summary>
/// Exception thrown when a restaurant-related business rule is violated.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Represents business rule violations specific to the Restaurant aggregate, such as
/// attempting to accept orders without configured business hours, invalid operating hour
/// configurations, or capacity constraint violations.</para>
///
/// <para><strong>Restaurant Aggregate Invariants:</strong></para>
/// <list type="bullet">
///   <item><description>A restaurant cannot accept orders without at least one configured business hours entry</description></item>
///   <item><description>Business hours close time must be after open time for the same day</description></item>
///   <item><description>Maximum simultaneous orders must be a positive value</description></item>
///   <item><description>Timezone must be a valid IANA or Windows timezone identifier</description></item>
/// </list>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new RestaurantDomainException("Cannot accept orders without setting business hours.");
/// throw new RestaurantDomainException("Close time must be after open time.", restaurantId);
/// </code>
/// </remarks>
public class RestaurantDomainException : DomainException
{
    /// <summary>
    /// Gets the restaurant identifier associated with this exception, if available.
    /// </summary>
    public int? RestaurantId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestaurantDomainException"/> class.
    /// </summary>
    /// <param name="message">A message describing the restaurant business rule violation.</param>
    public RestaurantDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestaurantDomainException"/> class with a restaurant identifier.
    /// </summary>
    /// <param name="message">A message describing the restaurant business rule violation.</param>
    /// <param name="restaurantId">The identifier of the restaurant involved in the violation.</param>
    public RestaurantDomainException(string message, int restaurantId)
        : base(message)
    {
        RestaurantId = restaurantId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestaurantDomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">A message describing the restaurant business rule violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RestaurantDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
