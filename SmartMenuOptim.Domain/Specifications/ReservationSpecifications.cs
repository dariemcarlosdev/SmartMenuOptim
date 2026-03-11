using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Enums;
using System.Linq.Expressions;

namespace SmartMenuOptim.Domain.Specifications;

/// <summary>
/// Specification for querying active reservations (Pending or Confirmed, not deleted).Consult S
/// 
/// </summary>
/// <remarks>
/// <para><strong>Domain Layer - Specification Pattern</strong></para>
/// 
/// This specification encapsulates the business rule that "active reservations"
/// are those with Pending or Confirmed status that have not been soft-deleted.
/// 
/// <para><strong>Clean Architecture Benefits:</strong></para>
/// <list type="bullet">
///   <item><description>Business query logic stays in the Domain layer</description></item>
///   <item><description>Reusable across different use cases</description></item>
///   <item><description>Testable in isolation</description></item>
///   <item><description>No infrastructure coupling</description></item>
/// </list>
/// </remarks>
public class ActiveReservationsSpecification : BaseSpecification<Reservation>
{
    public ActiveReservationsSpecification() 
        : base(r => (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) 
                    && !r.IsDeleted)
    {
        // No additional ordering or includes needed for cleanup operations
    }
}

/// <summary>
/// Specification for querying all non-deleted reservations (for statistics).
/// </summary>
/// <remarks>
/// Used for getting reservation counts and statistics across all statuses.
/// </remarks>
public class NonDeletedReservationsSpecification : BaseSpecification<Reservation>
{
    public NonDeletedReservationsSpecification() 
        : base(r => !r.IsDeleted)
    {
        // No additional configuration needed
    }
}
