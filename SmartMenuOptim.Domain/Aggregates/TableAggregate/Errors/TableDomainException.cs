using SmartMenuOptim.Domain.Exceptions;
namespace SmartMenuOptim.Domain.Aggregates.TableAggregate.Errors;

/// <summary>
/// Exception thrown when a table-related business rule is violated.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Represents business rule violations specific to the Table aggregate root,
/// such as invalid status transitions, capacity violations, tenant consistency failures,
/// or attempts to modify a table in an invalid state.</para>
///
/// <para><strong>Table Aggregate Invariants:</strong></para>
/// <list type="bullet">
///   <item><description>A reserved table cannot be directly occupied — it must be marked available first</description></item>
///   <item><description>An occupied table cannot be reserved — it must become available first</description></item>
///   <item><description>Table details cannot be updated while the table is occupied</description></item>
///   <item><description>Table must belong to exactly one restaurant with a valid RestaurantId</description></item>
///   <item><description>All child reservations must belong to the same restaurant as the table</description></item>
///   <item><description>A table with Reserved status must have at least one active reservation</description></item>
/// </list>
///
/// <para><strong>Distinction from ReservationDomainException:</strong></para>
/// <para><c>TableDomainException</c> covers the Table aggregate root's own invariants (status transitions,
/// tenant consistency, update guards), while <see cref="ReservationDomainException"/> covers reservation-specific
/// rules (booking conflicts, reservation lifecycle, party size vs. capacity).</para>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new TableDomainException("Cannot occupy a reserved table. Mark it as available first.");
/// throw new TableDomainException("Cannot reserve an occupied table.", tableId);
/// throw new TableDomainException("Cannot update table details while occupied.", tableId);
/// </code>
/// </remarks>
public class TableDomainException : DomainException
{
    /// <summary>
    /// Gets the table identifier associated with this exception, if available.
    /// </summary>
    public int? TableId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableDomainException"/> class.
    /// </summary>
    /// <param name="message">A message describing the table business rule violation.</param>
    public TableDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableDomainException"/> class with a table identifier.
    /// </summary>
    /// <param name="message">A message describing the table business rule violation.</param>
    /// <param name="tableId">The identifier of the table involved in the violation.</param>
    public TableDomainException(string message, int tableId)
        : base(message)
    {
        TableId = tableId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableDomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">A message describing the table business rule violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TableDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
