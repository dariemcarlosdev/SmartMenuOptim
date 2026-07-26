# Dependency Mapping Guide

How to trace and document all dependencies before refactoring to accurately assess blast radius.

## Mapping Process

### Step 1: Direct Dependencies (What the target uses)

Scan the target class for:
- Constructor parameters (DI injections)
- Method parameters and return types
- `using` statements for referenced namespaces
- Base classes and implemented interfaces
- Configuration classes accessed via `IOptions<T>`

```csharp
// Example: Reading dependencies from constructor
public sealed class EscrowService : IEscrowService
{
    // Direct dependencies (all injected)
    private readonly IEscrowRepository _repository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IFeeCalculator _feeCalculator;
    private readonly ILogger<EscrowService> _logger;
    private readonly IOptions<EscrowSettings> _settings;
}
```

### Step 2: Reverse Dependencies (What uses the target)

Search the codebase for references to the target:

```bash
# Find all files referencing EscrowService
grep -rn "EscrowService\|IEscrowService" --include="*.cs" src/

# Find DI registration
grep -rn "AddScoped.*EscrowService\|AddTransient.*EscrowService" --include="*.cs" src/

# Find test references
grep -rn "EscrowService" --include="*.cs" tests/
```

### Step 3: Transitive Dependencies

Trace the dependency chain one level deeper:
- If `EscrowService` uses `IPaymentGateway`, what does `PaymentGateway` depend on?
- If `OrderController` calls `EscrowService`, what calls `OrderController`?

Only trace transitive deps that could be affected by the refactoring.

### Step 4: Interface Contracts

Document the public API surface:

```text
IEscrowService
├── CreateAsync(CreateEscrowCommand, CancellationToken) → Result<EscrowId>
├── FundAsync(FundEscrowCommand, CancellationToken) → Result
├── ReleaseAsync(ReleaseEscrowCommand, CancellationToken) → Result
└── GetByIdAsync(EscrowId, CancellationToken) → EscrowDto?

Callers depend on these signatures — changing them is a breaking change.
```

### Step 5: Configuration Dependencies

Document DI registrations, middleware, config bindings:

```csharp
// DI Registration (in Program.cs or extension method)
services.AddScoped<IEscrowService, EscrowService>();
services.Configure<EscrowSettings>(config.GetSection("Escrow"));

// Middleware references
app.UseMiddleware<EscrowAuditMiddleware>();

// Config shape (appsettings.json)
"Escrow": {
    "MaxAmount": 1000000,
    "TimeoutDays": 30,
    "FeeRate": 0.025
}
```

### Step 6: Test Dependencies

```text
Tests exercising EscrowService:
├── EscrowServiceTests (15 unit tests)
│   ├── CreateAsync_ValidCommand_ReturnsEscrowId
│   ├── CreateAsync_InvalidAmount_ReturnsError
│   └── ... (13 more)
├── EscrowIntegrationTests (6 tests)
│   ├── CreateAndFund_EndToEnd
│   └── ... (5 more)
└── EscrowApiTests (4 acceptance tests)
```

## Dependency Summary Template

```text
Target: [ClassName]
├── Direct deps: [injected interfaces and types]
├── Reverse deps: [classes that reference target]
├── Transitive: [affected indirect dependencies]
├── Interface: [public contract: method count and signatures]
├── DI Registration: [how registered, lifetime]
├── Config: [IOptions<T> bindings]
├── Middleware: [any middleware dependencies]
└── Tests: [test classes and counts]
```

## Blast Radius Calculation

Count affected files per category:

| Category | Count | Weight |
|----------|-------|--------|
| Target files modified | N | ×1 |
| DI registration changes | N | ×1 |
| Test files requiring updates | N | ×0.5 |
| Config file changes | N | ×1 |
| Public API signature changes | N | ×3 (breaking!) |

**Total weighted score:**
- < 5 → 🟢 Contained
- 5-15 → 🟡 Moderate
- > 15 → 🔴 Wide

## Tips for .NET Projects

- Use **Solution Explorer** or `dotnet build` with warnings to find unused references
- Check `*.csproj` `<ProjectReference>` for cross-project dependencies
- Search for `nameof(EscrowService)` — string references won't show in IDE "Find References"
- Check AutoMapper/Mapster profiles that may reference the target types
- Check FluentValidation validators bound to command/query types
- Check MediatR pipeline behaviors that may be generic but apply to target handlers
