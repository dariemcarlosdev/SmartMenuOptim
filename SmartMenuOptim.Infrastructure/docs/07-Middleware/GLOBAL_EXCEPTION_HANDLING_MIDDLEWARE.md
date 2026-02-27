# Global Exception Handling Middleware

> **Layer:** Infrastructure  
> **Location:** `SmartMenuOptim.Infrastructure/Infrastructure/Middlewares/ExceptionHandlingMiddleware.cs`  
> **Last Updated:** 2026-02-24

---

## Overview

The `GlobalExceptionHandlingMiddleware` is a production-ready ASP.NET Core middleware that provides centralized exception handling for the HTTP request pipeline. It integrates with the **Domain layer's custom exception hierarchy** to deliver semantically correct HTTP responses based on exception type.

### Key Capabilities

| Feature | Description |
|---------|-------------|
| **Domain Exception Integration** | Maps `DomainException` and `EntityNotFoundException` to appropriate HTTP status codes |
| **Correlation IDs** | Tracks requests across distributed systems via `HttpContext.TraceIdentifier` |
| **Environment-Aware Responses** | Full details in Development, sanitized messages in Production |
| **Structured JSON Responses** | Consistent error format with `ErrorResponse` and `ErrorDetails` DTOs |
| **Comprehensive Logging** | Structured logging with correlation context for observability |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        EXCEPTION HANDLING FLOW                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐    ┌──────────────────────────────────────┐               │
│  │ HTTP Request │───►│ GlobalExceptionHandlingMiddleware    │               │
│  └──────────────┘    │                                      │               │
│                      │  try {                               │               │
│                      │      await _next(context);           │               │
│                      │  }                                   │               │
│                      │  catch (Exception ex) {              │               │
│                      │      HandleExceptionAsync(...)       │               │
│                      │  }                                   │               │
│                      └──────────────────────────────────────┘               │
│                                       │                                      │
│                      ┌────────────────┼────────────────┐                    │
│                      │                │                │                    │
│                      ▼                ▼                ▼                    │
│              ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│              │   Domain    │  │ Application │  │    .NET     │              │
│              │ Exceptions  │  │ Exceptions  │  │ Exceptions  │              │
│              └─────────────┘  └─────────────┘  └─────────────┘              │
│                      │                │                │                    │
│                      ▼                ▼                ▼                    │
│              ┌─────────────────────────────────────────────────┐            │
│              │           GetErrorResponse() Switch             │            │
│              │  ┌─────────────────────────────────────────┐   │            │
│              │  │ EntityNotFoundException  →  404         │   │            │
│              │  │ DomainException          →  422         │   │            │
│              │  │ ArgumentException        →  400         │   │            │
│              │  │ UnauthorizedAccess       →  401         │   │            │
│              │  │ KeyNotFoundException     →  404         │   │            │
│              │  │ TimeoutException         →  408         │   │            │
│              │  │ NotImplementedException  →  501         │   │            │
│              │  │ All Others               →  500         │   │            │
│              │  └─────────────────────────────────────────┘   │            │
│              └─────────────────────────────────────────────────┘            │
│                                       │                                      │
│                                       ▼                                      │
│              ┌─────────────────────────────────────────────────┐            │
│              │              JSON Error Response                │            │
│              │  {                                              │            │
│              │    "title": "...",                              │            │
│              │    "message": "...",                            │            │
│              │    "correlationId": "...",                      │            │
│              │    "timestamp": "...",                          │            │
│              │    "details": { ... } // Dev only               │            │
│              │  }                                              │            │
│              └─────────────────────────────────────────────────┘            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Domain Exception Integration

### Exception Hierarchy (Domain Layer)

The middleware integrates with the domain exception hierarchy defined in `SmartMenuOptim.Domain/Exceptions/`:

```
System.Exception
 └── DomainException                     (HTTP 422 Unprocessable Entity)
      ├── EntityNotFoundException         (HTTP 404 Not Found)
      ├── OrderDomainException
      ├── DishDomainException
      ├── MenuDomainException
      ├── PromotionDomainException
      ├── ReservationDomainException
      ├── RestaurantDomainException
      ├── TableDomainException
      └── LoyaltyDomainException
```

### Cross-Layer Reference

| Layer | Component | Responsibility |
|-------|-----------|----------------|
| **Domain** | `DomainException` | Base class for business rule violations |
| **Domain** | `EntityNotFoundException` | Entity lookup failures with `EntityName` and `EntityId` |
| **Domain** | `*DomainException` | Aggregate-specific exceptions (Order, Dish, Menu, etc.) |
| **Infrastructure** | `GlobalExceptionHandlingMiddleware` | Catches and maps exceptions to HTTP responses |

### Switch Expression Order

⚠️ **Critical:** `EntityNotFoundException` must appear **before** `DomainException` in the switch expression because it derives from `DomainException`. Pattern matching evaluates in order, so the more specific type must come first.

```csharp
return exception switch
{
    // MUST be first - more specific than DomainException
    EntityNotFoundException entityNotFound =>
        (HttpStatusCode.NotFound, CreateErrorResponse(...)),

    // Catches all other DomainException subtypes
    DomainException domainEx =>
        (HttpStatusCode.UnprocessableEntity, CreateErrorResponse(...)),

    // ... other exception types
};
```

---

## HTTP Status Code Mapping

| Exception Type | HTTP Status | Title | Use Case |
|----------------|-------------|-------|----------|
| `EntityNotFoundException` | `404 Not Found` | Not Found - Entity Not Found | Entity lookup by ID failed |
| `DomainException` | `422 Unprocessable Entity` | Domain Rule Violation | Business rule violation |
| `ArgumentException` | `400 Bad Request` | Bad Request | Invalid argument values |
| `ArgumentNullException` | `400 Bad Request` | Bad Request | Null argument passed |
| `InvalidOperationException` | `400 Bad Request` | Invalid Operation | Operation not valid for current state |
| `UnauthorizedAccessException` | `401 Unauthorized` | Unauthorized | Authentication/authorization failure |
| `KeyNotFoundException` | `404 Not Found` | Not Found | Generic key-based lookup failure |
| `TimeoutException` | `408 Request Timeout` | Request Timeout | Operation timed out |
| `NotImplementedException` | `501 Not Implemented` | Not Implemented | Feature not yet available |
| All others | `500 Internal Server Error` | Internal Server Error | Unexpected errors |

### Why 422 for DomainException?

| Status Code | Meaning | When to Use |
|-------------|---------|-------------|
| **400 Bad Request** | Malformed request syntax | Invalid JSON, missing required fields |
| **422 Unprocessable Entity** | Request understood but semantically invalid | Business rule violations |

`DomainException` represents **business rule violations** — the request was syntactically correct but violates domain invariants. RFC 4918 defines 422 specifically for this scenario.

---

## Response Structure

### ErrorResponse DTO

```csharp
public class ErrorResponse
{
    public string Title { get; set; }        // Error category/type
    public string Message { get; set; }      // User-friendly message
    public string CorrelationId { get; set; } // Request tracking ID
    public DateTime Timestamp { get; set; }   // UTC timestamp
    public ErrorDetails? Details { get; set; } // Dev-only details
}
```

### ErrorDetails DTO (Development Only)

```csharp
public class ErrorDetails
{
    public string? ExceptionType { get; set; }    // e.g., "EntityNotFoundException"
    public string? ExceptionMessage { get; set; } // Full exception message
    public string? StackTrace { get; set; }       // Stack trace
    public string? InnerException { get; set; }   // Inner exception message
}
```

### Example Responses

**Development Environment:**
```json
{
  "title": "Not Found - Entity Not Found",
  "message": "Entity 'Order' with identifier '42' was not found.",
  "correlationId": "0HN1GKQJ5K8QM:00000001",
  "timestamp": "2026-02-24T10:30:00.000Z",
  "details": {
    "exceptionType": "EntityNotFoundException",
    "exceptionMessage": "Entity 'Order' with identifier '42' was not found.",
    "stackTrace": "at SmartMenuOptim.Application.Services...",
    "innerException": null
  }
}
```

**Production Environment:**
```json
{
  "title": "Not Found - Entity Not Found",
  "message": "An error occurred while processing your request.",
  "correlationId": "0HN1GKQJ5K8QM:00000001",
  "timestamp": "2026-02-24T10:30:00.000Z"
}
```

---

## Registration

### API Project (`SmartMenuOptim.API`)

**Location:** `SmartMenuOptim.API/Extensions/WebApplicationExtensions.cs`

```csharp
public static WebApplication ConfigureHttpPipeline(this WebApplication app)
{
    // 1. GLOBAL EXCEPTION HANDLING - Must be first
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    // 2. SWAGGER
    app.UseSwagger();
    // ... remaining middleware
}
```

### Blazor Server Project (`SmartMenuOptim.Server`)

**Location:** `SmartMenuOptim.Server/Extensions/WebApplicationExtensions.cs`

```csharp
public static WebApplication ConfigureHttpPipeline(this WebApplication app)
{
    // Global Exception Handling - Must be first
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    app.UseRouting();
    // ... remaining middleware
}
```

### Pipeline Position

```
┌────────────────────────────────────────────────────────────────┐
│  HTTP REQUEST PIPELINE (order of execution)                     │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Request ──► GlobalExceptionHandlingMiddleware  ◄── MUST BE    │
│                 │                                    FIRST!     │
│                 ▼                                               │
│              UseSwagger() / UseRouting()                        │
│                 │                                               │
│                 ▼                                               │
│              ... other middleware ...                           │
│                 │                                               │
│                 ▼                                               │
│              Controller / Blazor Component                      │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

---

## Blazor-Specific Considerations

⚠️ **Important:** This middleware only handles **HTTP pipeline exceptions**. Blazor has additional error sources:

| Error Source | Middleware Catches? | Solution |
|--------------|---------------------|----------|
| API calls (HttpClient) | ✅ Yes | Middleware handles |
| Page requests | ✅ Yes | Middleware handles |
| Component render errors | ❌ No | Use `<ErrorBoundary>` |
| SignalR circuit errors | ❌ No | Try-catch in `@code` blocks |
| Event handler exceptions | ❌ No | Try-catch in `@code` blocks |

### ErrorBoundary Example for Blazor Components

```razor
<ErrorBoundary>
    <ChildContent>
        <YourComponent />
    </ChildContent>
    <ErrorContent Context="ex">
        <div class="alert alert-danger">
            <strong>Error:</strong> @ex.Message
        </div>
    </ErrorContent>
</ErrorBoundary>
```

---

## Logging & Observability

### Structured Logging

All exceptions are logged with structured parameters for observability platforms (Application Insights, Seq, etc.):

```csharp
_logger.LogError(
    exception,
    "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
    correlationId,
    context.Request.Path,
    context.Request.Method);
```

### Correlation ID Flow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Client    │────►│  Middleware │────►│    Logs     │
│             │     │             │     │             │
│ (receives   │◄────│ (generates  │     │ (includes   │
│  corrId in  │     │  corrId)    │     │  corrId)    │
│  response)  │     │             │     │             │
└─────────────┘     └─────────────┘     └─────────────┘
```

---

## Security Considerations

| Concern | Mitigation |
|---------|------------|
| **Information Disclosure** | Production responses contain only generic messages; details logged server-side |
| **Stack Trace Exposure** | `ErrorDetails` only populated when `IsDevelopment() == true` |
| **Sensitive Data in Messages** | Domain exceptions should not include PII in messages |
| **Correlation ID Predictability** | Uses `HttpContext.TraceIdentifier` (unpredictable) |

---

## Future Enhancements

| Enhancement | Description | Priority |
|-------------|-------------|----------|
| **FluentValidation Integration** | Catch `ValidationException` and return field-level errors | Medium |
| **ProblemDetails (RFC 7807)** | Standardized error format for API consumers | Medium |
| **OpenTelemetry Integration** | Distributed tracing spans for exceptions | Low |
| **Exception Rate Metrics** | Track exception counts by type for alerting | Low |

---

## Related Documentation

| Document | Location | Description |
|----------|----------|-------------|
| **Domain Exception Handling** | `SmartMenuOptim.Domain/docs/08-Exceptions/DOMAIN_EXCEPTION_HANDLING.md` | Exception hierarchy and design decisions |
| **DomainException** | `SmartMenuOptim.Domain/Exceptions/DomainException.cs` | Base exception class |
| **EntityNotFoundException** | `SmartMenuOptim.Domain/Exceptions/EntityNotFoundException.cs` | Entity lookup failure exception |

---

## Quick Reference

### Throwing Domain Exceptions

```csharp
// Entity not found (returns 404)
throw new EntityNotFoundException("Order", orderId);
throw new EntityNotFoundException("Dish", dishId, "The dish may have been removed.");

// Business rule violation (returns 422)
throw new DomainException("Cannot place an order without items.");
throw new OrderDomainException("Order is already completed and cannot be modified.");
```

### Adding New Exception Types

1. Create exception class in `SmartMenuOptim.Domain/Exceptions/`
2. Inherit from `DomainException` (or `EntityNotFoundException` for lookup failures)
3. Add case to `GetErrorResponse()` switch if custom HTTP status needed
4. Update documentation

---

*Last Updated: 2026-02-24 | Status: Production-Ready*
