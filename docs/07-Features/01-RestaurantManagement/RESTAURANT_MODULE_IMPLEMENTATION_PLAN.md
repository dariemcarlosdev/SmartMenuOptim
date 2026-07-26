# Restaurant Management System Implementation Guide

> **SmartMenuOptimizer - Restaurant Management Feature**  
> **Priority**: 1 (MVP Critical - Foundation)  
> **Version**: 2.5  
> **Last Updated**: 2026-03-14

---

## 📐 Document Structure Reference

> Use this section as a **template** when creating similar implementation plan documents for other feature modules.

```
# <Feature Name> Implementation Guide

  > Metadata block (Priority, Version, Last Updated)

  ## 📐 Document Structure Reference      ← This template section
  ## Overview                             ← Feature purpose & architecture context

  ## Project Structure                    ← Layer-by-layer path reference table
  ## 1. Domain Layer                      ← Aggregates, entities, value objects
  ## 2. Application Layer DTOs            ← Data transfer objects by purpose
  ## 3. Service Layer                     ← Interfaces, implementations, mappings
  ## 4. API Controllers                   ← Endpoints by controller & route
  ## 5. Blazor Components                 ← UI components by route
  ## 6. EF Core Configurations            ← Entity configuration by table
  ## 7. Validation Strategy               ← Validation layers (DTO, API, Domain, VO)
  ## 8. Performance Optimization          ← Indexes & caching strategy

  ## Implementation Checklist             ← Per-phase task checklist with status
  ## Related Documentation                ← Links to trackers, ADRs, standards
  ## Version History                      ← Document changelog
```

**Naming Convention**: `<MODULE>_MODULE_IMPLEMENTATION_PLAN.md`  
**Section Numbering**: Domain (1) → DTOs (2) → Services (3) → API (4) → UI (5) → EF (6) → Validation (7) → Performance (8)  
**Phase Checklist Format**: `### Phase N: <Layer> ✅ COMPLETE | ⏳ IN PROGRESS`

---

## Overview

This guide outlines the implementation steps for the Smart Menu Optimization Restaurant Management System following **Clean Architecture** and **Domain-Driven Design (DDD)** principles in a Blazor-based architecture.

Restaurant Management is the **foundational feature** - all other features depend on it.

> **Note**: For actual code implementations, refer to the source files directly. This guide serves as a structural reference and checklist.

---

## Project Structure

| Layer | Path | Purpose |
|-------|------|---------|
| **Domain** | `SmartMenuOptim.Domain/Aggregates/RestaurantAggregate/` | Restaurant, BusinessHours |
| **Domain** | `SmartMenuOptim.Domain/Aggregates/RestaurantAggregate/Errors/` | RestaurantDomainException |
| **Domain** | `SmartMenuOptim.Domain/Aggregates/MenuAggregate/` | Menu, MenuDish |
| **Domain** | `SmartMenuOptim.Domain/Aggregates/DishAggregate/` | Dish |
| **Domain** | `SmartMenuOptim.Domain/Entities/RestaurantEntities/` | Category |
| **Domain** | `SmartMenuOptim.Domain/ValueObjects/` | Address, Email, PhoneNumber, Money |
| **Application** | `SmartMenuOptim.Application/Features/Restaurants/DTOs/` | DTOs for data transfer |
| **Application** | `SmartMenuOptim.Application/Features/Restaurants/Services/` | Business services |
| **Application** | `SmartMenuOptim.Application/Features/Restaurants/Mappings/` | Mapping extensions |
| **Infrastructure** | `SmartMenuOptim.Infrastructure/Features/Restaurants/Configurations/` | Restaurant & BusinessHours EF configs |
| **Infrastructure** | `SmartMenuOptim.Infrastructure/Persistence/Configurations/` | Menu, MenuDish, Category, Dish EF configs |
| **Infrastructure** | `SmartMenuOptim.Infrastructure/Persistence/Repositories/` | Repository implementations |
| **API** | `SmartMenuOptim.API/Features/Restaurants/v1/` | REST API endpoints (versioned) |
| **UI** | `SmartMenuOptim.Server/Features/Restaurants/Components/` | Blazor components |
| **UI** | `SmartMenuOptim.Server/Features/Restaurants/Services/` | Client HTTP services |
| **UI** | `SmartMenuOptim.Server/Features/Restaurants/State/` | State containers |
| **Tests** | `SmartMenuOptim.Tests/UnitTests/Controllers/` | Controller unit tests |
| **Tests** | `SmartMenuOptim.Tests/UnitTests/Services/` | Service unit tests |

---

## 1. Domain Layer (✅ Complete)

### 1.1 Aggregates & Entities

| Component | Type | File | Description |
|-----------|------|------|-------------|
| `Restaurant` | Aggregate Root | `Restaurant.cs` | Tenant root, manages business info & hours |
| `BusinessHours` | Child Entity | `BusinessHours.cs` | Operating hours per day |
| `Menu` | Aggregate Root | `Menu.cs` | Menu with dish collections |
| `MenuDish` | Join Entity | `MenuDish.cs` | Menu-Dish relationship |
| `Dish` | Aggregate Root | `Dish.cs` | Menu item with pricing |
| `Category` | Entity | `Category.cs` | Dish categorization |

### 1.2 Value Objects

| Value Object | Purpose |
|--------------|---------|
| `Address` | Location information |
| `Email` | Validated email address |
| `PhoneNumber` | Validated phone number |
| `Money` | Currency and amount |

---

## 2. Application Layer DTOs (✅ Complete)

| DTO | Purpose | Location |
|-----|---------|----------|
| `RestaurantDTO` | Display restaurant data | `Application/Dtos/RestaurantDTO.cs` |
| `RestaurantCreateDTO` | Create new restaurant | `Application/Dtos/Restaurant/` |
| `RestaurantUpdateDTO` | Update existing restaurant | `Application/Dtos/Restaurant/` |
| `RestaurantDetailDTO` | Full details with relations | `Application/Dtos/Restaurant/` |
| `AddressDTO` | Address value object transfer | `Application/Dtos/Restaurant/` |
| `BusinessHoursDTO` | Operating hours transfer | `Application/Dtos/Restaurant/` |
| `MenuDTO` | Menu data transfer | `Application/Dtos/Restaurant/` |

---

## 3. Service Layer (✅ Complete)

### 3.1 Service Interfaces

| Interface | Purpose | Location |
|-----------|---------|----------|
| `IRestaurantService` | Restaurant CRUD & business ops | `Application/Services/Restaurant/` |
| `IMenuService` | Menu management | `Application/Services/Restaurant/` |
| `ICategoryService` | Category management | `Application/Services/Restaurant/` |

### 3.2 Service Implementations

| Service | Key Methods |
|---------|-------------|
| `RestaurantService` | GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, ToggleAcceptingOrdersAsync |
| `MenuService` | GetByRestaurantAsync, CreateAsync, AddDishAsync, RemoveDishAsync |
| `CategoryService` | GetAllAsync, CreateAsync, UpdateAsync, DeleteAsync |

### 3.3 Mapping Extensions

Location: `Application/Extensions/RestaurantMappingExtensions.cs`

- `ToDTO()` extension methods for Restaurant, Address, BusinessHours
- Converts domain entities to DTOs for API responses

---

## 4. API Controllers (✅ Complete)

### 4.1 Endpoints

| Controller | Route | Key Endpoints |
|------------|-------|---------------|
| `RestaurantsController` | `api/v1/restaurants` | GET, GET/{id}, POST, PUT/{id}, DELETE/{id}, PATCH/{id}/status |
| `MenusController` | `api/v1/menus` | GET, GET/{id}, POST, PUT/{id}, DELETE/{id} |
| `CategoriesController` | `api/v1/categories` | GET, GET/{id}, POST, PUT/{id}, DELETE/{id} |

---

## 5. Blazor Components (✅ Complete)

| Component | Route | Purpose |
|-----------|-------|---------|
| `RestaurantList.razor` | `/restaurants` | List all restaurants |
| `RestaurantDetail.razor` | `/restaurants/{id}` | View restaurant details |
| `RestaurantForm.razor` | `/restaurants/new`, `/restaurants/{id}/edit` | Create/Edit restaurant |
| `MenuList.razor` | `/restaurants/{id}/menus` | List menus for restaurant |
| `MenuEditor.razor` | `/menus/{id}/edit` | Edit menu dishes |
| `DishList.razor` | `/restaurants/{id}/dishes` | List dishes |
| `DishForm.razor` | `/dishes/new`, `/dishes/{id}/edit` | Create/Edit dish |
| `CategoryManager.razor` | `/restaurants/{id}/categories` | Manage categories |

---

## 6. EF Core Configurations (✅ Complete)

| Configuration | Table | Key Settings |
|---------------|-------|--------------|
| `RestaurantConfiguration` | Restaurants | Indexes on OwnerId, Name |
| `BusinessHoursConfiguration` | BusinessHours | FK to Restaurant |
| `MenuConfiguration` | Menus | FK to Restaurant, MenuType |
| `MenuDishConfiguration` | MenuDishes | Composite key (MenuId, DishId) |
| `CategoryConfiguration` | Categories | FK to Restaurant |
| `DishConfiguration` | Dishes | FK to Category, Restaurant |

---

## 7. Validation Strategy (MVP)

### 7.1 Validation Layers

| Layer | Validation Type | Implementation |
|-------|----------------|----------------|
| **DTO** | DataAnnotations | `[Required]`, `[StringLength]`, `[EmailAddress]`, etc. |
| **API** | ModelState | `if (!ModelState.IsValid) return BadRequest(ModelState);` |
| **Domain** | IValidatableObject | Business rule validation in entities |
| **Value Objects** | Constructor Guards | `Email`, `PhoneNumber`, `Address` self-validate |

### 7.2 Post-MVP Enhancement

> **Decision**: FluentValidation validators skipped for MVP.
> - DataAnnotations sufficient for basic validation
> - FluentValidation adds value with CQRS pipeline behaviors
> - Will be implemented during Post-MVP CQRS refactoring
> - See [ARCHITECTURE_DECISION.md](ARCHITECTURE_DECISION.md) for details

---

## 8. Performance Optimization

### 8.1 Database Indexes

- `IX_Restaurants_OwnerId` - Filter by owner
- `IX_Restaurants_Name` - Search by name
- `IX_Dishes_CategoryId` - Filter dishes by category
- `IX_Menus_RestaurantId` - Filter menus by restaurant

### 8.2 Caching Strategy

- Restaurant data cached for 5 minutes
- Cache invalidation on updates
- Implementation: `CachedRestaurantService` decorator pattern

---

## Implementation Checklist

### Phase 1: Domain Layer ✅ COMPLETE
- [x] Restaurant Aggregate Root
- [x] BusinessHours Child Entity
- [x] Menu Aggregate Root
- [x] MenuDish Join Entity
- [x] Dish Aggregate Root
- [x] Category Entity
- [x] Value Objects (Address, Email, PhoneNumber, Money)

### Phase 2: DTOs ✅ COMPLETE
- [x] AddressDTO
- [x] BusinessHoursDTO
- [x] RestaurantCreateDTO
- [x] RestaurantUpdateDTO
- [x] RestaurantDetailDTO
- [x] MenuDTO
- [x] Enhanced RestaurantDTO

### Phase 3: Service Layer ✅ COMPLETE
- [x] IRestaurantService interface
- [x] RestaurantService implementation
- [x] IMenuService interface
- [x] MenuService implementation
- [x] ICategoryService interface
- [x] CategoryService implementation
- [x] Mapping extensions
- [x] DI registration
- [x] FluentValidation validators (⏭️ Skipped for MVP - DataAnnotations sufficient)

### Phase 4: API Layer ✅ COMPLETE
- [x] RestaurantsController
- [x] MenusController
- [x] CategoriesController
- [x] DishController (⏭️ Deferred - add when needed)
- [x] Swagger documentation

### Phase 5: EF Core Configurations ✅ COMPLETE
- [x] RestaurantConfiguration (enhanced)
- [x] BusinessHoursConfiguration (new)
- [x] MenuConfiguration (new)
- [x] MenuDishConfiguration (new)
- [x] CategoryConfiguration (new)
- [x] DishConfiguration (enhanced)

### Phase 6: Blazor UI ✅ COMPLETE
- [x] RestaurantList.razor
- [x] RestaurantDetail.razor
- [x] RestaurantForm.razor
- [x] MenuList.razor
- [x] MenuEditor.razor
- [x] DishList.razor
- [x] DishForm.razor
- [x] CategoryManager.razor
- [x] Navigation updates

### Phase 7: Integration ⏳ IN PROGRESS
- [x] Dashboard integration
- [x] AI recommendations integration
- [x] Demo data seeding
- [ ] Unit tests
- [ ] Integration tests

---

## Related Documentation

| Document | Location | Description |
|----------|----------|-------------|
| Implementation Tracker | [IMPLEMENTATION_TRACKER.md](IMPLEMENTATION_TRACKER.md) | Progress tracking |
| Architecture Decision | [ARCHITECTURE_DECISION.md](ARCHITECTURE_DECISION.md) | Hybrid approach |
| MVP Prioritization | [docs/01-Overview/MVP_FEATURE_PRIORITIZATION.md](../../01-Overview/MVP_FEATURE_PRIORITIZATION.md) | Overall MVP plan |
| Coding Standards | [AI/Prompts/CODING-STANDARD-PROMPT.md](../../../AI/Prompts/CODING-STANDARD-PROMPT.md) | Development guidelines |

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 2.0 | 2025-02-08 | Updated to match actual DDD implementation |
| 2.1 | 2025-02-28 | Phase 2 & 3 complete, EF configs refactored |
| 2.2 | 2025-02-28 | Added validation strategy section |
| 2.3 | 2025-06-13 | Removed code examples, converted to reference guide |
| 2.4 | 2026-03-09 | Phase 7: Dashboard integration, AI recommendations integration, demo data seeding |
| 2.5 | 2026-03-14 | Added 📐 Document Structure Reference template section |

---

*This guide follows Clean Architecture + DDD patterns as implemented in the SmartMenuOptimizer codebase. For actual code implementations, refer to the source files directly.*