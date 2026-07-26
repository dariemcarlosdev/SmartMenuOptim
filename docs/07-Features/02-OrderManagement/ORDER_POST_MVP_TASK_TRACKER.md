# 📦 Order Management — Post-MVP Task Tracker

> **SmartMenuOptimizer — Order Feature Post-MVP Backlog**  
> **Version**: 2.3  
> **Created**: 2026-03-14  
> **Last Updated**: 2026-03-21  
> **Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`  
> **Extracted From**: [ORDER_MANAGEMENT_CODE_INVENTORY.md](ORDER_MANAGEMENT_CODE_INVENTORY.md)

---

> **🤖 For AI Agents — Document Guide**
>
> | Aspect | Details |
> |--------|---------|
> | **Document Type** | Post-MVP Task Tracker — deferred backlog of tasks beyond MVP release |
> | **Use As** | Backlog reference for future iterations; template for new module Post-MVP trackers |
> | **Scope Rule** | **Post-MVP only** — all tasks have status `⏸️ Deferred`. MVP tasks are tracked in the [Implementation Tracker](ORDER_MODULE_IMPLEMENTATION_TRACKER.md) |
> | **Key Sections** | Architecture & Refactoring (CQRS), Testing, API Hardening (§5.4), Performance, Integrations, Technical Debt, Event-Driven Architecture Reference |
> | **Event-Driven Pattern** | `📡 Event-Driven Architecture Reference` section documents Order event flows; for the canonical pattern framework, see [EVENT_DRIVEN_ARCHITECTURE_PATTERN.md](../../08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md) |
> | **Companion Docs** | [Implementation Plan](ORDER_MODULE_IMPLEMENTATION_PLAN.md) (prescriptive spec), [Implementation Tracker](ORDER_MODULE_IMPLEMENTATION_TRACKER.md) (MVP progress) |
> | **Do Not** | Add MVP tasks here; change task status from `⏸️` without moving to the Implementation Tracker |

---

## 📐 Document Structure Reference

> Use this section as a **template** when creating similar Post-MVP task tracker documents for other feature modules.
>
> **For AI Agents — Document Scope**: This document tracks **Post-MVP tasks only**. MVP pending tasks
> are tracked exclusively in the [Implementation Tracker](ORDER_MODULE_IMPLEMENTATION_TRACKER.md).
> Do **not** add MVP tasks here. All tasks in this document have status `⏸️ Deferred`.

```
# <Feature Name> — Post-MVP Task Tracker

  > Metadata block (Version, Created, Last Updated, Branch, Extracted From)

  ## 📐 Document Structure Reference          ← This template section (includes scope rules)
  ## 🗂️ Quick Reference                       ← Icons legend + related docs
  ## 📊 Summary                               ← Task counts by category

  ## 🚀 Post-MVP Pending Tasks                ← ALL pending content — grouped by concern
    ### 🔧 Architecture & Refactoring         ← CQRS, validation frameworks, structural changes
    ### 🧪 Testing                            ← Unit, integration, manual test plans
    ### 🔒 API Hardening                      ← Security, concurrency, observability (§5.4)
    ### ⚡ Performance & Caching              ← Database indexes, cache strategies
    ### 🔌 Integrations & Enhancements        ← External systems, real-time, analytics, AI
    ### 🧹 Technical Debt                     ← Code cleanup, stub activation, patterns

  ## 📝 New Task Template                     ← ID format & category reference
  ## 🔄 Version History                       ← Document changelog
```

**Naming Convention**: `<MODULE>_POST_MVP_TASK_TRACKER.md`
**ID Format**: `<PREFIX>-<CATEGORY>-<NUM>` (e.g., `ORD-APP-001`, `ORD-API-002`)  
**Categories**: `ARCH` · `DOM` · `APP` · `API` · `UI` · `TEST` · `DATA` · `ENH` · `PERF` · `TD` · `DOC` · `CQRS`  
**Grouping**: Tasks are grouped by **domain concern** (what the task is about), not by priority level.

---

## 🗂️ Quick Reference

| Status | Icon | | Priority | Icon |
|--------|------|-|----------|------|
| Deferred | ⏸️ | | Critical | 🔥 |
| In Progress | 🟡 | | High | 🔴 |
| Blocked | 🔴 | | Medium | 🟡 |
| Done | ✅ | | Low | 🟢 |

> **Related Docs**  
> [Implementation Tracker](ORDER_MODULE_IMPLEMENTATION_TRACKER.md) (MVP progress) · [Implementation Plan](ORDER_MODULE_IMPLEMENTATION_PLAN.md) (full spec) · [Code Inventory](ORDER_MANAGEMENT_CODE_INVENTORY.md) · [MVP Prioritization](../../01-Overview/MVP_FEATURE_PRIORITIZATION.md)
>
> **Event-Driven Architecture**  
> [Domain Events Guide](../../../SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md) · [Events Clean Architecture](../../../SmartMenuOptim.Domain/docs/06-Events/EVENTS_CLEAN.md) · [Event-Driven Architecture Pattern](../../08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md)

---

## 📊 Summary

```
Post-MVP Pending Tasks: 26

 By Category                          By Priority
 ─────────────────────────────        ──────────────────
 🔧 Architecture & Refactoring .. 4   🟡 Medium ...... 9
 🧪 Testing .................... 3   🟢 Low ........ 17
 🔒 API Hardening (§5.4) ....... 7
 ⚡ Performance & Caching ...... 4
 🔌 Integrations & Enhancements  5
 🧹 Technical Debt ............. 3
```

> **For AI Agents**: MVP pending tasks (pagination, dashboard, etc.) are tracked in the
> [Implementation Tracker](ORDER_MODULE_IMPLEMENTATION_TRACKER.md) Phase 5 and Phase 7 tables.
> This document contains **only** deferred (⏸️) Post-MVP work.

---

## 🚀 Post-MVP Pending Tasks

> All tasks in this section are intentionally **deferred** (⏸️) beyond the MVP release.
> They will be addressed in future iterations after MVP ships.

### 🔧 Architecture & Refactoring

> Structural improvements to codebase patterns and frameworks.
> **Dependency**: CQRS tasks require shared CQRS infrastructure (`REST-CQRS-001`) to be built first.

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| ORD-CQRS-001 | — | CQRS Commands — `CreateOrder`, `UpdateOrderStatus`, `CancelOrder` + handlers + validators | 🟡 | ⏸️ | Requires `REST-CQRS-001` infrastructure |
| ORD-CQRS-002 | — | CQRS Queries — `GetById`, `GetByRestaurant`, `GetByCustomer`, `GetByStatus` + handlers | 🟡 | ⏸️ | After CQRS infrastructure |
| ORD-CQRS-003 | — | Refactor `OrdersController` — replace `IOrderService` injection with `ISender` (MediatR) | 🟡 | ⏸️ | After command/query handlers |
| ORD-ENH-006 | §7.2 | FluentValidation for Order DTOs — replace DataAnnotations with pipeline behaviors | 🟢 | ⏸️ | Pairs with CQRS refactoring |

### 🧪 Testing

> Test coverage for the Order module. Deferred to align with CQRS refactoring.

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| ORD-TEST-001 | Phase 7 | Unit tests — `OrderService` (or CQRS handlers post-refactor) | 🟡 | ⏸️ | Service layer coverage |
| ORD-TEST-002 | Phase 7 | Integration tests — Orders API controllers | 🟡 | ⏸️ | End-to-end API tests |
| ORD-TEST-003 | Phase 7 | Manual UI testing checklist — Order pages (list, detail, form) | 🟡 | ⏸️ | Workflow scenario validation |

### 🔒 API Hardening — §5.4 Code Implementation

> Security, concurrency, and observability practices from Plan §5.4.
> The §5.4 **documentation** is complete (12 subsections); these tasks track **code implementation**.
>
> **For AI Agents**: Split from `ORD-DOC-003` during Phase 5 synchronization (2026-03-15).
> Only pagination (§5.4.6) is MVP — tracked in [Tracker Phase 5](ORDER_MODULE_IMPLEMENTATION_TRACKER.md).
> Cross-reference: [Plan §5.4](ORDER_MODULE_IMPLEMENTATION_PLAN.md).

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| ORD-API-010 | §5.4.10 | `[Authorize]` on controller — authentication for all endpoints | 🟢 | ⏸️ | Requires auth infrastructure |
| ORD-API-005 | §5.4.10 | Rate limiting middleware — sliding-window per tenant | 🟢 | ⏸️ | `Microsoft.AspNetCore.RateLimiting` |
| ORD-API-011 | §5.4.10 | Audit logging — state-changing operations with user identity | 🟢 | ⏸️ | Create, update, cancel, delete |
| ORD-API-006 | §5.4.11 | ETag-based optimistic concurrency — `If-Match` header | 🟢 | ⏸️ | Row version for lost-update prevention |
| ORD-API-007 | §5.4.11 | Idempotency keys for POST create — `Idempotency-Key` header | 🟢 | ⏸️ | Prevent duplicate order creation on retry |
| ORD-API-008 | §5.4.12 | Health check endpoint — `/health` with DB connectivity | 🟢 | ⏸️ | `Microsoft.Extensions.Diagnostics.HealthChecks` |
| ORD-API-009 | §5.4.12 | Request/response logging middleware — path, status code, duration | 🟢 | ⏸️ | Observability infrastructure |

### ⚡ Performance & Caching

> Database index optimization and cache strategies from Plan §8.

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| ORD-PERF-001 | §8.1 | Add composite index `IX_Orders_Restaurant_Date` | 🟢 | ⏸️ | Filter orders by restaurant + date |
| ORD-PERF-002 | §8.1 | Add composite index `IX_Orders_Customer_Date` | 🟢 | ⏸️ | Customer order history queries |
| ORD-PERF-003 | §8.1 | Add composite index `IX_Orders_Restaurant_Status` | 🟢 | ⏸️ | Status filter queries |
| ORD-PERF-004 | §8.2 | Caching — OrderStatus list per restaurant | 🟢 | ⏸️ | Frequently accessed, rarely changes |

### 🔌 Integrations & Enhancements

> New capabilities and external system integrations beyond MVP scope.

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| ORD-ENH-001 | — | Real-time updates (SignalR) — order status push to clients | 🟢 | ⏸️ | Requires SignalR hub infrastructure |
| ORD-ENH-002 | — | Kitchen Display System integration | 🟢 | ⏸️ | Depends on `ORD-ENH-001` (SignalR) |
| ORD-ENH-003 | — | Payment processing integration | 🟢 | ⏸️ | External payment gateway |
| ORD-ENH-004 | — | Order analytics & reporting dashboards | 🟢 | ⏸️ | Advanced data visualization |
| ORD-ENH-005 | — | AI-powered order prediction | 🟢 | ⏸️ | ML-based demand forecasting |

### 🧹 Technical Debt

> Code cleanup, stub activation, and pattern consistency improvements.

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| ORD-TD-001 | Inventory | Move `OrderConfiguration.cs` to `Features/Orders/Configurations/` | 🟢 | ⏸️ | Align with vertical slice convention |
| ORD-TD-002 | Inventory | Activate `OrderPricingApplicationService` dependencies | 🟢 | ⏸️ | Currently stubbed — all dependencies commented out |
| ORD-TD-003 | — | `CancellationToken` support in Order service methods | 🟢 | ⏸️ | Consistent async pattern across all layers |

---

## 📡 Event-Driven Architecture Reference

> The Order module is a primary consumer of the project's event-driven architecture.
> Refer to the following documents for implementation patterns:
>
> | Document | Purpose |
> |----------|---------|
> | [Domain Events Guide](../../../SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md) | Event catalog, schemas, aggregate collection pattern, handler rules |
> | [Events Clean Architecture](../../../SmartMenuOptim.Domain/docs/06-Events/EVENTS_CLEAN.md) | What belongs in Domain vs Application vs Infrastructure |
> | [Event-Driven Architecture Pattern](../../08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md) | Canonical reference — lifecycle, resilience, DI, templates, anti-patterns |

### Order Event Flows

The Order aggregate raises three domain events. Each triggers multiple independent handlers:

#### Flow 1: Order Placed

```
Order.Place()
    → raises OrderPlacedEvent
        → AppDbContext.SaveChangesAsync()
            → DB commit succeeds
            → MediatR dispatches OrderPlacedEvent
                ├── AwardLoyaltyPointsHandler     → CustomerLoyalty.AddPoints()
                │       → may raise LoyaltyPointsEarnedEvent
                │       → may raise LoyaltyTierChangedEvent (if threshold crossed)
                ├── SendOrderConfirmationHandler   → INotificationService (customer email/SMS)
                ├── SendKitchenNotificationHandler  → INotificationService (kitchen display)
                └── UpdateOrderAnalyticsHandler     → Structured logging for dashboards
```

#### Flow 2: Order Cancelled

```
Order.Cancel(reason, cancelledBy)
    → raises OrderCancelledEvent
        → AppDbContext.SaveChangesAsync()
            → DB commit succeeds
            → MediatR dispatches OrderCancelledEvent
                └── OrderCancelledHandler
                    → Reverse loyalty points (if RequiresRefund)
                    → Log cancellation analytics
                    → Notify restaurant staff
```

#### Flow 3: Order Completed

```
Order.Complete()
    → raises OrderCompletedEvent
        → AppDbContext.SaveChangesAsync()
            → DB commit succeeds
            → MediatR dispatches OrderCompletedEvent
                └── OrderCompletedHandler
                    → Calculate fulfillment time metrics
                    → Update order completion analytics
                    → Trigger customer satisfaction flow
```

### Key Implementation Notes

- **Event lifecycle**: Events dispatch only AFTER `SaveChangesAsync` succeeds (see [Pattern §4](../../08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md))
- **Resilience**: All handlers inherit `ResilientEventHandlerBase<T>` — 3 retries with exponential backoff, then Dead Letter Queue
- **Auto-discovery**: Order aggregate implements `IHasDomainEvents` — no `AppDbContext` registration needed
- **Multi-tenant**: All Order events include `RestaurantId` for tenant isolation
- **Cascading**: `AwardLoyaltyPointsHandler` demonstrates event cascading (Order → Loyalty events, max 2 levels deep)

---

## 📝 New Task Template

```markdown
| ORD-[CAT]-[NUM] | [REF] | [Description] | 🔴/🟡/🟢 | ⏸️ | [Details] |
```

**Categories**: `ARCH` · `DOM` · `APP` · `API` · `UI` · `TEST` · `DATA` · `ENH` · `PERF` · `TD` · `DOC` · `CQRS`

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.3 | 2026-03-21 | AI agent optimization — added `🤖 For AI Agents — Document Guide` block at top for AI model discoverability and document-role clarity |
| 2.2 | 2026-03-21 | Added `📡 Event-Driven Architecture Reference`
| 2.1 | 2026-03-15 | Removed `✅ Completed Tasks` section
| 2.0 | 2026-03-15 | **Major restructure** — document scope redefined to **Post-MVP only**; removed MVP Pending section (3 items already tracked in [Tracker](ORDER_MODULE_IMPLEMENTATION_TRACKER.md) Phases 5/7; `ORD-DOC-002` meta-task retired); reclassified 7 low-priority items (`ORD-PERF-001`–`004`, `ORD-TD-001`–`003`) from MVP to Post-MVP; reorganized tasks by **domain concern** (Architecture, Testing, API Hardening, Performance, Integrations, Tech Debt) instead of priority level; moved `ORD-ENH-006` (FluentValidation) to Architecture section (pairs with CQRS); reordered API Hardening by §5.4 subsection (security → concurrency → observability); updated template to reflect Post-MVP-only scope; 26 pending tasks total |
| 1.9 | 2026-03-15 | Phase 5 synchronization — scoped `ORD-DOC-003` to pagination only (§5.4.6 MVP); split 7 Post-MVP API best practices into `ORD-API-005`–`ORD-API-011`; added 2 missing completed items (`ORD-API-004` request models, `ORD-DOC-004` §5.4 docs); updated summary counts (49 total, 26 done, 14 deferred); aligned with Plan v3.0 and Tracker v2.0 |
| 1.8 | 2026-03-14 | Phase 5 marked partial — fixed 'All Phase 5 tasks completed' note, added partial note to Phase 5 completed section, fixed broken Related Docs links (stray 'x' removed, added Plan + Tracker links) |
| 1.7 | 2026-03-14 | Moved ORD-DOC-003 from Completed to MVP Pending — documentation is written but code implementation of best practices (pagination, rate limiting, ETag, health checks) is pending; updated counts (24 done, 9 pending) |
| 1.6 | 2026-03-14 | Added ORD-DOC-003 (API Design Best Practices §5.4 — 12 subsections); updated summary count to 25 done, 40 total |
| 1.5 | 2026-03-14 | Phase 6 enriched — updated ORD-UI-001/002/005/006 notes with full CRUD details, added ORD-UI-010 (inline CRUD enrichment); updated summary count to 24 done |
| 1.4 | 2026-03-14 | Phase 6 complete — moved ORD-UI-001–008 to completed, added ORD-UI-009 (DI); updated summary counts |
| 1.3 | 2026-03-14 | Phase 5 complete |
| 1.2 | 2026-03-14 | Phase 4 complete |
| 1.1 | 2026-03-14 | Domain layer review completed |
| 1.0 | 2026-03-14 | Initial creation — extracted from Code Inventory gap analysis; categorized into MVP vs Post-MVP |

---

*This document tracks **Post-MVP** tasks specific to the Order Management feature. For MVP progress and completed task history, see [ORDER_MODULE_IMPLEMENTATION_TRACKER.md](ORDER_MODULE_IMPLEMENTATION_TRACKER.md). For the global task backlog, see [PENDING_TASKS.md](../../09-ProjectManagement/PENDING_TASKS.md).*
