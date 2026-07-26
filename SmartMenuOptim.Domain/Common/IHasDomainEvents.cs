namespace SmartMenuOptim.Domain.Common;

/// <summary>
/// Contract for aggregates and entities that raise domain events.
/// Enables automatic event collection and clearing by the infrastructure layer
/// without requiring per-aggregate registration in <c>AppDbContext</c>.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>This interface eliminates the need to manually register each aggregate type
/// in <c>AppDbContext.CollectDomainEvents()</c> and <c>ClearDomainEventsFromAggregates()</c>.
/// The infrastructure layer uses <c>ChangeTracker.Entries&lt;IHasDomainEvents&gt;()</c> to
/// discover all tracked entities that raise events in a single pass.</para>
///
/// <para><strong>Open/Closed Principle:</strong></para>
/// <para>New aggregates that raise domain events only need to implement this interface.
/// No changes to <c>AppDbContext</c> or any infrastructure code are required.</para>
///
/// <para><strong>Implementation pattern (inside each aggregate):</strong></para>
/// <code>
/// public class MyAggregate : TenantEntityBase, IHasDomainEvents
/// {
///     private readonly List&lt;IDomainEvent&gt; _domainEvents = new();
///
///     [NotMapped]
///     public IReadOnlyCollection&lt;IDomainEvent&gt; DomainEvents =&gt; _domainEvents.AsReadOnly();
///     public void ClearDomainEvents() =&gt; _domainEvents.Clear();
///     protected void AddDomainEvent(IDomainEvent e) =&gt; _domainEvents.Add(e);
/// }
/// </code>
///
/// <para><strong>Related Documentation:</strong></para>
/// <para>See docs/08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md for the full event-driven framework.</para>
/// </remarks>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the domain events raised by this aggregate since the last clear.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all pending domain events. Called by the infrastructure layer
    /// before <c>SaveChangesAsync</c> to prevent double-dispatch on retry.
    /// </summary>
    void ClearDomainEvents();
}
