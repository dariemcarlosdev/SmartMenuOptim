# Coverage Metrics — Line, Branch, and Mutation Coverage

## Purpose

Explain the different types of coverage metrics, their strengths and limitations, and how to interpret them for meaningful quality assessment.

## Coverage Types

### Line Coverage (Statement Coverage)

**What it measures:** Percentage of source code lines executed during tests.

```csharp
public Money CalculateFee(EscrowType type, Money amount) // Line 1 ✅
{                                                          // Line 2 ✅
    if (type == EscrowType.Standard)                      // Line 3 ✅
        return amount * 0.025m;                           // Line 4 ✅
    if (type == EscrowType.Premium)                       // Line 5 ❌
        return amount * 0.015m;                           // Line 6 ❌
    throw new ArgumentOutOfRangeException(nameof(type));  // Line 7 ❌
}
// Line coverage: 4/7 = 57%
```

**Limitations:** A test can execute a line without asserting on its behavior. 100% line coverage ≠ correctness.

### Branch Coverage (Decision Coverage)

**What it measures:** Percentage of control flow branches (if/else, switch, ternary) exercised.

```csharp
// The method above has 3 branches:
// Branch 1: type == Standard → ✅ tested
// Branch 2: type == Premium  → ❌ not tested
// Branch 3: default (throw)  → ❌ not tested
// Branch coverage: 1/3 = 33%
```

**Better than line coverage** because it catches untested decision paths. Two tests can cover all lines but miss a branch.

### Mutation Coverage (Mutation Testing)

**What it measures:** Whether tests actually detect code changes (mutations). A mutation that doesn't cause a test failure is a "surviving mutant" — indicating weak tests.

```csharp
// Original code
return amount * 0.025m;

// Mutant 1: Change operator
return amount + 0.025m;  // Does any test fail? If not → SURVIVING MUTANT

// Mutant 2: Change constant
return amount * 0.050m;  // Does any test fail? If not → SURVIVING MUTANT

// Mutant 3: Remove return
// return amount * 0.025m; // Does any test fail? Should fail!
```

**Best quality indicator** but expensive to compute. Use for critical business logic only.

### .NET Mutation Testing with Stryker

```bash
dotnet tool install --global dotnet-stryker
cd tests/EscrowApp.Application.Tests
dotnet stryker --project EscrowApp.Application.csproj
```

## Coverage Targets for NexTruzt.io

**Do NOT chase 100% — optimize for quality, not quantity.**

| Layer | Line Target | Branch Target | Rationale |
|-------|------------|--------------|-----------|
| Domain (entities, value objects) | 90%+ | 85%+ | Core business rules — must be thoroughly tested |
| Application (handlers, validators) | 85%+ | 80%+ | Orchestration logic with many paths |
| Infrastructure (repos, services) | 70%+ | 60%+ | Integration-heavy — unit tests cover interfaces |
| Presentation (components) | 50%+ | 40%+ | UI testing has diminishing returns |

### What NOT to Cover

- Auto-generated code (migrations, designer files)
- DTOs and records with no logic
- Simple property getters/setters
- Startup/configuration code (test via integration tests)
- Third-party library wrappers with no custom logic

## Interpreting Coverage Reports

### High Coverage, Low Quality

```markdown
Line Coverage: 95% — but:
- 30% of tests have no assertions
- 15% assert only "not null"
- Tests mock everything, no real behavior tested

Actual quality: LOW despite high numbers
```

### Low Coverage, High Quality

```markdown
Line Coverage: 65% — but:
- All critical payment paths covered
- Every test has strong assertions
- Mutation score: 85% on Domain layer

Actual quality: GOOD — focused on what matters
```

## Coverage Gap Analysis Process

```markdown
1. Run coverage tool → identify uncovered lines/branches
2. Filter to business-critical classes only
3. For each uncovered branch:
   a. What condition triggers this branch?
   b. What would break if this branch had a bug?
   c. Is this branch reachable in production?
4. Prioritize by business impact, not by coverage number
5. Generate test stubs for top-priority gaps
```

## Combining Metrics

| Metric | Catches | Misses | Best For |
|--------|---------|--------|----------|
| Line coverage | Dead code, untouched methods | Weak assertions, missing branches | Quick overview |
| Branch coverage | Untested conditions | Weak assertions | Decision-heavy code |
| Mutation coverage | Weak assertions, missing checks | Expensive to compute | Critical business logic |
| Assertion quality | False-confidence tests | Can't find untested code | Test suite health audit |

**Recommendation:** Use line + branch coverage for overview, mutation testing for Domain/Application layers, and assertion quality audit for test suite health.
