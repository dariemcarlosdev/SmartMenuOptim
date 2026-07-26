# Architecture Fitness Functions

## Purpose

Define and evaluate measurable fitness functions that quantify architectural health, enabling automated governance and trend tracking over time.

## What Are Fitness Functions?

Fitness functions are objective, automatable tests that evaluate whether architecture characteristics are maintained. They turn subjective "is our architecture good?" into measurable assertions.

## Core Fitness Functions for Clean Architecture

### 1. Layer Dependency Compliance (Binary: Pass/Fail)

```csharp
// Automated test that runs in CI
[Fact]
public void Architecture_Should_Follow_Dependency_Rules()
{
    var domain = typeof(EscrowTransaction).Assembly;
    var application = typeof(CreateEscrowCommand).Assembly;
    
    // Domain depends on nothing
    domain.GetReferencedAssemblies()
        .Should().NotContain(a => a.Name!.StartsWith("EscrowApp."));
    
    // Application depends only on Domain
    var appRefs = application.GetReferencedAssemblies()
        .Where(a => a.Name!.StartsWith("EscrowApp."));
    appRefs.Should().OnlyContain(a => a.Name == "EscrowApp.Domain");
}
```

### 2. Coupling Score (Numeric: 0–1)

```markdown
Score = Average Instability Distance from Expected

| Layer          | Expected I | Actual I | Delta |
|----------------|-----------|----------|-------|
| Domain         | 0.0       | 0.0      | 0.0   |
| Application    | 0.3       | 0.33     | 0.03  |
| Infrastructure | 0.9       | 1.0      | 0.1   |

Coupling Fitness = 1 - Average(Deltas) = 1 - 0.043 = 0.957 ✅
Threshold: > 0.8 = Healthy
```

### 3. Abstraction Balance (Numeric)

Measures whether each layer has the right ratio of abstract to concrete types:

```csharp
// Count interfaces + abstract classes vs total types per assembly
static double CalculateAbstractness(Assembly assembly)
{
    var types = assembly.GetTypes().Where(t => t.IsPublic).ToList();
    var abstractTypes = types.Count(t => t.IsInterface || t.IsAbstract);
    return types.Count == 0 ? 0 : (double)abstractTypes / types.Count;
}
```

| Layer | Min A | Max A | Rationale |
|-------|-------|-------|-----------|
| Domain | 0.2 | 0.5 | Mix of entities and interfaces |
| Application | 0.3 | 0.7 | Commands, queries, interfaces |
| Infrastructure | 0.0 | 0.3 | Mostly concrete implementations |

### 4. Circular Dependency Count (Numeric: Target = 0)

```bash
# Use dotnet tools to detect cycles
dotnet list EscrowApp.sln reference | \
  awk '/Project/ {proj=$2} /->/ {print proj, $0}' | \
  sort | uniq
# Then check for A→B and B→A patterns
```

**Threshold:** Must be exactly 0. Any circular dependency is a CRITICAL finding.

### 5. Component Size Balance

```markdown
Score = 1 - (StandardDeviation(FileCounts) / Mean(FileCounts))

| Project | Files | Lines of Code |
|---------|-------|--------------|
| Domain | 25 | 1,200 |
| Application | 45 | 3,500 |
| Infrastructure | 30 | 2,100 |
| Web | 60 | 5,800 |

If one project has 10x the files of another, it may need splitting.
Threshold: No single project > 40% of total LOC.
```

## Composite Health Score

```markdown
Overall Architecture Health = Weighted Average of Fitness Functions

| Function | Weight | Score | Weighted |
|----------|--------|-------|----------|
| Layer Compliance | 0.30 | 1.00 | 0.300 |
| Coupling Score | 0.25 | 0.96 | 0.240 |
| Abstraction Balance | 0.15 | 0.85 | 0.128 |
| Circular Deps | 0.20 | 1.00 | 0.200 |
| Size Balance | 0.10 | 0.90 | 0.090 |
| **Total** | **1.00** | | **0.958** |

Rating: 🟢 Healthy (> 0.85)
        🟡 Needs Attention (0.65–0.85)
        🔴 Critical (< 0.65)
```

## Trend Tracking

Track fitness scores over time to detect architectural erosion:

```markdown
| Date | Layer | Coupling | Circular | Abstraction | Overall |
|------|-------|----------|----------|-------------|---------|
| Q1 | 1.00 | 0.96 | 0 | 0.85 | 0.96 |
| Q2 | 1.00 | 0.92 | 0 | 0.82 | 0.93 |
| Q3 | 0.95 | 0.88 | 1 | 0.78 | 0.85 ⚠️ |

Trend: ↓ Declining — investigate coupling increase and layer violation
```

## CI Integration

```yaml
# .github/workflows/architecture-fitness.yml
- name: Run Architecture Fitness Tests
  run: dotnet test --filter "Category=Architecture" --logger "trx"
```

Tag architecture tests with `[Trait("Category", "Architecture")]` to run them separately in CI.
