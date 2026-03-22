# Design Patterns Index

> **SmartMenuOptimizer - Design Patterns & Architecture Reference**
> 
> This directory contains documentation for all design patterns applied across the project,
> organized into three categories: Blazor/UI, Architecture, and Event-Driven.

---

## Pattern Catalog

### 01 — Blazor & UI Patterns

| Pattern | Type | Purpose | Key Files |
|---------|------|---------|-----------|
| [Blazor Component Clean Architecture](./01-BlazorUI/BLAZOR_COMPONENT_CLEAN_ARCHITECTURE_PATTERN.md) | Comprehensive | Full Blazor component pattern guide | All Blazor layers |
| [State Container — Prompt Guide](./01-BlazorUI/BLAZOR_STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md) | Behavioral | State management templates | `ComponentStateBase.cs`, `*State.cs` |
| [Code-Behind](./01-BlazorUI/CODE_BEHIND_PATTERN.md) | Structural | Separation of markup and logic | `.razor` + `.razor.cs` |
| [Reusable UI Components](./01-BlazorUI/REUSABLE_UI_COMPONENTS_PATTERN.md) | Structural | DRY UI patterns | `Components/Shared/*.razor` |

### 02 — Architecture Patterns

| Pattern | Type | Purpose | Key Files |
|---------|------|---------|-----------|
| **[Reference Implementation Guide](./02-Architecture/REFERENCE_IMPLEMENTATION_GUIDE.md)** | **Cross-Layer** | **Restaurant module as canonical pattern** | **All layers** |
| [Client Service Adapter](./02-Architecture/CLIENT_SERVICE_ADAPTER_PATTERN.md) | Structural | HTTP API abstraction | `I*ClientService.cs`, `*ClientService.cs` |
| [Response/Result Pattern](./02-Architecture/RESPONSE_RESULT_PATTERN.md) | Cross-Layer | Standardized responses across layers | `DomainResult`, `Result`, `ClientResult` |

### 03 — Event-Driven Patterns

| Pattern | Type | Purpose | Key Files |
|---------|------|---------|-----------|
| [Event-Driven Architecture](./03-EventDriven/EVENT_DRIVEN_ARCHITECTURE_PATTERN.md) | Architectural | Domain event publishing & handling | `IHasDomainEvents`, `MediatR` |
| [Observer Deep Dive](./03-EventDriven/OBSERVER_PATTERN_DEEP_DIVE.md) | Behavioral | State change notifications | `OnStateChanged` event |
| [Event-Driven Improvement Tracker](./03-EventDriven/EVENT_DRIVEN_IMPROVEMENT_TRACKER.md) | Tracker | Planned event system enhancements | — |

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

1. **Create Client Service** ([Guide](./02-Architecture/CLIENT_SERVICE_ADAPTER_PATTERN.md))
   ```
   Services/Interfaces/I{Entity}ClientService.cs
   Services/{Entity}ClientService.cs
   ```

2. **Create State Container** ([Guide](./01-BlazorUI/BLAZOR_STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md))
   ```
   State/{Entity}DetailState.cs
   State/{Entity}ListState.cs
   ```

3. **Create Component** ([Guide](./01-BlazorUI/CODE_BEHIND_PATTERN.md))
   ```
   Components/Pages/{Entity}/{Entity}Detail.razor
   Components/Pages/{Entity}/{Entity}Detail.razor.cs
   ```

4. **Use Shared Components** ([Guide](./01-BlazorUI/REUSABLE_UI_COMPONENTS_PATTERN.md))
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

📄 **[BLAZOR_COMPONENT_CLEAN_ARCHITECTURE_PATTERN.md](./01-BlazorUI/BLAZOR_COMPONENT_CLEAN_ARCHITECTURE_PATTERN.md)**

---

## File Structure

```
docs/08-Patterns/
├── README.md                                          # This file
├── 01-BlazorUI/
│   ├── BLAZOR_COMPONENT_CLEAN_ARCHITECTURE_PATTERN.md # Comprehensive Blazor guide
│   ├── BLAZOR_STATE_CONTAINER_PATTERN_PROMPT_GUIDE.md # State management templates
│   ├── CODE_BEHIND_PATTERN.md                         # Markup/logic separation
│   └── REUSABLE_UI_COMPONENTS_PATTERN.md              # Shared components
├── 02-Architecture/
│   ├── REFERENCE_IMPLEMENTATION_GUIDE.md              # Canonical feature pattern
│   ├── CLIENT_SERVICE_ADAPTER_PATTERN.md              # HTTP abstraction
│   └── RESPONSE_RESULT_PATTERN.md                     # Cross-layer responses
└── 03-EventDriven/
    ├── EVENT_DRIVEN_ARCHITECTURE_PATTERN.md            # Domain events guide
    ├── OBSERVER_PATTERN_DEEP_DIVE.md                   # Change notifications
    └── EVENT_DRIVEN_IMPROVEMENT_TRACKER.md             # Enhancement tracker
```

---

## Related Documentation

- [Domain Services](../04-DomainServices/)
- [Feature Implementation Guides](../07-Features/)
- [API Documentation](../05-API/)

---

*Document Version: 2.0*  
*Last Updated: 2026-03-22*
