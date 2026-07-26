# Kata Exercises — TDD Practice with .NET

## Purpose

Provide guided TDD practice exercises (katas) progressing from beginner to advanced, using the NexTruzt.io escrow domain for context.

## Kata 1: Money Value Object (Beginner)

**Goal:** Build an immutable `Money` value object with TDD.

### Increment Plan

1. Create Money from decimal → stores value
2. Two Money with same value → are equal
3. Add two Money values → returns new Money with sum
4. Subtract Money → returns difference
5. Negative amount → throws `ArgumentException`
6. Multiply by percentage → fee calculation
7. Different currencies → throws `CurrencyMismatchException`

### Starting Test

```csharp
[Fact]
public void Create_WhenPositiveAmount_ShouldStoreValue()
{
    var money = Money.From(100.50m);
    money.Value.Should().Be(100.50m);
}
```

### Expected Final Interface

```csharp
public sealed record Money
{
    public decimal Value { get; }
    public static Money From(decimal value);
    public static Money Zero => From(0m);
    public static Money operator +(Money a, Money b);
    public static Money operator -(Money a, Money b);
    public static Money operator *(Money a, decimal multiplier);
}
```

**Skills practiced:** Value objects, operator overloading, guard clauses, equality.

## Kata 2: Escrow State Machine (Intermediate)

**Goal:** Build escrow status transitions with TDD, enforcing valid state transitions.

### Increment Plan

1. New escrow → status is Pending
2. Fund a Pending escrow → status becomes Funded
3. Fund an already Funded escrow → throws `InvalidOperationException`
4. Release a Funded escrow → status becomes Released
5. Release a Pending escrow → throws (not funded yet)
6. Dispute a Funded escrow → status becomes Disputed
7. Cancel a Pending escrow → status becomes Cancelled
8. Cancel a Funded escrow → throws (must dispute first)
9. Resolve a Disputed escrow → status becomes Released or Refunded

### Starting Test

```csharp
[Fact]
public void NewEscrow_ShouldHavePendingStatus()
{
    var escrow = EscrowTransaction.Create(
        UserId.New(), UserId.New(), Money.From(1000m));
    escrow.Status.Should().Be(EscrowStatus.Pending);
}
```

### State Diagram (Target)

```
         Fund()         Release()
Pending ────────▶ Funded ────────▶ Released
   │                │
   │ Cancel()       │ Dispute()
   ▼                ▼
Cancelled       Disputed ────────▶ Released
                    │                 │
                    │ Resolve()       │
                    └────────────────▶ Refunded
```

**Skills practiced:** State machines, domain events, guard clauses, rich domain model.

## Kata 3: Fee Calculator with Strategy Pattern (Intermediate)

**Goal:** Build a fee calculator using TDD, letting the tests drive toward the Strategy pattern.

### Increment Plan

1. Standard escrow → 2.5% fee
2. Premium escrow → 1.5% fee
3. Enterprise escrow → 1.0% fee
4. Fee has minimum of $1.00
5. Fee has maximum of $500.00
6. Unknown escrow type → throws
7. **Refactor:** Extract Strategy pattern (tests already green — just restructure)

### Key Learning

The Strategy pattern should **emerge** from refactoring, not be planned upfront. After cycle 3, you'll have a switch statement with 3 cases — that's when the pattern naturally appears in the REFACTOR phase.

```csharp
// After Cycle 3 GREEN (switch statement)
public Money Calculate(EscrowType type, Money amount) => type switch
{
    EscrowType.Standard => amount * 0.025m,
    EscrowType.Premium => amount * 0.015m,
    EscrowType.Enterprise => amount * 0.010m,
    _ => throw new ArgumentOutOfRangeException(nameof(type))
};

// After Cycle 3 REFACTOR (Strategy pattern emerges)
private static readonly Dictionary<EscrowType, decimal> FeeRates = new()
{
    [EscrowType.Standard] = 0.025m,
    [EscrowType.Premium] = 0.015m,
    [EscrowType.Enterprise] = 0.010m
};
```

**Skills practiced:** Triangulation, emergent design, knowing when to introduce patterns.

## Kata 4: Escrow Notification Pipeline (Advanced)

**Goal:** Build a notification system using TDD with MediatR domain events.

### Increment Plan

1. Escrow funded → publishes `EscrowFundedEvent`
2. Event handler sends email to buyer
3. Event handler sends email to seller
4. Email service unavailable → logs warning, doesn't throw
5. Multiple handlers execute independently
6. Add SMS notification handler alongside email

**Skills practiced:** Observer pattern, MediatR notifications, error handling, resilience.

## Kata Ground Rules

1. **No peeking ahead** — Don't read the final solution before starting
2. **Time each cycle** — Keep 🔴→🟢→🔵 under 10 minutes
3. **Commit after each GREEN** — Practice small, atomic commits
4. **Delete and redo** — The value is in the practice, not the code
5. **Pair if possible** — One person writes test, other writes code

## Progression Path

```
Kata 1 (Money)          → Value objects, equality, operators
Kata 2 (State Machine)  → Domain modeling, state transitions
Kata 3 (Fee Calculator) → Emergent design, Strategy pattern
Kata 4 (Notifications)  → Domain events, MediatR, resilience
```

Each kata builds on concepts from the previous one. Complete them in order.
