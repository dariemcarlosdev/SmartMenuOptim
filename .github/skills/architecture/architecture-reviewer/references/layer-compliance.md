# Layer Compliance — Clean Architecture Boundaries

## Purpose

Validate that each layer in a Clean Architecture project adheres to its defined responsibilities and dependency constraints.

## Layer Responsibilities (NexTruzt.io Stack)

| Layer | Projects | Allowed Dependencies | Responsibility |
|-------|----------|---------------------|----------------|
| **Domain** | `EscrowApp.Domain` | None (innermost) | Entities, Value Objects, Domain Events, Repository interfaces |
| **Application** | `EscrowApp.Application` | Domain only | CQRS handlers, DTOs, Validators, Application interfaces |
| **Infrastructure** | `EscrowApp.Infrastructure` | Application, Domain | EF Core DbContext, Repository implementations, External services |
| **Presentation** | `EscrowApp.Web` | Application, Domain | Blazor components, API controllers, Middleware |

## Compliance Checks

### 1. Domain Layer Purity

The Domain layer must have **zero** outward dependencies:

```csharp
// ✅ Domain entity — no infrastructure references
namespace EscrowApp.Domain.Entities;

public sealed class EscrowTransaction : AggregateRoot<EscrowTransactionId>
{
    public Money Amount { get; private set; }
    public EscrowStatus Status { get; private set; }

    public void Release(UserId authorizedBy)
    {
        if (Status != EscrowStatus.Funded)
            throw new DomainException("Cannot release unfunded escrow");
        AddDomainEvent(new EscrowReleasedEvent(Id, authorizedBy));
        Status = EscrowStatus.Released;
    }
}
```

**Red flags in Domain layer:**
- `using Microsoft.EntityFrameworkCore;` — EF Core leak
- `using System.Net.Http;` — HTTP client dependency
- `using Microsoft.Extensions.Logging;` — Infrastructure concern
- Any `[JsonProperty]` or serialization attributes

### 2. Application Layer Boundaries

Application may reference Domain but NEVER Infrastructure:

```csharp
// ✅ Application handler — depends only on Domain interfaces
public sealed class FundEscrowHandler : IRequestHandler<FundEscrowCommand, Result>
{
    private readonly IEscrowRepository _escrowRepo;    // Domain interface
    private readonly IPaymentGateway _paymentGateway;  // Application interface
    private readonly IUnitOfWork _unitOfWork;           // Application interface
}

// ❌ Violation — Application referencing Infrastructure
using EscrowApp.Infrastructure.Data; // NEVER DO THIS
```

### 3. Infrastructure Implementation Check

Infrastructure implements interfaces defined in Domain/Application:

```csharp
// ✅ Infrastructure implements Domain interface
namespace EscrowApp.Infrastructure.Persistence;

internal sealed class EscrowRepository : IEscrowRepository
{
    private readonly EscrowDbContext _context;
    public async Task<EscrowTransaction?> GetByIdAsync(
        EscrowTransactionId id, CancellationToken ct)
        => await _context.EscrowTransactions
            .FirstOrDefaultAsync(e => e.Id == id, ct);
}
```

### 4. Presentation Layer Rules

- Blazor components inject Application services, never Infrastructure directly
- API controllers call MediatR, not repositories
- No business logic in components — delegate to Application layer

## Detection Commands

```bash
# Find Domain layer violations (.NET)
grep -rn "using EscrowApp.Infrastructure" src/EscrowApp.Domain/
grep -rn "using EscrowApp.Web" src/EscrowApp.Domain/
grep -rn "using Microsoft.EntityFrameworkCore" src/EscrowApp.Domain/

# Find Application layer violations
grep -rn "using EscrowApp.Infrastructure" src/EscrowApp.Application/

# Verify .csproj references
dotnet list src/EscrowApp.Domain/EscrowApp.Domain.csproj reference
```

## Severity Classification

| Violation | Severity | Example |
|-----------|----------|---------|
| Domain → Infrastructure | CRITICAL | Entity using DbContext |
| Application → Infrastructure | CRITICAL | Handler using concrete repository |
| Presentation → Infrastructure (direct) | WARNING | Component bypassing Application |
| Cross-cutting concern leak | WARNING | Logger in Domain entity |
| Shared kernel misuse | INFO | Utility in wrong layer |
