# 📋 Pending Tasks Tracker

> **SmartMenuOptimizer - Task Backlog & Follow-up Items**  
> **Version**: 1.2  
> **Last Updated**: 2026-03-01  
> **Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## Quick Reference

| Status | Icon | Priority | Icon |
|--------|------|----------|------|
| Not Started | ⬜ | Critical | 🔥 |
| In Progress | 🟡 | High | 🔴 |
| Blocked | 🔴 | Medium | 🟡 |
| Done | ✅ | Low | 🟢 |
| Deferred | ⏸️ | | |

**Related Docs**: [Implementation Tracker](../07-Features/01-RestaurantManagement/IMPLEMENTATION_TRACKER.md) | [Patterns](../08-Patterns/README.md) | [MVP Prioritization](../01-Overview/MVP_FEATURE_PRIORITIZATION.md)

---

## 🔥 High Priority

### Architecture & Refactoring

| ID | Task | Status | Notes |
|----|------|--------|-------|
| ARCH-001 | Complete Restaurant Management Phase 5 | ⬜ | Integration & Testing |
| ARCH-002 | Client Service pattern for remaining pages | 🟡 | Reference: `RestaurantClientService.cs` |
| ARCH-003 | Code-Behind pattern for all Restaurant pages | ⬜ | Reference: `RestaurantDetail.razor.cs` |
| ARCH-004 | State Container pattern across components | 🟡 | Reference: `RestaurantDetailState.cs` |
| ARCH-005 | Standardize Response Patterns | ✅ | [RESPONSE_PATTERN_IMPLEMENTATION.md](../08-Patterns/RESPONSE_PATTERN_IMPLEMENTATION.md) |
| ARCH-006 | Vertical Slice Architecture structure | ⏸️ | Post-MVP |
| ARCH-007 | Hybrid Modular Monolith Migration | ⬜ | [MODULAR_MONOLITH_MIGRATION_PLAN.md](../02-Architecture/MODULAR_MONOLITH_MIGRATION_PLAN.md) |

### Domain & Business Logic

| ID | Task | Status | Notes |
|----|------|--------|-------|
| DOM-001 | Domain exceptions in remaining services | ⬜ | `MenuService.cs` updated |
| DOM-002 | Result pattern in Application services | ✅ | `Result.cs`, `ResultExtensions.cs` |

### API & Integration

| ID | Task | Status | Notes |
|----|------|--------|-------|
| API-001 | Add DishesController | ⬜ | Deferred from Phase 3 |
| API-002 | FluentValidation | ⏸️ | Post-MVP (DataAnnotations for now) |

---

## 🟡 Medium Priority

### UI Components

| ID | Task | Status | Notes |
|----|------|--------|-------|
| UI-001 | Reusable ConfirmationModal | ⬜ | Currently inline |
| UI-002 | Error handling standardization | ✅ | `ApiErrorHelper.cs` |
| UI-003 | Form validation message component | ⬜ | Reduce duplication |
| UI-004 | Toast notifications | ⬜ | Replace alerts |

### Testing (Phase 5)

| ID | Task | Status | Notes |
|----|------|--------|-------|
| TEST-001 | Unit tests - RestaurantService | ⬜ | |
| TEST-002 | Unit tests - MenuService | ⬜ | |
| TEST-003 | Unit tests - CategoryService | ⬜ | |
| TEST-004 | Integration tests - API controllers | ⬜ | |
| TEST-005 | Manual UI testing checklist | ⬜ | |

### Data & Seeding

| ID | Task | Status | Notes |
|----|------|--------|-------|
| DATA-001 | Seed data for MVP demos | ⬜ | Phase 5 |
| DATA-002 | Dashboard integration | ⬜ | Link restaurant data |
| DATA-003 | AI recommendations integration | ⬜ | Restaurant context |

---

## 🟢 Low Priority

### Enhancements (Post-MVP)

| ID | Task | Status | Notes |
|----|------|--------|-------|
| ENH-001 | Image upload for dishes | ⏸️ | Out of MVP scope |
| ENH-002 | Menu scheduling automation | ⏸️ | Manual activation for MVP |
| ENH-003 | Nested categories | ⏸️ | Flat list for MVP |
| ENH-004 | Multi-location restaurants | ⏸️ | Single location for MVP |
| ENH-005 | Holiday hours | ⏸️ | Regular hours only |

### Performance

| ID | Task | Status | Notes |
|----|------|--------|-------|
| PERF-001 | Pagination for lists | ⬜ | Large datasets |
| PERF-002 | Caching (Redis) | ⬜ | Frequent data |
| PERF-003 | EF Core query optimization | ⬜ | Projections |

---

## 🔧 Technical Debt

| ID | Task | Impact | Status | Notes |
|----|------|--------|--------|-------|
| TD-001 | Duplicate modal code | Low | ⬜ | Create shared component |
| TD-002 | HTTP client patterns | Medium | 🟡 | Client Service pattern |
| TD-003 | Hardcoded OwnerId | Low | ⬜ | `OwnerId = 1` in RestaurantForm |
| TD-004 | Cancellation token support | Low | ⬜ | Inconsistent usage |
| TD-005 | XML documentation | Low | 🟡 | Public APIs |

---

## 📚 Documentation

| ID | Task | Priority | Status |
|----|------|----------|--------|
| DOC-001 | API endpoint docs (Swagger) | Medium | ⬜ |
| DOC-002 | Architecture diagrams | Low | ⬜ |
| DOC-003 | Developer onboarding guide | Low | ⬜ |
| DOC-004 | Deployment procedures | Medium | ⬜ |
| DOC-005 | Pattern usage examples | Medium | 🟡 |

---

## 📋 Task Details

### ARCH-006: Vertical Slice Architecture (Deferred to Post-MVP)

**Objective**: Reorganize to feature-based folder structure.

**Current (Horizontal)**:
```
Domain/Aggregates/, Exceptions/, Services/
Application/Dtos/, Services/
Server/Components/Pages/, Services/, State/
```

**Target (Vertical Slices)**:
```
Domain/Features/{Feature}/Aggregates, Exceptions, Repositories
Application/Features/{Feature}/Commands, Queries, Dtos, Services
Server/Features/{Feature}/Components, State, Services
```

**Status**: ⏸️ Deferred - Current structure works for MVP. Revisit post-MVP.

---

## ✅ Completed

### 2026-03-01

| Task | Category | Notes |
|------|----------|-------|
| ✅ ARCH-005 | Architecture | Response Pattern standardization - [Docs](../08-Patterns/RESPONSE_PATTERN_IMPLEMENTATION.md) |
| ✅ DOM-002 | Architecture | Result pattern with `ResultExtensions.cs` |
| ✅ UI-002 | UI/UX | `ApiErrorHelper.cs` - error handling |
| ✅ MenuList toggle fix | Bug | activate/deactivate endpoints |
| ✅ DOM-003 | Architecture | `MenuDomainException` handling |

### 2026-02-28

| Task | Category | Notes |
|------|----------|-------|
| ✅ Phase 4 | UI/UX | All 8 Blazor components |
| ✅ Phase 3 | Architecture | API Controllers |
| ✅ Phase 3.5 | Architecture | EF Configurations |

---

## 📊 Summary

```
Total: 42 tasks

By Status:                  By Priority:
⬜ Not Started: 28          🔥 Critical: 0
🟡 In Progress: 2           🔴 High: 5  
⏸️ Deferred: 7              🟡 Medium: 14
✅ Done: 5                  🟢 Low: 16
```

---

## 📝 New Task Template

```markdown
| ID | Task | Status | Notes |
|----|------|--------|-------|
| [CAT]-[NUM] | [Description] | ⬜/🟡/✅/⏸️ | [Details] |
```

**Categories**: `ARCH-`, `DOM-`, `API-`, `UI-`, `TEST-`, `DATA-`, `ENH-`, `PERF-`, `TD-`, `DOC-`

---

*Last Updated: 2026-03-01*
