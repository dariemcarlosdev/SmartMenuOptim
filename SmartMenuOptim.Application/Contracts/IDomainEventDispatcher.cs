using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Application.Contracts;

/// <summary>
/// Interface for dispatching domain events through the application.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>This interface abstracts the domain event dispatching mechanism, allowing events to be 
/// published to all registered handlers. It serves as the bridge between aggregates that raise 
/// events and the handlers that react to them.</para>
/// 
/// <para><strong>Integration Points:</strong></para>
/// <list type="bullet">
///     <item><description>Called by <c>AppDbContext.SaveChangesAsync()</c> after successful persistence</description></item>
///     <item><description>Uses MediatR internally to publish events to all handlers</description></item>
///     <item><description>Supports both sync and async event processing</description></item>
/// </list>
/// 
/// <para><strong>Usage:</strong></para>
/// <code>
/// // In AppDbContext.SaveChangesAsync
/// var events = ChangeTracker.Entries&lt;IAggregateRoot&gt;()
///     .SelectMany(e => e.Entity.DomainEvents);
/// await _eventDispatcher.DispatchEventsAsync(events, cancellationToken);
/// </code>
/// </remarks>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a collection of domain events to all registered handlers.
    /// </summary>
    /// <param name="events">The domain events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a single domain event to all registered handlers.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
