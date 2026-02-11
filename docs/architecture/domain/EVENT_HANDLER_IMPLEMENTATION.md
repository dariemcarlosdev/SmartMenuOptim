# SmartMenuOptimizer - Event Handler Implementation Guide

## 📋 Document Information

| Field | Value |
|-------|-------|
| **Document Title** | Event Handler Implementation Guide |
| **Version** | 1.3 |
| **Created** | 2026 |
| **Author** | SmartMenuOptimizer Architecture Team |
| **Status** | Implementation Complete |
| **Related Document** | [DOMAIN_EVENTS_GUIDE.md](./DOMAIN_EVENTS_GUIDE.md) |

---

## 📑 Table of Contents

1. [Overview](#overview)
2. [Quick Start Guide](#-quick-start-guide)
3. [Aggregate Event Infrastructure](#-aggregate-event-infrastructure)
4. [Architecture](#architecture)
5. [Handler Catalog](#handler-catalog)
6. [Implementation Details](#implementation-details)
   - [Order Event Handlers](#order-event-handlers)
   - [Loyalty Event Handlers](#loyalty-event-handlers)
   - [Menu Event Handlers](#menu-event-handlers)
   - [Sale Event Handlers](#sale-event-handlers)
7. [Infrastructure Services](#infrastructure-services)
8. [Background Jobs](#background-jobs)
9. [Dependency Injection Configuration](#dependency-injection-configuration)
10. [Error Handling & Resilience](#error-handling--resilience)
11. [Testing Handlers](#testing-handlers)
12. [Production Considerations](#production-considerations)

---

## Overview

This document describes the implementation of domain event handlers in the SmartMenuOptimizer application. Event handlers react to domain events raised by aggregates and perform secondary actions such as sending notifications, updating caches, and triggering analytics.


### Key Concepts

| Concept | Description |
|---------|-------------|
| **Event Handler** | A class that extends `ResilientEventHandlerBase<TEvent>` and implements `INotificationHandler<TEvent>` to react to domain events |
| **ResilientEventHandlerBase** | Abstract base class providing retry logic with exponential backoff and dead letter queue support |
| **MediatR** | The library used for in-process event publishing and handler discovery |
| **Event Dispatcher** | Service that collects events from aggregates and publishes them after persistence |
| **Dead Letter Queue** | Service that stores failed events for later review and retry |
| **Idempotency** | Handlers should be safe to run multiple times for the same event |

### Related Documentation

- 📖 [Domain Events Guide](./DOMAIN_EVENTS_GUIDE.md) - Event definitions and patterns
- 📖 [Clean Architecture Analysis](../CLEAN_ARCHITECTURE_FULL_ANALYSIS.md) - Overall architecture

---

## 🚀 Quick Start Guide

### How Events Are Automatically Dispatched



Events flow automatically through the system when you follow this pattern:

```
1. Aggregate raises event  →  2. SaveChangesAsync()  →  3. Dispatcher publishes  →  4. Handlers execute
```

#### Step 1: Aggregate Raises Event

```csharp
// In your aggregate (e.g., Order.cs)
public class Order : TenantEntityBase
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void Place()
    {
        // Business logic...
        Status = OrderStatus.Pending;
        
        // Raise event - this just adds to the collection
        _domainEvents.Add(new OrderPlacedEvent(
            orderId: Id,
            restaurantId: RestaurantId,
            customerId: CustomerId,
            totalAmount: TotalAmount.Amount,
            itemCount: Items.Count
        ));
    }
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

#### Step 2: AppDbContext Collects & Dispatches Events

**Location:** `SmartMenuOptim.Infrastructure/Persistence/Context/AppDbContext.cs`

The `AppDbContext` is configured to:
1. Accept an optional `IDomainEventDispatcher` via constructor injection
2. Collect domain events from all tracked aggregates before saving
3. Clear events from aggregates to prevent re-dispatch
4. Save changes to database first (atomic transaction)
5. Dispatch events only after successful persistence

```csharp
// In AppDbContext.cs - Constructor with domain event dispatcher
public AppDbContext(DbContextOptions<AppDbContext> options, IDomainEventDispatcher domainEventDispatcher)
    : base(options)
{
    _domainEventDispatcher = domainEventDispatcher;
}

// Override SaveChangesAsync to collect and dispatch domain events
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    SetAuditProperties();
    
    // 1. Collect domain events from all tracked aggregates that support events
    var domainEvents = CollectDomainEvents();
    
    // 2. Clear events from aggregates to prevent re-dispatch
    ClearDomainEventsFromAggregates();

    try
    {
        // 3. Save changes to database FIRST (ensures data consistency)
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        
        // 4. Dispatch events AFTER successful save
        if (domainEvents.Count > 0 && _domainEventDispatcher != null)
        {
            await _domainEventDispatcher.DispatchEventsAsync(domainEvents, cancellationToken)
                .ConfigureAwait(false);
        }
        
        return result;
    }
    catch (DbUpdateConcurrencyException ex)
    {
        throw new DbUpdateConcurrencyException(
            "Concurrency conflict detected while saving changes to the database.", ex);
    }
}

// Collects events from Order, CustomerLoyalty, Menu, and SaleRecord
private List<IDomainEvent> CollectDomainEvents()
{
    var domainEvents = new List<IDomainEvent>();
    
    // Collect from Order aggregates
    domainEvents.AddRange(ChangeTracker.Entries<Order>()
        .Where(e => e.Entity.DomainEvents.Any())
        .SelectMany(e => e.Entity.DomainEvents));
    
    // Collect from CustomerLoyalty aggregates
    domainEvents.AddRange(ChangeTracker.Entries<CustomerLoyalty>()
        .Where(e => e.Entity.DomainEvents.Any())
        .SelectMany(e => e.Entity.DomainEvents));
    
    // Collect from Menu aggregates
    domainEvents.AddRange(ChangeTracker.Entries<Menu>()
        .Where(e => e.Entity.DomainEvents.Any())
        .SelectMany(e => e.Entity.DomainEvents));
    
    // Collect from SaleRecord entities
    domainEvents.AddRange(ChangeTracker.Entries<SaleRecord>()
        .Where(e => e.Entity.DomainEvents.Any())
        .SelectMany(e => e.Entity.DomainEvents));
    
    return domainEvents;
}
```

> **📌 Note:** When adding new aggregates that raise domain events, remember to update both `CollectDomainEvents()` and `ClearDomainEventsFromAggregates()` methods in `AppDbContext`.

#### Step 3: Handlers Execute Automatically

MediatR automatically discovers and executes all handlers for each event:

```csharp
// All handlers implementing INotificationHandler<OrderPlacedEvent> run
// ✅ AwardLoyaltyPointsHandler.Handle() 
// ✅ SendKitchenNotificationHandler.Handle()
// ✅ SendOrderConfirmationHandler.Handle()
// ✅ UpdateOrderAnalyticsHandler.Handle()
```

---

### Handler Execution Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  ORDER SERVICE / CONTROLLER                                     │
│                                                                 │
│  var order = new Order(...);                                    │
│  order.Place();  // ← Event added to _domainEvents             │
│  await _repository.AddAsync(order);                             │
│  await _unitOfWork.SaveChangesAsync();  // ← Triggers dispatch │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  AppDbContext.SaveChangesAsync()                                │
│                                                                 │
│  1. Collect events from Order.DomainEvents                      │
│  2. order.ClearDomainEvents()                                   │
│  3. await base.SaveChangesAsync() ← DB Transaction              │
│  4. await _dispatcher.DispatchEventsAsync(events)               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  MediatRDomainEventDispatcher                                   │
│                                                                 │
│  foreach (var event in events)                                  │
│      await _mediator.Publish(event);  // ← MediatR routing     │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ AwardLoyalty    │ │ SendKitchen     │ │ SendConfirm     │
│ PointsHandler   │ │ Notification    │ │ Handler         │
│                 │ │ Handler         │ │                 │
│ Runs in         │ │ Runs in         │ │ Runs in         │
│ parallel        │ │ parallel        │ │ parallel        │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

---

### 🧪 How to Test Event Dispatching

#### Quick Manual Test

```csharp
// In a controller or service, trigger an event manually:
[HttpPost("test-event")]
public async Task<IActionResult> TestEventDispatch([FromServices] IMediator mediator)
{
    var testEvent = new OrderPlacedEvent(
        orderId: 999,
        restaurantId: 1,
        customerId: 100,
        totalAmount: 50.00m,
        itemCount: 3
    );
    
    // This will trigger ALL registered handlers
    await mediator.Publish(testEvent);
    
    return Ok("Event dispatched - check logs for handler execution");
}
```

#### Unit Test a Handler

```csharp
[Fact]
public async Task AwardLoyaltyPointsHandler_CalculatesPointsCorrectly()
{
    // Arrange
    var logger = new Mock<ILogger<AwardLoyaltyPointsHandler>>();
    var handler = new AwardLoyaltyPointsHandler(logger.Object);
    
    var @event = new OrderPlacedEvent(
        orderId: 1,
        restaurantId: 10,
        customerId: 100,
        totalAmount: 75.50m,  // Should award 75 points
        itemCount: 5
    );

    // Act
    await handler.Handle(@event, CancellationToken.None);

    // Assert - Check logs were called
    logger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("75")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ),
        Times.AtLeastOnce
    );
}
```

#### Integration Test - Full Flow

```csharp
[Fact]
public async Task OrderPlaced_TriggersAllHandlers()
{
    // Arrange - Use test host with real DI
    await using var factory = new WebApplicationFactory<Program>();
    using var scope = factory.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    
    var @event = new OrderPlacedEvent(1, 10, 100, 99.99m, 3);

    // Act
    await mediator.Publish(@event);

    // Assert - Handlers executed (check via mock services or logs)
    // In real tests, inject mock INotificationService and verify calls
}
```

#### Verify Handlers Are Registered

```csharp
[Fact]
public void AllEventHandlers_AreRegistered()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddApplicationServices();  // Your registration method
    services.AddLogging();
    
    var provider = services.BuildServiceProvider();

    // Act & Assert - Resolve handlers
    var handlers = provider.GetServices<INotificationHandler<OrderPlacedEvent>>();
    
    Assert.Equal(4, handlers.Count()); // 4 handlers for OrderPlacedEvent
}
```

---

## 🏗️ Aggregate Event Infrastructure

This section documents how domain events are raised from aggregates. All aggregates follow the same pattern for consistency and maintainability.

> **📌 Pattern Note:** The following event infrastructure pattern is used consistently across all aggregates. Future aggregate implementations should follow this same pattern to ensure uniformity in event handling.

### Event Flow (Detailed)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  1. BUSINESS METHOD CALLED                                                  │
│     e.g., order.Place(), loyalty.AddPoints(), menu.AddDish()               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  2. AGGREGATE VALIDATES AND UPDATES STATE                                   │
│     • Validates business rules                                              │
│     • Updates internal state (properties, child entities)                   │
│     • Sets UpdatedAt timestamp                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  3. AddDomainEvent(new XxxEvent(...)) CALLED                               │
│     • Creates strongly-typed event with all relevant data                   │
│     • Event immutable once created (init-only properties)                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  4. EVENT STORED IN _domainEvents COLLECTION                               │
│     • Event sits in memory, not yet dispatched                              │
│     • Multiple events can accumulate from same operation                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  5. AppDbContext.SaveChangesAsync() COLLECTS EVENTS                        │
│     • Iterates all tracked aggregates with DomainEvents                     │
│     • Collects events into single list                                      │
│     • Clears events from aggregates (prevent re-dispatch)                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  6. DATABASE TRANSACTION COMPLETES                                          │
│     • All entity changes persisted atomically                               │
│     • If transaction fails, events are NOT dispatched                       │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  7. IDomainEventDispatcher.DispatchEventsAsync() PUBLISHES TO MediatR      │
│     • Loops through collected events                                        │
│     • Calls _mediator.Publish(event) for each                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  8. ALL REGISTERED INotificationHandler<XxxEvent> EXECUTE IN PARALLEL      │
│     • MediatR discovers all handlers for event type                         │
│     • Handlers run concurrently (default behavior)                          │
│     • Each handler has try-catch to prevent cascade failures                │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### Order Aggregate (`Order.cs`)

**Location:** `SmartMenuOptim.Domain/Aggregates/OrderAggregate/Order.cs`

**Infrastructure Added:**

```csharp
// Domain events collection
private readonly List<IDomainEvent> _domainEvents = new();

[NotMapped]
public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

public void ClearDomainEvents() => _domainEvents.Clear();

protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
```

**Event-Raising Methods:**

| Method | Event Raised | Description |
|--------|--------------|-------------|
| `Place(confirmedStatusId, orderType)` | `OrderPlacedEvent` | Transitions order to confirmed, triggers kitchen/loyalty/notification handlers |
| `Cancel(cancelledStatusId, reason, cancelledBy, ...)` | `OrderCancelledEvent` | Handles cancellation with reason tracking, refund initiation |
| `Complete(completedStatusId, loyaltyPointsEarned, orderType)` | `OrderCompletedEvent` | Finalizes order, triggers thank-you notification and review requests |

**Example - Place() Method:**

```csharp
public void Place(int confirmedStatusId, string? orderType = null)
{
    if (!_orderItems.Any())
        throw new InvalidOperationException("Cannot place an order without items.");
    
    OrderStatusId = confirmedStatusId;
    UpdatedAt = DateTime.UtcNow;
    
    AddDomainEvent(new OrderPlacedEvent(
        orderId: Id,
        restaurantId: RestaurantId,
        customerId: CustomerId,
        totalAmount: TotalAmount,
        itemCount: _orderItems.Count,
        currencyCode: "USD",
        specialInstructions: SpecialInstructions,
        orderType: orderType
    ));
}
```

---

### CustomerLoyalty Aggregate (`CustomerLoyalty.cs`)

**Location:** `SmartMenuOptim.Domain/Aggregates/CustomerLoyaltyAggregate/CustomerLoyalty.cs`

**Infrastructure Added:**

```csharp
private readonly List<IDomainEvent> _domainEvents = new();

[NotMapped]
public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

public void ClearDomainEvents() => _domainEvents.Clear();

protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
```

**Event-Raising Methods:**

| Method | Event(s) Raised | Description |
|--------|-----------------|-------------|
| `AddPoints(points, description, ...)` | `LoyaltyPointsEarnedEvent` + optionally `LoyaltyTierChangedEvent` | Awards points and automatically detects tier changes |

**Helper Methods Added:**

| Method | Purpose |
|--------|---------|
| `MapTransactionTypeToEarningSource()` | Converts `LoyaltyTransactionType` to `PointEarningSource` enum for events |
| `RaiseTierChangedEvent()` | Creates and adds `LoyaltyTierChangedEvent` with full tier details |
| `GetTierDiscount()` | Returns discount percentage for a given tier |
| `GetBenefitsForTierChange()` | Lists benefits gained/lost during tier transitions |

**Example - AddPoints() with Automatic Tier Detection:**

```csharp
public void AddPoints(int points, string description, 
    LoyaltyTransactionType transactionType = LoyaltyTransactionType.OrderEarning,
    int? orderId = null, decimal? orderAmount = null, decimal pointsMultiplier = 1.0m)
{
    var previousTier = Tier;
    
    // ... point addition logic ...
    
    UpdateTier();
    
    // Raise LoyaltyPointsEarnedEvent
    AddDomainEvent(new LoyaltyPointsEarnedEvent(...));
    
    // Automatically raise LoyaltyTierChangedEvent if tier changed
    if (Tier != previousTier)
    {
        RaiseTierChangedEvent(previousTier, Tier, TierChangeReason.PointsAccumulation);
    }
}
```

---

### Menu Aggregate (`Menu.cs`)

**Location:** `SmartMenuOptim.Domain/Aggregates/MenuAggregate/Menu.cs`

**Infrastructure Added:**

```csharp
private readonly List<IDomainEvent> _domainEvents = new();

[NotMapped]
public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

public void ClearDomainEvents() => _domainEvents.Clear();

protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
```

**Event-Raising Methods:**

| Method | Event Raised | Description |
|--------|--------------|-------------|
| `AddDish(dish, displayOrder, specialPrice, notes)` | `DishAddedToMenuEvent` | Triggers cache invalidation and search index updates |
| `RemoveDish(dishId, reason, removedByStaffId)` | `DishRemovedFromMenuEvent` | Archives performance data, updates caches with removal reason |

**Example - RemoveDish() with Reason Tracking:**

```csharp
public void RemoveDish(int dishId, DishRemovalReason reason = DishRemovalReason.Other, 
    int? removedByStaffId = null)
{
    var menuDish = _menuDishes.FirstOrDefault(md => md.DishId == dishId);
    if (menuDish != null)
    {
        var dish = menuDish.Dish;
        _menuDishes.Remove(menuDish);
        UpdatedAt = DateTime.UtcNow;
        
        if (dish != null)
        {
            AddDomainEvent(new DishRemovedFromMenuEvent(
                menuId: Id,
                dishId: dishId,
                restaurantId: RestaurantId,
                dishName: dish.Name,
                menuName: Name,
                removalReason: reason,
                isPermanent: reason == DishRemovalReason.Discontinued,
                lastPrice: menuDish.SpecialPrice ?? dish.DishPrice,
                categoryName: dish.Category?.Name ?? "Unknown",
                removedByStaffId: removedByStaffId
            ));
        }
    }
}
```

---

### SaleRecord Entity (`SaleRecord.cs`)

**Location:** `SmartMenuOptim.Domain/Entities/RestaurantEntities/SaleRecord.cs`

> **Note:** Although `SaleRecord` is a Tier 2 entity (not a full aggregate), it follows the same event infrastructure pattern for consistency.

**Infrastructure Added:**

```csharp
private readonly List<IDomainEvent> _domainEvents = new();

[NotMapped]
public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

public void ClearDomainEvents() => _domainEvents.Clear();

protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
```

**Factory Method Added:**

| Method | Event Raised | Description |
|--------|--------------|-------------|
| `RecordSale(...)` (static factory) | `SaleRecordedEvent` | Creates sale record with full context and raises event |

**Example - RecordSale() Factory Method:**

```csharp
public static SaleRecord RecordSale(
    int restaurantId, int orderId, int dishId, string dishName, string categoryName,
    Money saleAmount, decimal unitPrice, int quantitySold,
    int? customerId = null, int? processedByStaffId = null, string? orderType = null)
{
    var record = new SaleRecord(restaurantId, dishId, saleAmount, quantitySold);
    
    record.AddDomainEvent(new SaleRecordedEvent(
        saleRecordId: record.Id,
        restaurantId: restaurantId,
        orderId: orderId,
        dishId: dishId,
        dishName: dishName,
        categoryName: categoryName,
        quantitySold: quantitySold,
        unitPrice: unitPrice,
        totalAmount: saleAmount.Amount,
        saleDateTime: record.SaleDate,
        currencyCode: saleAmount.Currency,
        customerId: customerId,
        processedByStaffId: processedByStaffId,
        orderType: orderType
    ));
    
    return record;
}
```

---

### 📋 Implementation Checklist for New Aggregates

When implementing event infrastructure in a new aggregate, follow this checklist:

- [ ] **Add private `_domainEvents` collection**
  ```csharp
  private readonly List<IDomainEvent> _domainEvents = new();
  ```

- [ ] **Add `DomainEvents` read-only property with `[NotMapped]`**
  ```csharp
  [NotMapped]
  public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
  ```

- [ ] **Add `ClearDomainEvents()` method**
  ```csharp
  public void ClearDomainEvents() => _domainEvents.Clear();
  ```

- [ ] **Add `AddDomainEvent()` protected method**
  ```csharp
  protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
  ```

- [ ] **Add required using statements**
  ```csharp
  using SmartMenuOptim.Domain.Events.XxxEvents;
  using SmartMenuOptim.Domain.Services.Contracts;
  using System.ComponentModel.DataAnnotations.Schema;
  ```

- [ ] **Identify business methods that should raise events**

- [ ] **Create corresponding event classes** (see [DOMAIN_EVENTS_GUIDE.md](./DOMAIN_EVENTS_GUIDE.md))

- [ ] **Call `AddDomainEvent()` at the end of each business method**

- [ ] **Ensure `AppDbContext` collects events from the new aggregate type**

---

## Architecture

### Event Flow Diagram. Example: OrderPlacedEvent. Same pattern is applied to all events raised from aggregates.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              DOMAIN LAYER                                   │
│                                                                             │
│  ┌─────────────────────┐         ┌─────────────────────────────────────┐   │
│  │   Order Aggregate   │────────▶│  OrderPlacedEvent                   │   │
│  │   (raises event)    │         │  • OrderId: 1234                    │   │
│  └─────────────────────┘         │  • CustomerId: 567                  │   │
│                                  │  • TotalAmount: $99.50              │   │
│                                  └─────────────────────────────────────┘   │
└───────────────────────────────────────────┬─────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          INFRASTRUCTURE LAYER                               │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    AppDbContext.SaveChangesAsync()                   │   │
│  │                                                                      │   │
│  │  1. Collect events from tracked aggregates                          │   │
│  │  2. Save changes to database                                        │   │
│  │  3. Call IDomainEventDispatcher.DispatchEventsAsync()               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                            │                                │
│                                            ▼                                │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    MediatRDomainEventDispatcher                      │   │
│  │                                                                      │   │
│  │  foreach (event in events)                                          │   │
│  │      await _mediator.Publish(event);                                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────┬─────────────────────────────────┘
                                            │
                                            ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           APPLICATION LAYER                                 │
│                                                                             │
│  ┌─────────────────────┐  ┌─────────────────────┐  ┌────────────────────┐  │
│  │ AwardLoyaltyPoints  │  │ SendKitchenNotif    │  │ SendOrderConfirm   │  │
│  │      Handler        │  │      Handler        │  │      Handler       │  │
│  │                     │  │                     │  │                    │  │
│  │ • Calculate points  │  │ • Send to kitchen   │  │ • Send to customer │  │
│  │ • Update loyalty    │  │ • Display on screen │  │ • Email/SMS/Push   │  │
│  └─────────────────────┘  └─────────────────────┘  └────────────────────┘  │
│             │                       │                        │              │
│             ▼                       ▼                        ▼              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    All handlers run in parallel                      │  │
│  │                    (MediatR default behavior)                        │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Raises events, defines event contracts |
| **Application** | Contains event handlers with business logic |
| **Infrastructure** | Event dispatcher, notification/cache service implementations |

---

## Handler Catalog

### Quick Reference

> **Note:** All handlers extend `ResilientEventHandlerBase<TEvent>` which provides retry logic and dead letter queue support. The `IDeadLetterQueueService` is injected as an optional parameter.

| Handler | Event | Purpose | Dependencies |
|---------|-------|---------|--------------|
| **AwardLoyaltyPointsHandler** | `OrderPlacedEvent` | Award loyalty points | Logger, IDeadLetterQueueService? |
| **SendKitchenNotificationHandler** | `OrderPlacedEvent` | Notify kitchen | INotificationService, IDeadLetterQueueService? |
| **SendOrderConfirmationHandler** | `OrderPlacedEvent` | Confirm to customer | INotificationService, IDeadLetterQueueService? |
| **UpdateOrderAnalyticsHandler** | `OrderPlacedEvent` | Update analytics | ICacheService, IDeadLetterQueueService? |
| **OrderCancelledHandler** | `OrderCancelledEvent` | Handle cancellation | INotificationService, ICacheService, IDeadLetterQueueService? |
| **OrderCompletedHandler** | `OrderCompletedEvent` | Finalize order | INotificationService, ICacheService, IDeadLetterQueueService? |
| **LoyaltyPointsEarnedHandler** | `LoyaltyPointsEarnedEvent` | Notify points | INotificationService, IDeadLetterQueueService? |
| **LoyaltyTierChangedHandler** | `LoyaltyTierChangedEvent` | Tier change | INotificationService, IDeadLetterQueueService? |
| **DishAddedToMenuHandler** | `DishAddedToMenuEvent` | Update cache/index | ICacheService, IDeadLetterQueueService? |
| **DishRemovedFromMenuHandler** | `DishRemovedFromMenuEvent` | Clean up | ICacheService, IDeadLetterQueueService? |
| **SaleRecordedHandler** | `SaleRecordedEvent` | Log analytics | ICacheService, IDeadLetterQueueService? |
| **DailySalesSummarizedHandler** | `DailySalesSummarizedEvent` | Reports & alerts | INotificationService, ICacheService, IDeadLetterQueueService? |

---

## Implementation Details

### Order Event Handlers

#### AwardLoyaltyPointsHandler

**Location:** `SmartMenuOptim.Application/Handlers/OrderEventHandlers/AwardLoyaltyPointsHandler.cs`

**Purpose:** Awards loyalty points to customers based on their order total.

**Base Class:** `ResilientEventHandlerBase<OrderPlacedEvent>` (provides retry + DLQ)

```csharp
public class AwardLoyaltyPointsHandler : ResilientEventHandlerBase<OrderPlacedEvent>, INotificationHandler<OrderPlacedEvent>
{
    protected override async Task ProcessEventAsync(OrderPlacedEvent notification, CancellationToken ct)
    {
        // Calculate: 1 point per $1 spent
        var points = (int)Math.Floor(notification.TotalAmount);
        
        // Award points to customer's loyalty account
        // This would retrieve the CustomerLoyalty aggregate and call AddPoints()
    }
}
```

**Business Rules:**
- 1 point per $1 spent (configurable)
- Only active loyalty members receive points
- Points may be multiplied during promotions

---

#### SendKitchenNotificationHandler

**Location:** `SmartMenuOptim.Application/Handlers/OrderEventHandlers/SendKitchenNotificationHandler.cs`

**Purpose:** Sends real-time notification to kitchen display system.

**Base Class:** `ResilientEventHandlerBase<OrderPlacedEvent>` (provides retry + DLQ)

```csharp
public class SendKitchenNotificationHandler : ResilientEventHandlerBase<OrderPlacedEvent>, INotificationHandler<OrderPlacedEvent>
{
    private readonly INotificationService _notificationService;
    
    protected override async Task ProcessEventAsync(OrderPlacedEvent notification, CancellationToken ct)
    {
        await _notificationService.SendKitchenOrderAsync(
            notification.RestaurantId,
            notification.OrderId,
            notification.ItemCount,
            notification.SpecialInstructions,
            ct);
    }
}
```

**Integration Points:**
- Kitchen Display System (KDS)
- SignalR for real-time updates
- Printer integration for tickets

---

#### SendOrderConfirmationHandler

**Location:** `SmartMenuOptim.Application/Handlers/OrderEventHandlers/SendOrderConfirmationHandler.cs`

**Purpose:** Sends order confirmation to customer.

**Notification Channels:**
- Email (primary)
- SMS (if opted in)
- Push notification (mobile app)
- In-app notification

**Integration Points:**
- SendGrid for email
- Twilio for SMS
- Firebase for push notifications
- SignalR for in-app notifications


---

#### UpdateOrderAnalyticsHandler

**Location:** `SmartMenuOptim.Application/Handlers/OrderEventHandlers/UpdateOrderAnalyticsHandler.cs`

**Purpose:** Updates real-time analytics dashboards.

**Metrics Updated:**
- Order count
- Revenue
- Average order value
- Orders by hour/day

**Integration Points:**
- Redis cache for real-time metrics
- SignalR for dashboard updates
- Background service for periodic aggregation
- Logging for historical analysis


---

#### OrderCancelledHandler

**Location:** `SmartMenuOptim.Application/Handlers/OrderEventHandlers/OrderCancelledHandler.cs`

**Purpose:** Coordinates all cancellation-related actions.

**Base Class:** `ResilientEventHandlerBase<OrderCancelledEvent>` (provides retry + DLQ)

```csharp
public class OrderCancelledHandler : ResilientEventHandlerBase<OrderCancelledEvent>, INotificationHandler<OrderCancelledEvent>
{
    protected override async Task ProcessEventAsync(OrderCancelledEvent notification, CancellationToken ct)
    {
        var tasks = new List<Task>();
        
        // 1. Send cancellation notification
        tasks.Add(SendCancellationNotificationAsync(notification, ct));
        
        // 2. Reverse loyalty points
        if (notification.LoyaltyPointsToReverse > 0)
            tasks.Add(ReverseLoyaltyPointsAsync(notification, ct));
        
        // 3. Update analytics
        tasks.Add(UpdateCancellationAnalyticsAsync(notification, ct));
        
        // 4. Notify kitchen
        tasks.Add(NotifyKitchenOfCancellationAsync(notification, ct));
        
        // Execute all concurrently
        await Task.WhenAll(tasks);
    }
}
```

**Cancellation Reasons Tracked:**
- Customer request
- Restaurant issue
- Payment failure
- No-show
- Other (with free-text reason)
- Refund processing (if applicable)
- Loyalty points reversal (if applicable)
- Analytics update for cancellation patterns
- Kitchen notification for order removal
- Customer notification with cancellation reason and next steps
- Restaurant staff notification for operational awareness
- Logging for audit and future analysis
- Potential future integration with third-party cancellation management tools
- Customer feedback collection on cancellation experience (optional)
- Automated follow-up for cancellations to encourage re-engagement (optional)

---

#### OrderCompletedHandler

**Location:** `SmartMenuOptim.Application/Handlers/OrderEventHandlers/OrderCompletedHandler.cs`

**Purpose:** Finalizes order and triggers post-order actions.

**Actions:**
- Log fulfillment metrics
- Send thank you notification
- Schedule review request
- Finalize loyalty points

**Order Completion Metrics:**
- Time from placement to completion
- Customer satisfaction (via review requests)
- Repeat purchase rate (tracked via customer ID)
- Revenue recognition for completed orders
- Potential future integration with CRM for customer lifecycle management
- Automated marketing campaigns based on completed orders (e.g., cross-sell, upsell)
- Customer segmentation based on order history and completion patterns
- Operational insights for kitchen efficiency and staffing based on completion times
- Data collection for machine learning models to predict order completion and customer behavior (optional)

**Integration Points:**
- Integration with third-party review platforms (e.g., Yelp, Google Reviews) for automated review requests and reputation management (optional)
- Integration with CRM systems (e.g., Salesforce, HubSpot) for enhanced customer relationship management and targeted marketing based on order completion data (optional)
  - Integration with marketing automation platforms (e.g., Mailchimp, Marketo) for post-order engagement campaigns, such as personalized offers or loyalty program promotions (optional)

---

### Loyalty Event Handlers

#### LoyaltyPointsEarnedHandler

**Location:** `SmartMenuOptim.Application/Handlers/LoyaltyEventHandlers/LoyaltyPointsEarnedHandler.cs`

**Purpose:** Notifies customer and checks for milestones.

**Milestones Tracked:**
- 100 points
- 250 points
- 500 points
- 1,000 points
- 2,500 points
- 5,000 points
- 10,000 points

**Notification Content:**
- Points earned
- Current balance
- Milestone achievements
- Personalized messages based on customer preferences and history
- Potential future integration with gamification platforms for enhanced customer engagement and loyalty program management (optional)
- Data collection for machine learning models to predict customer loyalty and optimize rewards (optional)
- Automated tier upgrades and personalized offers based on loyalty points and customer segmentation (optional)
- Customer feedback collection on loyalty program experience and rewards (optional)
- Automated follow-up for customers reaching milestones to encourage continued engagement and reward redemption (optional)

**integration Points:**
- Integration with third-party loyalty program management platforms for enhanced features and scalability (optional)
- Integration with social media platforms for sharing milestones and rewards, enhancing brand visibility and customer engagement (optional)
- Integration with mobile app features for real-time loyalty updates and personalized offers based on location and behavior (optional)

---

#### LoyaltyTierChangedHandler

**Location:** `SmartMenuOptim.Application/Handlers/LoyaltyEventHandlers/LoyaltyTierChangedHandler.cs`

**Purpose:** Manages tier change notifications and benefit activation.

**Tier Benefits:**

| Tier | Points | Discount | Additional Benefits |
|------|--------|----------|---------------------|
| Bronze | 0-99 | 0% | Newsletter |
| Silver | 100-499 | 10% | Birthday reward |
| Gold | 500-999 | 15% | Priority seating |
| Platinum | 1000+ | 20% | VIP access, free delivery |

**Notification Content:**
- Previous tier and new tier
- Benefits gained/lost
- Personalized message acknowledging the change and encouraging continued engagement
- Potential future integration with gamification platforms for enhanced customer engagement and loyalty program management (optional)
- Data collection for machine learning models to predict customer loyalty and optimize rewards (optional)
- Automated personalized offers and rewards based on tier changes and customer segmentation (optional)
- Customer feedback collection on tier change experience and benefits (optional)
- Automated follow-up for customers experiencing tier changes to encourage continued engagement and reward redemption (optional)

**Integration Points:**
- Integration with third-party loyalty program management platforms for enhanced features and scalability (optional)
- Integration with social media platforms for sharing tier changes and rewards, enhancing brand visibility and customer engagement (optional)
- Integration with mobile app features for real-time tier updates and personalized offers based on location and behavior (optional)
- Integration with CRM systems for enhanced customer relationship management and targeted marketing based on tier changes (optional)

---

### Menu Event Handlers

#### DishAddedToMenuHandler

**Location:** `SmartMenuOptim.Application/Handlers/MenuEventHandlers/DishAddedToMenuHandler.cs`

**Purpose:** Updates caches and initializes tracking for new dishes.

**Actions:**
- Invalidate menu cache
- Update search indexes
- Initialize dish performance tracking
- Log addition details

**Integration Points:**
- Search indexing service (e.g., Elasticsearch) for real-time search updates
- Analytics service for tracking new dish performance from day one
- Potential future integration with AI-driven menu optimization tools to analyze new dish performance and provide recommendations (optional)
- Customer feedback collection on new dishes to inform menu decisions and improvements (optional)
- Automated marketing campaigns to promote new dishes based on customer preferences and behavior (optional)
- Integration with third-party review platforms for collecting reviews and ratings on new dishes, enhancing reputation management and customer engagement (optional)
- Integration with CRM systems for enhanced customer relationship management and targeted marketing based on new dish performance and customer segmentation (optional)
- Integration with mobile app features for real-time updates on new dishes and personalized offers based on customer preferences and behavior (optional)

---

#### DishRemovedFromMenuHandler

**Location:** `SmartMenuOptim.Application/Handlers/MenuEventHandlers/DishRemovedFromMenuHandler.cs`

**Purpose:** Cleans up and archives data for removed dishes.

**Actions:**
- Invalidate menu cache
- Remove from search indexes
- Archive performance data
- Log removal analytics

---

### Sale Event Handlers

#### SaleRecordedHandler

**Location:** `SmartMenuOptim.Application/Handlers/SaleEventHandlers/SaleRecordedHandler.cs`

**Purpose:** Updates real-time analytics for individual sales.

**Analytics Captured:**
- Dish performance
- Time-of-day patterns
- Category performance
- Discount effectiveness

**notification Content:**
- Real-time updates for restaurant managers on sales performance
- Insights on best/worst performing dishes and categories
- Trends in customer preferences and behavior based on sales data

**Integration Points:**
- Real-time analytics dashboard for restaurant managers
- Data warehouse for historical analysis and machine learning models
- Potential future integration with AI-driven sales forecasting tools to predict demand and optimize inventory and staffing (optional)
- Customer feedback collection on dishes and dining experience to inform menu decisions and improvements (optional)
- Automated marketing campaigns based on sales data and customer preferences (optional)
- Integration with third-party review platforms for collecting reviews and ratings on dishes, enhancing reputation management and customer engagement (optional)
- Integration with CRM systems for enhanced customer relationship management and targeted marketing based on sales data and customer segmentation (optional)
- Integration with mobile app features for real-time updates on sales performance and personalized offers based on customer preferences and behavior (optional)

---

#### DailySalesSummarizedHandler

**Location:** `SmartMenuOptim.Application/Handlers/SaleEventHandlers/DailySalesSummarizedHandler.cs`

**Purpose:** Processes daily summaries and generates alerts.

**Alerts Generated:**
- Revenue drop > 20% vs last week
- Target achievement < 80%
- Cancellation rate > 10%
- Multiple underperforming dishes

**Integration Points:**
- Real-time analytics dashboard for restaurant managers
- Automated email/SMS alerts for critical performance issues
- Potential future integration with AI-driven anomaly detection tools to identify unusual patterns in sales data and generate proactive alerts (optional)
- Customer feedback collection on dining experience to inform improvements and address issues (optional)
- Automated marketing campaigns to address performance issues, such as targeted promotions or discounts based on sales data and customer preferences (optional)
- Integration with third-party review platforms for collecting reviews and ratings on dishes and dining experience, enhancing reputation management and customer engagement (optional)
- Integration with CRM systems for enhanced customer relationship management and targeted marketing based on sales performance and customer segmentation (optional)
- Integration with mobile app features for real-time updates on sales performance and personalized offers based on customer preferences and behavior (optional)
- Integration with operational tools for staffing and inventory management based on sales performance and trends (optional)
- Integration with financial systems for revenue recognition and financial reporting based on sales data (optional)

**Notification Content:**
- Daily performance summary for restaurant managers
- Alerts on critical performance issues with actionable insights
- Trends and patterns in sales data to inform operational and marketing decisions
- Potential future integration with AI-driven recommendation tools to provide actionable insights and recommendations based on sales data and performance trends (optional)
- Customer feedback collection on dining experience to inform improvements and address issues (optional)
- Automated marketing campaigns to address performance issues, such as targeted promotions or discounts based on sales data and customer preferences (optional)


---

## Infrastructure Services

### IDomainEventDispatcher

**Location:** `SmartMenuOptim.Application/Contracts/IDomainEventDispatcher.cs`

**Implementation:** `SmartMenuOptim.Infrastructure/EventDispatching/MediatRDomainEventDispatcher.cs`

```csharp
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
    Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
```

---

### INotificationService

**Location:** `SmartMenuOptim.Application/Contracts/INotificationService.cs`

**Development Implementation:** `LoggingNotificationService` (logs notifications)

**Production Implementations (future):**
- `SendGridEmailNotificationService`
- `TwilioSmsNotificationService`
- `SignalRNotificationService`
- `FirebasePushNotificationService`

```csharp
public interface INotificationService
{
    Task SendToCustomerAsync(int customerId, string title, string message, 
                              NotificationType type, CancellationToken ct = default);
    Task SendToRestaurantStaffAsync(int restaurantId, string title, string message,
                                     NotificationType type, CancellationToken ct = default);
    Task SendOrderConfirmationAsync(int customerId, int orderId, decimal total, CancellationToken ct = default);
    Task SendOrderCancellationAsync(int customerId, int orderId, string reason, CancellationToken ct = default);
    Task SendLoyaltyPointsEarnedAsync(int customerId, int points, int balance, CancellationToken ct = default);
    Task SendLoyaltyTierChangedAsync(int customerId, string prev, string newTier, bool isUpgrade, CancellationToken ct = default);
    Task SendKitchenOrderAsync(int restaurantId, int orderId, int items, string? instructions, CancellationToken ct = default);
}
```

---

### ICacheService

**Location:** `SmartMenuOptim.Application/Contracts/ICacheService.cs`

**Development Implementation:** `InMemoryCacheService` (in-memory cache)

**Production Implementation:** `RedisCacheService` (for distributed scenarios)

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
    Task InvalidateMenuCacheAsync(int restaurantId, CancellationToken ct = default);
    Task InvalidateAnalyticsCacheAsync(int restaurantId, CancellationToken ct = default);
}
```

---

## Background Jobs

### DailySalesSummaryBackgroundJob

**Location:** `SmartMenuOptim.Infrastructure/BackgroundJobs/DailySalesSummaryBackgroundJob.cs`

**Schedule:** Daily at 2:00 AM UTC

**Purpose:** Aggregates daily sales data and publishes `DailySalesSummarizedEvent` for each restaurant.

```csharp
public class DailySalesSummaryBackgroundJob : BackgroundService
{
    private readonly TimeSpan _runTime = new(2, 0, 0); // 2:00 AM
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetNextRunTime(DateTime.UtcNow) - DateTime.UtcNow;
            await Task.Delay(delay, stoppingToken);
            
            await RunDailySummaryAsync(stoppingToken);
        }
    }
}
```

**Production Recommendations:**
- Use Hangfire for more robust scheduling
- Use Azure Functions with Timer Trigger
- Implement retry logic with exponential backoff

---

## Dependency Injection Configuration

### Application Layer Registration

**Location:** `SmartMenuOptim.Application/Extensions/ServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    // Register MediatR and all handlers
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
    });

    // Register FluentValidation validators
    services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

    // Register application services
    services.AddScoped<IAImprovementStrategyService, AiImprovementService>();
    services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();

    return services;
}
```

### Infrastructure Layer Registration

**Location:** `SmartMenuOptim.Infrastructure/Extensions/InfrastructureServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
{
    // Domain event dispatcher
    services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

    // Notification service
    services.AddScoped<INotificationService, LoggingNotificationService>();

    // Caching service
    services.AddMemoryCache();
    services.AddScoped<ICacheService, InMemoryCacheService>();
    
    // Dead letter queue service for event handler resilience
    // Development: In-memory implementation (events lost on restart)
    // Production: Replace with Azure Service Bus DLQ or similar durable implementation
    services.AddSingleton<IDeadLetterQueueService, InMemoryDeadLetterQueueService>();

    return services;
}

public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
{
    services.AddHostedService<DailySalesSummaryBackgroundJob>();
    return services;
}
```

### Program.cs Integration

```csharp
// In Program.cs or Startup.cs
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddBackgroundJobs(); // Optional: add background jobs
```

---

## Error Handling & Resilience

### ResilientEventHandlerBase

All event handlers in this application extend `ResilientEventHandlerBase<TEvent>`, which provides built-in retry logic and dead letter queue support.

**Location:** `SmartMenuOptim.Application/Handlers/ResilientEventHandlerBase.cs`

**Features:**
- Exponential backoff retry strategy (3 attempts by default)
- Dead letter queue for events that fail after all retries
- Comprehensive error logging with structured context
- Exception isolation to prevent cascade failures

**Retry Strategy:**

```
Attempt 1: Immediate
Attempt 2: Wait 2 seconds (2^1)
Attempt 3: Wait 4 seconds (2^2)
Failure: Send to Dead Letter Queue
```

**Usage Example:**

```csharp
public class MyHandler : ResilientEventHandlerBase<MyEvent>
{
    public MyHandler(
        ILogger<MyHandler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)
        : base(logger, deadLetterQueue)
    {
    }

    protected override async Task ProcessEventAsync(MyEvent @event, CancellationToken cancellationToken)
    {
        // Your handler logic here - exceptions trigger retry
        await _notificationService.SendAsync(...);
    }
}
```

### Handler Error Policy

All handlers inherit resilient behavior from the base class. The `ProcessEventAsync` method should:

1. **Throw exceptions** for transient failures that should be retried (network issues, temporary unavailability)
2. **Log and return** for permanent failures that should not be retried (invalid data, business rule violations)

```csharp
protected override async Task ProcessEventAsync(OrderPlacedEvent notification, CancellationToken ct)
{
    // This will throw and trigger retry on transient failures
    await _notificationService.SendOrderConfirmationAsync(
        notification.CustomerId,
        notification.OrderId,
        notification.TotalAmount,
        ct);
    
    _logger.LogDebug("Order confirmation sent successfully");
}
```

### IDeadLetterQueueService

**Location:** `SmartMenuOptim.Application/Contracts/IDeadLetterQueueService.cs`

**Development Implementation:** `InMemoryDeadLetterQueueService` (in-memory, events lost on restart)

**Production Implementations (future):**
- Azure Service Bus Dead Letter Queue
- Amazon SQS Dead Letter Queue
- Database-backed DLQ with background processing

```csharp
public interface IDeadLetterQueueService
{
    Task EnqueueAsync(FailedDomainEvent failedEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FailedDomainEvent>> GetFailedEventsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<bool> RetryEventAsync(Guid failedEventId, CancellationToken cancellationToken = default);
    Task MarkAsResolvedAsync(Guid failedEventId, string resolution, CancellationToken cancellationToken = default);
}
```

### FailedDomainEvent

Represents a domain event that failed to process:

```csharp
public class FailedDomainEvent
{
    public Guid Id { get; init; }
    public IDomainEvent Event { get; init; }
    public string EventTypeName { get; init; }
    public string HandlerName { get; init; }
    public DateTime FailedAt { get; init; }
    public string ErrorMessage { get; init; }
    public string ExceptionDetails { get; init; }
    public int RetryCount { get; init; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
}
```

---

## Testing Handlers

### Unit Testing

```csharp
public class AwardLoyaltyPointsHandlerTests
{
    [Fact]
    public async Task Handle_CalculatesPointsCorrectly()
    {
        // Arrange
        var logger = Mock.Of<ILogger<AwardLoyaltyPointsHandler>>();
        // IDeadLetterQueueService is optional - pass null for unit tests
        var handler = new AwardLoyaltyPointsHandler(logger, deadLetterQueue: null);
        
        var @event = new OrderPlacedEvent(
            orderId: 1,
            restaurantId: 10,
            customerId: 100,
            totalAmount: 55.75m,
            itemCount: 3);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        // Verify 55 points would be awarded (floor of $55.75)
    }
}
```

### Integration Testing

```csharp
public class OrderEventHandlerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task OrderPlaced_TriggersAllHandlers()
    {
        // Arrange
        var mediator = _serviceProvider.GetRequiredService<IMediator>();
        var @event = new OrderPlacedEvent(...);

        // Act
        await mediator.Publish(@event);

        // Assert
        // Verify all handlers executed (check logs, mock calls, etc.)
    }
    
    [Fact]
    public async Task Handler_RetriesOnTransientFailure()
    {
        // Arrange
        var logger = Mock.Of<ILogger<SendKitchenNotificationHandler>>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockDlq = new Mock<IDeadLetterQueueService>();
        
        // Fail first 2 attempts, succeed on 3rd
        var callCount = 0;
        mockNotificationService
            .Setup(x => x.SendKitchenOrderAsync(It.IsAny<int>(), It.IsAny<int>(), 
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount < 3) throw new HttpRequestException("Transient error");
                return Task.CompletedTask;
            });
        
        var handler = new SendKitchenNotificationHandler(
            mockNotificationService.Object, 
            logger, 
            mockDlq.Object);
        
        var @event = new OrderPlacedEvent(1, 10, 100, 50m, 3);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert - Should succeed after 3 attempts
        Assert.Equal(3, callCount);
        mockDlq.Verify(x => x.EnqueueAsync(It.IsAny<FailedDomainEvent>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

---

## Production Considerations

### 1. Replace Stub Services

| Service | Development | Production |
|---------|-------------|------------|
| INotificationService | `LoggingNotificationService` | `SendGridEmailService`, `TwilioSmsService` |
| ICacheService | `InMemoryCacheService` | `RedisCacheService` |
| IDeadLetterQueueService | `InMemoryDeadLetterQueueService` | `AzureServiceBusDLQService`, `DatabaseDLQService` |

### 2. Add Observability

```csharp
protected override async Task ProcessEventAsync(OrderPlacedEvent notification, CancellationToken ct)
{
    using var activity = ActivitySource.StartActivity("HandleOrderPlacedEvent");
    activity?.SetTag("event.type", notification.EventType);
    activity?.SetTag("event.id", notification.EventId);
    activity?.SetTag("order.id", notification.OrderId);
    
    // Handler logic...
}
```

### 3. Consider Outbox Pattern

For reliable event publishing in distributed systems:

```csharp
// In AppDbContext.SaveChangesAsync
public override async Task<int> SaveChangesAsync(CancellationToken ct)
{
    var events = CollectDomainEvents();
    
    // Store events in outbox table (same transaction)
    foreach (var @event in events)
    {
        OutboxMessages.Add(new OutboxMessage
        {
            EventType = @event.EventType,
            Payload = JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow
        });
    }
    
    // Save changes + outbox entries atomically
    var result = await base.SaveChangesAsync(ct);
    
    // Background job processes outbox and publishes events
    return result;
}
```

### 4. Scale with Message Queues

For high-throughput scenarios:

```
Aggregate → AppDbContext → Outbox → Azure Service Bus → Event Handlers
```

---

## File Structure Summary

```
SmartMenuOptim.Application/
├── Contracts/
│   ├── IDomainEventDispatcher.cs
│   ├── INotificationService.cs
│   ├── ICacheService.cs
│   └── IDeadLetterQueueService.cs      # NEW: Dead letter queue contract
├── Handlers/
│   ├── ResilientEventHandlerBase.cs    # NEW: Base class with retry + DLQ
│   ├── OrderEventHandlers/
│   │   ├── AwardLoyaltyPointsHandler.cs
│   │   ├── SendKitchenNotificationHandler.cs
│   │   ├── SendOrderConfirmationHandler.cs
│   │   ├── UpdateOrderAnalyticsHandler.cs
│   │   ├── OrderCancelledHandler.cs
│   │   └── OrderCompletedHandler.cs
│   ├── LoyaltyEventHandlers/
│   │   ├── LoyaltyPointsEarnedHandler.cs
│   │   └── LoyaltyTierChangedHandler.cs
│   ├── MenuEventHandlers/
│   │   ├── DishAddedToMenuHandler.cs
│   │   └── DishRemovedFromMenuHandler.cs
│   └── SaleEventHandlers/
│       ├── SaleRecordedHandler.cs
│       └── DailySalesSummarizedHandler.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs

SmartMenuOptim.Infrastructure/
├── EventDispatching/
│   └── MediatRDomainEventDispatcher.cs
├── Services/
│   ├── Notifications/
│   │   └── LoggingNotificationService.cs
│   ├── Caching/
│   │   └── InMemoryCacheService.cs
│   └── DeadLetterQueue/                 # NEW: Dead letter queue folder
│       └── InMemoryDeadLetterQueueService.cs  # NEW: In-memory DLQ
├── BackgroundJobs/
│   └── DailySalesSummaryBackgroundJob.cs
└── Extensions/
    └── InfrastructureServiceCollectionExtensions.cs
```

---

## Cross-Reference Documentation

| Document | Purpose | Link |
|----------|---------|------|
| **Domain Events Guide** | Event definitions, patterns, and contracts | [DOMAIN_EVENTS_GUIDE.md](./DOMAIN_EVENTS_GUIDE.md) |
| **Clean Architecture Analysis** | Overall architecture and layer responsibilities | [CLEAN_ARCHITECTURE_FULL_ANALYSIS.md](./CLEAN_ARCHITECTURE_FULL_ANALYSIS.md) |
| **This Document** | Handler implementations and infrastructure | Current |

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026 | Initial implementation with all handlers |
| 1.1 | 2026 | Added `ResilientEventHandlerBase<TEvent>` for retry logic and dead letter queue support |
| 1.2 | 2026 | Updated all 12 handlers to extend `ResilientEventHandlerBase` with explicit `INotificationHandler<TEvent>` |
| 1.3 | 2026-02-08 | Added `IDeadLetterQueueService` contract and `InMemoryDeadLetterQueueService` implementation |

---

**Document Status:** Implementation Complete ✅  
**Last Updated:** 2026-02-08  
**Next Review:** After production deployment
