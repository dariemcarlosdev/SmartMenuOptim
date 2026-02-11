using System;
using MediatR;

namespace SmartMenuOptim.Domain.Services.Contracts
{
    /// <summary>
    /// Marker interface for domain events in the SmartMenuOptimizer application.
    /// All domain events must implement this interface to enable event-driven communication 
    /// between aggregates while maintaining loose coupling and clean architecture boundaries.
    /// </summary>
    /// <remarks>
    /// <para><strong>MediatR Integration:</strong></para>
    /// <para>This interface extends <see cref="INotification"/> from MediatR, enabling automatic 
    /// event dispatching through the MediatR pipeline. Event handlers implement 
    /// <see cref="INotificationHandler{TNotification}"/> to react to domain events.</para>
    /// 
    /// <para><strong>Domain Events Overview:</strong></para>
    /// <para>Domain events represent something significant that has happened in the domain. They are
    /// used to communicate changes between aggregates and bounded contexts without creating tight
    /// coupling between domain objects.</para>
    /// 
    /// <para><strong>Event-Driven Architecture Benefits:</strong></para>
    /// <list type="bullet">
    ///     <item><description><strong>Loose Coupling:</strong> Aggregates don't need to know about each other</description></item>
    ///     <item><description><strong>Single Responsibility:</strong> Each aggregate handles its own business logic</description></item>
    ///     <item><description><strong>Audit Trail:</strong> Events provide a natural audit log of domain activities</description></item>
    ///     <item><description><strong>Eventual Consistency:</strong> Enables async processing across bounded contexts</description></item>
    ///     <item><description><strong>Scalability:</strong> Events can be published to message queues for distributed processing</description></item>
    /// </list>
    /// 
    /// <para><strong>Usage Pattern:</strong></para>
    /// <code>
    /// // 1. Aggregate raises event
    /// public class Order : AggregateRoot
    /// {
    ///     public void Place()
    ///     {
    ///         // Business logic...
    ///         AddDomainEvent(new OrderPlacedEvent(this));
    ///     }
    /// }
    /// 
    /// // 2. Event handler reacts (using MediatR INotificationHandler)
    /// public class OrderPlacedEventHandler : INotificationHandler&lt;OrderPlacedEvent&gt;
    /// {
    ///     public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    ///     {
    ///         // Award loyalty points, send notification, etc.
    ///     }
    /// }
    /// </code>
    /// 
    /// <para><strong>Event Categories in SmartMenuOptimizer:</strong></para>
    /// <list type="bullet">
    ///     <item><description><strong>Order Events:</strong> OrderPlaced, OrderCancelled, OrderCompleted</description></item>
    ///     <item><description><strong>Loyalty Events:</strong> LoyaltyPointsEarned, LoyaltyTierChanged</description></item>
    ///     <item><description><strong>Menu Events:</strong> DishAddedToMenu, DishRemovedFromMenu</description></item>
    ///     <item><description><strong>Sale Events:</strong> SaleRecorded, DailySalesSummarized</description></item>
    /// </list>
    /// 
    /// <para><strong>Clean Architecture Placement:</strong></para>
    /// <para>Domain events are defined in the Domain layer as they represent core business occurrences.
    /// Event handlers typically reside in the Application or Infrastructure layers, depending on whether
    /// they contain business logic or technical concerns (e.g., sending emails).</para>
    /// 
    /// <para><strong>Related Documentation:</strong></para>
    /// <para>See docs/architecture/DOMAIN_EVENTS_GUIDE.md for comprehensive implementation guide.</para>
    /// <para>See docs/architecture/EVENT_HANDLER_IMPLEMENTATION.md for handler patterns and best practices.</para>
    /// </remarks>
    public interface IDomainEvent : INotification
    {
        /// <summary>
        /// Gets the unique identifier for this specific event instance.
        /// Used for idempotency, tracking, and debugging purposes.
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// Gets the UTC timestamp when this event occurred in the domain.
        /// </summary>
        DateTime OccurredOn { get; }

        /// <summary>
        /// Gets the type name of the event for serialization and routing purposes.
        /// </summary>
        string EventType { get; }
    }
}
