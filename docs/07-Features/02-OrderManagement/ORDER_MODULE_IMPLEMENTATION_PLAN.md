# Order Management System Implementation Guide

> **SmartMenuOptimizer — Order Management Feature**  
> **Priority**: 2 (MVP High — Depends on Restaurant Foundation)  
> **Version**: 3.6  
> **Last Updated**: 2026-03-21  
> **Architecture**: [ADR-005 — Vertical Slice + Aggregate-Centric](../../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md)

---

## 📐 Document Structure Reference

> Use this section as a **template** when creating similar implementation plan documents for other feature modules.

```
# <Feature Name> Implementation Guide

  > Metadata block (Priority, Version, Last Updated)

  ## 📐 Document Structure Reference      ← This template section
  ## Overview                             ← Feature purpose & architecture context

  ## Project Structure                    ← Layer-by-layer path reference table
  ## 1. Domain Layer                      ← Aggregates, entities, value objects         (Phase 1)
  ## 2. EF Core & Infrastructure          ← Configurations, DbContext, seed data        (Phase 2)
  ## 3. Event Handlers                    ← Domain event handlers                       (Phase 3)
  ## 4. DTOs & Service                    ← DTOs, interfaces, service, mappings         (Phase 4)
  ## 5. API Controllers                   ← Endpoints by controller & route             (Phase 5)
  ## 6. Blazor Components                 ← UI components by route                      (Phase 6)
  ## 7. Validation Strategy               ← Validation layers (cross-cutting)
  ## 8. Performance Optimization          ← Indexes & caching (cross-cutting)

  ## Implementation Checklist             ← Per-phase task checklist with status
  ## Related Documentation                ← Links to trackers, ADRs, standards
  ## Version History                      ← Document changelog
```

**Naming Convention**: `<MODULE>_MODULE_IMPLEMENTATION_PLAN.md`  
**Section Numbering**: Domain (1) → EF/Infra (2) → Event Handlers (3) → DTOs & Service (4) → API (5) → UI (6) → Validation (7) → Performance (8)  
**Phase Alignment**: Sections 1–6 directly correspond to Phases 1–6 in the [Implementation Tracker](ORDER_MODULE_IMPLEMENTATION_TRACKER.md)  
**Phase Checklist Format**: `### Phase N: <Layer> ✅ COMPLETE | ⏳ IN PROGRESS`

---

## Overview

This guide outlines the implementation steps for the Smart Menu Optimization Order Management System following **Clean Architecture** and **Domain-Driven Design (DDD)** principles in a Blazor-based architecture.

Order Management is the **second priority feature** — it depends on the Restaurant Management foundation (restaurants, menus, dishes, customers, staff) and integrates with the Loyalty Management System via domain events.

> **Note**: For actual code implementations, refer to the source files directly. This guide serves as a structural reference and checklist.

---

## Project Structure

| Layer | Path | Purpose |
|-------|------|---------|
| **Domain** | `SmartMenuOptim.Domain/Aggregates/OrderAggregate/` | Order (Aggregate Root), OrderItem (Child Entity) |
| **Domain** | `SmartMenuOptim.Domain/Aggregates/OrderAggregate/Errors/` | OrderDomainException |
| **Domain** | `SmartMenuOptim.Domain/Events/` | OrderPlacedEvent, OrderCompletedEvent, OrderCancelledEvent |
| **Domain** | `SmartMenuOptim.Domain/Aggregates/SaleRecordAggregate/Events/` | SaleRecordedEvent (cross-aggregate, raised per item on completion) |
| **Domain** | `SmartMenuOptim.Domain/Entities/RestaurantEntities/` | OrderStatus (lookup entity) |
| **Domain** | `SmartMenuOptim.Domain/ValueObjects/` | Money (used in events) |
| **Application** | `SmartMenuOptim.Application/Features/Orders/DTOs/` | DTOs for data transfer |
| **Application** | `SmartMenuOptim.Application/Features/Orders/Services/` | Business services (+ existing pricing stub) |
| **Application** | `SmartMenuOptim.Application/Features/Orders/Mappings/` | Mapping extensions |
| **Application** | `SmartMenuOptim.Application/Handlers/OrderEventHandlers/` | Domain event handlers (6 existing) |
| **Application** | `SmartMenuOptim.Application/Handlers/SaleEventHandlers/` | `SaleRecordedHandler` — persists SaleRecord + analytics |
| **Infrastructure** | `SmartMenuOptim.Infrastructure/Persistence/Configurations/` | OrderConfiguration (consider move to `Features/Orders/Configurations/`) |
| **Infrastructure** | `SmartMenuOptim.Infrastructure/Persistence/Repositories/` | Generic Repository (shared) |
| **API** | `SmartMenuOptim.API/Features/Orders/v1/` | REST API endpoints (versioned) |
| **UI** | `SmartMenuOptim.Server/Features/Orders/Components/` | Blazor components |
| **UI** | `SmartMenuOptim.Server/Features/Orders/Services/` | Client HTTP services |
| **UI** | `SmartMenuOptim.Server/Features/Orders/State/` | State containers |
| **Tests** | `SmartMenuOptim.Tests/UnitTests/Services/` | Service unit tests |

---

## 1. Domain Layer (✅ Complete — Reviewed 2026-03-14)

### 1.1 Aggregates & Entities

| Component | Type | File | Description |
|-----------|------|------|-------------|
| `Order` | Aggregate Root (Tier 1 Rich DDD) | `Order.cs` | ~800 lines — multi-tenant, rich behavior, domain events |
| `OrderItem` | Child Entity | `OrderItem.cs` | ~200 lines — internal constructor, created only via `Order.AddItem()` |
| `OrderStatus` | Lookup Entity (Tier 2) | `OrderStatus.cs` | Tenant-scoped status with `IsTerminal`, `ColorCode` |

### 1.2 Domain Events

| Event | Trigger | Key Properties |
|-------|---------|----------------|
| `OrderPlacedEvent` | `Order.Place()` | `OrderId`, `RestaurantId`, `CustomerId`, `TotalAmount`, `CurrencyCode`, `ItemCount`, `OrderType` |
| `OrderCompletedEvent` | `Order.Complete()` | `OrderId`, `FinalTotal`, `FulfillmentTimeMinutes` (computed), `LoyaltyPointsEarned`, `AppliedPromotions` |
| `SaleRecordedEvent` *(cross-aggregate)* | `Order.Complete()` — one per `OrderItem` | `OrderId`, `DishId`, `DishName`, `CategoryName`, `QuantitySold`, `UnitPrice`, `TotalAmount`, `OrderType` |
| `OrderCancelledEvent` | `Order.Cancel()` | `OrderId`, `CancellationReason`, `CancelledBy` (enum), `RequiresRefund`, `LoyaltyPointsToReverse` |

### 1.3 Domain Errors

| Component | File | Description |
|-----------|------|-------------|
| `OrderDomainException` | `Errors/OrderDomainException.cs` | Extends `DomainException`, includes optional `OrderId` |

### 1.4 Order Lifecycle

```
Pending → Confirmed → Preparing → Ready → In Delivery → Completed
   ↓                                  ↓
Cancelled ←─────────────────────────┘
```

### 1.5 Domain Dependencies

| Entity/VO | Relationship |
|-----------|-------------|
| `Customer` | FK `CustomerId` — who placed the order |
| `StaffMember` | FK `HandledByStaffId` — optional handler |
| `Dish` | Referenced by `OrderItem.DishId` |
| `TenantEntityBase` | Base class — provides `RestaurantId` |
| `SaleRecordedEvent` | Cross-aggregate event raised per `OrderItem` on `Complete()` — triggers `SaleRecord` persistence |
| `Money` | Used in events for currency-aware amounts |

### 1.6 Domain Review (2026-03-14)

8 issues identified and fixed during domain layer review:

| # | Issue | Fix | Category |
|---|-------|-----|----------|
| 1 | `Place()` XML doc referenced `InvalidOperationException` but throws `OrderDomainException` | Updated `<exception cref>` to `OrderDomainException` | XML Doc |
| 2 | `Cancel()` XML doc referenced `InvalidOperationException` but throws `ArgumentException` | Updated `<exception cref>` to `ArgumentException` | XML Doc |
| 3 | `ValidateTenantConsistency()` XML doc referenced `InvalidOperationException` but throws `OrderDomainException` | Updated `<exception cref>` to `OrderDomainException` | XML Doc |
| 4 | `Place()` missing guard clause for `confirmedStatusId <= 0` (inconsistent with `UpdateStatus`, constructor) | Added `ArgumentException` guard clause | Guard Clause |
| 5 | `Cancel()` missing guard clause for `cancelledStatusId <= 0` | Added `ArgumentException` guard clause | Guard Clause |
| 6 | `Complete()` missing guard clause for `completedStatusId <= 0` | Added `ArgumentException` guard clause | Guard Clause |
| 7 | `RemoveItem()` silently ignored non-existent items (inconsistent with `UpdateItemQuantity()`) | Now throws `OrderDomainException` when item not found | Domain Exception |
| 8 | `ValidateTenantConsistency()` accessed `oi.Dish.Name` without null check (`NullReferenceException` risk) | Changed to `oi.Dish?.Name` with `"Unknown Dish"` fallback | Null Safety |

Additional cleanup:
- Removed unused `dishName` parameter from `AddItem()` (was never passed to `OrderItem` or stored)
- Updated XML doc examples to match actual method signatures (`AssignStaffMember`, `UpdateStatus`)
- Updated `DbSeeder.cs` caller to match new `AddItem` signature
- Updated `OrderItem.cs` XML doc example to match new `AddItem` signature

---

## 2. EF Core & Infrastructure (✅ Complete — Phase 2)

| Configuration | Table | Key Settings |
|---------------|-------|--------------|
| `OrderConfiguration` | Orders | PK `Id`, `TotalAmount` precision(18,2), cascade delete on `OrderItems`, index on `CreatedAt` |

### 2.1 AppDbContext Registration

| Component | Status |
|-----------|--------|
| `DbSet<Order> Orders` | ✅ Registered |
| `DbSet<OrderItem> OrderItems` | ✅ Registered |
| `DbSet<OrderStatus> OrderStatuses` | ✅ Registered |
| `ApplyConfigurationsFromAssembly` | ✅ Auto-discovers `OrderConfiguration` |

### 2.2 Repository & UoW (Generic — Shared)

| Component | Notes |
|-----------|-------|
| `Repository<T>` | Generic repository — works with `Order` out of the box |
| `UnityOfWork` | Transaction-aware `SaveChangesAsync` — detects `CurrentTransaction` to prevent nested transaction conflicts when domain event handlers persist entities (ORD-003 fix) |

> **Note**: Consider moving `OrderConfiguration.cs` from `Persistence/Configurations/` to `Features/Orders/Configurations/` to follow vertical slice convention.

---

## 3. Event Handlers (✅ Complete — Phase 3)

All handlers extend `ResilientEventHandlerBase<TEvent>` with retry logic + dead letter queue.

| Handler | Event | Actions |
|---------|-------|---------|
| `AwardLoyaltyPointsHandler` | `OrderPlacedEvent` | Calculates & awards loyalty points (1 pt/$1) |
| `SendKitchenNotificationHandler` | `OrderPlacedEvent` | Sends kitchen display notification |
| `SendOrderConfirmationHandler` | `OrderPlacedEvent` | Sends customer confirmation |
| `UpdateOrderAnalyticsHandler` | `OrderPlacedEvent` | Invalidates analytics cache, logs metrics |
| `OrderCompletedHandler` | `OrderCompletedEvent` | Logs completion metrics, invalidates cache, schedules review request |
| `OrderCancelledHandler` | `OrderCancelledEvent` | Reverses loyalty points, sends cancellation notification |
| `SaleRecordedHandler` | `SaleRecordedEvent` | Persists `SaleRecord` entity via `IUnityOfWork`, logs sale analytics, invalidates analytics cache |

### 3.1 Cross-Aggregate Event Flow: Order → SaleRecord

When an order is completed, the `Order.Complete()` method raises a `SaleRecordedEvent` for each `OrderItem`.
The `SaleRecordedHandler` creates and persists `SaleRecord` entities, connecting order completion to the
SaleRecord aggregate for revenue tracking, dish performance analytics, and AI-powered menu optimization.

```
UI sets status → "Completed"
  → OrderService.UpdateStatusAsync() detects terminal status name
    → order.Complete() raises OrderCompletedEvent + SaleRecordedEvent per item
      → AppDbContext.SaveChangesAsync() persists order, then dispatches events
        → SaleRecordedHandler creates SaleRecord → persists to DB (within same transaction)
        → OrderCompletedHandler sends notifications, invalidates cache
```

> **Transaction Note**: `SaleRecordedHandler` calls `_unitOfWork.SaveChangesAsync()` which detects the
> active transaction from the outer save and participates in it (no nested `BeginTransactionAsync`).
> Both Order and SaleRecord changes commit or rollback atomically.

### 3.2 Pricing Stub

| File | Status |
|------|--------|
| `OrderPricingApplicationService.cs` | Stub only — dependencies commented out, pricing logic documented but not active |

---

## 4. DTOs & Service (✅ Complete — Phase 4)

### 4.1 DTOs

| DTO | Purpose | Target Location |
|-----|---------|-----------------|
| `OrderDTO` | List/card view — summary with `ItemCount`, `StatusName` | `Application/Features/Orders/DTOs/` |
| `OrderDetailDTO` | Detail view — includes `Items: List<OrderItemDTO>`, `StatusColorCode`, `IsTerminal` | `Application/Features/Orders/DTOs/` |
| `OrderItemDTO` | Nested in detail — `DishName`, `UnitPrice`, `Quantity`, `Subtotal` | `Application/Features/Orders/DTOs/` |
| `OrderCreateDTO` | Create form — `RestaurantId`, `CustomerId`, `Items: List<OrderItemCreateDTO>` | `Application/Features/Orders/DTOs/` |
| `OrderItemCreateDTO` | Nested in create — `DishId`, `Quantity`, `SpecialInstructions?` | `Application/Features/Orders/DTOs/` |
| `OrderUpdateDTO` | Update operations — `SpecialInstructions?`, `OrderStatusId?` | `Application/Features/Orders/DTOs/` |
| `OrderStatusDTO` | Status dropdown — `Name`, `DisplayOrder`, `IsTerminal`, `ColorCode` | `Application/Features/Orders/DTOs/` |

### 4.2 Service Interfaces & Implementations

| Interface | Implementation | Target Location |
|-----------|----------------|-----------------|
| `IOrderService` | `OrderService` | `Application/Features/Orders/Services/` |

### 4.3 Service Methods (Implemented)

| Method | Return Type | Description |
|--------|-------------|-------------|
| `GetByIdAsync(int id)` | `Result<OrderDetailDTO>` | Get order with items |
| `GetAllByRestaurantAsync(int restaurantId)` | `Result<IReadOnlyList<OrderDTO>>` | List orders for restaurant |
| `GetByCustomerAsync(int customerId)` | `Result<IReadOnlyList<OrderDTO>>` | Customer order history |
| `GetByStatusAsync(int restaurantId, int statusId)` | `Result<IReadOnlyList<OrderDTO>>` | Filter by status |
| `GetStatusesAsync(int restaurantId)` | `Result<IReadOnlyList<OrderStatusDTO>>` | Get available statuses |
| `CreateAsync(OrderCreateDTO dto)` | `Result<OrderDTO>` | Create new order |
| `UpdateStatusAsync(int id, int newStatusId)` | `Result<OrderDTO>` | Change order status — detects "Completed" → `Complete()`, "Cancelled" → `Cancel()` with domain events; loads `Dish` + `Category` nav props for sale event data |
| `CancelAsync(int id, string reason)` | `Result` | Cancel order |
| `DeleteAsync(int id)` | `Result` | Soft delete |

### 4.4 Mapping Extensions (✅ Complete)

Location: `Application/Features/Orders/Mappings/OrderMappingExtensions.cs`

- `ToDto()` extension methods for Order, OrderItem, OrderStatus
- Converts domain entities to DTOs for API responses
- Includes null-argument guards via `ArgumentNullException.ThrowIfNull()`

---

## 5. API Controllers (✅ Complete — Phase 5)

### 5.1 Endpoints

| Controller | Route | Target Location |
|------------|-------|-----------------|
| `OrdersController` | `api/v1/orders` | `API/Features/Orders/v1/OrdersController.cs` |

### 5.2 Implemented Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `api/v1/orders?restaurantId={id}` | List by restaurant |
| GET | `api/v1/orders/{id}` | Get order detail |
| POST | `api/v1/orders` | Place new order |
| PATCH | `api/v1/orders/{id}/status` | Change status |
| POST | `api/v1/orders/{id}/cancel` | Cancel with reason |
| DELETE | `api/v1/orders/{id}` | Soft delete |
| GET | `api/v1/orders/statuses?restaurantId={id}` | List available statuses |

### 5.3 API Patterns (from Restaurant reference)

- `ApiControllerBase` base class
- `ApiResponse` wrapper for all responses
- `[ApiVersion("1.0")]` versioning
- `[Route("api/v{version:apiVersion}/[controller]")]`
- XML documentation for Swagger
- RFC 7807 ProblemDetails error responses

### 5.4 API Design Best Practices

> **For AI Agents**: Use this section as the **canonical API design checklist** when implementing
> controllers for any feature module. These practices are verified against the Order module
> implementation and must be replicated for all future modules.

#### 5.4.1 RESTful Resource Design

| Rule | Implementation | Rationale |
|------|---------------|-----------|
| **Plural nouns for resources** | `/api/v1/orders` not `/api/v1/order` | REST convention — resources are collections |
| **Hierarchical sub-resources for actions** | `/orders/{id}/status`, `/orders/{id}/cancel` | Expresses action scoped to a specific resource |
| **Query params for filtering, not nested routes** | `?restaurantId={id}` not `/restaurants/{id}/orders` | Avoids deep nesting (max 2 levels); simpler routing |
| **Route constraints on all ID params** | `{id:int}` | Rejects non-integer IDs at routing level before controller |
| **Consistent JSON casing** | `camelCase` via global `System.Text.Json` `JsonNamingPolicy.CamelCase` | Client-server contract consistency |
| **No verbs in resource URIs** | `/orders/{id}/cancel` not `/orders/{id}/cancelOrder` | The HTTP method *is* the verb; the URI is the noun |
| **Trailing-slash agnostic** | ASP.NET default handles both | Prevents 404s from inconsistent client calls |

#### 5.4.2 HTTP Method Semantics

| Method | Semantics | Order Module Example | Response |
|--------|-----------|---------------------|----------|
| `GET` | Safe, idempotent read | `GetByRestaurantAsync`, `GetByIdAsync`, `GetStatusesAsync` | `200 OK` with body |
| `POST` | Create new resource | `CreateAsync` | `201 Created` with `Location` header |
| `POST` | Domain command (non-CRUD) | `CancelAsync` — `/orders/{id}/cancel` | `204 No Content` |
| `PATCH` | Partial update (single field) | `UpdateStatusAsync` — `/orders/{id}/status` | `200 OK` with updated resource |
| `PUT` | Full resource replacement | Not used in Order (used in Restaurant) | `200 OK` with replaced resource |
| `DELETE` | Remove (soft-delete) | `DeleteAsync` | `204 No Content` |

> **Key distinction**: Use `POST` (not `PATCH`) for cancel because cancellation is a domain **command**
> with side effects (domain events, loyalty reversal), not a simple field update.

#### 5.4.3 HTTP Status Code Strategy

| Code | When to Return | Order Module Usage |
|------|---------------|-------------------|
| `200 OK` | Successful GET or PATCH returning a body | List, detail, status update |
| `201 Created` | Successful POST creating a new resource | Order creation — with `Location` header via `CreatedAtAction` |
| `204 No Content` | Successful command with no response body | Cancel, delete |
| `400 Bad Request` | Validation failure or business rule violation | Invalid DTO, domain rule violated (e.g., cancel a completed order) |
| `404 Not Found` | Resource does not exist | Order/status not found |
| `409 Conflict` | Optimistic concurrency violation | Post-MVP (ETag-based concurrency) |
| `422 Unprocessable Entity` | Semantically invalid (valid JSON but bad domain data) | Post-MVP alternative to 400 for domain errors |
| `429 Too Many Requests` | Rate limit exceeded | Post-MVP (rate limiting middleware) |
| `500 Internal Server Error` | Unhandled server exception | Global exception handler only — never explicit in controller |

> **Anti-patterns to avoid**:
> - Never return `200 OK` with an error in the body — use proper HTTP status codes.
> - Never return `500` explicitly from a controller action — let middleware handle unhandled exceptions.
> - Never return `404` for empty collections — return `200` with `[]`.

#### 5.4.4 Error Response Pattern (RFC 7807 ProblemDetails)

All error responses use `ProblemDetails` with consistent structure:

```json
{
  "title": "Order.NotFound",
  "detail": "Order with ID 42 was not found.",
  "status": 404,
  "instance": "/api/v1/orders/42",
  "traceId": "00-abc123..."
}
```

| Field | Source | Purpose |
|-------|--------|---------|
| `title` | Domain error code (e.g., `Order.NotFound`, `Order.CancelError`) | Machine-readable error category for client `switch` statements |
| `detail` | `Result.Error` from service layer | Human-readable explanation for UI display |
| `status` | HTTP status code | Redundant but required by RFC 7807 spec |
| `instance` | `HttpContext.Request.Path` | Identifies which resource was affected |
| `traceId` | `HttpContext.TraceIdentifier` | Correlation ID for log-based debugging |

**Implementation rules:**
- Centralize via private `CreateProblemDetails(title, detail, status)` helper per controller
- Post-MVP: Extract to `ApiControllerBase` for cross-controller reuse
- `title` uses dot-notation: `{Aggregate}.{ErrorType}` (e.g., `Order.NotFound`, `Order.CancelError`, `Order.ValidationError`)
- Never expose stack traces or internal exception messages in `detail`

#### 5.4.5 Request & Response Model Conventions

| Convention | Example | Rationale |
|------------|---------|-----------|
| **Separate request models from Application DTOs** | `OrderStatusUpdateRequest` ≠ `OrderUpdateDTO` | API-layer models stay in controller file; DTOs are Application-layer |
| **DataAnnotations on all request models** | `[Required]`, `[Range]`, `[StringLength]` | Auto-validated by `[ApiController]` before action executes |
| **Inline request classes in controller file (MVP)** | Classes at bottom of `OrdersController.cs` | Reduces file count; extract to `Models/` folder when controller grows beyond 3 request types |
| **Default values for strings** | `= string.Empty` | Prevents NullReferenceException during validation |
| **Use DTOs for responses, never domain entities** | Return `OrderDTO` not `Order` | Prevents domain model leakage; controls serialized shape |
| **Envelope pattern optional** | Direct DTO in body, not `{ data: ..., meta: ... }` | Simpler for MVP; add `ApiResponse<T>` wrapper post-MVP if needed |

#### 5.4.6 Pagination, Sorting & Filtering (Collection Endpoints)

> **Status**: ✅ Complete (2026-03-15). Implemented via `PaginatedRequest` shared class
> (`Application/Common/PaginatedRequest.cs`) and `GET /api/v1/orders/paginated` endpoint.
> The existing `PaginatedResponseDto<T>` is reused for the response envelope.
> Sorting uses a server-side allowlist (`createdAt`, `orderDate`, `totalAmount`, `statusName`).

**Standard query parameter contract for all collection endpoints:**

```
GET /api/v1/orders/paginated?restaurantId=1&page=1&pageSize=20&sortBy=orderDate&sortDirection=desc&status=Pending
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | `int` | `1` | 1-based page number |
| `pageSize` | `int` | `20` | Items per page (max: `100`, enforced server-side) |
| `sortBy` | `string` | `createdAt` | Field to sort by (allowlist: `createdAt`, `orderDate`, `totalAmount`, `statusName`) |
| `sortDir` | `string` | `desc` | `asc` or `desc` |
| `status` | `string?` | `null` | Filter by status name (exact match) |
| `customerId` | `int?` | `null` | Filter by customer |
| `fromDate` / `toDate` | `DateTime?` | `null` | Date range filter (ISO 8601) |

**Paginated response envelope:**

```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 142,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

**Implementation rules:**
- Create a shared `PaginatedRequest` base class with `Page`, `PageSize`, `SortBy`, `SortDir` properties
- Create a shared `PaginatedResult<T>` response wrapper
- Enforce `pageSize` ceiling (e.g., 100) via `[Range(1, 100)]` — prevent full-table scans
- Apply sorting via allowlist — never pass raw `sortBy` to `OrderBy` (SQL injection risk)
- Return `totalCount` for UI pagination controls
- Filtering happens at the repository/query level — never load all records and filter in memory

#### 5.4.7 Controller Design Rules

| Rule | Implementation | Why |
|------|---------------|-----|
| **Thin controllers** | Controllers only map HTTP → service → HTTP response | All business logic stays in Application services |
| **Single service dependency (MVP)** | `IOrderService` + `ILogger` only | Post-MVP: replace with single `ISender` (MediatR) |
| **`CancellationToken` on every async endpoint** | `CancellationToken cancellationToken` parameter | Supports client disconnect detection; propagates through all layers |
| **Structured logging with semantic params** | `_logger.LogDebug("API: Getting order {OrderId}", id)` | Indexed by log aggregators; never use `$""` interpolation |
| **No `try/catch` in controllers** | Global exception middleware handles 500s | Controllers never catch — middleware handles uniformly |
| **`CreatedAtAction` for POST** | `CreatedAtAction(nameof(GetByIdAsync), new { id }, value)` | Returns `201` with `Location` header per REST spec |
| **`[ApiController]` attribute** | On every controller class | Auto model-state validation, auto `[FromBody]` binding, auto `400` for invalid models |
| **`[Produces("application/json")]`** | At class level | Declares content type globally; consistent Swagger docs |
| **No business logic branching** | `result.Error.Contains(...)` is the maximum — no `if/else` chains | If branching grows, move logic to service layer |
| **Idempotency awareness** | GET/PUT/DELETE are idempotent; POST/PATCH are not | Design APIs so repeated calls produce same result where appropriate |

#### 5.4.8 Swagger / OpenAPI Documentation

| Practice | Implementation |
|----------|---------------|
| `[ProducesResponseType]` on **every** endpoint | Documents all possible status codes in Swagger UI |
| XML `<summary>` + `<param>` + `<returns>` + `<response>` | Full endpoint docs auto-generated from code |
| `[Produces("application/json")]` at class level | Declares content type for all endpoints |
| Explicit `[FromQuery]` / `[FromBody]` binding | Eliminates ambiguity in Swagger parameter location |
| `[Tags("Orders")]` (post-MVP) | Groups endpoints by domain aggregate in Swagger |
| Include example values (post-MVP) | `[SwaggerExample]` or XML `<example>` for request/response bodies |

#### 5.4.9 Versioning Strategy

| Practice | Current (MVP) | Post-MVP |
|----------|--------------|----------|
| **Scheme** | URL-segment: `/api/v1/orders` | Same — URL versioning is explicit and cacheable |
| **Folder structure** | `API/Features/Orders/v1/OrdersController.cs` | Add `v2/` folder when breaking changes needed |
| **Attribute** | None (path-based by convention) | `[ApiVersion("1.0")]` via `Asp.Versioning.Mvc` package |
| **Route template** | `[Route("api/v1/[controller]")]` | `[Route("api/v{version:apiVersion}/[controller]")]` |
| **Version deprecation** | N/A | `[ApiVersion("1.0", Deprecated = true)]` + Sunset header |

**Versioning rules:**
- Never change the contract of a released endpoint — add a new version instead
- Additive (non-breaking) changes are OK within the same version: new optional fields, new endpoints
- Breaking changes require a new version: removed fields, renamed properties, changed semantics
- Maintain old versions until all clients have migrated

#### 5.4.10 Security Considerations

| Practice | Status | Implementation |
|----------|--------|---------------|
| **`[Authorize]` on controller** | ⬜ Post-MVP | Require authentication for all Order endpoints |
| **Tenant isolation** | ✅ Domain-level | `RestaurantId` filters enforce multi-tenant boundary at query level |
| **Input validation** | ✅ Automatic | `[ApiController]` + DataAnnotations reject malformed input |
| **No over-posting** | ✅ By design | Dedicated request models per endpoint — never bind directly to domain entities |
| **Rate limiting** | ⬜ Post-MVP | `Microsoft.AspNetCore.RateLimiting` middleware with sliding-window per tenant |
| **CORS policy** | ⬜ Post-MVP | Restrict allowed origins if Blazor WASM or external clients added |
| **HTTPS enforcement** | ✅ Infrastructure | `UseHttpsRedirection()` in middleware pipeline |
| **Request size limits** | ⬜ Post-MVP | `[RequestSizeLimit]` on POST endpoints to prevent abuse |
| **Audit logging** | ⬜ Post-MVP | Log all state-changing operations (create, update, cancel, delete) with user identity |

#### 5.4.11 Idempotency & Concurrency

| Practice | Status | Notes |
|----------|--------|-------|
| **GET is always safe** | ✅ | No side effects — reads only |
| **DELETE is idempotent** | ✅ | Deleting an already-deleted order returns `404`, not `500` |
| **POST cancel is idempotent** | ✅ | Cancelling an already-cancelled order returns domain error via `400` |
| **ETag-based optimistic concurrency** | ⬜ Post-MVP | `If-Match` header with row version to prevent lost updates |
| **Idempotency keys for POST create** | ⬜ Post-MVP | `Idempotency-Key` header to prevent duplicate order creation on retry |

#### 5.4.12 API Observability

| Practice | Status | Implementation |
|----------|--------|---------------|
| **Structured logging per endpoint** | ✅ | `LogDebug` for queries, `LogInformation` for commands |
| **`traceId` in all error responses** | ✅ | `HttpContext.TraceIdentifier` in ProblemDetails extensions |
| **Request/response logging middleware** | ⬜ Post-MVP | Log request path, status code, and duration for all API calls |
| **Health check endpoint** | ⬜ Post-MVP | `/health` with database connectivity check |
| **Metrics** | ⬜ Post-MVP | Request count, latency percentiles, error rate per endpoint |

---

## 6. Blazor Components (✅ Complete — Phase 6)

| Component | Route | Purpose | Pattern |
|-----------|-------|---------|---------|
| `OrderList.razor` + `.razor.cs` | `/orders` | Table with status filter, inline status update dropdown, cancel modal, delete modal | State container + client service |
| `OrderDetail.razor` + `.razor.cs` | `/orders/{id}` | Full order view with status update card, cancel modal | Detail state container |
| `OrderForm.razor` + `.razor.cs` | `/orders/new` | Create new order (select dishes, quantities) | Form with validation |
| `IOrderClientService` | — | Client service interface (7 methods) | Adapter pattern |
| `OrderClientService` | — | HTTP calls to Order API | `Result<T>` pattern |
| `OrderListState` | — | List state — load, filter, status update, cancel, delete | Extends `ComponentStateBase` |
| `OrderDetailState` | — | Detail state — load, status update, cancel | Extends `ComponentStateBase` |
| NavMenu update | — | Add "Orders" link | Existing pattern |

### 6.1 Reference Patterns (from Restaurant feature)

- **State Container**: `ComponentStateBase` → `RestaurantListState` / `RestaurantDetailState`
- **Client Service**: `IRestaurantClientService` → `RestaurantClientService` (calls API via `HttpClient`)
- **Code-Behind**: `.razor.cs` files for all pages
- **Shared Components**: `ErrorAlert`, `LoadingSpinner`, `NotFoundAlert`, `DetailCard`, `StatItem`
- **Error Handling**: `Result<T>` (from `Application.Common`) + `ApiErrorHelper` for HTTP response parsing
- **ProblemDetails**: `ProblemDetailsResponseDto` for RFC 7807 errors

### 6.2 Blazor CRUD Pattern Reference

> **For AI Agents**: Use this section as the canonical pattern when implementing full CRUD UI for any feature module.
> The Order module extends the Restaurant read-only list/detail pattern with **inline mutations**.

#### State Container — CRUD Method Map

Each `ComponentStateBase<T>` subclass should expose:

| Concern | List State Methods | Detail State Methods |
|---------|-------------------|---------------------|
| **Read** | `LoadAsync(restaurantId)`, `ReloadAsync()` | `LoadAsync(id)` |
| **Filter** | `FilteredOrders` (computed), `SelectedStatusFilter` | — |
| **Status Update** | `UpdateStatusAsync(orderId, statusId)` | `UpdateStatusAsync(orderId, statusId)` |
| **Cancel** | `ConfirmCancel(order)` / `DismissCancel()` / `CancelOrderAsync()` | `ShowCancelConfirmation()` / `HideCancelConfirmation()` / `CancelOrderAsync(orderId)` |
| **Delete** | `ConfirmDelete(order)` / `CancelDelete()` / `DeleteAsync()` | — (navigate to list for delete) |
| **Statuses** | `Statuses` (loaded once, used for filter + dropdown) | `Statuses` (loaded with order, used for dropdown) |

#### List Page — Inline Action Column Pattern

For non-terminal entities, render per-row action buttons:

```
[👁 View] [🔄 Status ▾] [⊘ Cancel] [🗑 Delete]
```

- **Status dropdown**: Bootstrap `data-bs-toggle="dropdown"` with `dropdown-menu-end`; excludes current status
- **Cancel button**: Opens modal with required reason `<textarea>` (min 3 chars)
- **Delete button**: Opens simple confirmation modal
- **Terminal rows**: Show only `[👁 View]`

#### Cancel-with-Reason Modal Pattern

Used on both List and Detail pages:

| Element | Implementation |
|---------|---------------|
| Trigger | Button sets modal visibility via state (`ShowCancelModal`) |
| Reason input | `<textarea>` bound to local `_cancelReasonInput` (not state) |
| Validation | Client-side: `string.IsNullOrWhiteSpace` + `Length < 3` → show inline error |
| Submit | Sets `State.CancelReason`, calls `State.CancelOrderAsync()`, clears local input |
| Dismiss | Resets local input + validation flag, calls `State.DismissCancel()` |
| Busy state | `IsCancelling` disables all modal controls + shows spinner |

#### Status Filter Bar Pattern (List page only)

- Loaded once via `GetStatusesAsync(restaurantId)` during `LoadAsync`
- Rendered as horizontal button group: `[All] [Pending] [Confirmed] [Preparing] ...`
- Active filter highlighted with `btn-primary`; inactive with `btn-outline-secondary`
- `FilteredOrders` is a computed property — filters `Data` client-side by status name
- "No matches" row shown inside `<tbody>` with clear-filter link

#### Detail Page — Status Update Card Pattern

- Rendered in right sidebar, only for non-terminal orders
- `<select>` dropdown bound to local `_selectedStatusId`, excludes current status
- Apply button disabled when `_selectedStatusId == 0` or `IsUpdatingStatus`
- After update: full `LoadAsync(id)` reload to reflect all server-side changes

---

## 7. Validation Strategy (MVP)

### 7.1 Validation Layers

| Layer | Validation Type | Implementation |
|-------|----------------|----------------|
| **DTO** | DataAnnotations | `[Required]`, `[StringLength]`, `[Range]`, etc. |
| **API** | ModelState | `ValidateModelActionFilter` (global) |
| **Domain** | IValidatableObject | `Order.Validate()` — business rule validation |
| **Domain** | Guard Clauses | `Order.AddItem()`, `Order.Place()`, `Order.Cancel()`, `Order.Complete()`, `Order.UpdateStatus()`, `Order.AssignStaffMember()` — method-level guards |

### 7.2 Post-MVP Enhancement

> **Decision**: FluentValidation validators skipped for MVP.
> - DataAnnotations sufficient for basic validation
> - FluentValidation adds value with CQRS pipeline behaviors
> - Will be implemented during Post-MVP CQRS refactoring

---

## 8. Performance Optimization

### 8.1 Database Indexes

- `IX_Orders_CreatedAt` — Sort/filter by creation date (existing)
- `IX_Orders_Restaurant_Date` — Filter orders by restaurant + date (planned)
- `IX_Orders_Customer_Date` — Filter customer order history (planned)
- `IX_Orders_Restaurant_Status` — Filter by restaurant + status (planned)

### 8.2 Caching Strategy

- Order analytics cache invalidation via `UpdateOrderAnalyticsHandler`
- Status list cached per restaurant (frequently accessed, rarely changes)
- Cache invalidation on order status updates

---

## Implementation Checklist

### Phase 1: Domain Layer ✅ COMPLETE (Reviewed 2026-03-14)
- [x] Order Aggregate Root (Tier 1 Rich DDD — ~800 lines)
- [x] OrderItem Child Entity (~200 lines)
- [x] OrderStatus Lookup Entity
- [x] Domain Events (OrderPlacedEvent, OrderCompletedEvent, OrderCancelledEvent, cross-aggregate SaleRecordedEvent per item)
- [x] OrderDomainException
- [x] Order Lifecycle (Place, Cancel, Complete with domain events)
- [x] Domain Review — exception correctness, guard clause consistency, null safety (8 fixes applied)

### Phase 2: EF Core & Infrastructure ✅ COMPLETE
- [x] OrderConfiguration (IEntityTypeConfiguration)
- [x] DbSet registrations (Orders, OrderItems, OrderStatuses)
- [x] Generic Repository compatibility
- [x] Seed data (3 orders per restaurant, 6 statuses, customers, staff)

### Phase 3: Application Layer — Event Handlers ✅ COMPLETE
- [x] AwardLoyaltyPointsHandler (OrderPlacedEvent)
- [x] SendKitchenNotificationHandler (OrderPlacedEvent)
- [x] SendOrderConfirmationHandler (OrderPlacedEvent)
- [x] UpdateOrderAnalyticsHandler (OrderPlacedEvent)
- [x] OrderCompletedHandler (OrderCompletedEvent)
- [x] OrderCancelledHandler (OrderCancelledEvent)
- [x] SaleRecordedHandler (SaleRecordedEvent) — persists SaleRecord entity + analytics logging + cache invalidation

### Phase 4: Application Layer — DTOs & Service ✅ COMPLETE (2026-03-14)
- [x] OrderDTO, OrderDetailDTO, OrderItemDTO
- [x] OrderCreateDTO, OrderItemCreateDTO
- [x] OrderUpdateDTO
- [x] OrderStatusDTO
- [x] IOrderService interface (5 queries + 4 commands)
- [x] OrderService implementation (Result pattern, structured logging)
- [x] OrderMappingExtensions (Order, OrderItem, OrderStatus → DTOs)
- [x] GlobalDtoUsings.cs entries (7 aliases)
- [x] DI Registration in ApplicationServiceCollectionExtensions
- [x] Added Orders + OrderStatuses repositories to IUnityOfWork + UnityOfWork

### Phase 5: API Layer ✅ COMPLETE (2026-03-15)

> **For AI Agents — MVP/Post-MVP Split**: Phase 5 MVP scope = controller code + pagination (§5.4.6).
> The 7 items marked ⏸️ below are **Post-MVP** per §5.4.10/§5.4.11/§5.4.12 internal status markers.
> Cross-reference: [Tracker Phase 5](ORDER_MODULE_IMPLEMENTATION_TRACKER.md), [Post-MVP Tracker](ORDER_POST_MVP_TASK_TRACKER.md).

- [x] OrdersController (7 endpoints — 3 GET, 1 POST, 1 PATCH, 1 POST cancel, 1 DELETE)
- [x] GlobalDtoUsings.cs entries (API — 7 Order DTO aliases + namespace import)
- [x] Swagger XML documentation (ProducesResponseType + XML comments on all endpoints)
- [x] Request models (OrderStatusUpdateRequest, OrderCancelRequest) with DataAnnotations
- [x] §5.4 API Design Best Practices documentation (12 subsections — canonical reference for all modules)
- [x] Pagination, sorting & filtering — `PaginatedRequest` shared class + `GET /orders/paginated` endpoint (§5.4.6)
- [ ] ⏸️ Post-MVP: Rate limiting middleware — sliding-window per tenant (§5.4.10)
- [ ] ⏸️ Post-MVP: ETag-based optimistic concurrency — `If-Match` header (§5.4.11)
- [ ] ⏸️ Post-MVP: Idempotency keys for POST create — `Idempotency-Key` header (§5.4.11)
- [ ] ⏸️ Post-MVP: Health check endpoint — `/health` with DB connectivity (§5.4.12)
- [ ] ⏸️ Post-MVP: Request/response logging middleware — path, status code, duration (§5.4.12)
- [ ] ⏸️ Post-MVP: `[Authorize]` on controller — authentication for all Order endpoints (§5.4.10)
- [ ] ⏸️ Post-MVP: Audit logging — state-changing operations with user identity (§5.4.10)

### Phase 6: Blazor UI ✅ COMPLETE (2026-03-14)
- [x] OrderList.razor + .razor.cs — table with status filter bar, inline status dropdown, cancel modal, delete modal
- [x] OrderDetail.razor + .razor.cs — detail view with status update card, cancel modal
- [x] OrderForm.razor + .razor.cs — create form with dynamic item management
- [x] IOrderClientService interface (7 methods)
- [x] OrderClientService implementation (Result pattern, ApiErrorHelper)
- [x] OrderListState — load, filter, inline status update, cancel, delete
- [x] OrderDetailState — load, status update, cancel, statuses
- [x] NavMenu update (add "Orders" link)
- [x] GlobalDtoUsings.cs entries (Server — namespace + 7 aliases)
- [x] DI Registration (ServiceCollectionExtensions.cs)
- [x] Full CRUD from List page (view, inline status update, cancel with reason, delete)
- [x] Full CRUD from Detail page (status update card, cancel with reason modal)

### Phase 7: Integration & Testing ✅ COMPLETE (2026-03-15)
- [x] Dashboard integration — Order Metrics section on `Dashboard.razor` (summary stats, status breakdown, per-restaurant table)
- [x] Manual UI testing checklist

### Phase 8: Event-Driven Sale Record Creation ✅ COMPLETE (2026-03-21)
- [x] `Order.Complete()` raises `SaleRecordedEvent` per `OrderItem` (Domain layer — cross-aggregate event)
- [x] `OrderService.UpdateStatusAsync()` detects terminal statuses and calls `Complete()`/`Cancel()` domain methods with `Dish`+`Category` includes
- [x] `SaleRecordedHandler` injects `IUnityOfWork`, creates `SaleRecord` via constructor + `Money` value object, persists to database
- [x] `UnityOfWork.SaveChangesAsync()` made transaction-aware — checks `CurrentTransaction` to prevent nested transaction crash (ORD-003)

---

## Related Documentation

| Document | Location | Description |
|----------|----------|-------------|
| **Reference Implementation Guide** | `docs/08-Patterns/REFERENCE_IMPLEMENTATION_GUIDE.md` | **Canonical patterns — Restaurant as golden path** |
| Code Inventory | `docs/07-Features/02-OrderManagement/ORDER_MANAGEMENT_CODE_INVENTORY.md` | Pre-implementation code inventory |
| Implementation Tracker | `docs/07-Features/02-OrderManagement/ORDER_MODULE_IMPLEMENTATION_TRACKER.md` | Progress tracking |
| Post-MVP Task Tracker | `docs/07-Features/02-OrderManagement/ORDER_POST_MVP_TASK_TRACKER.md` | Post-MVP backlog (deferred tasks only) |
| MVP Prioritization | `docs/01-Overview/MVP_FEATURE_PRIORITIZATION.md` | Overall MVP plan |
| Coding Standards | `AI/Prompts/CODING-STANDARD-PROMPT.md` | Development guidelines |
| Blazor State Pattern | `docs/08-Patterns/BLAZOR_STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md` | State container reference |
| Vertical Slice ADR | `docs/02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md` | Architecture decision |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 3.6 | 2026-03-21 | **§9 Bugs & Fixes** — added dedicated section documenting ORD-003a/b/c bug chain: root causes, error messages, call chains, before/after code, fix dependency diagram, and files changed summary. Expanded from single ORD-003 to 3 traceable sub-issues for future reference. |
| 3.5 | 2026-03-21 | **Phase 8: Event-Driven Sale Records**
| 3.4 | 2026-03-15 | Phase 7 dashboard integration complete
| 3.3 | 2026-03-15 | Phase 7 MVP scope reduced — removed unit tests and integration tests from MVP checklist (deferred to Post-MVP `ORD-TEST-001`/`002`); Phase 7 now 2 items: dashboard integration + manual UI testing checklist |
| 3.2 | 2026-03-15 | Phase 5 MVP complete — §5.4.6 pagination implemented (`PaginatedRequest` + `GET /orders/paginated`); §5.4.6 status updated to ✅; Phase 5 section header and checklist updated; §5 header changed from ⏳ to ✅ |
| 3.1 | 2026-03-15 | Updated cross-references — Pending Task Tracker renamed to Post-MVP Task Tracker (v2.0); PERF/TD items reclassified as Post-MVP; Related Documentation table updated |
| 3.0 | 2026-03-15 | Phase 5 synchronization — annotated 7 pending items as ⏸️ Post-MVP (per §5.4.10/§5.4.11/§5.4.12 markers); only pagination (§5.4.6) remains MVP pending; added AI agent cross-reference note to Phase 5 checklist; aligned with Tracker v2.0 and Pending Tracker v1.9 |
| 2.9 | 2026-03-14 | Phase 5 marked ⏳ In Progress — controller code complete but §5.4 best practices not yet implemented in code; §5.4.6 status corrected from 'post-MVP' to 'MVP'; added 8 unchecked items to Phase 5 checklist (pagination, rate limiting, ETag, idempotency, health checks, logging middleware, auth, audit) |
| 2.8 | 2026-03-14 | Added §5.4 API Design Best Practices (12 subsections) — RESTful design, HTTP methods/status codes, RFC 7807 ProblemDetails, request model conventions, pagination/sorting/filtering, controller design rules, Swagger, versioning, security, idempotency/concurrency, observability. Prescriptive rules for AI agents as canonical API pattern for all modules |
| 2.7 | 2026-03-14 | Accuracy fixes — §4.3 CancelAsync/DeleteAsync return types corrected to `Result` (non-generic), §4.4 mapping extensions marked ✅ Complete, §4.3/§5.2 stale 'Planned' labels updated, phantom `ClientResult<T>` replaced with actual `Result<T>`, metadata subtitle cleaned |
| 2.6 | 2026-03-14 | Phase 6 enriched — added §6.2 Blazor CRUD Pattern Reference (inline status update, cancel-with-reason modal, status filter bar, detail status card); updated checklist with full CRUD details |
| 2.5 | 2026-03-14 | Phase 6 complete — 3 Blazor pages, IOrderClientService/OrderClientService, OrderListState/OrderDetailState, NavMenu, DI, Server GlobalDtoUsings |
| 2.4 | 2026-03-14 | Phase 5 complete
| 2.3 | 2026-03-14 | Phase 4 complete
| 2.2 | 2026-03-14 | Restructured §2–§6 to match Tracker phase order
| 2.1 | 2026-03-14 | Domain layer review — 8 fixes applied (XML doc exceptions, guard clauses, null safety, dead code removal) |
| 2.0 | 2026-03-14 | Complete rewrite — aligned with Code Inventory, Restaurant reference structure, Clean Architecture + DDD patterns |
| 1.0 | TBD | Initial draft (generic implementation guide) |

---

*This guide follows Clean Architecture + DDD patterns as implemented in the SmartMenuOptimizer codebase. For actual code implementations, refer to the source files directly.*