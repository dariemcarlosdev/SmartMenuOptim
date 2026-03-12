# Design Patterns Index

> **SmartMenuOptim.Server - Blazor Server Patterns**
> 
> This directory contains documentation for all design patterns applied in the Blazor Server project.

---

## Pattern Catalog

| Pattern | Type | Purpose | Key Files |
|---------|------|---------|-----------|
| [State Container](./STATE_CONTAINER_PATTERN.md) | Behavioral | Centralized state management | `ComponentStateBase.cs`, `*State.cs` |
| [State Container — Prompt Guide](./STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md) | Behavioral | Reusable implementation templates | All layers |
| [Code-Behind](./CODE_BEHIND_PATTERN.md) | Structural | Separation of markup and logic | `.razor` + `.razor.cs` |
| [Client Service Adapter](./CLIENT_SERVICE_ADAPTER_PATTERN.md) | Structural | HTTP API abstraction | `I*ClientService.cs`, `*ClientService.cs` |
| [Response/Result Pattern](./RESPONSE_RESULT_PATTERN.md) | Cross-Layer | Standardized responses across layers | `DomainResult`, `Result`, `ClientResult` |
| [Reusable UI Components](./REUSABLE_UI_COMPONENTS_PATTERN.md) | Structural | DRY UI patterns | `Components/Shared/*.razor` |
| [Observer](./OBSERVER_PATTERN.md) | Behavioral | State change notifications | `OnStateChanged` event |

---

## Pattern Relationships

```
┌─────────────────────────────────────────────────────────────────────┐
│                         BLAZOR COMPONENT                             │
│                     (Code-Behind Pattern)                            │
│  ┌─────────────────┐         ┌─────────────────────────────────┐    │
│  │ .razor (Markup) │         │ .razor.cs (Logic)               │    │
│  │                 │         │                                 │    │
│  │ Uses Reusable   │         │ Subscribes to State             │    │
│  │ UI Components   │         │ (Observer Pattern)              │    │
│  └────────┬────────┘         └────────────────┬────────────────┘    │
│           │                                   │                      │
└───────────┼───────────────────────────────────┼──────────────────────┘
            │                                   │
            │ Renders                           │ Injects
            ▼                                   ▼
┌─────────────────────────┐         ┌─────────────────────────────────┐
│   SHARED COMPONENTS     │         │      STATE CONTAINER            │
│  (Reusable UI Pattern)  │         │   (State Container Pattern)     │
│                         │         │                                 │
│ • LoadingSpinner        │         │ • ComponentStateBase<T>         │
│ • ErrorAlert            │         │ • OnStateChanged event          │
│ • DetailCard            │         │ • SetLoading/SetData/SetError   │
│ • StatItem              │         │                                 │
└─────────────────────────┘         └────────────────┬────────────────┘
                                                     │
                                                     │ Uses
                                                     ▼
                                    ┌─────────────────────────────────┐
                                    │       CLIENT SERVICE            │
                                    │   (Adapter Pattern)             │
                                    │                                 │
                                    │ • I*ClientService interface     │
                                    │ • HTTP calls to API             │
                                    │ • Returns ClientResult<T>       │
                                    │   (Response Pattern)            │
                                    └────────────────┬────────────────┘
                                                     │
                                                     │ Calls
                                                     ▼
                                    ┌─────────────────────────────────┐
                                    │         BACKEND API             │
                                    │                                 │
                                    │ • REST Endpoints                │
                                    │ • Returns JSON/ProblemDetails   │
                                    └─────────────────────────────────┘
```

---

## Quick Reference

### Creating a New Feature

1. **Create Client Service** ([Guide](./CLIENT_SERVICE_ADAPTER_PATTERN.md))
   ```
   Services/Interfaces/I{Entity}ClientService.cs
   Services/{Entity}ClientService.cs
   ```

2. **Create State Container** ([Guide](./STATE_CONTAINER_PATTERN.md))
   ```
   State/{Entity}DetailState.cs
   State/{Entity}ListState.cs
   ```

3. **Create Component** ([Guide](./CODE_BEHIND_PATTERN.md))
   ```
   Components/Pages/{Entity}/{Entity}Detail.razor
   Components/Pages/{Entity}/{Entity}Detail.razor.cs
   ```

4. **Use Shared Components** ([Guide](./REUSABLE_UI_COMPONENTS_PATTERN.md))
   ```razor
   <LoadingSpinner IsLoading="_loading" />
   <ErrorAlert Message="@_error" />
   <DetailCard HeaderTitle="Info">...</DetailCard>
   ```

5. **Register Services**
   ```csharp
   services.AddScoped<I{Entity}ClientService, {Entity}ClientService>();
   services.AddScoped<{Entity}DetailState>();
   ```

---

## Pattern Selection Guide

| Scenario | Pattern(s) to Use |
|----------|-------------------|
| Page with API data | State Container + Client Service + Code-Behind |
| Reusable UI element | Reusable UI Components |
| Service returning data | Result Pattern |
| HTTP API calls | Client Service Adapter |
| Complex component | Code-Behind |
| State change updates | Observer (via State Container) |

---

## Comprehensive Pattern Document

For a complete guide combining all patterns with AI prompts:

📄 **[BLAZOR_COMPONENT_CLEAN_ARCHITECTURE_PATTERN.md](./BLAZOR_COMPONENT_CLEAN_ARCHITECTURE_PATTERN.md)**

---

## File Structure

```
docs/08-Patterns/
├── README.md                                    # This file
├── BLAZOR_COMPONENT_CLEAN_ARCHITECTURE_PATTERN.md  # Comprehensive guide
├── STATE_CONTAINER_PATTERN.md                   # State management
├── CODE_BEHIND_PATTERN.md                       # Markup/logic separation
├── CLIENT_SERVICE_ADAPTER_PATTERN.md            # HTTP abstraction
├── RESPONSE_RESULT_PATTERN.md                   # Error handling & cross-layer responses
├── REUSABLE_UI_COMPONENTS_PATTERN.md            # Shared components
└── OBSERVER_PATTERN.md                          # Change notifications
```

---

## Related Documentation

- [Domain Services](../04-DomainServices/)
- [Feature Implementation Guides](../07-Features/)
- [API Documentation](../05-API/)

---

*Document Version: 1.0*  
*Last Updated: 2025-03-01*
