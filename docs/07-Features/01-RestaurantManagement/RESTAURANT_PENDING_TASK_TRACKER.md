# 🍽️ Restaurant Management — Pending Task Tracker

> **SmartMenuOptimizer - Restaurant Feature Backlog**  
> **Version**: 2.0  
> **Created**: 2026-03-14  
> **Last Updated**: 2026-03-14  
> **Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`  
> **Extracted From**: [PENDING_TASKS.md](PENDING_TASKS.md)

---

## 📐 Document Structure Reference

> Use this section as a **template** when creating similar pending task tracker documents for other feature modules.

```
# <Feature Name> — Pending Task Tracker

  > Metadata block (Version, Created, Last Updated, Branch, Extracted From)

  ## 📐 Document Structure Reference      ← This template section
  ## 🗂️ Quick Reference                   ← Status & Priority icon legend
  ## 📊 Summary at a Glance               ← Counts by status & priority (MVP vs Post-MVP)

  ## 🎯 MVP Pending Tasks                 ← Active tasks required for MVP release
    ### 🔴 High Priority                  ← Architecture, domain, blocking items
    ### 🟡 Medium Priority                ← API, UI, documentation improvements
    ### 🟢 Low Priority                   ← Performance, tech debt, minor fixes

  ## 🚀 Post-MVP Pending Tasks            ← Deferred tasks for future iterations
    ### 🟡 Medium Priority                ← CQRS, testing, validation frameworks
    ### 🟢 Low Priority                   ← Enhancements, advanced features

  ## ✅ Completed Tasks                   ← Chronological log of finished work
  ## 📝 New Task Template                 ← ID format & category reference
  ## 🔄 Version History                   ← Document changelog
```

**Naming Convention**: `<MODULE>_PENDING_TASK_TRACKER.md`  
**ID Format**: `<PREFIX>-<CATEGORY>-<NUM>` (e.g., `REST-DOM-001`, `ORD-API-002`)  
**Categories**: `ARCH` · `DOM` · `API` · `UI` · `TEST` · `DATA` · `ENH` · `PERF` · `TD` · `DOC` · `CQRS`

---

## 🗂️ Quick Reference

| Status | Icon | | Priority | Icon |
|--------|------|-|----------|------|
| Not Started | ⬜ | | Critical | 🔥 |
| In Progress | 🟡 | | High | 🔴 |
| Blocked | 🔴 | | Medium | 🟡 |
| Done | ✅ | | Low | 🟢 |
| Deferred | ⏸️ | | | |

> **Related Docs**  
> [Implementation Tracker](../07-Features/01-RestaurantManagement/IMPLEMENTATION_TRACKER.md) · [Implementation Guide](../07-Features/01-RestaurantManagement/IMPLEMENTATION_GUIDE.md) · [MVP Prioritization](../01-Overview/MVP_FEATURE_PRIORITIZATION.md) · [Pending Tasks (Global)](PENDING_TASKS.md)
>
> **Event-Driven Architecture**  
> [Domain Events Guide](../../../SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md) · [Events Clean Architecture](../../../SmartMenuOptim.Domain/docs/06-Events/EVENTS_CLEAN.md) · [Event-Driven Architecture Pattern](../../08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md)

---

## 📊 Summary at a Glance

```
Restaurant Management Tasks: 37

 Overall Status              Overall Priority
 ─────────────────────       ──────────────────
 ⬜ Not Started .... 10       🔥 Critical ..... 0
 🟡 In Progress .... 0       🔴 High ........ 1
 ⏸️  Deferred ..... 16       🟡 Medium ...... 19
 ✅ Done .......... 11       🟢 Low ......... 17

 MVP Pending ........ 10     Post-MVP Deferred .. 16
```

---

## 🎯 MVP Pending Tasks

> Tasks required for the MVP release. These are **not deferred** and must be addressed before shipping.

### 🔴 High Priority — Architecture & Domain

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| REST-DOM-001 | DOM-001 | Domain exceptions in remaining services | 🔴 | ⬜ | `MenuService.cs` updated; `RestaurantService`, `CategoryService`, `DishService` pending |

### 🟡 Medium Priority — UI & Documentation

#### UI Components

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| REST-UI-001 | UI-001 | Reusable ConfirmationModal | 🟡 | ⬜ | Currently inline in `RestaurantList`, `MenuList`, `DishList` |
| REST-UI-003 | UI-003 | Form validation message component | 🟡 | ⬜ | Reduce duplication in `RestaurantForm`, `MenuEditor`, `DishForm` |
| REST-UI-004 | UI-004 | Toast notifications | 🟡 | ⬜ | Replace inline alerts in Restaurant CRUD operations |

#### Documentation

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| REST-DOC-001 | DOC-001 | API endpoint docs (Swagger) — Restaurant endpoints | 🟡 | ⬜ | `RestaurantsController`, `MenusController`, `CategoriesController`, `DishesController` |

### 🟢 Low Priority — Performance & Technical Debt

#### Performance

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| REST-PERF-001 | PERF-001 | Pagination for Restaurant/Menu/Dish lists | 🟢 | ⬜ | Large datasets |
| REST-PERF-002 | PERF-002 | Caching (Redis) — Restaurant data | 🟢 | ⬜ | Frequently accessed data |
| REST-PERF-003 | PERF-003 | EF Core query optimization — Restaurant queries | 🟢 | ⬜ | Projections, includes |

#### Technical Debt

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| REST-TD-001 | TD-001 | Duplicate modal code | 🟢 | ⬜ | Extract shared `ConfirmationModal` from `RestaurantList`, `MenuList`, `DishList` |
| REST-TD-003 | TD-003 | Hardcoded `OwnerId` | 🟢 | ⬜ | `OwnerId = 1` in `RestaurantForm` — resolve with auth |
| REST-TD-004 | TD-004 | Cancellation token support | 🟢 | ⬜ | Inconsistent usage in Restaurant services |

---

## 🚀 Post-MVP Pending Tasks

> Tasks intentionally **deferred** (⏸️) beyond the MVP release. These will be addressed in future iterations.

### 🟡 Medium Priority

#### API & Validation

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| REST-API-002 | API-002 | FluentValidation for Restaurant DTOs | 🟡 | ⏸️ | Post-MVP — DataAnnotations for now |

#### CQRS Refactoring

> Migrated from `RESTAURANT_MODULE_ARCHITECTURE_DECISION.md` — see [ADR-005](../../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md) for architecture context.

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| REST-CQRS-001 | CQRS-001 | CQRS Infrastructure — `ICommand<T>`, `IQuery<T>`, handler interfaces, `ValidationBehavior` pipeline | 🟡 | ⏸️ | MediatR pipeline setup |
| REST-CQRS-002 | CQRS-002 | CQRS Commands — Restaurant (`CreateRestaurant`, `UpdateRestaurant`, `DeleteRestaurant`, `SetBusinessHours`) + handlers + validators | 🟡 | ⏸️ | Phase 2a |
| REST-CQRS-003 | CQRS-003 | CQRS Commands — Menu & Category handlers + validators | 🟡 | ⏸️ | Phase 2b — repeat command pattern |
| REST-CQRS-004 | CQRS-004 | CQRS Queries — Restaurant (`GetById`, `GetAll`, `GetDetail`) + handlers | 🟡 | ⏸️ | Phase 3a |
| REST-CQRS-005 | CQRS-005 | CQRS Queries — Menu & Category query handlers | 🟡 | ⏸️ | Phase 3b — repeat query pattern |
| REST-CQRS-006 | CQRS-006 | Refactor Controllers — Replace service injection with `ISender` | 🟡 | ⏸️ | `RestaurantsController`, `MenusController`, `CategoriesController` |
| REST-CQRS-007 | CQRS-007 | Domain Events — `RestaurantCreated`, `RestaurantUpdated`, `MenuCreated` + event handlers | 🟡 | ⏸️ | `DishAddedToMenuEvent` already exists |

#### Testing

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| REST-TEST-001 | TEST-001 | Unit tests — `RestaurantService` | 🟡 | ⏸️ | Deferred (with CQRS refactoring) |
| REST-TEST-002 | TEST-002 | Unit tests — `MenuService` | 🟡 | ⏸️ | Deferred |
| REST-TEST-003 | TEST-003 | Unit tests — `CategoryService` | 🟡 | ⏸️ | Deferred |
| REST-TEST-004 | TEST-004 | Integration tests — Restaurant API controllers | 🟡 | ⏸️ | Deferred |
| REST-TEST-005 | TEST-005 | Manual UI testing checklist — Restaurant pages | 🟡 | ⏸️ | Deferred |

### 🟢 Low Priority

#### Enhancements

| ID | Ref | Task | Priority | Status | Notes |
|----|-----|------|----------|:------:|-------|
| REST-ENH-001 | ENH-001 | Image upload for dishes | 🟢 | ⏸️ | Out of MVP scope |
| REST-ENH-002 | ENH-002 | Menu scheduling automation | 🟢 | ⏸️ | Manual activation for MVP |
| REST-ENH-003 | ENH-003 | Nested categories | 🟢 | ⏸️ | Flat list for MVP |
| REST-ENH-004 | ENH-004 | Multi-location restaurants | 🟢 | ⏸️ | Single location for MVP |
| REST-ENH-005 | ENH-005 | Holiday hours | 🟢 | ⏸️ | Regular hours only |

---

## ✅ Completed Tasks

### 2026-03-12

| ID | Task | Category | Notes |
|----|------|----------|-------|
| REST-ARCH-001 | Restaurant Management Phase 5 (MVP) | Architecture | Dashboard + AI integrated, demo data seeded |
| REST-ARCH-002 | Client Service pattern — Restaurant pages | Architecture | 4 client services created |
| REST-ARCH-003 | Code-Behind — all 8 Restaurant `.razor.cs` files | Architecture | Complete separation |
| REST-ARCH-004 | State Container — 4 state classes + `ComponentStateBase` | Architecture | `RestaurantListState`, `RestaurantDetailState`, `MenuListState`, `MenuEditorState` |
| REST-API-001 | `DishesController` | API | `API\Features\Restaurants\v1\DishesController.cs` |
| REST-TD-002 | Client Service pattern for Restaurant feature | Tech Debt | Replaced direct `HttpClient` usage |
| REST-DATA-001 | Seed data — comprehensive seeding | Data | `DbSeeder.cs` |
| REST-DATA-002 | Dashboard integration | Data | `Dashboard.razor` via `IRestaurantClientService` |

### 2026-03-01

| ID | Task | Category | Notes |
|----|------|----------|-------|
| REST-UI-002 | Error handling standardization | UI/UX | `ApiErrorHelper.cs` |

### 2026-02-28

| ID | Task | Category | Notes |
|----|------|----------|-------|
| — | Phase 4: All 8 Blazor components | UI/UX | `RestaurantList`, `RestaurantForm`, `RestaurantDetail`, `CategoryList`, `MenuList`, `MenuEditor`, `DishList`, `DishForm` |
| — | Phase 3: API Controllers | Architecture | `RestaurantsController`, `MenusController`, `CategoriesController`, `DishesController` |
| — | Phase 3.5: EF Configurations | Architecture | `RestaurantConfiguration`, `BusinessHoursConfiguration`, `MenuConfiguration`, `MenuDishConfiguration`, `CategoryConfiguration`, `DishConfiguration` |

---

## 📡 Event-Driven Architecture Reference

> The Restaurant module uses domain events for Menu and Dish operations.
> Future CQRS tasks (`REST-CQRS-007`) will add Restaurant-level domain events.
> Refer to the following documents for implementation patterns:
>
> | Document | Purpose |
> |----------|---------|
> | [Domain Events Guide](../../../SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md) | Event catalog, schemas, aggregate collection pattern, handler rules |
> | [Events Clean Architecture](../../../SmartMenuOptim.Domain/docs/06-Events/EVENTS_CLEAN.md) | What belongs in Domain vs Application vs Infrastructure |
> | [Event-Driven Architecture Pattern](../../08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md) | Canonical reference — lifecycle, resilience, DI, templates, anti-patterns |

### Restaurant Module Event Flows

The Menu aggregate currently raises two domain events. Future Post-MVP work (`REST-CQRS-007`) will add Restaurant-level events (`RestaurantCreated`, `RestaurantUpdated`, `MenuCreated`).

#### Flow 1: Dish Added to Menu

```
Menu.AddDish(dish, categoryId, categoryName)
    → raises DishAddedToMenuEvent
        → AppDbContext.SaveChangesAsync()
            → DB commit succeeds
            → MediatR dispatches DishAddedToMenuEvent
                └── DishAddedToMenuHandler
                    → Invalidate menu cache (ICacheService)
                    → Log menu composition change
                    → (Future) Update search index
```

#### Flow 2: Dish Removed from Menu

```
Menu.RemoveDish(dishId, reason)
    → raises DishRemovedFromMenuEvent
        → AppDbContext.SaveChangesAsync()
            → DB commit succeeds
            → MediatR dispatches DishRemovedFromMenuEvent
                └── DishRemovedFromMenuHandler
                    → Invalidate menu cache (ICacheService)
                    → Log removal with reason and performance data
                    → (Future) Notify menu planners
```

#### Flow 3 (Post-MVP — `REST-CQRS-007`): Restaurant Created

```
Restaurant.Create(...)
    → raises RestaurantCreatedEvent
        → AppDbContext.SaveChangesAsync()
            → DB commit succeeds
            → MediatR dispatches RestaurantCreatedEvent
                ├── NotifyAdminHandler          → INotificationService
                └── InitializeDefaultMenuHandler → Create default menu structure
```

### Key Implementation Notes

- **Existing events**: `DishAddedToMenuEvent` and `DishRemovedFromMenuEvent` are already implemented in the Menu aggregate (see [Event Catalog](../../../SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md))
- **Event lifecycle**: Events dispatch only AFTER `SaveChangesAsync` succeeds (see [Pattern §4](../../08-Patterns/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md))
- **Resilience**: All handlers inherit `ResilientEventHandlerBase<T>` — 3 retries with exponential backoff, then Dead Letter Queue
- **Auto-discovery**: Menu aggregate implements `IHasDomainEvents` — no `AppDbContext` registration needed
- **CQRS dependency**: Restaurant-level domain events (`REST-CQRS-007`) are deferred until CQRS infrastructure (`REST-CQRS-001`) is built

---

## 📝 New Task Template

```markdown
| REST-[CAT]-[NUM] | [REF] | [Description] | 🔴/🟡/🟢 | ⬜/🟡/✅/⏸️ | [Details] |
```

**Categories**: `ARCH` · `DOM` · `API` · `UI` · `TEST` · `DATA` · `ENH` · `PERF` · `TD` · `DOC` · `CQRS`

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.1 | 2026-03-21 | Added `📡 Event-Driven Architecture Reference` section with Menu event flow examples and links to Domain Events Guide, Events Clean Architecture, and Event-Driven Architecture Pattern documents |
| 2.0 | 2026-03-14 | Restructured into MVP vs Post-MVP sections
| 1.1 | 2026-03-14 | Added CQRS Refactoring tasks (REST-CQRS-001–007) migrated from `RESTAURANT_MODULE_ARCHITECTURE_DECISION.md` |
| 1.0 | 2026-03-14 | Initial extraction from [PENDING_TASKS.md](PENDING_TASKS.md) — Restaurant Management tasks only |

---

*This document tracks tasks specific to the Restaurant Management feature. For the global task backlog, see [PENDING_TASKS.md](PENDING_TASKS.md).*
