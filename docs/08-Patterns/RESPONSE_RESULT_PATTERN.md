# Response/Result Pattern

> **Pattern Type:** Functional / Cross-Layer Error Handling  
> **Applied In:** SmartMenuOptim.Domain, SmartMenuOptim.Application, SmartMenuOptim.API, SmartMenuOptim.Server

> **Version**: 4.0  
> **Last Updated**: 2026-03-03

---

## Overview

This document describes the standardized Response/Result Pattern implementation across all layers of the SmartMenuOptimizer application, including naming conventions, file structure, and usage analysis.

The pattern is organized into two phases:
- **MVP (Current)**: 6 files actively in use — covers all current application needs
- **Post-MVP (Planned)**: 9 additional files ready for adoption when layer separation becomes necessary

---

## Pattern Flow

```
┌─────────────────────────────────────────────────────────────────┐
│              RESPONSE/RESULT PATTERN FLOW                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─ MVP (Current) ──────────────────────────────────────────┐  │
│  │                                                           │  │
│  │  ALL LAYERS                                               │  │
│  │  └─ Result<T>                                            │  │
│  │     └─ Location: Application\Common\                     │  │
│  │                                                           │  │
│  │  SERVER LAYER                                             │  │
│  │  └─ ApiErrorHelper + ProblemDetailsResponseDto           │  │
│  │     └─ Location: Server\Helpers\, Server\Models\Api\     │  │
│  │                                                           │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌─ Post-MVP (Planned) ────────────────────────────────────┐   │
│  │                                                          │   │
│  │  DOMAIN      → DomainResult<T> / DomainError            │   │
│  │       │                                                  │   │
│  │       ▼                                                  │   │
│  │  APPLICATION → Result<T> / ApplicationError              │   │
│  │       │        + ResultExtensions                        │   │
│  │       ▼                                                  │   │
│  │  API         → ApiResponse<T> / ApiControllerBase        │   │
│  │       │                                                  │   │
│  │       ▼                                                  │   │
│  │  BLAZOR      → ClientResult<T>                           │   │
│  │                + ClientResultExtensions                   │   │
│  │                                                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Naming Convention

### Rules

1. **Generic patterns** use their layer prefix — no suffix needed since they are unique per layer. The Application layer omits the prefix because `Result<T>` is the canonical type all other layers convert to/from
2. **Specific DTOs** always end with `Dto` — never embed `Result` in the DTO name to avoid confusion with the generic pattern
3. **Value Objects** use domain language (e.g., `Outcome`) — never `Result` to avoid confusion
4. **Extensions** match their target type name + `Extensions`
5. **Helpers** describe their action — no pattern suffix
6. **Domain service output models** live in `Services/Models/` — not `Services/Results/` to avoid confusion with the Result pattern

### By Type

| Category | Naming Rule | Layer | Examples |
|----------|------------|-------|---------|
| **Generic Result** | `{Layer}Result` or `Result` | All | `DomainResult<T>`, `Result<T>`, `ClientResult<T>` |
| **Generic Error** | `{Layer}Error` | Domain, Application | `DomainError`, `ApplicationError` |
| **Generic Response** | Descriptive, no suffix | Application, API | `ApiResponse<T>`, `PaginatedResponse<T>` |
| **Specific DTO** | `{Feature}{Concept}Dto` | Domain, Server | `ReviewSentimentDto`, `ProblemDetailsResponseDto` |
| **Value Object** | Domain language, no `Result` | Domain | `MenuValidationOutcome` |
| **Extensions** | `{TargetType}Extensions` | Application, Server | `ResultExtensions`, `ClientResultExtensions` |
| **Helpers** | `{Action}Helper` | Server | `ApiErrorHelper` |
| **Base Classes** | `{Layer}{Role}Base` | API | `ApiControllerBase` |

### Avoid

| ❌ Don't | ✅ Do | Why |
|----------|-------|-----|
| `MenuValidationResult` | `MenuValidationOutcome` | Conflicts with generic `Result` pattern |
| `ReviewSentimentResultDto` | `ReviewSentimentDto` | Don't embed `Result` in DTO names — it carries specific data, not a result pattern |
| `ProblemDetailsResponse` | `ProblemDetailsResponseDto` | Specific deserialization model, not a generic wrapper |
| `ClientError` | `ApplicationError` | Errors are scoped to the layer that defines them |
| `Services/Results/` folder | `Services/Models/` folder | "Results" conflicts with the Result pattern naming |

---

## Layer-by-Layer Structure

### 🟦 Domain Layer

| Folder | File | Type | Phase | Purpose |
|--------|------|------|-------|---------|
| `Common/` | `DomainResult.cs` | Generic Pattern | Post-MVP | Success/failure for domain operations |
| `Common/` | `DomainError.cs` | Generic Error | Post-MVP | Error with Code + Message |
| `Services/Models/` | `ReviewSentimentDto.cs` | Specific DTO | **MVP** | Sentiment analysis data |
| `Services/Models/` | `AggregateReviewSentimentDto.cs` | Specific DTO | **MVP** | Aggregate sentiment data |
| `ValueObjects/` | `MenuValidationOutcome.cs` | Value Object | **MVP** | Menu validation outcome |

**Used By:** `ReviewSentimentAnalysisService`, `MenuCompositionValidatorService`

---

### 🟩 Application Layer

| Folder | File | Type | Phase | Purpose |
|--------|------|------|-------|---------|
| `Common/` | `Result.cs` | Generic Pattern | **MVP** | Success/failure for all operations |
| `Common/` | `ApplicationError.cs` | Generic Error | Post-MVP | Error with Type (for HTTP mapping) |
| `Common/` | `ResultExtensions.cs` | Extensions | Post-MVP | Map/Bind/Match helpers |
| `Common/` | `PaginatedResponse.cs` | Generic Response | Post-MVP | Pagination wrapper |

**Used By:** (`Result.cs` — 11+ files)
- `IRestaurantService`, `RestaurantService`
- `IMenuService`, `MenuService`
- `ICategoryService`, `CategoryService`
- `RestaurantsController`, `AiController`
- `IRestaurantClientService`, `RestaurantClientService`

---

### 🟨 API Layer

| Folder | File | Type | Phase | Purpose |
|--------|------|------|-------|---------|
| `Common/` | `ApiResponse.cs` | Generic Response | Post-MVP | Standard API response wrapper |
| `Common/` | `ApiControllerBase.cs` | Base Class | Post-MVP | ProblemDetails helpers |

**Status:** Available for migration. Controllers currently use `ControllerBase`.

---

### 🟪 Server Layer (Blazor)

| Folder | File | Type | Phase | Purpose |
|--------|------|------|-------|---------|
| `Common/` | `ClientResult.cs` | Generic Pattern | Post-MVP | Client-side result for Blazor |
| `Common/` | `ClientResultExtensions.cs` | Extensions | Post-MVP | HTTP → ClientResult conversion |
| `Helpers/` | `ApiErrorHelper.cs` | Helper | **MVP** | Error message extraction |
| `Models/Api/` | `ProblemDetailsResponseDto.cs` | Specific DTO | **MVP** | RFC 7807 parsing |

**Used By:** `RestaurantClientService` (uses `ApiErrorHelper`, `ProblemDetailsResponseDto`)

---

## File Structure

```
SmartMenuOptim.Domain/
├── Common/                              ← GENERIC PATTERNS
│   ├── DomainResult.cs
│   └── DomainError.cs
├── Services/Models/                     ← DOMAIN SERVICE OUTPUT MODELS
│   ├── ReviewSentimentDto.cs
│   └── AggregateReviewSentimentDto.cs
└── ValueObjects/                        ← VALUE OBJECTS
    └── MenuValidationOutcome.cs

SmartMenuOptim.Application/
└── Common/                              ← GENERIC PATTERNS
    ├── Result.cs
    ├── ApplicationError.cs
    ├── ResultExtensions.cs
    └── PaginatedResponse.cs

SmartMenuOptim.API/
└── Common/                              ← GENERIC PATTERNS
    ├── ApiResponse.cs
    └── ApiControllerBase.cs

SmartMenuOptim.Server/
├── Common/                              ← GENERIC PATTERNS
│   ├── ClientResult.cs
│   └── ClientResultExtensions.cs
├── Helpers/
│   └── ApiErrorHelper.cs
└── Models/Api/                          ← SPECIFIC DTOs
    └── ProblemDetailsResponseDto.cs
```


## Usage Status

### MVP — Active (6 files)

These files cover all current application needs.

| File | Layer | Purpose |
|------|-------|---------|
| `Result.cs` | Application | Success/failure for all service operations |
| `ApiErrorHelper.cs` | Server | HTTP error message extraction |
| `ProblemDetailsResponseDto.cs` | Server | RFC 7807 error parsing |
| `MenuValidationOutcome.cs` | Domain | Menu composition validation |
| `ReviewSentimentDto.cs` | Domain | Single review sentiment data |
| `AggregateReviewSentimentDto.cs` | Domain | Aggregate sentiment data |

**MVP Pattern Flow:**
```
All Layers → Result<T> (Application)
API Error Parsing → ApiErrorHelper + ProblemDetailsResponseDto
Domain Outputs → ReviewSentimentDto, MenuValidationOutcome
```

### Post-MVP — Available (9 files)

These files are built, tested, and documented — ready for adoption when:
- Multiple teams work on different layers independently
- Error handling needs differ significantly per layer
- Compile-time enforcement of layer boundaries is required

| File | Layer | Status | Adopts When |
|------|-------|--------|-------------|
| `DomainResult.cs` | Domain | 🟡 Ready | Domain services need own result type |
| `DomainError.cs` | Domain | 🟡 Ready | Domain errors need Code + Message |
| `ApplicationError.cs` | Application | 🟡 Ready | Controllers need ErrorType → HTTP mapping |
| `ResultExtensions.cs` | Application | 🟡 Ready | Services chain Map/Bind/Match operations |
| `PaginatedResponse.cs` | Application | 🟡 Ready | Pagination endpoints are implemented |
| `ApiResponse.cs` | API | ⚠️ Pending | Standard API response wrapper needed |
| `ApiControllerBase.cs` | API | ⚠️ Pending | Controllers migrate from `ControllerBase` |
| `ClientResult.cs` | Server | ⚠️ Pending | Client services need Blazor-specific result |
| `ClientResultExtensions.cs` | Server | ⚠️ Pending | HTTP → ClientResult conversion needed |

**Legend:**
- 🟡 Ready: Built and available, adopt when the need arises
- ⚠️ Pending: Built, requires migration of existing code

---

## Error Code Convention

```
Format: {Entity}.{ErrorType}

Examples:
├── Menu.NotFound
├── Menu.ValidationError
├── Menu.CannotActivate
├── Restaurant.InvalidStatus
├── Dish.Conflict
└── General.UnexpectedError
```

---

## HTTP Status Code Mapping

| ErrorType | HTTP Status | Usage |
|-----------|-------------|-------|
| NotFound | 404 | Resource doesn't exist |
| Validation | 400 | Input validation failed |
| BusinessRule | 422 | Domain rule violation |
| Conflict | 409 | Duplicate or state conflict |
| Unexpected | 500 | Server error |

---

## Current vs Target Flow

### MVP Flow (Current — sufficient for current needs)

```
Domain Services → Result<T> (Application)
Application Services → Result<T>
API Controllers → ActionResult + manual ProblemDetails
Client Services → Result<T> + ApiErrorHelper
Blazor Components → Result<T>.IsSuccess/Error
```

### Post-MVP Flow (Target — adopt incrementally when needed)

| Aspect | MVP State | Post-MVP State | Trigger to Migrate |
|--------|-----------|----------------|-------------------|
| **Domain** | Uses `Result<T>` from Application | Uses own `DomainResult<T>` | Domain layer needs independence from Application |
| **Application** | `Result<T>` directly | `Result<T>` via `ResultExtensions` | Services need Map/Bind/Match chaining |
| **API** | Manual `ProblemDetails` creation | `ApiControllerBase` helpers | Multiple controllers duplicate error handling |
| **Client Services** | `Result<T>` + `ApiErrorHelper` | `ClientResult<T>` + `ClientResultExtensions` | Blazor needs `IsNotFound`, `IsValidationError` helpers |
| **Blazor Components** | Checks `Result<T>.IsSuccess` | Checks `ClientResult<T>.IsSuccess` | Components need richer error classification |

```
Post-MVP:
Domain Services → DomainResult<T>
Application Services → Result<T> (via ResultExtensions)
API Controllers → ApiControllerBase.ToActionResult()
Client Services → ClientResult<T> (via ClientResultExtensions)
Blazor Components → ClientResult<T>.IsSuccess/Error
```

**Why migrate (when the time comes)?**
- **Separation of Concerns**: Each layer has its own result type optimized for its context
- **Type Safety**: `ClientResult<T>` has Blazor-specific helpers like `IsNotFound`
- **Consistency**: All controllers use the same base class and helper methods
- **Maintainability**: Changes to error handling are centralized

---

## Result Pattern Guidelines

This section consolidates the guidance previously in `RESULT_PATTERN.md` (now merged).

### Problem It Solves

| Problem | Solution |
|---------|----------|
| Exceptions for expected failures | Explicit `Result` return type |
| Hidden failure paths | Visible `IsSuccess` / `IsFailure` |
| Stack traces for business errors | Clean error messages |
| Try-catch everywhere | Pattern matching on `Result` |
| Null returns for failures | Typed `Result` with `Error` property |

### Common Scenarios

| Scenario | Factory Method | Error Message Example |
|----------|---------------|----------------------|
| Entity found | `Result.Success(entity)` | — |
| Entity not found | `Result.Failure<T>(...)` | "Restaurant not found." |
| Validation failed | `Result.Failure<T>(...)` | "Name is required." |
| Duplicate exists | `Result.Failure<T>(...)` | "Already exists." |
| Delete succeeded | `Result.Success()` | — |
| Delete failed | `Result.Failure(...)` | "Failed to delete." |
| Network error | `Result.Failure<T>(...)` | "Unable to connect." |

### Best Practices

1. **Always check `IsSuccess` before accessing `Value`** — `Value` may be null on failure
2. **Provide user-friendly error messages** — avoid exposing technical details like SQL errors
3. **Use null-coalescing for fallback errors** — `result.Error ?? "An error occurred."`
4. **Combine success + null check** — `if (result.IsSuccess && result.Value is not null)`

### When to Use

| ✅ Use Result Pattern | ❌ Use Exceptions |
|-----------------------|-------------------|
| Service layer operations | Unexpected errors (out of memory) |
| Repository operations that can fail | Programming errors (null reference) |
| Validation operations | Infrastructure failures that shouldn't be caught |
| HTTP client operations | |
| Any operation with expected failure cases | |

### Benefits

| Benefit | Description |
|---------|-------------|
| **Explicit** | Success/failure is obvious in type signature |
| **No Exceptions** | Expected failures don't throw |
| **Error Messages** | User-friendly messages without stack traces |
| **Type Safety** | Compiler helps ensure handling |
| **Composable** | Can chain operations via `ResultExtensions` |
| **Testable** | Easy to assert on `Result` properties |

---

## Post-MVP Migration Tasks

These tasks are **not required for MVP**. Adopt incrementally when the trigger conditions in the table above are met.

| Task | Priority | Status | Trigger |
|------|----------|--------|---------|
| Migrate controllers to `ApiControllerBase` | Medium | ⬜ Deferred | Multiple controllers duplicate error logic |
| Migrate client services to `ClientResult<T>` | Low | ⬜ Deferred | Blazor needs richer error classification |
| Use `ResultExtensions` in services | Low | ⬜ Deferred | Services need operation chaining |
| Adopt `DomainResult<T>` in Domain Services | Low | ⬜ Deferred | Domain layer needs independence |

---

## Related Documentation

- [Pattern Catalog](./README.md)
- [Pending Tasks](../09-ProjectManagement/PENDING_TASKS.md)

---

*Last Updated: 2026-03-03*
