using SmartMenuOptim.Domain.Services.Contracts;

namespace SmartMenuOptim.Domain.Events;

/// <summary>
/// Abstract base class for all domain events in the SmartMenuOptimizer application.
/// Provides common properties and behavior for event identification, timestamping, and tracking.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>This base class implements the <see cref="IDomainEvent"/> interface and provides default 
/// implementations for common event properties. All concrete domain events should inherit from 
/// this class to ensure consistency across the domain event system.</para>
/// 
/// <para><strong>Key Features:</strong></para>
/// <list type="bullet">
///     <item><description><strong>Auto-generated EventId:</strong> Unique GUID for idempotency and tracking</description></item>
///     <item><description><strong>UTC Timestamp:</strong> Precise occurrence time for ordering and auditing</description></item>
///     <item><description><strong>Event Type Name:</strong> Derived from class name for routing and serialization</description></item>
///     <item><description><strong>Immutability:</strong> All properties are init-only to prevent modification after creation</description></item>
/// </list>
/// 
/// <para><strong>Usage:</strong></para>
/// <code>
/// public class OrderPlacedEvent : DomainEventBase
/// {
///     public int OrderId { get; init; }
///     public decimal TotalAmount { get; init; }
///     
///     public OrderPlacedEvent(int orderId, decimal totalAmount)
///     {
///         OrderId = orderId;
///         TotalAmount = totalAmount;
///     }
/// }
/// </code>
/// 
/// <para><strong>Integration with MediatR:</strong></para>
/// <para>These events can be used with MediatR's INotification pattern for in-process event handling,
/// or published to message queues (Azure Service Bus, RabbitMQ) for distributed scenarios.</para>
/// </remarks>
public abstract class DomainEventBase : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public virtual string EventType => GetType().Name;

    /// <summary>
    /// Gets the version of the event schema for handling event evolution.
    /// Override in derived classes when event structure changes.
    /// </summary>
    public virtual int EventVersion => 1;

    /// <summary>
    /// Gets or sets optional correlation ID for tracking related events across systems.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets or sets optional causation ID linking this event to the event that caused it.
    /// </summary>
    public string? CausationId { get; init; }
}
