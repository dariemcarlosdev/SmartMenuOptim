# Domain Exception Handling

**Date:** 2026-02-22  
**Branch:** `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## Overview

Domain-specific exceptions were introduced to give the application a clean, expressive way to surface business rule violations and entity lookup failures, keeping them clearly separate from infrastructure faults or programming errors. The design follows Clean Architecture: exceptions are **defined in the Domain layer** and **handled in the Infrastructure layer** via `GlobalExceptionHandlingMiddleware`.

---

## 1. Exception Hierarchy (Domain Layer)

```
System.Exception
 └── DomainException                     (base – business rule violations → HTTP 422)
      ├── EntityNotFoundException         (entity lookup failures → HTTP 404)
      ├── OrderDomainException
      ├── DishDomainException
      ├── MenuDomainException
      ├── PromotionDomainException
      ├── ReservationDomainException
      ├── RestaurantDomainException
      ├── TableDomainException
      └── LoyaltyDomainException
```

### `DomainException` — `SmartMenuOptim.Domain/Exceptions/DomainException.cs`

Base class for all business rule violations. Carries a user-friendly message and is mapped to **HTTP 422 Unprocessable Entity**, distinguishing domain intent from generic bad-input (HTTP 400).

```csharp
throw new DomainException("Cannot place an order without items.");
```

### `EntityNotFoundException` — `SmartMenuOptim.Domain/Exceptions/EntityNotFoundException.cs`

Derives from `DomainException`. Represents a "not found" domain condition and exposes `EntityName` and `EntityId` for structured error context. Mapped to **HTTP 404 Not Found**.

```csharp
throw new EntityNotFoundException("Order", orderId);
// → "Entity 'Order' with identifier '42' was not found."

throw new EntityNotFoundException("Dish", dishId, "The dish may have been removed from the menu.");
```

### Aggregate-Specific Exceptions

Each aggregate root has its own exception type (e.g., `OrderDomainException`) inheriting `DomainException`, allowing catch blocks and middleware to target specific aggregates when needed.

---

## 2. Middleware Handling (`GlobalExceptionHandlingMiddleware`)

Located at `SmartMenuOptim.Infrastructure/Infrastructure/Middlewares/ExceptionHandlingMiddleware.cs`.

The middleware wraps the entire request pipeline in a `try/catch` and routes exceptions via a `switch` expression. Domain exceptions are matched **before** generic .NET exceptions to ensure correct HTTP semantics.

### Exception → HTTP Status Mapping

| Exception | HTTP Status | Title |
|---|---|---|
| `EntityNotFoundException` | `404 Not Found` | `Not Found - Entity Not Found` |
| `DomainException` (all other) | `422 Unprocessable Entity` | `Domain Rule Violation - Unprocessable Entity` |
| `ArgumentException` / `ArgumentNullException` | `400 Bad Request` | `Bad Request` |
| `InvalidOperationException` | `400 Bad Request` | `Invalid Operation` |
| `UnauthorizedAccessException` | `401 Unauthorized` | `Unauthorized` |
| `KeyNotFoundException` | `404 Not Found` | `Not Found` |
| `TimeoutException` | `408 Request Timeout` | `Request Timeout` |
| `NotImplementedException` | `501 Not Implemented` | `Not Implemented` |
| All others | `500 Internal Server Error` | `Internal Server Error` |

### Switch Expression (relevant excerpt)

```csharp
return exception switch
{
    EntityNotFoundException entityNotFound =>
        (HttpStatusCode.NotFound, CreateErrorResponse(
            "Not Found - Entity Not Found",
            entityNotFound.Message,
            correlationId,
            entityNotFound)),

    DomainException domainEx =>
        (HttpStatusCode.UnprocessableEntity, CreateErrorResponse(
            "Domain Rule Violation - Unprocessable Entity",
            domainEx.Message,
            correlationId,
            domainEx)),

    // ... other cases
};
```

> `EntityNotFoundException` must appear **before** `DomainException` in the switch since it derives from it.

---

## 3. Error Response Structure

All exceptions produce a consistent JSON response. Sensitive details are suppressed in Production.

**Development**
```json
{
  "title": "Not Found - Entity Not Found",
  "message": "Entity 'Order' with identifier '42' was not found.",
  "correlationId": "0HN1GKQJ5K8QM:00000001",
  "timestamp": "2026-02-22T10:30:00.000Z",
  "details": {
    "exceptionType": "EntityNotFoundException",
    "exceptionMessage": "Entity 'Order' with identifier '42' was not found.",
    "stackTrace": "...",
    "innerException": null
  }
}
```

**Production**
```json
{
  "title": "Not Found - Entity Not Found",
  "message": "An error occurred while processing your request.",
  "correlationId": "0HN1GKQJ5K8QM:00000001",
  "timestamp": "2026-02-22T10:30:00.000Z"
}
```

---

## 4. Registration (`Program.cs`)

The middleware must be registered **first** in the HTTP pipeline so it intercepts all downstream exceptions.

```csharp
using SmartMenuOptim.Infrastructure.Infrastructure.Middlewares;

var app = builder.Build();

// Must be first
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
// ... remaining middleware
```

> **Blazor Note:** This middleware covers HTTP pipeline exceptions only (API calls, page requests). For component-level render errors, use `<ErrorBoundary>` components. Blazor SignalR circuit errors require `try/catch` in `@code` blocks.

---

## 5. Key Design Decisions

- **Domain layer owns exceptions** — no Infrastructure or Application dependencies required at throw sites.
- **`EntityNotFoundException` before `DomainException`** — pattern matching specificity enforced in switch order.
- **422 vs 404** — `DomainException` returns 422 (business rule violation); `EntityNotFoundException` returns 404 (resource absent).
- **Production safety** — `message` is redacted to a generic string; full details are logged server-side via `ILogger` with correlation IDs.
