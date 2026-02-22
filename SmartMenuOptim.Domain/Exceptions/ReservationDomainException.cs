namespace SmartMenuOptim.Domain.Exceptions;

/// <summary>
/// Exception thrown when a reservation or table-related business rule is violated.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Represents business rule violations specific to the Table and Reservation aggregates,
/// such as double-booking, invalid status transitions, capacity violations, or attempting
/// to reserve an unavailable table.</para>
///
/// <para><strong>Table/Reservation Aggregate Invariants:</strong></para>
/// <list type="bullet">
///   <item><description>A table cannot be reserved if it is already occupied or reserved for the requested time slot</description></item>
///   <item><description>Party size must not exceed table capacity</description></item>
///   <item><description>Reservation status transitions must follow valid lifecycle (Pending → Confirmed → Seated → Completed)</description></item>
///   <item><description>Completed, Cancelled, and NoShow reservations are in terminal states</description></item>
///   <item><description>Reservation time must be in the future at time of booking</description></item>
/// </list>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new ReservationDomainException("Table is not available for the requested time slot.");
/// throw new ReservationDomainException("Party size exceeds table capacity.", tableId);
/// throw new ReservationDomainException("Cannot modify a completed reservation.");
/// </code>
/// </remarks>
public class ReservationDomainException : DomainException
{
    /// <summary>
    /// Gets the table identifier associated with this exception, if available.
    /// </summary>
    public int? TableId { get; }

    /// <summary>
    /// Gets the reservation identifier associated with this exception, if available.
    /// </summary>
    public int? ReservationId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReservationDomainException"/> class.
    /// </summary>
    /// <param name="message">A message describing the reservation business rule violation.</param>
    public ReservationDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReservationDomainException"/> class with a table identifier.
    /// </summary>
    /// <param name="message">A message describing the reservation business rule violation.</param>
    /// <param name="tableId">The identifier of the table involved in the violation.</param>
    public ReservationDomainException(string message, int tableId)
        : base(message)
    {
        TableId = tableId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReservationDomainException"/> class with table and reservation identifiers.
    /// </summary>
    /// <param name="message">A message describing the reservation business rule violation.</param>
    /// <param name="tableId">The identifier of the table involved in the violation.</param>
    /// <param name="reservationId">The identifier of the reservation involved in the violation.</param>
    public ReservationDomainException(string message, int tableId, int reservationId)
        : base(message)
    {
        TableId = tableId;
        ReservationId = reservationId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReservationDomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">A message describing the reservation business rule violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ReservationDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
