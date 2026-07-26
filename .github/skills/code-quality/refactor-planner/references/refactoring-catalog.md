# Refactoring Techniques Catalog

Reference of refactoring techniques with when to apply, risk level, and .NET examples.

## Extract Method
**When:** Long method, duplicated code block, commented code section.
**Risk:** 🟢 Low — internal restructure only.

```csharp
// BEFORE: One method, multiple responsibilities
public async Task<Result> ProcessEscrowAsync(EscrowCommand cmd, CancellationToken ct)
{
    // 15 lines of validation
    if (cmd.Amount <= 0) return Result.Fail("Invalid amount");
    if (cmd.BuyerId == Guid.Empty) return Result.Fail("Buyer required");
    // ... more validation

    // 20 lines of state transition
    var escrow = await _repo.GetByIdAsync(cmd.EscrowId, ct);
    escrow.TransitionTo(EscrowStatus.Funded);
    // ... more state logic

    // 10 lines of notification
    await _notifier.SendAsync(escrow.SellerId, "Escrow funded", ct);
    // ... more notifications
}

// AFTER: Orchestrator calling focused methods
public async Task<Result> ProcessEscrowAsync(EscrowCommand cmd, CancellationToken ct)
{
    var validationResult = ValidateCommand(cmd);
    if (validationResult.IsFailure) return validationResult;

    var escrow = await TransitionEscrowStateAsync(cmd, ct);
    await NotifyPartiesAsync(escrow, ct);
    return Result.Ok();
}
```

## Extract Class
**When:** Large class (>300 lines), divergent change, data clumps.
**Risk:** 🟡 Medium — may require DI registration changes.

```csharp
// BEFORE: God class
public class EscrowService : IEscrowService
{
    // Validation logic (100 lines)
    // Fee calculation (80 lines)
    // State management (120 lines)
    // Notification dispatch (60 lines)
}

// AFTER: Focused classes
public sealed class EscrowValidator : IEscrowValidator { /* validation */ }
public sealed class EscrowFeeCalculator : IFeeCalculator { /* fees */ }
public sealed class EscrowStateManager : IEscrowStateManager { /* state */ }
public sealed class EscrowNotifier : IEscrowNotifier { /* notifications */ }

// DI update required:
services.AddScoped<IEscrowValidator, EscrowValidator>();
services.AddScoped<IFeeCalculator, EscrowFeeCalculator>();
```

## Replace Conditional with Polymorphism
**When:** Switch/if-else chains on type or status that keep growing.
**Risk:** 🟡 Medium — introduces new class hierarchy.

```csharp
// BEFORE: Switch on escrow type
public decimal CalculateFee(EscrowType type, decimal amount) => type switch
{
    EscrowType.Standard => amount * 0.025m,
    EscrowType.Premium => amount * 0.015m,
    EscrowType.Enterprise => amount * 0.01m,
    _ => throw new ArgumentException($"Unknown type: {type}")
};

// AFTER: Strategy pattern
public interface IFeeStrategy
{
    EscrowType Type { get; }
    Money CalculateFee(Money amount);
}

public sealed class StandardFeeStrategy : IFeeStrategy
{
    public EscrowType Type => EscrowType.Standard;
    public Money CalculateFee(Money amount) => amount * 0.025m;
}
```

## Introduce Parameter Object
**When:** Long parameter list (>4 params), data clumps across signatures.
**Risk:** 🟡 Medium — changes method signatures.

```csharp
// BEFORE
public async Task CreateEscrow(string buyerId, string sellerId,
    decimal amount, string currency, string description, DateTime deadline)

// AFTER
public record CreateEscrowRequest(
    UserId BuyerId, UserId SellerId, Money Amount,
    string Description, DateTime Deadline);

public async Task CreateEscrow(CreateEscrowRequest request)
```

## Replace Primitive with Value Object
**When:** Primitive obsession — domain concepts as `string`, `int`, `decimal`.
**Risk:** 🟡 Medium — type changes propagate through layers.

```csharp
// Value object with domain validation
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new DomainException("Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(currency)) throw new DomainException("Currency required");
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);
    public static Money operator *(Money money, decimal factor) =>
        new(money.Amount * factor, money.Currency);
}
```

## Introduce Interface
**When:** Tight coupling, DIP violation, testability needs.
**Risk:** 🟢 Low — additive, non-breaking change.

## Move Method / Move Field
**When:** Feature envy, inappropriate intimacy.
**Risk:** 🟢 Low — behavioral relocation.

## Replace Inheritance with Composition
**When:** Refused bequest, fragile base class, parallel hierarchies.
**Risk:** 🔴 High — restructures class hierarchy.

## Technique Selection Quick Guide

| Smell | Primary Technique | Risk |
|-------|------------------|------|
| Long Method | Extract Method | 🟢 |
| Large Class | Extract Class | 🟡 |
| Switch Statements | Replace with Polymorphism | 🟡 |
| Long Parameter List | Introduce Parameter Object | 🟡 |
| Primitive Obsession | Replace with Value Object | 🟡 |
| Feature Envy | Move Method | 🟢 |
| Tight Coupling | Introduce Interface | 🟢 |
| Fragile Inheritance | Composition over Inheritance | 🔴 |
| Duplicate Code | Extract Method/Class | 🟢-🟡 |
