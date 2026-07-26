# Red-Green-Refactor — TDD Cycle Walkthrough

## Purpose

Detailed guidance for each phase of the TDD cycle with .NET/C# examples from the NexTruzt.io escrow domain.

## Phase 1: 🔴 RED — Write a Failing Test

### Rules

- Write **exactly one** test that describes the next increment of behavior
- The test **must fail** when run — if it passes, it's not adding value
- The test should fail with an **assertion error**, not a compilation error
- Name it as a behavior specification: `{Method}_When{Condition}_Should{Expected}`

### Example

```csharp
[Fact]
public void CalculateFee_WhenStandardEscrow_ShouldReturn2Point5Percent()
{
    // Arrange
    var calculator = new FeeCalculator();

    // Act
    var fee = calculator.Calculate(EscrowType.Standard, Money.From(1000m));

    // Assert
    fee.Should().Be(Money.From(25m));
}
```

**At this point:** `FeeCalculator` class doesn't exist. Create just enough to compile (empty class, method returning default).

```csharp
// Just enough to compile — NOT to pass
public sealed class FeeCalculator
{
    public Money Calculate(EscrowType type, Money amount)
        => throw new NotImplementedException();
}
```

**Run test → ❌ Fails with NotImplementedException.** This is acceptable — the test is red.

### Common Mistakes in RED

- Writing test that passes immediately (test is not testing new behavior)
- Writing multiple tests at once
- Writing the production code before the test
- Test that won't compile (should compile but fail at assertion)

## Phase 2: 🟢 GREEN — Minimal Code to Pass

### Rules

- Write the **absolute minimum** code to make the test pass
- Hardcoding is acceptable and expected
- Do NOT write code "for the future"
- Run **all** tests — new test passes AND previous tests still pass

### Example

```csharp
public sealed class FeeCalculator
{
    public Money Calculate(EscrowType type, Money amount)
        => Money.From(amount.Value * 0.025m); // Simplest thing that works
}
```

**Run all tests → ✅ (1 passed, 0 failed)**

### When to Hardcode vs. Generalize

| Situation | Approach |
|-----------|----------|
| First test for a method | Hardcode the return value |
| Second test with different input | Simple conditional |
| Third test reveals a pattern | Generalize (extract formula/algorithm) |

This is called **Triangulation** — use multiple tests to drive out the general solution.

## Phase 3: 🔵 REFACTOR — Clean Without Changing Behavior

### Rules

- All tests must pass **before** and **after** refactoring
- Refactor both production code AND test code
- Run tests after **each** refactoring step (not just at the end)
- If no refactoring is needed, skip and start next cycle

### Common Refactorings

```csharp
// BEFORE — duplication in test setup
[Fact] public void Test1() { var calc = new FeeCalculator(); /* ... */ }
[Fact] public void Test2() { var calc = new FeeCalculator(); /* ... */ }

// AFTER — extract to field
private readonly FeeCalculator _sut = new();
[Fact] public void Test1() { /* use _sut */ }
[Fact] public void Test2() { /* use _sut */ }
```

```csharp
// BEFORE — magic numbers in production code
return Money.From(amount.Value * 0.025m);

// AFTER — named constant
private const decimal StandardFeeRate = 0.025m;
return Money.From(amount.Value * StandardFeeRate);
```

## Complete Multi-Cycle Example

```markdown
## Feature: Escrow Fee Calculator

### Cycle 1: Standard escrow fee
🔴 Test: Standard escrow → 2.5%
🟢 return amount * 0.025m (hardcoded)
🔵 Extract constant

### Cycle 2: Premium escrow fee
🔴 Test: Premium escrow → 1.5%
🟢 if (type == Premium) return amount * 0.015m; else return amount * 0.025m;
🔵 No refactoring yet

### Cycle 3: Enterprise escrow fee
🔴 Test: Enterprise escrow → 1.0%
🟢 switch statement with 3 cases
🔵 Extract fee rates to dictionary/configuration

### Cycle 4: Invalid escrow type
🔴 Test: Unknown type → throws ArgumentOutOfRangeException
🟢 Add default case throwing exception
🔵 Consider Strategy pattern (YAGNI — 3 types might not warrant it)
```

## Timing Guide

| Cycle Phase | Target Time | If Exceeding |
|-------------|-------------|-------------|
| 🔴 RED | 1–3 minutes | Test scope too large — break it down |
| 🟢 GREEN | 1–5 minutes | Implementation too ambitious — simplify |
| 🔵 REFACTOR | 1–5 minutes | Refactoring too aggressive — smaller steps |
| Full cycle | 2–10 minutes | Step is too big — decompose further |
