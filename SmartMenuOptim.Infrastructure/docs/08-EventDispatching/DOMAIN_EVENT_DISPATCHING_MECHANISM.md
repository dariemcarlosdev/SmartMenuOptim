# Domain Event Dispatching Mechanism

> **Layer:** Infrastructure  
> **Location:** `SmartMenuOptim.Infrastructure/EventDispatching/`  
> **Last Updated:** 2026-02-24

---

## Overview

The Event Dispatching Mechanism provides the infrastructure for publishing and routing domain events from aggregates to their registered handlers. It implements the **Mediator Pattern** using MediatR to achieve loose coupling between event producers (aggregates) and event consumers (handlers).

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `MediatRDomainEventDispatcher` | `Infrastructure/EventDispatching/` | Publishes domain events via MediatR |
| `IDomainEventDispatcher` | `Application/Contracts/` | Port interface (abstraction) |
| `InMemoryDeadLetterQueueService` | `Infrastructure/Services/DeadLetterQueue/` | Stores failed events for retry |
| `IDeadLetterQueueService` | `Application/Contracts/` | Dead letter queue port interface |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         DOMAIN EVENT DISPATCHING FLOW                            │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌─────────────────┐                                                            │
│  │    Aggregate    │  1. Raises domain event                                    │
│  │  (Order, Menu,  │     AddDomainEvent(new OrderPlacedEvent(...))              │
│  │   Loyalty...)   │                                                            │
│  └────────┬────────┘                                                            │
│           │                                                                      │
│           ▼                                                                      │
│  ┌─────────────────┐  2. Collects events from tracked entities                  │
│  │  AppDbContext   │     var events = CollectDomainEvents();                    │
│  │ SaveChangesAsync│     ClearDomainEvents();                                   │
│  └────────┬────────┘                                                            │
│           │                                                                      │
│           ▼ (after successful commit)                                           │
│  ┌─────────────────────────────────────────────────────────────┐                │
│  │              MediatRDomainEventDispatcher                    │                │
│  │  ┌─────────────────────────────────────────────────────┐    │                │
│  │  │  foreach (var event in events)                      │    │                │
│  │  │      await _mediator.Publish(event, ct);            │    │                │
│  │  └─────────────────────────────────────────────────────┘    │                │
│  └────────┬────────────────────────────────────────────────────┘                │
│           │                                                                      │
│           ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐                │
│  │                    MediatR Pipeline                          │                │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │                │
│  │  │  Handler 1  │  │  Handler 2  │  │  Handler N  │          │                │
│  │  │ (Loyalty)   │  │ (Kitchen)   │  │ (Analytics) │          │                │
│  │  └─────────────┘  └─────────────┘  └─────────────┘          │                │
│  └─────────────────────────────────────────────────────────────┘                │
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐                │
│  │           Error Handling (if handler throws)                 │                │
│  │  ┌─────────────────────────────────────────────────────┐    │                │
│  │  │  • Log error with correlation context               │    │                │
│  │  │  • Add to Dead Letter Queue (optional)              │    │                │
│  │  │  • Continue processing remaining events             │    │                │
│  │  └─────────────────────────────────────────────────────┘    │                │
│  └─────────────────────────────────────────────────────────────┘                │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Clean Architecture Boundaries

```
┌───────────────────────────────────────────────────────────────────────────┐
│                              DOMAIN LAYER                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │  IDomainEvent (interface)          DomainEventBase (abstract)       │  │
│  │  Location: Common/                 Location: Common/                 │  │
│  │                                                                      │  │
│  │  Concrete Events:                                                    │  │
│  │  • Events/OrderEvents/OrderPlacedEvent.cs                           │  │
│  │  • Events/LoyaltyEvents/LoyaltyPointsEarnedEvent.cs                 │  │
│  │  • Events/MenuEvents/DishAddedToMenuEvent.cs                        │  │
│  │  • Events/SaleEvents/SaleRecordedEvent.cs                           │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                           APPLICATION LAYER                                │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │  Contracts (Ports):                                                  │  │
│  │  • IDomainEventDispatcher.cs      ← Dispatcher abstraction          │  │
│  │  • IDeadLetterQueueService.cs     ← DLQ abstraction                 │  │
│  │                                                                      │  │
│  │  Handlers:                                                           │  │
│  │  • ResilientEventHandlerBase<TEvent>  ← Base with retry logic       │  │
│  │  • OrderEventHandlers/*               ← Concrete handlers           │  │
│  │  • LoyaltyEventHandlers/*                                           │  │
│  │  • MenuEventHandlers/*                                              │  │
│  │  • SaleEventHandlers/*                                              │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                         INFRASTRUCTURE LAYER                               │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │  Adapters (Implementations):                                         │  │
│  │  • EventDispatching/MediatRDomainEventDispatcher.cs                 │  │
│  │  • Services/DeadLetterQueue/InMemoryDeadLetterQueueService.cs       │  │
│  │                                                                      │
│  │  Integration Point:                                                  │  │
│  │  • Persistence/Context/AppDbContext.cs                              │  │
│  │    └── SaveChangesAsync() → Dispatches events after commit          │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Details

### MediatRDomainEventDispatcher

**Purpose:** Bridge between aggregates and MediatR for event publishing.

```csharp
public class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatRDomainEventDispatcher> _logger;

    public async Task DispatchEventsAsync(
        IEnumerable<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            await DispatchEventAsync(domainEvent, cancellationToken);
        }
    }

    public async Task DispatchEventAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error dispatching domain event: {EventType}, EventId: {EventId}",
                domainEvent.EventType, domainEvent.EventId);
            // Continue processing - don't cascade failures
        }
    }
}
```

### AppDbContext Integration

**Location:** `SmartMenuOptim.Infrastructure/Persistence/Context/AppDbContext.cs`

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // 1. Collect domain events BEFORE saving
    var domainEvents = CollectDomainEvents();
    
    // 2. Clear events from entities to prevent re-dispatch
    ClearDomainEvents();
    
    // 3. Persist changes to database
    var result = await base.SaveChangesAsync(cancellationToken);
    
    // 4. Dispatch events AFTER successful commit
    if (domainEvents.Any())
    {
        await _domainEventDispatcher.DispatchEventsAsync(domainEvents, cancellationToken);
    }
    
    return result;
}

private List<IDomainEvent> CollectDomainEvents()
{
    // Collect from all tracked entities that have domain events
    return ChangeTracker.Entries()
        .Where(e => e.Entity is IHasDomainEvents)
        .SelectMany(e => ((IHasDomainEvents)e.Entity).DomainEvents)
        .ToList();
}
```

---

## Event Handler Categories

### Order Events

| Event | Handlers | Purpose |
|-------|----------|---------|
| `OrderPlacedEvent` | `AwardLoyaltyPointsHandler`, `SendOrderConfirmationHandler`, `SendKitchenNotificationHandler`, `UpdateOrderAnalyticsHandler` | Triggered when order is placed |
| `OrderCancelledEvent` | `OrderCancelledHandler` | Triggered when order is cancelled |
| `OrderCompletedEvent` | `OrderCompletedHandler` | Triggered when order is completed |

### Loyalty Events

| Event | Handlers | Purpose |
|-------|----------|---------|
| `LoyaltyPointsEarnedEvent` | `LoyaltyPointsEarnedHandler` | Points awarded to customer |
| `LoyaltyTierChangedEvent` | `LoyaltyTierChangedHandler` | Customer tier upgrade/downgrade |

### Menu Events

| Event | Handlers | Purpose |
|-------|----------|---------|
| `DishAddedToMenuEvent` | `DishAddedToMenuHandler` | New dish added to menu |
| `DishRemovedFromMenuEvent` | `DishRemovedFromMenuHandler` | Dish removed from menu |

### Sale Events

| Event | Handlers | Purpose |
|-------|----------|---------|
| `SaleRecordedEvent` | `SaleRecordedHandler` | Individual sale recorded |
| `DailySalesSummarizedEvent` | `DailySalesSummarizedHandler` | Daily sales summary generated |

---

## Resilient Event Handling

The `ResilientEventHandlerBase<TEvent>` provides built-in retry logic and dead letter queue support:

```csharp
public abstract class ResilientEventHandlerBase<TEvent> : INotificationHandler<TEvent>
    where TEvent : class, IDomainEvent
{
    protected virtual int MaxRetryAttempts => 3;
    protected virtual TimeSpan RetryDelay => TimeSpan.FromSeconds(1);

    public async Task Handle(TEvent notification, CancellationToken ct)
    {
        var attempts = 0;
        while (attempts < MaxRetryAttempts)
        {
            try
            {
                await HandleEventAsync(notification, ct);
                return; // Success
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts >= MaxRetryAttempts)
                {
                    await _deadLetterQueue?.EnqueueAsync(notification, ex, ct);
                    throw;
                }
                await Task.Delay(RetryDelay, ct);
            }
        }
    }

    protected abstract Task HandleEventAsync(TEvent notification, CancellationToken ct);
}
```

---

## Dead Letter Queue

Failed events that exceed retry attempts are stored in the Dead Letter Queue for manual review and reprocessing.

### InMemoryDeadLetterQueueService

```csharp
public class InMemoryDeadLetterQueueService : IDeadLetterQueueService
{
    private readonly ConcurrentQueue<DeadLetterEntry> _queue = new();

    public Task EnqueueAsync(IDomainEvent @event, Exception exception, CancellationToken ct)
    {
        _queue.Enqueue(new DeadLetterEntry
        {
            Event = @event,
            Exception = exception,
            FailedAt = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public Task<IEnumerable<DeadLetterEntry>> GetAllAsync(CancellationToken ct)
    {
        return Task.FromResult(_queue.AsEnumerable());
    }
}
```

> ⚠️ **Production Note:** Replace `InMemoryDeadLetterQueueService` with a persistent implementation (e.g., database table, Azure Service Bus DLQ) for production environments.

---

## Service Registration

### Application Layer

```csharp
// ApplicationServiceCollectionExtensions.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
});
```

### Infrastructure Layer

```csharp
// InfrastructureServiceCollectionExtensions.cs
services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
services.AddSingleton<IDeadLetterQueueService, InMemoryDeadLetterQueueService>();
```

---

## Sequence Diagram

```
┌────────┐     ┌───────────┐     ┌────────────────┐     ┌─────────┐     ┌─────────────┐
│ Client │     │Controller │     │  AppDbContext  │     │Dispatcher│     │  Handlers   │
└───┬────┘     └─────┬─────┘     └───────┬────────┘     └────┬────┘     └──────┬──────┘
    │               │                    │                   │                  │
    │  HTTP POST    │                    │                   │                  │
    │──────────────►│                    │                   │                  │
    │               │                    │                   │                  │
    │               │  order.Place()     │                   │                  │
    │               │  (raises event)    │                   │                  │
    │               │───────────────────►│                   │                  │
    │               │                    │                   │                  │
    │               │  SaveChangesAsync()│                   │                  │
    │               │───────────────────►│                   │                  │
    │               │                    │                   │                  │
    │               │                    │ 1. Collect events │                  │
    │               │                    │ 2. Clear events   │                  │
    │               │                    │ 3. Commit to DB   │                  │
    │               │                    │                   │                  │
    │               │                    │ DispatchEventsAsync                  │
    │               │                    │──────────────────►│                  │
    │               │                    │                   │                  │
    │               │                    │                   │ Publish(event)   │
    │               │                    │                   │─────────────────►│
    │               │                    │                   │                  │
    │               │                    │                   │ Handler 1 executes
    │               │                    │                   │ Handler 2 executes
    │               │                    │                   │ Handler N executes
    │               │                    │                   │◄─────────────────│
    │               │                    │◄──────────────────│                  │
    │               │◄───────────────────│                   │                  │
    │◄──────────────│                    │                   │                  │
    │  HTTP 200 OK  │                    │                   │                  │
```

---

## Related Documentation

| Document | Location | Description |
|----------|----------|-------------|
| **Domain Events Guide** | `SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md` | Event design patterns |
| **Event Handler Implementation** | `SmartMenuOptim.Application/docs/03-EventHandlers/EVENT_HANDLER_IMPLEMENTATION.md` | Handler patterns |
| **AppDbContext** | `SmartMenuOptim.Infrastructure/docs/01-Persistence/DATABASE_CONTEXT.md` | Context configuration |

---

## File Reference

### Domain Layer
- `SmartMenuOptim.Domain/Common/IDomainEvent.cs`
- `SmartMenuOptim.Domain/Common/DomainEventBase.cs`
- `SmartMenuOptim.Domain/Events/OrderEvents/*.cs`
- `SmartMenuOptim.Domain/Events/LoyaltyEvents/*.cs`
- `SmartMenuOptim.Domain/Events/MenuEvents/*.cs`
- `SmartMenuOptim.Domain/Events/SaleEvents/*.cs`

### Application Layer
- `SmartMenuOptim.Application/Contracts/IDomainEventDispatcher.cs`
- `SmartMenuOptim.Application/Contracts/IDeadLetterQueueService.cs`
- `SmartMenuOptim.Application/Handlers/ResilientEventHandlerBase.cs`
- `SmartMenuOptim.Application/Handlers/OrderEventHandlers/*.cs`
- `SmartMenuOptim.Application/Handlers/LoyaltyEventHandlers/*.cs`
- `SmartMenuOptim.Application/Handlers/MenuEventHandlers/*.cs`
- `SmartMenuOptim.Application/Handlers/SaleEventHandlers/*.cs`

### Infrastructure Layer
- `SmartMenuOptim.Infrastructure/EventDispatching/MediatRDomainEventDispatcher.cs`
- `SmartMenuOptim.Infrastructure/Services/DeadLetterQueue/InMemoryDeadLetterQueueService.cs`
- `SmartMenuOptim.Infrastructure/Persistence/Context/AppDbContext.cs`

---

*Last Updated: 2026-02-24 | Status: Production-Ready*
