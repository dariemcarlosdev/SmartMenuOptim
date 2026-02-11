# SmartMenuOptimizer - Domain Events Implementation Guide

## 📋 Document Information

| Field | Value |
|-------|-------|
| **Document Title** | Domain Events Implementation Guide |
| **Version** | 1.2 |
| **Created** | 2026 |
| **Author** | SmartMenuOptimizer Architecture Team |
| **Status** | Implementation Complete ✅ |
| **Related Documents** | [EVENT_HANDLER_IMPLEMENTATION.md](./EVENT_HANDLER_IMPLEMENTATION.md), [CLEAN_ARCHITECTURE_FULL_ANALYSIS.md](../CLEAN_ARCHITECTURE_FULL_ANALYSIS.md) |

---

## 📑 Table of Contents

1. [Overview](#overview)
2. [Architecture & Design Principles](#architecture--design-principles)
3. [Event Catalog](#event-catalog)
4. [Base Classes & Interfaces](#base-classes--interfaces)
5. [Event Categories](#event-categories)
   - [Order Events](#order-events)
   - [Loyalty Events](#loyalty-events)
   - [Menu Events](#menu-events)
   - [Sale Events](#sale-events)
6. [Implementation Patterns](#implementation-patterns)
7. [Integration with Aggregates](#integration-with-aggregates)
8. [Event Handlers](#event-handlers)
9. [MediatR Integration](#mediatr-integration)
10. [Testing Domain Events](#testing-domain-events)
11. [Best Practices](#best-practices)
12. [Future Enhancements](#future-enhancements)

---

## Overview

### What are Domain Events?

Domain events are a fundamental pattern in Domain-Driven Design (DDD) that represent **something significant that has happened in the domain**. They enable:

- **Loose coupling** between aggregates
- **Eventual consistency** across bounded contexts
- **Audit trails** and event sourcing capabilities
- **Reactive architectures** with event-driven patterns

### SmartMenuOptimizer Event Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    DOMAIN LAYER                             │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Aggregates (Order, CustomerLoyalty, Menu, etc.)    │   │
│  │                                                     │   │
│  │  ┌───────────────────────────────────────────────┐ │   │
│  │  │  Raises Domain Events                         │ │   │
│  │  │  • OrderPlacedEvent                           │ │   │
│  │  │  • LoyaltyPointsEarnedEvent                   │ │   │
│  │  │  • DishAddedToMenuEvent                       │ │   │
│  │  └───────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                 APPLICATION LAYER                           │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Event Handlers (via MediatR)                       │   │
│  │                                                     │   │
│  │  • AwardLoyaltyPointsHandler                        │   │
│  │  • SendOrderConfirmationHandler                     │   │
│  │  • UpdateAnalyticsHandler                           │   │
│  │  • InvalidateCacheHandler                           │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              INFRASTRUCTURE LAYER                           │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  External Integrations                              │   │
│  │                                                     │   │
│  │  • Email/SMS Notifications                          │   │
│  │  • Message Queues (Azure Service Bus)               │   │
│  │  • External Analytics Services                      │   │
│  │  • Third-party Integrations                         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## Architecture & Design Principles

### Clean Architecture Placement

| Component | Layer | Purpose |
|-----------|-------|---------|
| `IDomainEvent` | Domain | Contract definition |
| `DomainEventBase` | Domain | Base implementation |
| Specific Events (e.g., `OrderPlacedEvent`) | Domain | Business event definitions |
| Event Handlers | Application/Infrastructure | React to events |
| Event Dispatcher | Infrastructure | Publishing mechanism |

### Design Principles Applied

#### 1. **Single Responsibility Principle (SRP)**
Each event represents exactly ONE business occurrence.

```csharp
// ✅ Good - Single responsibility
public class OrderPlacedEvent : DomainEventBase { }
public class OrderCancelledEvent : DomainEventBase { }

// ❌ Bad - Multiple responsibilities
public class OrderStateChangedEvent : DomainEventBase { } // Too generic
```

#### 2. **Open/Closed Principle (OCP)**
Add new events without modifying existing code.

```csharp
// New event added without changing existing events or handlers
public class OrderRefundedEvent : DomainEventBase { }
```

#### 3. **Immutability**
Events are immutable after creation - they represent facts that have happened.

```csharp
public sealed class OrderPlacedEvent : DomainEventBase
{
    public int OrderId { get; init; }  // init-only
    public decimal TotalAmount { get; init; }
}
```

#### 4. **Self-Describing**
Events contain all information needed to understand what happened.

```csharp
public class OrderPlacedEvent : DomainEventBase
{
    public int OrderId { get; init; }
    public int RestaurantId { get; init; }
    public int CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public int ItemCount { get; init; }
    // All context included - no need to query database
}
```

---

## Event Catalog

### Quick Reference

| Event | Category | Trigger | Key Handlers |
|-------|----------|---------|--------------|
| `OrderPlacedEvent` | Order | Order confirmed | Loyalty, Kitchen, Analytics |
| `OrderCancelledEvent` | Order | Order cancelled | Refund, Inventory, Loyalty reversal |
| `OrderCompletedEvent` | Order | Order fulfilled | Review request, Reports |
| `LoyaltyPointsEarnedEvent` | Loyalty | Points awarded | Tier check, Notifications |
| `LoyaltyTierChangedEvent` | Loyalty | Tier upgrade/downgrade | Benefits, Marketing |
| `DishAddedToMenuEvent` | Menu | New dish added | Search index, Cache, AI |
| `DishRemovedFromMenuEvent` | Menu | Dish removed | Search index, Orders validation |
| `SaleRecordedEvent` | Sale | Sale finalized | Analytics, Inventory |
| `DailySalesSummarizedEvent` | Sale | End of day | Reports, AI insights |

---

## Base Classes & Interfaces

### IDomainEvent Interface

**Location:** `SmartMenuOptim.Domain/Services/Contracts/IDomainEvent.cs`

```csharp
public interface IDomainEvent
{
    /// <summary>Unique identifier for idempotency and tracking.</summary>
    Guid EventId { get; }

    /// <summary>UTC timestamp when the event occurred.</summary>
    DateTime OccurredOn { get; }

    /// <summary>Event type name for serialization and routing.</summary>
    string EventType { get; }
}
```

### DomainEventBase Class

**Location:** `SmartMenuOptim.Domain/Events/DomainEventBase.cs`

```csharp
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

### Key Properties Explained

| Property | Purpose | Usage |
|----------|---------|-------|
| `EventId` | Unique identifier | Idempotency, deduplication, tracking |
| `OccurredOn` | Event timestamp | Ordering, auditing, time-based queries |
| `EventType` | Type discrimination | Serialization, routing, filtering |
| `EventVersion` | Schema versioning | Handle event evolution gracefully |
| `CorrelationId` | Request tracing | Track related events across systems |
| `CausationId` | Causal chain | Link events to their cause |

---

## Event Categories

### Order Events

**Location:** `SmartMenuOptim.Domain/Events/OrderEvents/`

#### OrderPlacedEvent

Raised when a customer successfully places an order.

```csharp
public sealed class OrderPlacedEvent : DomainEventBase
{
    public int OrderId { get; init; }
    public int RestaurantId { get; init; }
    public int CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; }
    public int ItemCount { get; init; }
    public string? SpecialInstructions { get; init; }
    public string? OrderType { get; init; }
}
```

**Typical Handlers:**
- Award loyalty points to customer
- Send order to kitchen display
- Update real-time analytics
- Decrement inventory
- Send confirmation notification

#### OrderCancelledEvent

Raised when an order is cancelled.

```csharp
public sealed class OrderCancelledEvent : DomainEventBase
{
    public int OrderId { get; init; }
    public int RestaurantId { get; init; }
    public int CustomerId { get; init; }
    public string CancellationReason { get; init; }
    public CancellationSource CancelledBy { get; init; }
    public decimal OrderTotal { get; init; }
    public bool RequiresRefund { get; init; }
    public int LoyaltyPointsToReverse { get; init; }
}

public enum CancellationSource
{
    Customer, Staff, System
}
```

**Typical Handlers:**
- Reverse loyalty points
- Restore inventory
- Process refund
- Send cancellation notification
- Update cancellation analytics

#### OrderCompletedEvent

Raised when an order is successfully fulfilled.

```csharp
public sealed class OrderCompletedEvent : DomainEventBase
{
    public int OrderId { get; init; }
    public int RestaurantId { get; init; }
    public int CustomerId { get; init; }
    public decimal FinalTotal { get; init; }
    public DateTime OrderPlacedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public double FulfillmentTimeMinutes { get; }
    public int LoyaltyPointsEarned { get; init; }
}
```

**Typical Handlers:**
- Finalize loyalty points
- Request customer review
- Create sale records
- Update completion metrics

---

### Loyalty Events

**Location:** `SmartMenuOptim.Domain/Events/LoyaltyEvents/`

#### LoyaltyPointsEarnedEvent

Raised when a customer earns loyalty points.

```csharp
public sealed class LoyaltyPointsEarnedEvent : DomainEventBase
{
    public int CustomerLoyaltyId { get; init; }
    public int CustomerId { get; init; }
    public int RestaurantId { get; init; }
    public int PointsEarned { get; init; }
    public int NewTotalBalance { get; init; }
    public PointEarningSource EarningSource { get; init; }
    public int? RelatedOrderId { get; init; }
    public decimal PointsMultiplier { get; init; }
}

public enum PointEarningSource
{
    Purchase, Bonus, Referral, Birthday, 
    Review, SignUpBonus, Adjustment, Survey, 
    SocialMedia, Restoration
}
```

**Typical Handlers:**
- Check for tier upgrade
- Send points notification
- Update loyalty analytics
- Check for achievement milestones

#### LoyaltyTierChangedEvent

Raised when a customer's loyalty tier changes.

```csharp
public sealed class LoyaltyTierChangedEvent : DomainEventBase
{
    public int CustomerLoyaltyId { get; init; }
    public int CustomerId { get; init; }
    public int RestaurantId { get; init; }
    public string PreviousTier { get; init; }
    public string NewTier { get; init; }
    public bool IsUpgrade { get; init; }
    public TierChangeReason ChangeReason { get; init; }
    public decimal PreviousTierDiscountPercent { get; init; }
    public decimal NewTierDiscountPercent { get; init; }
    public List<string> BenefitsChanged { get; init; }
}

public enum TierChangeReason
{
    PointsAccumulation, PointsRedemption, PointsExpiration,
    ManualAdjustment, Promotion, SignUpBonus, 
    InactivityReset, AnnualReview
}
```

**Tier Progression:**
- **Bronze:** 0-99 points (base tier)
- **Silver:** 100-499 points (10% discount)
- **Gold:** 500-999 points (15% discount)
- **Platinum:** 1000+ points (20% discount + VIP)

---

### Menu Events

**Location:** `SmartMenuOptim.Domain/Events/MenuEvents/`

#### DishAddedToMenuEvent

Raised when a dish is added to a menu.

```csharp
public sealed class DishAddedToMenuEvent : DomainEventBase
{
    public int MenuId { get; init; }
    public int DishId { get; init; }
    public int RestaurantId { get; init; }
    public string DishName { get; init; }
    public decimal Price { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; }
    public string MenuType { get; init; }
    public bool IsFeatured { get; init; }
    public List<string> DietaryFlags { get; init; }
    public List<string> Allergens { get; init; }
}
```

**Typical Handlers:**
- Update search indexes
- Verify ingredient availability
- Validate pricing
- Notify subscribed customers
- Initialize dish analytics
- Invalidate menu caches
- Update AI recommendation model

#### DishRemovedFromMenuEvent

Raised when a dish is removed from a menu.

```csharp
public sealed class DishRemovedFromMenuEvent : DomainEventBase
{
    public int MenuId { get; init; }
    public int DishId { get; init; }
    public int RestaurantId { get; init; }
    public string DishName { get; init; }
    public DishRemovalReason RemovalReason { get; init; }
    public bool IsPermanent { get; init; }
    public decimal LastPrice { get; init; }
    public int TotalQuantitySold { get; init; }
    public decimal TotalRevenue { get; init; }
    public int DaysOnMenu { get; init; }
}

public enum DishRemovalReason
{
    Discontinued, SeasonalEnd, OutOfStock, 
    Underperforming, MenuRedesign, QualityIssue,
    PricingIssue, SupplierIssue, CustomerFeedback,
    ComplianceIssue, Other
}
```

**Typical Handlers:**
- Remove from search indexes
- Adjust inventory forecasts
- Prevent new orders
- Invalidate caches
- Archive performance data
- Update AI model

---

### Sale Events

**Location:** `SmartMenuOptim.Domain/Events/SaleEvents/`

#### SaleRecordedEvent

Raised when a sale transaction is recorded.

```csharp
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
    public bool IsLunchHour { get; }
    public bool IsDinnerHour { get; }
}
```

**Analytics Properties:**
- `DayOfWeek` - Day analysis
- `HourOfDay` - Peak hours analysis
- `IsLunchHour` - 11 AM - 2 PM
- `IsDinnerHour` - 5 PM - 9 PM

#### DailySalesSummarizedEvent

Raised when daily sales are summarized (typically end of day).

```csharp
public sealed class DailySalesSummarizedEvent : DomainEventBase
{
    public int RestaurantId { get; init; }
    public DateOnly SummaryDate { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public int TotalItemsSold { get; init; }
    public decimal AverageOrderValue { get; }
    public string? TopSellingDish { get; init; }
    public int PeakHour { get; init; }
    public Dictionary<string, decimal> RevenueByCategory { get; init; }
    public decimal? PercentChangeFromPreviousDay { get; }
    public decimal? PercentChangeFromLastWeek { get; }
    public decimal? TargetAchievementPercent { get; }
    public List<string> UnderperformingDishes { get; init; }
}
```

---

## Implementation Patterns

### Pattern 1: Raising Events from Aggregates

Add event collection and raise mechanism to your aggregate base or individual aggregates:

```csharp
// Option A: Add to EntityBase or create AggregateRoot base
public abstract class AggregateRoot : EntityBase
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

### Pattern 2: Raising Events in Order Aggregate

```csharp
public class Order : TenantEntityBase
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void Place()
    {
        // Validate order can be placed
        if (Status != OrderStatusValue.Draft)
            throw new DomainException("Order already placed");
        
        // Change state
        Status = OrderStatusValue.Pending;
        
        // Raise event
        _domainEvents.Add(new OrderPlacedEvent(
            orderId: Id,
            restaurantId: RestaurantId,
            customerId: CustomerId,
            totalAmount: TotalAmount.Amount,
            itemCount: Items.Count,
            currencyCode: TotalAmount.Currency,
            orderType: OrderType
        ));
    }
    
    public void Cancel(string reason, CancellationSource source)
    {
        // Validate cancellation
        if (Status == OrderStatusValue.Completed)
            throw new DomainException("Cannot cancel completed order");
        
        var previousStatus = Status.ToString();
        Status = OrderStatusValue.Cancelled;
        
        _domainEvents.Add(new OrderCancelledEvent(
            orderId: Id,
            restaurantId: RestaurantId,
            customerId: CustomerId,
            cancellationReason: reason,
            cancelledBy: source,
            orderTotal: TotalAmount.Amount,
            previousStatus: previousStatus,
            requiresRefund: true,
            loyaltyPointsToReverse: CalculateLoyaltyPoints()
        ));
    }
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### Pattern 3: Raising Events in CustomerLoyalty Aggregate

```csharp
public class CustomerLoyalty : TenantEntityBase
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public void AddPoints(int points, PointEarningSource source, int? orderId = null)
    {
        if (points <= 0)
            throw new DomainException("Points must be positive");
        
        var previousBalance = TotalPoints;
        var previousTier = CurrentTier;
        
        TotalPoints += points;
        UpdateTier();
        
        // Raise points earned event
        _domainEvents.Add(new LoyaltyPointsEarnedEvent(
            customerLoyaltyId: Id,
            customerId: CustomerId,
            restaurantId: RestaurantId,
            pointsEarned: points,
            previousBalance: previousBalance,
            newTotalBalance: TotalPoints,
            earningSource: source,
            currentTier: CurrentTier.ToString(),
            relatedOrderId: orderId
        ));
        
        // Check for tier change
        if (CurrentTier != previousTier)
        {
            _domainEvents.Add(new LoyaltyTierChangedEvent(
                customerLoyaltyId: Id,
                customerId: CustomerId,
                restaurantId: RestaurantId,
                previousTier: previousTier.ToString(),
                newTier: CurrentTier.ToString(),
                currentPointBalance: TotalPoints,
                changeReason: TierChangeReason.PointsAccumulation
            ));
        }
    }
}
```

---

## Event Handlers

### Handler Location

| Handler Type | Layer | Purpose |
|--------------|-------|---------|
| Business Logic Handlers | Application | Award points, update state |
| Notification Handlers | Application | Send emails, SMS, push |
| Integration Handlers | Infrastructure | Sync with external systems |
| Analytics Handlers | Application | Update metrics, reports |

### Resilient Handler Pattern

All event handlers extend `ResilientEventHandlerBase<TEvent>` which provides:
- **Retry logic** with exponential backoff (3 attempts by default)
- **Dead Letter Queue** for failed events after all retries
- **Comprehensive error logging** with structured context
- **Exception isolation** to prevent cascade failures

### Sample Handler Structure

```csharp
// Application Layer - Using ResilientEventHandlerBase
namespace SmartMenuOptim.Application.Handlers.OrderEventHandlers
{
    public class AwardLoyaltyPointsHandler : ResilientEventHandlerBase<OrderPlacedEvent>, INotificationHandler<OrderPlacedEvent>
    {
        private readonly ICustomerLoyaltyRepository _loyaltyRepo;
        private readonly ILogger<AwardLoyaltyPointsHandler> _logger;
        
        public AwardLoyaltyPointsHandler(
            ICustomerLoyaltyRepository loyaltyRepo,
            ILogger<AwardLoyaltyPointsHandler> logger,
            IDeadLetterQueueService? deadLetterQueue = null)  // Optional DLQ
            : base(logger, deadLetterQueue)
        {
            _loyaltyRepo = loyaltyRepo;
            _logger = logger;
        }
        
        // Override ProcessEventAsync instead of Handle
        protected override async Task ProcessEventAsync(OrderPlacedEvent notification, CancellationToken ct)
        {
            _logger.LogInformation(
                "Processing OrderPlacedEvent: OrderId={OrderId}, CustomerId={CustomerId}",
                notification.OrderId,
                notification.CustomerId);
            
            // Award loyalty points
            var loyalty = await _loyaltyRepo
                .GetByCustomerAndRestaurantAsync(
                    notification.CustomerId, 
                    notification.RestaurantId, 
                    ct);
            
            if (loyalty != null)
            {
                var pointsToAward = CalculatePoints(notification.TotalAmount);
                loyalty.AddPoints(pointsToAward, PointEarningSource.Purchase, notification.OrderId);
                await _loyaltyRepo.UpdateAsync(loyalty, ct);
            }
        }
        
        private int CalculatePoints(decimal orderTotal)
        {
            // 1 point per $1 spent
            return (int)Math.Floor(orderTotal);
        }
    }
}
```

---

## MediatR Integration

### Package Installation

```bash
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
```

### Make Events MediatR Notifications

Update the interface to extend `INotification`:

```csharp
using MediatR;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}
```

### Service Registration

```csharp
// Program.cs or Startup.cs
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(OrderPlacedEventHandler).Assembly);
});
```

### Event Dispatcher

```csharp
// Infrastructure Layer
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventDispatcher> _logger;
    
    public DomainEventDispatcher(IMediator mediator, ILogger<DomainEventDispatcher> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            _logger.LogDebug("Dispatching event: {EventType}, EventId: {EventId}", 
                domainEvent.EventType, 
                domainEvent.EventId);
                
            await _mediator.Publish(domainEvent, ct);
        }
    }
}
```

### Integration in SaveChanges

```csharp
// AppDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Collect domain events before saving
    var domainEvents = ChangeTracker.Entries<AggregateRoot>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();
    
    // Clear events from entities
    foreach (var entry in ChangeTracker.Entries<AggregateRoot>())
    {
        entry.Entity.ClearDomainEvents();
    }
    
    // Save changes first
    var result = await base.SaveChangesAsync(cancellationToken);
    
    // Dispatch events after successful save
    await _eventDispatcher.DispatchEventsAsync(domainEvents, cancellationToken);
    
    return result;
}
```

---

## Testing Domain Events

### Unit Testing Events

```csharp
public class OrderPlacedEventTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var orderId = 1;
        var restaurantId = 10;
        var customerId = 100;
        var totalAmount = 99.99m;
        var itemCount = 3;
        
        // Act
        var @event = new OrderPlacedEvent(
            orderId, restaurantId, customerId, 
            totalAmount, itemCount);
        
        // Assert
        Assert.Equal(orderId, @event.OrderId);
        Assert.Equal(restaurantId, @event.RestaurantId);
        Assert.Equal(customerId, @event.CustomerId);
        Assert.Equal(totalAmount, @event.TotalAmount);
        Assert.Equal(itemCount, @event.ItemCount);
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.True(@event.OccurredOn <= DateTime.UtcNow);
        Assert.Equal("OrderPlacedEvent", @event.EventType);
    }
}
```

### Testing Aggregates Raise Events

```csharp
public class OrderAggregateEventTests
{
    [Fact]
    public void Place_RaisesOrderPlacedEvent()
    {
        // Arrange
        var order = CreateTestOrder();
        
        // Act
        order.Place();
        
        // Assert
        Assert.Single(order.DomainEvents);
        var @event = order.DomainEvents.First() as OrderPlacedEvent;
        Assert.NotNull(@event);
        Assert.Equal(order.Id, @event.OrderId);
        Assert.Equal(order.CustomerId, @event.CustomerId);
    }
    
    [Fact]
    public void Cancel_RaisesOrderCancelledEvent()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Place();
        order.ClearDomainEvents();
        
        // Act
        order.Cancel("Customer request", CancellationSource.Customer);
        
        // Assert
        Assert.Single(order.DomainEvents);
        var @event = order.DomainEvents.First() as OrderCancelledEvent;
        Assert.NotNull(@event);
        Assert.Equal("Customer request", @event.CancellationReason);
    }
}
```

### Testing Event Handlers

```csharp
public class OrderPlacedEventHandlerTests
{
    [Fact]
    public async Task Handle_AwardsLoyaltyPoints()
    {
        // Arrange
        var mockRepo = new Mock<ICustomerLoyaltyRepository>();
        var mockLoyalty = new CustomerLoyalty(customerId: 100, restaurantId: 10);
        mockRepo.Setup(r => r.GetByCustomerAndRestaurantAsync(100, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockLoyalty);
        
        var handler = new OrderPlacedEventHandler(mockRepo.Object, Mock.Of<ILogger<OrderPlacedEventHandler>>());
        
        var @event = new OrderPlacedEvent(1, 10, 100, 50.00m, 2);
        
        // Act
        await handler.Handle(@event, CancellationToken.None);
        
        // Assert
        Assert.Equal(50, mockLoyalty.TotalPoints); // $50 = 50 points
        mockRepo.Verify(r => r.UpdateAsync(mockLoyalty, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## Best Practices

### ✅ DO

1. **Make events immutable** - Use `init` properties
2. **Include all necessary data** - Events should be self-describing
3. **Use past tense naming** - `OrderPlaced` not `PlaceOrder`
4. **Keep events small and focused** - One event per occurrence
5. **Include correlation IDs** - For distributed tracing
6. **Version your events** - For schema evolution
7. **Make handlers idempotent** - Events may be replayed
8. **Log event processing** - For debugging and auditing

### ❌ DON'T

1. **Don't include entity references** - Use IDs instead
2. **Don't modify events after creation** - They're immutable facts
3. **Don't use events for queries** - They're for state changes
4. **Don't create too generic events** - `EntityChanged` is too vague
5. **Don't couple handlers** - Each handler should be independent
6. **Don't throw from handlers without retry support** - Use `ResilientEventHandlerBase` for automatic retry
7. **Don't include sensitive data** - Events may be logged/stored

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Events | Past tense verb + noun | `OrderPlacedEvent` |
| Handlers | Event name + Handler | `OrderPlacedEventHandler` |
| Commands | Verb + noun | `PlaceOrderCommand` |

---

## Future Enhancements

### Planned Improvements

1. **Event Sourcing Support**
   - Store events as source of truth
   - Rebuild state from events
   - Time-travel debugging

2. **Outbox Pattern**
   - Reliable event publishing
   - Transactional consistency
   - At-least-once delivery

3. **Event Store Integration**
   - Azure Event Hubs
   - Apache Kafka
   - EventStoreDB

4. **Saga/Process Manager**
   - Long-running business processes
   - Compensation logic
   - Distributed transactions

5. **Event Versioning**
   - Schema evolution
   - Backward compatibility
   - Event upcasting

### Recently Implemented ✅

1. **Resilient Event Handlers** (v1.2)
   - `ResilientEventHandlerBase<TEvent>` base class
   - Exponential backoff retry strategy (3 attempts)
   - Dead Letter Queue for failed events
   - `IDeadLetterQueueService` contract

2. **In-Memory Dead Letter Queue** (v1.2)
   - `InMemoryDeadLetterQueueService` for development
   - Production: Azure Service Bus DLQ or database-backed implementation

---

## File Structure Summary

```
SmartMenuOptim.Domain/
├── Services/
│   └── Contracts/
│       └── IDomainEvent.cs          # Base interface
├── Events/
│   ├── DomainEventBase.cs           # Base class
│   ├── OrderEvents/
│   │   ├── OrderPlacedEvent.cs
│   │   ├── OrderCancelledEvent.cs
│   │   └── OrderCompletedEvent.cs
│   ├── LoyaltyEvents/
│   │   ├── LoyaltyPointsEarnedEvent.cs
│   │   └── LoyaltyTierChangedEvent.cs
│   ├── MenuEvents/
│   │   ├── DishAddedToMenuEvent.cs
│   │   └── DishRemovedFromMenuEvent.cs
│   └── SaleEvents/
│       ├── SaleRecordedEvent.cs
│       └── DailySalesSummarizedEvent.cs

SmartMenuOptim.Application/
├── Contracts/
│   ├── IDomainEventDispatcher.cs
│   ├── IDeadLetterQueueService.cs   # NEW: Dead letter queue contract
│   └── ...
├── Handlers/
│   ├── ResilientEventHandlerBase.cs # NEW: Base class with retry + DLQ
│   ├── OrderEventHandlers/
│   ├── LoyaltyEventHandlers/
│   ├── MenuEventHandlers/
│   └── SaleEventHandlers/
```

---

## Related Documentation

- [Event Handler Implementation Guide](../EVENT_HANDLER_IMPLEMENTATION.md) - Handler patterns, DI configuration, background jobs
- [Clean Architecture Full Analysis](../CLEAN_ARCHITECTURE_FULL_ANALYSIS.md) - Overall architecture analysis
- [DDD Aggregates Guide](./DDD_AGGREGATES_GUIDE.md) *(coming soon)*
- [CQRS Implementation Guide](./CQRS_IMPLEMENTATION_GUIDE.md) *(coming soon)*

---

**Document Version:** 1.2  
**Last Updated:** 2026-02-08  
**Status:** Implementation Complete ✅

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026 | Initial domain events implementation |
| 1.1 | 2026 | Added event catalog and integration patterns |
| 1.2 | 2026-02-08 | Added `ResilientEventHandlerBase`, `IDeadLetterQueueService`, and retry patterns |
