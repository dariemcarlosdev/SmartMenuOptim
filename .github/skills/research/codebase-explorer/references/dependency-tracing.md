# Dependency Tracing

Patterns for tracing execution flows and dependency relationships.

## Project Reference Graph

### Extract from .csproj Files

```bash
# List all project references
grep -rn "ProjectReference" --include="*.csproj" | \
  sed 's/.*Include="\(.*\)".*/\1/' | sort

# Build adjacency list
# For each .csproj, list what it references
for f in $(find . -name "*.csproj"); do
  echo "=== $(basename $f .csproj) ==="
  grep "ProjectReference" "$f" | sed 's/.*\\//' | sed 's/".*//'
done
```

### ASCII Dependency Diagram

```
┌─────────────┐     ┌──────────────────┐
│   Web        │────▶│   Application     │
└─────────────┘     └──────────────────┘
                              │
┌─────────────┐              ▼
│Infrastructure│────▶┌──────────────────┐
└─────────────┘     │     Domain        │
                    └──────────────────┘

Arrow = "depends on" (has ProjectReference to)
```

## Request Flow Tracing

### MediatR Command Flow (Clean Architecture)

```
1. HTTP Request → Controller/MinimalAPI endpoint
2. Controller maps DTO → Command/Query object
3. mediator.Send(command)
4. Pipeline Behaviors execute (validation, logging, transaction)
5. Handler processes command using domain services + repositories
6. Repository interacts with DbContext
7. Response flows back through pipeline → Controller → HTTP Response
```

### Trace a Specific Feature

```bash
# 1. Find the API endpoint
grep -rn "MapPost\|MapGet\|HttpPost\|HttpGet" --include="*.cs" | grep -i "escrow"

# 2. Find the MediatR command/query
grep -rn "class.*Command\|class.*Query" --include="*.cs" | grep -i "escrow"

# 3. Find the handler
grep -rn "IRequestHandler.*Escrow" --include="*.cs"

# 4. Find repository usage in handler
grep -rn "IEscrowRepository\|_escrowRepository" --include="*.cs"

# 5. Find EF Core implementation
grep -rn "class.*EscrowRepository\|: IEscrowRepository" --include="*.cs"
```

## Package Dependency Analysis

### Categorize NuGet Packages

```bash
# Extract all PackageReference entries
grep -rn "PackageReference" --include="*.csproj" | \
  sed 's/.*Include="\([^"]*\)".*/\1/' | sort -u
```

| Category | Package Pattern | Signal |
|----------|----------------|--------|
| ORM | `Microsoft.EntityFrameworkCore.*` | EF Core data access |
| CQRS | `MediatR` | Command/Query separation |
| Validation | `FluentValidation.*` | Input validation layer |
| Auth | `Microsoft.Identity.Web` | Entra ID integration |
| Resilience | `Polly`, `Microsoft.Extensions.Http.Resilience` | Retry/circuit breaker |
| Testing | `xunit`, `Moq`, `FluentAssertions` | Test framework |
| Mapping | `AutoMapper`, `Mapster` | Object mapping |
| Logging | `Serilog.*` | Structured logging |

## Circular Dependency Detection

```bash
# Quick check: does A reference B AND B reference A?
# Build reference pairs and look for cycles
for f in $(find . -name "*.csproj"); do
  proj=$(basename $f .csproj)
  grep "ProjectReference" "$f" 2>/dev/null | \
    sed "s/.*\\\\\(.*\)\.csproj.*/  $proj -> \1/"
done
```

### Common Circular Dependency Patterns

| Pattern | Problem | Fix |
|---------|---------|-----|
| Domain ↔ Infrastructure | Domain depends on EF Core | Extract interfaces to Domain |
| Application ↔ Web | Shared DTOs | Move DTOs to Application |
| Service A ↔ Service B | Mutual calls | Extract shared contract |

## Coupling Metrics

### Fan-In / Fan-Out

```bash
# Fan-out: How many types does this file depend on?
grep -c "^using " src/Application/Handlers/CreateEscrowHandler.cs

# Fan-in: How many files depend on this type?
grep -rl "IEscrowRepository" --include="*.cs" | wc -l
```

| Metric | Healthy | Concerning | Critical |
|--------|---------|------------|----------|
| Fan-out per file | < 10 | 10-20 | > 20 |
| Fan-in per interface | 1-5 | 5-15 | > 15 |
| Circular references | 0 | 1-2 | > 2 |

## Service Communication Map

For distributed or modular systems:

```bash
# Find HTTP client registrations
grep -rn "AddHttpClient\|HttpClient" --include="*.cs"

# Find message/event publishers
grep -rn "IPublisher\|Publish\|SendAsync" --include="*.cs"

# Find background workers
grep -rn "BackgroundService\|IHostedService" --include="*.cs"
```
