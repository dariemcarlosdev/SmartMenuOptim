# Coupling Analysis — Measuring Module Dependencies

## Purpose

Measure and evaluate coupling between modules to identify change-magnets, fragile dependencies, and circular references.

## Key Metrics

### Afferent Coupling (Ca) — "Who depends on me?"

High Ca = module is heavily used = should be **stable** (hard to change safely).

### Efferent Coupling (Ce) — "What do I depend on?"

High Ce = module depends on many others = **unstable** (affected by others' changes).

### Instability Ratio: I = Ce / (Ca + Ce)

| Instability | Meaning | Guidance |
|-------------|---------|----------|
| I = 0.0 | Maximally stable | Domain layer — many dependents, few dependencies |
| I = 0.5 | Balanced | Application layer — moderate both ways |
| I = 1.0 | Maximally unstable | Presentation — depends on many, few depend on it |

**Rule:** Dependencies should flow from unstable → stable (high I → low I).

### Abstractness: A = abstract types / total types

| Layer | Expected A | Expected I |
|-------|-----------|-----------|
| Domain | 0.3–0.5 | 0.0–0.2 |
| Application | 0.4–0.6 | 0.3–0.5 |
| Infrastructure | 0.1–0.3 | 0.7–1.0 |
| Presentation | 0.0–0.2 | 0.8–1.0 |

### Distance from Main Sequence: D = |A + I − 1|

- D close to 0 = well-balanced
- D > 0.5 = **Zone of Pain** (too concrete and stable) or **Zone of Uselessness** (too abstract and unstable)

## Analysis Technique

```csharp
// Step 1: Count project references per .csproj
// Ca = number of OTHER projects that reference THIS project
// Ce = number of projects THIS project references

// Example for EscrowApp.Application:
// Ca = 2 (Infrastructure and Web reference it)
// Ce = 1 (references Domain)
// I = 1 / (2 + 1) = 0.33 ✅ (appropriately stable for Application)
```

### Detection Commands

```bash
# List all project references in solution
dotnet list EscrowApp.sln reference

# Per-project analysis
dotnet list src/EscrowApp.Domain/EscrowApp.Domain.csproj reference
dotnet list src/EscrowApp.Application/EscrowApp.Application.csproj reference

# Find namespace-level coupling via using statements
grep -rn "using EscrowApp\." src/EscrowApp.Application/ | \
  sed 's/.*using \(EscrowApp\.[^;]*\).*/\1/' | sort | uniq -c | sort -rn
```

## Circular Dependency Detection

Circular dependencies are **always CRITICAL** — they prevent independent deployment and testing.

```
# Circular reference example:
EscrowApp.Application → EscrowApp.Infrastructure  (violation!)
EscrowApp.Infrastructure → EscrowApp.Application  (correct)
# Result: CIRCULAR — neither can compile without the other
```

### Common Circular Dependency Patterns

| Pattern | Fix |
|---------|-----|
| Application ↔ Infrastructure | Extract interface to Application; implement in Infrastructure |
| Domain ↔ Application | Move shared types to Domain; Application depends on Domain only |
| Service A ↔ Service B | Introduce mediator or domain events to decouple |

### Breaking Cycles with DIP

```csharp
// BEFORE: Application directly depends on Infrastructure
// Application/Services/EscrowService.cs
using EscrowApp.Infrastructure.PaymentProviders; // ❌ Circular risk

// AFTER: Interface in Application, implementation in Infrastructure
// Application/Interfaces/IPaymentGateway.cs
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(Money amount, CancellationToken ct);
}

// Infrastructure/PaymentProviders/StripePaymentGateway.cs
internal sealed class StripePaymentGateway : IPaymentGateway { /* ... */ }
```

## Coupling Report Format

```markdown
| Module | Ca | Ce | I (Ce/(Ca+Ce)) | A | D | Assessment |
|--------|----|----|----------------|---|---|------------|
| Domain | 3 | 0 | 0.00 | 0.40 | 0.40 | ✅ Stable core |
| Application | 2 | 1 | 0.33 | 0.50 | 0.17 | ✅ Balanced |
| Infrastructure | 0 | 2 | 1.00 | 0.10 | 0.10 | ✅ Unstable (correct) |
| Web | 0 | 2 | 1.00 | 0.05 | 0.05 | ✅ Unstable (correct) |
```

## Red Flags

- **God Module:** Ca > 5 AND Ce > 5 — does too much, depended on by too many
- **Unstable Foundation:** Domain with I > 0.3 — core is too dependent on externals
- **Hidden Coupling:** Shared static state, service locator, or ambient context
- **Temporal Coupling:** Methods that must be called in specific order without compiler enforcement
