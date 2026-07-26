# Issues & Quick Fixes

> Documentation for bugs found and fixed during SmartMenuOptimizer development.

**Date Range**: 2026-03-15 — present

---

## Index

### Legacy Format (Pre-existing — 2026-03-15)

| # | Issue | Layer | Severity | Status |
|---|-------|-------|----------|--------|
| [001](./001_QUERY_APPLICATION.md) | EF Core global query filter excluded `OrderItems` from includes | Application | High | ✅ Fixed |
| [002](./002_NULL-REF_UI.md) | `NullReferenceException` in `LoadOrderCountsAsync` on Restaurant List page | UI | High | ✅ Fixed |
| [003](./003_UI_UI.md) | Restaurant cards showed misleading "50 orders" stat | UI | Medium | ✅ Fixed |
| [004](./004_DATA_UI.md) | Order List and Order Form hardcoded to Restaurant ID 1 | UI | Medium | ✅ Fixed |

### Standardized Format (BUG-NNN — 2026-03-21+)

| # | Issue | Layer | Severity | Feature | Status |
|---|-------|-------|----------|---------|--------|
| [BUG-001](./BUG-001__CREATEDAT_ACTION_ROUTE_MISMATCH__API__ORDER_MANAGEMENT.md) | `CreatedAtAction` route mismatch due to `SuppressAsyncSuffixInActionNames` | API | High | Order Management | ✅ Fixed |
| [BUG-002](./BUG-002__DISH_CLIENT_SERVICE_NOT_REGISTERED__UI__ORDER_MANAGEMENT.md) | `IDishClientService` not registered in Server DI | UI | High | Order Management | ✅ Fixed |
| [BUG-003](./BUG-003__UPDATE_STATUS_BYPASSED_DOMAIN_METHODS__APPLICATION__ORDER_MANAGEMENT.md) | `UpdateStatusAsync` bypassed domain methods — no events on terminal statuses | Application | Critical | Order Management | ✅ Fixed |
| [BUG-004](./BUG-004__SALE_HANDLER_NO_PERSISTENCE__APPLICATION__ORDER_MANAGEMENT.md) | `SaleRecordedHandler` never persisted `SaleRecord` entities | Application | Critical | Order Management | ✅ Fixed |
| [BUG-005](./BUG-005__NESTED_TRANSACTION_CRASH__INFRASTRUCTURE__ORDER_MANAGEMENT.md) | Nested transaction crash in `UnityOfWork.SaveChangesAsync()` | Infrastructure | Critical | Order Management | ✅ Fixed |

### Environment / Setup

| # | Issue | Layer | Severity | Status |
|---|-------|-------|----------|--------|
| [006](./006_DATABASE-LOGIN-FAILED-ON-STARTUP__API.md) | Database Login Failed on Startup — `28P01` Postgres password rejected | API | Critical | ✅ Fixed |

### UI / UX Enhancements

| # | Issue | Layer | Severity | Feature | Status |
|---|-------|-------|----------|---------|--------|
| [007](./007_AI-SUGGESTION-MODAL-MARKDOWN-FORMAT__UI.md) | AI Suggestion modal showed raw markdown in monospace box (💡 Improve) | UI | Medium | AI Menu Insights | ✅ Fixed |
| [008](./008_INSIGHTS-NO-RECOMMENDATIONS__API_UI.md) | `/insights` always "No recommendations" — `[Required] CustomerName` on `ReviewDTO` auto-400s the `[ApiController]` recommend endpoint | API + UI | High | AI Menu Insights | ✅ Fixed |

---

## Root Cause Categories

### ASP.NET Core Route Convention Mismatch (BUG-001)
ASP.NET Core's default `SuppressAsyncSuffixInActionNames = true` strips the "Async" suffix from method names when generating route names, but `nameof()` returns the full C# method name. `CreatedAtAction(nameof(GetByIdAsync), ...)` failed because the route system registered the action as "GetById", not "GetByIdAsync". **Pattern**: Set `SuppressAsyncSuffixInActionNames = false` globally in `AddControllers()` options, or use `nameof()` without the "Async" suffix.

### Missing DI Registration (BUG-002)
New service interfaces added to Blazor components require corresponding DI registrations. The `OrderForm` component injected `IDishClientService` to load dish dropdowns, but the service was never registered in `ServiceCollectionExtensions`. **Pattern**: When adding a new `@inject` to a Blazor component, immediately add the corresponding `AddScoped<>()` registration. Consider automated DI validation tests.

### Application Service Bypassing Domain Behavior (BUG-003)
The application service called a generic status setter (`order.UpdateStatus()`) that raised no domain events, instead of the rich domain methods (`order.Complete()`, `order.Cancel()`) that encapsulate lifecycle behavior and event raising. This violated the DDD aggregate pattern — the application layer should always use behavioral domain methods for state transitions with side effects. **Pattern**: Application services must detect the semantic intent of a status change (terminal vs. intermediate) and call the appropriate domain method. Never use generic setters for state transitions that have business rules or domain events.

### Event Handler Missing Persistence Logic (BUG-004)
The `SaleRecordedHandler` was implemented with analytics logging and cache invalidation but lacked the core responsibility — creating `SaleRecord` entities in the database. The handler had no `IUnityOfWork` dependency and no persistence code. **Pattern**: Event handlers that create cross-aggregate entities must inject the unit of work and persist entities as their primary responsibility. Analytics and caching are secondary concerns.

### Nested Transaction in Domain Event Dispatch Cycle (BUG-005)
`UnityOfWork.SaveChangesAsync()` unconditionally called `BeginTransactionAsync()`. Domain event handlers are dispatched by `AppDbContext.SaveChangesAsync()` **inside** the existing transaction from the outer `UoW.SaveChangesAsync()`. When a handler called `_unitOfWork.SaveChangesAsync()`, it tried to begin a second transaction on the same connection, which PostgreSQL/EF Core does not support. **Pattern**: Always check `Database.CurrentTransaction != null` before calling `BeginTransactionAsync()`. If a transaction is already active, participate in it instead of starting a new one. This ensures event handler persistence is atomic with the outer operation.

### Local Postgres Password Mismatch (006)
API loads its connection string from **user-secrets** in dev (`Program.cs` clears config sources, adds user-secrets when `IsDevelopment()` and not in Docker). `28P01 password authentication failed` means the secret's password (`copoadmin123`) no longer matches the local Postgres server password. **Pattern**: Realign the two — reset the server password via temporary `trust` auth in `pg_hba.conf`, or update the secret with `dotnet user-secrets set`. See [006](./006_DATABASE-LOGIN-FAILED-ON-STARTUP__API.md) for full app credentials + reset steps.

### `[Required]` on a Transport DTO Auto-400s an `[ApiController]` Endpoint (008)
`[ApiController]` recursively validates the request body — including every element of a `List<T>` — against its data annotations *before* the action body runs. A `[Required]` on a field that is legitimately empty in stored data (`ReviewDTO.CustomerName`, empty for customer-linked reviews) rejects the whole call with HTTP 400 at model binding, so no action code executes. **Pattern**: Put input validation on the input model (`ReviewFormModel`), not on the transport/read DTO used as a request body. Keep only constraints that hold for all stored data (e.g. `[StringLength]`) on the DTO. See [008](./008_INSIGHTS-NO-RECOMMENDATIONS__API_UI.md).

### Cross-Bug Dependency: BUG-003 → BUG-004 → BUG-005

These three bugs form a **causal chain** — all three had to be fixed for sale record creation to work:

```
BUG-003 (no events raised)
    ↓ fix: call order.Complete() for terminal statuses
BUG-004 (handler doesn't persist)
    ↓ fix: add IUnityOfWork + SaleRecord creation
BUG-005 (nested transaction crash)
    ↓ fix: CurrentTransaction check in UoW
✅ SaleRecords now persist atomically with order completion
```

---

## Cross-Reference

| Document | Location |
|----------|----------|
| Order Implementation Tracker — Known Issues | `docs/07-Features/02-OrderManagement/ORDER_MODULE_IMPLEMENTATION_TRACKER.md` §⚠️ Known Issues |
| Order Implementation Plan — §9 Bugs & Fixes | `docs/07-Features/02-OrderManagement/ORDER_MODULE_IMPLEMENTATION_PLAN.md` §9 |

---

## File Name Convention

```
BUG-{NNN}__{ISSUE_NAME}__{LAYER}__{FEATURE}.md
```

- `{NNN}`: Zero-padded sequential number
- `{ISSUE_NAME}`: Snake_case description (uppercase)
- `{LAYER}`: Affected architecture layer(s) — `INFRASTRUCTURE`, `APPLICATION`, `UI`, `DOMAIN`, `API`, or combinations like `APPLICATION_UI`
- `{FEATURE}`: Feature or module where the bug was found (e.g., `ORDER_MANAGEMENT`, `RESTAURANT_MANAGEMENT`)
