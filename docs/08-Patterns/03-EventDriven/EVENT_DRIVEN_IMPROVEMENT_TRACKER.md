# Event-Driven Architecture — Improvement Tracker

## 📋 Document Information

| Field | Value |
|-------|-------|
| **Created** | 2026-03-21 |
| **Status** | Active |
| **Related** | [EVENT_DRIVEN_ARCHITECTURE_PATTERN.md](./EVENT_DRIVEN_ARCHITECTURE_PATTERN.md) |

---

## ✅ Completed

### 1. `IHasDomainEvents` Interface — Automatic Aggregate Discovery

| Field | Value |
|-------|-------|
| **Priority** | Highest impact / Lowest effort |
| **Completed** | 2026-03-21 |
| **Files Changed** | 6 |

**Problem:** `CollectDomainEvents()` and `ClearDomainEventsFromAggregates()` in `AppDbContext`
manually enumerated each aggregate type (`Order`, `CustomerLoyalty`, `Menu`, `SaleRecord`).
Every new aggregate required modifying **two methods** in infrastructure code — violating OCP
and causing silent "events not firing" bugs when the step was forgotten.

**Solution:**
- Created `IHasDomainEvents` interface in `SmartMenuOptim.Domain/Common/`
- All 4 aggregates now implement it: `Order`, `CustomerLoyalty`, `Menu`, `SaleRecord`
- `AppDbContext` uses `ChangeTracker.Entries<IHasDomainEvents>()` — single generic scan
- ~120 lines of repetitive code replaced with ~20 lines
- New aggregates auto-participate by implementing the interface — zero infrastructure changes

**Files:**
| File | Change |
|------|--------|
| `SmartMenuOptim.Domain/Common/IHasDomainEvents.cs` | Created — new interface |
| `SmartMenuOptim.Domain/Aggregates/OrderAggregate/Order.cs` | Added `: IHasDomainEvents` |
| `SmartMenuOptim.Domain/Aggregates/CustomerLoyaltyAggregate/CustomerLoyalty.cs` | Added `: IHasDomainEvents` |
| `SmartMenuOptim.Domain/Aggregates/MenuAggregate/Menu.cs` | Added `: IHasDomainEvents` |
| `SmartMenuOptim.Domain/Aggregates/SaleRecordAggregate/SaleRecord.cs` | Added `: IHasDomainEvents` |
| `SmartMenuOptim.Infrastructure/Persistence/Context/AppDbContext.cs` | Replaced per-aggregate scanning with generic `IHasDomainEvents` scan |

---

## 🔲 Remaining Improvements

### 2. Nested Transaction in `SaleRecordedHandler` (BUG-005)

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Effort** | Low |
| **Risk** | Runtime crash (nested transaction) |
| **Layer** | Application / Infrastructure |

**Problem:** `SaleRecordedHandler` calls `_unitOfWork.SaveChangesAsync()` which wraps
in `BeginTransactionAsync()`. This handler runs *during* the dispatch phase of an outer
`AppDbContext.SaveChangesAsync()`. If the `UnityOfWork` creates a nested transaction, it
crashes at runtime.

**Fix options:**
1. Have handler use `AppDbContext.SaveChangesAsync()` directly (scoped DI gives same instance)
   without the UoW transaction wrapper
2. Add a `SaveChangesWithoutTransactionAsync()` method to `IUnityOfWork` for use inside handlers
3. Queue the persistence work to run after the dispatch phase completes

**Related:** `docs/10-ISSUES-QUICK-FIX/BUG-005__NESTED_TRANSACTION_CRASH__INFRASTRUCTURE__ORDER_MANAGEMENT.md`

---

### 3. Outbox Pattern — Prevent Event Loss on Process Crash

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Effort** | High |
| **Risk** | Event loss if process dies between DB commit and dispatch |
| **Layer** | Infrastructure |

**Problem:** Events live in memory between `base.SaveChangesAsync()` (commit) and
`DispatchEventsAsync()` (publish). If the process crashes in that window, events are
permanently lost.

**Current scope:** Acceptable for single-instance Blazor Server. Becomes critical
when scaling to multi-instance or background processing.

**Fix:**
1. Create an `OutboxMessage` EF entity
2. Write events to the Outbox table **in the same transaction** as the aggregate save
3. A background `IHostedService` polls the Outbox and dispatches pending events
4. Mark events as processed after successful dispatch
5. Add idempotency checks in handlers using `EventId`

**Impact:** Guarantees at-least-once delivery regardless of process lifecycle.

---

### 4. Dispatcher Error Visibility — Critical Handler Classification

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Effort** | Low |
| **Risk** | Silent data loss for essential handlers |
| **Layer** | Infrastructure |

**Problem:** `MediatRDomainEventDispatcher.DispatchEventAsync` swallows all exceptions.
This is correct for non-critical handlers (notifications, cache), but dangerous for
essential handlers like `SaleRecordedHandler` that create required downstream records.

**Fix options:**
1. Add a `[CriticalHandler]` attribute — dispatcher rethrows for these
2. Add a dispatch failure counter exposed as a metric for monitoring/alerting
3. Log at `Critical` level (not just `Error`) when the failed handler is classified
   as essential

---

### 5. Handler Idempotency — Deduplication via `EventId`

| Field | Value |
|-------|-------|
| **Priority** | Low (becomes Medium with Outbox) |
| **Effort** | Medium |
| **Risk** | Duplicate side effects on event replay |
| **Layer** | Application |

**Problem:** `DomainEventBase` provides `EventId` for idempotency, but no handler
currently checks it. If events are replayed (future Outbox, retry scenarios), handlers
will execute duplicate side effects (double notifications, double points).

**Fix:**
1. Create an `IProcessedEventTracker` service
2. Before processing, check if `EventId` was already handled
3. After processing, record the `EventId` as processed
4. Wrap in `ResilientEventHandlerBase` so all handlers benefit automatically

---

### 6. Event Versioning — Schema Evolution Support

| Field | Value |
|-------|-------|
| **Priority** | Low |
| **Effort** | Medium |
| **Risk** | Breaking changes when event shapes evolve |
| **Layer** | Domain |

**Problem:** `DomainEventBase.EventVersion` defaults to `1` but no event overrides
it and no infrastructure reads it. When an event's schema changes (properties added/removed),
old serialized events in a future Event Store or Outbox cannot be deserialized.

**Fix:**
1. Define versioning policy: additive-only (new properties) or breaking (new version class)
2. Create event upcasters that transform v1 → v2 during deserialization
3. Store `EventVersion` alongside serialized events
4. Only needed when Outbox or Event Store is implemented

---

### 7. Production Infrastructure Swaps

| Concern | Current (Dev) | Target (Production) | Effort |
|---------|--------------|---------------------|--------|
| Dead Letter Queue | `InMemoryDeadLetterQueueService` | Azure Service Bus DLQ or DB-backed | Medium |
| Notifications | `LoggingNotificationService` | SendGrid / Azure Communication Services | Medium |
| Cache | `InMemoryCacheService` | Redis (`IDistributedCache`) | Low |
| Event persistence | None | Outbox table (see item 3) | High |

---

## Priority Order

| # | Improvement | Impact | Effort | Recommended Phase |
|---|-------------|--------|--------|-------------------|
| ~~1~~ | ~~`IHasDomainEvents`~~ | ~~High~~ | ~~Low~~ | ~~✅ Done~~ |
| 2 | BUG-005 nested transaction | High | Low | Next sprint |
| 3 | Dispatcher error visibility | Medium | Low | Next sprint |
| 4 | Outbox Pattern | High | High | Pre-production |
| 5 | Handler idempotency | Medium | Medium | With Outbox |
| 6 | Event versioning | Low | Medium | With Event Store |
| 7 | Production infra swaps | High | Medium | Pre-production |
