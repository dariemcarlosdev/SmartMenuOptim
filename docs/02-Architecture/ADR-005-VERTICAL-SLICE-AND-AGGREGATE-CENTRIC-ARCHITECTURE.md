# ADR-005: Vertical Slice & Aggregate-Centric Architecture

> **Architecture Decision Record**  
> **Status**: Accepted · **Date**: 2026-03-12  
> **Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## 📋 Context

SmartMenuOptimizer started with a **horizontal (technical) folder structure** — DTOs in one folder, services in another, events in another. As the codebase grew:

- Opening a single feature required navigating 5+ disconnected folders
- Related files (DTO + Service + Component + State) were scattered across the tree
- Adding a new feature touched many unrelated directories

We needed a strategy that groups by **feature** in outer layers, by **aggregate** in Domain, maintains **Clean Architecture** rules, and preserves **backward compatibility**.

---

## ✅ Decision Summary

| Layer | Organized By | Path Pattern |
|-------|-------------|-------------|
| **API** | Feature (vertical slice) | `Features/{Feature}/v1/` |
| **Server (Blazor)** | Feature (vertical slice) | `Features/{Feature}/Components,Services,State,Models/` |
| **Application** | Feature (vertical slice) | `Features/{Feature}/DTOs,Services,Contracts,Mappings/` |
| **Infrastructure** | Feature configs + shared | `Features/{Feature}/Configurations/` + `Persistence/` |
| **Domain** | **Aggregate-centric** + shared kernel | `Aggregates/{Aggregate}/` + `Common/`, `ValueObjects/`, `Services/` |

---

## 🏗️ Why Different Strategies per Layer?

| Concern | Outer Layers (API, Server, App) | Domain (Inner Core) |
|---------|--------------------------------|---------------------|
| **Primary unit** | Use case / feature | Aggregate / business model |
| **Changes driven by** | New screens, endpoints, workflows | Business rule changes |
| **Coupling** | Feature A rarely touches Feature B | `MenuOptimizationService` uses 4 aggregates |
| **Shared types** | Rare (each feature has own DTOs) | Common (`Money`, `Email`, `Address` used everywhere) |
| **Best fit** | Vertical slice (feature folder) | Aggregate-centric + shared kernel |

---

## ✅ Why Vertical Slices in Outer Layers

**Before** — finding all "Review" code required visiting 8+ folders across `Dtos/`, `Services/`, `Components/Pages/`, `Abstractions/`.

**After** — all code for a feature lives in `Features/Reviews/` in each layer.

| Benefit | Description |
|---------|-------------|
| **Feature cohesion** | All code for a feature lives together |
| **Reduced merge conflicts** | Different features = different folders |
| **Easier onboarding** | "Look at `Features/Restaurants/`" |
| **Delete-friendly** | Remove a feature = delete one folder per layer |
| **Consistent naming** | `{Feature}ClientService`, `I{Feature}ClientService` |

---

## ❌ Why NOT Vertical Slices in Domain

> **Outer layers are organized by USE CASES (what the app does).**  
> **The Domain layer is organized by the BUSINESS MODEL (what the business IS).**

```csharp
// MenuOptimizationService uses 4 aggregates — which "feature folder" does it go in?
public class MenuOptimizationService
{
    // Uses: Menu, Dish, SaleRecord, Review aggregates
    // Answer: NONE. It's a cross-aggregate domain service → Domain/Services/
}
```

Forcing vertical slices in Domain would require duplicating, arbitrarily assigning, or creating a "Shared" folder — all worse than aggregate-centric.

### What Goes WHERE in Domain?

| Concept | Location | Rule |
|---------|----------|------|
| Aggregate root + children + events + errors + specs | `Aggregates/{Name}/` | Co-located — they share invariants |
| Value objects (`Money`, `Email`, `Address`, etc.) | `ValueObjects/` | Used across multiple aggregates |
| Cross-aggregate domain services | `Services/` | Touch multiple aggregates |
| Base classes (`EntityBase`, `DomainEventBase`) | `Common/` | Inherited by all entities |
| Generic contracts (`IRepository<T>`, `IUnityOfWork`) | `Repositories/` | Used by all aggregates |
| Base exceptions (`DomainException`) | `Exceptions/` | Inherited by aggregate-specific exceptions |
| Shared enums | `Enums/` | Shared across aggregates |

---

## 📁 Key Files by Layer

### Domain — Aggregates (`Aggregates/{Name}/`)

| Aggregate | Root Entity | Children / Joins | Errors | Events | Specifications |
|-----------|------------|-----------------|--------|--------|---------------|
| **Restaurant** | `Restaurant.cs` | `BusinessHours.cs` | `RestaurantDomainException` | — | — |
| **Menu** | `Menu.cs` | `MenuDish.cs` | `MenuDomainException` | `DishAdded…`, `DishRemoved…` | — |
| **Dish** | `Dish.cs` | — | `DishDomainException` | — | `DishWithDetails…` |
| **Order** | `Order.cs` | `OrderItem.cs` | `OrderDomainException` | `OrderPlaced…`, `Completed…`, `Cancelled…` | — |
| **Review** | `Review.cs` | — | — | — | `Filtered…`, `WithDetails…` |
| **SaleRecord** | `SaleRecord.cs` | — | — | `SaleRecorded…`, `DailySummary…` | `SaleRecordWithDetails…` |
| **Table** | `Table.cs` | `Reservation.cs` | `Table…`, `Reservation…` | — | `ReservationSpecifications` |
| **CustomerLoyalty** | `CustomerLoyalty.cs` | `LoyaltyTransaction.cs` | `LoyaltyDomainException` | `PointsEarned…`, `TierChanged…` | — |
| **Promotion** | `Promotion.cs` | — | `PromotionDomainException` | — | — |

**Shared kernel**: `Common/`, `ValueObjects/`, `Repositories/`, `Abstractions/`, `Enums/`, `Entities/`, `Exceptions/`, `Specifications/`, `Services/`, `Extensions/`

### Application — Features (`Features/{Feature}/`)

| Feature | DTOs | Services | Other |
|---------|------|----------|-------|
| **Restaurants** | `RestaurantDTO`, `DishDTO`, `MenuDTO`, `CategoryDTO` + Create/Update/Detail variants, `AddressDTO`, `BusinessHoursDTO`, `UnderperformingDishDTO` | `IRestaurantService`, `RestaurantService`, `IMenuService`, `MenuService`, `ICategoryService`, `CategoryService`, `IDishService`, `DishService` | `Mappings/RestaurantMappingExtensions` |
| **Reviews** | `ReviewDTO` | `ReviewApplicationService` | — |
| **Sales** | `SaleRecordDTO`, `CategoryGroupDTO` | — | — |
| **AI** | `AiRecommendationRequestDTO`, `ResponseDTO`, `InsightResponseDTO` | `AiImprovementService` | `Contracts/IAImprovementStrategyService` |
| **Admin** | `AdminUserDTO`, `BusinessRuleDTO` | `AdminAuthorizationService` | — |
| **Customers** | `CustomerDTO` | — | — |
| **Reservations** | — | `ReservationAutoCleanupService`, `ReservationReportingService` | — |
| **Pricing** | — | `CompetitivePricingApplicationService`, `DishPricingApplicationService` | — |
| **Analytics** | — | `MenuAnalyticsApplicationService` | — |
| **Catering** | — | `CateringPricingApplicationService` | — |
| **Orders** | — | `OrderPricingApplicationService` | — |
| **Promotions** | — | `PromotionPricingApplicationService` | — |
| **Testing** | — | `PriceTestingApplicationService` | — |

**Shared**: `Common/` (Result, PaginatedResponse), `Contracts/` (infra ports), `Handlers/` (event handlers), `Dtos/Common/` (`UserBaseDTO` — shared base), `GlobalDtoUsings.cs`

### Server — Features (`Features/{Feature}/`)

| Feature | Components | Services | State / Models |
|---------|-----------|----------|---------------|
| **Restaurants** | RestaurantList, Form, Detail, CategoryList, MenuList, MenuEditor, DishList, DishForm | `I{X}ClientService` + `{X}ClientService` × 4 | 4 state containers |
| **Reviews** | Reviews, SubmitReview, ReviewFilters, ReviewStatistics | `IReviewClientService`, `ReviewClientService` | `ReviewFormModel`, `ReviewFilterState` |
| **Sales** | — | `ISaleRecordClientService`, `SaleRecordClientService` | — |
| **AI** | Insights, Underperforming, AiSuggestionModal | `IAIClientService`, `AIClientService` | `AiImprovementRequest` |
| **Dashboard** | Dashboard | — | — |

**Shared**: `Components/Layout/` (MainLayout, NavMenu), `Components/Shared/` (ErrorAlert, LoadingSpinner, etc.), `Common/` (ClientResult), `Helpers/`, `State/` (ComponentStateBase)

### API — Features (`Features/{Feature}/v1/`)

| Feature | Controllers |
|---------|------------|
| **Restaurants** | `RestaurantsController`, `MenusController`, `CategoriesController`, `DishesController` |
| **Reviews** | `ReviewsController` |
| **Sales** | `SaleRecordsController` |
| **AI** | `AiController` |
| **Reservations** | `ReservationReportsController` |
| **Diagnostics** | `ConfigCheckController` |

**Shared**: `Common/` (ApiResponse, ApiControllerBase), `Filters/`, `Data/` (DbSeeder), `Extensions/`

---

## 📝 Naming Conventions

| Convention | Example | Rationale |
|------------|---------|-----------|
| Interface: `I{Feature}ClientService` | `IReviewClientService` | Clarifies client-side HTTP adapter |
| Implementation: `{Feature}ClientService` | `ReviewClientService` | Matches interface |
| Old pattern (deprecated) | `IReviewService` | Ambiguous — could be domain or client |
| **Plural** feature namespace | `Features.Restaurants` | Avoids C# namespace-type collision with singular `Restaurant` class |

---

## 🔄 Backward Compatibility

**`GlobalDtoUsings.cs`** — Each project has global using aliases for migrated DTOs, so old namespace paths continue to compile during incremental migration.

**`Features/_Imports.razor`** — Feature Razor components have their own `_Imports.razor` for shared usings, independent of `Components/_Imports.razor`.

---

## 📊 Consequences

| Type | Consequence | Impact / Mitigation |
|------|------------|-------------------|
| ✅ Positive | Feature isolation in outer layers | High — each feature is self-contained |
| ✅ Positive | Aggregate co-location in Domain | High — one folder = one aggregate |
| ✅ Positive | Incremental migration via GlobalDtoUsings | Medium — no big-bang refactor |
| ⚠️ Negative | Two organizational models to learn | This ADR documents the rationale |
| ➖ Neutral | `Dtos/Common/UserBaseDTO` remains shared | Shared base class — not feature-specific |
| ➖ Neutral | Cross-cutting handlers stay in `Handlers/` | They touch multiple features |
| ➖ Neutral | Infrastructure persistence stays shared | EF migrations/context are cross-cutting |

---

## 📚 References

- [Clean Architecture — Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Vertical Slice Architecture — Jimmy Bogard](https://www.jimmybogard.com/vertical-slice-architecture/)
- [Domain-Driven Design — Eric Evans](https://www.domainlanguage.com/ddd/)
- [ADR-004: Interface Placement Rules](ADR-004-INTERFACE-PLACEMENT-RULES.md)
- [Copilot Instructions](../../.github/copilot-instructions.md) — Vertical Slice conventions, namespace rules

---

*Last Updated: 2026-03-12*
