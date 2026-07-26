# 📦 Order Management — Code Inventory (Pre-Implementation)

> **SmartMenuOptimizer — Phase 6: Order Management Module**  
> **Created**: 2026-03-14  
> **Purpose**: Inventory of all existing code related to Order Management across all layers, prior to Application/API/Blazor implementation.

---

> **🤖 For AI Agents — Document Guide**
>
> | Aspect | Details |
> |--------|---------|
> | **Document Type** | Pre-implementation code inventory — point-in-time snapshot of what existed before Phase 4–6 build |
> | **Status** | ⚠️ **Historical snapshot** (2026-03-14) — §9 Gap Analysis items are now ✅ implemented. See [Implementation Tracker](ORDER_MODULE_IMPLEMENTATION_TRACKER.md) for current state |
> | **Use As** | Reference for understanding what was pre-existing (Domain, Infrastructure, Event Handlers) vs what was built during Phases 4–8 |
> | **Architecture** | [ADR-005 — Vertical Slice + Aggregate-Centric](../../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md) |
> | **Event-Driven Pattern** | §3 Application Layer lists 6+1 event handlers; for the canonical pattern framework, see [EVENT_DRIVEN_ARCHITECTURE_PATTERN.md](../../08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md) |
> | **Companion Docs** | [Implementation Plan](ORDER_MODULE_IMPLEMENTATION_PLAN.md) (spec) · [Tracker](ORDER_MODULE_IMPLEMENTATION_TRACKER.md) (progress) · [Post-MVP Tracker](ORDER_POST_MVP_TASK_TRACKER.md) (deferred) |
> | **Do Not** | Treat §9 Gap Analysis as current backlog — those items are completed; do not update this snapshot document |

---

## 📑 Table of Contents

1. [Inventory Summary](#-inventory-summary)
2. [Domain Layer (✅ Complete)](#-domain-layer--complete)
3. [Infrastructure Layer (✅ Complete)](#-infrastructure-layer--complete)
4. [Application Layer (🟡 Partial — Event Handlers Only)](#-application-layer--partial)
5. [API Layer (❌ Pending)](#-api-layer--pending)
6. [Blazor Server Layer (❌ Pending)](#-blazor-server-layer--pending)
7. [Shared / Cross-Cutting (✅ Reusable)](#-shared--cross-cutting--reusable)
8. [Seed Data (✅ Complete)](#-seed-data--complete)
9. [Gap Analysis — What Needs to Be Built](#-gap-analysis--what-needs-to-be-built)

---

## 📊 Inventory Summary

| Layer | Status | Existing Files | Files Needed |
|-------|--------|----------------|--------------|
| **Domain** | ✅ Complete | 7 files | 0 |
| **Infrastructure** | ✅ Complete | 1 file (+ DbContext registration) | 0 (possibly move to feature folder) |
| **Application** | 🟡 Partial | 7 files (event handlers + 1 pricing stub) | ~6–8 (DTOs, Service, Mappings) |
| **API** | ❌ Pending | 0 files | ~1–2 (Controller) |
| **Blazor Server** | ❌ Pending | 0 files | ~6–10 (Pages, Services, State) |

---

## 🟦 Domain Layer — ✅ Complete

The Order aggregate is fully implemented as a **Tier 1 Full Aggregate Root (Rich DDD)**.

### Order Aggregate Root

| File | Path | Description |
|------|------|-------------|
| `Order.cs` | `Domain\Aggregates\OrderAggregate\Order.cs` | **794 lines** — Full aggregate root with rich behavior |
| `OrderItem.cs` | `Domain\Aggregates\OrderAggregate\OrderItem.cs` | **~200 lines** — Child entity with private setters, internal constructor |

#### `Order.cs` — Key Characteristics

- **Base Class**: `TenantEntityBase` (multi-tenant), implements `IValidatableObject`
- **Table**: `[Table("Orders")]`
- **Private Collection**: `List<OrderItem> _orderItems` with read-only `Items` property
- **Domain Events Collection**: `List<IDomainEvent> _domainEvents` with `DomainEvents` property

**Properties** (all private setters):
| Property | Type | Notes |
|----------|------|-------|
| `CustomerId` | `int` | FK to `Customer` entity (Required) |
| `OrderStatusId` | `int` | FK to `OrderStatus` entity (Required) |
| `TotalAmount` | `decimal(18,2)` | Auto-calculated from items |
| `OrderDate` | `DateTime` | UTC, defaults to `DateTime.UtcNow` |
| `SpecialInstructions` | `string?` | MaxLength 1000 |
| `HandledByStaffId` | `int?` | FK to `StaffMember` (Optional) |

**Navigation Properties**:
| Nav Property | Type | Relationship |
|-------------|------|--------------|
| `Status` | `OrderStatus` | Required |
| `Customer` | `Customer?` | Required |
| `OrderItems` | `ICollection<OrderItem>` | One-to-Many (cascade delete) |
| `HandledBy` | `StaffMember?` | Optional |

**Business Methods**:
| Method | Signature | Description |
|--------|-----------|-------------|
| Constructor | `Order(int restaurantId, int customerId, int orderStatusId, string? specialInstructions)` | Creates new order |
| `AddItem` | `void AddItem(int dishId, string dishName, decimal unitPrice, int quantity, string? specialInstructions)` | Factory method for OrderItem child entities |
| `RemoveItem` | `void RemoveItem(int orderItemId)` | Removes item, recalculates total |
| `UpdateItemQuantity` | `void UpdateItemQuantity(int orderItemId, int newQuantity)` | Updates quantity, recalculates total |
| `UpdateStatus` | `void UpdateStatus(int newOrderStatusId)` | Simple status update |
| `SetSpecialInstructions` | `void SetSpecialInstructions(string? instructions)` | Updates instructions |
| `AssignStaffMember` | `void AssignStaffMember(int staffMemberId)` | Assigns handler |
| `UnassignStaffMember` | `void UnassignStaffMember()` | Clears handler |
| `GetItemCount` | `int GetItemCount()` | Items count |
| `GetTotalQuantity` | `int GetTotalQuantity()` | Sum of all item quantities |
| `Place` | `void Place(int confirmedStatusId, string? orderType)` | Places order → raises `OrderPlacedEvent` |
| `Cancel` | `void Cancel(int cancelledStatusId, string reason, CancellationSource cancelledBy, ...)` | Cancels order → raises `OrderCancelledEvent` |
| `Complete` | `void Complete(int completedStatusId, int loyaltyPointsEarned, string orderType)` | Completes order → raises `OrderCompletedEvent` |
| `ValidateTenantConsistency` | `void ValidateTenantConsistency()` | Multi-tenant boundary validation |
| `Validate` | `IEnumerable<ValidationResult> Validate(...)` | IValidatableObject implementation |

**Order Lifecycle**:
```
Pending → Confirmed → Preparing → Ready → In Delivery → Completed
   ↓                                  ↓
Cancelled ←─────────────────────────┘
```

#### `OrderItem.cs` — Key Characteristics

- **Base Class**: `TenantEntityBase`, implements `IValidatableObject`
- **Table**: `[Table("OrderItems")]`
- **Internal Constructor**: `internal OrderItem(int dishId, decimal unitPrice, int quantity)` — only creatable via `Order.AddItem()`
- **Computed Property**: `[NotMapped] decimal Subtotal => Quantity * UnitPrice`
- **Internal Method**: `internal void UpdateQuantity(int newQuantity)` — only callable via `Order.UpdateItemQuantity()`

### Domain Events (3 files)

| File | Path | Trigger | Key Properties |
|------|------|---------|----------------|
| `OrderPlacedEvent.cs` | `Events\OrderPlacedEvent.cs` | `Order.Place()` | `OrderId`, `RestaurantId`, `CustomerId`, `TotalAmount`, `CurrencyCode`, `ItemCount`, `SpecialInstructions`, `OrderType` |
| `OrderCompletedEvent.cs` | `Events\OrderCompletedEvent.cs` | `Order.Complete()` | `OrderId`, `RestaurantId`, `CustomerId`, `FinalTotal`, `ItemCount`, `OrderPlacedAt`, `CompletedAt`, `FulfillmentTimeMinutes` (computed), `OrderType`, `LoyaltyPointsEarned`, `AppliedPromotions` |
| `OrderCancelledEvent.cs` | `Events\OrderCancelledEvent.cs` | `Order.Cancel()` | `OrderId`, `RestaurantId`, `CustomerId`, `CancellationReason`, `CancelledBy` (enum), `OrderTotal`, `PreviousStatus`, `RequiresRefund`, `LoyaltyPointsToReverse` |

### Domain Errors (1 file)

| File | Path | Description |
|------|------|-------------|
| `OrderDomainException.cs` | `Errors\OrderDomainException.cs` | Extends `DomainException`, includes optional `OrderId` property |

### Supporting Domain Entities

| File | Path | Description |
|------|------|-------------|
| `OrderStatus.cs` | `Domain\Entities\RestaurantEntities\OrderStatus.cs` | Tier 2 lookup aggregate — `TenantEntityBase` with `Name`, `Description`, `DisplayOrder`, `IsTerminal`, `ColorCode` |
| `CancellationSource` (enum) | Inside `OrderCancelledEvent.cs` | `Customer = 0`, `Staff = 1`, `System = 2` |

### Domain Layer Dependencies Used by Order

| Entity/VO | Path | Relationship |
|-----------|------|-------------|
| `Customer` | `Domain\Entities\ProfileEntities\Customer.cs` | FK `CustomerId` — who placed the order |
| `StaffMember` | `Domain\Entities\ProfileEntities\StaffMember.cs` | FK `HandledByStaffId` — optional handler |
| `Dish` | `Domain\Aggregates\DishAggregate\Dish.cs` | Referenced by `OrderItem.DishId` |
| `TenantEntityBase` | `Domain\Common\TenantEntityBase.cs` | Base class — provides `RestaurantId` |
| `EntityBase` | `Domain\Common\EntityBase.cs` | Provides `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted` |
| `DomainEventBase` | `Domain\Common\DomainEventBase.cs` | Base class for domain events |
| `IDomainEvent` | `Domain\Common\IDomainEvent.cs` | Interface for domain events |
| `DomainException` | `Domain\Exceptions\DomainException.cs` | Base exception for `OrderDomainException` |
| `Money` | `Domain\ValueObjects\Money.cs` | Used in events for currency-aware amounts |

---

## 🟩 Infrastructure Layer — ✅ Complete

### EF Core Configuration

| File | Path | Description |
|------|------|-------------|
| `OrderConfiguration.cs` | `Infrastructure\Persistence\Configurations\OrderConfiguration.cs` | EF Core `IEntityTypeConfiguration<Order>` |

#### Configuration Details

```csharp
builder.ToTable("Orders");
builder.HasKey(o => o.Id);
builder.Property(o => o.Id).ValueGeneratedOnAdd();
builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
builder.Property(o => o.CreatedAt).IsRequired();
builder.Property(o => o.UpdatedAt).IsRequired(false);
builder.HasIndex(o => o.CreatedAt);
builder.HasMany(o => o.OrderItems)
    .WithOne(oi => oi.Order)
    .HasForeignKey(oi => oi.OrderId)
    .OnDelete(DeleteBehavior.Cascade);
```

> **Note**: Currently at `Persistence\Configurations\`. Consider moving to `Features\Orders\Configurations\` to follow vertical slice convention (like `RestaurantConfiguration` → `Features\Restaurants\Configurations\`).

### AppDbContext Registration

| Component | Path | Details |
|-----------|------|---------|
| `DbSet<Order> Orders` | `Infrastructure\Persistence\Context\AppDbContext.cs:85` | ✅ Registered |
| `DbSet<OrderItem> OrderItems` | `Infrastructure\Persistence\Context\AppDbContext.cs:86` | ✅ Registered |
| `DbSet<OrderStatus> OrderStatuses` | `Infrastructure\Persistence\Context\AppDbContext.cs:95` | ✅ Registered |
| `ApplyConfigurationsFromAssembly` | `AppDbContext.OnModelCreating()` | ✅ Auto-discovers `OrderConfiguration` |

### Repository & UoW (Generic — Shared)

| Component | Path | Notes |
|-----------|------|-------|
| `Repository<T>` | `Infrastructure\Persistence\Repositories\Repository.cs` | Generic repository — works with `Order` out of the box |
| `UnityOfWork` | `Infrastructure\Persistence\Repositories\UnityOfWork.cs` | Transaction management — no Order-specific changes needed |

---

## 🟨 Application Layer — 🟡 Partial (Event Handlers Only)

### Existing: Order Event Handlers (6 files)

All handlers extend `ResilientEventHandlerBase<TEvent>` with retry logic + dead letter queue.

| File | Path | Handles Event | Actions |
|------|------|--------------|---------|
| `AwardLoyaltyPointsHandler.cs` | `Handlers\OrderEventHandlers\` | `OrderPlacedEvent` | Calculates & awards loyalty points (1 pt/$1) |
| `SendKitchenNotificationHandler.cs` | `Handlers\OrderEventHandlers\` | `OrderPlacedEvent` | Sends kitchen display notification |
| `SendOrderConfirmationHandler.cs` | `Handlers\OrderEventHandlers\` | `OrderPlacedEvent` | Sends customer confirmation |
| `UpdateOrderAnalyticsHandler.cs` | `Handlers\OrderEventHandlers\` | `OrderPlacedEvent` | Invalidates analytics cache, logs metrics |
| `OrderCompletedHandler.cs` | `Handlers\OrderEventHandlers\` | `OrderCompletedEvent` | Logs completion metrics, invalidates cache, schedules review request |
| `OrderCancelledHandler.cs` | `Handlers\OrderEventHandlers\` | `OrderCancelledEvent` | Reverses loyalty points, sends cancellation notification, updates analytics |

> **Note**: All handlers use `INotificationService` and `ICacheService` contracts. Repository injection is commented/noted as "In a full implementation, inject IRepository<Order>". These are ready for Order service integration.

### Existing: Order Pricing Service (1 file — stub)

| File | Path | Status |
|------|------|--------|
| `OrderPricingApplicationService.cs` | `Application\Features\Orders\Services\` | **Stub only** — all dependencies commented out. Contains pricing logic documentation but no active code. |

### Missing: Core Order Application Components

| Component | Target Path | Status |
|-----------|-------------|--------|
| Order DTOs | `Application\Features\Orders\DTOs\` | ❌ **Not created** |
| `IOrderService` | `Application\Features\Orders\Services\` | ❌ **Not created** |
| `OrderService` | `Application\Features\Orders\Services\` | ❌ **Not created** |
| Order Mapping Extensions | `Application\Features\Orders\Mappings\` | ❌ **Not created** |
| `GlobalDtoUsings.cs` entries | `Application\GlobalDtoUsings.cs` | ❌ **No Order DTO aliases yet** |
| DI Registration | `Application\Extensions\ApplicationServiceCollectionExtensions.cs` | ❌ **No Order service registered** |

---

## 🟥 API Layer — ❌ Pending

### Missing: Order API Controller

| Component | Target Path | Status |
|-----------|-------------|--------|
| `OrdersController` | `API\Features\Orders\v1\OrdersController.cs` | ❌ **Not created** |
| `GlobalDtoUsings.cs` entries | `API\GlobalDtoUsings.cs` | ❌ **No Order DTO aliases yet** |

### Reference Pattern (from `RestaurantsController`)

The API follows:
- `ApiControllerBase` base class
- `ApiResponse` wrapper
- `[ApiVersion("1.0")]` versioning
- `[Route("api/v{version:apiVersion}/[controller]")]`
- XML documentation for Swagger
- RFC 7807 ProblemDetails error responses

---

## 🟥 Blazor Server Layer — ❌ Pending

### Missing: All Order Blazor Components

| Component | Target Path | Status |
|-----------|-------------|--------|
| `OrderList.razor` + `.razor.cs` | `Server\Features\Orders\Components\` | ❌ **Not created** |
| `OrderDetail.razor` + `.razor.cs` | `Server\Features\Orders\Components\` | ❌ **Not created** |
| `OrderForm.razor` + `.razor.cs` | `Server\Features\Orders\Components\` | ❌ **Not created** (for creating new orders) |
| `IOrderClientService` | `Server\Features\Orders\Services\` | ❌ **Not created** |
| `OrderClientService` | `Server\Features\Orders\Services\` | ❌ **Not created** |
| `OrderListState` | `Server\Features\Orders\State\` | ❌ **Not created** |
| `OrderDetailState` | `Server\Features\Orders\State\` | ❌ **Not created** |
| NavMenu update | `Server\Components\Layout\NavMenu.razor` | ❌ **Orders link not added** |

### Reference Patterns (from Restaurant feature)

- **State Container**: `ComponentStateBase` → `RestaurantListState` / `RestaurantDetailState`
- **Client Service**: `IRestaurantClientService` → `RestaurantClientService` (calls API via `HttpClient`)
- **Code-Behind**: `.razor.cs` files for all pages
- **Shared Components**: `ErrorAlert`, `LoadingSpinner`, `NotFoundAlert`, `DetailCard`, `StatItem`
- **Error Handling**: `ClientResult<T>` + `ClientResultExtensions` + `ApiErrorHelper`
- **ProblemDetails**: `ProblemDetailsResponseDto` for RFC 7807 errors

---

## 🔧 Shared / Cross-Cutting — ✅ Reusable

### Patterns Already Available for Order Feature

| Pattern | Location | Reuse Strategy |
|---------|----------|---------------|
| `Result<T>` / `ResultExtensions` | `Application\Common\` | Return type for all Order service methods |
| `PaginatedResponse<T>` | `Application\Common\` | Paginate order listings |
| `ApplicationError` | `Application\Common\` | Standardized error codes |
| `ApiControllerBase` | `API\Common\` | Base class for `OrdersController` |
| `ApiResponse` | `API\Common\` | Wrap all Order API responses |
| `ValidateModelActionFilter` | `API\Filters\` | Auto model validation |
| `ExceptionActionFilter` | `API\Filters\` | Global exception handling |
| `ClientResult<T>` / `ClientResultExtensions` | `Server\Common\` | Client-side error handling |
| `ComponentStateBase` | `Server\State\` | Base for `OrderListState` |
| `ApiErrorHelper` | `Server\Helpers\` | Parse API errors in Blazor |
| `ProblemDetailsResponseDto` | `Server\Models\Api\` | RFC 7807 error model |
| `IRepository<T>` | `Domain\Repositories\` | Generic repository for `Order` |
| `IUnityOfWork` | `Domain\Repositories\` | Transaction support |
| `ResilientEventHandlerBase<T>` | `Application\Handlers\` | Base for event handlers (already used) |
| `INotificationService` | `Application\Contracts\` | Kitchen/customer notifications |
| `ICacheService` | `Application\Contracts\` | Analytics cache invalidation |
| `IDeadLetterQueueService` | `Application\Contracts\` | Failed event capture |
| `IDomainEventDispatcher` | `Application\Contracts\` | MediatR event dispatch |

---

## 🌱 Seed Data — ✅ Complete

### Seeded Order-Related Data

| Data | Seeder Method | Details |
|------|---------------|---------|
| **OrderStatuses** (per restaurant) | `SeedOperationalInfrastructureAsync` | 6 statuses: Pending, Preparing, Ready, Served, Completed (terminal), Cancelled (terminal) |
| **Orders** (sample) | `SeedTransactionalDataAsync` | 3 completed orders per restaurant, 2 items each, assigned to first customer & staff |
| **Customers** | `SeedCustomerAndStaffUsersAsync` | 5 customers seeded (John Doe, Jane Smith, Robert Johnson, Maria Garcia, David Brown) |
| **StaffMembers** | `SeedCustomerAndStaffUsersAsync` | 5 staff (Manager, 2 Waiters, 2 Chefs) assigned to first restaurant |
| **Dishes** (for order items) | `SeedMenuStructureAsync` | ~10 dishes per restaurant (La Bella Italia, Sushi Master) |

---

## 🔍 Gap Analysis — What Needs to Be Built

### Phase 6 Implementation Plan

#### 6.1 — Application Layer DTOs

| DTO | Properties (from Domain) | Notes |
|-----|--------------------------|-------|
| `OrderDTO` | `Id`, `RestaurantId`, `CustomerId`, `CustomerName?`, `OrderStatusId`, `StatusName`, `TotalAmount`, `OrderDate`, `SpecialInstructions`, `HandledByStaffId`, `StaffName?`, `ItemCount` | List/card view |
| `OrderDetailDTO` | All of `OrderDTO` + `Items: List<OrderItemDTO>`, `StatusColorCode`, `IsTerminal` | Detail view |
| `OrderItemDTO` | `Id`, `DishId`, `DishName`, `UnitPrice`, `Quantity`, `Subtotal`, `SpecialInstructions` | Nested in detail |
| `OrderCreateDTO` | `RestaurantId`, `CustomerId`, `SpecialInstructions?`, `Items: List<OrderItemCreateDTO>` | Create form |
| `OrderItemCreateDTO` | `DishId`, `Quantity`, `SpecialInstructions?` | Nested in create |
| `OrderUpdateDTO` | `Id`, `SpecialInstructions?`, `OrderStatusId?` | Update operations |
| `OrderStatusDTO` | `Id`, `Name`, `Description`, `DisplayOrder`, `IsTerminal`, `ColorCode` | Status dropdown |

#### 6.2 — Application Layer Service

| Method | Return Type | Description |
|--------|-------------|-------------|
| `GetByIdAsync(int id)` | `Result<OrderDetailDTO>` | Get order with items |
| `GetAllByRestaurantAsync(int restaurantId)` | `Result<IReadOnlyList<OrderDTO>>` | List orders for restaurant |
| `GetByCustomerAsync(int customerId)` | `Result<IReadOnlyList<OrderDTO>>` | Customer order history |
| `GetByStatusAsync(int restaurantId, int statusId)` | `Result<IReadOnlyList<OrderDTO>>` | Filter by status |
| `CreateAsync(OrderCreateDTO dto)` | `Result<OrderDTO>` | Create new order |
| `UpdateStatusAsync(int id, int newStatusId)` | `Result<OrderDTO>` | Change order status |
| `CancelAsync(int id, string reason)` | `Result<bool>` | Cancel order |
| `DeleteAsync(int id)` | `Result<bool>` | Soft delete |
| `GetStatusesAsync(int restaurantId)` | `Result<IReadOnlyList<OrderStatusDTO>>` | Get available statuses |

#### 6.3 — API Layer

| Endpoint | Method | Route | Description |
|----------|--------|-------|-------------|
| Get Order | `GET` | `api/v1/orders/{id}` | Get order detail |
| List Orders | `GET` | `api/v1/orders?restaurantId={id}` | List by restaurant |
| Create Order | `POST` | `api/v1/orders` | Place new order |
| Update Status | `PATCH` | `api/v1/orders/{id}/status` | Change status |
| Cancel Order | `POST` | `api/v1/orders/{id}/cancel` | Cancel with reason |
| Delete Order | `DELETE` | `api/v1/orders/{id}` | Soft delete |
| Get Statuses | `GET` | `api/v1/orders/statuses?restaurantId={id}` | List available statuses |

#### 6.4 — Blazor Server Layer

| Component | Purpose | Pattern |
|-----------|---------|---------|
| `OrderList.razor` | Table/card view of orders with status filters | State container + client service |
| `OrderDetail.razor` | Full order view with items, status timeline | Detail state container |
| `OrderForm.razor` | Create new order (select dishes, quantities) | Form with validation |
| `OrderClientService` | HTTP calls to Order API | `ClientResult<T>` pattern |
| `OrderListState` | Manages order list state | Extends `ComponentStateBase` |
| `OrderDetailState` | Manages order detail state | Extends `ComponentStateBase` |
| NavMenu update | Add "Orders" link | Existing pattern |

---

## 📚 Related Documentation

| Document | Path |
|----------|------|
| MVP Feature Prioritization | `docs\01-Overview\MVP_FEATURE_PRIORITIZATION.md` |
| Order Implementation Plan | `docs\07-Features\02-OrderManagement\ORDER_MODULE_IMPLEMENTATION_PLAN.md` |
| Order Implementation Tracker | `docs\07-Features\02-OrderManagement\ORDER_MODULE_IMPLEMENTATION_TRACKER.md` |
| Order Post-MVP Task Tracker | `docs\07-Features\02-OrderManagement\ORDER_POST_MVP_TASK_TRACKER.md` |
| Event-Driven Architecture Pattern | `docs\08-Patterns\EVENT_DRIVEN_ARCHITECTURE_PATTERN.md` |
| Domain Events Guide | `SmartMenuOptim.Domain\docs\06-Events\DOMAIN_EVENTS_GUIDE.md` |
| ADR-005 Vertical Slice Architecture | `docs\02-Architecture\ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md` |
| Blazor State Container Pattern | `docs\08-Patterns\BLAZOR_STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md` |
| Coding Standards | `AI\Prompts\CODING-STANDARD-PROMPT.md` |

---

*This inventory was generated on 2026-03-14 to support Phase 6 Order Management implementation.*
