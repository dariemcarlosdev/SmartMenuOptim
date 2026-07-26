# 📦 Order Management — Implementation Tracker

> **Priority**: 2 (High — MVP Core Feature)  
> **Status**: 🟢 Complete (MVP)  
> **Started**: 2026-03-14 · **Last Updated**: 2026-03-21

---

> **🤖 For AI Agents — Document Guide**
>
> | Aspect | Details |
> |--------|---------|
> | **Document Type** | Implementation Tracker — per-phase progress log with task tables and completion status |
> | **Use As** | Reference for tracking implementation progress; template for new feature module trackers |
> | **Architecture** | [ADR-005 — Vertical Slice + Aggregate-Centric](../../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md) |
> | **Template Role** | The `📐 Document Structure Reference` section is a reusable template for new modules |
> | **Key Sections** | Phases 1–8 (Domain → EF → Handlers → DTOs → API → UI → Integration → Events), Known Issues, Key Decisions |
> | **Event-Driven Pattern** | Phase 3 (handlers) + Phase 8 (cross-aggregate SaleRecord events) document event implementations; for the canonical pattern framework, see [EVENT_DRIVEN_ARCHITECTURE_PATTERN.md](../../08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md) |
> | **Bug Traceability** | § Known Issues documents ORD-003a/b/c — the nested transaction bug chain when event handlers persist entities |
> | **Companion Docs** | [Implementation Plan](ORDER_MODULE_IMPLEMENTATION_PLAN.md) (prescriptive spec), [Post-MVP Tracker](ORDER_POST_MVP_TASK_TRACKER.md) (deferred tasks) |
> | **Do Not** | Track Post-MVP deferred tasks here (use the Post-MVP Tracker); modify phase structure without updating the Plan |

---

## 📐 Document Structure Reference

> Use this section as a **template** when creating similar implementation tracker documents for other feature modules.

```
# <Feature Name> — Implementation Tracker

  > Metadata block (Priority, Status, Started, Last Updated, Architecture)

  ## 📐 Document Structure Reference      ← This template section
  ## 📊 Progress                          ← Phase completion bars & overall MVP count
  ## 📋 Why Priority N?                   ← Business justification & MVP scope table

  ## 🏗️ Architecture                      ← Data flow diagram & key files by layer
  ## 📅 Implementation Phases             ← Detailed per-phase breakdown
    ### Phase 1: Domain Layer             ← Aggregates, entities, events
    ### Phase 2: EF Core & Infrastructure ← Configurations, DbContext, seed data
    ### Phase 3: Event Handlers           ← Domain event handlers
    ### Phase 4: DTOs & Service           ← Application layer core
    ### Phase 5: API Controllers          ← REST endpoints
    ### Phase 6: Blazor UI                ← Razor components
    ### Phase 7: Integration & Testing    ← Dashboard, tests, polish

  ## 🔌 API Endpoints                     ← Full endpoint reference by resource
  ## 🗄️ Database                          ← Migration & schema notes
  ## ⚠️ Known Issues                      ← Tracked bugs with resolutions
  ## 📝 Key Decisions                     ← Architecture & design choices
  ## 📚 Related Docs                      ← Links to guides, ADRs, patterns
  ## 🔄 Version History                   ← Document changelog
```

**Naming Convention**: `<MODULE>_MODULE_IMPLEMENTATION_TRACKER.md`  
**Phase Numbering**: Sequential integers; use `.5` for bonus/refactoring phases  
**Progress Bar Format**: `Phase N: Name  [██████████] 100%  ✅  YYYY-MM-DD  (notes)`

---

## 📊 Progress

```
Phase 1: Domain Layer     [██████████] 100%  ✅  Reviewed 2026-03-14  (Tier 1 Rich DDD + 8 review fixes)
Phase 2: EF/Infra         [██████████] 100%  ✅  Pre-existing  (config + seed data)
Phase 3: Event Handlers   [██████████] 100%  ✅  Pre-existing  (6 handlers + pricing stub)
Phase 4: DTOs & Service   [██████████] 100%  ✅  2026-03-21    (8 DTOs + service + mappings + DI + UoW + customer lookup)
Phase 5: API              [██████████] 100%  ✅  2026-03-21    (MVP complete — 8/8 done | 7 items deferred to Post-MVP)
Phase 6: Blazor UI        [██████████] 100%  ✅  2026-03-21    (3 pages + client services + 2 states + NavMenu + DI)
Phase 6.5: UX Polish      [██████████] 100%  ✅  2026-03-21    (customer/dish dropdowns, CustomerName display, bug fixes)
Phase 7: Integration      [██████████] 100%  ✅  2026-03-21    (dashboard ✅ + manual UI testing ✅)
Phase 8: Sale Record Events [██████████] 100% ✅  2026-03-21    (event-driven SaleRecord creation on order completion)

Overall MVP: 42/42 tasks ✅ — All phases complete 🎉
```

> **🎉 MVP Complete!** All phases finished. See [Post-MVP Task Tracker](ORDER_POST_MVP_TASK_TRACKER.md) for deferred items.

---

## 📋 Why Priority 2?

Order Management is the **core transactional feature** — it consumes Restaurant foundation data and drives loyalty, analytics, and customer engagement:

- Orders need menus/dishes → Menus need Restaurant ✅
- Loyalty points → Triggered by Order domain events ✅ (handlers exist)
- Analytics/AI → Fed by Order completion data
- Customer engagement → Order history, reviews

**Without Order Management, the application has no transactional capability.**

### MVP Scope

| ✅ In Scope | ❌ Post-MVP |
|------------|------------|
| Order CRUD (create, view, list, cancel) | Real-time updates (SignalR) |
| Status tracking (lifecycle) | Payment processing integration |
| Customer order history | Advanced order analytics |
| Staff assignment | Kitchen display system |
| Domain events (loyalty, notifications) | Order prediction (AI) |
| Basic validation (DataAnnotations) | FluentValidation + CQRS |

---

## 🏗️ Architecture

**Data Flow**: User → Blazor Component → HTTP → API Controller → Application Service → Repository → Database

All layers follow **Vertical Slice** (`Features/Orders/`) except Domain which uses **Aggregate-Centric** (`Aggregates/OrderAggregate/`). See [ADR-005](../../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md).

### Key Files by Layer

| Layer | Key Location | Contents |
|-------|-------------|----------|
| **Domain** | `Aggregates/OrderAggregate/` | `Order.cs` (~800 lines), `OrderItem.cs` (~200 lines), `Errors/OrderDomainException.cs` |
| **Domain** | `Events/` | `OrderPlacedEvent.cs`, `OrderCompletedEvent.cs`, `OrderCancelledEvent.cs` + cross-aggregate `SaleRecordedEvent` |
| **Domain** | `Entities/RestaurantEntities/` | `OrderStatus.cs` (Tier 2 lookup) |
| **Application** | `Handlers/OrderEventHandlers/` | 6 event handlers (resilient, with retry + DLQ) |
| **Application** | `Handlers/SaleEventHandlers/` | `SaleRecordedHandler` — persists `SaleRecord` entities + analytics |
| **Application** | `Features/Orders/Services/` | `OrderPricingApplicationService.cs` (stub) |
| **Application** | `Features/Orders/DTOs/` | 7 DTOs (OrderDTO, OrderDetailDTO, OrderItemDTO, OrderCreateDTO, OrderItemCreateDTO, OrderUpdateDTO, OrderStatusDTO) |
| **Application** | `Features/Customers/DTOs/` | `CustomerLookupDTO` (lightweight Id + Name for dropdowns) |
| **Application** | `Features/Orders/Services/` | `IOrderService` + `OrderService` (6 queries, 4 commands — includes `GetCustomerLookupsAsync`) |
| **Application** | `Features/Orders/Mappings/` | `OrderMappingExtensions` (Order, OrderItem, OrderStatus) |
| **Infrastructure** | `Persistence/Configurations/` | `OrderConfiguration.cs` |
| **API** | `Features/Orders/v1/` | `OrdersController` (8 endpoints) + `OrderStatusUpdateRequest`, `OrderCancelRequest` |
| **Server** | `Features/Orders/Components/` | `OrderList.razor(.cs)`, `OrderDetail.razor(.cs)`, `OrderForm.razor(.cs)` |
| **Server** | `Features/Orders/Services/` | `IOrderClientService`, `OrderClientService` |
| **Server** | `Features/Orders/State/` | `OrderListState`, `OrderDetailState` |

---

## 📅 Implementation Phases

### Phase 1: Domain Layer ✅ (Pre-existing — Reviewed 2026-03-14)

Order Aggregate Root fully implemented as Tier 1 Full Aggregate Root (Rich DDD).
**Domain Review** completed 2026-03-14 — 8 issues found and fixed:

| # | Issue | Fix | Category |
|---|-------|-----|----------|
| 1 | `Place()` XML doc referenced `InvalidOperationException` | Updated to `OrderDomainException` | XML Doc |
| 2 | `Cancel()` XML doc referenced `InvalidOperationException` | Updated to `ArgumentException` | XML Doc |
| 3 | `ValidateTenantConsistency()` XML doc referenced `InvalidOperationException` | Updated to `OrderDomainException` | XML Doc |
| 4 | `Place()` missing guard clause for `confirmedStatusId <= 0` | Added `ArgumentException` guard | Guard Clause |
| 5 | `Cancel()` missing guard clause for `cancelledStatusId <= 0` | Added `ArgumentException` guard | Guard Clause |
| 6 | `Complete()` missing guard clause for `completedStatusId <= 0` | Added `ArgumentException` guard | Guard Clause |
| 7 | `RemoveItem()` silently ignored non-existent items | Now throws `OrderDomainException` | Domain Exception |
| 8 | `ValidateTenantConsistency()` accessed `oi.Dish.Name` without null check | Changed to `oi.Dish?.Name` | Null Safety |

Additional cleanup: Removed unused `dishName` parameter from `AddItem()`, updated XML doc examples, updated `DbSeeder.cs` and `OrderItem.cs` doc examples.

| Component | File | Details |
|-----------|------|---------|
| `Order` | `Aggregates/OrderAggregate/Order.cs` | ~800 lines — 12+ business methods, 3 domain events + per-item `SaleRecordedEvent`, IValidatableObject |
| `OrderItem` | `Aggregates/OrderAggregate/OrderItem.cs` | ~200 lines — internal constructor, computed `Subtotal` |
| `OrderStatus` | `Entities/RestaurantEntities/OrderStatus.cs` | Tier 2 lookup — `Name`, `IsTerminal`, `ColorCode` |
| `OrderPlacedEvent` | `Events/OrderPlacedEvent.cs` | Raised by `Order.Place()` |
| `OrderCompletedEvent` | `Events/OrderCompletedEvent.cs` | Raised by `Order.Complete()` |
| `OrderCancelledEvent` | `Events/OrderCancelledEvent.cs` | Raised by `Order.Cancel()` |
| `OrderDomainException` | `Errors/OrderDomainException.cs` | Extends `DomainException` |

### Phase 2: EF Core & Infrastructure ✅ (Pre-existing)

| Component | Location | Status |
|-----------|----------|--------|
| `OrderConfiguration` | `Persistence/Configurations/OrderConfiguration.cs` | ✅ PK, precision, cascade, indexes |
| `DbSet<Order>` | `AppDbContext.cs:85` | ✅ Registered |
| `DbSet<OrderItem>` | `AppDbContext.cs:86` | ✅ Registered |
| `DbSet<OrderStatus>` | `AppDbContext.cs:95` | ✅ Registered |
| Generic `Repository<T>` | `Persistence/Repositories/Repository.cs` | ✅ Works with Order |
| `UnityOfWork` | `Persistence/Repositories/UnityOfWork.cs` | ✅ Transaction-aware `SaveChangesAsync` (ORD-003 fix) |
| Seed data | `DbSeeder.cs` | ✅ 6 statuses, 3 orders/restaurant, customers, staff |

### Phase 3: Application Event Handlers ✅ (Pre-existing)

6 order handlers at `Application/Handlers/OrderEventHandlers/` + 1 sale handler at `Application/Handlers/SaleEventHandlers/`, all extend `ResilientEventHandlerBase<TEvent>`:

| Handler | Event | Actions |
|---------|-------|---------|
| `AwardLoyaltyPointsHandler` | `OrderPlacedEvent` | Awards 1 pt/$1 |
| `SendKitchenNotificationHandler` | `OrderPlacedEvent` | Kitchen display notification |
| `SendOrderConfirmationHandler` | `OrderPlacedEvent` | Customer confirmation |
| `UpdateOrderAnalyticsHandler` | `OrderPlacedEvent` | Cache invalidation, metrics |
| `OrderCompletedHandler` | `OrderCompletedEvent` | Completion metrics, review scheduling |
| `OrderCancelledHandler` | `OrderCancelledEvent` | Loyalty reversal, cancellation notification |
| `SaleRecordedHandler` | `SaleRecordedEvent` | Persists `SaleRecord` entity, analytics logging, cache invalidation |

Plus: `OrderPricingApplicationService.cs` (stub — dependencies commented out)

### Phase 4: DTOs & Service ✅ (2026-03-21)

| Task | Target Location | Status |
|------|----------------|:------:|
| `OrderDTO` | `Application/Features/Orders/DTOs/OrderDTO.cs` | ✅ |
| `OrderDetailDTO` | `Application/Features/Orders/DTOs/OrderDetailDTO.cs` | ✅ |
| `OrderItemDTO` | `Application/Features/Orders/DTOs/OrderItemDTO.cs` | ✅ |
| `OrderCreateDTO` | `Application/Features/Orders/DTOs/OrderCreateDTO.cs` | ✅ |
| `OrderItemCreateDTO` | `Application/Features/Orders/DTOs/OrderItemCreateDTO.cs` | ✅ |
| `OrderUpdateDTO` | `Application/Features/Orders/DTOs/OrderUpdateDTO.cs` | ✅ |
| `OrderStatusDTO` | `Application/Features/Orders/DTOs/OrderStatusDTO.cs` | ✅ |
| `CustomerLookupDTO` | `Application/Features/Customers/DTOs/CustomerLookupDTO.cs` | ✅ |
| `IOrderService` | `Application/Features/Orders/Services/IOrderService.cs` | ✅ |
| `OrderService` | `Application/Features/Orders/Services/OrderService.cs` | ✅ |
| `OrderMappingExtensions` | `Application/Features/Orders/Mappings/OrderMappingExtensions.cs` | ✅ |
| `GlobalDtoUsings.cs` entries | `Application/GlobalDtoUsings.cs` (7 aliases) | ✅ |
| DI Registration | `ApplicationServiceCollectionExtensions.cs` | ✅ |
| `IUnityOfWork` + `UnityOfWork` | Added `Orders` + `OrderStatuses` repositories | ✅ |

### Phase 5: API Controllers ✅ (2026-03-21 — MVP Complete)

> **For AI Agents — MVP/Post-MVP Split**: Phase 5 MVP scope = controller code + pagination (§5.4.6).
> The 7 items marked ⏸️ below are **Post-MVP** per Plan §5.4.10/§5.4.11/§5.4.12 internal status markers.
> Cross-reference: [Plan Phase 5 checklist](ORDER_MODULE_IMPLEMENTATION_PLAN.md), [Post-MVP Tracker](ORDER_POST_MVP_TASK_TRACKER.md).

| Task | Target Location | Status | Scope |
|------|----------------|:------:|:-----:|
| `OrdersController` (8 endpoints) | `API/Features/Orders/v1/OrdersController.cs` | ✅ | MVP |
| `OrderStatusUpdateRequest` | `API/Features/Orders/v1/OrdersController.cs` (inline) | ✅ | MVP |
| `OrderCancelRequest` | `API/Features/Orders/v1/OrdersController.cs` (inline) | ✅ | MVP |
| `GlobalDtoUsings.cs` entries | `API/GlobalDtoUsings.cs` (namespace + 7 aliases) | ✅ | MVP |
| Swagger XML documentation | All endpoints with `[ProducesResponseType]` + XML comments | ✅ | MVP |
| §5.4 Best Practices docs | `ORDER_MODULE_IMPLEMENTATION_PLAN.md` §5.4 (12 subsections) | ✅ | MVP |
| Pagination, sorting & filtering | `PaginatedRequest` (shared) + `GET /orders/paginated` endpoint (§5.4.6) | ✅ | MVP |
| Customer lookup endpoint | `GET /api/v1/orders/customers/lookup` — lightweight customer list for dropdowns | ✅ | MVP |
| `SuppressAsyncSuffixInActionNames = false` | `API/Extensions/ServiceCollectionExtensions.cs` — fix `CreatedAtAction` route mismatch | ✅ | MVP |
| Rate limiting middleware | Sliding-window per tenant (§5.4.10) | ⏸️ | Post-MVP |
| ETag optimistic concurrency | `If-Match` header with row version (§5.4.11) | ⏸️ | Post-MVP |
| Idempotency keys for POST | `Idempotency-Key` header (§5.4.11) | ⏸️ | Post-MVP |
| Health check endpoint | `/health` with DB check (§5.4.12) | ⏸️ | Post-MVP |
| Request/response logging | Path, status code, duration (§5.4.12) | ⏸️ | Post-MVP |
| `[Authorize]` on controller | Authentication for all endpoints (§5.4.10) | ⏸️ | Post-MVP |
| Audit logging | State-changing ops with user identity (§5.4.10) | ⏸️ | Post-MVP |

> **API Best Practices Reference for AI Agents:** The Order API follows a comprehensive set of
> design best practices documented in `ORDER_MODULE_IMPLEMENTATION_PLAN.md` §5.4 (12 subsections).
> These cover RESTful resource design, HTTP method semantics, status code strategy, RFC 7807
> ProblemDetails, request model conventions, pagination/sorting/filtering contract, controller design
> rules, Swagger/OpenAPI documentation, versioning strategy, security considerations,
> idempotency/concurrency, and observability. **All new feature modules must follow these practices.**

### Phase 6: Blazor UI ✅ (2026-03-21)

| Task | Target Location | Status | CRUD Capabilities |
|------|----------------|:------:|-------------------|
| `OrderList.razor` + `.razor.cs` | `Server/Features/Orders/Components/` | ✅ | Customer name display, status filter bar, inline status dropdown, cancel modal, delete modal |
| `OrderDetail.razor` + `.razor.cs` | `Server/Features/Orders/Components/` | ✅ | Customer name in summary, status update card, cancel-with-reason modal |
| `OrderForm.razor` + `.razor.cs` | `Server/Features/Orders/Components/` | ✅ | Customer dropdown, dish dropdown (per restaurant), restaurant change reloads dishes, dynamic item rows |
| `IOrderClientService` | `Server/Features/Orders/Services/` | ✅ | 8 methods (4 queries + 4 commands — includes `GetCustomerLookupsAsync`) |
| `OrderClientService` | `Server/Features/Orders/Services/` | ✅ | HTTP adapter, Result pattern, ApiErrorHelper |
| `OrderListState` | `Server/Features/Orders/State/` | ✅ | Load, filter, inline status update, cancel, delete |
| `OrderDetailState` | `Server/Features/Orders/State/` | ✅ | Load, status update, cancel, statuses |
| NavMenu update | `Server/Components/Layout/NavMenu.razor` | ✅ | — |
| `GlobalDtoUsings.cs` entries | `Server/GlobalDtoUsings.cs` (namespace + 7 aliases) | ✅ | — |
| DI Registration | `Server/Extensions/ServiceCollectionExtensions.cs` | ✅ | `IOrderClientService` + `IDishClientService` |

> **Pattern Note for AI Agents:** The Order List page demonstrates the **inline CRUD pattern** —
> status updates, cancel-with-reason modals, and delete confirmations all happen directly on the list
> without navigating away. The Detail page provides the same mutations plus a status update card.
> See `ORDER_MODULE_IMPLEMENTATION_PLAN.md` §6.2 for the full Blazor CRUD Pattern Reference.

### Phase 6.5: UX Polish ✅ (2026-03-21)

> Bonus phase — UI/UX improvements discovered during manual testing. Replaced raw ID inputs with
> user-friendly dropdowns and fixed display issues across all three Order pages.

| # | Issue | Fix | Affected Files |
|---|-------|-----|----------------|
| 1 | OrderList showed Customer ID instead of name | Changed column header to "Customer", render `CustomerName` with fallback | `OrderList.razor` |
| 2 | OrderForm used `InputNumber` for Customer ID | Replaced with customer dropdown via `CustomerLookupDTO` + `GetCustomerLookupsAsync` | `OrderForm.razor(.cs)`, `IOrderService`, `OrderService`, `OrdersController`, `IOrderClientService`, `OrderClientService` |
| 3 | OrderForm used `InputNumber` for Dish ID | Replaced with dish dropdown filtered by selected restaurant; dishes reload on restaurant change | `OrderForm.razor(.cs)` |
| 4 | OrderForm restaurant selector didn't trigger dish reload | Changed from `InputSelect @bind-Value` to `<select @onchange>` pattern | `OrderForm.razor(.cs)` |
| 5 | OrderDetail Summary showed "Customer ID" label | Changed to "Customer" with `CustomerName` display + fallback | `OrderDetail.razor` |
| 6 | `IDishClientService` not registered in DI | Added `AddScoped<IDishClientService, DishClientService>()` | `Server/Extensions/ServiceCollectionExtensions.cs` |
| 7 | `CreatedAtAction(nameof(GetByIdAsync))` route mismatch | Set `SuppressAsyncSuffixInActionNames = false` globally | `API/Extensions/ServiceCollectionExtensions.cs` |
| 8 | Same `CreatedAtAction` bug in `RestaurantsController` + `DishesController` | Fixed by global `SuppressAsyncSuffixInActionNames = false` | `RestaurantsController.cs`, `DishesController.cs` |

### Phase 8: Event-Driven Sale Record Creation ✅ (2026-03-21)

> When an order is completed, `SaleRecord` entities are automatically created in the database
> via domain events — one per `OrderItem`. This connects Order completion to the SaleRecord
> aggregate for analytics, revenue tracking, and AI-powered menu optimization.

**Event Flow:**
```
UI sets status → "Completed"
  → OrderService.UpdateStatusAsync() detects terminal status name
    → order.Complete() raises OrderCompletedEvent + SaleRecordedEvent per item
      → AppDbContext.SaveChangesAsync() persists order, then dispatches events
        → SaleRecordedHandler creates SaleRecord entity → persists to DB
        → OrderCompletedHandler sends notifications, invalidates cache
```

| # | Task | File(s) | Details |
|---|------|---------|---------|
| 1 | `Order.Complete()` raises `SaleRecordedEvent` per item | `Order.cs` | Iterates `_orderItems`, includes `Dish?.Name`, `Dish?.Category?.Name` via nav props |
| 2 | `OrderService.UpdateStatusAsync()` uses domain methods for terminal statuses | `OrderService.cs` | Detects "Completed" → `Complete()`, "Cancelled" → `Cancel()`; includes `.ThenInclude(Dish).ThenInclude(Category)` |
| 3 | `SaleRecordedHandler` persists `SaleRecord` entities | `SaleRecordedHandler.cs` | Injects `IUnityOfWork`, creates `SaleRecord` via constructor + `Money` value object |
| 4 | `UnityOfWork.SaveChangesAsync()` transaction-aware | `UnityOfWork.cs` | Detects active transaction via `CurrentTransaction` to prevent nested transaction crash (ORD-003) |

### Phase 7: Integration ✅ (2026-03-21 — MVP Complete)

> Unit tests (`ORD-TEST-001`) and integration tests (`ORD-TEST-002`) are deferred to Post-MVP.
> See [Post-MVP Task Tracker](ORDER_POST_MVP_TASK_TRACKER.md).

| Task | Status | Notes |
|------|:------:|-------|
| Dashboard integration | ✅ | Order Metrics section added to `Dashboard.razor` — summary stats (total orders, revenue, avg value, active), status breakdown badges, per-restaurant table |
| Manual UI testing checklist | ✅ | Order workflow scenarios verified — create (restaurant/customer/dish dropdowns), list (filter, inline status, cancel, delete), detail (status update, cancel-with-reason), navigation, error states |

---

## 🔌 API Endpoints

### Orders (`/api/v1/orders`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/orders?restaurantId={id}` | List by restaurant |
| GET | `/api/v1/orders/paginated?restaurantId={id}` | Paginated list with sorting/filtering |
| GET | `/api/v1/orders/{id}` | Get order detail |
| POST | `/api/v1/orders` | Place new order |
| PATCH | `/api/v1/orders/{id}/status` | Change status |
| POST | `/api/v1/orders/{id}/cancel` | Cancel with reason |
| DELETE | `/api/v1/orders/{id}` | Soft delete |
| GET | `/api/v1/orders/statuses?restaurantId={id}` | List available statuses |
| GET | `/api/v1/orders/customers/lookup` | Customer lookups (Id + Name) for dropdowns |

---

## 🗄️ Database

No migrations required — all tables already exist: `Orders`, `OrderItems`, `OrderStatuses`.

Seed data includes: 6 order statuses per restaurant, 3 completed orders per restaurant (2 items each), 5 customers, 5 staff members.

---

## ⚠️ Known Issues

| ID | Issue | Resolution | Date |
|----|-------|------------|------|
| ORD-001 | `CreatedAtAction(nameof(GetByIdAsync))` threw `InvalidOperationException: No route matches the supplied values` on POST `/api/v1/orders` | ASP.NET Core default `SuppressAsyncSuffixInActionNames = true` strips "Async" from route names, but `nameof()` returns the full method name. Fixed by setting `SuppressAsyncSuffixInActionNames = false` in `AddControllers()` options. Same bug existed in `RestaurantsController` and `DishesController`. | 2026-03-21 |
| ORD-002 | `IDishClientService` not registered in Server DI — `OrderForm` could not inject dish service | Added `AddScoped<IDishClientService, DishClientService>()` to `Server/Extensions/ServiceCollectionExtensions.cs` | 2026-03-21 |
| ORD-003a | `OrderService.UpdateStatusAsync()` called `order.UpdateStatus(newStatusId)` for **all** status changes — a generic setter that raises **no domain events**. Setting status to "Completed" never invoked `order.Complete()`, so no `SaleRecordedEvent` was raised and no sale records were created. | `UpdateStatusAsync` now looks up the target `OrderStatus` entity by ID, checks its `Name`, and calls `order.Complete()` for "Completed", `order.Cancel()` for "Cancelled", or `order.UpdateStatus()` for intermediate statuses. Query includes `.ThenInclude(oi => oi.Dish).ThenInclude(d => d.Category)` so dish/category data is available in events. | 2026-03-21 |
| ORD-003b | `SaleRecordedHandler.ProcessEventAsync()` only logged analytics and called `_cacheService.InvalidateAnalyticsCacheAsync()` — it **never created `SaleRecord` entities** in the database. The handler had no `IUnityOfWork` dependency and no persistence logic. | Injected `IUnityOfWork` into the handler constructor. Added `PersistSaleRecordAsync()` method that creates a `SaleRecord` entity (via constructor + `Money` value object) and persists it via `_unitOfWork.SaleRecords.AddAsync()` + `_unitOfWork.SaveChangesAsync()`. Analytics logging and cache invalidation preserved. | 2026-03-21 |
| ORD-003c | `SaleRecordedHandler` threw `InvalidOperationException: The connection is already in a transaction` — `UnityOfWork.SaveChangesAsync()` unconditionally called `BeginTransactionAsync()`, but domain event handlers are dispatched **inside** the existing transaction from `AppDbContext.SaveChangesAsync()`. Call chain: `UoW.SaveChangesAsync()` → `BeginTransaction` → `AppDbContext.SaveChangesAsync()` → `base.Save` → `DispatchEvents` → `SaleRecordedHandler` → `UoW.SaveChangesAsync()` → `BeginTransaction` 💥 | Added `_context.Database.CurrentTransaction != null` check in `UnityOfWork.SaveChangesAsync()`. If a transaction is already active, the method calls `_context.SaveChangesAsync()` directly (participating in the existing transaction). Otherwise, wraps in a new explicit transaction as before. Both Order and SaleRecord changes commit or rollback atomically. | 2026-03-21 |

---

## 📝 Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Aggregate Tier | Tier 1 Rich DDD | Order has complex lifecycle, domain events, business rules |
| Domain Events | 3 order events (Placed, Completed, Cancelled) + cross-aggregate `SaleRecordedEvent` per item on completion | Drive side effects (loyalty, notifications, analytics, sale record persistence) without coupling |
| Event Handlers | `ResilientEventHandlerBase<T>` with retry + DLQ | Production-grade reliability |
| Validation | DataAnnotations (skip FluentValidation for MVP) | Consistent with Restaurant module; FluentValidation post-MVP with CQRS |
| Soft delete | Yes | Maintain data integrity |
| Folder structure | Vertical Slice (outer) + Aggregate-Centric (Domain) | [ADR-005](../../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md) |
| EF Configuration location | Consider move to `Features/Orders/Configurations/` | Align with vertical slice convention |
| Transaction handling | `UnityOfWork.SaveChangesAsync()` detects active transaction | Prevents nested transaction crash when domain event handlers persist entities within the dispatch cycle |

---

## 📚 Related Docs

| Document | Location |
|----------|----------|
| **Reference Implementation Guide** | **`docs/08-Patterns/REFERENCE_IMPLEMENTATION_GUIDE.md`** |
| **Event-Driven Architecture Pattern** | **`docs/08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md`** |
| Domain Events Guide | `SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md` |
| Events Clean Architecture | `SmartMenuOptim.Domain/docs/06-Events/EVENTS_CLEAN.md` |
| Code Inventory | `docs/07-Features/02-OrderManagement/ORDER_MANAGEMENT_CODE_INVENTORY.md` |
| Implementation Plan | `docs/07-Features/02-OrderManagement/ORDER_MODULE_IMPLEMENTATION_PLAN.md` |
| Post-MVP Task Tracker | `docs/07-Features/02-OrderManagement/ORDER_POST_MVP_TASK_TRACKER.md` |
| MVP Prioritization | `docs/01-Overview/MVP_FEATURE_PRIORITIZATION.md` |
| Blazor State Pattern | `docs/08-Patterns/BLAZOR_STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md` |
| Vertical Slice ADR | `docs/02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md` |
| Restaurant Tracker (reference) | `docs/07-Features/01-RestaurantManagement/RESTAURANT_MODULE_IMPLEMENTATION_TRACKER.md` |

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.9 | 2026-03-21 | AI agent optimization — added `🤖 For AI Agents — Document Guide` block at top; added Event-Driven Architecture Pattern, Domain Events Guide, and Events Clean Architecture to Related Docs table |
| 2.8 | 2026-03-21 | **ORD-003 expanded**
| 2.7 | 2026-03-21 | **Phase 8: Event-Driven Sale Records**
| 2.6 | 2026-03-21 | **🎉 MVP Complete**
| 2.5 | 2026-03-21 | Phase 6.5 UX Polish complete
| 2.4 | 2026-03-15 | Phase 7 dashboard integration complete — added Order Metrics section to `Dashboard.razor` with 4 summary stats (total orders, revenue, avg value, active), status breakdown badges, per-restaurant table; Phase 7 at 50% (1/2); overall MVP ~32/34 |
| 2.3 | 2026-03-15 | Phase 7 MVP scope reduced
| 2.2 | 2026-03-15 | Phase 5 MVP complete — implemented `PaginatedRequest` shared class (`Application/Common/`), `GetAllByRestaurantPaginatedAsync` service method with sorting allowlist + status filter, `GET /orders/paginated` endpoint; existing `PaginatedResponseDto<T>` reused; Phase 5 progress bar updated to 100%; overall MVP ~31/36 |
| 2.1 | 2026-03-15 | Updated cross-references — Pending Task Tracker renamed to Post-MVP Task Tracker (v2.0); document now contains only deferred Post-MVP tasks; MVP items tracked exclusively in this Tracker's phase tables |
| 2.0 | 2026-03-15 | Phase 5 synchronization — added Scope column (MVP/Post-MVP); 7 items reclassified as ⏸️ Post-MVP (per Plan §5.4 markers); progress bar updated to 86% MVP (6/7); overall MVP recalculated to ~30/36; added AI agent cross-reference note; aligned with Plan v3.0 and Pending Tracker v1.9 |
| 1.9 | 2026-03-14 | Phase 5 marked ⏳ In Progress — controller code complete but §5.4 best practices not implemented in code; added 8 pending tasks to Phase 5 table; updated progress bar to 40% |
| 1.8 | 2026-03-14 | Phase 5 enriched — added AI agent reference note pointing to Plan §5.4 API Design Best Practices (12 subsections) |
| 1.7 | 2026-03-14 | Phase 6 enriched — added CRUD capability column to Phase 6 table, added AI agent pattern reference note |
| 1.6 | 2026-03-14 | Phase 6 complete — 3 Blazor pages, client service, 2 state containers, NavMenu, DI, Server GlobalDtoUsings |
| 1.5 | 2026-03-14 | Phase 5 complete
| 1.4 | 2026-03-14 | Added Reference Implementation Guide
| 1.3 | 2026-03-14 | Phase 4 complete
| 1.2 | 2026-03-14 | Verified phase alignment with Implementation Plan v2.2 — all phases match 1:1 |
| 1.1 | 2026-03-14 | Domain layer review — 8 fixes applied (XML doc exceptions, guard clause consistency, null safety, dead code removal); updated Phase 1 with review findings |
| 1.0 | 2026-03-14 | Initial creation — inventory of existing code (Domain, Infrastructure, Event Handlers) and pending phases (DTOs, Service, API, Blazor, Integration) |

---

*This tracker follows the structure template from the Restaurant Management Implementation Tracker. For the complete code inventory, see [ORDER_MANAGEMENT_CODE_INVENTORY.md](ORDER_MANAGEMENT_CODE_INVENTORY.md).*
