# 📋 Pending Tasks Tracker( NEED TO BE SEGMENTED PER FEATURE AND PRIORITY)

> **SmartMenuOptimizer - Task Backlog & Follow-up Items**  
> **Version**: 1.7  
> **Last Updated**: 2026-03-12  
> **Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

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
> [Implementation Tracker](../07-Features/01-RestaurantManagement/IMPLEMENTATION_TRACKER.md) · [Patterns](../08-Patterns/README.md) · [MVP Prioritization](../01-Overview/MVP_FEATURE_PRIORITIZATION.md) · [ADR-005 (Vertical Slice)](../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md)

---

## 📊 Summary at a Glance

```
Total: 45 tasks

 Status                    Priority
 ─────────────────────     ──────────────────
 ⬜ Not Started .... 14     🔥 Critical ..... 0
 🟡 In Progress .... 2     🔴 High ........ 5
 ⏸️  Deferred ...... 12     🟡 Medium ...... 15
 ✅ Done .......... 19     🟢 Low ......... 16
```

---

## 🔥 High Priority — Architecture & Refactoring

| ID | Task | Status | Notes |
|----|------|:------:|-------|
| ARCH-001 | Restaurant Management Phase 5 | ✅ | MVP complete — Dashboard + AI integrated, demo data seeded |
| ARCH-002 | Client Service pattern for all pages | ✅ | `RestaurantClientService`, `MenuClientService`, `DishClientService`, `CategoryClientService` |
| ARCH-003 | Code-Behind pattern — Restaurant pages | ✅ | All 8 pages have `.razor.cs` |
| ARCH-004 | State Container pattern — Restaurant | ✅ | 4 state classes + `ComponentStateBase` |
| ARCH-005 | Standardize Response Patterns | ✅ | [RESPONSE_RESULT_PATTERN.md](../08-Patterns/RESPONSE_RESULT_PATTERN.md) |
| ARCH-006 | Vertical Slice Architecture | ✅ | All layers. [ADR-005](../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md) |
| ARCH-007 | Hybrid Modular Monolith Migration | ⬜ | [Migration Plan](../02-Architecture/MODULAR_MONOLITH_MIGRATION_PLAN.md) |
| ARCH-008 | Domain aggregate-centric reorganization | ✅ | Events/Errors/Specs co-located; Review + SaleRecord → aggregates |
| ARCH-009 | Code-Behind pattern — remaining features | 🟡 | ✅ Restaurants (8/8) · ❌ AI (0/3) · ❌ Reviews (0/3) · ❌ Dashboard (0/1) |
| ARCH-010 | State Container pattern — remaining features | 🟡 | ✅ Restaurants (4) · 🟡 Reviews (1 partial) · ❌ AI · ❌ Dashboard · ❌ Sales |

### Domain & Business Logic

| ID | Task | Status | Notes |
|----|------|:------:|-------|
| DOM-001 | Domain exceptions in remaining services | ⬜ | `MenuService.cs` updated; others pending |
| DOM-002 | Result pattern in Application services | ✅ | `Result.cs`, `ResultExtensions.cs` |

### API & Integration

| ID | Task | Status | Notes |
|----|------|:------:|-------|
| API-001 | DishesController | ✅ | `API\Features\Restaurants\v1\DishesController.cs` |
| API-002 | FluentValidation | ⏸️ | Post-MVP — DataAnnotations for now |

---

## 🟡 Medium Priority

### UI Components

| ID | Task | Status | Notes |
|----|------|:------:|-------|
| UI-001 | Reusable ConfirmationModal | ⬜ | Currently inline in each page |
| UI-002 | Error handling standardization | ✅ | `ApiErrorHelper.cs` |
| UI-003 | Form validation message component | ⬜ | Reduce duplication across forms |
| UI-004 | Toast notifications | ⬜ | Replace inline alerts |

### Testing (Post-MVP)

| ID | Task | Status | Notes |
|----|------|:------:|-------|
| TEST-001 | Unit tests — RestaurantService | ⏸️ | Deferred (with CQRS refactoring) |
| TEST-002 | Unit tests — MenuService | ⏸️ | Deferred |
| TEST-003 | Unit tests — CategoryService | ⏸️ | Deferred |
| TEST-004 | Integration tests — API controllers | ⏸️ | Deferred |
| TEST-005 | Manual UI testing checklist | ⏸️ | Deferred |

### Data & Seeding

| ID | Task | Status | Notes |
|----|------|:------:|-------|
| DATA-001 | Seed data for MVP demos | ✅ | `DbSeeder.cs` — 2 restaurants, 20 dishes, 30-day sales, reviews |
| DATA-002 | Dashboard integration | ✅ | `Dashboard.razor` via `IRestaurantClientService` |
| DATA-003 | AI recommendations integration | ✅ | `Insights.razor` → `AIService.GetRecommendationsAsync()` |

---

## 🟢 Low Priority

### Enhancements (Post-MVP)

| ID | Task | Status | Notes |
|----|------|:------:|-------|
| ENH-001 | Image upload for dishes | ⏸️ | Out of MVP scope |
| ENH-002 | Menu scheduling automation | ⏸️ | Manual activation for MVP |
| ENH-003 | Nested categories | ⏸️ | Flat list for MVP |
| ENH-004 | Multi-location restaurants | ⏸️ | Single location for MVP |
| ENH-005 | Holiday hours | ⏸️ | Regular hours only |

### Performance

| ID | Task | Status | Notes |
|----|------|:------:|-------|
| PERF-001 | Pagination for lists | ⬜ | Large datasets |
| PERF-002 | Caching (Redis) | ⬜ | Frequent data |
| PERF-003 | EF Core query optimization | ⬜ | Projections |

---

## 🔧 Technical Debt

| ID | Task | Impact | Status | Notes |
|----|------|--------|:------:|-------|
| TD-001 | Duplicate modal code | Low | ⬜ | Create shared component |
| TD-002 | HTTP client patterns | Medium | ✅ | Client Service pattern implemented |
| TD-003 | Hardcoded OwnerId | Low | ⬜ | `OwnerId = 1` in RestaurantForm |
| TD-004 | Cancellation token support | Low | ⬜ | Inconsistent usage |
| TD-005 | XML documentation | Low | 🟡 | Public APIs |

---

## 📚 Documentation

| ID | Task | Priority | Status |
|----|------|----------|:------:|
| DOC-001 | API endpoint docs (Swagger) | Medium | ⬜ |
| DOC-002 | Architecture diagrams | Low | ⬜ |
| DOC-003 | Developer onboarding guide | Low | ⬜ |
| DOC-004 | Deployment procedures | Medium | ⬜ |
| DOC-005 | Pattern usage examples | Medium | ✅ |

---

## 📋 Task Details

### ARCH-006: Vertical Slice Architecture ✅

> **Decision**: [ADR-005](../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md)

| Layer | Strategy | Status |
|-------|----------|:------:|
| API | `Features/{Feature}/v1/` | ✅ |
| Server (Blazor) | `Features/{Feature}/Components,Services,State/` | ✅ |
| Application | `Features/{Feature}/DTOs,Services,Contracts/` | ✅ |
| Infrastructure | Feature configs + shared persistence | ✅ |
| Domain | Aggregate-centric + shared kernel | ✅ |

**Key changes**: Pages moved to `Features/`; services, state, models under feature folders; Domain events/errors/specs co-located per aggregate; `GlobalDtoUsings.cs` + `Features/_Imports.razor` for backward compatibility.

---

### ARCH-009: Code-Behind Pattern — Remaining Features 🟡

> **Goal**: Separate markup (`.razor`) from logic (`.razor.cs`) for all feature components.

| Feature | Components | Code-Behind | Status |
|---------|:----------:|:-----------:|:------:|
| **Restaurants** | 8 | 8 | ✅ Complete |
| **AI** | 3 | 0 | ⬜ Pending |
| **Reviews** | 3 | 0 | ⬜ Pending |
| **Dashboard** | 1 | 0 | ⬜ Pending |
| **Total** | **15** | **8** | 🟡 **53%** |

**Components needing `.razor.cs`**:
- `Insights.razor`, `Underperforming.razor`, `AiSuggestionModal.razor`
- `Reviews.razor`, `SubmitReview.razor`, `ReviewFilters.razor`
- `Dashboard.razor`

---

### ARCH-010: State Container Pattern — Remaining Features 🟡

> **Goal**: Extract component state into injectable state classes per feature.

| Feature | State Classes | Status |
|---------|:------------:|:------:|
| **Restaurants** | 4 (`RestaurantListState`, `RestaurantDetailState`, `MenuListState`, `MenuEditorState`) | ✅ Complete |
| **Reviews** | 1 (`ReviewFilterState` — partial) | 🟡 Partial |
| **AI** | 0 | ⬜ Pending |
| **Dashboard** | 0 | ⬜ Pending |
| **Sales** | 0 | ⬜ Pending |

---

## ✅ Completed

### 2026-03-12

| Task | Category | Notes |
|------|----------|-------|
| ARCH-006 | Architecture | Vertical Slice complete. [ADR-005](../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md) |
| ARCH-008 | Architecture | Domain aggregate-centric — events, errors, specs per aggregate |
| ARCH-002 | Architecture | Client Service pattern — 4 client services |
| ARCH-003 | Architecture | Code-Behind — all 8 Restaurant `.razor.cs` files |
| ARCH-004 | Architecture | State Container — 4 state classes + `ComponentStateBase` |
| API-001 | API | `DishesController` |
| TD-002 | Tech Debt | Client Service pattern for Restaurant feature |
| DOC-005 | Docs | 7 pattern docs created |
| DATA-001 | Data | `DbSeeder.cs` comprehensive seeding |
| DATA-002 | Data | Dashboard integration |
| DATA-003 | Data | AI recommendations integration |
| ARCH-001 | Architecture | Restaurant Management Phase 5 MVP |

### 2026-03-01

| Task | Category | Notes |
|------|----------|-------|
| ARCH-005 | Architecture | Response Pattern — [Docs](../08-Patterns/RESPONSE_RESULT_PATTERN.md) |
| DOM-002 | Domain | Result pattern with `ResultExtensions.cs` |
| UI-002 | UI/UX | `ApiErrorHelper.cs` |
| — | Bug | MenuList toggle fix |
| DOM-003 | Domain | `MenuDomainException` handling |

### 2026-02-28

| Task | Category | Notes |
|------|----------|-------|
| Phase 4 | UI/UX | All 8 Blazor components |
| Phase 3 | Architecture | API Controllers |
| Phase 3.5 | Architecture | EF Configurations |

---

## 📝 New Task Template

```markdown
| [CAT]-[NUM] | [Description] | ⬜/🟡/✅/⏸️ | [Details] |
```

**Categories**: `ARCH` · `DOM` · `API` · `UI` · `TEST` · `DATA` · `ENH` · `PERF` · `TD` · `DOC`

---

*Last Updated: 2026-03-12*
