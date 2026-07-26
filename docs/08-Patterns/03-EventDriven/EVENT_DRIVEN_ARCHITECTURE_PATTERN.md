---
title: Event-Driven Architecture Pattern Framework
project: SmartMenuOptimizer
layer: Cross-cutting (Domain → Application → Infrastructure)
version: "1.3"
created: "2026-03-21"
updated: "2026-03-21"
status: reference-implementation
tags: [event-driven, domain-events, DDD, MediatR, clean-architecture, resilience, dead-letter-queue]
audience: [ai-agent, developer]
related:
  - SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md
  - SmartMenuOptim.Domain/docs/06-Events/EVENTS_CLEAN.md
  - docs/08-Patterns/EVENT_DRIVEN_IMPROVEMENT_TRACKER.md
  - docs/08-Patterns/REFERENCE_IMPLEMENTATION_GUIDE.md
  - docs/07-Features/02-OrderManagement/ORDER_POST_MVP_TASK_TRACKER.md
  - docs/07-Features/01-RestaurantManagement/RESTAURANT_PENDING_TASK_TRACKER.md
actual_file_locations:
  IDomainEvent: SmartMenuOptim.Domain/Common/IDomainEvent.cs
  IHasDomainEvents: SmartMenuOptim.Domain/Common/IHasDomainEvents.cs
  DomainEventBase: SmartMenuOptim.Domain/Common/DomainEventBase.cs
  IDomainEventDispatcher: SmartMenuOptim.Application/Contracts/IDomainEventDispatcher.cs
  IDeadLetterQueueService: SmartMenuOptim.Application/Contracts/IDeadLetterQueueService.cs
  ResilientEventHandlerBase: SmartMenuOptim.Application/Handlers/ResilientEventHandlerBase.cs
  MediatRDomainEventDispatcher: SmartMenuOptim.Infrastructure/EventDispatching/MediatRDomainEventDispatcher.cs
  InMemoryDeadLetterQueueService: SmartMenuOptim.Infrastructure/Services/DeadLetterQueue/InMemoryDeadLetterQueueService.cs
  AppDbContext: SmartMenuOptim.Infrastructure/Persistence/Context/AppDbContext.cs
  DI_Registration: SmartMenuOptim.Infrastructure/Extensions/InfrastructureServiceCollectionExtensions.cs
---

# Event-Driven Architecture Pattern — SmartMenuOptimizer Framework

## 📋 Document Information

| Field | Value |
|-------|-------|
| **Document Title** | Event-Driven Architecture Pattern Framework |
| **Version** | 1.3 |
| **Created** | 2026-03-21 |
| **Updated** | 2026-03-21 |
| **Author** | SmartMenuOptimizer Architecture Team |
| **Status** | Reference Implementation ✅ |
| **Scope** | Domain → Application → Infrastructure (full stack) |

> **For AI Agents — Document Role**: This is the **canonical reference** for all event-driven
> implementation in SmartMenuOptimizer. When implementing any event, handler, or aggregate
> event collection, follow the templates and checklists in this document. For event schemas
> and catalog details, cross-reference [DOMAIN_EVENTS_GUIDE.md](../../SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md).
> For layer-placement rules (what goes where), cross-reference [EVENTS_CLEAN.md](../../SmartMenuOptim.Domain/docs/06-Events/EVENTS_CLEAN.md).
> The `actual_file_locations` block in the YAML frontmatter maps each artifact to its exact
> file path in the codebase — use it for direct file access.

---

## 1. Purpose

This document is the **canonical reference** for implementing event-driven features in SmartMenuOptimizer. It codifies every layer's responsibilities, file locations, naming rules, and concrete code skeletons extracted from the running codebase. Use it as a checklist whenever you add a new aggregate, event, or handler.

---

## 1.1 When to Use This Pattern

Use the Event-Driven Architecture pattern when **something that happened in one part of the system needs to trigger reactions in other parts**, without those parts knowing about each other.

✅ **Use when:**
- An aggregate's state change must trigger side effects in other aggregates or bounded contexts
- You need loose coupling between the "what happened" (domain) and "what should we do about it" (application/infrastructure)
- Multiple independent reactions must occur for the same business event
- You want to add new behaviors (handlers) without modifying existing aggregate code

❌ **Avoid when:**
- Simple CRUD with no side effects — direct service calls are simpler
- Synchronous request/response with tight latency constraints (events add indirection)
- Single consumer only and coupling is acceptable — a direct method call is clearer

### Real-World Scenarios

| # | Scenario | Event | Handlers & Reactions |
|---|----------|-------|---------------------|
| 1 | **Order placed at a restaurant** | `OrderPlacedEvent` | ① Create a `SaleRecord` for financial reporting ② Award loyalty points to the customer ③ Notify the kitchen display system ④ Update real-time dashboard counters. Each handler is independent — if loyalty service is down, the sale record is still created. |
| 2 | **Menu item price changed** | `DishPriceChangedEvent` | ① Recalculate active menu totals ② Invalidate cached pricing on the POS terminal ③ Log the change for audit trail ④ Trigger an AI recommendation refresh if the price swing is > 15%. Adding new reactions (e.g., push notification to regular customers) requires only a new handler — the `Menu` aggregate is untouched. |
| 3 | **Daily sales summary generated** | `DailySalesSummarizedEvent` | ① Email the manager a PDF report ② Feed data into the menu-optimization ML model ③ Update the analytics dashboard snapshot ④ Archive raw sale records to cold storage. The background job that generates the summary doesn't know or care about any of these consumers. |

---

## 2. Architectural Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER                                │
│  SmartMenuOptim.Domain                                              │
│                                                                     │
│  ┌─────────────────────┐   ┌──────────────────────────────────────┐ │
│  │ Common/              │   │ Aggregates/{Name}Aggregate/          │ │
│  │  IDomainEvent.cs     │   │  {Name}.cs          ← Aggregate Root│ │
│  │  IHasDomainEvents.cs │   │  Events/                            │ │
│  │  DomainEventBase.cs  │   │   {Name}{Action}Event.cs            │ │
│  │  EntityBase.cs       │   └──────────────────────────────────────┘ │
│  │  TenantEntityBase.cs │                                            │
│  └─────────────────────┘                                             │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ raises events
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       APPLICATION LAYER                             │
│  SmartMenuOptim.Application                                         │
│                                                                     │
│  ┌────────────────────┐   ┌──────────────────────────────────────┐ │
│  │ Contracts/          │   │ Handlers/                            │ │
│  │  IDomainEvent       │   │  ResilientEventHandlerBase<T>.cs    │ │
│  │   Dispatcher.cs     │   │  {Category}EventHandlers/           │ │
│  │  IDeadLetterQueue   │   │   {Name}{Action}Handler.cs          │ │
│  │   Service.cs        │   └──────────────────────────────────────┘ │
│  │  ICacheService.cs   │                                            │
│  │  INotification      │                                            │
│  │   Service.cs        │                                            │
│  └────────────────────┘                                             │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ dispatched by
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     INFRASTRUCTURE LAYER                            │
│  SmartMenuOptim.Infrastructure                                      │
│                                                                     │
│  ┌────────────────────────────────┐  ┌───────────────────────────┐ │
│  │ EventDispatching/              │  │ Services/DeadLetterQueue/  │ │
│  │  MediatRDomainEventDispatcher  │  │  InMemoryDeadLetterQueue  │ │
│  └────────────────────────────────┘  │   Service.cs              │ │
│  ┌────────────────────────────────┐  └───────────────────────────┘ │
│  │ Persistence/Context/           │                                 │
│  │  AppDbContext.cs               │  (SaveChangesAsync override    │
│  │   → CollectDomainEvents()      │   collects + dispatches)       │
│  │   → ClearDomainEventsFrom      │                                 │
│  │      Aggregates()              │                                 │
│  └────────────────────────────────┘                                 │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 3. Layer-by-Layer Responsibilities

### 3.1 Domain Layer — "What happened"

| Artifact | Location | Owns |
|----------|----------|------|
| `IDomainEvent` | `Common/IDomainEvent.cs` | Contract (extends MediatR `INotification`) |
| `IHasDomainEvents` | `Common/IHasDomainEvents.cs` | Contract for aggregates that raise events (enables automatic collection) |
| `DomainEventBase` | `Common/DomainEventBase.cs` | Shared properties (`EventId`, `OccurredOn`, `EventType`, `EventVersion`, `CorrelationId`) |
| Concrete events | `Aggregates/{Agg}Aggregate/Events/{Name}Event.cs` | Immutable data describing what happened |
| Event collection | Inside each aggregate root class implementing `IHasDomainEvents` | `_domainEvents` list + `AddDomainEvent()` + `ClearDomainEvents()` |

**The Domain layer NEVER handles or dispatches events — it only defines and raises them.**

### 3.2 Application Layer — "What to do about it"

| Artifact | Location | Owns |
|----------|----------|------|
| `IDomainEventDispatcher` | `Contracts/IDomainEventDispatcher.cs` | Abstraction consumed by Infrastructure |
| `IDeadLetterQueueService` | `Contracts/IDeadLetterQueueService.cs` | Failed-event persistence contract |
| `ResilientEventHandlerBase<T>` | `Handlers/ResilientEventHandlerBase.cs` | Retry + DLQ base class |
| Concrete handlers | `Handlers/{Category}EventHandlers/{Name}Handler.cs` | Side-effect logic (notifications, cache, analytics, persistence) |

**The Application layer NEVER publishes events to MediatR directly — it implements `INotificationHandler<T>` and lets Infrastructure dispatch.**

### 3.3 Infrastructure Layer — "How it gets wired"

| Artifact | Location | Owns |
|----------|----------|------|
| `MediatRDomainEventDispatcher` | `EventDispatching/MediatRDomainEventDispatcher.cs` | Bridges aggregates → MediatR pipeline |
| `AppDbContext.SaveChangesAsync` | `Persistence/Context/AppDbContext.cs` | Collect → Clear → Save → Dispatch lifecycle |
| `InMemoryDeadLetterQueueService` | `Services/DeadLetterQueue/InMemoryDeadLetterQueueService.cs` | Dev/test DLQ (swap for Azure Service Bus in prod) |
| DI registration | `Extensions/InfrastructureServiceCollectionExtensions.cs` | `IDomainEventDispatcher`, `IDeadLetterQueueService`, `ICacheService`, `INotificationService` |

---

## 4. Event Lifecycle — Step by Step

```
  Aggregate.Method()            AppDbContext.SaveChangesAsync()         MediatR Pipeline
 ─────────────────────        ──────────────────────────────────      ─────────────────────
 1. Business logic runs       3. CollectDomainEvents()                6. Publish(event)
 2. AddDomainEvent(new …)     4. ClearDomainEventsFromAggregates()    7. Handler1.Handle()
                              5. base.SaveChangesAsync()  ← DB commit   Handler2.Handle()
                                 ↓ success                               HandlerN.Handle()
                              6. DispatchEventsAsync(events)
```

**Critical guarantee:** Events dispatch only AFTER the database commit succeeds. If `SaveChangesAsync` throws, no events are dispatched.

**Clear-before-save safety:** Events are cleared from aggregates BEFORE the save attempt. This prevents double-dispatch if `SaveChangesAsync` is retried by a resilience policy.

---

## 5. Contract Definitions

### 5.1 `IDomainEvent` (Domain)

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

### 5.2 `DomainEventBase` (Domain)

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
| `EventId` | Idempotency key — handlers use it to deduplicate |
| `OccurredOn` | UTC timestamp — ordering, auditing |
| `EventType` | Derived from class name — serialization routing |
| `EventVersion` | Schema evolution — override when event shape changes |
| `CorrelationId` | Distributed tracing — links related events across boundaries |
| `CausationId` | Causal chain — links this event to the event that caused it |

### 5.3 `IDomainEventDispatcher` (Application)

```csharp
// SmartMenuOptim.Application/Contracts/IDomainEventDispatcher.cs
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
    Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
```

### 5.4 `IHasDomainEvents` (Domain)

```csharp
// SmartMenuOptim.Domain/Common/IHasDomainEvents.cs
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

All aggregates that raise domain events implement this interface. The infrastructure layer uses
`ChangeTracker.Entries<IHasDomainEvents>()` to discover them automatically — **no per-aggregate
registration required** in `AppDbContext`.

### 5.5 `IDeadLetterQueueService` (Application)

```csharp
// SmartMenuOptim.Application/Contracts/IDeadLetterQueueService.cs
public interface IDeadLetterQueueService
{
    Task EnqueueAsync(FailedDomainEvent failedEvent, CancellationToken ct = default);
    Task<IReadOnlyList<FailedDomainEvent>> GetFailedEventsAsync(int limit = 100, CancellationToken ct = default);
    Task<bool> RetryEventAsync(Guid failedEventId, CancellationToken ct = default);
    Task MarkAsResolvedAsync(Guid failedEventId, string resolution, CancellationToken ct = default);
}
```

---

## 6. Skeleton Templates

### 6.1 Domain Event — `sealed class` with `init` properties

```csharp
// Location: SmartMenuOptim.Domain/Aggregates/{Agg}Aggregate/Events/{Name}{Action}Event.cs

using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Domain.Aggregates.{Agg}Aggregate.Events;

/// <summary>
/// Raised when [describe business occurrence].
/// </summary>
public sealed class {Name}{Action}Event : DomainEventBase
{
    // ── Identity ──
    public int {Name}Id { get; init; }
    public int RestaurantId { get; init; }       // Always include for multi-tenant

    // ── Context (only what handlers need) ──
    public string RelevantProperty { get; init; } = string.Empty;
    public decimal MonetaryValue { get; init; }

    // ── Constructor ──
    public {Name}{Action}Event(
        int {name}Id,
        int restaurantId,
        string relevantProperty,
        decimal monetaryValue)
    {
        {Name}Id = {name}Id;
        RestaurantId = restaurantId;
        RelevantProperty = relevantProperty;
        MonetaryValue = monetaryValue;
    }
}
```

**Checklist:**
- [x] `sealed class` — events are not extended
- [x] All properties `{ get; init; }` — immutable after creation
- [x] Past-tense naming: `OrderPlacedEvent`, not `PlaceOrderEvent`
- [x] `RestaurantId` present for tenant isolation
- [x] Only IDs, never navigation properties or entity references
- [x] Inherits `DomainEventBase` (gets `EventId`, `OccurredOn`, `EventType` automatically)

### 6.2 Aggregate Root — Event Collection Pattern

```csharp
// Aggregate class declaration — implement IHasDomainEvents
public class MyAggregate : TenantEntityBase, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

// Usage inside a behavioral method:
public void Place()
{
    // 1. Guard clauses (ArgumentException for contract violations)
    // 2. Domain validation (DomainException for business rule violations)
    // 3. State mutation
    // 4. Raise event
    AddDomainEvent(new OrderPlacedEvent(
        orderId: Id,
        restaurantId: RestaurantId,
        customerId: CustomerId,
        totalAmount: TotalAmount,
        itemCount: Items.Count));
}
```

**Key rules:**
- Class implements `IHasDomainEvents` — enables automatic discovery by `ChangeTracker`
- `_domainEvents` is `private readonly` — external code cannot manipulate
- `DomainEvents` is `[NotMapped]` — EF Core ignores it
- `ClearDomainEvents()` is `public` — called by `AppDbContext` before save via `IHasDomainEvents`
- `AddDomainEvent()` is `protected` — only the aggregate itself raises events
- Events are raised **at the end** of the method, after state is valid

### 6.3 Event Handler — Resilient Pattern

```csharp
// Location: SmartMenuOptim.Application/Handlers/{Category}EventHandlers/{Name}{Action}Handler.cs

using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Aggregates.{Agg}Aggregate.Events;

namespace SmartMenuOptim.Application.Handlers.{Category}EventHandlers;

/// <summary>
/// Handles <see cref="{Name}{Action}Event"/> to [describe responsibility].
/// </summary>
public class {Name}{Action}Handler
    : ResilientEventHandlerBase<{Name}{Action}Event>,
      INotificationHandler<{Name}{Action}Event>          // Explicit for MediatR discovery
{
    private readonly ISomeDependency _dependency;
    private readonly ILogger<{Name}{Action}Handler> _logger;

    public {Name}{Action}Handler(
        ISomeDependency dependency,
        ILogger<{Name}{Action}Handler> logger,
        IDeadLetterQueueService? deadLetterQueue = null)  // Optional DLQ
        : base(logger, deadLetterQueue)
    {
        _dependency = dependency;
        _logger = logger;
    }

    protected override async Task ProcessEventAsync(
        {Name}{Action}Event @event,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing {EventType}. {Name}Id={Id}, RestaurantId={RestaurantId}",
            @event.EventType,
            @event.{Name}Id,
            @event.RestaurantId);

        // Handler logic here — throw for transient failures (will be retried)
        // Return silently for permanent failures you've already logged
    }
}
```

**Handler rules:**
- Inherit `ResilientEventHandlerBase<TEvent>` + implement `INotificationHandler<TEvent>`
- Override `ProcessEventAsync` — **never** override `Handle` directly
- DLQ parameter is always `IDeadLetterQueueService? deadLetterQueue = null`
- One handler per single concern (SRP) — multiple handlers per event is normal
- Throw exceptions for **transient** failures (network, timeout) → base class retries
- Catch and log **permanent** failures (bad data) → return without throwing

---

## 7. Resilience Framework

### 7.1 `ResilientEventHandlerBase<T>` Behavior

```
Attempt 1 → ProcessEventAsync()  → Success? ✅ Done
                                  → Failure? ⏳ Wait 2s (2^1)
Attempt 2 → ProcessEventAsync()  → Success? ✅ Done (logged as retry success)
                                  → Failure? ⏳ Wait 4s (2^2)
Attempt 3 → ProcessEventAsync()  → Success? ✅ Done
                                  → Failure? 💀 Send to Dead Letter Queue
```

| Config Property | Default | Override |
|-----------------|---------|----------|
| `MaxRetries` | 3 | `protected virtual int MaxRetries => 5;` |
| `BaseDelaySeconds` | 2 | `protected virtual int BaseDelaySeconds => 1;` |
| `HandlerName` | Class name | `protected virtual string HandlerName => "Custom";` |

### 7.2 Dead Letter Queue Flow

```
Handler fails after MaxRetries
    ↓
FailedDomainEvent created with:
  • Original IDomainEvent
  • EventTypeName (fully qualified)
  • HandlerName
  • FailedAt (UTC)
  • ErrorMessage + ExceptionDetails
  • RetryCount
    ↓
IDeadLetterQueueService.EnqueueAsync()
    ↓
Dev:   InMemoryDeadLetterQueueService (ConcurrentDictionary)
Prod:  Azure Service Bus DLQ / Database-backed implementation
```

### 7.3 Cancellation Safety

```csharp
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    // Don't retry — cancellation was requested. Rethrow for proper cleanup.
    throw;
}
```

---

## 8. `AppDbContext` — Event Collection & Dispatch

### 8.1 `SaveChangesAsync` Override

The `AppDbContext.SaveChangesAsync` method is the **single integration point** between aggregates and the event pipeline:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    SetAuditProperties();

    // 1. Collect domain events from ALL tracked aggregates
    var domainEvents = CollectDomainEvents();

    // 2. Clear events BEFORE save (prevents double-dispatch on retry)
    ClearDomainEventsFromAggregates();

    try
    {
        // 3. Persist to database
        var result = await base.SaveChangesAsync(ct).ConfigureAwait(false);

        // 4. Dispatch AFTER successful commit
        if (domainEvents.Count > 0 && _domainEventDispatcher != null)
        {
            await _domainEventDispatcher.DispatchEventsAsync(domainEvents, ct)
                .ConfigureAwait(false);
        }

        return result;
    }
    catch (DbUpdateConcurrencyException ex)
    {
        throw new DbUpdateConcurrencyException(
            "Concurrency conflict detected.", ex);
    }
}
```

### 8.2 Adding a New Aggregate to Event Collection

**No changes to `AppDbContext` are needed.** Both `CollectDomainEvents()` and
`ClearDomainEventsFromAggregates()` use `ChangeTracker.Entries<IHasDomainEvents>()`
to automatically discover every tracked entity that raises events:

```csharp
private List<IDomainEvent> CollectDomainEvents()
{
    var aggregatesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
        .Where(e => e.Entity.DomainEvents.Any())
        .Select(e => e.Entity)
        .ToList();

    return aggregatesWithEvents.SelectMany(a => a.DomainEvents).ToList();
}

private void ClearDomainEventsFromAggregates()
{
    var aggregatesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
        .Where(e => e.Entity.DomainEvents.Any())
        .Select(e => e.Entity)
        .ToList();

    foreach (var aggregate in aggregatesWithEvents)
        aggregate.ClearDomainEvents();
}
```

New aggregates only need to implement `IHasDomainEvents` on their class declaration.
This follows the Open/Closed Principle — the infrastructure is closed for modification
but open for extension through the interface.

---

## 9. `MediatRDomainEventDispatcher` — Error Isolation

```csharp
// Infrastructure/EventDispatching/MediatRDomainEventDispatcher.cs

public async Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken ct)
{
    try
    {
        await _mediator.Publish(domainEvent, ct);   // All handlers invoked
    }
    catch (Exception ex)
    {
        // Log but DO NOT rethrow — prevents one failing handler from
        // blocking other events or crashing the save pipeline.
        _logger.LogError(ex, "Error dispatching {EventType}", domainEvent.EventType);
    }
}
```

**Design decision:** The dispatcher swallows exceptions to prevent cascade failures. Individual handlers get their own retry/DLQ through `ResilientEventHandlerBase<T>`.

---

## 10. DI Registration Summary

```csharp
// Application layer — ApplicationServiceCollectionExtensions.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
});
// MediatR auto-discovers all INotificationHandler<T> implementations in this assembly.

// Infrastructure layer — InfrastructureServiceCollectionExtensions.cs
services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
services.AddScoped<INotificationService, LoggingNotificationService>();
services.AddScoped<ICacheService, InMemoryCacheService>();
services.AddSingleton<IDeadLetterQueueService, InMemoryDeadLetterQueueService>();
// Note: DLQ is Singleton because it holds in-memory state across requests.
// AppDbContext receives IDomainEventDispatcher via constructor injection.
```

---

## 11. Current Event Catalog

### Domain Events (9 implemented)

| Event | Aggregate | Category Folder | Handlers |
|-------|-----------|-----------------|----------|
| `OrderPlacedEvent` | Order | `OrderEventHandlers/` | `AwardLoyaltyPointsHandler`, `SendOrderConfirmationHandler`, `SendKitchenNotificationHandler`, `UpdateOrderAnalyticsHandler` |
| `OrderCancelledEvent` | Order | `OrderEventHandlers/` | `OrderCancelledHandler` |
| `OrderCompletedEvent` | Order | `OrderEventHandlers/` | `OrderCompletedHandler` |
| `DishAddedToMenuEvent` | Menu | `MenuEventHandlers/` | `DishAddedToMenuHandler` |
| `DishRemovedFromMenuEvent` | Menu | `MenuEventHandlers/` | `DishRemovedFromMenuHandler` |
| `LoyaltyPointsEarnedEvent` | CustomerLoyalty | `LoyaltyEventHandlers/` | `LoyaltyPointsEarnedHandler` |
| `LoyaltyTierChangedEvent` | CustomerLoyalty | `LoyaltyEventHandlers/` | `LoyaltyTierChangedHandler` |
| `SaleRecordedEvent` | SaleRecord | `SaleEventHandlers/` | `SaleRecordedHandler` |
| `DailySalesSummarizedEvent` | SaleRecord | `SaleEventHandlers/` | `DailySalesSummarizedHandler` |

### Handler Classification

| Handler Type | Injects Repositories? | Mutates State? | Example |
|--------------|----------------------|----------------|---------|
| **Persistence** | Yes (`IUnityOfWork`) | Yes | `SaleRecordedHandler` |
| **Notification** | No | No | `SendOrderConfirmationHandler` |
| **Cache** | No | No (cache only) | `DishAddedToMenuHandler` |
| **Analytics** | No | No (logging) | `UpdateOrderAnalyticsHandler` |
| **Orchestration** | Yes | Yes (cross-aggregate) | `AwardLoyaltyPointsHandler` |

---

## 12. Implementation Checklist — New Feature

Use this checklist when adding a new event-driven feature:

> **For AI Agents — Template Placeholder Substitution Rules**:
>
> | Placeholder | Meaning | Example |
> |-------------|---------|---------|
> | `{Agg}` | Aggregate name (PascalCase) — the DDD aggregate root | `Order`, `Menu`, `CustomerLoyalty`, `SaleRecord` |
> | `{Name}` | Entity/concept name (PascalCase) — may equal `{Agg}` or be a child entity | `Order`, `Dish`, `Loyalty`, `Sale` |
> | `{name}` | camelCase of `{Name}` — used for constructor parameters | `order`, `dish`, `loyalty`, `sale` |
> | `{Action}` | Past-tense verb describing what happened (PascalCase) | `Placed`, `Cancelled`, `Completed`, `Added`, `Removed` |
> | `{Category}` | Logical grouping for handler folders — usually the aggregate domain | `Order`, `Menu`, `Loyalty`, `Sale` |
>
> **Example substitution**: For "dish removed from menu":
> - Event: `{Name}` = `Dish`, `{Action}` = `RemovedFromMenu` → `DishRemovedFromMenuEvent`
> - Folder: `{Agg}` = `Menu` → `Aggregates/MenuAggregate/Events/`
> - Handler folder: `{Category}` = `Menu` → `Handlers/MenuEventHandlers/`

### Phase 1: Domain Layer

- [ ] **Create event class** at `Aggregates/{Agg}Aggregate/Events/{Name}{Action}Event.cs`
  - `sealed class` inheriting `DomainEventBase`
  - All properties `{ get; init; }`, constructor sets values
  - Include `RestaurantId` for tenant isolation
  - Past-tense naming (`Created`, `Updated`, `Deleted`, `Changed`)
- [ ] **Add event collection** to aggregate root (if not already present)
  - Implement `IHasDomainEvents` on the class declaration
  - `private readonly List<IDomainEvent> _domainEvents = new();`
  - `[NotMapped] public IReadOnlyCollection<IDomainEvent> DomainEvents`
  - `public void ClearDomainEvents()`
  - `protected void AddDomainEvent(IDomainEvent)`
- [ ] **Raise event** in the aggregate's behavioral method
  - After state mutation, before method returns
  - Use `AddDomainEvent(new {Event}(...))`

### Phase 2: Application Layer

- [ ] **Create handler class** at `Handlers/{Category}EventHandlers/{Name}{Action}Handler.cs`
  - Inherit `ResilientEventHandlerBase<TEvent>`
  - Implement `INotificationHandler<TEvent>` explicitly
  - Override `ProcessEventAsync`
  - Inject `IDeadLetterQueueService?` as optional (null default)
- [ ] **No manual DI registration needed** — MediatR auto-discovers handlers via `RegisterServicesFromAssembly`

### Phase 3: Infrastructure Layer

- [ ] **No `AppDbContext` changes needed** — `IHasDomainEvents` is auto-discovered via `ChangeTracker`
- [ ] **Verify DI registrations** in `InfrastructureServiceCollectionExtensions.cs`
  - `IDomainEventDispatcher` → `MediatRDomainEventDispatcher`
  - `IDeadLetterQueueService` → `InMemoryDeadLetterQueueService`
  - Any new infrastructure services handlers depend on

### Phase 4: Testing

- [ ] **Event unit test** — verify constructor sets all properties, `EventId` is non-empty, `OccurredOn` ≤ `UtcNow`
- [ ] **Aggregate test** — verify behavioral method raises the correct event with correct data
- [ ] **Handler test** — mock dependencies, verify side effects, verify structured log output
- [ ] **Integration test** — verify `SaveChangesAsync` dispatches events after commit

---

## 13. File Naming Conventions

| Artifact | Pattern | Example |
|----------|---------|---------|
| Event class | `{Entity}{PastTenseVerb}Event.cs` | `OrderPlacedEvent.cs` |
| Event folder | `Aggregates/{Agg}Aggregate/Events/` | `Aggregates/OrderAggregate/Events/` |
| Handler class | `{DescriptiveAction}Handler.cs` | `AwardLoyaltyPointsHandler.cs` |
| Handler folder | `Handlers/{Category}EventHandlers/` | `Handlers/OrderEventHandlers/` |
| Enums (in events) | Defined in same file or aggregate namespace | `CancellationSource`, `DishRemovalReason` |

---

## 14. Anti-Patterns to Avoid

| ❌ Anti-Pattern | ✅ Correct Approach |
|-----------------|---------------------|
| Event handler throws and crashes the pipeline | Inherit `ResilientEventHandlerBase` for retry + DLQ |
| Dispatching events BEFORE `SaveChangesAsync` | Always dispatch AFTER successful commit |
| Calling `_mediator.Publish()` from Application services | Let `AppDbContext.SaveChangesAsync` dispatch via `IDomainEventDispatcher` |
| Including entity/navigation references in events | Use scalar IDs only — events must be serializable |
| Generic event names (`EntityUpdatedEvent`) | Specific names (`DishPriceChangedEvent`) |
| Handler modifies the originating aggregate | One handler = one concern; use separate aggregate operations |
| Overriding `Handle()` in resilient handlers | Override `ProcessEventAsync()` only |
| Not clearing events before save | Events cleared in `ClearDomainEventsFromAggregates()` before `base.SaveChangesAsync()` |
| Wrapping `SaveChangesAsync` in another transaction inside a handler | Causes nested transaction crashes — handlers receive their own `IUnityOfWork` scope |

---

## 15. Multi-Handler per Event Pattern

A single event frequently triggers **multiple independent handlers**. This is by design:

```
OrderPlacedEvent
    ├── AwardLoyaltyPointsHandler    (loyalty points)
    ├── SendOrderConfirmationHandler (customer notification)
    ├── SendKitchenNotificationHandler (kitchen display)
    └── UpdateOrderAnalyticsHandler  (analytics/dashboard)
```

MediatR invokes all `INotificationHandler<OrderPlacedEvent>` implementations. Each handler:
- Runs independently (failure in one does not block others)
- Has its own retry strategy (via `ResilientEventHandlerBase`)
- Can be added/removed without modifying the event or other handlers (OCP)

---

## 16. Event Cascading Pattern

One event's handler can trigger another aggregate's events:

```
OrderPlacedEvent
    → AwardLoyaltyPointsHandler
        → loyalty.AddPoints(...)
            → raises LoyaltyPointsEarnedEvent
            → raises LoyaltyTierChangedEvent (if threshold crossed)
        → await _unitOfWork.SaveChangesAsync()
            → dispatches LoyaltyPointsEarnedEvent
            → dispatches LoyaltyTierChangedEvent
                → LoyaltyPointsEarnedHandler (notification)
                → LoyaltyTierChangedHandler (benefits activation)
```

**⚠️ Caution:** Keep cascade depth shallow (≤ 2 levels) to maintain debuggability.

---

## 17. Production Readiness Checklist

| Concern | Dev Implementation | Production Swap |
|---------|-------------------|-----------------|
| Dead Letter Queue | `InMemoryDeadLetterQueueService` (Singleton) | Azure Service Bus DLQ / DB-backed |
| Notifications | `LoggingNotificationService` | SendGrid / Azure Communication Services |
| Cache | `InMemoryCacheService` | Redis (`IDistributedCache`) |
| Event persistence | Not persisted | Event Store / Outbox table |
| Event ordering | In-process sequential | Message broker with partitioning |

---

## 18. Related Documentation

| Document | Location | Relationship |
|----------|----------|--------------|
| Domain Events Guide | `SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md` | Event schemas, catalog, aggregate patterns |
| Events Clean Architecture | `SmartMenuOptim.Domain/docs/06-Events/EVENTS_CLEAN.md` | Layer placement rules (what belongs where) |
| Event-Driven Improvement Tracker | `docs/08-Patterns/EVENT_DRIVEN_IMPROVEMENT_TRACKER.md` | Completed & planned improvements to this pattern |
| Reference Implementation Guide | `docs/08-Patterns/REFERENCE_IMPLEMENTATION_GUIDE.md` | Restaurant module as canonical code reference |
| Response Result Pattern | `docs/08-Patterns/RESPONSE_RESULT_PATTERN.md` | Error handling pattern used by handlers |
| Order Post-MVP Task Tracker | `docs/07-Features/02-OrderManagement/ORDER_POST_MVP_TASK_TRACKER.md` | Order event flows & CQRS tasks referencing this pattern |
| Restaurant Pending Task Tracker | `docs/07-Features/01-RestaurantManagement/RESTAURANT_PENDING_TASK_TRACKER.md` | Menu event flows & REST-CQRS-007 domain events task |

---

## 19. Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.3 | 2026-03-21 | Code verification pass — added missing `CausationId` property to §5.2 `DomainEventBase` snippet (was present in actual code at line 65 but missing from doc) |
| 1.2 | 2026-03-21 | AI agent optimization
| 1.1 | 2026-03-21 | Added `IHasDomainEvents` auto-discovery pattern (§5.4, §6.2, §8.2); updated §3/§4/§8 to reflect interface-based collection; added §12 Phase 3 note on no `AppDbContext` changes needed |
| 1.0 | 2026-03-21 | Initial creation — consolidated from codebase analysis; 18 sections covering full event lifecycle |

---

*This document is the canonical reference for SmartMenuOptimizer's event-driven architecture. All new event implementations must follow these patterns.*
