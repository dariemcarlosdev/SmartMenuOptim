# 📊 MVP Feature Prioritization

> **SmartMenuOptimizer - Minimum Viable Product Strategy**  
> **Version**: 2.3  
> **Created**: 2025-02-08  
> **Last Updated**: 2026-03-12

---

## 📑 Table of Contents

1. [Executive Summary](#-executive-summary)
2. [MVP Constraints & Strategy](#-mvp-constraints--strategy)
3. [Feature Analysis](#-feature-analysis)
4. [Implementation Status](#-implementation-status)
5. [Priority Recommendations](#-priority-recommendations)
6. [MVP Data Flow](#-mvp-data-flow)
7. [Implementation Roadmap](#-implementation-roadmap)
8. [Deferred Features](#-deferred-features)
9. [Success Metrics](#-success-metrics)

---

## 🎯 Executive Summary

SmartMenuOptimizer is an **AI-powered SaaS platform** for restaurant menu optimization. This document outlines the MVP feature prioritization strategy based on:

- **Target Users**: Both B2B (Restaurant Owners/Managers) AND B2C (End Customers)
- **Core Differentiator**: AI-powered menu recommendations and insights
- **MVP Approach**: Defer authentication, focus on core value demonstration

### Key Decision: AI-Centric MVP

The MVP strategy centers on demonstrating the **AI value proposition**:
- Sales data → AI analysis → Menu recommendations
- Review sentiment → AI insights → Actionable suggestions

---

## 🎯 MVP Constraints & Strategy

### Target Audience

| Audience | Type | Primary Value |
|----------|------|---------------|
| **Restaurant Owners/Managers** | B2B | Dashboard, AI recommendations, menu optimization |
| **End Customers** | B2C | Ordering, reviews, menu browsing |

### Strategic Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Authentication** | ❌ Deferred | Simplifies MVP, use mock tenant/user data |
| **AI Focus** | ✅ Core | Primary differentiator, already implemented |
| **Multi-tenancy** | ✅ Maintained | Architecture supports it, use demo data |

### MVP Demo Approach (No Auth)

```
┌─────────────────────────────────────────────────────────────────┐
│                     MVP DEMO MODE                               │
├─────────────────────────────────────────────────────────────────┤
│  • Mock tenant/restaurant IDs (hardcoded or query param)        │
│  • Demo user modes (Manager view vs Customer view toggle)       │
│  • Focus on AI value proposition showcase                       │
│  • Pre-seeded demo data for compelling demonstrations           │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📋 Feature Analysis

### All Documented Features (docs/07-Features/)

| # | Feature | File | Complexity | Azure Services |
|---|---------|------|------------|----------------|
| 1 | **Restaurant Management** | `RestaurantManagementImplementation.md` | Medium | - |
| 2 | **Profile Management** | `ProfileManagementImplementation.md` | Medium | - |
| 3 | **Order Management** | `OrderManagementImplementation.md` | High | - |
| 4 | **Review Management** | `ReviewManagementImplementation.md` | Low | - |
| 5 | Inventory Management | `InventoryManagementImplementation.md` | High | - |
| 6 | Loyalty Management | `LoyaltyManagementImplementation.md` | Medium | - |
| 7 | Loyalty - Additional | `LoyaltyManagement-AdditionalComponents.md` | Medium | - |
| 8 | Reservation Management | `ReservationManagementImplementation.md` | Medium | - |
| 9 | Analytics & Reporting | `AnalyticsReportingImplementation.md` | High | Synapse, Power BI, Cognitive |
| 10 | Notification System | `NotificationSystemImplementation.md` | High | Service Bus, SignalR |
| 11 | Financial Management | `FinancialManagementImplementation.md` | High | - |
| 12 | Promotion & Marketing | `PromotionMarketingImplementation.md` | Medium | - |
| 13 | Quality Control | `QualityControlImplementation.md` | Medium | - |

### Feature Dependency Map

```
┌─────────────────────────────────────────────────────────────────┐
│                    FEATURE DEPENDENCIES                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌──────────────────┐                                         │
│   │    RESTAURANT    │ ◄──── Foundation (must be first)        │
│   │    MANAGEMENT    │                                         │
│   └────────┬─────────┘                                         │
│            │                                                    │
│            ▼                                                    │
│   ┌──────────────────┐      ┌──────────────────┐              │
│   │      ORDER       │      │     PROFILE      │              │
│   │    MANAGEMENT    │      │   MANAGEMENT     │              │
│   └────────┬─────────┘      └────────┬─────────┘              │
│            │                         │                         │
│            ▼                         ▼                         │
│   ┌──────────────────┐      ┌──────────────────┐              │
│   │     REVIEW       │      │    LOYALTY       │              │
│   │   MANAGEMENT     │      │   MANAGEMENT     │              │
│   └────────┬─────────┘      └──────────────────┘              │
│            │                                                    │
│            ▼                                                    │
│   ┌──────────────────┐                                         │
│   │   AI ENGINE      │ ◄──── Already Implemented!              │
│   │ (Recommendations)│                                         │
│   └──────────────────┘                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✅ Implementation Status

### Domain Layer (Complete)

| Component | Status | Notes |
|-----------|--------|-------|
| `Restaurant` aggregate | ✅ Complete | Full DDD with value objects, business hours (`Aggregates/RestaurantAggregate/`) |
| `Menu` aggregate | ✅ Complete | Full DDD with MenuDish join entity (`Aggregates/MenuAggregate/`) |
| `Dish` aggregate | ✅ Complete | Full DDD with relationships (`Aggregates/DishAggregate/`) |
| `Order` aggregate | ✅ Complete | Full DDD implementation (`Aggregates/OrderAggregate/`) |
| `Review` aggregate | ✅ Complete | Promoted from entity to aggregate (`Aggregates/ReviewAggregate/`) |
| `SaleRecord` aggregate | ✅ Complete | Promoted from entity to aggregate (`Aggregates/SaleRecordAggregate/`) |
| `Category` entity | ✅ Complete | In Entities/RestaurantEntities |
| `BusinessHours` child entity | ✅ Complete | Part of Restaurant aggregate |
| Value Objects | ✅ Complete | Address, Email, PhoneNumber, Money, Rating, Percentage, etc. |
| Domain Services | ✅ Complete | MenuOptimization, Pricing, Reservation, ReviewSentimentAnalysis, etc. |
| Domain Events | ✅ Complete | Co-located per aggregate: Order, Menu, Sale, Loyalty events |
| Domain Errors | ✅ Complete | Co-located per aggregate: `{Aggregate}/Errors/` |
| Specifications | ✅ Complete | Co-located per aggregate: `{Aggregate}/Specifications/` |
| Aggregate-Centric Structure | ✅ Complete | [ADR-005](../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md) |

### Infrastructure Layer (Complete)

| Component | Status | Notes |
|-----------|--------|-------|
| `AppDbContext` | ✅ Complete | With value converters, `ApplyConfigurationsFromAssembly` |
| `RestaurantConfiguration` | ✅ Enhanced | Full config with value objects, indexes (`Features/Restaurants/`) |
| `BusinessHoursConfiguration` | ✅ Complete | Separate config file (`Features/Restaurants/`) |
| `MenuConfiguration` | ✅ Complete | Separate config file |
| `MenuDishConfiguration` | ✅ Complete | Join entity configuration |
| `CategoryConfiguration` | ✅ Complete | Separate config file |
| `DishConfiguration` | ✅ Complete | Entity configuration |
| `OrderConfiguration` | ✅ Complete | Entity configuration |
| `Repository<T>` | ✅ Complete | Generic repository |
| `UnitOfWork` | ✅ Complete | Transaction management |
| Value Converters | ✅ Complete | All value objects mapped (Address, Money, Email, Phone, etc.) |
| Interceptors | ✅ Complete | `AuditInterceptor`, `TenantInterceptor` |
| Middlewares | ✅ Complete | `ExceptionHandling`, `RateLimiting`, `TenantResolver` |

### Application Layer (Complete)

| Component | Status | Notes |
|-----------|--------|-------|
| `RestaurantDTO` | ✅ Enhanced | Full properties (`Features/Restaurants/DTOs/`) |
| `RestaurantCreateDTO` | ✅ Complete | With validation attributes (`Features/Restaurants/DTOs/`) |
| `RestaurantUpdateDTO` | ✅ Complete | With validation attributes (`Features/Restaurants/DTOs/`) |
| `AddressDTO` | ✅ Complete | Value object DTO (`Features/Restaurants/DTOs/`) |
| `BusinessHoursDTO` | ✅ Complete | Operating hours DTO (`Features/Restaurants/DTOs/`) |
| `RestaurantDetailDTO` | ✅ Complete | Full details with relations (`Features/Restaurants/DTOs/`) |
| `MenuDTO` | ✅ Complete | Menu data transfer |
| `MenuCreateDTO` / `MenuUpdateDTO` | ✅ Complete | Menu CRUD DTOs |
| `DishDTO` / `DishCreateDTO` / `DishUpdateDTO` | ✅ Complete | Dish CRUD DTOs |
| `CategoryDTO` / `CategoryCreateDTO` / `CategoryUpdateDTO` | ✅ Complete | Category CRUD DTOs |
| `IRestaurantService` | ✅ Complete | Service interface (`Features/Restaurants/Services/`) |
| `RestaurantService` | ✅ Complete | Service implementation (`Features/Restaurants/Services/`) |
| `IMenuService` / `MenuService` | ✅ Complete | Menu CRUD service |
| `ICategoryService` / `CategoryService` | ✅ Complete | Category CRUD service |
| `IDishService` / `DishService` | ✅ Complete | Dish CRUD service |
| `RestaurantMappingExtensions` | ✅ Complete | Entity-DTO mappings (`Features/Restaurants/Mappings/`) |
| `Result` / `ResultExtensions` | ✅ Complete | Result pattern for error handling |
| `PaginatedResponse` | ✅ Complete | Pagination support |
| `ApplicationError` | ✅ Complete | Standardized error handling |
| Event Handlers | ✅ Complete | Order, Menu, Sale, Loyalty event handlers |

### API Layer (Complete)

| Component | Status | Notes |
|-----------|--------|-------|
| `AiController` | ✅ Complete | AI recommendations (`Features/Ai/v1/`) |
| `ReviewsController` | ✅ Complete | Review CRUD (`Features/Reviews/v1/`) |
| `SaleRecordsController` | ✅ Complete | Sales data (`Features/Sales/v1/`) |
| `RestaurantsController` | ✅ Complete | Full CRUD (`Features/Restaurants/v1/`) |
| `MenusController` | ✅ Complete | Full CRUD (`Features/Restaurants/v1/`) |
| `CategoriesController` | ✅ Complete | Full CRUD (`Features/Restaurants/v1/`) |
| `DishesController` | ✅ Complete | Full CRUD (`Features/Restaurants/v1/`) |
| `ReservationReportsController` | ✅ Complete | Reservation reports (`Features/Reservations/v1/`) |
| `ConfigCheckController` | ✅ Complete | Diagnostics (`Features/Diagnostics/v1/`) |
| `ApiControllerBase` | ✅ Complete | Base controller with common patterns |
| `ApiResponse` | ✅ Complete | Standardized API response wrapper |
| `ValidateModelActionFilter` | ✅ Complete | Model validation filter |
| `ExceptionActionFilter` | ✅ Complete | Global exception handling filter |

### Blazor Server (Mostly Complete)

| Component | Status | Notes |
|-----------|--------|-------|
| Dashboard | ✅ Complete | Main dashboard |
| Insights | ✅ Complete | AI insights display |
| Reviews | ✅ Complete | Review management with filters, statistics |
| Underperformance | ✅ Complete | Underperforming dishes |
| `RestaurantList.razor` | ✅ Complete | Card grid, delete modal, loading/error states |
| `RestaurantForm.razor` | ✅ Complete | Create/Edit with validation |
| `RestaurantDetail.razor` | ✅ Complete | Full details view (`Features/Restaurants/Components/`) |
| `CategoryList.razor` | ✅ Complete | Category management |
| `MenuList.razor` | ✅ Complete | Card grid, status toggle, delete modal |
| `MenuEditor.razor` | ✅ Complete | Create/Edit with availability hours |
| `DishList.razor` | ✅ Complete | Table view, category filter, menu dish management |
| `DishForm.razor` | ✅ Complete | Create/Edit with dietary info, live preview |
| **Client Services** | ✅ Complete | `RestaurantClientService`, `MenuClientService`, `DishClientService`, `CategoryClientService` |
| **State Containers** | ✅ Complete | `RestaurantListState`, `RestaurantDetailState`, `MenuListState`, `MenuEditorState` |
| **Shared Components** | ✅ Complete | `ErrorAlert`, `LoadingSpinner`, `NotFoundAlert`, `DetailCard`, `StatItem` |
| `ApiErrorHelper` | ✅ Complete | Centralized API error handling |
| `ProblemDetailsResponseDto` | ✅ Complete | RFC 7807 error model |
| `ClientResult` / `ClientResultExtensions` | ✅ Complete | Client-side Result pattern |

### Cross-Cutting Concerns

| Component | Status | Notes |
|-----------|--------|-------|
| Vertical Slice (Feature Folders) | ✅ Complete | All layers migrated — [ADR-005](../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md) |
| Domain Aggregate-Centric Structure | ✅ Complete | Events, errors, specs co-located per aggregate; Review + SaleRecord promoted |
| State Container Pattern | 🟡 In Progress | Restaurant components done; others pending |
| Code-Behind Pattern | 🟡 In Progress | `.razor.cs` separation for Restaurant pages |
| `GlobalDtoUsings.cs` | ✅ Complete | Backward-compatible type aliases (Application, API, Server) |
| `Features/_Imports.razor` | ✅ Complete | Shared imports for all feature Blazor components |

---

## 🚀 Priority Recommendations

### MVP Feature Priority Matrix

| Priority | Feature | MVP Relevance | Effort | Status | Why? |
|----------|---------|---------------|--------|--------|------|
| **1** | Restaurant Management | 🔴 Critical | Medium | ✅ MVP Complete | Foundation — Phases 1–5 done; tests deferred to post-MVP |
| **2** | Order Management | 🔴 Critical | High | ⏳ Pending | Generates **sales data** → feeds AI |
| **3** | Review Management | 🟡 Partial | Low | ✅ Partial (expand) | Generates **sentiment data** → feeds AI (partially done!) |
| **4** | Profile Management | 🟢 Defer | Medium | ⏸️ Deferred | No auth needed for MVP |
| 5 | Inventory Management | 🟢 Defer | High | ⏸️ Deferred | Nice-to-have, not Day 1 critical |
| 6 | Loyalty Management | 🟢 Defer | Medium | ⏸️ Deferred | Customer retention - post-MVP |
| 7 | Reservation Management | 🟢 Defer | Medium | ⏸️ Deferred | Can add after core ordering |
| 8 | Analytics & Reporting | 🟢 Defer | High | ⏸️ Deferred | Already have basic AI insights |
| 9 | Notification System | 🟢 Defer | High | ⏸️ Deferred | Infrastructure heavy |
| 10 | Financial Management | 🟢 Defer | High | ⏸️ Deferred | Post-MVP optimization |
| 11 | Promotion & Marketing | 🟢 Defer | Medium | ⏸️ Deferred | Growth feature |
| 12 | Quality Control | 🟢 Defer | Medium | ⏸️ Deferred | Operations feature |

### Core MVP Flow

```
Restaurant Management → Order Management → Review Management (expand)
        ↓                      ↓                    ↓
   (Menus exist)         (Transactions)       (Sentiment data)
                               ↓
                    AI Recommendations improve
```

---

## 🔄 MVP Data Flow

### AI-Centric Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        MVP DATA FLOW                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Restaurant Management          Order Management               │
│   ┌──────────────────┐          ┌──────────────────┐           │
│   │ • Create Menu    │          │ • Place Orders   │           │
│   │ • Add Dishes     │────────▶ │ • Track Sales    │           │
│   │ • Set Prices     │          │ • Sales History  │           │
│   └──────────────────┘          └────────┬─────────┘           │
│                                          │                      │
│                                          ▼                      │
│                               ┌──────────────────┐              │
│                               │   SALES DATA     │              │
│                               └────────┬─────────┘              │
│                                        │                        │
│   Review Management                    │                        │
│   ┌──────────────────┐                 │                        │
│   │ • Submit Reviews │                 │                        │
│   │ • Rate Dishes    │─────────────────┤                        │
│   │ • Feedback       │                 │                        │
│   └──────────────────┘                 │                        │
│            │                           │                        │
│            ▼                           ▼                        │
│   ┌──────────────────┐      ┌──────────────────┐               │
│   │  SENTIMENT DATA  │──────│   🤖 AI ENGINE   │               │
│   └──────────────────┘      │  (AiController)  │               │
│                             └────────┬─────────┘               │
│                                      │                         │
│                                      ▼                         │
│                          ┌─────────────────────┐               │
│                          │  AI RECOMMENDATIONS │               │
│                          │  • Best Sellers     │               │
│                          │  • Underperformers  │               │
│                          │  • Menu Suggestions │               │
│                          │  • Pricing Insights │               │
│                          └─────────────────────┘               │
│                                                                │
└─────────────────────────────────────────────────────────────────┘
```

### Value Proposition Loop

```
┌───────────────────────────────────────────────────────────────────────┐
│                     AI VALUE DEMONSTRATION LOOP                        │
├───────────────────────────────────────────────────────────────────────┤
│                                                                       │
│    1. COLLECT              2. ANALYZE              3. RECOMMEND       │
│   ┌─────────┐            ┌─────────┐            ┌─────────┐          │
│   │ Orders  │──────────▶│   AI    │──────────▶│ Best    │          │
│   │ Reviews │            │ Engine  │            │ Sellers │          │
│   │ Sales   │            │         │            │ Remove  │          │
│   └─────────┘            └─────────┘            │ Items   │          │
│                                                 │ Pricing │          │
│                                                 └────┬────┘          │
│                                                      │               │
│                              ◀───────────────────────┘               │
│                    4. IMPLEMENT & MEASURE                            │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘
```

---

## 📅 Implementation Roadmap

### Phase 1: DTOs ✅ Complete (2026-02-08)

| Task | Status | Files Created |
|------|--------|---------------|
| Create AddressDTO | ✅ | `Application\Features\Restaurants\DTOs\AddressDTO.cs` |
| Create BusinessHoursDTO | ✅ | `Application\Features\Restaurants\DTOs\BusinessHoursDTO.cs` |
| Create RestaurantCreateDTO | ✅ | `Application\Features\Restaurants\DTOs\RestaurantCreateDTO.cs` |
| Create RestaurantUpdateDTO | ✅ | `Application\Features\Restaurants\DTOs\RestaurantUpdateDTO.cs` |
| Create RestaurantDetailDTO | ✅ | `Application\Features\Restaurants\DTOs\RestaurantDetailDTO.cs` |
| Create MenuDTO | ✅ | `Application\Dtos\Restaurant\MenuDTO.cs` |
| Enhance RestaurantDTO | ✅ | `Application\Features\Restaurants\DTOs\RestaurantDTO.cs` |

### Phase 2: Service Layer ✅ Complete (2026-02-28)

| Task | Status | Files Created |
|------|--------|---------------|
| Create IRestaurantService | ✅ | `Application\Features\Restaurants\Services\IRestaurantService.cs` |
| Create RestaurantService | ✅ | `Application\Features\Restaurants\Services\RestaurantService.cs` |
| Create IMenuService / MenuService | ✅ | `Application\Services\Restaurant\` |
| Create ICategoryService / CategoryService | ✅ | `Application\Services\Restaurant\` |
| Create IDishService / DishService | ✅ | `Application\Services\Restaurant\` |
| Create mapping extensions | ✅ | `Application\Features\Restaurants\Mappings\RestaurantMappingExtensions.cs` |
| Register services in DI | ✅ | `Application\Extensions\ApplicationServiceCollectionExtensions.cs` |
| FluentValidation | ⏭️ Skipped | Deferred to post-MVP (DataAnnotations for now) |

### Phase 3: API Layer ✅ Complete (2026-02-28)

| Task | Status | Files Created |
|------|--------|---------------|
| Create RestaurantsController | ✅ | `API\Features\Restaurants\v1\RestaurantsController.cs` |
| Create MenusController | ✅ | `API\Features\Restaurants\v1\MenusController.cs` |
| Create CategoriesController | ✅ | `API\Features\Restaurants\v1\CategoriesController.cs` |
| Create DishesController | ✅ | `API\Features\Restaurants\v1\DishesController.cs` |
| Add Swagger/XML documentation | ✅ | XML comments in controllers |
| RFC 7807 ProblemDetails | ✅ | Error responses follow standard |

### Phase 3.5: EF Core Configurations ✅ Complete (2026-02-28)

| Task | Status | Files Created |
|------|--------|---------------|
| RestaurantConfiguration (enhanced) | ✅ | `Infrastructure\Features\Restaurants\Configurations\` |
| BusinessHoursConfiguration | ✅ | `Infrastructure\Features\Restaurants\Configurations\` |
| MenuConfiguration | ✅ | `Infrastructure\Persistence\Configurations\` |
| MenuDishConfiguration | ✅ | `Infrastructure\Persistence\Configurations\` |
| CategoryConfiguration | ✅ | `Infrastructure\Persistence\Configurations\` |
| DishConfiguration (enhanced) | ✅ | `Infrastructure\Persistence\Configurations\` |
| `ApplyConfigurationsFromAssembly` | ✅ | Added to `AppDbContext` |

### Phase 4: Blazor UI ✅ Complete (2026-02-28)

| Task | Status | Files Created |
|------|--------|---------------|
| Create RestaurantList.razor | ✅ | `Server\Components\Pages\Restaurant\` |
| Create RestaurantForm.razor | ✅ | `Server\Components\Pages\Restaurant\` |
| Create RestaurantDetail.razor | ✅ | `Server\Features\Restaurants\Components\` |
| Create CategoryList.razor | ✅ | `Server\Components\Pages\Restaurant\` |
| Create MenuList.razor | ✅ | `Server\Components\Pages\Restaurant\` |
| Create MenuEditor.razor | ✅ | `Server\Components\Pages\Restaurant\` |
| Create DishList.razor | ✅ | `Server\Components\Pages\Restaurant\` |
| Create DishForm.razor | ✅ | `Server\Components\Pages\Restaurant\` |
| Update NavMenu.razor | ✅ | Added Restaurants navigation link |
| Client Services | ✅ | `RestaurantClientService`, `MenuClientService`, `DishClientService`, `CategoryClientService` |
| State Containers | ✅ | `RestaurantListState`, `RestaurantDetailState`, `MenuListState`, `MenuEditorState` |

### Phase 4.5: Architecture Patterns ✅ Complete (2026-03-01)

| Task | Status | Notes |
|------|--------|-------|
| State Container Pattern | ✅ | `ComponentStateBase`, per-feature state classes |
| Client Service Pattern | ✅ | `IRestaurantClientService` → `RestaurantClientService` |
| Code-Behind Pattern | ✅ | `.razor.cs` separation for Restaurant pages |
| ClientResult Pattern | ✅ | `ClientResult<T>`, `ClientResultExtensions` |
| API Error Handling | ✅ | `ApiErrorHelper`, `ProblemDetailsResponseDto` |
| Response Pattern Standardization | ✅ | `ApiResponse`, `ApiControllerBase` |

### Phase 5: Integration & Testing ✅ Complete (MVP)

| Task | Status | Notes |
|------|--------|-------|
| Integration with Dashboard | ✅ Complete | `Dashboard.razor` injects `IRestaurantClientService`, shows restaurant overview with business hours, links to details |
| Integration with AI recommendations | ✅ Complete | Dashboard links to Insights; `Insights.razor` feeds sales + reviews into `AIService.GetRecommendationsAsync()` |
| Seed demo data | ✅ Complete | `API\Data\DbSeeder.cs` — comprehensive seeding: 2 restaurants, 20 dishes, menus, categories, orders, sales records, reviews with weighted sentiment, loyalty programs |
| Unit tests | ⏸️ Deferred | Post-MVP — will add with CQRS refactoring |
| Integration tests | ⏸️ Deferred | Post-MVP — will add with CQRS refactoring |
| UI testing | ⏸️ Deferred | Post-MVP — manual test scenarios |

### Phase 6: Order Management ⏳ Pending

| Task | Status | Target Location |
|------|--------|-----------------|
| Create Order DTOs | ⏳ Pending | `Application\Features\Orders\DTOs\` |
| Create IOrderService | ⏳ Pending | `Application\Features\Orders\Services\` |
| Create OrderService | ⏳ Pending | `Application\Features\Orders\Services\` |
| Create OrderController | ⏳ Pending | `API\Features\Orders\v1\` |
| Create Order Blazor pages | ⏳ Pending | `Server\Features\Orders\` |

### Updated Timeline

```
┌─────────────────────────────────────────────────────────────────┐
│                      MVP TIMELINE (Updated 2026-03-12)           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ✅ COMPLETED                                                   │
│  ├── Phase 1: DTOs (2026-02-08)                                 │
│  ├── Phase 2: Services (2026-02-28)                             │
│  ├── Phase 3: API Controllers (2026-02-28)                      │
│  ├── Phase 3.5: EF Core Configurations (2026-02-28)             │
│  ├── Phase 4: Blazor UI (2026-02-28)                            │
│  ├── Phase 4.5: Architecture Patterns (2026-03-01)              │
│  └── Phase 5: Integration & Seeding (2026-03-12) ← MVP Done!   │
│                                                                 │
│  ⏸️ DEFERRED (Post-MVP)                                         │
│  ├── Unit tests (with CQRS refactoring)                         │
│  ├── Integration tests (with CQRS refactoring)                  │
│  └── UI testing (manual test scenarios)                         │
│                                                                 │
│  ⏳ NEXT PRIORITY                                                │
│  ├── Phase 6: Order Management                                  │
│  │   ├── Order DTOs, Services, API                              │
│  │   └── Blazor UI + AI integration                             │
│  │                                                               │
│  └── Phase 7: Review Enhancement & Polish                       │
│      ├── Expand existing Review UI                              │
│      ├── Integration with AI sentiment                          │
│      └── UI polish & documentation                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔴 Deferred Features

### Post-MVP Features (Phase 2+)

| Feature | Reason for Deferral | Prerequisite |
|---------|---------------------|--------------|
| **Profile Management** | No auth needed for MVP | Auth implementation |
| **Inventory Management** | Nice-to-have | Restaurant + Order Management |
| **Loyalty Management** | Customer retention | Profile Management |
| **Reservation Management** | Operational feature | Restaurant Management |
| **Analytics & Reporting** | Azure infrastructure heavy | Core features stable |
| **Notification System** | Azure Service Bus/SignalR | Order Management |
| **Financial Management** | Post-revenue feature | Order Management |
| **Promotion & Marketing** | Growth feature | Loyalty Management |
| **Quality Control** | Operations feature | Restaurant Management |

### Infrastructure-Heavy Features (Defer)

These features require significant Azure infrastructure:

| Feature | Azure Services Required | Cost Impact |
|---------|------------------------|-------------|
| Analytics & Reporting | Synapse, Power BI, Cognitive Services | High |
| Notification System | Service Bus, SignalR | Medium |
| Advanced AI | OpenAI, Cognitive Services | Variable |

---

## 📈 Success Metrics

### MVP Success Criteria

| Metric | Target | Current Status | Measurement |
|--------|--------|----------------|-------------|
| **Restaurant CRUD** | 100% functional | ✅ Complete | All operations work |
| **Menu Management** | 100% functional | ✅ Complete | Create/Edit menus and dishes |
| **Dish Management** | 100% functional | ✅ Complete | Create/Edit dishes with categories |
| **Category Management** | 100% functional | ✅ Complete | Full CRUD with restaurants |
| **Order Flow** | Basic flow working | ⏳ Pending | Place and track orders |
| **AI Recommendations** | Visible improvements | ✅ Functional | Recommendations based on data |
| **Demo Quality** | Compelling presentation | ✅ Complete | Dashboard + AI integrated, demo data seeded (`DbSeeder.cs`) |

### Key Performance Indicators (KPIs)

| KPI | Description | Target |
|-----|-------------|--------|
| **Data-to-Insight Time** | Time from order to AI recommendation | < 1 minute |
| **UI Response Time** | Blazor page load time | < 2 seconds |
| **API Response Time** | Average API call duration | < 500ms |
| **Demo Completion Rate** | Full demo flow without errors | 100% |

---

## 📚 Related Documentation

| Document | Location | Description |
|----------|----------|-------------|
| Features Index | `docs/07-Features/README.md` | All features overview |
| Restaurant Management Guide | `docs/07-Features/01-RestaurantManagement/IMPLEMENTATION_GUIDE.md` | Full implementation guide |
| Restaurant Management Tracker | `docs/07-Features/01-RestaurantManagement/IMPLEMENTATION_TRACKER.md` | Progress tracking (MVP complete, tests deferred) |
| Order Management Guide | `docs/07-Features/02-OrderManagement/IMPLEMENTATION_GUIDE.md` | Order system details |
| Review Management Guide | `docs/07-Features/04-ReviewManagement/IMPLEMENTATION_GUIDE.md` | Review system details |
| Architecture Overview | `docs/02-Architecture/` | System architecture |
| Pending Tasks | `docs/09-ProjectManagement/PENDING_TASKS.md` | Task backlog & follow-ups |
| Blazor State Container Pattern | `docs/08-Patterns/BLAZOR_STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md` | State management pattern |
| State Container Pattern | `docs/08-Patterns/STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md` | General state container guide |
| Patterns Index | `docs/08-Patterns/README.md` | All patterns overview |
| Coding Standards | `AI/Prompts/CODING-STANDARD-PROMPT.md` | Development guidelines |

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.3 | 2026-03-12 | Vertical Slice Architecture complete across all layers; Domain aggregate-centric reorganization; [ADR-005](../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md) created |
| 2.2 | 2026-03-12 | Phase 5 MVP complete: seed data confirmed done (`DbSeeder.cs`); unit/integration/UI tests deferred to post-MVP; Restaurant Management → MVP Complete |
| 2.1 | 2026-03-12 | Marked Dashboard integration and AI recommendations integration as complete (already wired in Dashboard.razor + Insights.razor); updated progress 91% → 93% |
| 2.0 | 2026-03-12 | Major update: Reflect completed Phases 1–4.5, updated file paths for vertical slice structure, added cross-cutting concerns status, updated success metrics with current status |
| 1.1 | 2025-02-08 | Updated doc paths after feature folder reorganization |
| 1.0 | 2025-02-08 | Initial MVP prioritization document |

---

*This document is a living document and will be updated as the MVP evolves.*
