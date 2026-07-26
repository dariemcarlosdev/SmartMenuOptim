# Code Smells Catalog

Comprehensive catalog of code smells organized by category with detection criteria for .NET projects.

## Bloaters

| Smell | Detection Criteria | Severity |
|-------|-------------------|----------|
| **Long Method** | Method exceeds 30 lines or cyclomatic complexity > 10 | Medium |
| **Large Class** | Class exceeds 300 lines or has more than 7 public methods | Medium |
| **Long Parameter List** | Method accepts more than 4 parameters | Low-Medium |
| **Data Clumps** | Same group of 3+ fields/parameters appears in multiple places | Medium |
| **Primitive Obsession** | Domain concepts as primitives (`string email`, `int status`, `decimal amount`) | Medium |

### Primitive Obsession in .NET — Common Offenders
```csharp
// SMELL: Primitives for domain concepts
public class EscrowTransaction
{
    public string BuyerEmail { get; set; }     // Should be Email value object
    public decimal Amount { get; set; }         // Should be Money value object
    public int Status { get; set; }             // Should be EscrowStatus enum
    public string TransactionId { get; set; }   // Should be TransactionId value object
}

// CLEAN: Value objects encode domain rules
public sealed class EscrowTransaction
{
    public Email BuyerEmail { get; init; }
    public Money Amount { get; init; }
    public EscrowStatus Status { get; private set; }
    public TransactionId Id { get; init; }
}
```

## Object-Orientation Abusers

| Smell | Detection Criteria |
|-------|-------------------|
| **Switch Statements** | Switch/if-else chains on type or status that should be polymorphism |
| **Refused Bequest** | Subclass overrides parent methods to do nothing or throw `NotSupportedException` |
| **Temporary Field** | Fields only used in certain scenarios, null/default otherwise |
| **Alternative Classes** | Multiple classes doing the same thing with different interfaces |

### Switch Statement Smell
```csharp
// SMELL: Switch that grows with each new payment type
decimal fee = paymentType switch
{
    PaymentType.CreditCard => amount * 0.029m,
    PaymentType.BankTransfer => 1.50m,
    PaymentType.Crypto => amount * 0.01m,
    // New payment types require modifying this method (OCP violation)
    _ => throw new ArgumentException("Unknown payment type")
};

// CLEAN: Strategy pattern
public interface IFeeCalculator
{
    PaymentType Type { get; }
    Money Calculate(Money amount);
}
// Each payment type has its own calculator registered in DI
```

## Change Preventers

| Smell | Detection Criteria |
|-------|-------------------|
| **Divergent Change** | One class changed for many different reasons (SRP violation) |
| **Shotgun Surgery** | One change requires edits to 5+ classes |
| **Parallel Inheritance** | Creating subclass in one hierarchy requires subclass in another |

### Divergent Change Detection
Ask: "What reasons would cause this class to change?" If the answer is more than one, it's divergent change.

Example: `EscrowService` that changes when:
- Validation rules change → Extract `EscrowValidator`
- Fee calculation changes → Extract `FeeCalculator`
- Notification logic changes → Extract `EscrowNotifier`
- Persistence approach changes → Extract `IEscrowRepository`

## Dispensables

| Smell | Detection Criteria |
|-------|-------------------|
| **Dead Code** | Unreachable code, unused variables, commented-out blocks |
| **Speculative Generality** | Abstract classes, interfaces, parameters "just in case" with only one implementation and no foreseeable need |
| **Duplicate Code** | Identical or near-identical logic in 2+ locations |
| **Lazy Class** | Class that does too little to justify its existence (< 20 lines, pure delegation) |

### Dead Code Red Flags in .NET
- Methods with `// TODO: implement` that have been there for months
- `#if DEBUG` blocks with stale code
- Event handlers subscribed but never triggered
- `[Obsolete]` members still in active use paths
- Constructor parameters assigned to fields never read

## Couplers

| Smell | Detection Criteria |
|-------|-------------------|
| **Feature Envy** | Method accesses data of another class more than its own |
| **Inappropriate Intimacy** | Two classes access each other's private/internal members |
| **Message Chains** | `a.GetB().GetC().GetD().DoThing()` — Law of Demeter violations |
| **Middle Man** | Class that only delegates to another class without adding value |

### Feature Envy Example
```csharp
// SMELL: This method belongs on Customer, not EscrowService
public decimal CalculateDiscount(Customer customer)
{
    if (customer.TotalOrders > 100 && customer.MemberSince.Year < 2020
        && customer.LoyaltyTier == Tier.Gold)
        return customer.BaseDiscount * 1.5m;
    return customer.BaseDiscount;
}

// CLEAN: Move to Customer where the data lives
public decimal CalculateDiscount() =>
    TotalOrders > 100 && MemberSince.Year < 2020 && LoyaltyTier == Tier.Gold
        ? BaseDiscount * 1.5m
        : BaseDiscount;
```

## Smell Priority Matrix

| Impact \ Frequency | Rare | Occasional | Frequent |
|---------------------|------|-----------|----------|
| **High** | Fix when touched | Plan fix | Fix now |
| **Medium** | Note for later | Plan fix | Fix soon |
| **Low** | Ignore | Note for later | Plan fix |
