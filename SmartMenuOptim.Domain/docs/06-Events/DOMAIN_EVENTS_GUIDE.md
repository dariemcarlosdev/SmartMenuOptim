---
title: Domain Events Implementation Guide
project: SmartMenuOptimizer
layer: Domain
version: "1.3"
created: "2026"
updated: "2026-03-21"
status: implementation-complete
tags: [domain-events, DDD, MediatR, clean-architecture, event-driven]
related:
  - docs/08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md
  - SmartMenuOptim.Domain/docs/06-Events/EVENTS_CLEAN.md
  - docs/08-Patterns/EVENT_DRIVEN_IMPROVEMENT_TRACKER.md
actual_file_locations:
  IDomainEvent: SmartMenuOptim.Domain/Common/IDomainEvent.cs
  IHasDomainEvents: SmartMenuOptim.Domain/Common/IHasDomainEvents.cs
  DomainEventBase: SmartMenuOptim.Domain/Common/DomainEventBase.cs
  IDomainEventDispatcher: SmartMenuOptim.Application/Contracts/IDomainEventDispatcher.cs
  IDeadLetterQueueService: SmartMenuOptim.Application/Contracts/IDeadLetterQueueService.cs
  ResilientEventHandlerBase: SmartMenuOptim.Application/Handlers/ResilientEventHandlerBase.cs
  MediatRDomainEventDispatcher: SmartMenuOptim.Infrastructure/EventDispatching/MediatRDomainEventDispatcher.cs
  InMemoryDeadLetterQueueService: SmartMenuOptim.Infrastructure/Services/DeadLetterQueue/InMemoryDeadLetterQueueService.cs
---

# Domain Events Implementation Guide

Domain events are immutable objects representing significant business occurrences in the SmartMenuOptim domain. They enable loose coupling between aggregates, eventual consistency, audit trails, and reactive event-driven processing. Events are defined and raised in the Domain layer, handled in the Application layer, and dispatched by Infrastructure.

## Layer Placement

| Component | Layer | Location |
|-----------|-------|----------|
| `IDomainEvent` | Domain | `Common/IDomainEvent.cs` |
| `IHasDomainEvents` | Domain | `Common/IHasDomainEvents.cs` |
| `DomainEventBase` | Domain | `Common/DomainEventBase.cs` |
| Concrete events | Domain | `Aggregates/{Agg}Aggregate/Events/{Name}Event.cs` |
| `IDomainEventDispatcher` | Application | `Contracts/IDomainEventDispatcher.cs` |
| `IDeadLetterQueueService` | Application | `Contracts/IDeadLetterQueueService.cs` |
| `ResilientEventHandlerBase<T>` | Application | `Handlers/ResilientEventHandlerBase.cs` |
| Concrete handlers | Application | `Handlers/{Category}EventHandlers/{Name}Handler.cs` |
| `MediatRDomainEventDispatcher` | Infrastructure | `EventDispatching/MediatRDomainEventDispatcher.cs` |
| `AppDbContext` (dispatch hook) | Infrastructure | `Persistence/Context/AppDbContext.cs` |

## Design Principles

| Principle | Rule | Example |
|-----------|------|---------|
| SRP | One event = one business occurrence | `OrderPlacedEvent` + `OrderCancelledEvent`, not `OrderStateChangedEvent` |
| OCP | New events require no changes to existing code | Aggregates implement `IHasDomainEvents` for auto-discovery |
| Immutability | All properties `{ get; init; }`, `sealed class` | Events are facts about the past — never modified |
| Self-describing | Events carry all context handlers need | Use scalar IDs, never entity/navigation references |
| Multi-tenant | Always include `RestaurantId` | Handlers scope operations to the correct tenant |

## Base Contracts

### IDomainEvent

```csharp
// SmartMenuOptim.Domain/Common/IDomainEvent.cs
using MediatR;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}
```

### IHasDomainEvents

```csharp
// SmartMenuOptim.Domain/Common/IHasDomainEvents.cs
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

Aggregates implementing this interface are auto-discovered by `AppDbContext.SaveChangesAsync` via `ChangeTracker.Entries<IHasDomainEvents>()`. No per-aggregate registration required.

### DomainEventBase

```csharp
// SmartMenuOptim.Domain/Common/DomainEventBase.cs
public abstract class DomainEventBase : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public virtual string EventType => GetType().Name;
    public virtual int EventVersion => 1;
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
}
```

| Property | Purpose |
|----------|---------|
| `EventId` | Idempotency key, deduplication, tracking |
| `OccurredOn` | UTC timestamp for ordering and auditing |
| `EventType` | Derived from class name — serialization routing |
| `EventVersion` | Schema evolution — override when event shape changes |
| `CorrelationId` | Distributed tracing — links related events |
| `CausationId` | Causal chain — links event to its cause |

## Event Catalog

| Event | Aggregate | Location | Handlers |
|-------|-----------|----------|----------|
| `OrderPlacedEvent` | Order | `OrderAggregate/Events/` | `AwardLoyaltyPointsHandler`, `SendOrderConfirmationHandler`, `SendKitchenNotificationHandler`, `UpdateOrderAnalyticsHandler` |
| `OrderCancelledEvent` | Order | `OrderAggregate/Events/` | `OrderCancelledHandler` |
| `OrderCompletedEvent` | Order | `OrderAggregate/Events/` | `OrderCompletedHandler` |
| `LoyaltyPointsEarnedEvent` | CustomerLoyalty | `CustomerLoyaltyAggregate/Events/` | `LoyaltyPointsEarnedHandler` |
| `LoyaltyTierChangedEvent` | CustomerLoyalty | `CustomerLoyaltyAggregate/Events/` | `LoyaltyTierChangedHandler` |
| `DishAddedToMenuEvent` | Menu | `MenuAggregate/Events/` | `DishAddedToMenuHandler` |
| `DishRemovedFromMenuEvent` | Menu | `MenuAggregate/Events/` | `DishRemovedFromMenuHandler` |
| `SaleRecordedEvent` | SaleRecord | `SaleRecordAggregate/Events/` | `SaleRecordedHandler` |
| `DailySalesSummarizedEvent` | SaleRecord | `SaleRecordAggregate/Events/` | `DailySalesSummarizedHandler` |

## Event Schemas

### Order Events

```csharp
// Aggregates/OrderAggregate/Events/OrderPlacedEvent.cs
public sealed class OrderPlacedEvent : DomainEventBase
{
    public int OrderId { get; init; }
    public int RestaurantId { get; init; }
    public int CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; }
    public int ItemCount { get; init; }
    public string? SpecialInstructions { get; init; }
    public string? OrderType { get; init; }  // DineIn, TakeOut, Delivery
}
```

```csharp
// Aggregates/OrderAggregate/Events/OrderCancelledEvent.cs
public sealed class OrderCancelledEvent : DomainEventBase
{
    public int OrderId { get; init; }
    public int RestaurantId { get; init; }
    public int CustomerId { get; init; }
    public string CancellationReason { get; init; }
    public CancellationSource CancelledBy { get; init; }  // Customer, Staff, System
    public decimal OrderTotal { get; init; }
    public bool RequiresRefund { get; init; }
    public int LoyaltyPointsToReverse { get; init; }
}
```

```csharp
// Aggregates/OrderAggregate/Events/OrderCompletedEvent.cs
public sealed class OrderCompletedEvent : DomainEventBase
{
    public int OrderId { get; init; }
    public int RestaurantId { get; init; }
    public int CustomerId { get; init; }
    public decimal FinalTotal { get; init; }
    public DateTime OrderPlacedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public double FulfillmentTimeMinutes => (CompletedAt - OrderPlacedAt).TotalMinutes;
    public int LoyaltyPointsEarned { get; init; }
}
```

### Loyalty Events

```csharp
// Aggregates/CustomerLoyaltyAggregate/Events/LoyaltyPointsEarnedEvent.cs
public sealed class LoyaltyPointsEarnedEvent : DomainEventBase
{
    public int CustomerLoyaltyId { get; init; }
    public int CustomerId { get; init; }
    public int RestaurantId { get; init; }
    public int PointsEarned { get; init; }
    public int NewTotalBalance { get; init; }
    public PointEarningSource EarningSource { get; init; }
    // Purchase, Bonus, Referral, Birthday, Review, SignUpBonus, Adjustment, Survey, SocialMedia, Restoration
    public int? RelatedOrderId { get; init; }
    public decimal PointsMultiplier { get; init; }
}
```

```csharp
// Aggregates/CustomerLoyaltyAggregate/Events/LoyaltyTierChangedEvent.cs
public sealed class LoyaltyTierChangedEvent : DomainEventBase
{
    public int CustomerLoyaltyId { get; init; }
    public int CustomerId { get; init; }
    public int RestaurantId { get; init; }
    public string PreviousTier { get; init; }
    public string NewTier { get; init; }
    public bool IsUpgrade { get; init; }
    public TierChangeReason ChangeReason { get; init; }
    // PointsAccumulation, PointsRedemption, PointsExpiration, ManualAdjustment, Promotion, SignUpBonus, InactivityReset, AnnualReview
    public decimal PreviousTierDiscountPercent { get; init; }
    public decimal NewTierDiscountPercent { get; init; }
    public List<string> BenefitsChanged { get; init; }
}
```

Tier thresholds: Bronze 0-99 (base), Silver 100-499 (10%), Gold 500-999 (15%), Platinum 1000+ (20% + VIP).

### Menu Events

```csharp
// Aggregates/MenuAggregate/Events/DishAddedToMenuEvent.cs
public sealed class DishAddedToMenuEvent : DomainEventBase
{
    public int MenuId { get; init; }
    public int DishId { get; init; }
    public int RestaurantId { get; init; }
    public string DishName { get; init; }
    public decimal Price { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; }
    public string MenuType { get; init; }  // Regular, Breakfast, Lunch, Dinner, Seasonal, Special
    public bool IsFeatured { get; init; }
    public List<string> DietaryFlags { get; init; }
    public List<string> Allergens { get; init; }
}
```

```csharp
// Aggregates/MenuAggregate/Events/DishRemovedFromMenuEvent.cs
public sealed class DishRemovedFromMenuEvent : DomainEventBase
{
    public int MenuId { get; init; }
    public int DishId { get; init; }
    public int RestaurantId { get; init; }
    public string DishName { get; init; }
    public DishRemovalReason RemovalReason { get; init; }
    // Discontinued, SeasonalEnd, OutOfStock, Underperforming, MenuRedesign, QualityIssue, PricingIssue, SupplierIssue, CustomerFeedback, ComplianceIssue, Other
    public bool IsPermanent { get; init; }
    public decimal LastPrice { get; init; }
    public int TotalQuantitySold { get; init; }
    public decimal TotalRevenue { get; init; }
    public int DaysOnMenu { get; init; }
}
```

### Sale Events

```csharp
// Aggregates/SaleRecordAggregate/Events/SaleRecordedEvent.cs
public sealed class SaleRecordedEvent : DomainEventBase
{
    public int SaleRecordId { get; init; }
    public int RestaurantId { get; init; }
    public int OrderId { get; init; }
    public int DishId { get; init; }
    public string DishName { get; init; }
    public int QuantitySold { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime SaleDateTime { get; init; }
    public DayOfWeek DayOfWeek => SaleDateTime.DayOfWeek;
    public int HourOfDay => SaleDateTime.Hour;
    public bool IsLunchHour => HourOfDay is >= 11 and <= 14;  // 11 AM - 2 PM
    public bool IsDinnerHour => HourOfDay is >= 17 and <= 21; // 5 PM - 9 PM
}
```

```csharp
// Aggregates/SaleRecordAggregate/Events/DailySalesSummarizedEvent.cs
public sealed class DailySalesSummarizedEvent : DomainEventBase
{
    public int RestaurantId { get; init; }
    public DateOnly SummaryDate { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public int TotalItemsSold { get; init; }
    public decimal AverageOrderValue => TotalOrders > 0 ? TotalRevenue / TotalOrders : 0;
    public string? TopSellingDish { get; init; }
    public int PeakHour { get; init; }
    public Dictionary<string, decimal> RevenueByCategory { get; init; }
    public decimal? PercentChangeFromPreviousDay { get; }
    public decimal? PercentChangeFromLastWeek { get; }
    public decimal? TargetAchievementPercent { get; }
    public List<string> UnderperformingDishes { get; init; }
}
```

## Aggregate Event Collection Pattern

Every aggregate that raises events implements `IHasDomainEvents` and follows this pattern:

```csharp
// Example: Order aggregate
public class Order : TenantEntityBase, IValidatableObject, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void Place(int confirmedStatusId, string? orderType = null)
    {
        // 1. Guard clauses
        // 2. Domain validation
        // 3. State mutation
        OrderStatusId = confirmedStatusId;
        // 4. Raise event (after state is valid)
        AddDomainEvent(new OrderPlacedEvent(
            orderId: Id, restaurantId: RestaurantId,
            customerId: CustomerId, totalAmount: TotalAmount,
            itemCount: Items.Count, orderType: orderType));
    }
}
```

Cascading example — `CustomerLoyalty.AddPoints()` may raise both `LoyaltyPointsEarnedEvent` and `LoyaltyTierChangedEvent` if the new balance crosses a tier threshold.

## Event Lifecycle

```
Aggregate.Method()        →  AppDbContext.SaveChangesAsync()      →  MediatR Pipeline
1. Business logic runs       3. CollectDomainEvents()                6. _mediator.Publish(event)
2. AddDomainEvent(new …)     4. ClearDomainEventsFromAggregates()    7. Handler1.ProcessEventAsync()
                              5. base.SaveChangesAsync() [DB commit]    Handler2.ProcessEventAsync()
                              6. DispatchEventsAsync(events)            HandlerN.ProcessEventAsync()
```

Events dispatch only AFTER database commit succeeds. Events are cleared BEFORE save to prevent double-dispatch on retry.

## Resilient Handler Pattern

All handlers inherit `ResilientEventHandlerBase<TEvent>` which provides exponential backoff retry (3 attempts, 2^n second delays) and dead letter queue for permanently failed events.

```csharp
// SmartMenuOptim.Application/Handlers/{Category}EventHandlers/{Name}Handler.cs
public class AwardLoyaltyPointsHandler
    : ResilientEventHandlerBase<OrderPlacedEvent>, INotificationHandler<OrderPlacedEvent>
{
    public AwardLoyaltyPointsHandler(
        ILogger<AwardLoyaltyPointsHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue) { }

    protected override async Task ProcessEventAsync(
        OrderPlacedEvent @event, CancellationToken ct)
    {
        var pointsToAward = (int)Math.Floor(@event.TotalAmount); // 1 point per $1
        // Retrieve CustomerLoyalty, call AddPoints, save via UoW
    }
}
```

Handler rules:
- Override `ProcessEventAsync`, never `Handle`
- Throw for transient failures (retried automatically) — catch and log permanent failures
- DLQ parameter always `IDeadLetterQueueService? deadLetterQueue = null`
- One handler per concern; multiple handlers per event is normal (OCP)

| Handler Type | Injects Repos? | Mutates State? | Example |
|--------------|---------------|----------------|---------|
| Persistence | Yes | Yes | `SaleRecordedHandler` |
| Notification | No | No | `SendOrderConfirmationHandler` |
| Cache | No | No | `DishAddedToMenuHandler` |
| Analytics | No | No | `UpdateOrderAnalyticsHandler` |
| Orchestration | Yes | Yes | `AwardLoyaltyPointsHandler` |

## MediatR Integration

`IDomainEvent` extends MediatR `INotification`. Handlers are auto-discovered via assembly scanning:

```csharp
// Application DI registration
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly));

// Infrastructure DI registration
services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
services.AddSingleton<IDeadLetterQueueService, InMemoryDeadLetterQueueService>();
```

`MediatRDomainEventDispatcher` catches and logs exceptions per-event to prevent one failing handler from blocking others.

## Testing Patterns

### Event unit test — verify immutability and base properties

```csharp
[Fact]
public void Constructor_SetsAllProperties()
{
    var @event = new OrderPlacedEvent(orderId: 1, restaurantId: 10,
        customerId: 100, totalAmount: 99.99m, itemCount: 3);

    Assert.Equal(1, @event.OrderId);
    Assert.Equal(10, @event.RestaurantId);
    Assert.NotEqual(Guid.Empty, @event.EventId);
    Assert.True(@event.OccurredOn <= DateTime.UtcNow);
    Assert.Equal("OrderPlacedEvent", @event.EventType);
}
```

### Aggregate test — verify correct event is raised

```csharp
[Fact]
public void Place_RaisesOrderPlacedEvent()
{
    var order = CreateTestOrder();
    order.Place();
    Assert.Single(order.DomainEvents);
    var @event = Assert.IsType<OrderPlacedEvent>(order.DomainEvents.First());
    Assert.Equal(order.Id, @event.OrderId);
}
```

### Handler test — mock dependencies, verify side effects

```csharp
[Fact]
public async Task Handle_AwardsLoyaltyPoints()
{
    var mockRepo = new Mock<ICustomerLoyaltyRepository>();
    var loyalty = new CustomerLoyalty(customerId: 100, restaurantId: 10);
    mockRepo.Setup(r => r.GetByCustomerAndRestaurantAsync(100, 10, It.IsAny<CancellationToken>()))
        .ReturnsAsync(loyalty);

    var handler = new AwardLoyaltyPointsHandler(
        mockRepo.Object, Mock.Of<ILogger<AwardLoyaltyPointsHandler>>());
    await handler.Handle(new OrderPlacedEvent(1, 10, 100, 50.00m, 2), CancellationToken.None);

    Assert.Equal(50, loyalty.TotalPoints);
    mockRepo.Verify(r => r.UpdateAsync(loyalty, It.IsAny<CancellationToken>()), Times.Once);
}
```

## Best Practices

| Do | Don't |
|----|-------|
| `sealed class`, `{ get; init; }` properties | Include entity/navigation references (use IDs) |
| Past-tense naming: `OrderPlacedEvent` | Generic names: `EntityChangedEvent` |
| Include `RestaurantId` for tenant isolation | Modify events after creation |
| One event per business occurrence | Couple handlers to each other |
| Include correlation IDs for tracing | Throw from handlers without retry support |
| Version events via `EventVersion` override | Include sensitive data (events may be logged) |
| Make handlers idempotent | Use events for queries (they signal state changes) |
| Log structured event processing data | Override `Handle()` — override `ProcessEventAsync()` |

## Naming Conventions

| Artifact | Pattern | Example |
|----------|---------|---------|
| Event class | `{Entity}{PastTenseVerb}Event` | `OrderPlacedEvent` |
| Event file | `Aggregates/{Agg}Aggregate/Events/{Name}Event.cs` | `OrderAggregate/Events/OrderPlacedEvent.cs` |
| Handler class | `{DescriptiveAction}Handler` | `AwardLoyaltyPointsHandler` |
| Handler file | `Handlers/{Category}EventHandlers/{Name}Handler.cs` | `Handlers/OrderEventHandlers/AwardLoyaltyPointsHandler.cs` |
| Enums | Same file as event or aggregate namespace | `CancellationSource`, `DishRemovalReason` |

## Roadmap

| Enhancement | Status | Notes |
|-------------|--------|-------|
| `ResilientEventHandlerBase` + DLQ | Done (v1.2) | Exponential backoff, `IDeadLetterQueueService` |
| `IHasDomainEvents` auto-discovery | Done (v1.3) | Eliminates per-aggregate `AppDbContext` registration |
| Outbox Pattern | Planned | At-least-once delivery, survives process crashes |
| Event Sourcing | Planned | Store events as source of truth |
| Saga/Process Manager | Planned | Long-running business processes with compensation |
| Event Versioning/Upcasting | Planned | Schema evolution for stored events |
| Event Store Integration | Planned | Azure Event Hubs / Kafka / EventStoreDB |

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026 | Initial domain events implementation |
| 1.1 | 2026 | Event catalog and integration patterns |
| 1.2 | 2026-02-08 | `ResilientEventHandlerBase`, `IDeadLetterQueueService`, retry patterns |
| 1.3 | 2026-03-21 | `IHasDomainEvents` interface, RAG-optimized format, corrected file paths |
