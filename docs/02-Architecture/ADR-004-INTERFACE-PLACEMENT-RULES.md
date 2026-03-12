# ADR-004: Interface Placement Rules

> **Status**: Accepted  
> **Date**: 2026-03-04  
> **Deciders**: Development Team  
> **Context**: Clean Architecture + DDD + Vertical Slice

---

## Decision

Interfaces are defined in the layer that **needs** the abstraction, not the layer that **implements** it. This follows the Dependency Inversion Principle (DIP).

---

## Rules

### Domain Layer

**Location**: `Domain/Repositories/`, `Domain/Services/Abstractions/`

| Interface | Purpose | Example |
|-----------|---------|---------|
| Repository contracts | Data access abstractions the domain needs | `IRepository<T>`, `IUnityOfWork` |
| Domain service abstractions | Business logic contracts | `ISentimentAnalyzer`, `IMenuCompositionValidator` |

**Rationale**: The domain defines *what* it needs to persist and compute. Infrastructure provides *how*.

### Application Layer

**Location**: `Application/Contracts/`, `Application/Features/*/Services/`

| Interface | Purpose | Example |
|-----------|---------|---------|
| Infrastructure ports | External system abstractions | `ICacheService`, `IExternalPricingApi`, `IEmailService` |
| Application service contracts | Use case orchestration | `IRestaurantService`, `IMenuService`, `ICategoryService` |

**Rationale**: The application layer orchestrates use cases and defines what infrastructure it needs. It should not know *how* caching or external APIs work — only that they exist.

### Presentation Layer

**Location**: `Server/Features/*/Services/`

| Interface | Purpose | Example |
|-----------|---------|---------|
| Client adapters | UI-to-API communication | `IRestaurantClientService` |

**Rationale**: The Blazor Server project adapts backend services for UI consumption via HTTP.

---

## Anti-Patterns

| ❌ Don't | ✅ Do Instead | Why |
|----------|--------------|-----|
| Define `ICacheService` in Domain | Define in `Application/Contracts/` | Domain has no concept of caching |
| Define `IExternalPricingApi` in Domain | Define in `Application/Contracts/` | External APIs are infrastructure concerns |
| Define `IRepository<T>` in Application | Define in `Domain/Repositories/` | Repositories express domain persistence needs |
| Define `IRestaurantService` in Domain | Define in `Application/Features/Restaurants/Services/` | Use case orchestration belongs in Application |

---

## Dependency Flow

```
Presentation  →  Application  →  Domain
    ↓                ↓              ↓
 Defines:         Defines:       Defines:
 IClient*         ICacheService  IRepository<T>
 Service          IExternal*Api  ISentiment*
                  IRestaurant*   IMenuComposition*
                  Service        Validator

                     ↑              ↑
              Infrastructure implements both
```

---

## Related

- [copilot-instructions.md](../../.github/copilot-instructions.md) — Copilot enforces these rules
- [IMPLEMENTATION_GUIDE.md](../07-Features/01-RestaurantManagement/IMPLEMENTATION_GUIDE.md) — Restaurant feature reference
