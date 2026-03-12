# Hybrid Modular Monolith Migration Plan

> **SmartMenuOptimizer - Incremental Architecture Evolution**  
> **Version**: 1.0  
> **Created**: 2026-03-04  
> **Last Updated**: 2026-03-04  
> **Author**: Architecture Team  
> **Status**: Planning - Ready for Review  
> **Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## 📑 Table of Contents

1. [Executive Summary](#-executive-summary)
2. [Current Architecture](#-current-architecture)
3. [Target Architecture: Hybrid Modular Monolith](#-target-architecture-hybrid-modular-monolith)
4. [Key Benefits for Your Project](#-key-benefits-for-your-project)
5. [What Changes vs. Stays](#-what-changes-vs-stays)
6. [Identified Bounded Contexts (Modules)](#-identified-bounded-contexts-modules)
7. [Target Solution Structure](#-target-solution-structure)
8. [Project Reference Graph](#-project-reference-graph)
9. [Module Communication Rules](#-module-communication-rules)
10. [Incremental Migration Strategy](#-incremental-migration-strategy)
11. [Phase Breakdown](#-phase-breakdown)
12. [Risk Mitigation](#-risk-mitigation)
13. [Definition of Done per Module](#-definition-of-done-per-module)
14. [Related Documentation](#-related-documentation)

---

## 📋 Executive Summary

This document outlines the **incremental migration plan** from the current classic layered Clean Architecture to a **Hybrid Modular Monolith** architecture. The hybrid approach uses **separate Domain projects** (compiler-enforced purity) combined with **single Module projects** (Application + Infrastructure + Presentation per bounded context), striking a balance between strict boundary enforcement and solution complexity.

### Why Hybrid?

| Approach | Projects per Module | Domain Purity | Complexity |
|----------|-------------------|---------------|------------|
| Full separation (4 projects/module) | 4 × N | Compiler-enforced everywhere | High — project proliferation |
| Single project (folder-based) | 1 × N | Convention-only | Low — no compiler guardrails |
| **Hybrid (our choice)** | **2 × N** | **Compiler-enforced for Domain** | **Balanced** |

The hybrid approach guarantees that Domain projects have **zero external dependencies** at the compiler level (the most critical boundary), while keeping Application + Infrastructure + Presentation combined to avoid excessive project count.

---

## 🏗️ Current Architecture

### Solution Layout (7 Projects, Classic Layered)

```
SmartMenuOptimizerApp/
├── SmartMenuOptim.Domain          (.NET 8)  ← Pure domain
├── SmartMenuOptim.Application     (.NET 8)  ← Use cases, DTOs, handlers
├── SmartMenuOptim.Infrastructure  (.NET 8)  ← EF Core, services, persistence
├── SmartMenuOptim.API             (.NET 8)  ← REST controllers
├── SmartMenuOptim.Server          (.NET 9)  ← Blazor SSR
├── SmartMenuOptim.Shared          (.NET 8)  ← Cross-cutting constants
└── SmartMenuOptim.Tests           (.NET 9)  ← Unit + integration tests
```

### Current Pain Points

| Pain Point | Impact | Example in Codebase |
|------------|--------|---------------------|
| **Monolithic layers** | All features mixed within each layer project | `SmartMenuOptim.Domain` contains Order, Menu, Restaurant, Loyalty, Reservation aggregates side by side |
| **No module boundaries** | Any handler can reference any repository/entity across features | An Order handler could directly query Restaurant data |
| **Single shared DbContext** | All entities in one `AppDbContext` | `SmartMenuOptim.Infrastructure\Persistence\Context\AppDbContext.cs` |
| **Cross-feature coupling** | Changes in one feature risk regressions in others | Changing Menu configuration affects Order persistence |
| **Scaling limitations** | Cannot independently deploy or scale features | All features deploy as one unit |
| **Team scalability** | Multiple developers editing the same layer projects | Merge conflicts in shared projects |

---

## 🎯 Target Architecture: Hybrid Modular Monolith

### The Hybrid Rule

> **Each module = 2 projects:**
> - `{Module}.Domain/` — Compiler-enforced purity, zero external dependencies (except SharedKernel)
> - `{Module}.Module/` — Application + Infrastructure + Presentation combined, wired via internal DI

### Why This Works

```
┌──────────────────────────────────────────────────────────────────────┐
│  {Module}.Domain/ (.csproj)                                         │
│  ✅ Compiler PREVENTS: EF Core references, HTTP, logging            │
│  ✅ Only references: SharedKernel (EntityBase, IDomainEvent, etc.)  │
│  Contains: Aggregates, Entities, Value Objects, Domain Services,    │
│            Domain Events, Domain Errors                              │
└──────────────────────────┬───────────────────────────────────────────┘
                           │ referenced by
                           ▼
┌──────────────────────────────────────────────────────────────────────┐
│  {Module}.Module/ (.csproj)                                         │
│  Contains: Application/ + Infrastructure/ + Presentation/           │
│  Separation: Folder-based (enforced by architecture tests)          │
│  References: {Module}.Domain, SharedKernel, NuGet packages          │
│  Exposes: DependencyInjection.cs for host registration              │
└──────────────────────────────────────────────────────────────────────┘
```

---

## ⭐ Key Benefits for Your Project

### 1. Compiler-Enforced Domain Purity

| Benefit | Detail |
|---------|--------|
| **What** | Each module's Domain project physically cannot reference EF Core, MediatR, or any infrastructure package |
| **Why it matters** | Your domain services (`MenuOptimizationService`, `RevenueAnalysisService`, `TableAvailabilityService`) stay pure business logic — guaranteed by the compiler, not by code review |
| **Current risk** | Today, nothing prevents adding a `using Microsoft.EntityFrameworkCore` in `SmartMenuOptim.Domain` except discipline |

### 2. Feature Isolation & Independent Development

| Benefit | Detail |
|---------|--------|
| **What** | Each bounded context is a self-contained module with clear boundaries |
| **Why it matters** | A developer working on Loyalty features cannot accidentally break Restaurant persistence; changes to `OrderConfiguration.cs` cannot affect `MenuConfiguration.cs` |
| **Current risk** | All 10+ EF configurations share one project, one DbContext, one migration history |

### 3. Incremental Build Performance

| Benefit | Detail |
|---------|--------|
| **What** | Only changed modules need to recompile |
| **Why it matters** | Editing a Loyalty domain rule doesn't rebuild Menu or Restaurant modules |
| **Current state** | Changing anything in `SmartMenuOptim.Domain` triggers rebuild of Application → Infrastructure → API → Server |

### 4. Clear Ownership & Reduced Merge Conflicts

| Benefit | Detail |
|---------|--------|
| **What** | Each module folder is a self-contained unit a developer or team can own |
| **Why it matters** | Feature branches for "Reservation cleanup" only touch `Reservation.Domain/` and `Reservation.Module/`, reducing conflicts |
| **Current state** | A reservation feature touches Domain, Application, Infrastructure, and API projects — 4-way merge risk |

### 5. Microservice-Ready Extraction Path

| Benefit | Detail |
|---------|--------|
| **What** | Each module is already structured as an independent deployable unit |
| **Why it matters** | If Ordering needs to scale independently in the future, extract `Ordering.Domain/` + `Ordering.Module/` → standalone service with minimal surgery |
| **Current state** | Extracting a feature requires untangling shared layers — a major refactoring effort |

### 6. Explicit Cross-Module Communication

| Benefit | Detail |
|---------|--------|
| **What** | Modules communicate via integration events and public contracts, not direct references |
| **Why it matters** | The `OrderCompletedEvent → AwardLoyaltyPointsHandler` flow is already an integration event pattern in your codebase; modularization formalizes this |
| **Current state** | Event handlers exist but nothing prevents a handler from directly querying another module's repository |

### 7. Testability Per Module

| Benefit | Detail |
|---------|--------|
| **What** | Each module can have its own test project testing domain logic in isolation |
| **Why it matters** | `Menu.Domain.Tests` runs in milliseconds with zero infrastructure dependencies; `Ordering.Module.Tests` can test handlers with a module-scoped in-memory database |
| **Current state** | `SmartMenuOptim.Tests` is a single project testing everything together |

---

## 🔄 What Changes vs. Stays

### ✅ What STAYS the Same

| Component | Current Location | Status |
|-----------|-----------------|--------|
| **SharedKernel base classes** | `SmartMenuOptim.Domain\Common\EntityBase.cs`, `TenantEntityBase.cs`, `DomainEventBase.cs`, `IDomainEvent.cs`, `DomainResult.cs`, `DomainError.cs` | Moves to `Shared\SmartMenuOptim.SharedKernel\` — same code, new home |
| **Shared specifications** | `SmartMenuOptim.Domain\Specifications\ISpecification.cs`, `BaseSpecification.cs` | Moves to SharedKernel — same interfaces |
| **Shared value objects** | `SmartMenuOptim.Domain\ValueObjects\Email.cs`, `Percentage.cs` | Moves to SharedKernel — used across modules |
| **Repository contract** | `SmartMenuOptim.Domain\Repositories\IRepository.cs`, `IUnityOfWork.cs` | Moves to SharedKernel — shared abstraction |
| **Host API project** | `SmartMenuOptim.API\Program.cs`, controllers, filters, middleware | Stays as composition root — registers modules via `DependencyInjection.cs` |
| **Host Blazor project** | `SmartMenuOptim.Server\Program.cs`, components, client services | Stays as composition root — same role |
| **Shared constants** | `SmartMenuOptim.Shared\Constants\AuthConstants.cs` | Merges into SharedKernel |
| **Domain event dispatcher** | `SmartMenuOptim.Infrastructure\EventDispatching\MediatRDomainEventDispatcher.cs` | Moves to SharedKernel or a shared BuildingBlocks project |
| **Existing domain logic** | All aggregate methods, factory methods, domain services | **Unchanged** — only moved to module-scoped projects |
| **Existing EF configurations** | All `IEntityTypeConfiguration<T>` classes | **Unchanged** — only moved to respective module projects |
| **Existing event handlers** | All handlers in `SmartMenuOptim.Application\Handlers\` | **Unchanged** — only moved to respective modules |
| **API endpoint contracts** | Controller routes, response shapes | **Unchanged** — controllers move to module Presentation folders |
| **Blazor components** | All `.razor` / `.razor.cs` files | **Unchanged** — stay in Server host project |
| **Test logic** | Existing test methods and assertions | **Unchanged** — reorganized into per-module test projects |

### 🔀 What CHANGES

| Aspect | Before | After |
|--------|--------|-------|
| **Solution structure** | 7 projects (layer-based) | ~13 projects (module-based: 5 Domain + 5 Module + SharedKernel + 2 Hosts) |
| **Domain project** | Single `SmartMenuOptim.Domain` with all aggregates | One `{Module}.Domain` per bounded context |
| **Application project** | Single `SmartMenuOptim.Application` with all features | Application layer folders inside each `{Module}.Module` |
| **Infrastructure project** | Single `SmartMenuOptim.Infrastructure` with all persistence | Infrastructure folders inside each `{Module}.Module` |
| **DbContext** | One `AppDbContext` with all entities | Each module has a scoped `{Module}DbContext` (or contributes configurations to a shared one) |
| **Cross-module communication** | Direct project references | Integration events via MediatR/EventBus + public contracts |
| **Namespace structure** | `SmartMenuOptim.Domain.Aggregates.OrderAggregate` | `Ordering.Domain.Aggregates.OrderAggregate` |
| **DI registration** | Centralized in `Program.cs` | Each module exposes `DependencyInjection.cs`; hosts call `services.AddOrderingModule()` |
| **Controller location** | All in `SmartMenuOptim.API\Controllers\v1\` | Each controller moves to its module's `Presentation/` folder |
| **Test organization** | Single `SmartMenuOptim.Tests` | Per-module: `Restaurant.Domain.Tests`, `Menu.Module.Tests`, etc. |
| **EF Migrations** | Single migration history in Infrastructure | Per-module migration history (or a shared migration project) |

### ⚠️ Migration Decisions Required

| Decision | Options | Recommendation |
|----------|---------|----------------|
| **DbContext strategy** | (A) One shared DbContext with module configurations, (B) One DbContext per module | Start with **(A)** — easier migration; move to (B) when extracting microservices |
| **Migration project** | (A) Keep migrations in a shared project, (B) Per-module migrations | Start with **(A)** — single migration history is simpler for a modular monolith |
| **Blazor component location** | (A) Keep all in Server host, (B) Move to module Presentation | Start with **(A)** — Blazor components consume client services, not domain directly |
| **Shared value objects** | (A) SharedKernel owns all, (B) Module-specific value objects in module Domain | **(Both)** — `Email`, `Percentage` in SharedKernel; `DishName`, `Rating` in respective module Domain |

---

## 🗂️ Identified Bounded Contexts (Modules)

Based on the aggregates, entities, domain services, and event handlers already in the codebase:

| Module | Bounded Context | Current Aggregates/Entities | Domain Services |
|--------|----------------|----------------------------|-----------------|
| **Restaurant** | Restaurant Management | `Restaurant`, `BusinessHours`, `Category`, `Review` | `ReviewSentimentAnalysisService` |
| **Menu** | Menu & Dish Management | `Menu`, `MenuDish`, `Dish` | `MenuCompositionValidatorService`, `MenuOptimizationService`, `MenuPricingService`, `DishPopularityRankingService` |
| **Ordering** | Orders & Sales | `Order`, `OrderItem`, `SaleRecord`, `OrderStatus` | `RevenueAnalysisService`, `InventoryForecastingService` |
| **Reservation** | Tables & Reservations | `Table`, `Reservation` | `TableAvailabilityService`, `ReservationManagementService` |
| **Loyalty** | Customer Loyalty & Promotions | `CustomerLoyalty`, `LoyaltyTransaction`, `Promotion` | `PromotionEligibilityService` |

### Shared / Cross-Cutting (Not a Module)

| Component | Contains | Current Location |
|-----------|----------|-----------------|
| **SharedKernel** | `EntityBase`, `TenantEntityBase`, `DomainEventBase`, `IDomainEvent`, `DomainResult`, `DomainError`, `IRepository`, `IUnityOfWork`, `ISpecification`, `BaseSpecification`, `Email`, `Percentage`, `Money` | `SmartMenuOptim.Domain\Common\*`, `Specifications\*`, `Repositories\*`, select `ValueObjects\*` |
| **BuildingBlocks** | `MediatRDomainEventDispatcher`, `IDomainEventDispatcher`, `ExceptionHandlingMiddleware`, `TenantResolverMiddleware`, `RateLimitingMiddleware` | `SmartMenuOptim.Infrastructure\EventDispatching\*`, `Middlewares\*` |
| **Profiles** (deferred) | `Customer`, `StaffMember`, `AdminUser`, `ApplicationUser`, `UserPermission`, `BusinessRule` | `SmartMenuOptim.Domain\Entities\ProfileEntities\*`, `GlobalEntities\*` |

---

## 📁 Target Solution Structure

```
SmartMenuOptimizerApp/
│
├── src/
│   ├── Modules/
│   │   │
│   │   ├── Restaurant/                              ← 🍽️ Module 1
│   │   │   ├── Restaurant.Domain/                   ← .csproj
│   │   │   │   ├── Aggregates/
│   │   │   │   │   └── RestaurantAggregate/
│   │   │   │   │       ├── Restaurant.cs
│   │   │   │   │       └── BusinessHours.cs
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── Category.cs
│   │   │   │   │   └── Review.cs
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   ├── Address.cs
│   │   │   │   │   ├── Rating.cs
│   │   │   │   │   └── PhoneNumber.cs
│   │   │   │   ├── Events/
│   │   │   │   ├── Errors/
│   │   │   │   ├── Specifications/
│   │   │   │   └── Services/
│   │   │   │       └── ReviewSentimentAnalysisService.cs
│   │   │   │
│   │   │   └── Restaurant.Module/                   ← .csproj
│   │   │       ├── Application/
│   │   │       │   ├── Features/
│   │   │       │   │   ├── Restaurants/
│   │   │       │   │   │   ├── Commands/CreateRestaurant/
│   │   │       │   │   │   ├── Queries/GetRestaurantById/
│   │   │       │   │   │   └── DTOs/
│   │   │       │   │   ├── Categories/
│   │   │       │   │   └── Reviews/
│   │   │       │   └── Contracts/
│   │   │       ├── Infrastructure/
│   │   │       │   ├── Persistence/
│   │   │       │   │   ├── Configurations/
│   │   │       │   │   └── Repositories/
│   │   │       │   └── Services/
│   │   │       │       └── SentimentService.cs
│   │   │       ├── Presentation/
│   │   │       │   └── RestaurantsController.cs
│   │   │       └── DependencyInjection.cs
│   │   │
│   │   ├── Menu/                                    ← 📋 Module 2
│   │   │   ├── Menu.Domain/                         ← .csproj
│   │   │   │   ├── Aggregates/
│   │   │   │   │   ├── MenuAggregate/
│   │   │   │   │   │   ├── Menu.cs
│   │   │   │   │   │   └── MenuDish.cs
│   │   │   │   │   └── DishAggregate/
│   │   │   │   │       └── Dish.cs
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   └── DishName.cs
│   │   │   │   ├── Events/
│   │   │   │   │   ├── DishAddedToMenuEvent.cs
│   │   │   │   │   └── DishRemovedFromMenuEvent.cs
│   │   │   │   └── Services/
│   │   │   │       ├── MenuCompositionValidatorService.cs
│   │   │   │       ├── MenuOptimizationService.cs
│   │   │   │       ├── MenuPricingService.cs
│   │   │   │       └── DishPopularityRankingService.cs
│   │   │   │
│   │   │   └── Menu.Module/                         ← .csproj
│   │   │       ├── Application/
│   │   │       │   ├── Features/
│   │   │       │   │   ├── Menus/
│   │   │       │   │   └── Dishes/
│   │   │       │   └── Contracts/
│   │   │       ├── Infrastructure/
│   │   │       │   ├── Persistence/Configurations/
│   │   │       │   └── Services/
│   │   │       │       ├── AiImprovementService.cs
│   │   │       │       └── AdvancedPricingService.cs
│   │   │       ├── Presentation/
│   │   │       │   ├── MenusController.cs
│   │   │       │   └── AiController.cs
│   │   │       └── DependencyInjection.cs
│   │   │
│   │   ├── Ordering/                                ← 🛒 Module 3
│   │   │   ├── Ordering.Domain/                     ← .csproj
│   │   │   │   ├── Aggregates/
│   │   │   │   │   └── OrderAggregate/
│   │   │   │   │       ├── Order.cs
│   │   │   │   │       └── OrderItem.cs
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── SaleRecord.cs
│   │   │   │   │   └── OrderStatus.cs
│   │   │   │   ├── Events/
│   │   │   │   │   ├── OrderPlacedEvent.cs
│   │   │   │   │   ├── OrderCompletedEvent.cs
│   │   │   │   │   ├── OrderCancelledEvent.cs
│   │   │   │   │   ├── SaleRecordedEvent.cs
│   │   │   │   │   └── DailySalesSummarizedEvent.cs
│   │   │   │   └── Services/
│   │   │   │       ├── RevenueAnalysisService.cs
│   │   │   │       └── InventoryForecastingService.cs
│   │   │   │
│   │   │   └── Ordering.Module/                     ← .csproj
│   │   │       ├── Application/
│   │   │       │   └── Features/
│   │   │       │       ├── Orders/
│   │   │       │       └── Sales/
│   │   │       ├── Infrastructure/
│   │   │       │   ├── Persistence/Configurations/
│   │   │       │   └── BackgroundJobs/
│   │   │       │       └── DailySalesSummaryBackgroundJob.cs
│   │   │       ├── Presentation/
│   │   │       │   └── SaleRecordsController.cs
│   │   │       └── DependencyInjection.cs
│   │   │
│   │   ├── Reservation/                             ← 📅 Module 4
│   │   │   ├── Reservation.Domain/                  ← .csproj
│   │   │   │   ├── Aggregates/
│   │   │   │   │   └── TableAggregate/
│   │   │   │   │       ├── Table.cs
│   │   │   │   │       └── Reservation.cs
│   │   │   │   ├── Enums/
│   │   │   │   │   └── ReservationStatus.cs
│   │   │   │   └── Services/
│   │   │   │       ├── TableAvailabilityService.cs
│   │   │   │       └── ReservationManagementService.cs
│   │   │   │
│   │   │   └── Reservation.Module/                  ← .csproj
│   │   │       ├── Application/
│   │   │       │   └── Features/Reservations/
│   │   │       ├── Infrastructure/
│   │   │       │   ├── Persistence/
│   │   │       │   └── BackgroundJobs/
│   │   │       │       └── ReservationAutoCleanupBackgroundService.cs
│   │   │       ├── Presentation/
│   │   │       │   └── ReservationReportsController.cs
│   │   │       └── DependencyInjection.cs
│   │   │
│   │   └── Loyalty/                                 ← ⭐ Module 5
│   │       ├── Loyalty.Domain/                      ← .csproj
│   │       │   ├── Aggregates/
│   │       │   │   ├── CustomerLoyaltyAggregate/
│   │       │   │   │   ├── CustomerLoyalty.cs
│   │       │   │   │   └── LoyaltyTransaction.cs
│   │       │   │   └── PromotionAggregate/
│   │       │   │       └── Promotion.cs
│   │       │   ├── Enums/
│   │       │   │   ├── CustomerLoyaltyTier.cs
│   │       │   │   └── LoyaltyTransactionType.cs
│   │       │   └── Events/
│   │       │       ├── LoyaltyPointsEarnedEvent.cs
│   │       │       └── LoyaltyTierChangedEvent.cs
│   │       │
│   │       └── Loyalty.Module/                      ← .csproj
│   │           ├── Application/
│   │           │   └── Features/
│   │           │       ├── Loyalty/
│   │           │       └── Promotions/
│   │           ├── Infrastructure/
│   │           │   └── Persistence/
│   │           └── DependencyInjection.cs
│   │
│   ├── Shared/
│   │   └── SmartMenuOptim.SharedKernel/             ← .csproj
│   │       ├── Common/
│   │       │   ├── EntityBase.cs
│   │       │   ├── TenantEntityBase.cs
│   │       │   ├── DomainEventBase.cs
│   │       │   ├── IDomainEvent.cs
│   │       │   ├── DomainResult.cs
│   │       │   └── DomainError.cs
│   │       ├── Abstractions/
│   │       │   ├── IRepository.cs
│   │       │   ├── IUnityOfWork.cs
│   │       │   ├── IDomainEventDispatcher.cs
│   │       │   ├── INotificationService.cs
│   │       │   ├── IEmailService.cs
│   │       │   └── IPaymentGateway.cs
│   │       ├── Specifications/
│   │       │   ├── ISpecification.cs
│   │       │   └── BaseSpecification.cs
│   │       ├── ValueObjects/
│   │       │   ├── Email.cs
│   │       │   ├── Money.cs
│   │       │   └── Percentage.cs
│   │       ├── Exceptions/
│   │       │   ├── DomainException.cs
│   │       │   └── EntityNotFoundException.cs
│   │       ├── EventDispatching/
│   │       │   └── MediatRDomainEventDispatcher.cs
│   │       └── Constants/
│   │           └── AuthConstants.cs
│   │
│   └── Host/
│       ├── SmartMenuOptim.API/                      ← .csproj (Composition Root - REST)
│       │   ├── Program.cs
│       │   ├── Common/
│       │   │   ├── ApiControllerBase.cs
│       │   │   └── ApiResponse.cs
│       │   ├── Filters/
│       │   ├── Middlewares/
│       │   └── Data/
│       │       ├── DbSeeder.cs
│       │       └── SeedHelper.cs
│       │
│       └── SmartMenuOptim.Server/                   ← .csproj (Composition Root - Blazor)
│           ├── Program.cs
│           ├── Components/Pages/
│           ├── Services/ClientServices/
│           ├── State/
│           └── Common/
│
├── tests/
│   ├── Restaurant.Domain.Tests/
│   ├── Restaurant.Module.Tests/
│   ├── Menu.Domain.Tests/
│   ├── Menu.Module.Tests/
│   ├── Ordering.Domain.Tests/
│   ├── Ordering.Module.Tests/
│   ├── Reservation.Domain.Tests/
│   ├── Loyalty.Domain.Tests/
│   └── SmartMenuOptim.IntegrationTests/
│
└── SmartMenuOptim.sln
```

---

## 📐 Project Reference Graph

```
                        ┌───────────────────────┐
                        │    SharedKernel        │
                        │  (EntityBase, IRepo,   │
                        │   Result, IDomainEvent,│
                        │   Money, Email)        │
                        └───────────┬────────────┘
                                    │
                   referenced by all Domain projects
                                    │
        ┌───────────────┬───────────┼───────────┬───────────────┐
        ▼               ▼           ▼           ▼               ▼
┌──────────────┐ ┌────────────┐ ┌──────────────┐ ┌────────────┐ ┌──────────────┐
│ Restaurant   │ │   Menu     │ │  Ordering    │ │Reservation │ │   Loyalty    │
│   .Domain    │ │  .Domain   │ │   .Domain    │ │  .Domain   │ │   .Domain    │
│(zero deps    │ │            │ │              │ │            │ │              │
│ except       │ │            │ │              │ │            │ │              │
│ SharedKernel)│ │            │ │              │ │            │ │              │
└──────┬───────┘ └─────┬──────┘ └──────┬───────┘ └─────┬──────┘ └──────┬───────┘
       │               │              │               │               │
       ▼               ▼              ▼               ▼               ▼
┌──────────────┐ ┌────────────┐ ┌──────────────┐ ┌────────────┐ ┌──────────────┐
│ Restaurant   │ │   Menu     │ │  Ordering    │ │Reservation │ │   Loyalty    │
│   .Module    │ │  .Module   │ │   .Module    │ │  .Module   │ │   .Module    │
│(App+Infra    │ │            │ │              │ │            │ │              │
│ +Pres)       │ │            │ │              │ │            │ │              │
└──────┬───────┘ └─────┬──────┘ └──────┬───────┘ └─────┬──────┘ └──────┬───────┘
       │               │              │               │               │
       └───────────────┴──────────────┼───────────────┴───────────────┘
                                      ▼
                        ┌────────────────────────┐
                        │     Host/API           │  ← registers all modules
                        │     Host/Server        │  ← registers all modules
                        │   (Composition Roots)  │
                        └────────────────────────┘
```

### Critical Reference Rules

```
✅ SharedKernel      → references NOTHING
✅ {Module}.Domain   → references ONLY SharedKernel
✅ {Module}.Module   → references {Module}.Domain + SharedKernel + NuGet packages
✅ Host projects     → references ALL {Module}.Module projects

❌ {Module}.Domain   → NEVER references NuGet infrastructure packages
❌ {Module}.Module   → NEVER references another module's .Domain or .Module
❌ Module-to-module  → NEVER direct project references (use integration events)
```

---

## 📡 Module Communication Rules

### Current Cross-Module Event Flows (Already in Codebase)

```
┌─────────────────────┐          Integration Event          ┌─────────────────────┐
│   Ordering Module    │ ──── OrderCompletedEvent ─────────→ │   Loyalty Module    │
│                      │                                     │ AwardLoyaltyPoints  │
│                      │                                     │ Handler             │
└─────────────────────┘                                     └─────────────────────┘

┌─────────────────────┐          Integration Event          ┌─────────────────────┐
│   Ordering Module    │ ──── OrderCompletedEvent ─────────→ │   Ordering Module   │
│                      │                                     │ UpdateOrderAnalytics│
│                      │                                     │ Handler             │
└─────────────────────┘                                     └─────────────────────┘

┌─────────────────────┐          Integration Event          ┌─────────────────────┐
│     Menu Module      │ ──── DishAddedToMenuEvent ────────→ │     Menu Module     │
│                      │                                     │ DishAddedToMenu     │
│                      │                                     │ Handler             │
└─────────────────────┘                                     └─────────────────────┘

┌─────────────────────┐          Integration Event          ┌─────────────────────┐
│   Loyalty Module     │ ──── LoyaltyTierChangedEvent ────→ │ Notification Module │
│                      │                                     │ (future)            │
└─────────────────────┘                                     └─────────────────────┘
```

### Communication Patterns

| Pattern | When to Use | Example |
|---------|-------------|---------|
| **Domain Events** (in-process, same module) | Side effects within the same module | `DishAddedToMenuEvent` → log/cache update in Menu module |
| **Integration Events** (cross-module) | One module needs to notify another | `OrderCompletedEvent` → `AwardLoyaltyPointsHandler` in Loyalty module |
| **Public Contracts** (interfaces) | One module needs to query another synchronously | `IRestaurantQueryService` exposed by Restaurant module, consumed by Menu module |

---

## 🚀 Incremental Migration Strategy

### Core Principles

1. **The system must compile and pass tests after every step**
2. **Migrate one module at a time** — never multiple modules in parallel
3. **SharedKernel first** — extract common abstractions before any module
4. **Start with the least-coupled module** — build confidence before tackling complex ones
5. **Keep the old projects as "shells"** until all code is migrated out

### Migration Order (Recommended)

```
Phase 0 ──→ Phase 1 ──→ Phase 2 ──→ Phase 3 ──→ Phase 4 ──→ Phase 5 ──→ Phase 6
SharedKernel  Reservation  Loyalty    Ordering     Menu      Restaurant   Cleanup
(foundation)  (simplest)  (moderate) (moderate+)  (complex)  (complex)   (remove
                                                                          old
                                                                          projects)
```

**Rationale for order:**

| Order | Module | Reason |
|-------|--------|--------|
| 1st | **Reservation** | Fewest cross-module dependencies; 2 aggregates, 2 domain services, 1 controller, 1 background job |
| 2nd | **Loyalty** | Moderate complexity; depends on Ordering events but handler can stay; self-contained aggregates |
| 3rd | **Ordering** | Central module with events consumed by Loyalty; migrate after Loyalty handler is ready to receive |
| 4th | **Menu** | Most domain services; depends on Restaurant for context; complex but well-defined boundaries |
| 5th | **Restaurant** | Foundation module — most other modules have some reference to Restaurant; migrate last to minimize disruption |

---

## 📋 Phase Breakdown

### Phase 0: Foundation — SharedKernel Extraction

**Goal:** Create the shared foundation all modules will depend on.

**Duration estimate:** 1-2 days

| Step | Action | Validation |
|------|--------|------------|
| 0.1 | Create `src/Shared/SmartMenuOptim.SharedKernel/` project targeting .NET 8 | Project compiles |
| 0.2 | Move `EntityBase.cs`, `TenantEntityBase.cs`, `DomainEventBase.cs`, `IDomainEvent.cs` from `SmartMenuOptim.Domain\Common\` | All existing projects still compile |
| 0.3 | Move `DomainResult.cs`, `DomainError.cs` from `SmartMenuOptim.Domain\Common\` | Tests pass |
| 0.4 | Move `ISpecification.cs`, `BaseSpecification.cs` from `SmartMenuOptim.Domain\Specifications\` | Tests pass |
| 0.5 | Move `IRepository.cs`, `IUnityOfWork.cs` from `SmartMenuOptim.Domain\Repositories\` | Tests pass |
| 0.6 | Move shared `ValueObjects\Email.cs`, `ValueObjects\Money.cs`, `ValueObjects\Percentage.cs` | Tests pass |
| 0.7 | Move `DomainException.cs`, `EntityNotFoundException.cs` from exceptions | Tests pass |
| 0.8 | Move `IDomainEventDispatcher.cs` from Application contracts | Tests pass |
| 0.9 | Move `MediatRDomainEventDispatcher.cs` from Infrastructure | Tests pass |
| 0.10 | Move `AuthConstants.cs` from Shared project | Tests pass |
| 0.11 | Update `SmartMenuOptim.Domain` to reference `SharedKernel` instead of containing these files | Full solution compiles, all tests pass |
| 0.12 | Update `SmartMenuOptim.Application` and `SmartMenuOptim.Infrastructure` references | Full solution compiles, all tests pass |

**Completion criteria:** Existing solution structure unchanged except SharedKernel extracted; all tests green.

---

### Phase 1: Reservation Module (Pilot)

**Goal:** First complete module extraction — prove the pattern works.

**Duration estimate:** 2-3 days

| Step | Action | Validation |
|------|--------|------------|
| 1.1 | Create `src/Modules/Reservation/Reservation.Domain/` project | Project compiles; references only SharedKernel |
| 1.2 | Move `Table.cs`, `Reservation.cs` aggregates from `SmartMenuOptim.Domain\Aggregates\TableAggregate\` | Compilation check |
| 1.3 | Move `ReservationStatus.cs` enum | Compilation check |
| 1.4 | Move `TableAvailabilityService.cs`, `ReservationManagementService.cs` domain services | Compilation check |
| 1.5 | Move `ReservationDomainException.cs`, `TableDomainException.cs` | Compilation check |
| 1.6 | Move `ReservationSpecifications.cs` | Compilation check |
| 1.7 | Verify `Reservation.Domain` has **zero** NuGet infrastructure packages | Inspect `.csproj` — only SharedKernel reference |
| 1.8 | Create `src/Modules/Reservation/Reservation.Module/` project | Project compiles |
| 1.9 | Move `ReservationReportingService.cs`, `ReservationAutoCleanupService.cs` from Application | Compilation check |
| 1.10 | Move `ReservationAutoCleanupBackgroundService.cs` from Infrastructure | Compilation check |
| 1.11 | Move `ReservationReportsController.cs` from API | Compilation check |
| 1.12 | Create `DependencyInjection.cs` with `AddReservationModule()` extension method | Compilation check |
| 1.13 | Register module in `SmartMenuOptim.API\Program.cs` and `SmartMenuOptim.Server\Program.cs` | Application starts, endpoints respond |
| 1.14 | Remove moved files from old projects (leave `using` redirects if needed) | Full solution compiles |
| 1.15 | Run all existing tests | All tests pass |
| 1.16 | Create `tests/Reservation.Domain.Tests/` with domain unit tests | Tests pass |

**Completion criteria:** Reservation module fully extracted; old projects no longer contain reservation code; all tests green; API endpoints functional.

---

### Phase 2: Loyalty Module

**Duration estimate:** 2-3 days

| Step | Action | Validation |
|------|--------|------------|
| 2.1 | Create `Loyalty.Domain/` — move `CustomerLoyalty`, `LoyaltyTransaction`, `Promotion` aggregates | Compiles |
| 2.2 | Move loyalty enums (`CustomerLoyaltyTier`, `LoyaltyTransactionType`) | Compiles |
| 2.3 | Move loyalty events (`LoyaltyPointsEarnedEvent`, `LoyaltyTierChangedEvent`) | Compiles |
| 2.4 | Move `PromotionEligibilityService.cs`, `LoyaltyDomainException.cs`, `PromotionDomainException.cs` | Compiles |
| 2.5 | Create `Loyalty.Module/` — move handlers (`LoyaltyPointsEarnedHandler`, `LoyaltyTierChangedHandler`, `AwardLoyaltyPointsHandler`) | Compiles |
| 2.6 | Move `PromotionPricingApplicationService.cs` | Compiles |
| 2.7 | Create `DependencyInjection.cs`, register in hosts | App starts |
| 2.8 | Validate cross-module event: `OrderCompletedEvent` → `AwardLoyaltyPointsHandler` still works | Event flow tested |
| 2.9 | Remove from old projects, run tests | All green |

**Key decision:** `AwardLoyaltyPointsHandler` subscribes to `OrderCompletedEvent` (from Ordering domain). This handler lives in `Loyalty.Module` but references the event type. The event record should be defined in SharedKernel or published as an integration event DTO.

---

### Phase 3: Ordering Module

**Duration estimate:** 2-3 days

| Step | Action | Validation |
|------|--------|------------|
| 3.1 | Create `Ordering.Domain/` — move `Order`, `OrderItem`, `SaleRecord`, `OrderStatus` | Compiles |
| 3.2 | Move order events (`OrderPlacedEvent`, `OrderCompletedEvent`, `OrderCancelledEvent`, `SaleRecordedEvent`, `DailySalesSummarizedEvent`) | Compiles |
| 3.3 | Move `RevenueAnalysisService.cs`, `InventoryForecastingService.cs`, `OrderDomainException.cs` | Compiles |
| 3.4 | Move sale record specifications | Compiles |
| 3.5 | Create `Ordering.Module/` — move order handlers, sale handlers, application services | Compiles |
| 3.6 | Move `OrderConfiguration.cs` EF configuration | Compiles |
| 3.7 | Move `DailySalesSummaryBackgroundJob.cs` | Compiles |
| 3.8 | Move `SaleRecordsController.cs` | Compiles |
| 3.9 | Create `DependencyInjection.cs`, register in hosts | App starts |
| 3.10 | Validate Ordering → Loyalty event flow | Integration test |
| 3.11 | Remove from old projects, run tests | All green |

---

### Phase 4: Menu Module

**Duration estimate:** 3-4 days (most domain services)

| Step | Action | Validation |
|------|--------|------------|
| 4.1 | Create `Menu.Domain/` — move `Menu`, `MenuDish`, `Dish` aggregates | Compiles |
| 4.2 | Move `DishName.cs` value object, menu events | Compiles |
| 4.3 | Move 4 domain services (`MenuCompositionValidatorService`, `MenuOptimizationService`, `MenuPricingService`, `DishPopularityRankingService`) | Compiles |
| 4.4 | Move `MenuDomainException.cs`, `DishDomainException.cs` | Compiles |
| 4.5 | Move dish specifications | Compiles |
| 4.6 | Create `Menu.Module/` — move menu/dish handlers, DTOs, application services | Compiles |
| 4.7 | Move `MenuConfiguration.cs`, `MenuDishConfiguration.cs`, `DishConfiguration.cs` | Compiles |
| 4.8 | Move `AiImprovementService.cs`, `AdvancedPricingService.cs` | Compiles |
| 4.9 | Move `MenusController.cs`, `AiController.cs`, `CategoriesController.cs` | Compiles |
| 4.10 | Move `MenuOptimizationJob.cs`, `ReportGenerationJob.cs` background jobs | Compiles |
| 4.11 | Create `DependencyInjection.cs`, register in hosts | App starts |
| 4.12 | Run full test suite | All green |

---

### Phase 5: Restaurant Module

**Duration estimate:** 3-4 days (foundation module, most references)

| Step | Action | Validation |
|------|--------|------------|
| 5.1 | Create `Restaurant.Domain/` — move `Restaurant`, `BusinessHours` aggregates | Compiles |
| 5.2 | Move `Category.cs`, `Review.cs` entities | Compiles |
| 5.3 | Move `Address.cs`, `Rating.cs`, `PhoneNumber.cs` value objects | Compiles |
| 5.4 | Move `ReviewSentimentAnalysisService.cs`, `RestaurantDomainException.cs` | Compiles |
| 5.5 | Move review specifications | Compiles |
| 5.6 | Create `Restaurant.Module/` — move restaurant/category/review services and DTOs | Compiles |
| 5.7 | Move `RestaurantConfiguration.cs`, `CategoryConfiguration.cs`, `BusinessHoursConfiguration.cs` | Compiles |
| 5.8 | Move `SentimentService.cs` (Azure AI integration) | Compiles |
| 5.9 | Move `RestaurantsController.cs`, `CategoriesController.cs`, `ReviewsController.cs` | Compiles |
| 5.10 | Create `DependencyInjection.cs`, register in hosts | App starts |
| 5.11 | Run full test suite | All green |

---

### Phase 6: Cleanup & Finalization

**Goal:** Remove old shell projects, finalize structure.

**Duration estimate:** 1-2 days

| Step | Action | Validation |
|------|--------|------------|
| 6.1 | Verify `SmartMenuOptim.Domain` is empty (all code migrated) | No source files remain |
| 6.2 | Verify `SmartMenuOptim.Application` is empty | No source files remain |
| 6.3 | Verify `SmartMenuOptim.Infrastructure` only has shared persistence (migrations, DbContext) or is empty | Inspect project |
| 6.4 | Remove empty old projects from solution | Solution compiles |
| 6.5 | Remove `SmartMenuOptim.Shared` (merged into SharedKernel) | Solution compiles |
| 6.6 | Update solution folder structure in `.sln` file | IDE shows correct grouping |
| 6.7 | Update all `README.md` and documentation references | Docs accurate |
| 6.8 | Run full test suite | All green |
| 6.9 | Run application end-to-end | All features functional |
| 6.10 | Update `PENDING_TASKS.md` to reflect completed migration | Tracked |

---

## 🛡️ Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Breaking cross-module event flows** | Medium | High | Test event handlers after each module migration; keep integration tests |
| **EF Core migration conflicts** | Medium | Medium | Keep a single shared migration project until all modules are extracted; migrate DbContext strategy last |
| **Namespace/using breakage** | High | Low | Use IDE refactoring tools; find-and-replace for namespace changes; compile after each step |
| **Circular dependencies between modules** | Low | High | Strict rule: no module-to-module project references; use integration events and SharedKernel contracts |
| **Team confusion during transition** | Medium | Medium | Document migration status; use feature flags; migrate in dedicated branches |
| **Build time regression** | Low | Low | Monitor build times; the hybrid approach (2 projects/module) keeps project count reasonable |
| **Partial migration state** | Medium | Medium | Each phase has clear "done" criteria; old projects remain functional until their code is fully moved |

---

## ✅ Definition of Done per Module

A module migration is **complete** when:

- [ ] `{Module}.Domain/` project exists with zero infrastructure NuGet packages
- [ ] `{Module}.Domain/` references only `SmartMenuOptim.SharedKernel`
- [ ] `{Module}.Module/` project exists with Application + Infrastructure + Presentation folders
- [ ] `{Module}.Module/` has a `DependencyInjection.cs` with `Add{Module}Module()` extension method
- [ ] All aggregates, entities, value objects moved from old Domain project
- [ ] All handlers, services, DTOs moved from old Application project
- [ ] All EF configurations, repositories moved from old Infrastructure project
- [ ] All controllers moved from old API project
- [ ] Host `Program.cs` calls `Add{Module}Module()`
- [ ] All existing tests pass (no regressions)
- [ ] New domain unit tests created for the module
- [ ] No source files for this module remain in old projects
- [ ] Application starts and all module endpoints respond correctly
- [ ] Cross-module event flows verified (if applicable)

---

## 📊 Migration Progress Tracker

| Phase | Module | Status | Start Date | End Date | Notes |
|-------|--------|--------|------------|----------|-------|
| 0 | SharedKernel | ⬜ Not Started | | | Foundation |
| 1 | Reservation | ⬜ Not Started | | | Pilot module |
| 2 | Loyalty | ⬜ Not Started | | | |
| 3 | Ordering | ⬜ Not Started | | | |
| 4 | Menu | ⬜ Not Started | | | |
| 5 | Restaurant | ⬜ Not Started | | | |
| 6 | Cleanup | ⬜ Not Started | | | Remove old projects |

---

## 🔗 Related Documentation

| Document | Location | Relationship |
|----------|----------|--------------|
| Clean Architecture Full Analysis | [`docs/02-Architecture/CLEAN_ARCHITECTURE_FULL_ANALYSIS.md`](./CLEAN_ARCHITECTURE_FULL_ANALYSIS.md) | Current architecture baseline |
| Multitenant Architecture | [`docs/02-Architecture/MULTITENANT_ARCHITECTURE.md`](./MULTITENANT_ARCHITECTURE.md) | Tenant isolation considerations per module |
| Coding Standards | `AI/Prompts/01-Architecture-Patterns/CODING_STANDARD_PROMPT.md` | Architectural constraints reference |
| Domain Events Guide | `SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md` | Event patterns for cross-module communication |
| Event Dispatching Mechanism | `SmartMenuOptim.Infrastructure/docs/08-EventDispatching/DOMAIN_EVENT_DISPATCHING_MECHANISM.md` | Current dispatcher implementation |
| Pending Tasks | [`docs/09-ProjectManagement/PENDING_TASKS.md`](../09-ProjectManagement/PENDING_TASKS.md) | Track migration as ARCH-007 |
| Patterns Documentation | [`docs/08-Patterns/README.md`](../08-Patterns/README.md) | Implementation patterns reference |
| Feature Implementation Guides | [`docs/07-Features/`](../07-Features/) | Feature-specific implementation details to align with module boundaries |

---

*Version 1.0 - 2026-03-04*
