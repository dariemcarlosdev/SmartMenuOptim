---
applyTo: "**/*.cs"
---

# Clean Architecture — NexTruzt.io EscrowApp

## Layer Overview

```
Presentation (Components/)
    ↓
Application (Features/)
    ↓
Domain (Models/, Events/, Services/Strategies/ interfaces)
    ↑
Infrastructure (Data/, Infrastructure/)
```

Inner layers **never** reference outer layers. Dependencies always point inward.

---

## Domain Layer

**Namespaces:** `EscrowApp.Models`, `EscrowApp.Events`, `EscrowApp.Services.Strategies`

Contains the core business logic with zero framework dependencies.

| Directory | Contents | Examples |
|---|---|---|
| `Models/` | Entities, value objects, enums | `EscrowTransaction`, `Actor`, `IdentityMapping`, `EscrowStatus` |
| `Events/` | Domain events | `FundsHeldEvent`, `FundsReleasedEvent`, `DisputeRaisedEvent` |
| `Services/Strategies/` | Strategy interfaces | `IFundHoldable`, `IFundReleasable`, `IFundCancellable` |

**Rules:**
- No references to EF Core, ASP.NET, MediatR, or any infrastructure package
- Entities own their invariants — validate state transitions inside the aggregate
- Use `record` types for value objects and domain events
- Strategy interfaces define **what** can happen, not **how**

```csharp
// ✅ Domain — pure business logic
namespace EscrowApp.Models;

public sealed class EscrowTransaction
{
    public Guid Id { get; private set; }
    public EscrowStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public Actor Buyer { get; private set; } = default!;
    public Actor Seller { get; private set; } = default!;

    public void HoldFunds()
    {
        if (Status != EscrowStatus.Pending)
            throw new InvalidOperationException("Funds can only be held on a pending transaction.");

        Status = EscrowStatus.Held;
    }
}
```

---

## Application Layer

**Namespace:** `EscrowApp.Features.Escrow.*` (vertical slices)

Orchestrates use cases via MediatR commands/queries. Depends on Domain; never on Infrastructure.

| Directory | Contents | Examples |
|---|---|---|
| `Features/Escrow/HoldFunds/` | Command, handler, result DTO | `HoldFundsCommand`, `HoldFundsHandler`, `HoldFundsResult` |
| `Features/Escrow/ReleaseFunds/` | Command, handler, result DTO | `ReleaseFundsCommand`, `ReleaseFundsHandler` |
| `Features/Escrow/DisputeFunds/` | Command, handler, result DTO | `DisputeFundsCommand`, `DisputeFundsHandler` |
| `Services/` | Application service interfaces | `IEscrowManagerService` |

**Rules:**
- Inject **interfaces** (`IEscrowTransactionRepository`, `IEventBus`) — never concrete types
- Never reference `EscrowDbContext` or any EF Core type
- Return result DTOs — never expose domain entities to outer layers
- FluentValidation validators live next to their commands

```csharp
// ✅ Application — depends on Domain interfaces only
namespace EscrowApp.Features.Escrow.HoldFunds;

public sealed class HoldFundsHandler(
    IEscrowTransactionRepository repository,
    IEventBus eventBus) : IRequestHandler<HoldFundsCommand, HoldFundsResult>
{
    public async Task<HoldFundsResult> Handle(
        HoldFundsCommand request, CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(request.TransactionId, cancellationToken);
        // ...orchestration logic
    }
}
```

---

## Infrastructure Layer

**Namespaces:** `EscrowApp.Data`, `EscrowApp.Infrastructure`

Implements interfaces defined in Domain and Application. Owns all external concerns.

| Directory | Contents | Examples |
|---|---|---|
| `Data/` | EF Core context, repository implementations, migrations | `EscrowDbContext`, `EscrowTransactionRepository` |
| `Infrastructure/` | External integrations, auth middleware | Stripe payment service, `InMemoryEventBus` |

**Rules:**
- Implements `IEscrowTransactionRepository`, `IEventBus`, strategy implementations
- Stripe SDK usage is confined to this layer
- EF Core configurations (Fluent API) live in `Data/Configurations/`
- Never expose `DbContext` outside this layer

```csharp
// ✅ Infrastructure — implements Domain interface
namespace EscrowApp.Data;

public sealed class EscrowTransactionRepository(EscrowDbContext context)
    : IEscrowTransactionRepository
{
    public async Task<EscrowTransaction?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken) =>
        await context.EscrowTransactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
}
```

---

## Presentation Layer

**Namespace:** `EscrowApp.Components`

Blazor Server pages, layouts, and shared UI components. Depends on Application only.

**Rules:**
- Never inject repositories, `DbContext`, or infrastructure services
- Always go through `IMediator.Send()` or application service interfaces
- Code-behind pattern mandatory (`.razor` + `.razor.cs` + `.razor.css`)
- Use `[CascadingParameter] Task<AuthenticationState>` for auth — not `IHttpContextAccessor`

---

## DI Registration in Program.cs

Register dependencies with interface-to-implementation mapping:

```csharp
// Domain strategy implementations
builder.Services.AddScoped<IFundHoldable, StripeFundHoldStrategy>();
builder.Services.AddScoped<IFundReleasable, StripeFundReleaseStrategy>();
builder.Services.AddScoped<IFundCancellable, StripeFundCancelStrategy>();

// Application services
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<HoldFundsCommand>());
builder.Services.AddScoped<IEscrowManagerService, EscrowManagerService>();

// Infrastructure
builder.Services.AddDbContext<EscrowDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IEscrowTransactionRepository, EscrowTransactionRepository>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
```

---

## Namespace Conventions

| Layer | Namespace Pattern | Example |
|---|---|---|
| Domain | `EscrowApp.Models`, `EscrowApp.Events` | `EscrowApp.Models.EscrowTransaction` |
| Application | `EscrowApp.Features.{Aggregate}.{Action}` | `EscrowApp.Features.Escrow.HoldFunds` |
| Infrastructure | `EscrowApp.Data`, `EscrowApp.Infrastructure` | `EscrowApp.Data.EscrowDbContext` |
| Presentation | `EscrowApp.Components.Pages` | `EscrowApp.Components.Pages.Dashboard` |

---

## Anti-Patterns — What NOT to Do

```csharp
// ❌ Domain referencing Infrastructure
namespace EscrowApp.Models;
using EscrowApp.Data; // VIOLATION — Domain must not know about EF Core

// ❌ Injecting DbContext in Application layer
public sealed class HoldFundsHandler(EscrowDbContext context) // VIOLATION — use IRepository
    : IRequestHandler<HoldFundsCommand, HoldFundsResult> { }

// ❌ Blazor component calling repository directly
@inject IEscrowTransactionRepository Repository  // VIOLATION — use IMediator

// ❌ Returning domain entities from handlers to Presentation
return transaction; // VIOLATION — map to a result DTO

// ❌ Infrastructure types leaking into Application interfaces
public interface IEscrowManagerService
{
    Task<DbSet<EscrowTransaction>> GetAll(); // VIOLATION — DbSet is EF Core
}
```

---

## Reference

See `docs/` directory (files 00–09) for full architecture documentation and decision records.
