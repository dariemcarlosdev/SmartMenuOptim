# Documentation Mining

Extract documentation and knowledge from code artifacts.

## XML Documentation Extraction

### Harvest Public API Docs

```bash
# Find all XML doc comments on public members
grep -B1 -A5 "/// <summary>" --include="*.cs" -rn

# Extract interface contracts (the most valuable docs)
grep -B2 -A10 "public interface" --include="*.cs" -rn

# Find TODO/HACK/FIXME markers
grep -rn "TODO\|HACK\|FIXME\|WORKAROUND\|BUG" --include="*.cs"
```

### Interface-as-Documentation Pattern

Interfaces in Clean Architecture are the primary specification:

```csharp
// Domain/Interfaces/IEscrowRepository.cs
// This IS the specification for escrow persistence
public interface IEscrowRepository
{
    Task<Escrow?> GetByIdAsync(EscrowId id, CancellationToken ct);
    Task<IReadOnlyList<Escrow>> GetByBuyerAsync(UserId buyerId, CancellationToken ct);
    Task AddAsync(Escrow escrow, CancellationToken ct);
    Task UpdateAsync(Escrow escrow, CancellationToken ct);
}
// Each method signature documents a required capability
```

## Configuration-as-Documentation

### appsettings.json Analysis

```bash
# Read all configuration files
cat appsettings.json appsettings.Development.json 2>/dev/null

# Find IOptions<T> bindings — reveals configuration structure
grep -rn "Configure<\|IOptions<\|IOptionsSnapshot<" --include="*.cs"

# Find environment variable references
grep -rn "GetEnvironmentVariable\|env:" --include="*.cs" --include="*.json"
```

### What Configuration Reveals

| Config Section | Documents |
|---------------|-----------|
| `ConnectionStrings` | Database dependencies |
| `Authentication` / `AzureAd` | Identity provider |
| `Logging` / `Serilog` | Observability setup |
| Feature flags | Toggleable capabilities |
| `Cors` | Allowed client origins |

## Test-as-Documentation

### Extract Business Rules from Tests

```bash
# Test method names document expected behavior
grep -rn "public.*void\|public.*Task.*Test\|Fact\|Theory" \
  --include="*.cs" tests/

# Find test data builders — they document valid states
grep -rn "class.*Builder\|class.*Factory" --include="*.cs" tests/

# Find assertion patterns — they document invariants
grep -rn "Should\|Assert\|Expect" --include="*.cs" tests/
```

### Test Name Convention Mapping

```
Test: CreateEscrow_WithValidAmount_ShouldSetStatusToPending
  → Business Rule: New escrows start in Pending status
  → Precondition: Amount must be valid

Test: ReleaseEscrow_WhenBothPartiesApprove_ShouldTransferFunds
  → Business Rule: Fund release requires dual approval
  → Trigger: Both buyer and seller approve
```

## Migration-as-Documentation

### EF Core Migrations Tell the Data Story

```bash
# List all migrations in chronological order
find . -path "*/Migrations/*.cs" -not -name "*Designer*" | sort

# Extract schema changes
grep -A20 "protected override void Up" \
  --include="*.cs" -rn src/Infrastructure/Migrations/
```

## README and Docs Inventory

```bash
# Find all documentation files
find . -name "README.md" -o -name "*.md" -o -name "ARCHITECTURE.md" \
  -o -name "CONTRIBUTING.md" -o -name "CHANGELOG.md" | sort

# Find architecture decision records
find . -path "*/adr/*" -o -path "*/decisions/*" | sort

# Find OpenAPI/Swagger specs
find . -name "swagger.json" -o -name "openapi.*" | sort
```

## Knowledge Extraction Checklist

| Source | What It Documents | Priority |
|--------|------------------|----------|
| Domain entities | Core business concepts | Critical |
| Domain interfaces | Required capabilities | Critical |
| MediatR handlers | Use cases / features | High |
| FluentValidation rules | Business constraints | High |
| EF configurations | Data relationships | High |
| Test names | Expected behaviors | Medium |
| appsettings.json | External dependencies | Medium |
| Migrations | Schema evolution | Medium |
| CI/CD workflows | Build/deploy process | Low |
| README files | Project overview | Low |

## Generating Documentation Output

When compiling findings into a report:

```markdown
## Discovered Business Rules

| Rule | Source | Location |
|------|--------|----------|
| {rule description} | {test/validator/entity} | {file:line} |

## API Surface

| Endpoint | Method | Handler | Description |
|----------|--------|---------|-------------|
| /api/escrow | POST | CreateEscrowHandler | {from XML docs or test names} |

## Data Model

| Entity | Key Properties | Relationships |
|--------|---------------|---------------|
| Escrow | Id, Amount, Status | Buyer, Seller, Transaction |
```
