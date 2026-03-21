# 🏗️ Restaurant Module — Reference Implementation Guide

> **SmartMenuOptimizer — Canonical Patterns for Feature Module Implementation**  
> **Version**: 1.0  
> **Created**: 2026-03-14  
> **Last Updated**: 2026-03-14  
> **Architecture**: [ADR-005 — Vertical Slice + Aggregate-Centric](../02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md)

---

## Purpose

The **Restaurant Management module** is the first fully implemented feature in SmartMenuOptimizer and serves as the **canonical reference implementation** (golden path) for all subsequent feature modules.

**Every new feature module MUST follow the patterns, conventions, and file structures established here.** This ensures consistency across the codebase, reduces onboarding friction, and enables predictable code review.

> **Rule**: Before implementing any layer of a new module, read the corresponding Restaurant reference file first. Match its structure, naming, documentation style, and error handling — then adapt the domain-specific logic.

---

## Quick Reference — File Map

When implementing **Module X**, create files that mirror these Restaurant files:

| Layer | Restaurant Reference File | New Module Pattern |
|-------|--------------------------|-------------------|
| **DTOs** | `Application/Features/Restaurants/DTOs/RestaurantDTO.cs` | `Application/Features/{Module}/DTOs/{Module}DTO.cs` |
| **DTOs** | `Application/Features/Restaurants/DTOs/RestaurantDetailDTO.cs` | `Application/Features/{Module}/DTOs/{Module}DetailDTO.cs` |
| **DTOs** | `Application/Features/Restaurants/DTOs/RestaurantCreateDTO.cs` | `Application/Features/{Module}/DTOs/{Module}CreateDTO.cs` |
| **DTOs** | `Application/Features/Restaurants/DTOs/RestaurantUpdateDTO.cs` | `Application/Features/{Module}/DTOs/{Module}UpdateDTO.cs` |
| **Mappings** | `Application/Features/Restaurants/Mappings/RestaurantMappingExtensions.cs` | `Application/Features/{Module}/Mappings/{Module}MappingExtensions.cs` |
| **Service Interface** | `Application/Features/Restaurants/Services/IRestaurantService.cs` | `Application/Features/{Module}/Services/I{Module}Service.cs` |
| **Service Impl** | `Application/Features/Restaurants/Services/RestaurantService.cs` | `Application/Features/{Module}/Services/{Module}Service.cs` |
| **DI Registration** | `Application/Extensions/ApplicationServiceCollectionExtensions.cs` | Add `services.AddScoped<I{Module}Service, {Module}Service>()` |
| **Global Usings** | `Application/GlobalDtoUsings.cs` | Add `global using` aliases for new DTOs |
| **API Controller** | `API/Features/Restaurants/v1/RestaurantsController.cs` | `API/Features/{Module}/v1/{Module}sController.cs` |
| **API Global Usings** | `API/GlobalDtoUsings.cs` | Add namespace import + aliases |
| **Blazor Components** | `Server/Features/Restaurants/Components/RestaurantList.razor(.cs)` | `Server/Features/{Module}/Components/{Module}List.razor(.cs)` |
| **Blazor Components** | `Server/Features/Restaurants/Components/RestaurantDetail.razor(.cs)` | `Server/Features/{Module}/Components/{Module}Detail.razor(.cs)` |
| **Client Service** | `Server/Features/Restaurants/Services/IRestaurantClientService.cs` | `Server/Features/{Module}/Services/I{Module}ClientService.cs` |
| **Client Service** | `Server/Features/Restaurants/Services/RestaurantClientService.cs` | `Server/Features/{Module}/Services/{Module}ClientService.cs` |
| **State Containers** | `Server/Features/Restaurants/State/RestaurantListState.cs` | `Server/Features/{Module}/State/{Module}ListState.cs` |
| **State Containers** | `Server/Features/Restaurants/State/RestaurantDetailState.cs` | `Server/Features/{Module}/State/{Module}DetailState.cs` |
| **NavMenu** | `Server/Components/Layout/NavMenu.razor` | Add new link entry |

---

## Layer-by-Layer Patterns

### 1. DTOs (`Application/Features/{Module}/DTOs/`)

**Reference**: `RestaurantDTO.cs`, `RestaurantCreateDTO.cs`, `RestaurantUpdateDTO.cs`

| Convention | Detail |
|------------|--------|
| **Type** | Mutable POCOs with `get; set;` (not records) — required for Blazor two-way binding |
| **Defaults** | `string` → `= string.Empty;`, `List<T>` → `= [];` |
| **Nullable** | Optional fields use `?` (e.g., `string? Description`) |
| **Validation** | `DataAnnotations` on Create/Update DTOs (`[Required]`, `[StringLength]`, `[Range]`) |
| **XML Docs** | `///` on every public property |
| **Namespace** | `SmartMenuOptim.Application.Features.{Module}.DTOs` |
| **Naming** | `{Entity}DTO` (list), `{Entity}DetailDTO` (detail), `{Entity}CreateDTO`, `{Entity}UpdateDTO` |

**DTO Family Pattern**:
```
{Entity}DTO          → List/card views (lightweight, includes computed fields like ItemCount)
{Entity}DetailDTO    → Detail pages (includes nested child DTOs, timestamps)
{Entity}CreateDTO    → Create forms (validation attributes, required fields)
{Entity}UpdateDTO    → Update forms (includes Id, validation attributes)
{Child}DTO           → Nested in detail DTOs (e.g., OrderItemDTO inside OrderDetailDTO)
{Child}CreateDTO     → Nested in create DTOs (e.g., OrderItemCreateDTO inside OrderCreateDTO)
{Lookup}DTO          → Dropdown/filter data (e.g., OrderStatusDTO)
```

### 2. Mapping Extensions (`Application/Features/{Module}/Mappings/`)

**Reference**: `RestaurantMappingExtensions.cs`

| Convention | Detail |
|------------|--------|
| **Type** | `public static class {Module}MappingExtensions` |
| **Methods** | `public static {Entity}DTO ToDto(this {Entity} entity)` |
| **Guard** | `ArgumentNullException.ThrowIfNull(entity)` as first line |
| **Null safety** | Navigation properties accessed with `?.` (e.g., `entity.Customer?.Name`) |
| **Section headers** | `// ═══ {ENTITY} MAPPINGS ═══` comment blocks |
| **No AutoMapper** | Manual mapping via extension methods only |

### 3. Service Interface (`Application/Features/{Module}/Services/`)

**Reference**: `IRestaurantService.cs`

| Convention | Detail |
|------------|--------|
| **Returns** | `Task<Result<T>>` for queries, `Task<Result>` for void commands |
| **CancellationToken** | Every method includes `CancellationToken cancellationToken = default` |
| **XML docs** | Full `<summary>`, `<param>`, `<returns>` on every method |
| **Section headers** | `// ═══ QUERIES ═══` and `// ═══ COMMANDS ═══` |
| **Namespace** | `SmartMenuOptim.Application.Features.{Module}.Services` |

### 4. Service Implementation (`Application/Features/{Module}/Services/`)

**Reference**: `RestaurantService.cs`

| Convention | Detail |
|------------|--------|
| **Constructor** | `IUnityOfWork` + `ILogger<{Module}Service>` — null-checked with `?? throw new ArgumentNullException()` |
| **Query pattern** | `_unitOfWork.{Entities}.Query().Include(...).Where(!IsDeleted).ToListAsync()` → `.Select(e => e.ToDto()).ToList()` |
| **Command pattern** | Create entity → `AddAsync` → `SaveChangesAsync` → return `ToDto()` |
| **Error handling** | `try/catch` with `catch (DomainException)` → `Result.Failure(ex.Message)`, `catch (ArgumentException)`, `catch (Exception)` |
| **Logging** | `LogDebug` for queries, `LogInformation` for commands, `LogWarning` for not-found/validation, `LogError` for unexpected |
| **Result pattern** | `Result<T>.Success(value)` / `Result<T>.Failure(message)` — never throw for expected failures |

### 5. DI Registration (`Application/Extensions/ApplicationServiceCollectionExtensions.cs`)

**Reference**: Lines 56–60

```csharp
// {Module} Management Application Services (Phase N)
services.AddScoped<I{Module}Service, {Module}Service>();
```

Group by feature module with a comment header.

### 6. Global DTO Usings (`Application/GlobalDtoUsings.cs`)

**Reference**: Lines 40–46

```csharp
// {Module} DTOs (migrated to Features/{Module}/DTOs/)
global using {Entity}DTO = SmartMenuOptim.Application.Features.{Module}.DTOs.{Entity}DTO;
```

### 7. API Controller (`API/Features/{Module}/v1/`)

**Reference**: `RestaurantsController.cs`

| Convention | Detail |
|------------|--------|
| **Base class** | Inherits `ControllerBase` (note: `ApiControllerBase` exists but Restaurant uses `ControllerBase` directly) |
| **Attributes** | `[ApiController]`, `[Route("api/v1/[controller]")]`, `[Produces("application/json")]` |
| **Constructor** | `I{Module}Service` + `ILogger<{Module}sController>` |
| **Query endpoints** | Return `ActionResult<T>` — `result.IsSuccess ? Ok(result.Value) : NotFound(CreateProblemDetails(...))` |
| **Create endpoint** | Return `CreatedAtAction(nameof(GetByIdAsync), new { id = result.Value.Id }, result.Value)` (201) |
| **Delete endpoint** | Return `NoContent()` (204) on success |
| **Error responses** | `CreateProblemDetails(errorCode, message, statusCode)` — RFC 7807 ProblemDetails |
| **Error codes** | `"{Entity}.NotFound"`, `"{Entity}.Conflict"`, `"{Entity}.ValidationError"` |
| **Swagger** | `[ProducesResponseType]` on every endpoint, full XML doc with `<response>` tags |
| **Logging** | `"API: {Action} {Entity} with ID {Id}"` format |

**Helper method** (defined inline in Restaurant, also available via `ApiControllerBase`):

```csharp
private static ProblemDetails CreateProblemDetails(string errorCode, string detail, int statusCode)
{
    return new ProblemDetails
    {
        Type = $"https://httpstatuses.com/{statusCode}",
        Title = ...,
        Detail = detail,
        Status = statusCode,
        Extensions = { ["errorCode"] = errorCode }
    };
}
```

### 8. Blazor Components (`Server/Features/{Module}/Components/`)

**Reference patterns documented in**: `docs/08-Patterns/`

| Pattern | Reference Doc | Key Principle |
|---------|---------------|---------------|
| **Code-Behind** | `CODE_BEHIND_PATTERN.md` | `.razor` (markup) + `.razor.cs` (logic) — always separated |
| **State Container** | `BLAZOR_STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md` | Extend `ComponentStateBase` — `SetLoading()`, `SetData()`, `SetError()` |
| **Client Service** | `CLIENT_SERVICE_ADAPTER_PATTERN.md` | `I{Module}ClientService` → `{Module}ClientService` — returns `ClientResult<T>` |
| **Reusable Components** | `REUSABLE_UI_COMPONENTS_PATTERN.md` | `ErrorAlert`, `LoadingSpinner`, `NotFoundAlert`, `DetailCard`, `StatItem` |
| **Response Pattern** | `RESPONSE_RESULT_PATTERN.md` | Domain `Result<T>` → API `ApiResponse<T>` → Client `ClientResult<T>` |

### 9. API Global Usings (`API/GlobalDtoUsings.cs`)

**Reference**: Lines 4–16

```csharp
global using SmartMenuOptim.Application.Features.{Module}.DTOs;
```

### 10. NavMenu Update (`Server/Components/Layout/NavMenu.razor`)

Add new `NavLink` entry following the existing pattern with appropriate icon.

---

## Implementation Phase Order

All modules follow this phase order (matching the [Implementation Tracker](../07-Features/02-OrderManagement/ORDER_MODULE_IMPLEMENTATION_TRACKER.md) template):

```
Phase 1: Domain Layer          ← Aggregates, entities, events, exceptions
Phase 2: EF Core & Infra       ← Configurations, DbContext, seed data, UoW repositories
Phase 3: Event Handlers         ← Domain event handlers (if applicable)
Phase 4: DTOs & Service         ← DTOs, mappings, service interface + implementation, DI, global usings
Phase 5: API Controllers        ← REST endpoints, Swagger docs, API global usings
Phase 6: Blazor UI              ← Components, client services, state containers, NavMenu
Phase 7: Integration & Testing  ← Dashboard, unit tests, integration tests
```

> **Critical**: Complete each phase fully before starting the next. Each phase should compile cleanly (`run_build` ✅).

---

## Documentation Per Module

Every feature module MUST have these three tracking documents:

| Document | Naming Convention | Purpose |
|----------|-------------------|---------|
| **Implementation Plan** | `{MODULE}_MODULE_IMPLEMENTATION_PLAN.md` | Structural reference — sections match phases, with detailed specs per layer |MVP vs Post-MVP, priority categorization
| **Implementation Tracker** | `{MODULE}_MODULE_IMPLEMENTATION_TRACKER.md` | Progress tracking — phase bars, task tables, version history |MVP vs Post-MVP, priority categorization
| **Pending Task Tracker** | `{MODULE}_PENDING_TASK_TRACKER.md` | Backlog — MVP vs Post-MVP, priority categorization |

All three use the `📐 Document Structure Reference` template section defined in the Restaurant Tracker.

---

## Cross-Cutting Conventions

| Convention | Standard |
|------------|----------|
| **Namespace collision** | Use **plural** for feature folders: `Features.Restaurants` (not `Features.Restaurant`) to avoid collision with class names |
| **Soft delete** | All entities support `IsDeleted` — filter with `!e.IsDeleted` in queries |
| **Multi-tenant** | Tenant-scoped entities filter by `RestaurantId` |
| **File headers** | `/* File, Purpose, Version, .NET Target */` block comment on service/controller files |
| **XML docs** | `///` on all public classes, methods, and properties |
| **Section separators** | `// ═══════════ SECTION NAME ═══════════` for major code sections |
| **Commit messages** | [Conventional Commits](https://www.conventionalcommits.org) — `feat(orders): add OrdersController with 7 endpoints` |

---

## Verified Module Implementations

| Module | Status | Notes |
|--------|--------|-------|
| **Restaurant Management** | ✅ MVP Complete | Canonical reference — all patterns established here |
| **Order Management** | 🟡 Phase 4 Complete | Following Restaurant patterns — Phase 5 next |

---

## Related Documentation

| Document | Location |
|----------|----------|
| Restaurant Tracker | `docs/07-Features/01-RestaurantManagement/RESTAURANT_MODULE_IMPLEMENTATION_TRACKER.md` |
| Restaurant Plan | `docs/07-Features/01-RestaurantManagement/RESTAURANT_MODULE_IMPLEMENTATION_PLAN.md` |
| Order Tracker | `docs/07-Features/02-OrderManagement/ORDER_MODULE_IMPLEMENTATION_TRACKER.md` |
| Order Plan | `docs/07-Features/02-OrderManagement/ORDER_MODULE_IMPLEMENTATION_PLAN.md` |
| Vertical Slice ADR | `docs/02-Architecture/ADR-005-VERTICAL-SLICE-AND-AGGREGATE-CENTRIC-ARCHITECTURE.md` |
| Interface Placement ADR | `docs/02-Architecture/ADR-004-INTERFACE-PLACEMENT-RULES.md` |
| Blazor Patterns Index | `docs/08-Patterns/README.md` |
| Copilot Instructions | `.github/copilot-instructions.md` |

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-03-14 | Initial creation — consolidated scattered Restaurant references into single authoritative guide |

---

*This document is the single source of truth for implementation patterns. When in doubt, read the corresponding Restaurant file first, then adapt for the target module.*
