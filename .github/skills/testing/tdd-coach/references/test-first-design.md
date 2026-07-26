# Test-First Design — Tests Driving Design Decisions

## Purpose

Explain how writing tests first naturally drives better software design through emergent architecture, dependency injection, and clear interfaces.

## How Tests Drive Design

### 1. Tests Force Constructor Injection (DIP)

If a class is hard to test, it's poorly designed. Tests naturally push you toward dependency injection:

```csharp
// ❌ HARD TO TEST — creates its own dependency
public sealed class EscrowService
{
    public async Task<Result> Fund(EscrowTransactionId id, Money amount)
    {
        var gateway = new StripePaymentGateway(); // Can't mock this!
        return await gateway.ProcessAsync(amount);
    }
}

// ✅ EASY TO TEST — dependency injected (test drove this design)
public sealed class EscrowService(IPaymentGateway gateway)
{
    public async Task<Result> Fund(EscrowTransactionId id, Money amount)
        => await gateway.ProcessAsync(amount);
}
```

### 2. Tests Drive Small, Focused Interfaces (ISP)

When you mock a large interface, you realize most methods are irrelevant to the test:

```csharp
// ❌ Test reveals: we only need 1 method but mock has 15
var bigRepoMock = new Mock<IEverythingRepository>(); // 15 methods to mock

// ✅ Test drives interface segregation
var escrowRepoMock = new Mock<IEscrowRepository>(); // Only escrow methods
```

### 3. Tests Drive Single Responsibility (SRP)

When a test class grows too many tests, the production class is doing too much:

```markdown
EscrowServiceTests.cs — 47 tests ← RED FLAG
├── 12 tests for creation
├── 15 tests for payment processing
├── 10 tests for dispute resolution
└── 10 tests for notifications

→ Split into:
  CreateEscrowHandler (12 tests)
  ProcessPaymentHandler (15 tests)
  ResolveDisputeHandler (10 tests)
  NotificationService (10 tests)
```

### 4. Tests Drive Clear Return Types

If asserting on a method's result is awkward, the return type needs redesign:

```csharp
// ❌ HARD TO ASSERT — throws exception or returns void
public void ProcessPayment(Money amount) { ... }
// Test: How do we know it worked? Check side effects? Catch exception?

// ✅ EASY TO ASSERT — returns Result object (test drove this design)
public Result<PaymentConfirmation> ProcessPayment(Money amount) { ... }
// Test: result.IsSuccess.Should().BeTrue();
```

## Increment Planning — Breaking Features into Testable Steps

### Strategy: Simplest First, Then Triangulate

```markdown
Feature: Escrow milestone-based release

Increments (ordered by complexity):
1. Empty milestones → release full amount (degenerate case)
2. Single milestone completed → release milestone amount
3. Multiple milestones, one completed → release only that one
4. All milestones completed → release full amount
5. No milestones completed → release nothing
6. Milestone with zero amount → skip it
7. Total milestone amounts exceed escrow amount → error
8. Concurrent milestone completions → no double-release
```

### The Transformation Priority Premise

Guide implementation from simple to complex:

| Priority | Transformation | Example |
|----------|---------------|---------|
| 1 | {} → nil (return nothing) | `return null;` |
| 2 | nil → constant | `return Money.Zero;` |
| 3 | constant → variable | `return amount;` |
| 4 | unconditional → conditional | `if (funded) return amount;` |
| 5 | scalar → collection | `foreach (var milestone in milestones)` |
| 6 | statement → recursion/iteration | `milestones.Where(m => m.IsComplete)` |
| 7 | value → type (polymorphism) | Strategy pattern for different fee types |

## Design Signals from Tests

| Test Signal | Design Action |
|-------------|--------------|
| Test requires many mocks (>3) | Class has too many dependencies — split it |
| Test setup is very long | Consider a Builder for test data |
| Multiple tests test the same condition | Missing abstraction — extract common behavior |
| Test name is hard to write | Method is doing too much — decompose |
| Test needs internal access | Design not exposing right public API |
| Test is flaky | Hidden dependency on time, IO, or state |

## Test-Driven Domain Modeling

Tests help discover domain concepts:

```csharp
// First attempt — primitive obsession
calculator.Calculate(1000m, "standard"); // What's "standard"?

// Test drives Value Object creation
calculator.Calculate(Money.From(1000m), EscrowType.Standard);

// Tests drive domain events
escrow.Fund(amount);
escrow.DomainEvents.Should().ContainSingle()
    .Which.Should().BeOfType<EscrowFundedEvent>();
// → We discovered we need domain events!
```

## When TDD Is Not the Right Approach

- **Exploratory/spike code:** Write throwaway code first, then TDD the real implementation
- **UI layout/styling:** Visual tests are better suited for screenshot comparison
- **Third-party integration:** Use integration tests after the adapter is built
- **Performance optimization:** Profile first, then TDD the optimized path
