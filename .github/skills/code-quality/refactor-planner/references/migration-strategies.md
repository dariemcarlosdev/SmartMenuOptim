# Migration Strategies

Safe migration patterns for refactoring .NET codebases without breaking production.

## Strangler Fig Pattern

Gradually replace old implementation with new while keeping both running.

```csharp
// Step 1: Introduce new interface alongside old
public interface IEscrowServiceV2
{
    Task<Result<EscrowId>> CreateAsync(CreateEscrowCommand cmd, CancellationToken ct);
}

// Step 2: Implement new version
public sealed class EscrowServiceV2 : IEscrowServiceV2 { /* clean implementation */ }

// Step 3: Feature flag to switch
services.AddScoped<IEscrowService>(sp =>
    featureFlags.UseNewEscrowService
        ? sp.GetRequiredService<EscrowServiceV2>()
        : sp.GetRequiredService<LegacyEscrowService>());

// Step 4: Monitor, then remove legacy
```

**When to use:** Large class rewrites where incremental extraction is too risky.

## Parallel Change (Expand-Contract)

Make a breaking change in three safe steps.

```csharp
// Step 1: EXPAND — Add new method, keep old
public interface IEscrowRepository
{
    [Obsolete("Use GetByIdAsync(EscrowId) instead")]
    Task<Escrow?> GetByIdAsync(int id, CancellationToken ct);

    // New signature with value object
    Task<Escrow?> GetByIdAsync(EscrowId id, CancellationToken ct);
}

// Step 2: MIGRATE — Update all callers to use new method
// (This can be done incrementally across multiple PRs)

// Step 3: CONTRACT — Remove old method once all callers migrated
```

**When to use:** Changing method signatures on widely-used interfaces.

## Branch by Abstraction

Introduce an abstraction layer to swap implementations safely.

```csharp
// Step 1: Extract interface from existing concrete class
public interface IFeeCalculator
{
    Money Calculate(EscrowType type, Money amount);
}

// Step 2: Existing class implements the interface (no behavior change)
public sealed class LegacyFeeCalculator : IFeeCalculator { /* existing logic */ }

// Step 3: Create new implementation
public sealed class TieredFeeCalculator : IFeeCalculator { /* new logic */ }

// Step 4: Swap in DI
services.AddScoped<IFeeCalculator, TieredFeeCalculator>();
```

**When to use:** Replacing an algorithm or strategy without touching callers.

## Database Schema Migration Safety

When refactoring requires schema changes:

### Additive-Only Migrations (Safe for Zero-Downtime)
```sql
-- ✅ SAFE: Add nullable column
ALTER TABLE Escrows ADD FeeAmount DECIMAL(18,2) NULL;

-- ✅ SAFE: Add new table
CREATE TABLE EscrowFees (Id INT PRIMARY KEY, EscrowId INT, Amount DECIMAL(18,2));

-- ✅ SAFE: Add index
CREATE INDEX IX_Escrows_Status ON Escrows(Status);
```

### Destructive Migrations (Require Coordination)
```sql
-- ❌ UNSAFE without coordination: Drop column
ALTER TABLE Escrows DROP COLUMN LegacyFee;

-- ❌ UNSAFE without coordination: Rename column
EXEC sp_rename 'Escrows.Fee', 'FeeAmount', 'COLUMN';

-- ❌ UNSAFE without coordination: Change column type
ALTER TABLE Escrows ALTER COLUMN Amount DECIMAL(18,4);
```

### Safe Destructive Migration Pattern
1. **Release 1:** Add new column, write to both old and new
2. **Release 2:** Backfill new column from old, switch reads to new
3. **Release 3:** Stop writing to old column
4. **Release 4:** Drop old column

## Feature Flag-Gated Refactoring

```csharp
public sealed class EscrowCommandHandler : IRequestHandler<CreateEscrowCommand, Result>
{
    private readonly IFeatureManager _features;

    public async Task<Result> Handle(CreateEscrowCommand cmd, CancellationToken ct)
    {
        if (await _features.IsEnabledAsync("UseNewEscrowValidation"))
            return await _newValidator.ValidateAsync(cmd, ct);
        else
            return await _legacyValidator.ValidateAsync(cmd, ct);
    }
}
```

## DI Registration Migration Checklist

When refactoring changes DI registrations:

- [ ] Old service still registered (parallel period)
- [ ] New service registered with correct lifetime
- [ ] No captive dependency introduced (Singleton capturing Scoped)
- [ ] `IHttpClientFactory` used for HTTP clients (not raw `HttpClient`)
- [ ] Integration tests pass with new registration
- [ ] Health checks updated if services expose health endpoints
- [ ] Verify no circular dependencies introduced

## Rollback Strategy

Every refactoring step should have a documented rollback:

| Step | Change | Rollback |
|------|--------|----------|
| 1 | Extract `EscrowValidator` | Revert commit, inline methods back |
| 2 | Update DI registrations | Restore old registration line |
| 3 | Remove old code | `git revert` — old code still in history |

**Rule:** If any step cannot be rolled back independently, it must be merged with its dependent step into a single atomic change.
